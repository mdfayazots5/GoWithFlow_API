namespace GoWithFlow.Application.DTOs.Requests.Script;

public sealed class ScriptSearchRequestDto
{
	public string? SearchTerm { get; set; }

	public string? Category { get; set; }

	public string? GrammarFocusTag { get; set; }

	public string? TargetAgeGroup { get; set; }

	public bool? IsActive { get; set; }

	public int PageNumber { get; set; } = 1;

	public int PageSize { get; set; } = 12;
}
