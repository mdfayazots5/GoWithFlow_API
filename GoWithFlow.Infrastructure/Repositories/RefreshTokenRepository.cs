using System.Data;
using System.Data.Common;
using GoWithFlow.Application.Interfaces.Repositories;
using GoWithFlow.Domain.Entities;
using GoWithFlow.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GoWithFlow.Infrastructure.Repositories;

public sealed class RefreshTokenRepository : GenericRepository<RefreshToken>, IRefreshTokenRepository
{
	public RefreshTokenRepository(GoWithFlowDbContext dbContext)
		: base(dbContext)
	{
	}

	public async Task<long> InsertRefreshTokenAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default)
	{
		var connection = DbContext.Database.GetDbConnection();
		await EnsureConnectionOpenAsync(connection, cancellationToken);

		await using var command = connection.CreateCommand();
		command.CommandText = DbCommandHelper.QualifyRoutineName(DbContext.DatabaseProvider, "dbo.uspInsertRefreshToken");
		command.CommandType = CommandType.StoredProcedure;

		command.Parameters.Add(CreateParameter("@UserId", refreshToken.UserId));
		command.Parameters.Add(CreateParameter("@Token", refreshToken.Token));
		command.Parameters.Add(CreateParameter("@ExpiresAt", refreshToken.ExpiresAt));
		command.Parameters.Add(CreateParameter("@DeviceInfo", refreshToken.DeviceInfo));
		command.Parameters.Add(CreateParameter("@CreatedBy", refreshToken.CreatedBy));
		command.Parameters.Add(CreateParameter("@IPAddress", refreshToken.IPAddress));

		var result = await command.ExecuteScalarAsync(cancellationToken);

		return Convert.ToInt64(result);
	}

	public async Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default)
	{
		return await DbContext.RefreshTokens
			.AsNoTracking()
			.FirstOrDefaultAsync(
				refreshToken => refreshToken.Token == token && refreshToken.IsDeleted == false,
				cancellationToken);
	}

	public async Task RevokeRefreshTokenAsync(string token, string updatedBy, string ipAddress, CancellationToken cancellationToken = default)
	{
		var connection = DbContext.Database.GetDbConnection();
		await EnsureConnectionOpenAsync(connection, cancellationToken);

		await using var command = connection.CreateCommand();
		command.CommandText = DbCommandHelper.QualifyRoutineName(DbContext.DatabaseProvider, "dbo.uspRevokeRefreshToken");
		command.CommandType = CommandType.StoredProcedure;

		command.Parameters.Add(CreateParameter("@Token", token));
		command.Parameters.Add(CreateParameter("@UpdatedBy", updatedBy));
		command.Parameters.Add(CreateParameter("@IPAddress", ipAddress));

		await command.ExecuteNonQueryAsync(cancellationToken);
	}

	private DbParameter CreateParameter(string parameterName, object? value)
	{
		return DbCommandHelper.CreateParameter(DbContext.DatabaseProvider, parameterName, value);
	}

	private static async Task EnsureConnectionOpenAsync(System.Data.Common.DbConnection connection, CancellationToken cancellationToken)
	{
		if (connection.State != ConnectionState.Open)
		{
			await connection.OpenAsync(cancellationToken);
		}
	}
}
