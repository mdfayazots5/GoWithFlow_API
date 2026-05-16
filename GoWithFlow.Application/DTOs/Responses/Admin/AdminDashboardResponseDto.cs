namespace GoWithFlow.Application.DTOs.Responses.Admin;

public sealed class AdminDashboardResponseDto
{
	public int TotalUsers { get; set; }

	public int ActiveSessionsToday { get; set; }

	public int TotalScriptsUploaded { get; set; }

	public int TotalMistakesRecorded { get; set; }

	public List<RecentActivityDto> RecentActivities { get; set; } = new();

	public List<GrammarMistakeSummaryDto> TopGrammarMistakes { get; set; } = new();
}
