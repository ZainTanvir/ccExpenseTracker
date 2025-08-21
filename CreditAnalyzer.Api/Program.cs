using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.RateLimiting;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using CreditAnalyzer.Infrastructure;
using CreditAnalyzer.Infrastructure.Options;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------
// Services (startup only)
// ---------------------------
builder.Host.UseSerilog((ctx, cfg) =>
    cfg.ReadFrom.Configuration(ctx.Configuration)
       .Enrich.FromLogContext()
       .Enrich.WithMachineName()
       .WriteTo.Console());

builder.Services.AddControllers()
    .AddJsonOptions(o =>
    {
        // Consistent JSON casing/enum handling if you need it later
        o.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    });

// FluentValidation: auto-run validators discovered in Api assembly
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<CreditAnalyzer.Api.Validation.LoginRequestValidator>();

// Strongly-typed options (production-friendly, hot-reload via IOptionsMonitor)
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));
builder.Services.Configure<MinioOptions>(builder.Configuration.GetSection("Minio"));

// Swagger (dev-only UI later in pipeline)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// API Versioning (+ explorer for Swagger grouping)
builder.Services.AddApiVersioning(opt =>
{
    opt.DefaultApiVersion = new ApiVersion(1, 0);
    opt.AssumeDefaultVersionWhenUnspecified = true;
    opt.ReportApiVersions = true;
});
builder.Services.AddVersionedApiExplorer(opt =>
{
    opt.GroupNameFormat = "'v'VVV";          // v1, v1.1
    opt.SubstituteApiVersionInUrl = true;
});

// CORS (tighten origins to your UI domains)
builder.Services.AddCors(opt => opt.AddPolicy("Default", p =>
    p.WithOrigins("http://localhost:5173")   // TODO: replace with your frontend origin(s)
     .AllowAnyHeader()
     .AllowAnyMethod()
     .AllowCredentials()));

// Rate limiting (per-IP policies you can opt-into via [EnableRateLimiting])
builder.Services.AddRateLimiter(opt =>
{
    opt.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    opt.AddFixedWindowLimiter("login", options =>
    {
        options.Window = TimeSpan.FromMinutes(1);
        options.PermitLimit = 5;    // 5 login attempts/minute per IP
        options.QueueLimit = 0;
        options.AutoReplenishment = true;
    });

    opt.AddTokenBucketLimiter("uploads", options =>
    {
        options.TokenLimit = 20;    // 20 uploads/min per IP
        options.TokensPerPeriod = 20;
        options.ReplenishmentPeriod = TimeSpan.FromMinutes(1);
        options.AutoReplenishment = true;
        options.QueueLimit = 0;
    });
});

// AuthN/Z
var jwt = builder.Configuration.GetSection("Jwt");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = jwt["Issuer"],
            ValidAudience = jwt["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Key"]!)),
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true
        };
    });
builder.Services.AddAuthorization();

// Health checks
builder.Services.AddHealthChecks();

// Infra (DbContext, Repos/UoW, MinIO, etc.)
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

// ---------------------------
// Pipeline (per-request; order matters)
// ---------------------------
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Apply migrations & seed with simple retry (configurable flags)
await app.ApplyMigrationsAndSeedAsync();

app.UseSerilogRequestLogging();

app.UseGlobalExceptionHandler();    // ProblemDetails for unhandled exceptions

app.UseHttpsRedirection();

app.UseCors("Default");

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

// Health endpoints
app.MapHealthChecks("/health/live");
app.MapHealthChecks("/health/ready");

// Map controllers (remember to version your controllers/routes: [ApiVersion("1.0")] + /api/v{version:apiVersion}/...)
app.MapControllers();

app.Run();


// ---------------------------
// Global exception middleware
// ---------------------------
public static class GlobalExceptionMiddleware
{
    public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder app)
    {
        return app.Use(async (context, next) =>
        {
            try
            {
                await next();
            }
            catch (Exception ex)
            {
                var env = context.RequestServices.GetRequiredService<IWebHostEnvironment>();
                var logger = context.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("GlobalException");
                logger.LogError(ex, "Unhandled exception");

                var status = StatusCodes.Status500InternalServerError;
                var problem = new ProblemDetails
                {
                    Status = status,
                    Title = "An unexpected error occurred.",
                    Type = "https://httpstatuses.com/500",
                    Detail = env.IsDevelopment() ? ex.ToString() : null
                };

                context.Response.StatusCode = status;
                context.Response.ContentType = "application/problem+json";
                await context.Response.WriteAsync(JsonSerializer.Serialize(problem));
            }
        });
    }
}

// ---------------------------
// Startup data bootstrap (migrate + seed with retry)
// ---------------------------
public static class MigrationBootstrapper
{
    public static async Task ApplyMigrationsAndSeedAsync(this WebApplication app)
    {
        // Flags in appsettings.*.json:
        // { "Data": { "MigrateOnStartup": true, "SeedOnStartup": true } }
        var cfg = app.Configuration;
        var migrate = cfg.GetValue("Data:MigrateOnStartup", true);
        var seed = cfg.GetValue("Data:SeedOnStartup", true);

        if (!migrate && !seed) return;

        await using var scope = app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<CreditAnalyzer.Infrastructure.Persistence.Db.AppDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");

        var attempt = 0;
        var maxAttempts = 5;

        while (true)
        {
            try
            {
                if (migrate)
                {
                    await db.Database.MigrateAsync();
                    logger.LogInformation("EF migrations applied.");
                }
                if (seed)
                {
                    await CreditAnalyzer.Infrastructure.Persistence.Db.DbSeeder.SeedAsync(db);
                    logger.LogInformation("Seed completed.");
                }
                break;
            }
            catch (Exception ex) when (++attempt < maxAttempts)
            {
                var delay = TimeSpan.FromSeconds(2 * attempt);
                logger.LogWarning(ex, "Database not ready. Retry {Attempt}/{Max} in {Delay}s…", attempt, maxAttempts, delay.TotalSeconds);
                await Task.Delay(delay);
            }
        }
    }
}
