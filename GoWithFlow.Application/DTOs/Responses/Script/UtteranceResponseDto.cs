namespace GoWithFlow.Application.DTOs.Responses.Script;

public sealed class UtteranceResponseDto
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

	/// <summary>Question &amp; Answer — raw pipe-separated <c>word:meaning</c> hard-words list (Column I). Null elsewhere.</summary>
	public string? HardWords { get; set; }
}
