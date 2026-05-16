namespace GoWithFlow.Application.DTOs.Responses.User;

public sealed class WeeklyScoreDto
{
	public string WeekLabel { get; set; } = string.Empty;

	public decimal AvgFluencyScore { get; set; }
}
