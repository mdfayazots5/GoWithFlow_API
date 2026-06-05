namespace GoWithFlow.Application.DTOs.Responses.User;

public sealed class AudioArchiveItemDto
{
	public long ArchiveId { get; set; }

	public long SessionId { get; set; }

	public long UserId { get; set; }

	public int TurnIndex { get; set; }

	public string AudioUrl { get; set; } = string.Empty;

	public int DurationSecs { get; set; }

	public DateTime ExpiresAt { get; set; }

	public DateTime DateCreated { get; set; }

	public string? UserName { get; set; }
}
