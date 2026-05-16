namespace GoWithFlow.Application.DTOs.Responses.Session;

public sealed class SessionPreviewResponseDto
{
	public long SessionId { get; set; }

	public string SessionName { get; set; } = string.Empty;

	public string SessionMode { get; set; } = string.Empty;

	public string ScriptTitle { get; set; } = string.Empty;

	public string ScriptGrammarTag { get; set; } = string.Empty;

	public int Duration { get; set; }

	public byte MaxMembers { get; set; }

	public int CurrentMemberCount { get; set; }

	public string Status { get; set; } = string.Empty;

	public List<SlotInfoDto> Slots { get; set; } = new();
}
