namespace GoWithFlow.Application.DTOs.Responses.User;

public sealed class GrammarProgressResponseDto
{
	public string GrammarTag { get; set; } = string.Empty;

	public int TotalMistakes { get; set; }

	public int ResolvedMistakes { get; set; }

	public decimal ImprovementPercent { get; set; }

	public int ProgressBarValue { get; set; }
}
