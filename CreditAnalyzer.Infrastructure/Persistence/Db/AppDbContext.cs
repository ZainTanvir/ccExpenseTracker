// CreditAnalyzer.Infrastructure/Persistence/Db/AppDbContext.cs
using System.Text.Json;
using CreditAnalyzer.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace CreditAnalyzer.Infrastructure.Persistence.Db;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User>        Users        => Set<User>();
    public DbSet<Account>     Accounts     => Set<Account>();
    public DbSet<Category>    Categories   => Set<Category>();
    public DbSet<Statement>   Statements   => Set<Statement>();
    public DbSet<Transaction> Transactions => Set<Transaction>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        // USERS
        b.Entity<User>(e =>
        {
            e.ToTable("users");
            e.HasKey(x => x.Id);
            e.Property(x => x.Email).IsRequired();
            e.HasIndex(x => x.Email).IsUnique();          // unique email
            e.Property(x => x.PasswordHash).IsRequired();
            e.Property(x => x.Role).IsRequired();         // "User" | "Admin"
            e.Property(x => x.CreatedAt).IsRequired();
        });

        // ACCOUNTS
        b.Entity<Account>(e =>
        {
            e.ToTable("accounts");
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).IsRequired();
            e.Property(x => x.Currency).HasMaxLength(3).IsRequired();
            e.Property(x => x.CreatedAt).IsRequired();

            e.HasOne(x => x.User)
             .WithMany(u => u.Accounts)
             .HasForeignKey(x => x.UserId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // CATEGORIES
        b.Entity<Category>(e =>
        {
            e.ToTable("categories");
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).IsRequired();
            e.HasIndex(x => x.Name).IsUnique();           // unique category names (global)
            e.HasOne(x => x.Parent)
             .WithMany()
             .HasForeignKey(x => x.ParentId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // STATEMENTS
        b.Entity<Statement>(e =>
        {
            e.ToTable("statements");
            e.HasKey(x => x.Id);
            e.Property(x => x.ObjectKey).IsRequired();    // MinIO object key
            e.Property(x => x.OriginalFilename).IsRequired();
            e.Property(x => x.UploadedAt).IsRequired();

            // DateOnly maps to SQL Server 'date' (EF Core 8/9)
            e.Property(x => x.PeriodStart).HasColumnType("date");
            e.Property(x => x.PeriodEnd).HasColumnType("date");

            e.HasOne(x => x.Account)
             .WithMany(a => a.Statements)
             .HasForeignKey(x => x.AccountId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // TRANSACTIONS
        b.Entity<Transaction>(e =>
        {
            e.HasOne(x => x.Statement)
                .WithMany(s => s.Transactions)
                .HasForeignKey(x => x.StatementId)
                .OnDelete(DeleteBehavior.Cascade);  

            e.HasOne(x => x.Account)
                .WithMany()
                .HasForeignKey(x => x.AccountId)
                .OnDelete(DeleteBehavior.NoAction);  

            e.HasOne(x => x.Category)
                .WithMany()
                .HasForeignKey(x => x.CategoryId)
                .OnDelete(DeleteBehavior.SetNull);   // ok
        });
    }
}
