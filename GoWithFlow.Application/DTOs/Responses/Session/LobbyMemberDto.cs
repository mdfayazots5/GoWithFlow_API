namespace GoWithFlow.Application.DTOs.Responses.Session;

public sealed class LobbyMemberDto
{
	public long UserId { get; set; }

	public string FullName { get; set; } = string.Empty;

	public string? AvatarUrl { get; set; }

	public byte SlotIndex { get; set; }

	public string SlotName { get; set; } = string.Empty;

	public bool IsReady { get; set; }

	public bool IsHost { get; set; }
}
