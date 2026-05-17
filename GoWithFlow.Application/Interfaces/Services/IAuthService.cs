using GoWithFlow.Application.Common;
using GoWithFlow.Application.DTOs.Requests;
using GoWithFlow.Application.DTOs.Responses;

namespace GoWithFlow.Application.Interfaces.Services;

public interface IAuthService
{
	Task<ApiResponse<AuthResponseDto>> LoginAsync(LoginRequestDto dto, CancellationToken cancellationToken = default);

	Task<ApiResponse<UserProfileResponseDto>> RegisterAsync(RegisterRequestDto dto, CancellationToken cancellationToken = default);

	Task<ApiResponse<AuthResponseDto>> RefreshTokenAsync(RefreshTokenRequestDto dto, CancellationToken cancellationToken = default);

	Task<ApiResponse<bool>> LogoutAsync(string refreshToken, CancellationToken cancellationToken = default);
}
