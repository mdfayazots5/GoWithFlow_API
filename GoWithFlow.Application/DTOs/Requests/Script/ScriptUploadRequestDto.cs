namespace GoWithFlow.Application.DTOs.Requests.Script;

public sealed class ScriptUploadRequestDto
{
	public string ScriptTitle { get; set; } = string.Empty;

	public string Category { get; set; } = string.Empty;

	public string GrammarFocusTag { get; set; } = string.Empty;

	public string ContextTag { get; set; } = string.Empty;

	public byte ComplexityLevel { get; set; }

	public string TargetAgeGroup { get; set; } = string.Empty;

	public string HintLanguage { get; set; } = string.Empty;
}
