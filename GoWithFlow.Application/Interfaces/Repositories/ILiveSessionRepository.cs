using GoWithFlow.Application.DTOs.Responses.LiveSession;
using GoWithFlow.Domain.Entities;

namespace GoWithFlow.Application.Interfaces.Repositories;

public interface ILiveSessionRepository
{
	Task<long> InsertTurnStateAsync(TurnState turnState, CancellationToken cancellationToken = default);

	Task<TurnStateResponseDto?> GetCurrentTurnAsync(long sessionId, CancellationToken cancellationToken = default);

	Task<TurnState?> GetCurrentTurnEntityAsync(long sessionId, CancellationToken cancellationToken = default);

	Task<TurnState?> GetTurnBySessionAndTurnIndexAsync(long sessionId, int turnIndex, CancellationToken cancellationToken = default);

	Task UpdateTurnStatusAsync(long turnStateId, string turnStatus, string updatedBy, string ipAddress, CancellationToken cancellationToken = default);

	Task IncrementReReadCountAsync(long turnStateId, string updatedBy, string ipAddress, CancellationToken cancellationToken = default);

	Task<long> InsertVoiceAnalysisAsync(VoiceAnalysis voiceAnalysis, CancellationToken cancellationToken = default);

	Task<List<VoiceAnalysisResponseDto>> GetVoiceAnalysisBySessionIdAsync(long sessionId, CancellationToken cancellationToken = default);

	Task<List<VoiceAnalysisResponseDto>> GetVoiceAnalysisByUserIdAsync(long userId, long sessionId = 0, CancellationToken cancellationToken = default);

	Task InsertListenerFeedbackAsync(ListenerFeedback listenerFeedback, CancellationToken cancellationToken = default);

	Task<SessionSummaryResponseDto?> GetSessionCompletionSummaryAsync(long sessionId, CancellationToken cancellationToken = default);

	Task<List<SessionMember>> GetActiveSessionMembersBySessionIdAsync(long sessionId, CancellationToken cancellationToken = default);

	Task<SessionMember?> GetActiveSessionMemberByUserIdAsync(long sessionId, long userId, CancellationToken cancellationToken = default);

	Task<List<Utterance>> GetOrderedUtterancesBySessionIdAsync(long sessionId, CancellationToken cancellationToken = default);

	Task<bool> VoiceAnalysisExistsAsync(long sessionId, long userId, int turnIndex, CancellationToken cancellationToken = default);

	Task<bool> ListenerFeedbackExistsAsync(long sessionId, int turnIndex, long fromUserId, long targetUserId, string feedbackTag, CancellationToken cancellationToken = default);
}
