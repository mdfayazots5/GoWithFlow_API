namespace GoWithFlow.Domain.Entities;

public sealed class RepracticeSession : BaseAuditEntity
{
	public long RepracticeSessionId { get; set; }

	public long UserId { get; set; }

	public long SourceSessionId { get; set; }

	public int TotalMistakes { get; set; }

	public int CompletedRounds { get; set; }

	public decimal ImprovementPercent { get; set; }

	public string Status { get; set; } = string.Empty;

	public DateTime GeneratedDate { get; set; }

	public User? User { get; set; }

	public Session? SourceSession { get; set; }

	public ICollection<RepracticeUtterance> Utterances { get; set; } = new List<RepracticeUtterance>();
}
