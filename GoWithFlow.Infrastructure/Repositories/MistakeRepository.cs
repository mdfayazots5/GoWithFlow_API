using System.Data;
using System.Data.Common;
using GoWithFlow.Application.Common;
using GoWithFlow.Application.DTOs.Requests.User;
using GoWithFlow.Application.DTOs.Responses.User;
using GoWithFlow.Application.Interfaces.Repositories;
using GoWithFlow.Domain.Entities;
using GoWithFlow.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GoWithFlow.Infrastructure.Repositories;

public sealed class MistakeRepository : IMistakeRepository
{
	private readonly GoWithFlowDbContext _dbContext;

	public MistakeRepository(GoWithFlowDbContext dbContext)
	{
		_dbContext = dbContext;
	}

	public async Task<List<VoiceAnalysis>> GetVoiceAnalysesBySessionAndUserIdAsync(long sessionId, long userId, CancellationToken cancellationToken = default)
	{
		return await _dbContext.VoiceAnalyses
			.AsNoTracking()
			.Include(voiceAnalysis => voiceAnalysis.User)
			.Include(voiceAnalysis => voiceAnalysis.Session)
			.Include(voiceAnalysis => voiceAnalysis.Utterance)
			.Where(voiceAnalysis => voiceAnalysis.SessionId == sessionId &&
				voiceAnalysis.UserId == userId &&
				voiceAnalysis.IsDeleted == false)
			.OrderBy(voiceAnalysis => voiceAnalysis.TurnIndex)
			.ThenBy(voiceAnalysis => voiceAnalysis.VoiceAnalysisId)
			.ToListAsync(cancellationToken);
	}

	public async Task<bool> MistakeExistsAsync(long userId, long sessionId, long utteranceId, string mistakeType, string? mistakeDetail, CancellationToken cancellationToken = default)
	{
		return await _dbContext.Mistakes
			.AsNoTracking()
			.AnyAsync(mistake =>
				mistake.UserId == userId &&
				mistake.SessionId == sessionId &&
				mistake.UtteranceId == utteranceId &&
				mistake.MistakeType == mistakeType &&
				mistake.MistakeDetail == mistakeDetail &&
				mistake.IsDeleted == false,
				cancellationToken);
	}

	public async Task<long> InsertMistakeAsync(Mistake mistake, CancellationToken cancellationToken = default)
	{
		await using var command = await CreateStoredProcedureCommandAsync("dbo.uspInsertMistake", cancellationToken);
		command.Parameters.Add(CreateParameter("@UserId", mistake.UserId));
		command.Parameters.Add(CreateParameter("@SessionId", mistake.SessionId));
		command.Parameters.Add(CreateParameter("@UtteranceId", mistake.UtteranceId));
		command.Parameters.Add(CreateParameter("@ScriptId", mistake.ScriptId));
		command.Parameters.Add(CreateParameter("@UtteranceText", mistake.UtteranceText));
		command.Parameters.Add(CreateParameter("@SpokenText", mistake.SpokenText));
		command.Parameters.Add(CreateParameter("@MistakeType", mistake.MistakeType));
		command.Parameters.Add(CreateParameter("@MistakeDetail", mistake.MistakeDetail));
		command.Parameters.Add(CreateParameter("@GrammarTag", mistake.GrammarTag));
		command.Parameters.Add(CreateParameter("@ContextTag", mistake.ContextTag));
		command.Parameters.Add(CreateParameter("@CorrectionText", mistake.CorrectionText));
		command.Parameters.Add(CreateParameter("@CreatedBy", mistake.CreatedBy));
		command.Parameters.Add(CreateParameter("@IPAddress", mistake.IPAddress));

		var result = await DbCommandHelper.ExecuteScalarAsync(command, cancellationToken);
		return Convert.ToInt64(result);
	}

	public async Task<PagedResult<MistakeResponseDto>> GetMistakesAsync(MistakeFilterRequestDto dto, long userId, CancellationToken cancellationToken = default)
	{
		await using var command = await CreateStoredProcedureCommandAsync("dbo.uspGetMistakeByUserIdWithFilter", cancellationToken);
		command.Parameters.Add(CreateParameter("@UserId", userId));
		command.Parameters.Add(CreateParameter("@MistakeType", dto.MistakeType));
		command.Parameters.Add(CreateParameter("@IsResolved", dto.IsResolved));
		command.Parameters.Add(CreateParameter("@PageNumber", dto.PageNumber));
		command.Parameters.Add(CreateParameter("@PageSize", dto.PageSize));

		await using var reader = await DbCommandHelper.ExecuteReaderAsync(command, cancellationToken);
		var items = new List<MistakeResponseDto>();

		while (await reader.ReadAsync(cancellationToken))
		{
			items.Add(new MistakeResponseDto
			{
				MistakeId = GetInt64(reader, "MistakeId"),
				UserId = GetInt64(reader, "UserId"),
				SessionId = GetInt64(reader, "SessionId"),
				UtteranceId = GetInt64(reader, "UtteranceId"),
				ScriptId = GetInt64(reader, "ScriptId"),
				UtteranceText = GetString(reader, "UtteranceText"),
				SpokenText = GetNullableString(reader, "SpokenText"),
				MistakeType = GetString(reader, "MistakeType"),
				MistakeDetail = GetNullableString(reader, "MistakeDetail"),
				GrammarTag = GetNullableString(reader, "GrammarTag"),
				ContextTag = GetNullableString(reader, "ContextTag"),
				CorrectionText = GetNullableString(reader, "CorrectionText"),
				PracticeCount = GetInt32(reader, "PracticeCount"),
				IsResolved = GetBoolean(reader, "IsResolved"),
				FirstOccurrence = GetDateTime(reader, "FirstOccurrence"),
				LastAttempt = GetNullableDateTime(reader, "LastAttempt"),
				SessionName = GetString(reader, "SessionName"),
				ScriptTitle = GetString(reader, "ScriptTitle")
			});
		}

		await reader.CloseAsync();

		var totalCount = await CountMistakesAsync(dto, userId, cancellationToken);

		return new PagedResult<MistakeResponseDto>
		{
			Items = items,
			TotalCount = totalCount,
			PageNumber = dto.PageNumber,
			PageSize = dto.PageSize
		};
	}

	public async Task<MistakeSummaryResponseDto> GetMistakeSummaryAsync(long userId, CancellationToken cancellationToken = default)
	{
		await using var command = await CreateStoredProcedureCommandAsync("dbo.uspGetMistakeSummaryByUserId", cancellationToken);
		command.Parameters.Add(CreateParameter("@UserId", userId));

		await using var reader = await DbCommandHelper.ExecuteReaderAsync(command, cancellationToken);

		if (await reader.ReadAsync(cancellationToken) == false)
		{
			return new MistakeSummaryResponseDto();
		}

		return new MistakeSummaryResponseDto
		{
			TotalMistakes = GetInt32(reader, "TotalMistakes"),
			ResolvedMistakes = GetInt32(reader, "ResolvedMistakes"),
			PendingMistakes = GetInt32(reader, "PendingMistakes"),
			ImprovementPercent = GetDecimal(reader, "ImprovementPercent")
		};
	}

	public async Task<List<GrammarProgressResponseDto>> GetGrammarProgressAsync(long userId, CancellationToken cancellationToken = default)
	{
		await using var command = await CreateStoredProcedureCommandAsync("dbo.uspGetGrammarProgressByUserId", cancellationToken);
		command.Parameters.Add(CreateParameter("@UserId", userId));

		await using var reader = await DbCommandHelper.ExecuteReaderAsync(command, cancellationToken);
		var items = new List<GrammarProgressResponseDto>();

		while (await reader.ReadAsync(cancellationToken))
		{
			items.Add(new GrammarProgressResponseDto
			{
				GrammarTag = GetString(reader, "GrammarTag"),
				TotalMistakes = GetInt32(reader, "TotalMistakes"),
				ResolvedMistakes = GetInt32(reader, "ResolvedMistakes"),
				ImprovementPercent = GetDecimal(reader, "ImprovementPercent"),
				ProgressBarValue = GetInt32(reader, "ProgressBarValue")
			});
		}

		return items;
	}

	public async Task<List<Mistake>> GetUnresolvedMistakesAsync(long userId, long sourceSessionId, CancellationToken cancellationToken = default)
	{
		await using var command = await CreateStoredProcedureCommandAsync("dbo.uspGetUnresolvedMistakeByUserId", cancellationToken);
		command.Parameters.Add(CreateParameter("@UserId", userId));
		command.Parameters.Add(CreateParameter("@SourceSessionId", sourceSessionId));

		await using var reader = await DbCommandHelper.ExecuteReaderAsync(command, cancellationToken);
		var items = new List<Mistake>();

		while (await reader.ReadAsync(cancellationToken))
		{
			items.Add(new Mistake
			{
				MistakeId = GetInt64(reader, "MistakeId"),
				UserId = GetInt64(reader, "UserId"),
				SessionId = GetInt64(reader, "SessionId"),
				UtteranceId = GetInt64(reader, "UtteranceId"),
				ScriptId = GetInt64(reader, "ScriptId"),
				UtteranceText = GetString(reader, "UtteranceText"),
				SpokenText = GetNullableString(reader, "SpokenText"),
				MistakeType = GetString(reader, "MistakeType"),
				MistakeDetail = GetNullableString(reader, "MistakeDetail"),
				GrammarTag = GetNullableString(reader, "GrammarTag"),
				ContextTag = GetNullableString(reader, "ContextTag"),
				CorrectionText = GetNullableString(reader, "CorrectionText"),
				PracticeCount = GetInt32(reader, "PracticeCount"),
				IsResolved = GetBoolean(reader, "IsResolved"),
				FirstOccurrence = GetDateTime(reader, "FirstOccurrence"),
				LastAttempt = GetNullableDateTime(reader, "LastAttempt")
			});
		}

		return items;
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

	private async Task<int> CountMistakesAsync(MistakeFilterRequestDto dto, long userId, CancellationToken cancellationToken)
	{
		var query = _dbContext.Mistakes
			.AsNoTracking()
			.Where(mistake => mistake.UserId == userId && mistake.IsDeleted == false);

		if (string.IsNullOrWhiteSpace(dto.MistakeType) == false)
		{
			query = query.Where(mistake => mistake.MistakeType == dto.MistakeType);
		}

		if (dto.IsResolved.HasValue)
		{
			query = query.Where(mistake => mistake.IsResolved == dto.IsResolved.Value);
		}

		return await query.CountAsync(cancellationToken);
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

	private static DateTime? GetNullableDateTime(DbDataReader reader, string columnName)
	{
		var ordinal = reader.GetOrdinal(columnName);
		return reader.IsDBNull(ordinal) ? null : reader.GetDateTime(ordinal);
	}
}
