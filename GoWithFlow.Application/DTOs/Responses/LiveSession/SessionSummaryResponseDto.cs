using GoWithFlow.Application.DTOs.Responses.Vocabulary;

namespace GoWithFlow.Application.DTOs.Responses.LiveSession;

public sealed class SessionSummaryResponseDto
{
	public List<MemberScoreDto> MemberScores { get; set; } = new();

	public int TotalTurns { get; set; }

	public string ScriptTitle { get; set; } = string.Empty;

	public string GrammarFocusTag { get; set; } = string.Empty;

	public int TotalMistakesAllMembers { get; set; }

	/// <summary>Populated for VocabularySprint sessions only. Null for all other categories.</summary>
	public SessionVocabularySummaryDto? VocabularySummary { get; set; }
}
