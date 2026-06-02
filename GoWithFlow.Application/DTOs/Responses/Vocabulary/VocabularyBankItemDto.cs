namespace GoWithFlow.Application.DTOs.Responses.Vocabulary;

public sealed class VocabularyBankItemDto
{
	public string FocusWord { get; set; } = string.Empty;

	public DateTime DateIntroduced { get; set; }

	public int TimesEncountered { get; set; }

	public int TimesCorrect { get; set; }

	/// <summary>Percentage of encounters where the word was produced correctly.</summary>
	public decimal CorrectRate { get; set; }

	/// <summary>True if the word has not been seen in any session for 7+ days.</summary>
	public bool IsDueForReview { get; set; }
}
