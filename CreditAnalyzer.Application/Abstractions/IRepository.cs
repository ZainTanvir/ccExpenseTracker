using System.Linq.Expressions;

namespace CreditAnalyzer.Application.Abstractions;
public interface IRepository<T> where T : class
{
    Task AddAsync(T entity, CancellationToken ct = default);
    Task AddRangeAsync(IEnumerable<T> entities, CancellationToken ct = default);
    Task<T?> FirstOrDefaultAsync(Expression<Func<T,bool>> predicate, CancellationToken ct = default);
    Task<bool> AnyAsync(Expression<Func<T,bool>> predicate, CancellationToken ct = default);
    IQueryable<T> Query(); // for read-only projections
}