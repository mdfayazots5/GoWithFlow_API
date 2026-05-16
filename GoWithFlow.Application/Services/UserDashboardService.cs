using GoWithFlow.Application.Common;
using GoWithFlow.Application.DTOs.Responses.User;
using GoWithFlow.Application.Interfaces.Repositories;
using GoWithFlow.Application.Interfaces.Services;

namespace GoWithFlow.Application.Services;

public sealed class UserDashboardService : IUserDashboardService
{
	private readonly IUserRepository _userRepository;

	public UserDashboardService(IUserRepository userRepository)
	{
		_userRepository = userRepository;
	}

	public async Task<ApiResponse<UserDashboardResponseDto>> GetDashboardAsync(long userId, CancellationToken cancellationToken = default)
	{
		if (userId <= 0)
		{
			return ApiResponse<UserDashboardResponseDto>.FailureResult(new[] { "UserId must be greater than zero." }, "Validation failed.");
		}

		var dashboard = await _userRepository.GetUserDashboardAsync(userId, cancellationToken);

		if (dashboard is null)
		{
			return ApiResponse<UserDashboardResponseDto>.FailureResult(new[] { "User dashboard was not found." }, "User dashboard not found.");
		}

		return ApiResponse<UserDashboardResponseDto>.SuccessResult(dashboard, "User dashboard retrieved successfully.");
	}
}
