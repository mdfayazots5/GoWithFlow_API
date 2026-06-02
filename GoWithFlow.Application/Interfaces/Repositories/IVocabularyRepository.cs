using GoWithFlow.Application.DTOs.Responses.Vocabulary;

namespace GoWithFlow.Application.Interfaces.Repositories;

public interface IVocabularyRepository
{
	/// <summary>
	/// Inserts or updates a vocabulary record.
	/// If UserId + FocusWord + SourceSessionId already exists, increments TimesEncountered
	/// and updates WasProducedCorrectly if wasCorrect is true.
	/// </summary>
	Task UpsertVocabularyWordAsync(long userId, string focusWord, long sourceSessionId, bool wasProducedCorrectly, CancellationToken cancellationToken = default);

	/// <summary>
	/// Returns all vocabulary words in the user's bank, aggregated per word across all sessions.
	/// Words not seen in 7+ days are flagged IsDueForReview = true.
	/// </summary>
	Task<VocabularyBankResponseDto> GetVocabularyBankAsync(long userId, CancellationToken cancellationToken = default);

	/// <summary>
	/// Returns the count of words in the user's vocabulary bank and how many are due for review.
	/// Used for the session completion summary.
	/// </summary>
	Task<(int TotalWords, int DueForReview)> GetVocabularyBankTotalsAsync(long userId, CancellationToken cancellationToken = default);
}
