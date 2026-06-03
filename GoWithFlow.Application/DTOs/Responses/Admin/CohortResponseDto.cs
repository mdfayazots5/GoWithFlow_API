namespace GoWithFlow.Application.DTOs.Responses.Admin;

public sealed class CohortResponseDto
{
	public long CohortId { get; set; }

	public string CohortName { get; set; } = string.Empty;

	public string? Description { get; set; }

	public bool IsActive { get; set; }

	public int MemberCount { get; set; }

	public DateTime DateCreated { get; set; }
}

public sealed class CohortMemberDto
{
	public long UserId { get; set; }

	public string FullName { get; set; } = string.Empty;

	public string MobileNumber { get; set; } = string.Empty;

	public string AgeGroup { get; set; } = string.Empty;

	public bool IsActive { get; set; }

	public int DailyStreakCount { get; set; }

	public int TotalSessionsPlayed { get; set; }

	public DateTime? LastLoginDate { get; set; }

	public string? AvatarUrl { get; set; }

	public int SessionCount { get; set; }

	public decimal AvgFluencyScore { get; set; }

	public int TotalMistakes { get; set; }
}

public sealed class CohortAnalyticsResponseDto
{
	public long CohortId { get; set; }

	public string CohortName { get; set; } = string.Empty;

	public string? Description { get; set; }

	public int MemberCount { get; set; }

	public decimal AvgFluencyScore { get; set; }

	public int InactiveCount { get; set; }

	public List<CohortGrammarMistakeDto> TopGrammarMistakes { get; set; } = new();

	public CohortMostImprovedDto? MostImproved { get; set; }
}

public sealed class CohortGrammarMistakeDto
{
	public string GrammarTag { get; set; } = string.Empty;

	public int MistakeCount { get; set; }
}

public sealed class CohortMostImprovedDto
{
	public long UserId { get; set; }

	public string FullName { get; set; } = string.Empty;

	public decimal ImprovementDelta { get; set; }
}
