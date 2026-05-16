namespace GoWithFlow.Domain.Entities;

public sealed class VoiceAnalysis : BaseAuditEntity
{
	public long VoiceAnalysisId { get; set; }

	public long SessionId { get; set; }

	public long UserId { get; set; }

	public int TurnIndex { get; set; }

	public long UtteranceId { get; set; }

	public string? TranscribedText { get; set; }

	public string ExpectedText { get; set; } = string.Empty;

	public decimal FluencyScore { get; set; }

	public decimal ConfidenceScore { get; set; }

	public int SpeakingSpeedWpm { get; set; }

	public int PauseCount { get; set; }

	public string? HesitationWords { get; set; }

	public string? RepeatedWords { get; set; }

	public string? GrammarErrorsJson { get; set; }

	public string? PronunciationJson { get; set; }

	public decimal OverallScore { get; set; }

	public DateTime RecordedAt { get; set; }

	public Session? Session { get; set; }

	public User? User { get; set; }

	public Utterance? Utterance { get; set; }
}
