namespace GoWithFlow.Application.DTOs.Responses.Vocabulary;

public sealed class VocabularyBankResponseDto
{
	public int TotalWords { get; set; }

	public int DueForReviewCount { get; set; }

	public List<VocabularyBankItemDto> Words { get; set; } = new();
}
