namespace GoWithFlow.Application.DTOs.Responses.Admin;

/// <summary>
/// One consolidated recording per session (replaces the per-turn clip list in the admin view).
/// </summary>
public sealed class SessionRecordingDto
{
    public long RecordingId { get; set; }

    public long SessionId { get; set; }

    /// <summary>CAPTURING | PENDING_MERGE | PROCESSING | READY | FAILED.</summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>Presigned playback URL for the consolidated file. Null until Status = READY.</summary>
    public string? AudioUrl { get; set; }

    public string? Format { get; set; }

    public int? DurationSecs { get; set; }

    public long? SizeBytes { get; set; }

    public int? SegmentCount { get; set; }

    /// <summary>Distinct participants captured in this recording (name + turn count).</summary>
    public List<SessionRecordingParticipantDto> Participants { get; set; } = new();

    public string? FailureReason { get; set; }

    /// <summary>Recording start (session start), surfaced to admin as "created time".</summary>
    public DateTime CreatedAt { get; set; }

    public DateTime? CompletedAt { get; set; }
}

public sealed class SessionRecordingParticipantDto
{
    public long UserId { get; set; }

    public string Name { get; set; } = string.Empty;

    public int Turns { get; set; }
}
