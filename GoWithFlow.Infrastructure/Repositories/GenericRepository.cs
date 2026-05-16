using System.Linq.Expressions;
using GoWithFlow.Application.Interfaces.Repositories;
using GoWithFlow.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GoWithFlow.Infrastructure.Repositories;

public class GenericRepository<T> : IGenericRepository<T>
	where T : class
{
	protected readonly GoWithFlowDbContext DbContext;
	protected readonly DbSet<T> DbSet;

	public GenericRepository(GoWithFlowDbContext dbContext)
	{
		DbContext = dbContext;
		DbSet = dbContext.Set<T>();
	}

	public virtual async Task<T?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
	{
		return await DbSet.FindAsync(new object[] { id }, cancellationToken);
	}

	public virtual async Task AddAsync(T entity, CancellationToken cancellationToken = default)
	{
		await DbSet.AddAsync(entity, cancellationToken);
	}

	public virtual void Update(T entity)
	{
		DbSet.Update(entity);
	}

	public virtual async Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
	{
		return await DbSet.AnyAsync(predicate, cancellationToken);
	}

	public virtual async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
	{
		return await DbContext.SaveChangesAsync(cancellationToken);
	}
}
