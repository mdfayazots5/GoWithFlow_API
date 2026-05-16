using GoWithFlow.Application.Common;
using GoWithFlow.Application.DTOs.Responses.User;

namespace GoWithFlow.Application.Interfaces.Services;

public interface IUserDashboardService
{
	Task<ApiResponse<UserDashboardResponseDto>> GetDashboardAsync(long userId, CancellationToken cancellationToken = default);
}
