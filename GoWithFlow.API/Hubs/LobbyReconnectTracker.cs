using System.Collections.Concurrent;
using GoWithFlow.Application.Interfaces.Services;
using Microsoft.AspNetCore.SignalR;

namespace GoWithFlow.API.Hubs;

public interface ILobbyReconnectTracker
{
    void ScheduleLeave(long sessionId, long userId, byte slotIndex, string groupName);
    bool CancelPendingLeave(long sessionId, long userId);
}

public sealed class LobbyReconnectTracker : ILobbyReconnectTracker
{
    private static readonly TimeSpan GracePeriod = TimeSpan.FromSeconds(20);

    private readonly ConcurrentDictionary<string, CancellationTokenSource> _pending = new();
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHubContext<SessionHub> _hubContext;
    private readonly ILogger<LobbyReconnectTracker> _logger;

    public LobbyReconnectTracker(
        IServiceScopeFactory scopeFactory,
        IHubContext<SessionHub> hubContext,
        ILogger<LobbyReconnectTracker> logger)
    {
        _scopeFactory = scopeFactory;
        _hubContext = hubContext;
        _logger = logger;
    }

    private static string Key(long sessionId, long userId) => $"{sessionId}:{userId}";

    public void ScheduleLeave(long sessionId, long userId, byte slotIndex, string groupName)
    {
        var key = Key(sessionId, userId);

        // Replace any existing pending leave (e.g. rapid reload)
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

                // Grace period elapsed without a reconnect
                if (!_pending.TryRemove(key, out _))
                {
                    return;
                }

                await using var scope = _scopeFactory.CreateAsyncScope();
                var sessionService = scope.ServiceProvider.GetRequiredService<ISessionService>();

                var leaveResponse = await sessionService.LeaveSessionAsync(sessionId, userId, CancellationToken.None);

                if (leaveResponse.Success)
                {
                    await _hubContext.Clients
                        .Group(groupName)
                        .SendAsync("MEMBER_LEFT", new { userId, slotIndex });

                    _logger.LogInformation(
                        "Lobby grace window expired. Member {UserId} removed from session {SessionId}.",
                        userId, sessionId);
                }
            }
            catch (OperationCanceledException)
            {
                // Reconnected within grace period — leave cancelled
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Lobby grace window leave failed. UserId {UserId}, SessionId {SessionId}.",
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
                "Lobby grace window cancelled (reconnected). UserId {UserId}, SessionId {SessionId}.",
                userId, sessionId);
            return true;
        }

        return false;
    }
}
