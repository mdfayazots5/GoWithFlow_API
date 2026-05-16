namespace GoWithFlow.Domain.Entities;

public sealed class Mistake : BaseAuditEntity
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

	public User? User { get; set; }

	public Session? Session { get; set; }

	public Utterance? Utterance { get; set; }

	public Script? Script { get; set; }

	public ICollection<RepracticeUtterance> RepracticeUtterances { get; set; } = new List<RepracticeUtterance>();
}
