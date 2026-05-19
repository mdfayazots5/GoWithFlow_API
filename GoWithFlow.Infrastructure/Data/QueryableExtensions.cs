using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Npgsql.EntityFrameworkCore.PostgreSQL;

namespace GoWithFlow.Infrastructure.Data;

public static class QueryableExtensions
{
	public static IQueryable<T> WhereNameContains<T>(
		this IQueryable<T> query,
		Expression<Func<T, string?>> selector,
		string? searchTerm,
		string provider)
	{
		if (string.IsNullOrWhiteSpace(searchTerm))
		{
			return query;
		}

		var normalizedSearchTerm = searchTerm.Trim();
		var parameter = selector.Parameters[0];

		if (DatabaseProviderNames.IsPostgreSql(provider))
		{
			var ilikeExpression = Expression.Call(
				typeof(NpgsqlDbFunctionsExtensions),
				nameof(NpgsqlDbFunctionsExtensions.ILike),
				Type.EmptyTypes,
				Expression.Property(null, typeof(EF), nameof(EF.Functions)),
				selector.Body,
				Expression.Constant($"%{normalizedSearchTerm}%"));

			return query.Where(Expression.Lambda<Func<T, bool>>(ilikeExpression, parameter));
		}

		var containsMethod = typeof(string).GetMethod(nameof(string.Contains), new[] { typeof(string) })
			?? throw new InvalidOperationException("string.Contains(string) could not be resolved.");
		var containsExpression = Expression.Call(selector.Body, containsMethod, Expression.Constant(normalizedSearchTerm));

		return query.Where(Expression.Lambda<Func<T, bool>>(containsExpression, parameter));
	}
}
