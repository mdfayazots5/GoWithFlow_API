using GoWithFlow.Application.Common;
using GoWithFlow.Application.DTOs.Requests.User;
using GoWithFlow.Application.DTOs.Responses.User;
using GoWithFlow.Application.Interfaces.Repositories;
using GoWithFlow.Application.Interfaces.Services;
using GoWithFlow.Domain.Entities;
using GoWithFlow.Domain.Enums;

namespace GoWithFlow.Application.Services;

public sealed class RepracticeService : IRepracticeService
{
	private readonly IMistakeRepository _mistakeRepository;
	private readonly IRepracticeRepository _repracticeRepository;
	private readonly IUserRepository _userRepository;
	private readonly IUserService _userService;
	private readonly ISessionRepository _sessionRepository;

	public RepracticeService(
		IMistakeRepository mistakeRepository,
		IRepracticeRepository repracticeRepository,
		IUserRepository userRepository,
		IUserService userService,
		ISessionRepository sessionRepository)
	{
		_mistakeRepository = mistakeRepository;
		_repracticeRepository = repracticeRepository;
		_userRepository = userRepository;
		_userService = userService;
		_sessionRepository = sessionRepository;
	}

	public async Task<ApiResponse<RepracticeSessionResponseDto>> GenerateRepracticeSessionAsync(GenerateRepracticeRequestDto dto, long userId, CancellationToken cancellationToken = default)
	{
		if (dto.SourceSessionId <= 0 || userId <= 0)
		{
			return ApiResponse<RepracticeSessionResponseDto>.FailureResult(new[] { "SourceSessionId and UserId must be greater than zero." }, "Validation failed.");
		}

		var user = await _userRepository.GetByUserIdAsync(userId, cancellationToken);
		var sourceSession = await _sessionRepository.GetSessionBySessionIdAsync(dto.SourceSessionId, cancellationToken);

		if (user is null || sourceSession is null)
		{
			return ApiResponse<RepracticeSessionResponseDto>.FailureResult(new[] { "User or source session was not found." }, "Repractice generation failed.");
		}

		var mistakes = await _mistakeRepository.GetUnresolvedMistakesAsync(userId, dto.SourceSessionId, cancellationToken);

		if (mistakes.Count == 0)
		{
			return ApiResponse<RepracticeSessionResponseDto>.FailureResult(new[] { "No unresolved mistakes were found for repractice generation." }, "Repractice generation failed.");
		}

		var repracticeSession = new RepracticeSession
		{
			UserId = userId,
			SourceSessionId = dto.SourceSessionId,
			TotalMistakes = mistakes.Count,
			Status = RepracticeStatusType.PENDING.ToString(),
			CreatedBy = user.FullName,
			IPAddress = "127.0.0.1"
		};

		var repracticeSessionId = await _repracticeRepository.InsertRepracticeSessionAsync(repracticeSession, cancellationToken);

		foreach (var mistake in mistakes)
		{
			var repracticeUtterance = new RepracticeUtterance
			{
				RepracticeSessionId = repracticeSessionId,
				MistakeId = mistake.MistakeId,
				OriginalUtteranceId = mistake.UtteranceId,
				EnglishText = mistake.UtteranceText,
				HintText = mistake.GrammarTag,
				MistakeType = mistake.MistakeType,
				MistakeDetail = mistake.MistakeDetail,
				CorrectionNote = mistake.CorrectionText ?? mistake.MistakeDetail,
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

	public async Task<ApiResponse<bool>> UpdateAttemptAsync(UpdateAttemptRequestDto dto, long userId, CancellationToken cancellationToken = default)
	{
		if (dto.RepracticeUtteranceId <= 0 || dto.Score < 0 || dto.Score > 100 || userId <= 0)
		{
			return ApiResponse<bool>.FailureResult(new[] { "RepracticeUtteranceId, Score, and UserId are invalid." }, "Validation failed.");
		}

		var user = await _userRepository.GetByUserIdAsync(userId, cancellationToken);
		var repracticeUtterance = await _repracticeRepository.GetRepracticeUtteranceEntityAsync(dto.RepracticeUtteranceId, cancellationToken);

		if (user is null || repracticeUtterance?.RepracticeSession is null)
		{
			return ApiResponse<bool>.FailureResult(new[] { "User or repractice utterance was not found." }, "Repractice attempt failed.");
		}

		if (repracticeUtterance.RepracticeSession.UserId != userId)
		{
			return ApiResponse<bool>.FailureResult(new[] { "This repractice utterance does not belong to the current user." }, "Repractice attempt failed.");
		}

		await _repracticeRepository.UpdateRepracticeUtteranceAttemptAsync(dto.RepracticeUtteranceId, dto.Score, user.FullName, "127.0.0.1", cancellationToken);

		return ApiResponse<bool>.SuccessResult(true, "Repractice attempt updated successfully.");
	}

	public async Task<ApiResponse<bool>> CompleteRepracticeSessionAsync(long repracticeSessionId, long userId, CancellationToken cancellationToken = default)
	{
		if (repracticeSessionId <= 0 || userId <= 0)
		{
			return ApiResponse<bool>.FailureResult(new[] { "RepracticeSessionId and UserId must be greater than zero." }, "Validation failed.");
		}

		var user = await _userRepository.GetByUserIdAsync(userId, cancellationToken);
		var repracticeSession = await _repracticeRepository.GetRepracticeSessionEntityAsync(repracticeSessionId, cancellationToken);

		if (user is null || repracticeSession is null)
		{
			return ApiResponse<bool>.FailureResult(new[] { "User or repractice session was not found." }, "Repractice completion failed.");
		}

		if (repracticeSession.UserId != userId)
		{
			return ApiResponse<bool>.FailureResult(new[] { "This repractice session does not belong to the current user." }, "Repractice completion failed.");
		}

		if (string.Equals(repracticeSession.Status, RepracticeStatusType.COMPLETED.ToString(), StringComparison.OrdinalIgnoreCase))
		{
			return ApiResponse<bool>.SuccessResult(true, "Repractice session already completed.");
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

		return ApiResponse<bool>.SuccessResult(true, "Repractice session completed successfully.");
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
