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

	/// <summary>
	/// Returns a member record regardless of IsActive state (active or inactive, not soft-deleted).
	/// Used to detect page-refresh reconnects and voice-analysis saves after a temporary disconnect.
	/// </summary>
	Task<SessionMember?> GetSessionMemberByUserIdAsync(long sessionId, long userId, CancellationToken cancellationToken = default);

	/// <summary>
	/// Sets IsActive = true for a member who was deactivated by a WebSocket disconnect.
	/// Called on hub reconnect to restore a member who refreshed the page mid-session.
	/// </summary>
	Task ReactivateMemberAsync(long sessionId, long userId, CancellationToken cancellationToken = default);

	Task<List<Utterance>> GetOrderedUtterancesBySessionIdAsync(long sessionId, CancellationToken cancellationToken = default);

	Task<bool> VoiceAnalysisExistsAsync(long sessionId, long userId, int turnIndex, CancellationToken cancellationToken = default);

	/// <summary>
	/// Returns the existing VoiceAnalysis entity for a specific session + user + turn, or null if none exists.
	/// Used by the UPSERT path in SaveVoiceAnalysisAsync to handle re-recording on a non-completed turn.
	/// </summary>
	Task<VoiceAnalysis?> GetVoiceAnalysisByUserTurnAsync(long sessionId, long userId, int turnIndex, CancellationToken cancellationToken = default);

	/// <summary>
	/// Updates all score and transcript fields on an existing VoiceAnalysis row.
	/// Called when the speaker re-records an active turn (e.g. after page refresh) and a record already exists.
	/// </summary>
	Task UpdateVoiceAnalysisAsync(long voiceAnalysisId, VoiceAnalysis updates, string updatedBy, CancellationToken cancellationToken = default);

	Task<bool> ListenerFeedbackExistsAsync(long sessionId, int turnIndex, long fromUserId, long targetUserId, string feedbackTag, CancellationToken cancellationToken = default);
}
