namespace GoWithFlow.Domain.Entities;

public sealed class WeeklyChallenge : BaseAuditEntity
{
	public long ChallengeId { get; set; }

	public long ScriptId { get; set; }

	public DateTime WeekStartDate { get; set; }

	public DateTime WeekEndDate { get; set; }

	public bool IsActive { get; set; } = true;

	public Script? Script { get; set; }

	public ICollection<ChallengeAttempt> Attempts { get; set; } = new List<ChallengeAttempt>();
}
