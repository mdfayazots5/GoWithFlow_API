using GoWithFlow.Application.Common;
using GoWithFlow.Application.DTOs.Responses.Vocabulary;

namespace GoWithFlow.Application.Interfaces.Services;

public interface IVocabularyService
{
	/// <summary>
	/// Called after a VocabularySprint session completes.
	/// Reads the Learner's turns from tblUtterance (FocusWord column) and matches them
	/// against voice analysis to determine correct production. Saves to tblUserVocabulary.
	/// </summary>
	Task SaveSessionVocabularyAsync(long sessionId, long userId, CancellationToken cancellationToken = default);

	/// <summary>Returns the vocabulary bank for the authenticated user.</summary>
	Task<ApiResponse<VocabularyBankResponseDto>> GetVocabularyBankAsync(long userId, CancellationToken cancellationToken = default);

	/// <summary>
	/// Returns a session vocabulary summary (words practiced, bank totals) for the session completion
	/// response of a VocabularySprint session.
	/// </summary>
	Task<SessionVocabularySummaryDto?> GetSessionVocabularySummaryAsync(long sessionId, long userId, CancellationToken cancellationToken = default);
}
