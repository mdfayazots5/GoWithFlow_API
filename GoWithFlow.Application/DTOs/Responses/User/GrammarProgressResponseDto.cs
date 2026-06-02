namespace GoWithFlow.Application.DTOs.Responses.User;

public sealed class GrammarProgressResponseDto
{
	public string GrammarTag { get; set; } = string.Empty;

	public int TotalMistakes { get; set; }

	public int ResolvedMistakes { get; set; }

	public decimal ImprovementPercent { get; set; }

	public int ProgressBarValue { get; set; }

	/// <summary>4-week rolling trend: Improving | Stable | Regressing. Null when insufficient data.</summary>
	public string? TrendLabel { get; set; }

	/// <summary>Average errors per session in the current 4-week period.</summary>
	public decimal CurrentPeriodAvg { get; set; }

	/// <summary>Average errors per session in the previous 4-week period.</summary>
	public decimal PreviousPeriodAvg { get; set; }

	/// <summary>Delta vs previous period (positive = more errors = regressing).</summary>
	public decimal TrendDelta { get; set; }
}
