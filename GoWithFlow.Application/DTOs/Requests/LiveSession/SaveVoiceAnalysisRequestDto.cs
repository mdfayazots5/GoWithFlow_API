namespace GoWithFlow.Application.DTOs.Requests.LiveSession;

public sealed class SaveVoiceAnalysisRequestDto
{
	public long SessionId { get; set; }

	public int TurnIndex { get; set; }

	public long UtteranceId { get; set; }

	public string? TranscribedText { get; set; }

	public string ExpectedText { get; set; } = string.Empty;

	public decimal FluencyScore { get; set; }

	public decimal ConfidenceScore { get; set; }

	public int SpeakingSpeedWpm { get; set; }

	public int PauseCount { get; set; }

	public List<string> HesitationWords { get; set; } = new();

	public List<string> RepeatedWords { get; set; } = new();

	public List<GrammarErrorDto> GrammarErrors { get; set; } = new();

	public List<PronunciationIssueDto> PronunciationIssues { get; set; } = new();

	public decimal OverallScore { get; set; }
}
