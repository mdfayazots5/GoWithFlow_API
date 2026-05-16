namespace GoWithFlow.Domain.Entities;

public sealed class Script : BaseAuditEntity
{
	public long ScriptId { get; set; }

	public string ScriptTitle { get; set; } = string.Empty;

	public string Category { get; set; } = string.Empty;

	public string GrammarFocusTag { get; set; } = string.Empty;

	public string ContextTag { get; set; } = string.Empty;

	public byte ComplexityLevel { get; set; }

	public string TargetAgeGroup { get; set; } = string.Empty;

	public string HintLanguage { get; set; } = string.Empty;

	public bool IsActive { get; set; } = true;

	public DateTime UploadedDate { get; set; }

	public long UploadedByUserId { get; set; }

	public int Version { get; set; }

	public int UtteranceCount { get; set; }

	public ICollection<Utterance> Utterances { get; set; } = new List<Utterance>();
}
