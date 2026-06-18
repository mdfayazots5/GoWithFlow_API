namespace GoWithFlow.Application.DTOs.Responses.LiveSession;

/// <summary>
/// Question &amp; Answer practice aid — a single "Key word to remember" surfaced on the Interviewer/listen
/// turn. Parsed from the utterance's pipe-separated <c>word:meaning</c> HardWords string.
/// </summary>
public sealed class HardWordDto
{
	public string Word { get; set; } = string.Empty;

	public string? Meaning { get; set; }
}
