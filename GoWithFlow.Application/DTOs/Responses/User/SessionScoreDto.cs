namespace GoWithFlow.Application.DTOs.Responses.User;

public sealed class SessionScoreDto
{
	public DateTime SessionDate { get; set; }

	public string SessionName { get; set; } = string.Empty;

	public decimal FluencyScore { get; set; }

	public decimal ConfidenceScore { get; set; }

	public int MistakeCount { get; set; }
}
