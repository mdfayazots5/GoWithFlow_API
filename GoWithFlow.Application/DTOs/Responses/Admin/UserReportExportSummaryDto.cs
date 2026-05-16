namespace GoWithFlow.Application.DTOs.Responses.Admin;

public sealed class UserReportExportSummaryDto
{
	public long UserId { get; set; }

	public string FullName { get; set; } = string.Empty;

	public int TotalSessions { get; set; }

	public decimal AvgScore { get; set; }

	public int TotalMistakes { get; set; }

	public decimal ImprovementPercent { get; set; }
}
