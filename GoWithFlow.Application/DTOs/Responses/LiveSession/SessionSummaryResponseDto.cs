namespace GoWithFlow.Application.DTOs.Responses.LiveSession;

public sealed class SessionSummaryResponseDto
{
	public List<MemberScoreDto> MemberScores { get; set; } = new();

	public int TotalTurns { get; set; }

	public string ScriptTitle { get; set; } = string.Empty;

	public string GrammarFocusTag { get; set; } = string.Empty;

	public int TotalMistakesAllMembers { get; set; }
}
