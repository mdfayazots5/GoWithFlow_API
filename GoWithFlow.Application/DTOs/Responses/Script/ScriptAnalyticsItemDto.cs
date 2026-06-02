namespace GoWithFlow.Application.DTOs.Responses.Script;

public sealed class ScriptAnalyticsItemDto
{
    public long ScriptId { get; set; }
    public string ScriptTitle { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public int TotalSessionsStarted { get; set; }
    /// <summary>Sessions with Status = COMPLETED / total sessions started, 0–100.</summary>
    public decimal CompletionRate { get; set; }
    public decimal AvgFluencyScore { get; set; }
    public decimal AvgMistakeCount { get; set; }
    public decimal AvgDurationMinutes { get; set; }
    /// <summary>Average ReReadCount per turn across all sessions on this script.</summary>
    public decimal AvgReReadRate { get; set; }
    /// <summary>% of sessions that triggered at least one RepracticeSession, 0–100.</summary>
    public decimal RepracticeConversionRate { get; set; }
    public DateTime? LastUsedDate { get; set; }
    /// <summary>True when last used date is more than 60 days ago or never used.</summary>
    public bool IsInactive { get; set; }
}
