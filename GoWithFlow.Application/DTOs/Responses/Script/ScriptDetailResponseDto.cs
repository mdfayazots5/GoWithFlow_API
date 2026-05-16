namespace GoWithFlow.Application.DTOs.Responses.Script;

public sealed class ScriptDetailResponseDto
{
	public long ScriptId { get; set; }

	public string ScriptTitle { get; set; } = string.Empty;

	public string Category { get; set; } = string.Empty;

	public string GrammarFocusTag { get; set; } = string.Empty;

	public string ContextTag { get; set; } = string.Empty;

	public byte ComplexityLevel { get; set; }

	public string TargetAgeGroup { get; set; } = string.Empty;

	public string HintLanguage { get; set; } = string.Empty;

	public bool IsActive { get; set; }

	public DateTime UploadedDate { get; set; }

	public long UploadedByUserId { get; set; }

	public int Version { get; set; }

	public int UtteranceCount { get; set; }

	public List<UtteranceResponseDto> Utterances { get; set; } = new();
}
