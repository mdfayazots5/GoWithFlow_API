namespace GoWithFlow.Application.DTOs.Responses.Session;

public sealed class SessionListItemResponseDto
{
	public long SessionId { get; set; }

	public string SessionName { get; set; } = string.Empty;

	public string SessionMode { get; set; } = string.Empty;

	public DateTime SessionDate { get; set; }

	public int Duration { get; set; }

	public decimal? FluencyScore { get; set; }

	public int MistakeCount { get; set; }

	public string Status { get; set; } = string.Empty;

	public string ScriptTitle { get; set; } = string.Empty;
}
