namespace GoWithFlow.Application.DTOs.Responses.User;

public sealed class ActiveChallengeResponseDto
{
	public bool HasActiveChallenge { get; set; }

	public long? ChallengeId { get; set; }

	public long? ScriptId { get; set; }

	public string ScriptTitle { get; set; } = string.Empty;

	public string Category { get; set; } = string.Empty;

	public int ComplexityLevel { get; set; }

	public DateTime? WeekStartDate { get; set; }

	public DateTime? WeekEndDate { get; set; }

	public int DaysRemaining { get; set; }

	public decimal UserBestScore { get; set; }

	public int UserAttemptCount { get; set; }

	public List<ChallengeLeaderboardEntryDto> Leaderboard { get; set; } = new();
}

public sealed class ChallengeLeaderboardEntryDto
{
	public int Rank { get; set; }

	public string FullName { get; set; } = string.Empty;

	public decimal BestScore { get; set; }
}
