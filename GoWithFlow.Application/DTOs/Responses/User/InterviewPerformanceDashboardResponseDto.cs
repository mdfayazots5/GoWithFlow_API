namespace GoWithFlow.Application.DTOs.Responses.User;

public sealed class InterviewSessionTimelineDto
{
    public long SessionId { get; set; }
    public DateTime SessionDate { get; set; }
    public decimal ReadinessScore { get; set; }
    public decimal FluencyScore { get; set; }
    public decimal ConfidenceScore { get; set; }
    public int MistakeCount { get; set; }
}

public sealed class InterviewGrammarWeaknessDto
{
    public string GrammarTag { get; set; } = string.Empty;
    public int ErrorCount { get; set; }
}

public sealed class InterviewFocusWordDto
{
    public string FocusWord { get; set; } = string.Empty;
    public int TimesSpoken { get; set; }
    public int TimesCorrect { get; set; }
    public decimal CorrectRate { get; set; }
}

public sealed class InterviewPerformanceDashboardResponseDto
{
    /// <summary>True when user has at least one completed MockInterview session.</summary>
    public bool HasData { get; set; }

    /// <summary>Composite 0–100 score across the last 5 MockInterview sessions.</summary>
    public decimal InterviewReadinessScore { get; set; }

    /// <summary>Trend label for readiness score: Improving / Stable / Declining.</summary>
    public string ReadinessTrend { get; set; } = string.Empty;

    /// <summary>Total completed MockInterview sessions for this user.</summary>
    public int TotalMockSessions { get; set; }

    /// <summary>Per-session timeline ordered oldest → newest (up to last 10 sessions).</summary>
    public List<InterviewSessionTimelineDto> SessionTimeline { get; set; } = new();

    /// <summary>Top 3 grammar error tags from Candidate turns.</summary>
    public List<InterviewGrammarWeaknessDto> TopGrammarErrors { get; set; } = new();

    /// <summary>Professional FocusWords spoken correctly vs stumbled on.</summary>
    public List<InterviewFocusWordDto> FocusWordPerformance { get; set; } = new();

    /// <summary>Trend of average Candidate answer length: Improving / Stable / Declining.</summary>
    public string AnswerLengthTrend { get; set; } = string.Empty;

    /// <summary>Average Candidate answer speed (WPM) across all sessions.</summary>
    public decimal AvgAnswerSpeedWpm { get; set; }

    /// <summary>Script recommended to target the weakest grammar area.</summary>
    public long RecommendedScriptId { get; set; }

    public string RecommendedScriptTitle { get; set; } = string.Empty;

    public string RecommendedScriptReason { get; set; } = string.Empty;
}
