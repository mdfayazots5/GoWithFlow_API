using System.Data;
using System.Data.Common;
using GoWithFlow.Application.Common;
using GoWithFlow.Application.DTOs.Responses.User;
using GoWithFlow.Application.Interfaces.Repositories;
using GoWithFlow.Domain.Entities;
using GoWithFlow.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GoWithFlow.Infrastructure.Repositories;

public sealed class RepracticeRepository : IRepracticeRepository
{
	private readonly GoWithFlowDbContext _dbContext;

	public RepracticeRepository(GoWithFlowDbContext dbContext)
	{
		_dbContext = dbContext;
	}

	public async Task<long> InsertRepracticeSessionAsync(RepracticeSession repracticeSession, CancellationToken cancellationToken = default)
	{
		await using var command = await CreateStoredProcedureCommandAsync("dbo.uspInsertRepracticeSession", cancellationToken);
		command.Parameters.Add(CreateParameter("@UserId", repracticeSession.UserId));
		command.Parameters.Add(CreateParameter("@SourceSessionId", repracticeSession.SourceSessionId));
		command.Parameters.Add(CreateParameter("@TotalMistakes", repracticeSession.TotalMistakes));
		command.Parameters.Add(CreateParameter("@CreatedBy", repracticeSession.CreatedBy));
		command.Parameters.Add(CreateParameter("@IPAddress", repracticeSession.IPAddress));

		var result = await DbCommandHelper.ExecuteScalarAsync(command, cancellationToken);
		return Convert.ToInt64(result);
	}

	public async Task<long> InsertRepracticeUtteranceAsync(RepracticeUtterance repracticeUtterance, CancellationToken cancellationToken = default)
	{
		await using var command = await CreateStoredProcedureCommandAsync("dbo.uspInsertRepracticeUtterance", cancellationToken);
		command.Parameters.Add(CreateParameter("@RepracticeSessionId", repracticeUtterance.RepracticeSessionId));
		command.Parameters.Add(CreateParameter("@MistakeId", repracticeUtterance.MistakeId));
		command.Parameters.Add(CreateParameter("@OriginalUtteranceId", repracticeUtterance.OriginalUtteranceId));
		command.Parameters.Add(CreateParameter("@EnglishText", repracticeUtterance.EnglishText));
		command.Parameters.Add(CreateParameter("@HintText", repracticeUtterance.HintText));
		command.Parameters.Add(CreateParameter("@MistakeType", repracticeUtterance.MistakeType));
		command.Parameters.Add(CreateParameter("@MistakeDetail", repracticeUtterance.MistakeDetail));
		command.Parameters.Add(CreateParameter("@CorrectionNote", repracticeUtterance.CorrectionNote));
		command.Parameters.Add(CreateParameter("@CreatedBy", repracticeUtterance.CreatedBy));
		command.Parameters.Add(CreateParameter("@IPAddress", repracticeUtterance.IPAddress));

		var result = await DbCommandHelper.ExecuteScalarAsync(command, cancellationToken);
		return Convert.ToInt64(result);
	}

	public async Task<RepracticeSessionResponseDto?> GetRepracticeSessionByIdAsync(long repracticeSessionId, CancellationToken cancellationToken = default)
	{
		await using var command = await CreateStoredProcedureCommandAsync("dbo.uspGetRepracticeSessionByRepracticeSessionId", cancellationToken);
		command.Parameters.Add(CreateParameter("@RepracticeSessionId", repracticeSessionId));

		await using var reader = await DbCommandHelper.ExecuteReaderAsync(command, cancellationToken);

		if (await reader.ReadAsync(cancellationToken) == false)
		{
			return null;
		}

		var response = new RepracticeSessionResponseDto
		{
			RepracticeSessionId = GetInt64(reader, "RepracticeSessionId"),
			SourceSessionId = GetInt64(reader, "SourceSessionId"),
			Status = GetString(reader, "Status"),
			TotalMistakes = GetInt32(reader, "TotalMistakes"),
			CompletedRounds = GetInt32(reader, "CompletedRounds"),
			ImprovementPercent = GetDecimal(reader, "ImprovementPercent"),
			GeneratedDate = GetDateTime(reader, "GeneratedDate")
		};

		await reader.CloseAsync();

		response.Utterances = await _dbContext.RepracticeUtterances
			.AsNoTracking()
			.Where(utterance => utterance.RepracticeSessionId == repracticeSessionId && utterance.IsDeleted == false)
			.OrderBy(utterance => utterance.SortOrder)
			.ThenBy(utterance => utterance.RepracticeUtteranceId)
			.Select(utterance => new RepracticeUtteranceResponseDto
			{
				RepracticeUtteranceId = utterance.RepracticeUtteranceId,
				MistakeId = utterance.MistakeId,
				OriginalUtteranceId = utterance.OriginalUtteranceId,
				EnglishText = utterance.EnglishText,
				HintText = utterance.HintText,
				MistakeType = utterance.MistakeType,
				MistakeDetail = utterance.MistakeDetail,
				CorrectionNote = utterance.CorrectionNote,
				AttemptCount = utterance.AttemptCount,
				BestScore = utterance.BestScore,
				LastScore = utterance.LastScore,
				IsResolved = utterance.IsResolved
			})
			.ToListAsync(cancellationToken);

		return response;
	}

	public async Task<PagedResult<RepracticeSessionResponseDto>> GetRepracticeHistoryAsync(long userId, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
	{
		await using var command = await CreateStoredProcedureCommandAsync("dbo.uspGetRepracticeSessionListByUserId", cancellationToken);
		command.Parameters.Add(CreateParameter("@UserId", userId));
		command.Parameters.Add(CreateParameter("@Status", DBNull.Value));
		command.Parameters.Add(CreateParameter("@PageNumber", pageNumber));
		command.Parameters.Add(CreateParameter("@PageSize", pageSize));

		await using var reader = await DbCommandHelper.ExecuteReaderAsync(command, cancellationToken);
		var items = new List<RepracticeSessionResponseDto>();

		while (await reader.ReadAsync(cancellationToken))
		{
			items.Add(new RepracticeSessionResponseDto
			{
				RepracticeSessionId = GetInt64(reader, "RepracticeSessionId"),
				SourceSessionId = GetInt64(reader, "SourceSessionId"),
				Status = GetString(reader, "Status"),
				TotalMistakes = GetInt32(reader, "TotalMistakes"),
				CompletedRounds = GetInt32(reader, "CompletedRounds"),
				ImprovementPercent = GetDecimal(reader, "ImprovementPercent"),
				GeneratedDate = GetDateTime(reader, "GeneratedDate")
			});
		}

		await reader.CloseAsync();

		var totalCount = await CountRepracticeHistoryAsync(userId, cancellationToken);

		return new PagedResult<RepracticeSessionResponseDto>
		{
			Items = items,
			TotalCount = totalCount,
			PageNumber = pageNumber,
			PageSize = pageSize
		};
	}

	public async Task<RepracticeSession?> GetRepracticeSessionEntityAsync(long repracticeSessionId, CancellationToken cancellationToken = default)
	{
		return await _dbContext.RepracticeSessions
			.AsNoTracking()
			.FirstOrDefaultAsync(repracticeSession =>
				repracticeSession.RepracticeSessionId == repracticeSessionId &&
				repracticeSession.IsDeleted == false,
				cancellationToken);
	}

	public async Task<RepracticeUtterance?> GetRepracticeUtteranceEntityAsync(long repracticeUtteranceId, CancellationToken cancellationToken = default)
	{
		return await _dbContext.RepracticeUtterances
			.AsNoTracking()
			.Include(repracticeUtterance => repracticeUtterance.RepracticeSession)
			.FirstOrDefaultAsync(repracticeUtterance =>
				repracticeUtterance.RepracticeUtteranceId == repracticeUtteranceId &&
				repracticeUtterance.IsDeleted == false,
				cancellationToken);
	}

	public async Task UpdateRepracticeUtteranceAttemptAsync(long repracticeUtteranceId, decimal score, string updatedBy, string ipAddress, CancellationToken cancellationToken = default)
	{
		await using var command = await CreateStoredProcedureCommandAsync("dbo.uspUpdateRepracticeUtteranceAttempt", cancellationToken);
		command.Parameters.Add(CreateParameter("@RepracticeUtteranceId", repracticeUtteranceId));
		command.Parameters.Add(CreateParameter("@Score", score));
		command.Parameters.Add(CreateParameter("@UpdatedBy", updatedBy));
		command.Parameters.Add(CreateParameter("@IPAddress", ipAddress));

		await DbCommandHelper.ExecuteNonQueryAsync(command, cancellationToken);
	}

	public async Task<decimal> CalculateImprovementPercentageAsync(long userId, CancellationToken cancellationToken = default)
	{
		await using var command = await CreateStoredProcedureCommandAsync("dbo.uspCalculateImprovementPercentByUserId", cancellationToken);
		command.Parameters.Add(CreateParameter("@UserId", userId));

		var result = await DbCommandHelper.ExecuteScalarAsync(command, cancellationToken);

		if (result is null || result == DBNull.Value)
		{
			return 0m;
		}

		return Convert.ToDecimal(result);
	}

	public async Task UpdateRepracticeSessionStatusAsync(long repracticeSessionId, string status, decimal improvementPercent, string updatedBy, string ipAddress, CancellationToken cancellationToken = default)
	{
		await using var command = await CreateStoredProcedureCommandAsync("dbo.uspUpdateRepracticeSessionStatus", cancellationToken);
		command.Parameters.Add(CreateParameter("@RepracticeSessionId", repracticeSessionId));
		command.Parameters.Add(CreateParameter("@Status", status));
		command.Parameters.Add(CreateParameter("@ImprovementPercent", improvementPercent));
		command.Parameters.Add(CreateParameter("@UpdatedBy", updatedBy));
		command.Parameters.Add(CreateParameter("@IPAddress", ipAddress));

		await DbCommandHelper.ExecuteNonQueryAsync(command, cancellationToken);
	}

	private async Task<DbCommand> CreateStoredProcedureCommandAsync(string storedProcedureName, CancellationToken cancellationToken)
	{
		var connection = _dbContext.Database.GetDbConnection();
		await EnsureConnectionOpenAsync(connection, cancellationToken);

		var command = connection.CreateCommand();
		command.CommandText = DbCommandHelper.QualifyRoutineName(_dbContext.DatabaseProvider, storedProcedureName);
		command.CommandType = CommandType.StoredProcedure;
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

	private async Task<int> CountRepracticeHistoryAsync(long userId, CancellationToken cancellationToken)
	{
		return await _dbContext.RepracticeSessions
			.AsNoTracking()
			.CountAsync(session => session.UserId == userId && session.IsDeleted == false, cancellationToken);
	}

	private static string GetString(DbDataReader reader, string columnName)
	{
		var ordinal = reader.GetOrdinal(columnName);
		return reader.IsDBNull(ordinal) ? string.Empty : reader.GetString(ordinal);
	}

	private static string? GetNullableString(DbDataReader reader, string columnName)
	{
		var ordinal = reader.GetOrdinal(columnName);
		return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
	}

	private static long GetInt64(DbDataReader reader, string columnName)
	{
		return reader.GetInt64(reader.GetOrdinal(columnName));
	}

	private static int GetInt32(DbDataReader reader, string columnName)
	{
		return reader.GetInt32(reader.GetOrdinal(columnName));
	}

	private static bool GetBoolean(DbDataReader reader, string columnName)
	{
		return reader.GetBoolean(reader.GetOrdinal(columnName));
	}

	private static decimal GetDecimal(DbDataReader reader, string columnName)
	{
		return reader.GetDecimal(reader.GetOrdinal(columnName));
	}

	private static DateTime GetDateTime(DbDataReader reader, string columnName)
	{
		return reader.GetDateTime(reader.GetOrdinal(columnName));
	}
}
