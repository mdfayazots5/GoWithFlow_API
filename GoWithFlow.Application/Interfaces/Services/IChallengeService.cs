using GoWithFlow.Application.Common;
using GoWithFlow.Application.DTOs.Requests.User;
using GoWithFlow.Application.DTOs.Responses.User;

namespace GoWithFlow.Application.Interfaces.Services;

public interface IChallengeService
{
	Task<ApiResponse<ActiveChallengeResponseDto>> GetActiveChallengeAsync(long userId, CancellationToken cancellationToken = default);

	Task<ApiResponse<bool>> SubmitAttemptAsync(SubmitChallengeAttemptRequestDto dto, long userId, string createdBy, string ipAddress, CancellationToken cancellationToken = default);

	Task<ApiResponse<bool>> SetWeeklyChallengeAsync(SetWeeklyChallengeRequestDto dto, string createdBy, string ipAddress, CancellationToken cancellationToken = default);
}
