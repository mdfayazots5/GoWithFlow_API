namespace GoWithFlow.Application.DTOs.Responses.User;

public sealed class StreakDataResponseDto
{
	public int CurrentStreak { get; set; }

	public int LongestStreak { get; set; }

	public List<DailyStreakDto> Last30Days { get; set; } = new();
}
