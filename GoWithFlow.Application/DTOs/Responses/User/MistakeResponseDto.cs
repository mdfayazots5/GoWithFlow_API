namespace GoWithFlow.Application.DTOs.Responses.User;

public sealed class MistakeResponseDto
{
	public long MistakeId { get; set; }

	public long UserId { get; set; }

	public long SessionId { get; set; }

	public long UtteranceId { get; set; }

	public long ScriptId { get; set; }

	public string UtteranceText { get; set; } = string.Empty;

	public string? SpokenText { get; set; }

	public string MistakeType { get; set; } = string.Empty;

	public string? MistakeDetail { get; set; }

	public string? GrammarTag { get; set; }

	public string? ContextTag { get; set; }

	public string? CorrectionText { get; set; }

	public int PracticeCount { get; set; }

	public bool IsResolved { get; set; }

	public DateTime FirstOccurrence { get; set; }

	public DateTime? LastAttempt { get; set; }

	public string SessionName { get; set; } = string.Empty;

	public string ScriptTitle { get; set; } = string.Empty;
}
