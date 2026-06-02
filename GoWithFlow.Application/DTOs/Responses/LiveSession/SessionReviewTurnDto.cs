using GoWithFlow.Application.DTOs.Requests.LiveSession;

namespace GoWithFlow.Application.DTOs.Responses.LiveSession;

public sealed class SessionReviewTurnDto
{
	public int TurnIndex { get; set; }

	public string SpeakerLabel { get; set; } = string.Empty;

	public bool IsFacilitatorTurn { get; set; }

	public string EnglishText { get; set; } = string.Empty;

	public string? TranscribedText { get; set; }

	public decimal FluencyScore { get; set; }

	public decimal ConfidenceScore { get; set; }

	public int SpeakingSpeedWpm { get; set; }

	public decimal OverallScore { get; set; }

	public List<string> HesitationWords { get; set; } = new();

	public List<GrammarErrorDto> GrammarErrors { get; set; } = new();

	public List<PronunciationIssueDto> PronunciationIssues { get; set; } = new();

	/// <summary>False for facilitator turns and turns that were skipped with no recording.</summary>
	public bool WasAnalyzed { get; set; }
}
