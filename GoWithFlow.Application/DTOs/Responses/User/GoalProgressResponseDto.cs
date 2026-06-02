namespace GoWithFlow.Application.DTOs.Responses.User;

public sealed class GoalProgressResponseDto
{
    public bool HasActiveGoal { get; set; }

    /// <summary>interview | grammar | vocabulary | fluency</summary>
    public string GoalType { get; set; } = string.Empty;

    /// <summary>Human-readable goal label.</summary>
    public string GoalLabel { get; set; } = string.Empty;

    public int TimelineWeeks { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime TargetDate { get; set; }

    /// <summary>Beginner | Intermediate | Advanced — auto-detected at goal creation.</summary>
    public string DetectedLevel { get; set; } = string.Empty;

    /// <summary>Sessions completed since the goal was set.</summary>
    public int SessionsCompleted { get; set; }

    /// <summary>Recommended sessions target over the goal period.</summary>
    public int SessionsTarget { get; set; }

    /// <summary>Human-readable label of the primary tracked metric.</summary>
    public string PrimaryMetricLabel { get; set; } = string.Empty;

    public decimal StartingScore { get; set; }

    public decimal CurrentScore { get; set; }

    /// <summary>Target score to reach by TargetDate.</summary>
    public decimal TargetScore { get; set; }

    /// <summary>Improving | Stable | Declining</summary>
    public string TrendLabel { get; set; } = string.Empty;

    public int EstimatedWeeksRemaining { get; set; }

    /// <summary>Recommended practice plan: e.g. "3 sessions/week — 2×GrammarDrill + 1×RepracticeRound".</summary>
    public string RecommendedPlan { get; set; } = string.Empty;

    /// <summary>0–100 visual progress toward goal.</summary>
    public decimal ProgressPercent { get; set; }
}
