using System.Linq.Expressions;
using CreditAnalyzer.Application.Abstractions;
using CreditAnalyzer.Infrastructure.Persistence.Db;
using Microsoft.EntityFrameworkCore;

namespace CreditAnalyzer.Infrastructure.Persistence;

public class EfRepository<T>(AppDbContext db) : IRepository<T> where T : class
{
    public Task AddAsync(T entity, CancellationToken ct = default) => db.Set<T>().AddAsync(entity, ct).AsTask();
    public Task AddRangeAsync(IEnumerable<T> entities, CancellationToken ct = default) { db.Set<T>().AddRange(entities); return Task.CompletedTask; }
    public Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default)
        => db.Set<T>().FirstOrDefaultAsync(predicate, ct);
    public Task<bool> AnyAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default)
        => db.Set<T>().AnyAsync(predicate, ct);
    public IQueryable<T> Query() => db.Set<T>().AsNoTracking();
}