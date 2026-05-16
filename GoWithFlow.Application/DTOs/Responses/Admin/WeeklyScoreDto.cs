namespace GoWithFlow.Application.DTOs.Responses.Admin;

public sealed class WeeklyScoreDto
{
	public string WeekLabel { get; set; } = string.Empty;

	public decimal AvgFluencyScore { get; set; }
}
