namespace GoWithFlow.Application.DTOs.Responses.User;

public sealed class PronunciationSessionEntryDto
{
    public long SessionId { get; set; }
    public DateTime SessionDate { get; set; }
    /// <summary>True when this word appeared as a pronunciation issue in this session.</summary>
    public bool HadIssue { get; set; }
    public string IssueNote { get; set; } = string.Empty;
}

public sealed class PronunciationProblemWordDto
{
    public string Word { get; set; } = string.Empty;
    /// <summary>Total sessions where this word was flagged as a pronunciation issue.</summary>
    public int TotalOccurrences { get; set; }
    /// <summary>IPA reference from tblUtterance.PronunciationNote where FocusWord matches.</summary>
    public string IpaReference { get; set; } = string.Empty;
    /// <summary>True when word had issues in 3 or more of the last 10 sessions.</summary>
    public bool IsPersistent { get; set; }
    /// <summary>Script recommended to practice this word (has it as a FocusWord).</summary>
    public long PracticeScriptId { get; set; }
    public string PracticeScriptTitle { get; set; } = string.Empty;
    /// <summary>Per-session timeline ordered oldest → newest.</summary>
    public List<PronunciationSessionEntryDto> SessionHistory { get; set; } = new();
}

public sealed class PronunciationTimelineResponseDto
{
    public bool HasData { get; set; }
    /// <summary>All problem words ordered by frequency descending.</summary>
    public List<PronunciationProblemWordDto> ProblemWords { get; set; } = new();
    /// <summary>Top 5 words with issues in 3+ of last 10 sessions.</summary>
    public List<PronunciationProblemWordDto> TopPersistentWords { get; set; } = new();
}
