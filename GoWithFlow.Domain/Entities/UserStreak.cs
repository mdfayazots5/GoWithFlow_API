namespace GoWithFlow.Domain.Entities;

public sealed class UserStreak : BaseAuditEntity
{
	public long UserStreakId { get; set; }

	public long UserId { get; set; }

	public DateTime StreakDate { get; set; }

	public int SessionCount { get; set; }

	public int PracticeMinutes { get; set; }

	public User? User { get; set; }
}
