using System.Threading.Channels;
using GoWithFlow.Application.Interfaces.Services;

namespace GoWithFlow.Infrastructure.ExternalServices;

/// <summary>
/// Unbounded in-process channel of sessionIds awaiting merge. Singleton.
/// Survives only for the process lifetime; the worker also re-scans PENDING_MERGE/FAILED rows
/// on startup so an enqueue lost to a restart is recovered (see SessionRecordingMergeWorker).
/// </summary>
public sealed class SessionRecordingMergeQueue : ISessionRecordingMergeQueue
{
    private readonly Channel<long> _channel =
        Channel.CreateUnbounded<long>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });

    public ValueTask EnqueueAsync(long sessionId, CancellationToken cancellationToken = default)
        => _channel.Writer.WriteAsync(sessionId, cancellationToken);

    public ValueTask<long> DequeueAsync(CancellationToken cancellationToken)
        => _channel.Reader.ReadAsync(cancellationToken);
}
