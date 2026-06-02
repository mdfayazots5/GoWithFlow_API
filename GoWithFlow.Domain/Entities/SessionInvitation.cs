namespace GoWithFlow.Domain.Entities;

public sealed class SessionInvitation : BaseAuditEntity
{
	public long InvitationId { get; set; }

	public long SessionId { get; set; }

	public long UserId { get; set; }

	public byte SlotIndex { get; set; }

	public string SlotName { get; set; } = string.Empty;

	/// <summary>Valid values: PENDING, ACCEPTED, DECLINED, EXPIRED, CANCELLED</summary>
	public string Status { get; set; } = "PENDING";

	public DateTime SentAt { get; set; }

	public DateTime? RespondedAt { get; set; }

	public DateTime? ExpiresAt { get; set; }

	public Session? Session { get; set; }

	public User? User { get; set; }
}
