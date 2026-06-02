namespace GoWithFlow.Domain.Entities;

public sealed class ChallengeAttempt : BaseAuditEntity
{
	public long AttemptId { get; set; }

	public long ChallengeId { get; set; }

	public long UserId { get; set; }

	public decimal FluencyScore { get; set; }

	public DateTime AttemptDate { get; set; }

	public WeeklyChallenge? Challenge { get; set; }

	public User? User { get; set; }
}
