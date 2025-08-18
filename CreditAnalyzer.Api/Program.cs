using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using CreditAnalyzer.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// =========================
// SERVICE REGISTRATION (runs once at startup)
// =========================
builder.Services.AddControllers();

builder.Host.UseSerilog((ctx, cfg) =>
    cfg.ReadFrom.Configuration(ctx.Configuration)
       .Enrich.FromLogContext()
       .Enrich.WithMachineName()
       .WriteTo.Console());

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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

// Infra (DbContext, Repos/UoW, MinIO, etc.)
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

// =========================
// MIDDLEWARE PIPELINE (runs per request; order matters)
// =========================
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();               // MIDDLEWARE: serves /swagger/v1/swagger.json
    app.UseSwaggerUI();             // MIDDLEWARE: serves Swagger UI
}

app.UseSerilogRequestLogging();     // MIDDLEWARE: logs each HTTP request summary

app.UseGlobalExceptionHandler();    // MIDDLEWARE: catch-all -> ProblemDetails JSON

app.UseHttpsRedirection();          // MIDDLEWARE: redirect HTTP -> HTTPS (optional in dev)

app.UseAuthentication();            // MIDDLEWARE: validates JWT, sets HttpContext.User
app.UseAuthorization();             // MIDDLEWARE: enforces [Authorize] policies

app.MapControllers();               // endpoint mapping (routes -> controllers)

app.Run();


// =========================
// Global exception middleware (minimal, centralized ProblemDetails)
// =========================
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
                var logger = context.RequestServices.GetRequiredService<ILoggerFactory>()
                                                 .CreateLogger("GlobalException");
                logger.LogError(ex, "Unhandled exception");

                var status = (int)HttpStatusCode.InternalServerError;
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
