namespace GoWithFlow.Application.DTOs.Responses.Admin;

public sealed class AdminReportSummaryDto
{
	public long UserId { get; set; }

	public string FullName { get; set; } = string.Empty;

	public int TotalSessions { get; set; }

	public decimal AvgFluencyScore { get; set; }

	public string MostCommonMistakeType { get; set; } = string.Empty;

	public decimal ImprovementPercent { get; set; }

	public DateTime? LastSessionDate { get; set; }
}
