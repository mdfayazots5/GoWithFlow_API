using GoWithFlow.Application.Common;
using GoWithFlow.Application.DTOs.Requests.Session;
using GoWithFlow.Application.DTOs.Responses.Session;

namespace GoWithFlow.Application.Interfaces.Services;

public interface ISessionService
{
	Task<ApiResponse<CreateSessionResponseDto>> CreateSessionAsync(CreateSessionRequestDto dto, long hostUserId, CancellationToken cancellationToken = default);

	Task<ApiResponse<SessionPreviewResponseDto>> ValidateJoinCodeAsync(string joinCode, CancellationToken cancellationToken = default);

	Task<ApiResponse<LobbyStateResponseDto>> JoinSessionAsync(JoinSessionRequestDto dto, long userId, CancellationToken cancellationToken = default);

	Task<ApiResponse<LobbyStateResponseDto>> GetLobbyStateAsync(long sessionId, CancellationToken cancellationToken = default);

	Task<ApiResponse<bool>> UpdateReadyStatusAsync(UpdateReadyStatusRequestDto dto, long userId, CancellationToken cancellationToken = default);

	Task<ApiResponse<bool>> StartSessionAsync(long sessionId, long hostUserId, CancellationToken cancellationToken = default);

	Task<ApiResponse<bool>> EndSessionAsync(long sessionId, CancellationToken cancellationToken = default);

	Task<ApiResponse<PagedResult<SessionListItemResponseDto>>> GetSessionHistoryAsync(long userId, string? statusFilter, int pageNumber, int pageSize, CancellationToken cancellationToken = default);

	Task<ApiResponse<bool>> LeaveSessionAsync(long sessionId, long userId, CancellationToken cancellationToken = default);
}
