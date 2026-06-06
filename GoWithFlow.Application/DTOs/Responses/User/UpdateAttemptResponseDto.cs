namespace GoWithFlow.Application.DTOs.Responses.User;

/// <summary>
/// Returned after a repractice attempt is recorded so the client can reflect the
/// authoritative server-side resolution state (resolves after two consecutive scores &gt; 80)
/// without guessing locally.
/// </summary>
public sealed class UpdateAttemptResponseDto
{
	public long RepracticeUtteranceId { get; set; }

	/// <summary>True once the linked mistake/utterance has been resolved server-side.</summary>
	public bool IsResolved { get; set; }

	public int AttemptCount { get; set; }

	public decimal BestScore { get; set; }

	public decimal LastScore { get; set; }
}
