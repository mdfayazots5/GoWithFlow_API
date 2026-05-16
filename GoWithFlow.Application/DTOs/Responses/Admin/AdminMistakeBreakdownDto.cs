namespace GoWithFlow.Application.DTOs.Responses.Admin;

public sealed class AdminMistakeBreakdownDto
{
	public string GrammarTag { get; set; } = string.Empty;

	public int MistakeCount { get; set; }
}
