using GoWithFlow.Application.Common;
using GoWithFlow.Application.DTOs.Requests.Session;
using GoWithFlow.Application.DTOs.Responses.Session;
using GoWithFlow.Application.DTOs.Responses.User;

namespace GoWithFlow.Application.Interfaces.Services;

public interface ISessionInvitationService
{
	Task<ApiResponse<List<SessionInvitationDto>>> SendInvitationsAsync(SendInvitationsRequestDto dto, long hostUserId, CancellationToken cancellationToken = default);

	Task<ApiResponse<bool>> RespondToInvitationAsync(long invitationId, RespondToInvitationRequestDto dto, long userId, CancellationToken cancellationToken = default);

	Task<ApiResponse<bool>> CancelInvitationAsync(long invitationId, long sessionId, long hostUserId, CancellationToken cancellationToken = default);

	Task<ApiResponse<List<SessionInvitationDto>>> GetSessionInvitationsAsync(long sessionId, long hostUserId, CancellationToken cancellationToken = default);

	Task<ApiResponse<List<UserInvitationDto>>> GetMyInvitationsAsync(long userId, CancellationToken cancellationToken = default);

	Task<ApiResponse<List<UserSearchResultDto>>> SearchUsersAsync(string searchTerm, long excludeUserId, CancellationToken cancellationToken = default);
}
