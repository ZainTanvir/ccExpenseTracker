using CreditAnalyzer.Application.Abstractions;
using CreditAnalyzer.Infrastructure.Persistence.Db;

namespace CreditAnalyzer.Infrastructure.Persistence;

public class EfUnitOfWork(AppDbContext db) : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken ct = default) => db.SaveChangesAsync(ct);
}