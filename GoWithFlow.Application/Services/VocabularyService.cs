using GoWithFlow.Application.Common;
using GoWithFlow.Application.DTOs.Responses.Vocabulary;
using GoWithFlow.Application.Interfaces.Repositories;
using GoWithFlow.Application.Interfaces.Services;

namespace GoWithFlow.Application.Services;

public sealed class VocabularyService : IVocabularyService
{
	private readonly IVocabularyRepository _vocabularyRepository;
	private readonly ILiveSessionRepository _liveSessionRepository;
	private readonly ISessionRepository _sessionRepository;

	public VocabularyService(
		IVocabularyRepository vocabularyRepository,
		ILiveSessionRepository liveSessionRepository,
		ISessionRepository sessionRepository)
	{
		_vocabularyRepository = vocabularyRepository;
		_liveSessionRepository = liveSessionRepository;
		_sessionRepository = sessionRepository;
	}

	public async Task SaveSessionVocabularyAsync(long sessionId, long userId, CancellationToken cancellationToken = default)
	{
		// Get all utterances for this session — we need Learner/performance turns with FocusWord
		var utterances = await _liveSessionRepository.GetOrderedUtterancesBySessionIdAsync(sessionId, cancellationToken);

		// Get voice analysis for this user + session — to check if FocusWord was spoken correctly
		var voiceAnalysisList = await _liveSessionRepository.GetVoiceAnalysisByUserIdAsync(userId, sessionId, cancellationToken);
		var vaDictionary = voiceAnalysisList.ToDictionary(v => v.UtteranceId);

		// Determine category to filter to Learner/performance turns only
		var session = await _sessionRepository.GetSessionBySessionIdAsync(sessionId, cancellationToken);
		if (session is null)
			return;

		// Get script category from the session's utterances (we can't fetch directly without script join here)
		// Use utterance speaker labels — for VocabularySprint, performance speaker is 'Learner'
		var performanceSpeakers = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Learner" };

		foreach (var utterance in utterances)
		{
			// Only process Learner turns with a FocusWord
			if (!performanceSpeakers.Contains(utterance.SpeakerLabel ?? string.Empty))
				continue;

			if (string.IsNullOrWhiteSpace(utterance.FocusWord))
				continue;

			// Determine if the Learner produced the FocusWord correctly in their voice analysis
			var wasCorrect = false;
			if (vaDictionary.TryGetValue(utterance.UtteranceId, out var va))
			{
				// Word was produced correctly if: fluency score >= 60 AND no grammar errors for the focus word
				// Simplified check: if overall score >= 60, mark as correctly produced
				wasCorrect = va.OverallScore >= 60;
			}

			await _vocabularyRepository.UpsertVocabularyWordAsync(
				userId,
				utterance.FocusWord.Trim().ToLowerInvariant(),
				sessionId,
				wasCorrect,
				cancellationToken);
		}
	}

	public async Task<ApiResponse<VocabularyBankResponseDto>> GetVocabularyBankAsync(long userId, CancellationToken cancellationToken = default)
	{
		if (userId <= 0)
		{
			return ApiResponse<VocabularyBankResponseDto>.FailureResult(new[] { "UserId must be greater than zero." }, "Validation failed.");
		}

		var bank = await _vocabularyRepository.GetVocabularyBankAsync(userId, cancellationToken);
		return ApiResponse<VocabularyBankResponseDto>.SuccessResult(bank, "Vocabulary bank retrieved successfully.");
	}

	public async Task<SessionVocabularySummaryDto?> GetSessionVocabularySummaryAsync(long sessionId, long userId, CancellationToken cancellationToken = default)
	{
		// Get the utterances for this session to count Learner FocusWords
		var utterances = await _liveSessionRepository.GetOrderedUtterancesBySessionIdAsync(sessionId, cancellationToken);

		var wordsThisSession = utterances
			.Where(u => string.Equals(u.SpeakerLabel, "Learner", StringComparison.OrdinalIgnoreCase)
			            && !string.IsNullOrWhiteSpace(u.FocusWord))
			.Select(u => u.FocusWord!.Trim().ToLowerInvariant())
			.Distinct()
			.ToList();

		if (wordsThisSession.Count == 0)
			return null;

		var (totalWords, dueForReview) = await _vocabularyRepository.GetVocabularyBankTotalsAsync(userId, cancellationToken);

		return new SessionVocabularySummaryDto
		{
			WordsPracticedThisSession = wordsThisSession.Count,
			TotalWordsInBank          = totalWords,
			WordsDueForReview         = dueForReview,
			WordsPracticed            = wordsThisSession
		};
	}
}
