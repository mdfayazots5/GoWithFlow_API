using GoWithFlow.Application.Common;
using GoWithFlow.Application.DTOs.Responses.Admin;

namespace GoWithFlow.Application.Interfaces.Services;

public interface ISessionRecordingService
{
    /// <summary>
    /// Called on session completion. If the host enabled recording, creates the consolidated
    /// recording row (PENDING_MERGE) and enqueues the merge job. No-op if recording was not enabled.
    /// Never throws into the completion flow.
    /// </summary>
    Task EnsureRecordingQueuedAsync(long sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Performs the actual merge: claims the row, downloads ordered per-turn segments, concatenates
    /// + normalizes via ffmpeg into one .m4a, uploads it, and marks the row READY (or FAILED).
    /// Idempotent — safe to call more than once for the same session.
    /// </summary>
    Task MergeSessionAsync(long sessionId, CancellationToken cancellationToken = default);

    /// <summary>Admin view: the single consolidated recording for a session (or null if none).</summary>
    Task<ApiResponse<SessionRecordingDto?>> GetAdminSessionRecordingAsync(long sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Host toggles "Record Session" for a session (typically from the lobby before start).
    /// Host-only; returns failure if the caller is not the session host.
    /// </summary>
    Task<ApiResponse<bool>> SetRecordingEnabledAsync(long sessionId, long hostUserId, bool enabled, CancellationToken cancellationToken = default);
}
