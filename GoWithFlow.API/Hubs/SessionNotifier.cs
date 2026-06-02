using GoWithFlow.Application.Interfaces.Services;
using Microsoft.AspNetCore.SignalR;

namespace GoWithFlow.API.Hubs;

/// <summary>
/// Pushes invitation events to connected users via the SessionHub.
/// User-level groups follow the naming convention "user_{userId}".
/// Each user joins their own group on SessionHub connect.
/// </summary>
public sealed class SessionNotifier : ISessionNotifier
{
	private readonly IHubContext<SessionHub> _hubContext;

	public SessionNotifier(IHubContext<SessionHub> hubContext)
	{
		_hubContext = hubContext;
	}

	public Task NotifyInvitationReceivedAsync(long userId, object payload, CancellationToken cancellationToken = default)
	{
		return _hubContext.Clients
			.Group(UserGroup(userId))
			.SendAsync("INVITATION_RECEIVED", payload, cancellationToken);
	}

	public Task NotifyInvitationRespondedAsync(long hostUserId, long sessionId, object payload, CancellationToken cancellationToken = default)
	{
		return _hubContext.Clients
			.Group(SessionGroup(sessionId))
			.SendAsync("INVITATION_RESPONDED", payload, cancellationToken);
	}

	public Task NotifyInvitationCancelledAsync(long userId, object payload, CancellationToken cancellationToken = default)
	{
		return _hubContext.Clients
			.Group(UserGroup(userId))
			.SendAsync("INVITATION_CANCELLED", payload, cancellationToken);
	}

	public static string UserGroup(long userId) => $"user_{userId}";

	public static string SessionGroup(long sessionId) => $"session_{sessionId}";
}
