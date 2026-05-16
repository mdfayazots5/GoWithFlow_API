namespace GoWithFlow.Application.DTOs.Requests.LiveSession;

public sealed class TurnShiftRequestDto
{
	public long SessionId { get; set; }

	public long MemberId { get; set; }

	public int TurnIndex { get; set; }

	public decimal AnalysisScore { get; set; }
}
