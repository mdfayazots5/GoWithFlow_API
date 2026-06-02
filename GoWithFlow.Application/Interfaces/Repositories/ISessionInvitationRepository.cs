using GoWithFlow.Application.DTOs.Responses.Session;

namespace GoWithFlow.Application.Interfaces.Repositories;

public interface ISessionInvitationRepository
{
	Task<long> InsertInvitationAsync(long sessionId, long userId, byte slotIndex, string slotName, DateTime? expiresAt, string createdBy, string ipAddress, CancellationToken cancellationToken = default);

	Task UpdateInvitationStatusAsync(long invitationId, long userId, string status, string updatedBy, string ipAddress, CancellationToken cancellationToken = default);

	Task CancelInvitationAsync(long invitationId, long sessionId, string updatedBy, CancellationToken cancellationToken = default);

	Task<List<SessionInvitationDto>> GetInvitationsBySessionIdAsync(long sessionId, CancellationToken cancellationToken = default);

	Task<List<UserInvitationDto>> GetPendingInvitationsByUserIdAsync(long userId, CancellationToken cancellationToken = default);

	Task<(long InvitationId, long SessionId, long UserId, byte SlotIndex, string SlotName, string Status)?> GetInvitationByIdAsync(long invitationId, CancellationToken cancellationToken = default);
}
