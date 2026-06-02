namespace GoWithFlow.Application.DTOs.Responses.User;

public sealed class WeeklyReportResponseDto
{
	/// <summary>Sessions completed in the current calendar week (Mon–Sun).</summary>
	public int SessionsThisWeek { get; set; }

	/// <summary>Total practice time in minutes this week.</summary>
	public int PracticeMinutesThisWeek { get; set; }

	/// <summary>Grammar errors detected this week from tblMistake.</summary>
	public int ErrorsDetectedThisWeek { get; set; }

	/// <summary>Grammar errors resolved via RepracticeRound this week.</summary>
	public int ErrorsResolvedThisWeek { get; set; }

	/// <summary>The metric with the largest positive delta vs. last week.</summary>
	public string TopImprovementMetric { get; set; } = string.Empty;

	/// <summary>The grammar tag with the highest error count this week.</summary>
	public string WeakestGrammarTag { get; set; } = string.Empty;

	/// <summary>Title of the recommended session targeting the weakest grammar area.</summary>
	public string RecommendedScript1Title { get; set; } = string.Empty;

	public long RecommendedScript1Id { get; set; }

	/// <summary>Title of a variety recommendation (next complexity step or unvisited category).</summary>
	public string RecommendedScript2Title { get; set; } = string.Empty;

	public long RecommendedScript2Id { get; set; }

	/// <summary>True if the user had zero sessions this week — triggers re-engagement message.</summary>
	public bool IsReengagement { get; set; }

	/// <summary>Last session date used for re-engagement message.</summary>
	public DateTime? LastSessionDate { get; set; }
}
