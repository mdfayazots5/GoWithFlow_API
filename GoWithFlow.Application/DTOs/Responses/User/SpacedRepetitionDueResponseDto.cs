namespace GoWithFlow.Application.DTOs.Responses.User;

public sealed class SpacedRepetitionDueResponseDto
{
	public int DueCount { get; set; }

	public List<SpacedRepetitionDueItemDto> Items { get; set; } = new();
}

public sealed class SpacedRepetitionDueItemDto
{
	public long MistakeId { get; set; }

	public string MistakeType { get; set; } = string.Empty;

	public string? GrammarTag { get; set; }

	public string UtteranceText { get; set; } = string.Empty;

	public string? CorrectionText { get; set; }

	public byte ReviewStage { get; set; }

	public DateTime NextReviewDate { get; set; }

	public string SessionName { get; set; } = string.Empty;

	public string ScriptTitle { get; set; } = string.Empty;
}
