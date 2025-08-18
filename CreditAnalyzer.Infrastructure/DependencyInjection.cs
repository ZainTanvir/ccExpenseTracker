using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Minio;
using CreditAnalyzer.Infrastructure.Persistence.Db;
using CreditAnalyzer.Application.Abstractions;
using CreditAnalyzer.Infrastructure.FileStorage;

namespace CreditAnalyzer.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        // EF Core (SQL Server)
        services.AddDbContext<AppDbContext>(opt =>
            opt.UseSqlServer(config.GetConnectionString("Default")));

        // Unit of Work & Repository (if you added them)
        services.AddScoped<IUnitOfWork, CreditAnalyzer.Infrastructure.Persistence.EfUnitOfWork>();
        services.AddScoped(typeof(IRepository<>), typeof(CreditAnalyzer.Infrastructure.Persistence.EfRepository<>));

        // MinIO client + storage
        var minio = config.GetSection("Minio");
        services.AddSingleton<IMinioClient>(_ => new MinioClient()
            .WithEndpoint(minio["Endpoint"])
            .WithCredentials(minio["AccessKey"], minio["SecretKey"])
            .Build());
        services.AddSingleton<IFileStorage>(sp =>
            new MinioFileStorage(sp.GetRequiredService<IMinioClient>(), minio["Bucket"] ?? "statements"));

        return services;
    }
}