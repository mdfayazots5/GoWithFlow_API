using System.Data;
using System.Data.Common;
using GoWithFlow.Application.Common;
using GoWithFlow.Application.DTOs.Responses.Session;
using GoWithFlow.Application.Interfaces.Repositories;
using GoWithFlow.Domain.Entities;
using GoWithFlow.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GoWithFlow.Infrastructure.Repositories;

public sealed class SessionRepository : ISessionRepository
{
	private readonly GoWithFlowDbContext _dbContext;

	public SessionRepository(GoWithFlowDbContext dbContext)
	{
		_dbContext = dbContext;
	}

	public async Task<(long SessionId, string JoinCode)> CreateSessionAsync(Session session, SessionMember hostMember, CancellationToken cancellationToken = default)
	{
		var connection = _dbContext.Database.GetDbConnection();
		await EnsureConnectionOpenAsync(connection, cancellationToken);

		await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

		try
		{
			var (sessionId, joinCode) = await InsertSessionInternalAsync(session, transaction, cancellationToken);
			hostMember.SessionId = sessionId;

			await InsertSessionMemberInternalAsync(hostMember, transaction, cancellationToken);
			await transaction.CommitAsync(cancellationToken);

			return (sessionId, joinCode);
		}
		catch
		{
			await transaction.RollbackAsync(cancellationToken);
			throw;
		}
	}

	public async Task JoinSessionAsync(SessionMember sessionMember, CancellationToken cancellationToken = default)
	{
		var connection = _dbContext.Database.GetDbConnection();
		await EnsureConnectionOpenAsync(connection, cancellationToken);

		await using var command = CreateCommand(connection, "dbo.uspInsertSessionMember", null);
		command.Parameters.Add(CreateParameter("@SessionId", sessionMember.SessionId));
		command.Parameters.Add(CreateParameter("@UserId", sessionMember.UserId));
		command.Parameters.Add(CreateParameter("@SlotIndex", sessionMember.SlotIndex));
		command.Parameters.Add(CreateParameter("@SlotName", sessionMember.SlotName));
		command.Parameters.Add(CreateParameter("@IsHost", sessionMember.IsHost));
		command.Parameters.Add(CreateParameter("@CreatedBy", sessionMember.CreatedBy));
		command.Parameters.Add(CreateParameter("@IPAddress", sessionMember.IPAddress));

		await DbCommandHelper.ExecuteNonQueryAsync(command, cancellationToken);
	}

	public async Task<Session?> GetSessionBySessionIdAsync(long sessionId, CancellationToken cancellationToken = default)
	{
		return await _dbContext.Sessions
			.AsNoTracking()
			.FirstOrDefaultAsync(session => session.SessionId == sessionId && session.IsDeleted == false, cancellationToken);
	}

	public async Task<SessionPreviewResponseDto?> GetSessionPreviewByJoinCodeAsync(string joinCode, CancellationToken cancellationToken = default)
	{
		var connection = _dbContext.Database.GetDbConnection();
		await EnsureConnectionOpenAsync(connection, cancellationToken);

		await using var command = CreateCommand(connection, "dbo.uspGetSessionByJoinCode", null);
		command.Parameters.Add(CreateParameter("@JoinCode", joinCode));

		SessionPreviewResponseDto response;
		List<SlotInfoDto> slots;

		await using (var reader = await DbCommandHelper.ExecuteReaderAsync(command, cancellationToken))
		{
			if (await reader.ReadAsync(cancellationToken) == false)
			{
				return null;
			}

			response = new SessionPreviewResponseDto
			{
				SessionId = GetInt64(reader, "SessionId"),
				SessionName = GetString(reader, "SessionName"),
				SessionMode = GetString(reader, "SessionMode"),
				ScriptTitle = GetString(reader, "ScriptTitle"),
				ScriptGrammarTag = GetString(reader, "ScriptGrammarTag"),
				Duration = GetInt32(reader, "Duration"),
				MaxMembers = GetByte(reader, "MaxMembers"),
				CurrentMemberCount = GetInt32(reader, "CurrentMemberCount"),
				Status = GetString(reader, "Status")
			};

			slots = await ReadSlotInfoDtosAsync(reader, cancellationToken);
		}

		if (slots.Count == 0)
		{
			slots = await GetAvailableSlotsBySessionIdAsync(response.SessionId, cancellationToken);
		}

		response.Slots = slots;

		return response;
	}

	public async Task<LobbyStateResponseDto?> GetLobbyStateBySessionIdAsync(long sessionId, CancellationToken cancellationToken = default)
	{
		var connection = _dbContext.Database.GetDbConnection();
		await EnsureConnectionOpenAsync(connection, cancellationToken);

		string? resolvedStatus = null;
		await using var command = CreateCommand(connection, "dbo.uspGetSessionBySessionId", null);
		command.Parameters.Add(CreateParameter("@SessionId", sessionId));
		LobbyStateResponseDto? response;

		await using (var reader = await DbCommandHelper.ExecuteReaderAsync(command, cancellationToken))
		{
			if (await reader.ReadAsync(cancellationToken) == false)
			{
				return null;
			}

			response = new LobbyStateResponseDto
			{
				SessionId = GetInt64(reader, "SessionId"),
				SessionName = GetString(reader, "SessionName"),
				JoinCode = GetString(reader, "JoinCode"),
				SessionMode = GetString(reader, "SessionMode"),
				ScriptTitle = GetString(reader, "ScriptTitle"),
				MaxMembers = GetByte(reader, "MaxMembers"),
				SessionDuration = GetInt32(reader, "SessionDuration"),
				Status = string.Empty,
				CanStart = false
			};

			if (TryGetString(reader, "Status", out var status))
			{
				resolvedStatus = status;
			}

		}

		response.Members = await (
			from sessionMember in _dbContext.SessionMembers.AsNoTracking()
			join user in _dbContext.Users.AsNoTracking() on sessionMember.UserId equals user.UserId
			where sessionMember.SessionId == sessionId
				&& sessionMember.IsActive == true
				&& sessionMember.IsDeleted == false
				&& user.IsDeleted == false
			orderby sessionMember.SlotIndex, sessionMember.SessionMemberId
			select new LobbyMemberDto
			{
				UserId = user.UserId,
				FullName = user.FullName,
				AvatarUrl = user.AvatarUrl,
				SlotIndex = sessionMember.SlotIndex,
				SlotName = sessionMember.SlotName,
				IsReady = sessionMember.IsReady,
				IsHost = sessionMember.IsHost
			})
			.ToListAsync(cancellationToken);

		response.Status = await ResolveLobbyStatusAsync(resolvedStatus, sessionId, cancellationToken);

		return response;
	}

	public async Task<List<SlotInfoDto>> GetAvailableSlotsBySessionIdAsync(long sessionId, CancellationToken cancellationToken = default)
	{
		var connection = _dbContext.Database.GetDbConnection();
		await EnsureConnectionOpenAsync(connection, cancellationToken);

		await using var command = CreateCommand(connection, "dbo.uspGetAvailableSlotsBySessionId", null);
		command.Parameters.Add(CreateParameter("@SessionId", sessionId));

		await using var reader = await DbCommandHelper.ExecuteReaderAsync(command, cancellationToken);
		return await ReadSlotInfoDtosAsync(reader, cancellationToken, advanceToNextResult: false);
	}

	public async Task UpdateSessionMemberReadyStatusAsync(long sessionId, long userId, bool isReady, string updatedBy, string ipAddress, CancellationToken cancellationToken = default)
	{
		var connection = _dbContext.Database.GetDbConnection();
		await EnsureConnectionOpenAsync(connection, cancellationToken);

		await using var command = CreateCommand(connection, "dbo.uspUpdateSessionMemberReadyStatus", null);
		command.Parameters.Add(CreateParameter("@SessionId", sessionId));
		command.Parameters.Add(CreateParameter("@UserId", userId));
		command.Parameters.Add(CreateParameter("@IsReady", isReady));
		command.Parameters.Add(CreateParameter("@UpdatedBy", updatedBy));
		command.Parameters.Add(CreateParameter("@IPAddress", ipAddress));

		await DbCommandHelper.ExecuteNonQueryAsync(command, cancellationToken);
	}

	public async Task UpdateSessionStatusAsync(long sessionId, string status, string updatedBy, string ipAddress, CancellationToken cancellationToken = default)
	{
		var connection = _dbContext.Database.GetDbConnection();
		await EnsureConnectionOpenAsync(connection, cancellationToken);

		await using var command = CreateCommand(connection, "dbo.uspUpdateSessionStatus", null);
		command.Parameters.Add(CreateParameter("@SessionId", sessionId));
		command.Parameters.Add(CreateParameter("@Status", status));
		command.Parameters.Add(CreateParameter("@UpdatedBy", updatedBy));
		command.Parameters.Add(CreateParameter("@IPAddress", ipAddress));

		await DbCommandHelper.ExecuteNonQueryAsync(command, cancellationToken);
	}

	public async Task<PagedResult<SessionListItemResponseDto>> GetSessionHistoryAsync(long userId, string? statusFilter, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
	{
		var connection = _dbContext.Database.GetDbConnection();
		await EnsureConnectionOpenAsync(connection, cancellationToken);

		await using var command = CreateCommand(connection, "dbo.uspGetSessionListByUserId", null);
		command.Parameters.Add(CreateParameter("@UserId", userId));
		command.Parameters.Add(CreateParameter("@StatusFilter", statusFilter));
		command.Parameters.Add(CreateParameter("@PageNumber", pageNumber));
		command.Parameters.Add(CreateParameter("@PageSize", pageSize));

		await using var reader = await DbCommandHelper.ExecuteReaderAsync(command, cancellationToken);
		var items = new List<SessionListItemResponseDto>();

		while (await reader.ReadAsync(cancellationToken))
		{
			items.Add(new SessionListItemResponseDto
			{
				SessionId = GetInt64(reader, "SessionId"),
				SessionName = GetString(reader, "SessionName"),
				SessionMode = GetString(reader, "SessionMode"),
				SessionDate = GetDateTime(reader, "SessionDate"),
				Duration = GetInt32(reader, "Duration"),
				FluencyScore = GetNullableDecimal(reader, "FluencyScore"),
				MistakeCount = GetInt32(reader, "MistakeCount"),
				Status = GetString(reader, "Status"),
				ScriptTitle = GetString(reader, "ScriptTitle")
			});
		}

		await reader.CloseAsync();

		var totalCount = await _dbContext.SessionMembers
			.AsNoTracking()
			.Where(sessionMember => sessionMember.UserId == userId && sessionMember.IsDeleted == false)
			.Where(sessionMember => string.IsNullOrWhiteSpace(statusFilter) || sessionMember.Session!.Status == statusFilter)
			.CountAsync(cancellationToken);

		return new PagedResult<SessionListItemResponseDto>
		{
			Items = items,
			TotalCount = totalCount,
			PageNumber = pageNumber,
			PageSize = pageSize
		};
	}

	public async Task<(bool IsValid, long SessionId, string SessionName, string Status, int CurrentMemberCount)?> ValidateJoinCodeAsync(string joinCode, CancellationToken cancellationToken = default)
	{
		var connection = _dbContext.Database.GetDbConnection();
		await EnsureConnectionOpenAsync(connection, cancellationToken);

		await using var command = CreateCommand(connection, "dbo.uspValidateJoinCode", null);
		command.Parameters.Add(CreateParameter("@JoinCode", joinCode));
		var isValidParameter = CreateOutputParameter("@IsValid", DbType.Boolean);
		var sessionIdParameter = CreateOutputParameter("@SessionId", DbType.Int64);
		var sessionNameParameter = CreateOutputParameter("@SessionName", DbType.String, 128);
		var statusParameter = CreateOutputParameter("@Status", DbType.String, 16);
		var currentMemberCountParameter = CreateOutputParameter("@CurrentMemberCount", DbType.Int32);
		command.Parameters.Add(isValidParameter);
		command.Parameters.Add(sessionIdParameter);
		command.Parameters.Add(sessionNameParameter);
		command.Parameters.Add(statusParameter);
		command.Parameters.Add(currentMemberCountParameter);

		await DbCommandHelper.ExecuteNonQueryAsync(command, cancellationToken);

		return (
			isValidParameter.Value is bool isValid && isValid,
			sessionIdParameter.Value == DBNull.Value ? 0 : Convert.ToInt64(sessionIdParameter.Value),
			sessionNameParameter.Value == DBNull.Value ? string.Empty : sessionNameParameter.Value?.ToString() ?? string.Empty,
			statusParameter.Value == DBNull.Value ? string.Empty : statusParameter.Value?.ToString() ?? string.Empty,
			currentMemberCountParameter.Value == DBNull.Value ? 0 : Convert.ToInt32(currentMemberCountParameter.Value));
	}

	public async Task UpdateSessionMemberLeftAsync(long sessionId, long userId, string updatedBy, string ipAddress, CancellationToken cancellationToken = default)
	{
		var connection = _dbContext.Database.GetDbConnection();
		await EnsureConnectionOpenAsync(connection, cancellationToken);

		await using var command = CreateCommand(connection, "dbo.uspUpdateSessionMemberLeft", null);
		command.Parameters.Add(CreateParameter("@SessionId", sessionId));
		command.Parameters.Add(CreateParameter("@UserId", userId));
		command.Parameters.Add(CreateParameter("@UpdatedBy", updatedBy));
		command.Parameters.Add(CreateParameter("@IPAddress", ipAddress));

		await DbCommandHelper.ExecuteNonQueryAsync(command, cancellationToken);
	}

	public async Task<(bool Exists, string Status, bool IsExpired, int CurrentMemberCount, int MaxMembers)?> CheckJoinCodeStatusAsync(string joinCode, CancellationToken cancellationToken = default)
	{
		var now = DateTime.UtcNow;

		var result = await _dbContext.Sessions
			.AsNoTracking()
			.Where(session => session.JoinCode == joinCode && session.IsDeleted == false)
			.Select(session => new
			{
				session.Status,
				IsExpired = session.RoomExpiresAt != null && session.RoomExpiresAt <= now,
				MaxMembers = (int)session.MaxMembers,
				CurrentMemberCount = _dbContext.SessionMembers.Count(sessionMember =>
					sessionMember.SessionId == session.SessionId &&
					sessionMember.IsDeleted == false &&
					sessionMember.IsActive)
			})
			.FirstOrDefaultAsync(cancellationToken);

		return result is null
			? null
			: (true, result.Status, result.IsExpired, result.CurrentMemberCount, result.MaxMembers);
	}

	private async Task<(long SessionId, string JoinCode)> InsertSessionInternalAsync(Session session, DbTransaction transaction, CancellationToken cancellationToken)
	{
		await using var command = CreateCommand(transaction.Connection!, "dbo.uspInsertSession", transaction);
		command.Parameters.Add(CreateParameter("@SessionName", session.SessionName));
		command.Parameters.Add(CreateParameter("@SessionMode", session.SessionMode));
		command.Parameters.Add(CreateParameter("@MaxMembers", session.MaxMembers));
		command.Parameters.Add(CreateParameter("@SessionDuration", session.SessionDuration));
		command.Parameters.Add(CreateParameter("@HostUserId", session.HostUserId));
		command.Parameters.Add(CreateParameter("@ScriptId", session.ScriptId));
		command.Parameters.Add(CreateParameter("@RoomExpiryMinutes", session.RoomExpiryMinutes));
		command.Parameters.Add(CreateParameter("@CreatedBy", session.CreatedBy));
		command.Parameters.Add(CreateParameter("@IPAddress", session.IPAddress));
		var sessionIdParameter = CreateOutputParameter("@SessionId", DbType.Int64);
		var joinCodeParameter = CreateOutputParameter("@JoinCode", DbType.String, 8);
		command.Parameters.Add(sessionIdParameter);
		command.Parameters.Add(joinCodeParameter);

		await DbCommandHelper.ExecuteNonQueryAsync(command, cancellationToken);

		var sessionId = sessionIdParameter.Value == DBNull.Value
			? 0
			: Convert.ToInt64(sessionIdParameter.Value);
		var joinCode = joinCodeParameter.Value == DBNull.Value
			? string.Empty
			: joinCodeParameter.Value?.ToString() ?? string.Empty;

		if (sessionId <= 0 || string.IsNullOrWhiteSpace(joinCode))
		{
			throw new InvalidOperationException("Session insert did not return the expected output parameters.");
		}

		return (sessionId, joinCode);
	}

	private async Task<string> ResolveLobbyStatusAsync(string? status, long sessionId, CancellationToken cancellationToken)
	{
		if (string.IsNullOrWhiteSpace(status) == false)
		{
			return status;
		}

		return await _dbContext.Sessions
			.AsNoTracking()
			.Where(session => session.SessionId == sessionId && session.IsDeleted == false)
			.Select(session => session.Status)
			.FirstOrDefaultAsync(cancellationToken)
			?? "LOBBY";
	}

	private async Task<List<SlotInfoDto>> ReadSlotInfoDtosAsync(
		DbDataReader reader,
		CancellationToken cancellationToken,
		bool advanceToNextResult = true)
	{
		if (advanceToNextResult && await reader.NextResultAsync(cancellationToken) == false)
		{
			return [];
		}

		var slots = new List<SlotInfoDto>();

		while (await reader.ReadAsync(cancellationToken))
		{
			slots.Add(new SlotInfoDto
			{
				SlotIndex = GetByte(reader, "SlotIndex"),
				SlotName = GetString(reader, "SlotName"),
				IsOccupied = GetBoolean(reader, "IsOccupied"),
				UserFullName = GetNullableString(reader, "UserFullName"),
				IsReady = GetBoolean(reader, "IsReady")
			});
		}

		return slots;
	}

	private async Task InsertSessionMemberInternalAsync(SessionMember sessionMember, DbTransaction transaction, CancellationToken cancellationToken)
	{
		await using var command = CreateCommand(transaction.Connection!, "dbo.uspInsertSessionMember", transaction);
		command.Parameters.Add(CreateParameter("@SessionId", sessionMember.SessionId));
		command.Parameters.Add(CreateParameter("@UserId", sessionMember.UserId));
		command.Parameters.Add(CreateParameter("@SlotIndex", sessionMember.SlotIndex));
		command.Parameters.Add(CreateParameter("@SlotName", sessionMember.SlotName));
		command.Parameters.Add(CreateParameter("@IsHost", sessionMember.IsHost));
		command.Parameters.Add(CreateParameter("@CreatedBy", sessionMember.CreatedBy));
		command.Parameters.Add(CreateParameter("@IPAddress", sessionMember.IPAddress));

		await DbCommandHelper.ExecuteNonQueryAsync(command, cancellationToken);
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

	private DbParameter CreateOutputParameter(string parameterName, DbType dbType, int size = 0)
	{
		return DbCommandHelper.CreateParameter(
			_dbContext.DatabaseProvider,
			parameterName,
			null,
			dbType,
			ParameterDirection.Output,
			size);
	}

	private static async Task EnsureConnectionOpenAsync(DbConnection connection, CancellationToken cancellationToken)
	{
		if (connection.State != ConnectionState.Open)
		{
			await connection.OpenAsync(cancellationToken);
		}
	}

	private static string GetString(DbDataReader reader, string columnName)
	{
		var ordinal = reader.GetOrdinal(columnName);
		return reader.IsDBNull(ordinal) ? string.Empty : reader.GetString(ordinal);
	}

	private static bool TryGetString(DbDataReader reader, string columnName, out string value)
	{
		value = string.Empty;

		if (TryGetOrdinal(reader, columnName, out var ordinal) == false || reader.IsDBNull(ordinal))
		{
			return false;
		}

		value = Convert.ToString(reader.GetValue(ordinal)) ?? string.Empty;
		return true;
	}

	private static string? GetNullableString(DbDataReader reader, string columnName)
	{
		var ordinal = reader.GetOrdinal(columnName);
		return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
	}

	private static long GetInt64(DbDataReader reader, string columnName)
	{
		var ordinal = reader.GetOrdinal(columnName);
		return reader.IsDBNull(ordinal) ? 0 : Convert.ToInt64(reader.GetValue(ordinal));
	}

	private static int GetInt32(DbDataReader reader, string columnName)
	{
		var ordinal = reader.GetOrdinal(columnName);
		return reader.IsDBNull(ordinal) ? 0 : Convert.ToInt32(reader.GetValue(ordinal));
	}

	private static byte GetByte(DbDataReader reader, string columnName)
	{
		var ordinal = reader.GetOrdinal(columnName);
		return reader.IsDBNull(ordinal) ? (byte)0 : Convert.ToByte(reader.GetValue(ordinal));
	}

	private static bool GetBoolean(DbDataReader reader, string columnName)
	{
		var ordinal = reader.GetOrdinal(columnName);
		return reader.IsDBNull(ordinal) == false && Convert.ToBoolean(reader.GetValue(ordinal));
	}

	private static DateTime GetDateTime(DbDataReader reader, string columnName)
	{
		var ordinal = reader.GetOrdinal(columnName);
		return reader.IsDBNull(ordinal) ? DateTime.MinValue : Convert.ToDateTime(reader.GetValue(ordinal));
	}

	private static decimal? GetNullableDecimal(DbDataReader reader, string columnName)
	{
		var ordinal = reader.GetOrdinal(columnName);
		return reader.IsDBNull(ordinal) ? null : Convert.ToDecimal(reader.GetValue(ordinal));
	}

	private static bool TryGetOrdinal(DbDataReader reader, string columnName, out int ordinal)
	{
		for (var index = 0; index < reader.FieldCount; index++)
		{
			if (string.Equals(reader.GetName(index), columnName, StringComparison.OrdinalIgnoreCase))
			{
				ordinal = index;
				return true;
			}
		}

		ordinal = -1;
		return false;
	}
}
