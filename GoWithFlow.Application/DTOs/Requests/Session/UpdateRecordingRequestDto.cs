namespace GoWithFlow.Application.DTOs.Requests.Session;

/// <summary>Host toggles "Record Session" for a session (typically from the lobby).</summary>
public sealed class UpdateRecordingRequestDto
{
    public bool Enabled { get; set; }
}
