namespace GoWithFlow.Application.DTOs.Requests.User;

public sealed class SubmitChallengeAttemptRequestDto
{
	public long ChallengeId { get; set; }

	public decimal Score { get; set; }
}

public sealed class SetWeeklyChallengeRequestDto
{
	public long ScriptId { get; set; }
}
