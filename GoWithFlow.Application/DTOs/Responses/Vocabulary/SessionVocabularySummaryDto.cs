namespace GoWithFlow.Application.DTOs.Responses.Vocabulary;

/// <summary>
/// Appended to session completion summary for VocabularySprint sessions.
/// Shows words practiced this session and vocabulary bank totals.
/// </summary>
public sealed class SessionVocabularySummaryDto
{
	public int WordsPracticedThisSession { get; set; }

	public int TotalWordsInBank { get; set; }

	public int WordsDueForReview { get; set; }

	public List<string> WordsPracticed { get; set; } = new();
}
