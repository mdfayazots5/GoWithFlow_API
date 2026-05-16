using System.Linq.Expressions;

namespace GoWithFlow.Application.Interfaces.Repositories;

public interface IGenericRepository<T>
	where T : class
{
	Task<T?> GetByIdAsync(long id, CancellationToken cancellationToken = default);

	Task AddAsync(T entity, CancellationToken cancellationToken = default);

	void Update(T entity);

	Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);

	Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
