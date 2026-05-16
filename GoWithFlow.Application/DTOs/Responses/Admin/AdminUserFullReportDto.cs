namespace GoWithFlow.Application.DTOs.Responses.Admin;

public sealed class AdminUserFullReportDto
{
	public AdminUserReportHeaderDto UserHeader { get; set; } = new();

	public List<SessionSummaryDto> SessionHistoryList { get; set; } = new();

	public List<AdminMistakeBreakdownDto> MistakeBreakdownList { get; set; } = new();

	public List<WeeklyScoreDto> WeeklyScoreList { get; set; } = new();
}
