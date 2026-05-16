namespace GoWithFlow.Application.DTOs.Responses.User;

public sealed class RepracticeUtteranceResponseDto
{
	public long RepracticeUtteranceId { get; set; }

	public long MistakeId { get; set; }

	public long OriginalUtteranceId { get; set; }

	public string EnglishText { get; set; } = string.Empty;

	public string? HintText { get; set; }

	public string MistakeType { get; set; } = string.Empty;

	public string? MistakeDetail { get; set; }

	public string? CorrectionNote { get; set; }

	public int AttemptCount { get; set; }

	public decimal BestScore { get; set; }

	public decimal LastScore { get; set; }

	public bool IsResolved { get; set; }
}
