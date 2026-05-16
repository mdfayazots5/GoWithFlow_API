namespace GoWithFlow.Domain.Entities;

public sealed class UserBadge : BaseAuditEntity
{
	public long UserBadgeId { get; set; }

	public long UserId { get; set; }

	public string BadgeCode { get; set; } = string.Empty;

	public string BadgeName { get; set; } = string.Empty;

	public DateTime EarnedDate { get; set; }

	public User? User { get; set; }
}
