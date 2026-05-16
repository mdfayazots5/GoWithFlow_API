namespace GoWithFlow.Application.DTOs.Responses.User;

public sealed class PerformanceSummaryDto
{
	public decimal FluencyScore { get; set; }

	public decimal ConfidenceScore { get; set; }

	public int SpeakingSpeedWpm { get; set; }

	public int PauseCount { get; set; }
}
