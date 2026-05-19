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
}
