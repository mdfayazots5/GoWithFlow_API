using GoWithFlow.Application.Common;
using GoWithFlow.Application.DTOs.Requests.User;
using GoWithFlow.Application.DTOs.Responses.User;
using GoWithFlow.Application.Interfaces.Repositories;
using GoWithFlow.Application.Interfaces.Services;
using GoWithFlow.Domain.Entities;
using GoWithFlow.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace GoWithFlow.Application.Services;

public sealed class RepracticeService : IRepracticeService
{
	private readonly IMistakeRepository _mistakeRepository;
	private readonly IRepracticeRepository _repracticeRepository;
	private readonly IUserRepository _userRepository;
	private readonly IUserService _userService;
	private readonly ISessionRepository _sessionRepository;
	private readonly ILogger<RepracticeService> _logger;

	public RepracticeService(
		IMistakeRepository mistakeRepository,
		IRepracticeRepository repracticeRepository,
		IUserRepository userRepository,
		IUserService userService,
		ISessionRepository sessionRepository,
		ILogger<RepracticeService> logger)
	{
		_mistakeRepository = mistakeRepository;
		_repracticeRepository = repracticeRepository;
		_userRepository = userRepository;
		_userService = userService;
		_sessionRepository = sessionRepository;
		_logger = logger;
	}

	private static readonly int[] ReviewIntervals = { 1, 3, 7, 14, 30 };

	public async Task<ApiResponse<RepracticeSessionResponseDto>> GenerateRepracticeSessionAsync(GenerateRepracticeRequestDto dto, long userId, CancellationToken cancellationToken = default)
	{
		if (userId <= 0 || (!dto.IncludeAllSessions && dto.SourceSessionId <= 0))
		{
			return ApiResponse<RepracticeSessionResponseDto>.FailureResult(new[] { "SourceSessionId and UserId must be greater than zero." }, "Validation failed.");
		}

		var user = await _userRepository.GetByUserIdAsync(userId, cancellationToken);

		if (user is null)
		{
			return ApiResponse<RepracticeSessionResponseDto>.FailureResult(new[] { "User was not found." }, "Repractice generation failed.");
		}

		// "Practice All Mistakes" (IncludeAllSessions) pulls every unresolved mistake across all
		// sessions — the SP treats sessionFilter = 0 as "all sessions". Per-row practice restricts
		// to the supplied session. Without this, "Practice All" silently practiced only one session's
		// mistakes and skipped the rest (the reported "stops prematurely / skips pending items" bug).
		var sessionFilter = dto.IncludeAllSessions ? 0L : dto.SourceSessionId;

		if (!dto.IncludeAllSessions)
		{
			var sourceSession = await _sessionRepository.GetSessionBySessionIdAsync(dto.SourceSessionId, cancellationToken);
			if (sourceSession is null)
			{
				return ApiResponse<RepracticeSessionResponseDto>.FailureResult(new[] { "Source session was not found." }, "Repractice generation failed.");
			}
		}

		var mistakes = await _mistakeRepository.GetUnresolvedMistakesAsync(userId, sessionFilter, cancellationToken);

		_logger.LogInformation(
			"Repractice generation requested. UserId={UserId}, IncludeAllSessions={IncludeAllSessions}, RequestedSessionId={RequestedSessionId}, UnresolvedMistakes={MistakeCount}",
			userId, dto.IncludeAllSessions, dto.SourceSessionId, mistakes.Count);

		if (mistakes.Count == 0)
		{
			return ApiResponse<RepracticeSessionResponseDto>.FailureResult(new[] { "No unresolved mistakes were found for repractice generation." }, "Repractice generation failed.");
		}

		// tblRepracticeSession.SourceSessionId is NOT NULL with an FK to tblSession, so the stored
		// anchor must be a real session. For all-sessions mode derive it from the first loaded mistake.
		var anchorSessionId = dto.IncludeAllSessions ? mistakes[0].SessionId : dto.SourceSessionId;

		var repracticeSession = new RepracticeSession
		{
			UserId = userId,
			SourceSessionId = anchorSessionId,
			TotalMistakes = mistakes.Count,
			Status = RepracticeStatusType.PENDING.ToString(),
			CreatedBy = user.FullName,
			IPAddress = "127.0.0.1"
		};

		var repracticeSessionId = await _repracticeRepository.InsertRepracticeSessionAsync(repracticeSession, cancellationToken);

		foreach (var mistake in mistakes)
		{
			var hintText = mistake.MistakeType?.ToUpperInvariant() switch
			{
				"SPEED"         => "Speaking Speed",
				"PRONUNCIATION" => "Pronunciation",
				_               => mistake.GrammarTag
			};

			var correctionNote = mistake.MistakeType?.ToUpperInvariant() switch
			{
				"SPEED"         => "Aim for 80–120 WPM. Read sentences aloud at a natural conversational pace — not too slow, not too fast.",
				"PRONUNCIATION" => "Focus on clear articulation. Repeat the sentence slowly, then at natural speed. Pay attention to stressed syllables.",
				"INCOMPLETE"    => "Complete the full sentence without stopping. Speak confidently through to the end.",
				_               => mistake.CorrectionText ?? mistake.MistakeDetail
			};

			var repracticeUtterance = new RepracticeUtterance
			{
				RepracticeSessionId = repracticeSessionId,
				MistakeId = mistake.MistakeId,
				OriginalUtteranceId = mistake.UtteranceId,
				EnglishText = mistake.UtteranceText,
				HintText = hintText,
				MistakeType = mistake.MistakeType ?? string.Empty,
				MistakeDetail = mistake.MistakeDetail,
				CorrectionNote = correctionNote,
				CreatedBy = user.FullName,
				IPAddress = "127.0.0.1"
			};

			await _repracticeRepository.InsertRepracticeUtteranceAsync(repracticeUtterance, cancellationToken);
		}

		var response = await _repracticeRepository.GetRepracticeSessionByIdAsync(repracticeSessionId, cancellationToken);

		if (response is null)
		{
			return ApiResponse<RepracticeSessionResponseDto>.FailureResult(new[] { "Generated repractice session could not be retrieved." }, "Repractice generation failed.");
		}

		return ApiResponse<RepracticeSessionResponseDto>.SuccessResult(response, "Repractice session generated successfully.");
	}

	public async Task<ApiResponse<RepracticeSessionResponseDto>> GetRepracticeSessionAsync(long repracticeSessionId, long userId, CancellationToken cancellationToken = default)
	{
		if (repracticeSessionId <= 0 || userId <= 0)
		{
			return ApiResponse<RepracticeSessionResponseDto>.FailureResult(new[] { "RepracticeSessionId and UserId must be greater than zero." }, "Validation failed.");
		}

		var repracticeSession = await _repracticeRepository.GetRepracticeSessionEntityAsync(repracticeSessionId, cancellationToken);

		if (repracticeSession is null)
		{
			return ApiResponse<RepracticeSessionResponseDto>.FailureResult(new[] { "Repractice session was not found." }, "Repractice session not found.");
		}

		if (repracticeSession.UserId != userId)
		{
			return ApiResponse<RepracticeSessionResponseDto>.FailureResult(new[] { "This repractice session does not belong to the current user." }, "Repractice session not found.");
		}

		var response = await _repracticeRepository.GetRepracticeSessionByIdAsync(repracticeSessionId, cancellationToken);

		if (response is null)
		{
			return ApiResponse<RepracticeSessionResponseDto>.FailureResult(new[] { "Repractice session was not found." }, "Repractice session not found.");
		}

		return ApiResponse<RepracticeSessionResponseDto>.SuccessResult(response, "Repractice session retrieved successfully.");
	}

	public async Task<ApiResponse<PagedResult<RepracticeSessionResponseDto>>> GetRepracticeHistoryAsync(long userId, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
	{
		if (userId <= 0 || pageNumber <= 0 || pageSize <= 0)
		{
			return ApiResponse<PagedResult<RepracticeSessionResponseDto>>.FailureResult(new[] { "UserId, PageNumber, and PageSize must be greater than zero." }, "Validation failed.");
		}

		var result = await _repracticeRepository.GetRepracticeHistoryAsync(userId, pageNumber, pageSize, cancellationToken);

		return ApiResponse<PagedResult<RepracticeSessionResponseDto>>.SuccessResult(result, "Repractice history retrieved successfully.");
	}

	public async Task<ApiResponse<UpdateAttemptResponseDto>> UpdateAttemptAsync(UpdateAttemptRequestDto dto, long userId, CancellationToken cancellationToken = default)
	{
		if (dto.RepracticeUtteranceId <= 0 || dto.Score < 0 || dto.Score > 100 || userId <= 0)
		{
			return ApiResponse<UpdateAttemptResponseDto>.FailureResult(new[] { "RepracticeUtteranceId, Score, and UserId are invalid." }, "Validation failed.");
		}

		var user = await _userRepository.GetByUserIdAsync(userId, cancellationToken);
		var repracticeUtterance = await _repracticeRepository.GetRepracticeUtteranceEntityAsync(dto.RepracticeUtteranceId, cancellationToken);

		if (user is null || repracticeUtterance?.RepracticeSession is null)
		{
			return ApiResponse<UpdateAttemptResponseDto>.FailureResult(new[] { "User or repractice utterance was not found." }, "Repractice attempt failed.");
		}

		if (repracticeUtterance.RepracticeSession.UserId != userId)
		{
			return ApiResponse<UpdateAttemptResponseDto>.FailureResult(new[] { "This repractice utterance does not belong to the current user." }, "Repractice attempt failed.");
		}

		await _repracticeRepository.UpdateRepracticeUtteranceAttemptAsync(dto.RepracticeUtteranceId, dto.Score, user.FullName, "127.0.0.1", cancellationToken);

		// Re-read the authoritative post-update state so the client reflects server-side resolution
		// (resolves after two consecutive scores > 80) instead of guessing locally.
		var updated = await _repracticeRepository.GetRepracticeUtteranceEntityAsync(dto.RepracticeUtteranceId, cancellationToken);

		var result = new UpdateAttemptResponseDto
		{
			RepracticeUtteranceId = dto.RepracticeUtteranceId,
			IsResolved   = updated?.IsResolved ?? false,
			AttemptCount = updated?.AttemptCount ?? 0,
			BestScore    = updated?.BestScore ?? 0m,
			LastScore    = updated?.LastScore ?? dto.Score
		};

		_logger.LogInformation(
			"Repractice attempt recorded. UserId={UserId}, RepracticeUtteranceId={UtteranceId}, Score={Score}, AttemptCount={AttemptCount}, IsResolved={IsResolved}",
			userId, dto.RepracticeUtteranceId, dto.Score, result.AttemptCount, result.IsResolved);

		return ApiResponse<UpdateAttemptResponseDto>.SuccessResult(result, "Repractice attempt updated successfully.");
	}

	public async Task<ApiResponse<CompleteRepracticeResponseDto>> CompleteRepracticeSessionAsync(long repracticeSessionId, long userId, CancellationToken cancellationToken = default)
	{
		if (repracticeSessionId <= 0 || userId <= 0)
		{
			return ApiResponse<CompleteRepracticeResponseDto>.FailureResult(new[] { "RepracticeSessionId and UserId must be greater than zero." }, "Validation failed.");
		}

		var user = await _userRepository.GetByUserIdAsync(userId, cancellationToken);
		var repracticeSession = await _repracticeRepository.GetRepracticeSessionEntityAsync(repracticeSessionId, cancellationToken);

		if (user is null || repracticeSession is null)
		{
			return ApiResponse<CompleteRepracticeResponseDto>.FailureResult(new[] { "User or repractice session was not found." }, "Repractice completion failed.");
		}

		if (repracticeSession.UserId != userId)
		{
			return ApiResponse<CompleteRepracticeResponseDto>.FailureResult(new[] { "This repractice session does not belong to the current user." }, "Repractice completion failed.");
		}

		if (string.Equals(repracticeSession.Status, RepracticeStatusType.COMPLETED.ToString(), StringComparison.OrdinalIgnoreCase))
		{
			var existing = new CompleteRepracticeResponseDto
			{
				ImprovementPercent = repracticeSession.ImprovementPercent,
				ResolvedCount      = repracticeSession.Utterances?.Count(u => u.IsResolved && !u.IsDeleted) ?? 0
			};
			return ApiResponse<CompleteRepracticeResponseDto>.SuccessResult(existing, "Repractice session already completed.");
		}

		var improvementPercent = await _repracticeRepository.CalculateImprovementPercentageAsync(userId, cancellationToken);

		await _repracticeRepository.UpdateRepracticeSessionStatusAsync(
			repracticeSessionId,
			RepracticeStatusType.COMPLETED.ToString(),
			improvementPercent,
			user.FullName,
			"127.0.0.1",
			cancellationToken);

		await _userService.CheckAndAwardBadgesAsync(userId, cancellationToken);

		var resolvedMistakeIds = await _repracticeRepository.GetResolvedMistakeIdsBySessionAsync(repracticeSessionId, cancellationToken);
		foreach (var mistakeId in resolvedMistakeIds)
		{
			await _mistakeRepository.ScheduleMistakeReviewAsync(mistakeId, user.FullName, "127.0.0.1", cancellationToken);
		}

		var result = new CompleteRepracticeResponseDto
		{
			ImprovementPercent = improvementPercent,
			ResolvedCount      = resolvedMistakeIds.Count
		};

		return ApiResponse<CompleteRepracticeResponseDto>.SuccessResult(result, "Repractice session completed successfully.");
	}

	public async Task<ApiResponse<decimal>> GetImprovementPercentageAsync(long userId, CancellationToken cancellationToken = default)
	{
		if (userId <= 0)
		{
			return ApiResponse<decimal>.FailureResult(new[] { "UserId must be greater than zero." }, "Validation failed.");
		}

		var result = await _repracticeRepository.CalculateImprovementPercentageAsync(userId, cancellationToken);

		return ApiResponse<decimal>.SuccessResult(result, "Improvement percentage retrieved successfully.");
	}
}
