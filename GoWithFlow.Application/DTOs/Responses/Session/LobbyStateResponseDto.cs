namespace GoWithFlow.Application.DTOs.Responses.Session;

public sealed class LobbyStateResponseDto
{
	public long SessionId { get; set; }

	public string SessionName { get; set; } = string.Empty;

	public string JoinCode { get; set; } = string.Empty;

	public string SessionMode { get; set; } = string.Empty;

	public string ScriptTitle { get; set; } = string.Empty;

	public byte MaxMembers { get; set; }

	public int SessionDuration { get; set; }

	public string Status { get; set; } = string.Empty;

	public List<LobbyMemberDto> Members { get; set; } = new();

	public bool CanStart { get; set; }

	/// <summary>Phase 16: host enabled "Record Session" — drives all-participant audio capture.</summary>
	public bool RecordingEnabled { get; set; }

	/// <summary>Phase 17: session has the AI Voice Participant enabled. The lobby hides the
	/// "Record Session" toggle when true (AI turns are TTS-narrated and not captured today).</summary>
	public bool AiEnabled { get; set; }
}
