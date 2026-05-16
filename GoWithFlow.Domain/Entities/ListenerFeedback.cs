namespace GoWithFlow.Domain.Entities;

public sealed class ListenerFeedback : BaseAuditEntity
{
	public long ListenerFeedbackId { get; set; }

	public long SessionId { get; set; }

	public int TurnIndex { get; set; }

	public long FromUserId { get; set; }

	public long TargetUserId { get; set; }

	public string FeedbackTag { get; set; } = string.Empty;

	public DateTime FeedbackAt { get; set; }

	public Session? Session { get; set; }

	public User? FromUser { get; set; }

	public User? TargetUser { get; set; }
}
