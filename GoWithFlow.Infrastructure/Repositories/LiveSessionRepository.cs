using System.Data;
using System.Data.Common;
using System.Text.Json;
using GoWithFlow.Application.Common;
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
			join script in _dbContext.Scripts.AsNoTracking() on utterance.ScriptId equals script.ScriptId
			join sessionRow in _dbContext.Sessions.AsNoTracking() on turnState.SessionId equals sessionRow.SessionId
			where turnState.SessionId == sessionId
				&& turnState.TurnStatus == "ACTIVE"
				&& turnState.IsDeleted == false
				&& activeMember.IsDeleted == false
				&& utterance.IsDeleted == false
				&& script.IsDeleted == false
			orderby turnState.TurnIndex, turnState.TurnStateId
			select new TurnStateResponseDto
			{
				SessionId = turnState.SessionId,
				TurnIndex = turnState.TurnIndex,
				TotalTurns = turnState.TotalTurns,
				ActiveMemberId = turnState.ActiveMemberId,
				ActiveMemberName = activeMember.FullName,
				ActiveMemberAvatarUrl = activeMember.AvatarUrl,
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
				MaxReReads = turnState.MaxReReads,
				IsFacilitatorTurn = FacilitatorRoles.IsFacilitator(script.Category, utterance.SpeakerLabel),
				// Phase 17 — match by slot (not UserId): the one reserved AI user can hold several slots.
				IsAi = _dbContext.SessionMembers.Any(member =>
					member.SessionId == turnState.SessionId &&
					member.SlotIndex == turnState.ActiveSlotIndex &&
					member.IsAi &&
					member.IsActive &&
					member.IsDeleted == false),
				AiVoiceGender = sessionRow.AiVoiceGender,
				AiSpeechRate = sessionRow.AiSpeechRate,
				AiQuestionDelaySec = sessionRow.AiQuestionDelaySec
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

	public async Task CompleteAndAdvanceTurnAsync(
		long completedTurnStateId,
		string completedStatus,
		string completedBy,
		string completedByIp,
		TurnState nextTurn,
		CancellationToken cancellationToken = default)
	{
		var connection = _dbContext.Database.GetDbConnection();
		await EnsureConnectionOpenAsync(connection, cancellationToken);

		await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

		try
		{
			// 1. Mark the current turn COMPLETED.
			await using (var completeCommand = CreateCommand(connection, "dbo.uspUpdateTurnStatusByTurnStateId"))
			{
				completeCommand.Transaction = transaction;
				completeCommand.Parameters.Add(CreateParameter("@TurnStateId", completedTurnStateId));
				completeCommand.Parameters.Add(CreateParameter("@TurnStatus", completedStatus));
				completeCommand.Parameters.Add(CreateParameter("@UpdatedBy", completedBy));
				completeCommand.Parameters.Add(CreateParameter("@IPAddress", completedByIp));
				await DbCommandHelper.ExecuteNonQueryAsync(completeCommand, cancellationToken);
			}

			// 2. Insert the next turn. If this fails, the transaction rolls back and the
			//    current turn stays ACTIVE — the session is never left without an active turn.
			await using (var insertCommand = CreateCommand(connection, "dbo.uspInsertTurnState"))
			{
				insertCommand.Transaction = transaction;
				insertCommand.Parameters.Add(CreateParameter("@SessionId", nextTurn.SessionId));
				insertCommand.Parameters.Add(CreateParameter("@TurnIndex", nextTurn.TurnIndex));
				insertCommand.Parameters.Add(CreateParameter("@TotalTurns", nextTurn.TotalTurns));
				insertCommand.Parameters.Add(CreateParameter("@ActiveMemberId", nextTurn.ActiveMemberId));
				insertCommand.Parameters.Add(CreateParameter("@ActiveSlotIndex", nextTurn.ActiveSlotIndex));
				insertCommand.Parameters.Add(CreateParameter("@UtteranceId", nextTurn.UtteranceId));
				insertCommand.Parameters.Add(CreateParameter("@MaxReReads", nextTurn.MaxReReads));
				insertCommand.Parameters.Add(CreateParameter("@CreatedBy", nextTurn.CreatedBy));
				insertCommand.Parameters.Add(CreateParameter("@IPAddress", nextTurn.IPAddress));
				await DbCommandHelper.ExecuteNonQueryAsync(insertCommand, cancellationToken);
			}

			await transaction.CommitAsync(cancellationToken);
		}
		catch
		{
			await transaction.RollbackAsync(cancellationToken);
			throw;
		}
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
		command.Parameters.Add(CreateJsonParameter("@GrammarErrorsJson", voiceAnalysis.GrammarErrorsJson ?? "[]"));
		command.Parameters.Add(CreateJsonParameter("@PronunciationJson", voiceAnalysis.PronunciationJson ?? "[]"));
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

		var response = new SessionSummaryResponseDto();

		await using (var command = CreateCommand(connection, "dbo.uspGetSessionCompletionSummary"))
		{
			command.Parameters.Add(CreateParameter("@SessionId", sessionId));

			await using var reader = await DbCommandHelper.ExecuteReaderAsync(command, cancellationToken);

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
		} // reader and command disposed here — connection free for EF queries below

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
				script.GrammarFocusTag,
				script.Category
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

		if (response.MemberScores.Count > 0)
		{
			var userIds = response.MemberScores.Select(s => s.UserId).ToList();

			// Per-user mistake counts from tblMistake (canonical source, populated before summary is built)
			var mistakesByUser = await _dbContext.Mistakes
				.AsNoTracking()
				.Where(m => m.SessionId == sessionId && m.IsDeleted == false && userIds.Contains(m.UserId))
				.GroupBy(m => m.UserId)
				.Select(g => new { UserId = g.Key, Count = g.Count() })
				.ToDictionaryAsync(x => x.UserId, x => x.Count, cancellationToken);

			// Avatar raw keys (R2 key or legacy URL) — resolved to presigned URLs in service layer
			var avatarByUser = await _dbContext.Users
				.AsNoTracking()
				.Where(u => userIds.Contains(u.UserId) && u.IsDeleted == false)
				.Select(u => new { u.UserId, u.AvatarUrl })
				.ToDictionaryAsync(u => u.UserId, u => u.AvatarUrl, cancellationToken);

			// Slot + AI lookup for facilitator/AI tagging. Group by user: the reserved AI participant
			// can hold MORE THAN ONE slot in a multi-role session, so a plain ToDictionary(UserId)
			// would throw on the duplicate key.
			var members = await _dbContext.SessionMembers
				.AsNoTracking()
				.Where(m => m.SessionId == sessionId && m.IsDeleted == false)
				.Select(m => new { m.UserId, m.SlotName, m.IsAi })
				.ToListAsync(cancellationToken);

			var slotByUser = members
				.GroupBy(m => m.UserId)
				.ToDictionary(g => g.Key, g => g.First().SlotName ?? string.Empty);
			var aiByUser = members
				.GroupBy(m => m.UserId)
				.ToDictionary(g => g.Key, g => g.Any(m => m.IsAi));

			foreach (var score in response.MemberScores)
			{
				score.MistakeCount = mistakesByUser.TryGetValue(score.UserId, out var cnt) ? cnt : 0;
				score.AvatarUrl = avatarByUser.TryGetValue(score.UserId, out var url) ? url : null;
				score.IsAi = aiByUser.TryGetValue(score.UserId, out var isAi) && isAi;

				if (slotByUser.TryGetValue(score.UserId, out var slotName))
				{
					score.IsFacilitator = FacilitatorRoles.IsFacilitator(sessionSummary.Category, slotName);
				}
			}
		}

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

	public async Task<SessionMember?> GetSessionMemberByUserIdAsync(long sessionId, long userId, CancellationToken cancellationToken = default)
	{
		// Returns any member row (active or inactive) — used to detect page-refresh reconnects.
		return await _dbContext.SessionMembers
			.AsNoTracking()
			.FirstOrDefaultAsync(
				sessionMember => sessionMember.SessionId == sessionId &&
					sessionMember.UserId == userId &&
					sessionMember.IsDeleted == false,
				cancellationToken);
	}

	public async Task ReactivateMemberAsync(long sessionId, long userId, CancellationToken cancellationToken = default)
	{
		// Restore IsActive = true for a member who disconnected temporarily (page refresh).
		// Intentionally not AsNoTracking — we need EF change tracking to update the row.
		var member = await _dbContext.SessionMembers
			.FirstOrDefaultAsync(
				sm => sm.SessionId == sessionId &&
				      sm.UserId == userId &&
				      sm.IsDeleted == false,
				cancellationToken);

		if (member is not null && member.IsActive == false)
		{
			member.IsActive = true;
			await _dbContext.SaveChangesAsync(cancellationToken);
		}
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

	public async Task<VoiceAnalysis?> GetVoiceAnalysisByUserTurnAsync(long sessionId, long userId, int turnIndex, CancellationToken cancellationToken = default)
	{
		return await _dbContext.VoiceAnalyses
			.FirstOrDefaultAsync(
				v => v.SessionId == sessionId &&
					v.UserId == userId &&
					v.TurnIndex == turnIndex &&
					v.IsDeleted == false,
				cancellationToken);
	}

	public async Task UpdateVoiceAnalysisAsync(long voiceAnalysisId, VoiceAnalysis updates, string updatedBy, CancellationToken cancellationToken = default)
	{
		await _dbContext.VoiceAnalyses
			.Where(v => v.VoiceAnalysisId == voiceAnalysisId && v.IsDeleted == false)
			.ExecuteUpdateAsync(s => s
				.SetProperty(v => v.TranscribedText, updates.TranscribedText)
				.SetProperty(v => v.FluencyScore, updates.FluencyScore)
				.SetProperty(v => v.ConfidenceScore, updates.ConfidenceScore)
				.SetProperty(v => v.SpeakingSpeedWpm, updates.SpeakingSpeedWpm)
				.SetProperty(v => v.PauseCount, updates.PauseCount)
				.SetProperty(v => v.HesitationWords, updates.HesitationWords)
				.SetProperty(v => v.RepeatedWords, updates.RepeatedWords)
				.SetProperty(v => v.GrammarErrorsJson, updates.GrammarErrorsJson)
				.SetProperty(v => v.PronunciationJson, updates.PronunciationJson)
				.SetProperty(v => v.OverallScore, updates.OverallScore)
				.SetProperty(v => v.UpdatedBy, updatedBy)
				.SetProperty(v => v.LastUpdated, DateTime.UtcNow),
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

	private DbParameter CreateJsonParameter(string parameterName, string json)
	{
		return DbCommandHelper.CreateJsonParameter(_dbContext.DatabaseProvider, parameterName, json);
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

	public async Task<SessionReviewResponseDto?> GetSessionReviewAsync(long sessionId, long userId, CancellationToken cancellationToken = default)
	{
		// Session → Script metadata
		var sessionMeta = await (
			from session in _dbContext.Sessions.AsNoTracking()
			join script in _dbContext.Scripts.AsNoTracking() on session.ScriptId equals script.ScriptId
			where session.SessionId == sessionId && session.IsDeleted == false && script.IsDeleted == false
			select new
			{
				session.SessionId,
				script.ScriptTitle,
				script.Category,
				script.GrammarFocusTag,
				script.ScriptId
			}
		).FirstOrDefaultAsync(cancellationToken);

		if (sessionMeta is null)
			return null;

		// All utterances for this script ordered by SequenceId
		var utterances = await _dbContext.Utterances.AsNoTracking()
			.Where(u => u.ScriptId == sessionMeta.ScriptId && u.IsDeleted == false)
			.OrderBy(u => u.SequenceId)
			.ToListAsync(cancellationToken);

		// Voice analysis rows for this user + session (existing SP call)
		var voiceAnalysisList = await GetVoiceAnalysisByUserIdAsync(userId, sessionId, cancellationToken);
		var vaDictionary = voiceAnalysisList.ToDictionary(v => v.UtteranceId);

		// Build per-turn review
		var turns = new List<SessionReviewTurnDto>(utterances.Count);
		var turnIndex = 1;
		foreach (var utterance in utterances)
		{
			var isFacilitator = FacilitatorRoles.IsFacilitator(sessionMeta.Category, utterance.SpeakerLabel ?? string.Empty);
			vaDictionary.TryGetValue(utterance.UtteranceId, out var va);

			turns.Add(new SessionReviewTurnDto
			{
				TurnIndex             = turnIndex++,
				SpeakerLabel          = utterance.SpeakerLabel ?? string.Empty,
				IsFacilitatorTurn     = isFacilitator,
				EnglishText           = utterance.EnglishText ?? string.Empty,
				TranscribedText       = va?.TranscribedText,
				FluencyScore          = va?.FluencyScore ?? 0,
				ConfidenceScore       = va?.ConfidenceScore ?? 0,
				SpeakingSpeedWpm      = va?.SpeakingSpeedWpm ?? 0,
				OverallScore          = va?.OverallScore ?? 0,
				HesitationWords       = va?.HesitationWords ?? new List<string>(),
				GrammarErrors         = va?.GrammarErrors ?? new List<GrammarErrorDto>(),
				PronunciationIssues   = va?.PronunciationIssues ?? new List<PronunciationIssueDto>(),
				WasAnalyzed           = va is not null
			});
		}

		// Average score across performance turns that were analyzed
		var performanceTurns = turns.Where(t => !t.IsFacilitatorTurn && t.WasAnalyzed).ToList();
		var avgScore = performanceTurns.Count > 0
			? Math.Round(performanceTurns.Average(t => t.OverallScore), 2)
			: 0m;

		return new SessionReviewResponseDto
		{
			SessionId          = sessionId,
			ScriptTitle        = sessionMeta.ScriptTitle,
			Category           = sessionMeta.Category,
			GrammarFocusTag    = sessionMeta.GrammarFocusTag,
			TotalTurns         = utterances.Count,
			AverageOverallScore = avgScore,
			Turns              = turns
		};
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

	public async Task UpdateVoiceAnalysisAudioKeyAsync(long voiceAnalysisId, string audioStorageKey, CancellationToken cancellationToken = default)
	{
		var entity = await _dbContext.VoiceAnalyses.FindAsync(new object[] { voiceAnalysisId }, cancellationToken);
		if (entity is null) return;

		entity.AudioStorageKey = audioStorageKey;
		await _dbContext.SaveChangesAsync(cancellationToken);
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
