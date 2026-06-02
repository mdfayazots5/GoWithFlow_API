using System.Data;
using System.Data.Common;
using GoWithFlow.Application.DTOs.Responses.Session;
using GoWithFlow.Application.Interfaces.Repositories;
using GoWithFlow.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GoWithFlow.Infrastructure.Repositories;

public sealed class SessionInvitationRepository : ISessionInvitationRepository
{
	private readonly GoWithFlowDbContext _dbContext;

	public SessionInvitationRepository(GoWithFlowDbContext dbContext)
	{
		_dbContext = dbContext;
	}

	public async Task<long> InsertInvitationAsync(
		long sessionId, long userId, byte slotIndex, string slotName,
		DateTime? expiresAt, string createdBy, string ipAddress,
		CancellationToken cancellationToken = default)
	{
		var connection = _dbContext.Database.GetDbConnection();
		await EnsureConnectionOpenAsync(connection, cancellationToken);

		await using var command = CreateCommand(connection, "dbo.uspInsertSessionInvitation", null);
		command.Parameters.Add(CreateParameter("@SessionId", sessionId));
		command.Parameters.Add(CreateParameter("@UserId", userId));
		command.Parameters.Add(CreateParameter("@SlotIndex", slotIndex));
		command.Parameters.Add(CreateParameter("@SlotName", slotName));
		command.Parameters.Add(CreateParameter("@ExpiresAt", (object?)expiresAt ?? DBNull.Value));
		command.Parameters.Add(CreateParameter("@CreatedBy", createdBy));
		command.Parameters.Add(CreateParameter("@IPAddress", ipAddress));

		await using var reader = await DbCommandHelper.ExecuteReaderAsync(command, cancellationToken);

		if (await reader.ReadAsync(cancellationToken))
		{
			return Convert.ToInt64(reader.GetValue(0));
		}

		return 0;
	}

	public async Task UpdateInvitationStatusAsync(
		long invitationId, long userId, string status,
		string updatedBy, string ipAddress,
		CancellationToken cancellationToken = default)
	{
		var connection = _dbContext.Database.GetDbConnection();
		await EnsureConnectionOpenAsync(connection, cancellationToken);

		await using var command = CreateCommand(connection, "dbo.uspUpdateInvitationStatus", null);
		command.Parameters.Add(CreateParameter("@InvitationId", invitationId));
		command.Parameters.Add(CreateParameter("@UserId", userId));
		command.Parameters.Add(CreateParameter("@Status", status));
		command.Parameters.Add(CreateParameter("@UpdatedBy", updatedBy));
		command.Parameters.Add(CreateParameter("@IPAddress", ipAddress));

		await DbCommandHelper.ExecuteNonQueryAsync(command, cancellationToken);
	}

	public async Task CancelInvitationAsync(
		long invitationId, long sessionId, string updatedBy,
		CancellationToken cancellationToken = default)
	{
		var connection = _dbContext.Database.GetDbConnection();
		await EnsureConnectionOpenAsync(connection, cancellationToken);

		await using var command = CreateCommand(connection, "dbo.uspCancelSessionInvitation", null);
		command.Parameters.Add(CreateParameter("@InvitationId", invitationId));
		command.Parameters.Add(CreateParameter("@SessionId", sessionId));
		command.Parameters.Add(CreateParameter("@UpdatedBy", updatedBy));

		await DbCommandHelper.ExecuteNonQueryAsync(command, cancellationToken);
	}

	public async Task<List<SessionInvitationDto>> GetInvitationsBySessionIdAsync(
		long sessionId,
		CancellationToken cancellationToken = default)
	{
		var connection = _dbContext.Database.GetDbConnection();
		await EnsureConnectionOpenAsync(connection, cancellationToken);

		await using var command = CreateCommand(connection, "dbo.uspGetInvitationsBySessionId", null);
		command.Parameters.Add(CreateParameter("@SessionId", sessionId));

		var results = new List<SessionInvitationDto>();

		await using var reader = await DbCommandHelper.ExecuteReaderAsync(command, cancellationToken);

		while (await reader.ReadAsync(cancellationToken))
		{
			results.Add(new SessionInvitationDto
			{
				InvitationId = GetInt64(reader, "InvitationId"),
				SessionId    = GetInt64(reader, "SessionId"),
				UserId       = GetInt64(reader, "UserId"),
				SlotIndex    = GetByte(reader, "SlotIndex"),
				SlotName     = GetString(reader, "SlotName"),
				Status       = GetString(reader, "Status"),
				SentAt       = GetDateTime(reader, "SentAt"),
				RespondedAt  = GetNullableDateTime(reader, "RespondedAt"),
				ExpiresAt    = GetNullableDateTime(reader, "ExpiresAt"),
				FullName     = GetString(reader, "FullName"),
				AvatarUrl    = GetNullableString(reader, "AvatarUrl")
			});
		}

		return results;
	}

	public async Task<List<UserInvitationDto>> GetPendingInvitationsByUserIdAsync(
		long userId,
		CancellationToken cancellationToken = default)
	{
		var connection = _dbContext.Database.GetDbConnection();
		await EnsureConnectionOpenAsync(connection, cancellationToken);

		await using var command = CreateCommand(connection, "dbo.uspGetPendingInvitationsByUserId", null);
		command.Parameters.Add(CreateParameter("@UserId", userId));

		var results = new List<UserInvitationDto>();

		await using var reader = await DbCommandHelper.ExecuteReaderAsync(command, cancellationToken);

		while (await reader.ReadAsync(cancellationToken))
		{
			results.Add(new UserInvitationDto
			{
				InvitationId    = GetInt64(reader, "InvitationId"),
				SessionId       = GetInt64(reader, "SessionId"),
				SlotIndex       = GetByte(reader, "SlotIndex"),
				SlotName        = GetString(reader, "SlotName"),
				Status          = GetString(reader, "Status"),
				SentAt          = GetDateTime(reader, "SentAt"),
				ExpiresAt       = GetNullableDateTime(reader, "ExpiresAt"),
				SessionName     = GetString(reader, "SessionName"),
				SessionMode     = GetString(reader, "SessionMode"),
				SessionDuration = GetInt32(reader, "SessionDuration"),
				ScheduledAt     = GetNullableDateTime(reader, "ScheduledAt"),
				HostName        = GetString(reader, "HostName"),
				HostAvatarUrl   = GetNullableString(reader, "HostAvatarUrl")
			});
		}

		return results;
	}

	public async Task<(long InvitationId, long SessionId, long UserId, byte SlotIndex, string SlotName, string Status)?> GetInvitationByIdAsync(
		long invitationId,
		CancellationToken cancellationToken = default)
	{
		var result = await _dbContext.SessionInvitations
			.AsNoTracking()
			.Where(inv => inv.InvitationId == invitationId && inv.IsDeleted == false)
			.Select(inv => new
			{
				inv.InvitationId,
				inv.SessionId,
				inv.UserId,
				inv.SlotIndex,
				inv.SlotName,
				inv.Status
			})
			.FirstOrDefaultAsync(cancellationToken);

		if (result is null)
		{
			return null;
		}

		return (result.InvitationId, result.SessionId, result.UserId, result.SlotIndex, result.SlotName, result.Status);
	}

	private DbCommand CreateCommand(DbConnection connection, string storedProcedureName, DbTransaction? transaction)
	{
		var command = connection.CreateCommand();
		command.CommandText = DbCommandHelper.QualifyRoutineName(_dbContext.DatabaseProvider, storedProcedureName);
		command.CommandType = CommandType.StoredProcedure;
		command.Transaction = transaction;
		return command;
	}

	private DbParameter CreateParameter(string parameterName, object? value)
	{
		return DbCommandHelper.CreateParameter(_dbContext.DatabaseProvider, parameterName, value);
	}

	private static async Task EnsureConnectionOpenAsync(DbConnection connection, CancellationToken cancellationToken)
	{
		if (connection.State != ConnectionState.Open)
		{
			await connection.OpenAsync(cancellationToken);
		}
	}

	private static long GetInt64(DbDataReader reader, string col)
	{
		var ord = reader.GetOrdinal(col);
		return reader.IsDBNull(ord) ? 0 : Convert.ToInt64(reader.GetValue(ord));
	}

	private static int GetInt32(DbDataReader reader, string col)
	{
		var ord = reader.GetOrdinal(col);
		return reader.IsDBNull(ord) ? 0 : Convert.ToInt32(reader.GetValue(ord));
	}

	private static byte GetByte(DbDataReader reader, string col)
	{
		var ord = reader.GetOrdinal(col);
		return reader.IsDBNull(ord) ? (byte)0 : Convert.ToByte(reader.GetValue(ord));
	}

	private static string GetString(DbDataReader reader, string col)
	{
		var ord = reader.GetOrdinal(col);
		return reader.IsDBNull(ord) ? string.Empty : reader.GetString(ord);
	}

	private static string? GetNullableString(DbDataReader reader, string col)
	{
		var ord = reader.GetOrdinal(col);
		return reader.IsDBNull(ord) ? null : reader.GetString(ord);
	}

	private static DateTime GetDateTime(DbDataReader reader, string col)
	{
		var ord = reader.GetOrdinal(col);
		return reader.IsDBNull(ord) ? DateTime.MinValue : Convert.ToDateTime(reader.GetValue(ord));
	}

	private static DateTime? GetNullableDateTime(DbDataReader reader, string col)
	{
		var ord = reader.GetOrdinal(col);
		return reader.IsDBNull(ord) ? null : Convert.ToDateTime(reader.GetValue(ord));
	}
}
