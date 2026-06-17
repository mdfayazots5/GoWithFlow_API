using System.Collections.Concurrent;
using GoWithFlow.Application.Interfaces.Services;
using Microsoft.AspNetCore.SignalR;

namespace GoWithFlow.API.Hubs;

public interface ILiveSessionReconnectTracker
{
    void ScheduleLeave(long sessionId, long userId, byte? slotIndex, string? name, string groupName);
    bool CancelPendingLeave(long sessionId, long userId);
}

/// <summary>
/// Defers a live-session member's MEMBER_LEFT broadcast + IsActive=0 update by a short
/// grace window so a transient WebSocket drop (mobile network handoff, app backgrounded,
/// WebView/page reconnect, doze) does NOT make other participants see the member as having
/// "left" while they are reconnecting. This mirrors <see cref="LobbyReconnectTracker"/>,
/// which solved the identical problem for the lobby hub. If the member reconnects within the
/// window, <see cref="CancelPendingLeave"/> (called from LiveSessionHub.OnConnectedAsync)
/// cancels the pending leave and no MEMBER_LEFT is ever sent.
/// </summary>
public sealed class LiveSessionReconnectTracker : ILiveSessionReconnectTracker
{
    private static readonly TimeSpan GracePeriod = TimeSpan.FromSeconds(20);

    private readonly ConcurrentDictionary<string, CancellationTokenSource> _pending = new();
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHubContext<LiveSessionHub> _hubContext;
    private readonly ILogger<LiveSessionReconnectTracker> _logger;

    public LiveSessionReconnectTracker(
        IServiceScopeFactory scopeFactory,
        IHubContext<LiveSessionHub> hubContext,
        ILogger<LiveSessionReconnectTracker> logger)
    {
        _scopeFactory = scopeFactory;
        _hubContext = hubContext;
        _logger = logger;
    }

    private static string Key(long sessionId, long userId) => $"{sessionId}:{userId}";

    public void ScheduleLeave(long sessionId, long userId, byte? slotIndex, string? name, string groupName)
    {
        var key = Key(sessionId, userId);

        // Replace any existing pending leave (e.g. a rapid reconnect/disconnect flap).
        if (_pending.TryRemove(key, out var existing))
        {
            existing.Cancel();
            existing.Dispose();
        }

        var cts = new CancellationTokenSource();
        _pending[key] = cts;
        var token = cts.Token;

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(GracePeriod, token);

                // Grace period elapsed without a reconnect — the member is genuinely gone.
                if (!_pending.TryRemove(key, out _))
                {
                    return;
                }

                await _hubContext.Clients.Group(groupName).SendAsync(
                    "MEMBER_LEFT",
                    new
                    {
                        userId,
                        name = name ?? "A participant",
                        slotIndex
                    });

                await using var scope = _scopeFactory.CreateAsyncScope();
                var liveSessionService = scope.ServiceProvider.GetRequiredService<ILiveSessionService>();
                await liveSessionService.MarkMemberLeftAsync(sessionId, userId);

                _logger.LogInformation(
                    "Live session grace window expired. Member {UserId} marked left in session {SessionId}.",
                    userId, sessionId);
            }
            catch (OperationCanceledException)
            {
                // Reconnected within grace period — leave cancelled.
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Live session grace window leave failed. UserId {UserId}, SessionId {SessionId}.",
                    userId, sessionId);
            }
        });
    }

    public bool CancelPendingLeave(long sessionId, long userId)
    {
        var key = Key(sessionId, userId);

        if (_pending.TryRemove(key, out var cts))
        {
            cts.Cancel();
            cts.Dispose();
            _logger.LogInformation(
                "Live session grace window cancelled (reconnected). UserId {UserId}, SessionId {SessionId}.",
                userId, sessionId);
            return true;
        }

        return false;
    }
}
