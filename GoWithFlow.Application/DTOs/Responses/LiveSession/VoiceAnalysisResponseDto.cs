using GoWithFlow.Application.DTOs.Requests.LiveSession;

namespace GoWithFlow.Application.DTOs.Responses.LiveSession;

public sealed class VoiceAnalysisResponseDto
{
	public long VoiceAnalysisId { get; set; }

	public long SessionId { get; set; }

	public long UserId { get; set; }

	public string FullName { get; set; } = string.Empty;

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

	public DateTime RecordedAt { get; set; }
}
