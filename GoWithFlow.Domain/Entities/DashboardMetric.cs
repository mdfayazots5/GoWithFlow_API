namespace GoWithFlow.Domain.Entities;

public sealed class DashboardMetric : BaseAuditEntity
{
	public long DashboardMetricId { get; set; }

	public DateTime MetricDate { get; set; }

	public int TotalUsers { get; set; }

	public int ActiveSessionsToday { get; set; }

	public int TotalScriptsUploaded { get; set; }

	public int TotalMistakesRecorded { get; set; }
}
