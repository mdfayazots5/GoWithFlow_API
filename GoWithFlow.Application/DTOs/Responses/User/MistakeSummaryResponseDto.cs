namespace GoWithFlow.Application.DTOs.Responses.User;

public sealed class MistakeSummaryResponseDto
{
	public int TotalMistakes { get; set; }

	public int ResolvedMistakes { get; set; }

	public int PendingMistakes { get; set; }

	public decimal ImprovementPercent { get; set; }
}
