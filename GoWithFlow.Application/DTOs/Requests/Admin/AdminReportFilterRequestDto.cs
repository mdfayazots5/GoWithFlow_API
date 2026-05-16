namespace GoWithFlow.Application.DTOs.Requests.Admin;

public sealed class AdminReportFilterRequestDto
{
	public DateTime? FromDate { get; set; }

	public DateTime? ToDate { get; set; }

	public long? UserId { get; set; }

	public int PageNumber { get; set; } = 1;

	public int PageSize { get; set; } = 10;
}
