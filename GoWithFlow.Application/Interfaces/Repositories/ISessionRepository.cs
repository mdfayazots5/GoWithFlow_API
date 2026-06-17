using GoWithFlow.Application.Common;
using GoWithFlow.Application.DTOs.Responses.Session;
using GoWithFlow.Domain.Entities;

namespace GoWithFlow.Application.Interfaces.Repositories;

public interface ISessionRepository
{
	/// <summary>
	/// Creates the session and host member transactionally. When <paramref name="aiMembers"/> is
	/// non-empty (AI Voice Participant enabled), the AI config is persisted and each AI member is
	/// inserted in the SAME transaction, so the session is never left half-configured. Pass an empty
	/// list for a normal human session.
	/// </summary>
	Task<(long SessionId, string JoinCode)> CreateSessionAsync(Session session, SessionMember hostMember, IReadOnlyList<SessionMember> aiMembers, CancellationToken cancellationToken = default);

	Task JoinSessionAsync(SessionMember sessionMember, CancellationToken cancellationToken = default);

	Task<Session?> GetSessionBySessionIdAsync(long sessionId, CancellationToken cancellationToken = default);

	Task<SessionPreviewResponseDto?> GetSessionPreviewByJoinCodeAsync(string joinCode, CancellationToken cancellationToken = default);

	Task<LobbyStateResponseDto?> GetLobbyStateBySessionIdAsync(long sessionId, CancellationToken cancellationToken = default);

	Task<List<SlotInfoDto>> GetAvailableSlotsBySessionIdAsync(long sessionId, CancellationToken cancellationToken = default);

	Task UpdateSessionMemberReadyStatusAsync(long sessionId, long userId, bool isReady, string updatedBy, string ipAddress, CancellationToken cancellationToken = default);

	Task UpdateSessionStatusAsync(long sessionId, string status, string updatedBy, string ipAddress, CancellationToken cancellationToken = default);

	Task<PagedResult<SessionListItemResponseDto>> GetSessionHistoryAsync(long userId, string? statusFilter, int pageNumber, int pageSize, CancellationToken cancellationToken = default);

	Task<(bool IsValid, long SessionId, string SessionName, string Status, int CurrentMemberCount)?> ValidateJoinCodeAsync(string joinCode, CancellationToken cancellationToken = default);

	Task UpdateSessionMemberLeftAsync(long sessionId, long userId, string updatedBy, string ipAddress, CancellationToken cancellationToken = default);

	/// <summary>
	/// Returns true if the user has any member record for the session (active or inactive).
	/// Used to distinguish "never joined" from "already left" in idempotent leave logic.
	/// </summary>
	Task<bool> HasSessionMemberAsync(long sessionId, long userId, CancellationToken cancellationToken = default);

	Task<(bool Exists, string Status, bool IsExpired, int CurrentMemberCount, int MaxMembers)?> CheckJoinCodeStatusAsync(string joinCode, CancellationToken cancellationToken = default);
}
