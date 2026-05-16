namespace GoWithFlow.Domain.Entities;

public sealed class TurnState : BaseAuditEntity
{
	public long TurnStateId { get; set; }

	public long SessionId { get; set; }

	public int TurnIndex { get; set; }

	public int TotalTurns { get; set; }

	public long ActiveMemberId { get; set; }

	public byte ActiveSlotIndex { get; set; }

	public long UtteranceId { get; set; }

	public bool ReReadAllowed { get; set; } = true;

	public int ReReadCount { get; set; }

	public int MaxReReads { get; set; } = 2;

	public string TurnStatus { get; set; } = string.Empty;

	public DateTime? TurnStartedAt { get; set; }

	public DateTime? TurnCompletedAt { get; set; }

	public Session? Session { get; set; }

	public User? ActiveMember { get; set; }

	public Utterance? Utterance { get; set; }
}
