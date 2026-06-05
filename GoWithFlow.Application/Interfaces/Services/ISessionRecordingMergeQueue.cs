namespace GoWithFlow.Application.Interfaces.Services;

/// <summary>
/// In-process queue of sessionIds awaiting consolidation into a single recording.
/// Producer: CompleteSession (via ISessionRecordingService). Consumer: the merge background worker.
/// Registered as a singleton.
/// </summary>
public interface ISessionRecordingMergeQueue
{
    ValueTask EnqueueAsync(long sessionId, CancellationToken cancellationToken = default);

    ValueTask<long> DequeueAsync(CancellationToken cancellationToken);
}
