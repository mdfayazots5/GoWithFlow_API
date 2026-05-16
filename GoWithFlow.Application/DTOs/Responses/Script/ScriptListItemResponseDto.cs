namespace GoWithFlow.Application.DTOs.Responses.Script;

public sealed class ScriptListItemResponseDto
{
	public long ScriptId { get; set; }

	public string ScriptTitle { get; set; } = string.Empty;

	public string Category { get; set; } = string.Empty;

	public string GrammarFocusTag { get; set; } = string.Empty;

	public string ContextTag { get; set; } = string.Empty;

	public byte ComplexityLevel { get; set; }

	public string TargetAgeGroup { get; set; } = string.Empty;

	public int UtteranceCount { get; set; }

	public bool IsActive { get; set; }

	public DateTime UploadedDate { get; set; }

	public int Version { get; set; }
}
