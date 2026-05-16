namespace GoWithFlow.Domain.Entities;

public sealed class SessionMember : BaseAuditEntity
{
	public long SessionMemberId { get; set; }

	public long SessionId { get; set; }

	public long UserId { get; set; }

	public byte SlotIndex { get; set; }

	public string SlotName { get; set; } = string.Empty;

	public bool IsReady { get; set; }

	public bool IsHost { get; set; }

	public DateTime? JoinedAt { get; set; }

	public DateTime? LeftAt { get; set; }

	public bool IsActive { get; set; } = true;

	public Session? Session { get; set; }

	public User? User { get; set; }
}
