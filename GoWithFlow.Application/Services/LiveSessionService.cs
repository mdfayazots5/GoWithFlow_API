using System.Text.Json;
using GoWithFlow.Application.Common;
using GoWithFlow.Application.DTOs.Requests.LiveSession;
using GoWithFlow.Application.DTOs.Responses.LiveSession;
using GoWithFlow.Application.Helpers;
using GoWithFlow.Application.Interfaces.Repositories;
using GoWithFlow.Application.Interfaces.Services;
using GoWithFlow.Application.Settings;
using GoWithFlow.Domain.Entities;
using GoWithFlow.Domain.Enums;
using Microsoft.Extensions.Options;

namespace GoWithFlow.Application.Services;

public sealed class LiveSessionService : ILiveSessionService
{
	private static readonly Dictionary<string, string> FeedbackTagMap = new(StringComparer.OrdinalIgnoreCase)
	{
		["Good"]       = "Good",
		["Needs Work"] = "Needs Work",
		["NeedsWork"]  = "Needs Work"
	};

	private readonly IUserRepository _userRepository;
	private readonly ISessionRepository _sessionRepository;
	private readonly ILiveSessionRepository _liveSessionRepository;
	private readonly IUserService _userService;
	private readonly IMistakeService _mistakeService;
	private readonly IVocabularyService _vocabularyService;
	private readonly IStorageService _storageService;
	private readonly ISessionRecordingService _sessionRecordingService;
	private readonly CloudflareR2Settings _r2Settings;

	public LiveSessionService(
		IUserRepository userRepository,
		ISessionRepository sessionRepository,
		ILiveSessionRepository liveSessionRepository,
		IUserService userService,
		IMistakeService mistakeService,
		IVocabularyService vocabularyService,
		IStorageService storageService,
		ISessionRecordingService sessionRecordingService,
		IOptions<CloudflareR2Settings> r2Options)
	{
		_userRepository          = userRepository;
		_sessionRepository       = sessionRepository;
		_liveSessionRepository   = liveSessionRepository;
		_userService             = userService;
		_mistakeService          = mistakeService;
		_vocabularyService       = vocabularyService;
		_storageService          = storageService;
		_sessionRecordingService = sessionRecordingService;
		_r2Settings              = r2Options.Value;
	}

	public async Task<ApiResponse<TurnStateResponseDto>> GetCurrentTurnAsync(long sessionId, CancellationToken cancellationToken = default)
	{
		if (sessionId <= 0)
		{
			return ApiResponse<TurnStateResponseDto>.FailureResult(new[] { "SessionId must be greater than zero." }, "Validation failed.");
		}

		var (turn, error) = await EnsureCurrentTurnAsync(sessionId, "System", cancellationToken);

		if (turn is null)
		{
			return ApiResponse<TurnStateResponseDto>.FailureResult(new[] { error ?? "Active turn was not found for this session." }, "Current turn not found.");
		}

		return ApiResponse<TurnStateResponseDto>.SuccessResult(turn, "Current turn retrieved successfully.");
	}

	public async Task<ApiResponse<TurnStateResponseDto>> ShiftTurnAsync(TurnShiftRequestDto dto, long userId, CancellationToken cancellationToken = default)
	{
		if (dto.SessionId <= 0 || dto.MemberId <= 0 || dto.TurnIndex <= 0 || userId <= 0)
		{
			return ApiResponse<TurnStateResponseDto>.FailureResult(new[] { "SessionId, MemberId, TurnIndex, and UserId must be greater than zero." }, "Validation failed.");
		}

		if (dto.MemberId != userId)
		{
			return ApiResponse<TurnStateResponseDto>.FailureResult(new[] { "Only the active speaker can complete the current turn." }, "Turn shift failed.");
		}

		var user = await _userRepository.GetByUserIdAsync(userId, cancellationToken);
		var session = await _sessionRepository.GetSessionBySessionIdAsync(dto.SessionId, cancellationToken);
		var currentTurnEntity = await _liveSessionRepository.GetCurrentTurnEntityAsync(dto.SessionId, cancellationToken);

		if (user is null || session is null || currentTurnEntity is null)
		{
			return ApiResponse<TurnStateResponseDto>.FailureResult(new[] { "Session, current turn, or user was not found." }, "Turn shift failed.");
		}

		if (string.Equals(session.Status, SessionStatusType.ACTIVE.ToString(), StringComparison.OrdinalIgnoreCase) == false)
		{
			return ApiResponse<TurnStateResponseDto>.FailureResult(new[] { "Session must be active to shift turns." }, "Turn shift failed.");
		}

		if (currentTurnEntity.ActiveMemberId != dto.MemberId || currentTurnEntity.TurnIndex != dto.TurnIndex)
		{
			return ApiResponse<TurnStateResponseDto>.FailureResult(new[] { "The provided turn does not match the active turn." }, "Turn shift failed.");
		}

		// Resolve-validate-advance is shared with AdvanceAiTurnAsync — see AdvanceFromCurrentTurnAsync
		// for the brick-prevention contract.
		return await AdvanceFromCurrentTurnAsync(session, currentTurnEntity, user.FullName, cancellationToken);
	}

	public async Task<ApiResponse<TurnStateResponseDto>> AdvanceAiTurnAsync(long sessionId, int turnIndex, long callerUserId, CancellationToken cancellationToken = default)
	{
		if (sessionId <= 0 || turnIndex <= 0 || callerUserId <= 0)
		{
			return ApiResponse<TurnStateResponseDto>.FailureResult(new[] { "SessionId, TurnIndex, and UserId must be greater than zero." }, "Validation failed.");
		}

		var session = await _sessionRepository.GetSessionBySessionIdAsync(sessionId, cancellationToken);
		var currentTurnEntity = await _liveSessionRepository.GetCurrentTurnEntityAsync(sessionId, cancellationToken);
		var currentTurnDto = await _liveSessionRepository.GetCurrentTurnAsync(sessionId, cancellationToken);
		var callerMember = await _liveSessionRepository.GetActiveSessionMemberByUserIdAsync(sessionId, callerUserId, cancellationToken);

		if (session is null || currentTurnEntity is null || currentTurnDto is null || callerMember is null)
		{
			return ApiResponse<TurnStateResponseDto>.FailureResult(new[] { "Session, current turn, or caller membership was not found." }, "AI turn advance failed.");
		}

		if (string.Equals(session.Status, SessionStatusType.ACTIVE.ToString(), StringComparison.OrdinalIgnoreCase) == false)
		{
			return ApiResponse<TurnStateResponseDto>.FailureResult(new[] { "Session must be active to advance the AI turn." }, "AI turn advance failed.");
		}

		if (currentTurnEntity.TurnIndex != turnIndex)
		{
			return ApiResponse<TurnStateResponseDto>.FailureResult(new[] { "The provided turn does not match the active turn." }, "AI turn advance failed.");
		}

		// Only an AI-held turn may be advanced through this path; human turns must go through CompleteTurn
		// (which scores). And only a human member may drive it — the AI never holds a hub connection.
		if (currentTurnDto.IsAi == false)
		{
			return ApiResponse<TurnStateResponseDto>.FailureResult(new[] { "The current turn is not an AI turn." }, "AI turn advance failed.");
		}

		if (callerMember.IsAi)
		{
			return ApiResponse<TurnStateResponseDto>.FailureResult(new[] { "AI members cannot advance turns." }, "AI turn advance failed.");
		}

		// No voice analysis is written for AI turns. Reuse the same atomic advance as a human shift.
		return await AdvanceFromCurrentTurnAsync(session, currentTurnEntity, currentTurnDto.ActiveMemberName, cancellationToken);
	}

	/// <summary>
	/// Shared resolve-validate-advance core used by both ShiftTurnAsync (human) and AdvanceAiTurnAsync.
	/// CRITICAL: resolve and VALIDATE the next turn BEFORE mutating the current turn. If the next turn
	/// cannot be created (e.g. the next utterance's speaker label matches no active slot), the current
	/// turn must stay ACTIVE. Marking it COMPLETED first — as the old code did — left the session with
	/// no ACTIVE turn and bricked it permanently. See the Turn Shift flow drift note (2026-06-05) in
	/// ProjectOverview. Keeping this in ONE place prevents the two callers from diverging.
	/// </summary>
	private async Task<ApiResponse<TurnStateResponseDto>> AdvanceFromCurrentTurnAsync(
		Session session, TurnState currentTurnEntity, string actorName, CancellationToken cancellationToken)
	{
		var (nextTurn, isEndOfScript, nextTurnError) = await ResolveNextTurnAsync(
			session, currentTurnEntity.TurnIndex + 1, actorName, cancellationToken);

		if (nextTurnError is not null)
		{
			// Current turn is untouched — session is NOT bricked. The caller can fix the script /
			// slot configuration and retry the same turn.
			return ApiResponse<TurnStateResponseDto>.FailureResult(new[] { nextTurnError }, "Turn shift failed.");
		}

		if (isEndOfScript || nextTurn is null)
		{
			// Genuine end of script: complete the final turn, then signal session completion.
			// The signal is carried in Message (TurnShiftSignals.SessionComplete) — the field the
			// hub inspects — so the last turn triggers auto-completion instead of throwing.
			await _liveSessionRepository.UpdateTurnStatusAsync(
				currentTurnEntity.TurnStateId,
				TurnStatusType.COMPLETED.ToString(),
				actorName,
				"127.0.0.1",
				cancellationToken);

			return ApiResponse<TurnStateResponseDto>.FailureResult(
				new[] { "No further turns remain in this session. Complete the session." },
				TurnShiftSignals.SessionComplete);
		}

		// Atomic advance: mark the current turn COMPLETED and insert the next turn in one
		// transaction. If the insert fails, the completion is rolled back and the current turn
		// stays ACTIVE — the session can never be left without an active turn.
		await _liveSessionRepository.CompleteAndAdvanceTurnAsync(
			currentTurnEntity.TurnStateId,
			TurnStatusType.COMPLETED.ToString(),
			actorName,
			"127.0.0.1",
			nextTurn,
			cancellationToken);

		var createdTurn = await _liveSessionRepository.GetCurrentTurnAsync(session.SessionId, cancellationToken);

		if (createdTurn is null)
		{
			return ApiResponse<TurnStateResponseDto>.FailureResult(
				new[] { "Turn was advanced but the new turn could not be retrieved. Check uspGetCurrentTurnBySessionId." },
				"Turn shift failed.");
		}

		createdTurn.ActiveMemberAvatarUrl = await ResolveAvatarUrlAsync(createdTurn.ActiveMemberAvatarUrl, cancellationToken);

		return ApiResponse<TurnStateResponseDto>.SuccessResult(createdTurn, "Turn shifted successfully.");
	}

	public async Task<ApiResponse<VoiceAnalysisResponseDto>> SaveVoiceAnalysisAsync(SaveVoiceAnalysisRequestDto dto, long userId, CancellationToken cancellationToken = default)
	{
		if (dto.SessionId <= 0 || dto.TurnIndex <= 0 || dto.UtteranceId <= 0 || userId <= 0)
		{
			return ApiResponse<VoiceAnalysisResponseDto>.FailureResult(new[] { "SessionId, TurnIndex, UtteranceId, and UserId must be greater than zero." }, "Validation failed.");
		}

		var user = await _userRepository.GetByUserIdAsync(userId, cancellationToken);
		// Use GetSessionMemberByUserIdAsync (any IsActive state) — the speaker may have briefly
		// disconnected (page refresh race) but their voice analysis is still valid for the turn
		// they owned. Turn ownership is enforced by ActiveMemberId == userId below.
		var sessionMember = await _liveSessionRepository.GetSessionMemberByUserIdAsync(dto.SessionId, userId, cancellationToken);
		var turnState = await _liveSessionRepository.GetTurnBySessionAndTurnIndexAsync(dto.SessionId, dto.TurnIndex, cancellationToken);

		if (user is null || sessionMember is null || turnState is null)
		{
			return ApiResponse<VoiceAnalysisResponseDto>.FailureResult(new[] { "Session member or turn was not found." }, "Voice analysis save failed.");
		}

		if (turnState.ActiveMemberId != userId || turnState.UtteranceId != dto.UtteranceId)
		{
			return ApiResponse<VoiceAnalysisResponseDto>.FailureResult(new[] { "Voice analysis can only be saved for the caller's active turn." }, "Voice analysis save failed.");
		}

		// Build the value object (shared for both insert and update paths)
		var voiceAnalysis = new VoiceAnalysis
		{
			SessionId = dto.SessionId,
			UserId = userId,
			TurnIndex = dto.TurnIndex,
			UtteranceId = dto.UtteranceId,
			TranscribedText = NormalizeNullableText(dto.TranscribedText),
			ExpectedText = dto.ExpectedText.Trim(),
			FluencyScore = dto.FluencyScore,
			ConfidenceScore = dto.ConfidenceScore,
			SpeakingSpeedWpm = dto.SpeakingSpeedWpm,
			PauseCount = dto.PauseCount,
			HesitationWords = JoinCsv(dto.HesitationWords),
			RepeatedWords = JoinCsv(dto.RepeatedWords),
			GrammarErrorsJson = SerializeJson(dto.GrammarErrors),
			PronunciationJson = SerializeJson(dto.PronunciationIssues),
			OverallScore = dto.OverallScore,
			CreatedBy = user.FullName,
			IPAddress = "127.0.0.1"
		};

		// UPSERT: if a record already exists for this session+user+turn (e.g. speaker re-recording
		// after page refresh before CompleteTurn was called), update it — do not reject.
		// This makes the endpoint idempotent for the active speaker's own turn.
		var existing = await _liveSessionRepository.GetVoiceAnalysisByUserTurnAsync(dto.SessionId, userId, dto.TurnIndex, cancellationToken);

		long voiceAnalysisId;
		string saveMessage;

		if (existing is not null)
		{
			// Update path: speaker re-recorded the same active turn
			voiceAnalysisId = existing.VoiceAnalysisId;
			await _liveSessionRepository.UpdateVoiceAnalysisAsync(voiceAnalysisId, voiceAnalysis, user.FullName, cancellationToken);
			saveMessage = "Voice analysis updated successfully.";
		}
		else
		{
			// Insert path: first recording for this turn
			voiceAnalysisId = await _liveSessionRepository.InsertVoiceAnalysisAsync(voiceAnalysis, cancellationToken);
			saveMessage = "Voice analysis saved successfully.";
		}

		// Optional: upload audio blob to R2 if provided by the frontend
		if (!string.IsNullOrWhiteSpace(dto.AudioBase64))
		{
			try
			{
				var audioBytes = Convert.FromBase64String(dto.AudioBase64);
				var audioKey   = StorageKeyBuilder.VoiceRecording(dto.SessionId, dto.TurnIndex, userId);
				var bucket     = _r2Settings.Buckets.Audio;

				await using var audioStream = new MemoryStream(audioBytes);
				await _storageService.UploadAsync(audioStream, bucket, audioKey, "audio/ogg", cancellationToken);
				await _liveSessionRepository.UpdateVoiceAnalysisAudioKeyAsync(voiceAnalysisId, audioKey, cancellationToken);
			}
			catch (FormatException)
			{
				// Invalid Base64 — skip audio upload, do not fail the voice analysis save
			}
		}

		return ApiResponse<VoiceAnalysisResponseDto>.SuccessResult(
			new VoiceAnalysisResponseDto
			{
				VoiceAnalysisId = voiceAnalysisId,
				SessionId = dto.SessionId,
				UserId = userId,
				FullName = user.FullName,
				TurnIndex = dto.TurnIndex,
				UtteranceId = dto.UtteranceId,
				TranscribedText = voiceAnalysis.TranscribedText,
				ExpectedText = voiceAnalysis.ExpectedText,
				FluencyScore = dto.FluencyScore,
				ConfidenceScore = dto.ConfidenceScore,
				SpeakingSpeedWpm = dto.SpeakingSpeedWpm,
				PauseCount = dto.PauseCount,
				HesitationWords = NormalizeStringList(dto.HesitationWords),
				RepeatedWords = NormalizeStringList(dto.RepeatedWords),
				GrammarErrors = dto.GrammarErrors,
				PronunciationIssues = dto.PronunciationIssues,
				OverallScore = dto.OverallScore,
				RecordedAt = DateTime.UtcNow
			},
			saveMessage);
	}

	public async Task<ApiResponse<bool>> SubmitListenerFeedbackAsync(ListenerFeedbackRequestDto dto, long userId, CancellationToken cancellationToken = default)
	{
		if (dto.SessionId <= 0 || dto.TurnIndex <= 0 || dto.TargetUserId <= 0 || userId <= 0)
		{
			return ApiResponse<bool>.FailureResult(new[] { "SessionId, TurnIndex, TargetUserId, and UserId must be greater than zero." }, "Validation failed.");
		}

		var normalizedFeedbackTag = NormalizeFeedbackTag(dto.FeedbackTag);

		if (normalizedFeedbackTag is null)
		{
			return ApiResponse<bool>.FailureResult(new[] { "FeedbackTag is invalid." }, "Validation failed.");
		}

		var fromUser = await _userRepository.GetByUserIdAsync(userId, cancellationToken);
		var targetUser = await _userRepository.GetByUserIdAsync(dto.TargetUserId, cancellationToken);
		var fromSessionMember = await _liveSessionRepository.GetActiveSessionMemberByUserIdAsync(dto.SessionId, userId, cancellationToken);
		var targetTurn = await _liveSessionRepository.GetTurnBySessionAndTurnIndexAsync(dto.SessionId, dto.TurnIndex, cancellationToken);

		if (fromUser is null || targetUser is null || fromSessionMember is null || targetTurn is null)
		{
			return ApiResponse<bool>.FailureResult(new[] { "Feedback source, target, or turn was not found." }, "Listener feedback submission failed.");
		}

		if (targetTurn.ActiveMemberId != dto.TargetUserId)
		{
			return ApiResponse<bool>.FailureResult(new[] { "TargetUserId does not match the requested turn speaker." }, "Listener feedback submission failed.");
		}

		if (dto.TargetUserId == userId)
		{
			return ApiResponse<bool>.FailureResult(new[] { "Users cannot submit listener feedback for themselves." }, "Listener feedback submission failed.");
		}

		var alreadyExists = await _liveSessionRepository.ListenerFeedbackExistsAsync(
			dto.SessionId,
			dto.TurnIndex,
			userId,
			dto.TargetUserId,
			normalizedFeedbackTag,
			cancellationToken);

		if (alreadyExists)
		{
			return ApiResponse<bool>.FailureResult(new[] { "Duplicate listener feedback is not allowed for the same turn and tag." }, "Listener feedback submission failed.");
		}

		await _liveSessionRepository.InsertListenerFeedbackAsync(
			new ListenerFeedback
			{
				SessionId = dto.SessionId,
				TurnIndex = dto.TurnIndex,
				FromUserId = userId,
				TargetUserId = dto.TargetUserId,
				FeedbackTag = normalizedFeedbackTag,
				CreatedBy = fromUser.FullName,
				IPAddress = "127.0.0.1"
			},
			cancellationToken);

		return ApiResponse<bool>.SuccessResult(true, "Listener feedback submitted successfully.");
	}

	public async Task<ApiResponse<bool>> SubmitListenerFeedbackByTurnIndexAsync(long sessionId, string feedbackTag, int targetTurnIndex, long userId, CancellationToken cancellationToken = default)
	{
		var targetTurn = await _liveSessionRepository.GetTurnBySessionAndTurnIndexAsync(sessionId, targetTurnIndex, cancellationToken);

		if (targetTurn is null)
		{
			return ApiResponse<bool>.FailureResult(new[] { "Target turn was not found." }, "Listener feedback submission failed.");
		}

		return await SubmitListenerFeedbackAsync(
			new ListenerFeedbackRequestDto
			{
				SessionId = sessionId,
				TurnIndex = targetTurnIndex,
				TargetUserId = targetTurn.ActiveMemberId,
				FeedbackTag = feedbackTag
			},
			userId,
			cancellationToken);
	}

	public async Task<ApiResponse<SessionSummaryResponseDto>> CompleteSessionAsync(long sessionId, CancellationToken cancellationToken = default)
	{
		if (sessionId <= 0)
		{
			return ApiResponse<SessionSummaryResponseDto>.FailureResult(new[] { "SessionId must be greater than zero." }, "Validation failed.");
		}

		var session = await _sessionRepository.GetSessionBySessionIdAsync(sessionId, cancellationToken);

		if (session is null)
		{
			return ApiResponse<SessionSummaryResponseDto>.FailureResult(new[] { "Session was not found." }, "Session completion failed.");
		}

		if (string.Equals(session.Status, SessionStatusType.COMPLETED.ToString(), StringComparison.OrdinalIgnoreCase))
		{
			var existingSummary = await _liveSessionRepository.GetSessionCompletionSummaryAsync(sessionId, cancellationToken);

			if (existingSummary is null)
			{
				return ApiResponse<SessionSummaryResponseDto>.FailureResult(new[] { "Session summary could not be generated." }, "Session completion failed.");
			}

			await ResolveAvatarUrlsAsync(existingSummary, cancellationToken);
			return ApiResponse<SessionSummaryResponseDto>.SuccessResult(existingSummary, "Session completed successfully.");
		}

		var currentTurn = await _liveSessionRepository.GetCurrentTurnEntityAsync(sessionId, cancellationToken);

		if (currentTurn is not null)
		{
			await _liveSessionRepository.UpdateTurnStatusAsync(currentTurn.TurnStateId, TurnStatusType.COMPLETED.ToString(), "System", "127.0.0.1", cancellationToken);
		}

		await _sessionRepository.UpdateSessionStatusAsync(sessionId, SessionStatusType.COMPLETED.ToString(), "System", "127.0.0.1", cancellationToken);

		var completedSession = await _sessionRepository.GetSessionBySessionIdAsync(sessionId, cancellationToken);

		long? vocabularyLearnerId = null;

		if (completedSession is not null)
		{
			var activeMembers = await _liveSessionRepository.GetActiveSessionMembersBySessionIdAsync(sessionId, cancellationToken);
			var practiceMinutes = ResolvePracticeMinutes(completedSession);

			foreach (var memberId in activeMembers.Select(sessionMember => sessionMember.UserId).Distinct())
			{
				await _mistakeService.SaveMistakesFromSessionAsync(sessionId, memberId, cancellationToken);
				await _userService.UpsertStreakAsync(memberId, practiceMinutes, cancellationToken);
				await _userService.CheckAndAwardBadgesAsync(memberId, cancellationToken);
			}

			// Vocabulary tracking: for VocabularySprint sessions, save FocusWords per Learner member
			if (IsVocabularySprintSession(session))
			{
				var learnerSlotNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Learner" };
				var learnerMembers = activeMembers.Where(m => learnerSlotNames.Contains(m.SlotName ?? string.Empty)).ToList();
				foreach (var learner in learnerMembers)
				{
					await _vocabularyService.SaveSessionVocabularyAsync(sessionId, learner.UserId, cancellationToken);
					vocabularyLearnerId ??= learner.UserId;
				}
			}
		}

		var summary = await _liveSessionRepository.GetSessionCompletionSummaryAsync(sessionId, cancellationToken);

		if (summary is null)
		{
			return ApiResponse<SessionSummaryResponseDto>.FailureResult(new[] { "Session summary could not be generated." }, "Session completion failed.");
		}

		// Enrich summary with vocabulary data for VocabularySprint sessions
		if (vocabularyLearnerId.HasValue)
		{
			summary.VocabularySummary = await _vocabularyService.GetSessionVocabularySummaryAsync(sessionId, vocabularyLearnerId.Value, cancellationToken);
		}

		// If the host enabled "Record Session", queue the consolidated recording merge.
		// Fire-and-await but never let recording failures break completion (method is self-guarding).
		await _sessionRecordingService.EnsureRecordingQueuedAsync(sessionId, cancellationToken);

		await ResolveAvatarUrlsAsync(summary, cancellationToken);
		return ApiResponse<SessionSummaryResponseDto>.SuccessResult(summary, "Session completed successfully.");
	}

	public async Task<ApiResponse<bool>> RequestReReadAsync(long sessionId, long userId, CancellationToken cancellationToken = default)
	{
		if (sessionId <= 0 || userId <= 0)
		{
			return ApiResponse<bool>.FailureResult(new[] { "SessionId and UserId must be greater than zero." }, "Validation failed.");
		}

		var user = await _userRepository.GetByUserIdAsync(userId, cancellationToken);
		var currentTurn = await _liveSessionRepository.GetCurrentTurnEntityAsync(sessionId, cancellationToken);
		var sessionMember = await _liveSessionRepository.GetActiveSessionMemberByUserIdAsync(sessionId, userId, cancellationToken);

		if (user is null || currentTurn is null || sessionMember is null)
		{
			return ApiResponse<bool>.FailureResult(new[] { "Current turn or session member was not found." }, "Re-read request failed.");
		}

		if (currentTurn.ReReadAllowed == false || currentTurn.ReReadCount >= currentTurn.MaxReReads)
		{
			return ApiResponse<bool>.FailureResult(new[] { "No re-reads remain for the current turn." }, "Re-read request failed.");
		}

		await _liveSessionRepository.IncrementReReadCountAsync(currentTurn.TurnStateId, user.FullName, "127.0.0.1", cancellationToken);

		return ApiResponse<bool>.SuccessResult(true, "Re-read requested successfully.");
	}

	public async Task MarkMemberLeftAsync(long sessionId, long userId, CancellationToken cancellationToken = default)
	{
		if (sessionId <= 0 || userId <= 0) return;
		try
		{
			var user = await _userRepository.GetByUserIdAsync(userId, cancellationToken);
			if (user is null) return;
			await _sessionRepository.UpdateSessionMemberLeftAsync(sessionId, userId, user.FullName, "127.0.0.1", cancellationToken);
		}
		catch
		{
			// best-effort: hub disconnect should not propagate exceptions
		}
	}

	public async Task ReactivateMemberAsync(long sessionId, long userId, CancellationToken cancellationToken = default)
	{
		if (sessionId <= 0 || userId <= 0) return;
		try
		{
			await _liveSessionRepository.ReactivateMemberAsync(sessionId, userId, cancellationToken);
		}
		catch
		{
			// best-effort: hub reconnect should not propagate exceptions to the client
		}
	}

	private async Task<(TurnStateResponseDto? Turn, string? Error)> EnsureCurrentTurnAsync(long sessionId, string requestedBy, CancellationToken cancellationToken)
	{
		var existingTurn = await _liveSessionRepository.GetCurrentTurnAsync(sessionId, cancellationToken);

		if (existingTurn is not null)
		{
			existingTurn.ActiveMemberAvatarUrl = await ResolveAvatarUrlAsync(existingTurn.ActiveMemberAvatarUrl, cancellationToken);
			return (existingTurn, null);
		}

		var session = await _sessionRepository.GetSessionBySessionIdAsync(sessionId, cancellationToken);

		if (session is null)
		{
			return (null, "Session not found.");
		}

		if (string.Equals(session.Status, SessionStatusType.ACTIVE.ToString(), StringComparison.OrdinalIgnoreCase) == false)
		{
			return (null, $"Session is not active. Current status: {session.Status}.");
		}

		return await CreateNextTurnAsync(session, 1, requestedBy, cancellationToken);
	}

	/// <summary>
	/// Resolves (but does NOT persist) the next turn for a session. Pure validation + construction
	/// so callers can verify the next turn is creatable BEFORE mutating the current turn.
	/// Returns:
	///   - NextTurn set, IsEndOfScript=false, Error=null → a valid next turn ready to insert
	///   - IsEndOfScript=true                            → the script is finished (no next turn)
	///   - Error set                                     → next turn cannot be created (surface it)
	/// </summary>
	private async Task<(TurnState? NextTurn, bool IsEndOfScript, string? Error)> ResolveNextTurnAsync(
		Session session, int nextTurnIndex, string createdBy, CancellationToken cancellationToken)
	{
		var activeMembers = await _liveSessionRepository.GetActiveSessionMembersBySessionIdAsync(session.SessionId, cancellationToken);
		var orderedUtterances = await _liveSessionRepository.GetOrderedUtterancesBySessionIdAsync(session.SessionId, cancellationToken);

		if (activeMembers.Count == 0)
		{
			return (null, false, "No active session members found.");
		}

		if (orderedUtterances.Count == 0)
		{
			return (null, false, "The script linked to this session has no utterances.");
		}

		if (nextTurnIndex > orderedUtterances.Count)
		{
			// Not an error — the script is complete. The caller decides how to finish the session.
			return (null, true, null);
		}

		var nextUtterance = orderedUtterances[nextTurnIndex - 1];
		var activeMember = activeMembers.FirstOrDefault(sessionMember =>
			string.Equals(sessionMember.SlotName.Trim(), nextUtterance.SpeakerLabel.Trim(), StringComparison.OrdinalIgnoreCase));

		if (activeMember is null)
		{
			var activeSlots = string.Join(", ", activeMembers.Select(m => $"'{m.SlotName}'"));
			return (null, false, $"No active member holds the slot '{nextUtterance.SpeakerLabel}'. Active slots: [{activeSlots}]. Check that the script speaker labels match the session slot names.");
		}

		var nextTurn = new TurnState
		{
			SessionId = session.SessionId,
			TurnIndex = nextTurnIndex,
			TotalTurns = orderedUtterances.Count,
			ActiveMemberId = activeMember.UserId,
			ActiveSlotIndex = activeMember.SlotIndex,
			UtteranceId = nextUtterance.UtteranceId,
			MaxReReads = 2,
			TurnStatus = TurnStatusType.ACTIVE.ToString(),
			CreatedBy = createdBy,
			IPAddress = "127.0.0.1"
		};

		return (nextTurn, false, null);
	}

	/// <summary>
	/// Resolves, inserts, and returns the next turn. Used by the start-session path (turn 1) where
	/// there is no prior turn to complete. The turn-shift path uses <see cref="ResolveNextTurnAsync"/>
	/// plus an atomic complete-and-advance instead.
	/// </summary>
	private async Task<(TurnStateResponseDto? Turn, string? Error)> CreateNextTurnAsync(Session session, int nextTurnIndex, string createdBy, CancellationToken cancellationToken)
	{
		var (nextTurn, isEndOfScript, error) = await ResolveNextTurnAsync(session, nextTurnIndex, createdBy, cancellationToken);

		if (error is not null)
		{
			return (null, error);
		}

		if (isEndOfScript || nextTurn is null)
		{
			return (null, $"Turn {nextTurnIndex} exceeds the total utterance count. Session may already be complete.");
		}

		await _liveSessionRepository.InsertTurnStateAsync(nextTurn, cancellationToken);

		var createdTurn = await _liveSessionRepository.GetCurrentTurnAsync(session.SessionId, cancellationToken);

		if (createdTurn is null)
		{
			return (null, "Turn was inserted but could not be retrieved. Check uspGetCurrentTurnBySessionId stored procedure.");
		}

		createdTurn.ActiveMemberAvatarUrl = await ResolveAvatarUrlAsync(createdTurn.ActiveMemberAvatarUrl, cancellationToken);
		return (createdTurn, null);
	}

	public async Task<ApiResponse<SessionReviewResponseDto>> GetSessionReviewAsync(long sessionId, long userId, CancellationToken cancellationToken = default)
	{
		if (sessionId <= 0 || userId <= 0)
		{
			return ApiResponse<SessionReviewResponseDto>.FailureResult(new[] { "SessionId and UserId must be greater than zero." }, "Validation failed.");
		}

		var review = await _liveSessionRepository.GetSessionReviewAsync(sessionId, userId, cancellationToken);

		if (review is null)
		{
			return ApiResponse<SessionReviewResponseDto>.FailureResult(new[] { "Session review was not found." }, "Session review not found.");
		}

		return ApiResponse<SessionReviewResponseDto>.SuccessResult(review, "Session review retrieved successfully.");
	}

	private static string? NormalizeFeedbackTag(string feedbackTag)
	{
		if (string.IsNullOrWhiteSpace(feedbackTag))
		{
			return null;
		}

		return FeedbackTagMap.TryGetValue(feedbackTag.Trim(), out var normalizedFeedbackTag)
			? normalizedFeedbackTag
			: null;
	}

	private static string? NormalizeNullableText(string? value)
	{
		return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
	}

	private static List<string> NormalizeStringList(IEnumerable<string> items)
	{
		return items
			.Where(item => string.IsNullOrWhiteSpace(item) == false)
			.Select(item => item.Trim())
			.ToList();
	}

	private static string? JoinCsv(IEnumerable<string> items)
	{
		var normalizedItems = NormalizeStringList(items);
		return normalizedItems.Count == 0 ? null : string.Join(",", normalizedItems);
	}

	private static string? SerializeJson<T>(T value)
	{
		return JsonSerializer.Serialize(value);
	}

	private static bool IsVocabularySprintSession(Session session)
	{
		// SessionMode matches the script category set at session creation time.
		// Accepts both canonical and legacy names.
		return string.Equals(session.SessionMode, "Vocabulary Sprint", StringComparison.OrdinalIgnoreCase)
			|| string.Equals(session.SessionMode, "Vocabulary", StringComparison.OrdinalIgnoreCase);
	}

	private static int ResolvePracticeMinutes(Session session)
	{
		if (session.ActualDurationSec is > 0)
		{
			return Math.Max(1, (int)Math.Ceiling(session.ActualDurationSec.Value / 60D));
		}

		return Math.Max(1, session.SessionDuration);
	}

	private async Task<string?> ResolveAvatarUrlAsync(string? raw, CancellationToken cancellationToken)
	{
		if (string.IsNullOrEmpty(raw)) return null;
		if (raw.StartsWith('/') || raw.StartsWith("http", StringComparison.OrdinalIgnoreCase)) return raw;

		try
		{
			return await _storageService.GetPresignedUrlAsync(
				_r2Settings.Buckets.Avatars,
				raw,
				_r2Settings.PresignedUrlExpiryMinutes.Avatars,
				cancellationToken);
		}
		catch
		{
			return null;
		}
	}

	private async Task ResolveAvatarUrlsAsync(SessionSummaryResponseDto summary, CancellationToken cancellationToken)
	{
		var bucket = _r2Settings.Buckets.Avatars;
		var expiry  = _r2Settings.PresignedUrlExpiryMinutes.Avatars;

		foreach (var score in summary.MemberScores)
		{
			var raw = score.AvatarUrl;
			if (string.IsNullOrEmpty(raw)) continue;
			if (raw.StartsWith('/') || raw.StartsWith("http", StringComparison.OrdinalIgnoreCase)) continue;

			try
			{
				score.AvatarUrl = await _storageService.GetPresignedUrlAsync(bucket, raw, expiry, cancellationToken);
			}
			catch
			{
				score.AvatarUrl = null;
			}
		}
	}
}
