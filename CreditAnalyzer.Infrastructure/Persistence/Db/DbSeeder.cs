// Infrastructure/Persistence/Db/DbSeeder.cs
using BCrypt.Net;
using CreditAnalyzer.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CreditAnalyzer.Infrastructure.Persistence.Db;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        await db.Database.MigrateAsync();

        if (!await db.Users.AnyAsync())
        {
            var admin = new User
            {
                Email = "admin@local",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
                Role = "Admin"
            };
            db.Users.Add(admin);
            db.Accounts.Add(new Account { User = admin, Name = "Primary Card", Currency = "USD" });

            foreach (var n in new[] { "Food","Travel","Shopping","Bills","Groceries","Health","Entertainment","Other" })
                db.Categories.Add(new Category { Name = n });

            await db.SaveChangesAsync();
        }
    }
}