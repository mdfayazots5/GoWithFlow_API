namespace GoWithFlow.Application.DTOs.Responses.Admin;

public sealed class AdminUserReportHeaderDto
{
	public long UserId { get; set; }

	public string? AvatarUrl { get; set; }

	public string FullName { get; set; } = string.Empty;

	public int DailyStreakCount { get; set; }

	public int TotalSessions { get; set; }

	public decimal AvgScore { get; set; }
}
