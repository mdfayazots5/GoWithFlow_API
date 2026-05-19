using System.Text.Json;
using GoWithFlow.Application.Common;
using GoWithFlow.Application.DTOs.Requests.LiveSession;
using GoWithFlow.Application.DTOs.Responses.LiveSession;
using GoWithFlow.Application.Interfaces.Repositories;
using GoWithFlow.Application.Interfaces.Services;
using GoWithFlow.Domain.Entities;
using GoWithFlow.Domain.Enums;

namespace GoWithFlow.Application.Services;

public sealed class LiveSessionService : ILiveSessionService
{
	private static readonly Dictionary<string, string> FeedbackTagMap = new(StringComparer.OrdinalIgnoreCase)
	{
		["Good"] = "Good",
		["Hesitated"] = "Hesitated",
		["Mistake"] = "Mistake",
		["Unclear Pronunciation"] = "Unclear Pronunciation",
		["UnclearPronunciation"] = "Unclear Pronunciation"
	};

	private readonly IUserRepository _userRepository;
	private readonly ISessionRepository _sessionRepository;
	private readonly ILiveSessionRepository _liveSessionRepository;
	private readonly IUserService _userService;
	private readonly IMistakeService _mistakeService;

	public LiveSessionService(
		IUserRepository userRepository,
		ISessionRepository sessionRepository,
		ILiveSessionRepository liveSessionRepository,
		IUserService userService,
		IMistakeService mistakeService)
	{
		_userRepository = userRepository;
		_sessionRepository = sessionRepository;
		_liveSessionRepository = liveSessionRepository;
		_userService = userService;
		_mistakeService = mistakeService;
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

		await _liveSessionRepository.UpdateTurnStatusAsync(
			currentTurnEntity.TurnStateId,
			TurnStatusType.COMPLETED.ToString(),
			user.FullName,
			"127.0.0.1",
			cancellationToken);

		var (nextTurn, nextTurnError) = await CreateNextTurnAsync(session, currentTurnEntity.TurnIndex + 1, user.FullName, cancellationToken);

		if (nextTurn is null)
		{
			return ApiResponse<TurnStateResponseDto>.FailureResult(new[] { nextTurnError ?? "No further turns remain in this session. Complete the session." }, "Turn shift completed.");
		}

		return ApiResponse<TurnStateResponseDto>.SuccessResult(nextTurn, "Turn shifted successfully.");
	}

	public async Task<ApiResponse<VoiceAnalysisResponseDto>> SaveVoiceAnalysisAsync(SaveVoiceAnalysisRequestDto dto, long userId, CancellationToken cancellationToken = default)
	{
		if (dto.SessionId <= 0 || dto.TurnIndex <= 0 || dto.UtteranceId <= 0 || userId <= 0)
		{
			return ApiResponse<VoiceAnalysisResponseDto>.FailureResult(new[] { "SessionId, TurnIndex, UtteranceId, and UserId must be greater than zero." }, "Validation failed.");
		}

		var user = await _userRepository.GetByUserIdAsync(userId, cancellationToken);
		var sessionMember = await _liveSessionRepository.GetActiveSessionMemberByUserIdAsync(dto.SessionId, userId, cancellationToken);
		var turnState = await _liveSessionRepository.GetTurnBySessionAndTurnIndexAsync(dto.SessionId, dto.TurnIndex, cancellationToken);

		if (user is null || sessionMember is null || turnState is null)
		{
			return ApiResponse<VoiceAnalysisResponseDto>.FailureResult(new[] { "Session member or turn was not found." }, "Voice analysis save failed.");
		}

		if (turnState.ActiveMemberId != userId || turnState.UtteranceId != dto.UtteranceId)
		{
			return ApiResponse<VoiceAnalysisResponseDto>.FailureResult(new[] { "Voice analysis can only be saved for the caller's active turn." }, "Voice analysis save failed.");
		}

		var alreadyExists = await _liveSessionRepository.VoiceAnalysisExistsAsync(dto.SessionId, userId, dto.TurnIndex, cancellationToken);

		if (alreadyExists)
		{
			return ApiResponse<VoiceAnalysisResponseDto>.FailureResult(new[] { "Voice analysis already exists for this user and turn." }, "Voice analysis save failed.");
		}

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

		var voiceAnalysisId = await _liveSessionRepository.InsertVoiceAnalysisAsync(voiceAnalysis, cancellationToken);

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
			"Voice analysis saved successfully.");
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

			return ApiResponse<SessionSummaryResponseDto>.SuccessResult(existingSummary, "Session completed successfully.");
		}

		var currentTurn = await _liveSessionRepository.GetCurrentTurnEntityAsync(sessionId, cancellationToken);

		if (currentTurn is not null)
		{
			await _liveSessionRepository.UpdateTurnStatusAsync(currentTurn.TurnStateId, TurnStatusType.COMPLETED.ToString(), "System", "127.0.0.1", cancellationToken);
		}

		await _sessionRepository.UpdateSessionStatusAsync(sessionId, SessionStatusType.COMPLETED.ToString(), "System", "127.0.0.1", cancellationToken);

		var completedSession = await _sessionRepository.GetSessionBySessionIdAsync(sessionId, cancellationToken);

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
		}

		var summary = await _liveSessionRepository.GetSessionCompletionSummaryAsync(sessionId, cancellationToken);

		if (summary is null)
		{
			return ApiResponse<SessionSummaryResponseDto>.FailureResult(new[] { "Session summary could not be generated." }, "Session completion failed.");
		}

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

	private async Task<(TurnStateResponseDto? Turn, string? Error)> EnsureCurrentTurnAsync(long sessionId, string requestedBy, CancellationToken cancellationToken)
	{
		var existingTurn = await _liveSessionRepository.GetCurrentTurnAsync(sessionId, cancellationToken);

		if (existingTurn is not null)
		{
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

	private async Task<(TurnStateResponseDto? Turn, string? Error)> CreateNextTurnAsync(Session session, int nextTurnIndex, string createdBy, CancellationToken cancellationToken)
	{
		var activeMembers = await _liveSessionRepository.GetActiveSessionMembersBySessionIdAsync(session.SessionId, cancellationToken);
		var orderedUtterances = await _liveSessionRepository.GetOrderedUtterancesBySessionIdAsync(session.SessionId, cancellationToken);

		if (activeMembers.Count == 0)
		{
			return (null, "No active session members found.");
		}

		if (orderedUtterances.Count == 0)
		{
			return (null, "The script linked to this session has no utterances.");
		}

		if (nextTurnIndex > orderedUtterances.Count)
		{
			return (null, $"Turn {nextTurnIndex} exceeds the total utterance count ({orderedUtterances.Count}). Session may already be complete.");
		}

		var nextUtterance = orderedUtterances[nextTurnIndex - 1];
		var activeMember = activeMembers.FirstOrDefault(sessionMember =>
			string.Equals(sessionMember.SlotName.Trim(), nextUtterance.SpeakerLabel.Trim(), StringComparison.OrdinalIgnoreCase));

		if (activeMember is null)
		{
			var activeSlots = string.Join(", ", activeMembers.Select(m => $"'{m.SlotName}'"));
			return (null, $"No active member holds the slot '{nextUtterance.SpeakerLabel}'. Active slots: [{activeSlots}]. Check that the script speaker labels match the session slot names.");
		}

		await _liveSessionRepository.InsertTurnStateAsync(
			new TurnState
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
			},
			cancellationToken);

		var createdTurn = await _liveSessionRepository.GetCurrentTurnAsync(session.SessionId, cancellationToken);

		if (createdTurn is null)
		{
			return (null, "Turn was inserted but could not be retrieved. Check uspGetCurrentTurnBySessionId stored procedure.");
		}

		return (createdTurn, null);
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

	private static int ResolvePracticeMinutes(Session session)
	{
		if (session.ActualDurationSec is > 0)
		{
			return Math.Max(1, (int)Math.Ceiling(session.ActualDurationSec.Value / 60D));
		}

		return Math.Max(1, session.SessionDuration);
	}
}
