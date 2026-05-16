namespace GoWithFlow.Application.DTOs.Responses.Admin;

public sealed class GrammarMistakeSummaryDto
{
	public string GrammarTag { get; set; } = string.Empty;

	public int UserCount { get; set; }

	public decimal Percentage { get; set; }
}
