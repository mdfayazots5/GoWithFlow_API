using System.Data;
using System.Data.Common;
using System.Text.Json;
using GoWithFlow.Application.DTOs.Requests.LiveSession;
using GoWithFlow.Application.DTOs.Responses.LiveSession;
using GoWithFlow.Application.DTOs.Responses.Script;
using GoWithFlow.Application.Interfaces.Repositories;
using GoWithFlow.Domain.Entities;
using GoWithFlow.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GoWithFlow.Infrastructure.Repositories;

public sealed class LiveSessionRepository : ILiveSessionRepository
{
	private readonly GoWithFlowDbContext _dbContext;

	public LiveSessionRepository(GoWithFlowDbContext dbContext)
	{
		_dbContext = dbContext;
	}

	public async Task<long> InsertTurnStateAsync(TurnState turnState, CancellationToken cancellationToken = default)
	{
		var connection = _dbContext.Database.GetDbConnection();
		await EnsureConnectionOpenAsync(connection, cancellationToken);

		await using var command = CreateCommand(connection, "dbo.uspInsertTurnState");
		command.Parameters.Add(CreateParameter("@SessionId", turnState.SessionId));
		command.Parameters.Add(CreateParameter("@TurnIndex", turnState.TurnIndex));
		command.Parameters.Add(CreateParameter("@TotalTurns", turnState.TotalTurns));
		command.Parameters.Add(CreateParameter("@ActiveMemberId", turnState.ActiveMemberId));
		command.Parameters.Add(CreateParameter("@ActiveSlotIndex", turnState.ActiveSlotIndex));
		command.Parameters.Add(CreateParameter("@UtteranceId", turnState.UtteranceId));
		command.Parameters.Add(CreateParameter("@MaxReReads", turnState.MaxReReads));
		command.Parameters.Add(CreateParameter("@CreatedBy", turnState.CreatedBy));
		command.Parameters.Add(CreateParameter("@IPAddress", turnState.IPAddress));

		var result = await DbCommandHelper.ExecuteScalarAsync(command, cancellationToken);
		return Convert.ToInt64(result);
	}

	public async Task<TurnStateResponseDto?> GetCurrentTurnAsync(long sessionId, CancellationToken cancellationToken = default)
	{
		return await (
			from turnState in _dbContext.TurnStates.AsNoTracking()
			join activeMember in _dbContext.Users.AsNoTracking() on turnState.ActiveMemberId equals activeMember.UserId
			join utterance in _dbContext.Utterances.AsNoTracking() on turnState.UtteranceId equals utterance.UtteranceId
			where turnState.SessionId == sessionId
				&& turnState.TurnStatus == "ACTIVE"
				&& turnState.IsDeleted == false
				&& activeMember.IsDeleted == false
				&& utterance.IsDeleted == false
			orderby turnState.TurnIndex, turnState.TurnStateId
			select new TurnStateResponseDto
			{
				SessionId = turnState.SessionId,
				TurnIndex = turnState.TurnIndex,
				TotalTurns = turnState.TotalTurns,
				ActiveMemberId = turnState.ActiveMemberId,
				ActiveMemberName = activeMember.FullName,
				ActiveSlotIndex = turnState.ActiveSlotIndex,
				Utterance = new UtteranceResponseDto
				{
					UtteranceId = utterance.UtteranceId,
					ScriptId = utterance.ScriptId,
					SequenceId = utterance.SequenceId,
					SpeakerLabel = utterance.SpeakerLabel,
					EnglishText = utterance.EnglishText,
					HintText = utterance.HintText,
					GrammarTag = utterance.GrammarTag,
					ContextTag = utterance.ContextTag,
					FocusWord = utterance.FocusWord,
					PronunciationNote = utterance.PronunciationNote
				},
				ReReadAllowed = turnState.ReReadAllowed,
				ReReadCount = turnState.ReReadCount,
				MaxReReads = turnState.MaxReReads
			})
			.FirstOrDefaultAsync(cancellationToken);
	}

	public async Task<TurnState?> GetCurrentTurnEntityAsync(long sessionId, CancellationToken cancellationToken = default)
	{
		return await _dbContext.TurnStates
			.AsNoTracking()
			.FirstOrDefaultAsync(
				turnState => turnState.SessionId == sessionId &&
					turnState.TurnStatus == "ACTIVE" &&
					turnState.IsDeleted == false,
				cancellationToken);
	}

	public async Task<TurnState?> GetTurnBySessionAndTurnIndexAsync(long sessionId, int turnIndex, CancellationToken cancellationToken = default)
	{
		return await _dbContext.TurnStates
			.AsNoTracking()
			.FirstOrDefaultAsync(
				turnState => turnState.SessionId == sessionId &&
					turnState.TurnIndex == turnIndex &&
					turnState.IsDeleted == false,
				cancellationToken);
	}

	public async Task UpdateTurnStatusAsync(long turnStateId, string turnStatus, string updatedBy, string ipAddress, CancellationToken cancellationToken = default)
	{
		var connection = _dbContext.Database.GetDbConnection();
		await EnsureConnectionOpenAsync(connection, cancellationToken);

		await using var command = CreateCommand(connection, "dbo.uspUpdateTurnStatusByTurnStateId");
		command.Parameters.Add(CreateParameter("@TurnStateId", turnStateId));
		command.Parameters.Add(CreateParameter("@TurnStatus", turnStatus));
		command.Parameters.Add(CreateParameter("@UpdatedBy", updatedBy));
		command.Parameters.Add(CreateParameter("@IPAddress", ipAddress));

		await DbCommandHelper.ExecuteNonQueryAsync(command, cancellationToken);
	}

	public async Task IncrementReReadCountAsync(long turnStateId, string updatedBy, string ipAddress, CancellationToken cancellationToken = default)
	{
		var connection = _dbContext.Database.GetDbConnection();
		await EnsureConnectionOpenAsync(connection, cancellationToken);

		await using var command = CreateCommand(connection, "dbo.uspIncrementReReadCount");
		command.Parameters.Add(CreateParameter("@TurnStateId", turnStateId));
		command.Parameters.Add(CreateParameter("@UpdatedBy", updatedBy));
		command.Parameters.Add(CreateParameter("@IPAddress", ipAddress));

		await DbCommandHelper.ExecuteNonQueryAsync(command, cancellationToken);
	}

	public async Task<long> InsertVoiceAnalysisAsync(VoiceAnalysis voiceAnalysis, CancellationToken cancellationToken = default)
	{
		var connection = _dbContext.Database.GetDbConnection();
		await EnsureConnectionOpenAsync(connection, cancellationToken);

		await using var command = CreateCommand(connection, "dbo.uspInsertVoiceAnalysis");
		command.Parameters.Add(CreateParameter("@SessionId", voiceAnalysis.SessionId));
		command.Parameters.Add(CreateParameter("@UserId", voiceAnalysis.UserId));
		command.Parameters.Add(CreateParameter("@TurnIndex", voiceAnalysis.TurnIndex));
		command.Parameters.Add(CreateParameter("@UtteranceId", voiceAnalysis.UtteranceId));
		command.Parameters.Add(CreateParameter("@TranscribedText", voiceAnalysis.TranscribedText));
		command.Parameters.Add(CreateParameter("@ExpectedText", voiceAnalysis.ExpectedText));
		command.Parameters.Add(CreateParameter("@FluencyScore", voiceAnalysis.FluencyScore));
		command.Parameters.Add(CreateParameter("@ConfidenceScore", voiceAnalysis.ConfidenceScore));
		command.Parameters.Add(CreateParameter("@SpeakingSpeedWpm", voiceAnalysis.SpeakingSpeedWpm));
		command.Parameters.Add(CreateParameter("@PauseCount", voiceAnalysis.PauseCount));
		command.Parameters.Add(CreateParameter("@HesitationWords", voiceAnalysis.HesitationWords));
		command.Parameters.Add(CreateParameter("@RepeatedWords", voiceAnalysis.RepeatedWords));
		command.Parameters.Add(CreateParameter("@GrammarErrorsJson", voiceAnalysis.GrammarErrorsJson));
		command.Parameters.Add(CreateParameter("@PronunciationJson", voiceAnalysis.PronunciationJson));
		command.Parameters.Add(CreateParameter("@OverallScore", voiceAnalysis.OverallScore));
		command.Parameters.Add(CreateParameter("@CreatedBy", voiceAnalysis.CreatedBy));
		command.Parameters.Add(CreateParameter("@IPAddress", voiceAnalysis.IPAddress));

		var result = await DbCommandHelper.ExecuteScalarAsync(command, cancellationToken);
		return Convert.ToInt64(result);
	}

	public async Task<List<VoiceAnalysisResponseDto>> GetVoiceAnalysisBySessionIdAsync(long sessionId, CancellationToken cancellationToken = default)
	{
		var connection = _dbContext.Database.GetDbConnection();
		await EnsureConnectionOpenAsync(connection, cancellationToken);

		await using var command = CreateCommand(connection, "dbo.uspGetVoiceAnalysisBySessionId");
		command.Parameters.Add(CreateParameter("@SessionId", sessionId));

		await using var reader = await DbCommandHelper.ExecuteReaderAsync(command, cancellationToken);
		return await ReadVoiceAnalysisAsync(reader, cancellationToken);
	}

	public async Task<List<VoiceAnalysisResponseDto>> GetVoiceAnalysisByUserIdAsync(long userId, long sessionId = 0, CancellationToken cancellationToken = default)
	{
		var connection = _dbContext.Database.GetDbConnection();
		await EnsureConnectionOpenAsync(connection, cancellationToken);

		await using var command = CreateCommand(connection, "dbo.uspGetVoiceAnalysisByUserId");
		command.Parameters.Add(CreateParameter("@UserId", userId));
		command.Parameters.Add(CreateParameter("@SessionId", sessionId));

		await using var reader = await DbCommandHelper.ExecuteReaderAsync(command, cancellationToken);
		return await ReadVoiceAnalysisAsync(reader, cancellationToken);
	}

	public async Task InsertListenerFeedbackAsync(ListenerFeedback listenerFeedback, CancellationToken cancellationToken = default)
	{
		var connection = _dbContext.Database.GetDbConnection();
		await EnsureConnectionOpenAsync(connection, cancellationToken);

		await using var command = CreateCommand(connection, "dbo.uspInsertListenerFeedback");
		command.Parameters.Add(CreateParameter("@SessionId", listenerFeedback.SessionId));
		command.Parameters.Add(CreateParameter("@TurnIndex", listenerFeedback.TurnIndex));
		command.Parameters.Add(CreateParameter("@FromUserId", listenerFeedback.FromUserId));
		command.Parameters.Add(CreateParameter("@TargetUserId", listenerFeedback.TargetUserId));
		command.Parameters.Add(CreateParameter("@FeedbackTag", listenerFeedback.FeedbackTag));
		command.Parameters.Add(CreateParameter("@CreatedBy", listenerFeedback.CreatedBy));
		command.Parameters.Add(CreateParameter("@IPAddress", listenerFeedback.IPAddress));

		await DbCommandHelper.ExecuteNonQueryAsync(command, cancellationToken);
	}

	public async Task<SessionSummaryResponseDto?> GetSessionCompletionSummaryAsync(long sessionId, CancellationToken cancellationToken = default)
	{
		var connection = _dbContext.Database.GetDbConnection();
		await EnsureConnectionOpenAsync(connection, cancellationToken);

		await using var command = CreateCommand(connection, "dbo.uspGetSessionCompletionSummary");
		command.Parameters.Add(CreateParameter("@SessionId", sessionId));

		await using var reader = await DbCommandHelper.ExecuteReaderAsync(command, cancellationToken);
		var response = new SessionSummaryResponseDto();

		while (await reader.ReadAsync(cancellationToken))
		{
			response.MemberScores.Add(new MemberScoreDto
			{
				UserId = GetInt64(reader, "UserId"),
				FullName = GetString(reader, "FullName"),
				FluencyScore = GetDecimal(reader, "FluencyScore"),
				ConfidenceScore = GetDecimal(reader, "ConfidenceScore"),
				MistakeCount = GetInt32(reader, "MistakeCount"),
				ListenerRating = GetDecimal(reader, "ListenerRating")
			});
		}

		var sessionSummary = await (
			from session in _dbContext.Sessions.AsNoTracking()
			join script in _dbContext.Scripts.AsNoTracking() on session.ScriptId equals script.ScriptId
			where session.SessionId == sessionId
				&& session.IsDeleted == false
				&& script.IsDeleted == false
			select new
			{
				TotalTurns = script.UtteranceCount,
				script.ScriptTitle,
				script.GrammarFocusTag
			}
		).FirstOrDefaultAsync(cancellationToken);

		if (sessionSummary is null)
		{
			return response.MemberScores.Count == 0 ? null : response;
		}

		response.TotalTurns = sessionSummary.TotalTurns;
		response.ScriptTitle = sessionSummary.ScriptTitle;
		response.GrammarFocusTag = sessionSummary.GrammarFocusTag;
		response.TotalMistakesAllMembers = await _dbContext.Mistakes
			.AsNoTracking()
			.CountAsync(mistake => mistake.SessionId == sessionId && mistake.IsDeleted == false, cancellationToken);

		return response;
	}

	public async Task<List<SessionMember>> GetActiveSessionMembersBySessionIdAsync(long sessionId, CancellationToken cancellationToken = default)
	{
		return await _dbContext.SessionMembers
			.AsNoTracking()
			.Where(sessionMember => sessionMember.SessionId == sessionId && sessionMember.IsDeleted == false && sessionMember.IsActive)
			.OrderBy(sessionMember => sessionMember.SlotIndex)
			.ToListAsync(cancellationToken);
	}

	public async Task<SessionMember?> GetActiveSessionMemberByUserIdAsync(long sessionId, long userId, CancellationToken cancellationToken = default)
	{
		return await _dbContext.SessionMembers
			.AsNoTracking()
			.FirstOrDefaultAsync(
				sessionMember => sessionMember.SessionId == sessionId &&
					sessionMember.UserId == userId &&
					sessionMember.IsDeleted == false &&
					sessionMember.IsActive,
				cancellationToken);
	}

	public async Task<List<Utterance>> GetOrderedUtterancesBySessionIdAsync(long sessionId, CancellationToken cancellationToken = default)
	{
		var scriptId = await _dbContext.Sessions
			.AsNoTracking()
			.Where(session => session.SessionId == sessionId && session.IsDeleted == false)
			.Select(session => (long?)session.ScriptId)
			.FirstOrDefaultAsync(cancellationToken);

		if (scriptId is null or <= 0)
		{
			return new List<Utterance>();
		}

		return await _dbContext.Utterances
			.AsNoTracking()
			.Where(utterance => utterance.ScriptId == scriptId.Value && utterance.IsDeleted == false)
			.OrderBy(utterance => utterance.SequenceId)
			.ThenBy(utterance => utterance.UtteranceId)
			.ToListAsync(cancellationToken);
	}

	public async Task<bool> VoiceAnalysisExistsAsync(long sessionId, long userId, int turnIndex, CancellationToken cancellationToken = default)
	{
		return await _dbContext.VoiceAnalyses
			.AsNoTracking()
			.AnyAsync(
				voiceAnalysis => voiceAnalysis.SessionId == sessionId &&
					voiceAnalysis.UserId == userId &&
					voiceAnalysis.TurnIndex == turnIndex &&
					voiceAnalysis.IsDeleted == false,
				cancellationToken);
	}

	public async Task<bool> ListenerFeedbackExistsAsync(long sessionId, int turnIndex, long fromUserId, long targetUserId, string feedbackTag, CancellationToken cancellationToken = default)
	{
		return await _dbContext.ListenerFeedbacks
			.AsNoTracking()
			.AnyAsync(
				listenerFeedback => listenerFeedback.SessionId == sessionId &&
					listenerFeedback.TurnIndex == turnIndex &&
					listenerFeedback.FromUserId == fromUserId &&
					listenerFeedback.TargetUserId == targetUserId &&
					listenerFeedback.FeedbackTag == feedbackTag &&
					listenerFeedback.IsDeleted == false,
				cancellationToken);
	}

	private DbCommand CreateCommand(DbConnection connection, string storedProcedureName)
	{
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

	private static async Task<List<VoiceAnalysisResponseDto>> ReadVoiceAnalysisAsync(DbDataReader reader, CancellationToken cancellationToken)
	{
		var items = new List<VoiceAnalysisResponseDto>();

		while (await reader.ReadAsync(cancellationToken))
		{
			items.Add(new VoiceAnalysisResponseDto
			{
				VoiceAnalysisId = GetInt64(reader, "VoiceAnalysisId"),
				SessionId = GetInt64(reader, "SessionId"),
				UserId = GetInt64(reader, "UserId"),
				FullName = GetString(reader, "FullName"),
				TurnIndex = GetInt32(reader, "TurnIndex"),
				UtteranceId = GetInt64(reader, "UtteranceId"),
				TranscribedText = GetNullableString(reader, "TranscribedText"),
				ExpectedText = GetString(reader, "ExpectedText"),
				FluencyScore = GetDecimal(reader, "FluencyScore"),
				ConfidenceScore = GetDecimal(reader, "ConfidenceScore"),
				SpeakingSpeedWpm = GetInt32(reader, "SpeakingSpeedWpm"),
				PauseCount = GetInt32(reader, "PauseCount"),
				HesitationWords = SplitCsv(GetNullableString(reader, "HesitationWords")),
				RepeatedWords = SplitCsv(GetNullableString(reader, "RepeatedWords")),
				GrammarErrors = DeserializeJson<List<GrammarErrorDto>>(GetNullableString(reader, "GrammarErrorsJson")) ?? new List<GrammarErrorDto>(),
				PronunciationIssues = DeserializeJson<List<PronunciationIssueDto>>(GetNullableString(reader, "PronunciationJson")) ?? new List<PronunciationIssueDto>(),
				OverallScore = GetDecimal(reader, "OverallScore"),
				RecordedAt = GetDateTime(reader, "RecordedAt")
			});
		}

		return items;
	}

	private static T? DeserializeJson<T>(string? json)
	{
		if (string.IsNullOrWhiteSpace(json))
		{
			return default;
		}

		return JsonSerializer.Deserialize<T>(json);
	}

	private static List<string> SplitCsv(string? csv)
	{
		if (string.IsNullOrWhiteSpace(csv))
		{
			return new List<string>();
		}

		return csv
			.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
			.ToList();
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

	private static byte GetByte(DbDataReader reader, string columnName)
	{
		return reader.GetByte(reader.GetOrdinal(columnName));
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
