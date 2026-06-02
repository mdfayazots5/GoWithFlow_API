using GoWithFlow.Application.Common;
using GoWithFlow.Application.DTOs.Requests.User;
using GoWithFlow.Application.DTOs.Responses.User;
using GoWithFlow.Application.Interfaces.Repositories;
using GoWithFlow.Application.Interfaces.Services;

namespace GoWithFlow.Application.Services;

public sealed class ChallengeService : IChallengeService
{
	private readonly IChallengeRepository _challengeRepository;
	private readonly IUserRepository _userRepository;

	public ChallengeService(IChallengeRepository challengeRepository, IUserRepository userRepository)
	{
		_challengeRepository = challengeRepository;
		_userRepository = userRepository;
	}

	public async Task<ApiResponse<ActiveChallengeResponseDto>> GetActiveChallengeAsync(long userId, CancellationToken cancellationToken = default)
	{
		if (userId <= 0)
		{
			return ApiResponse<ActiveChallengeResponseDto>.FailureResult(new[] { "UserId must be greater than zero." }, "Validation failed.");
		}

		var result = await _challengeRepository.GetActiveChallengeAsync(userId, cancellationToken);
		return ApiResponse<ActiveChallengeResponseDto>.SuccessResult(result, "Active challenge retrieved successfully.");
	}

	public async Task<ApiResponse<bool>> SubmitAttemptAsync(SubmitChallengeAttemptRequestDto dto, long userId, string createdBy, string ipAddress, CancellationToken cancellationToken = default)
	{
		if (dto.ChallengeId <= 0 || userId <= 0 || dto.Score < 0 || dto.Score > 100)
		{
			return ApiResponse<bool>.FailureResult(new[] { "Invalid challenge attempt data." }, "Validation failed.");
		}

		await _challengeRepository.InsertChallengeAttemptAsync(dto.ChallengeId, userId, dto.Score, createdBy, ipAddress, cancellationToken);
		return ApiResponse<bool>.SuccessResult(true, "Challenge attempt submitted successfully.");
	}

	public async Task<ApiResponse<bool>> SetWeeklyChallengeAsync(SetWeeklyChallengeRequestDto dto, string createdBy, string ipAddress, CancellationToken cancellationToken = default)
	{
		if (dto.ScriptId <= 0)
		{
			return ApiResponse<bool>.FailureResult(new[] { "ScriptId must be greater than zero." }, "Validation failed.");
		}

		await _challengeRepository.SetWeeklyChallengeAsync(dto.ScriptId, createdBy, ipAddress, cancellationToken);
		return ApiResponse<bool>.SuccessResult(true, "Weekly challenge set successfully.");
	}
}
