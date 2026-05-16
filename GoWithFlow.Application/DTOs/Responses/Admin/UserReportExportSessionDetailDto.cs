namespace GoWithFlow.Application.DTOs.Responses.Admin;

public sealed class UserReportExportSessionDetailDto
{
	public long UserId { get; set; }

	public string FullName { get; set; } = string.Empty;

	public long SessionId { get; set; }

	public string SessionName { get; set; } = string.Empty;

	public DateTime SessionDate { get; set; }

	public decimal FluencyScore { get; set; }

	public int MistakeCount { get; set; }

	public string Status { get; set; } = string.Empty;
}
