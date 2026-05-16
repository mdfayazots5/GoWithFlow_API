namespace GoWithFlow.Domain.Entities;

public sealed class Utterance : BaseAuditEntity
{
	public long UtteranceId { get; set; }

	public long ScriptId { get; set; }

	public int SequenceId { get; set; }

	public string SpeakerLabel { get; set; } = string.Empty;

	public string EnglishText { get; set; } = string.Empty;

	public string? HintText { get; set; }

	public string? GrammarTag { get; set; }

	public string? ContextTag { get; set; }

	public string? FocusWord { get; set; }

	public string? PronunciationNote { get; set; }

	public Script? Script { get; set; }
}
