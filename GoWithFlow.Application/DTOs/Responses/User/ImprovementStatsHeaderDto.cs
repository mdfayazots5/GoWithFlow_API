namespace GoWithFlow.Application.DTOs.Responses.User;

public sealed class ImprovementStatsHeaderDto
{
	public int SessionsCompleted { get; set; }

	public decimal AvgScoreThisWeek { get; set; }

	public int MistakesResolved { get; set; }

	public int CurrentStreak { get; set; }
}
