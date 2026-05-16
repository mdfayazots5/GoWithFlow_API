namespace GoWithFlow.Application.DTOs.Responses.User;

public sealed class RepracticeSessionResponseDto
{
	public long RepracticeSessionId { get; set; }

	public long SourceSessionId { get; set; }

	public string Status { get; set; } = string.Empty;

	public int TotalMistakes { get; set; }

	public int CompletedRounds { get; set; }

	public decimal ImprovementPercent { get; set; }

	public DateTime GeneratedDate { get; set; }

	public List<RepracticeUtteranceResponseDto> Utterances { get; set; } = new();
}
