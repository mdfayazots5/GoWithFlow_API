namespace GoWithFlow.Application.DTOs.Responses.LiveSession;

public sealed class SessionReviewResponseDto
{
	public long SessionId { get; set; }

	public string ScriptTitle { get; set; } = string.Empty;

	public string Category { get; set; } = string.Empty;

	public string GrammarFocusTag { get; set; } = string.Empty;

	public int TotalTurns { get; set; }

	public decimal AverageOverallScore { get; set; }

	public List<SessionReviewTurnDto> Turns { get; set; } = new();
}
