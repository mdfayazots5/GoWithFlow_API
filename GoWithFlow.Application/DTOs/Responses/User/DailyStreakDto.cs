namespace GoWithFlow.Application.DTOs.Responses.User;

public sealed class DailyStreakDto
{
	public DateTime StreakDate { get; set; }

	public int SessionCount { get; set; }

	public int PracticeMinutes { get; set; }
}
