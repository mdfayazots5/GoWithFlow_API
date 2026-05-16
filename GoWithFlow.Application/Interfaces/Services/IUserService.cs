using GoWithFlow.Application.Common;
using GoWithFlow.Application.DTOs.Requests.User;
using GoWithFlow.Application.DTOs.Responses;
using GoWithFlow.Application.DTOs.Responses.User;
using Microsoft.AspNetCore.Http;

namespace GoWithFlow.Application.Interfaces.Services;

public interface IUserService
{
	Task<ApiResponse<UserProfileResponseDto>> GetProfileAsync(long userId, CancellationToken cancellationToken = default);

	Task<ApiResponse<UserProfileResponseDto>> UpdateProfileAsync(long userId, UpdateProfileRequestDto dto, CancellationToken cancellationToken = default);

	Task<ApiResponse<string>> UploadAvatarAsync(long userId, IFormFile file, CancellationToken cancellationToken = default);

	Task<ApiResponse<SessionDetailResponseDto>> GetSessionDetailAsync(long sessionId, long userId, CancellationToken cancellationToken = default);

	Task<ApiResponse<ImprovementDataResponseDto>> GetImprovementDataAsync(long userId, CancellationToken cancellationToken = default);

	Task<ApiResponse<StreakDataResponseDto>> GetStreakDataAsync(long userId, CancellationToken cancellationToken = default);

	Task<ApiResponse<List<UserBadgeDto>>> GetBadgesAsync(long userId, CancellationToken cancellationToken = default);

	Task UpsertStreakAsync(long userId, int practiceMinutes, CancellationToken cancellationToken = default);

	Task CheckAndAwardBadgesAsync(long userId, CancellationToken cancellationToken = default);
}
