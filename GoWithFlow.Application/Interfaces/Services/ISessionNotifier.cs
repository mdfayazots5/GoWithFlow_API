namespace GoWithFlow.Application.Interfaces.Services;

public interface ISessionNotifier
{
	Task NotifyInvitationReceivedAsync(long userId, object payload, CancellationToken cancellationToken = default);

	Task NotifyInvitationRespondedAsync(long hostUserId, long sessionId, object payload, CancellationToken cancellationToken = default);

	Task NotifyInvitationCancelledAsync(long userId, object payload, CancellationToken cancellationToken = default);

	Task NotifyMemberReadyAsync(long sessionId, long userId, bool isReady, CancellationToken cancellationToken = default);
}
