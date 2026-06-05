namespace GoWithFlow.Application.Interfaces.Repositories;

/// <summary>
/// Raw row of tblSessionRecording (one per session). Used by the merge worker and admin read.
/// </summary>
public sealed class SessionRecordingRow
{
    public long RecordingId { get; set; }
    public long SessionId { get; set; }
    public string? StorageKey { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Format { get; set; }
    public int? DurationSecs { get; set; }
    public long? SizeBytes { get; set; }
    public int? SegmentCount { get; set; }
    public string? ParticipantsJson { get; set; }
    public string? FailureReason { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

public interface ISessionRecordingRepository
{
    /// <summary>True if the host enabled "Record Session" for this session.</summary>
    Task<bool> GetRecordingEnabledAsync(long sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets the host "Record Session" flag. Only succeeds when <paramref name="hostUserId"/> is the
    /// session host. Returns false if the session does not exist or the caller is not the host.
    /// </summary>
    Task<bool> SetRecordingEnabledAsync(long sessionId, long hostUserId, bool enabled, CancellationToken cancellationToken = default);

    /// <summary>
    /// Inserts a PENDING_MERGE recording row, or returns the existing row's id if one already
    /// exists for the session (idempotent — safe to call once per CompleteSession).
    /// </summary>
    Task<long> CreateOrGetAsync(long sessionId, DateTime createdAt, DateTime expiresAt, CancellationToken cancellationToken = default);

    Task<SessionRecordingRow?> GetBySessionAsync(long sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Atomically claims the row for merging: PENDING_MERGE/FAILED → PROCESSING and increments
    /// AttemptCount. Returns false if another worker already claimed it or it is already READY
    /// (single-merge idempotency guard).
    /// </summary>
    Task<bool> TryClaimForMergeAsync(long recordingId, CancellationToken cancellationToken = default);

    Task MarkReadyAsync(long recordingId, string storageKey, string format, int durationSecs, long sizeBytes, int segmentCount, string participantsJson, CancellationToken cancellationToken = default);

    Task MarkFailedAsync(long recordingId, string failureReason, CancellationToken cancellationToken = default);

    /// <summary>
    /// SessionIds whose recording is still PENDING_MERGE or FAILED with AttemptCount below the cap.
    /// Used by the worker on startup to recover jobs lost to a restart and to retry failures.
    /// </summary>
    Task<IReadOnlyList<long>> GetResumableSessionIdsAsync(int maxAttempts, CancellationToken cancellationToken = default);

    /// <summary>Recordings past their ExpiresAt that are not yet deleted (retention cleanup).</summary>
    Task<IReadOnlyList<(long RecordingId, string? StorageKey)>> GetExpiredAsync(int batchSize, CancellationToken cancellationToken = default);

    /// <summary>Soft-deletes a recording row after its R2 object has been removed.</summary>
    Task SoftDeleteAsync(long recordingId, CancellationToken cancellationToken = default);
}
