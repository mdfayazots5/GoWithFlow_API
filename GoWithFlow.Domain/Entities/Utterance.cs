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

	/// <summary>
	/// Question &amp; Answer only — optional set of hard/important words for this turn, authored on
	/// Interviewer rows. Stored as pipe-separated <c>word:meaning</c> pairs
	/// (e.g. <c>mitigate:to reduce harm | leverage:to make use of</c>). Surfaced as a "Key words to
	/// remember" study aid on the Interviewer/listen turn only when the session's ShowHardWords flag is on.
	/// Null/blank for every other category.
	/// </summary>
	public string? HardWords { get; set; }

	public Script? Script { get; set; }
}
