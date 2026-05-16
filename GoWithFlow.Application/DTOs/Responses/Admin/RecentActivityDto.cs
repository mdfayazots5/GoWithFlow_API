namespace GoWithFlow.Application.DTOs.Responses.Admin;

public sealed class RecentActivityDto
{
	public string UserFullName { get; set; } = string.Empty;

	public string SessionName { get; set; } = string.Empty;

	public DateTime SessionDate { get; set; }

	public decimal FluencyScore { get; set; }

	public int MistakeCount { get; set; }

	public string SessionStatus { get; set; } = string.Empty;
}
