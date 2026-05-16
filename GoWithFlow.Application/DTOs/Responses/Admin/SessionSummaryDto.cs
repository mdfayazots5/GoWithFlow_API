namespace GoWithFlow.Application.DTOs.Responses.Admin;

public sealed class SessionSummaryDto
{
	public long SessionId { get; set; }

	public string SessionName { get; set; } = string.Empty;

	public DateTime Date { get; set; }

	public int Duration { get; set; }

	public decimal FluencyScore { get; set; }

	public int MistakeCount { get; set; }
}
