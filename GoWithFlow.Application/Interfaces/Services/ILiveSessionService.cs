using GoWithFlow.Application.Common;
using GoWithFlow.Application.DTOs.Requests.LiveSession;
using GoWithFlow.Application.DTOs.Responses.LiveSession;

namespace GoWithFlow.Application.Interfaces.Services;

public interface ILiveSessionService
{
	Task<ApiResponse<TurnStateResponseDto>> GetCurrentTurnAsync(long sessionId, CancellationToken cancellationToken = default);

	Task<ApiResponse<TurnStateResponseDto>> ShiftTurnAsync(TurnShiftRequestDto dto, long userId, CancellationToken cancellationToken = default);

	/// <summary>
	/// Phase 17 — advances a turn that is currently held by the AI Voice Participant. Callable by any
	/// active HUMAN member of the session (the AI never calls the hub). Performs no scoring and writes
	/// no voice analysis; otherwise reuses the same atomic resolve-and-advance path as ShiftTurnAsync.
	/// </summary>
	Task<ApiResponse<TurnStateResponseDto>> AdvanceAiTurnAsync(long sessionId, int turnIndex, long callerUserId, CancellationToken cancellationToken = default);

	Task<ApiResponse<VoiceAnalysisResponseDto>> SaveVoiceAnalysisAsync(SaveVoiceAnalysisRequestDto dto, long userId, CancellationToken cancellationToken = default);

	Task<ApiResponse<bool>> SubmitListenerFeedbackAsync(ListenerFeedbackRequestDto dto, long userId, CancellationToken cancellationToken = default);

	Task<ApiResponse<bool>> SubmitListenerFeedbackByTurnIndexAsync(long sessionId, string feedbackTag, int targetTurnIndex, long userId, CancellationToken cancellationToken = default);

	Task<ApiResponse<SessionSummaryResponseDto>> CompleteSessionAsync(long sessionId, CancellationToken cancellationToken = default);

	Task<ApiResponse<bool>> RequestReReadAsync(long sessionId, long userId, CancellationToken cancellationToken = default);

	Task MarkMemberLeftAsync(long sessionId, long userId, CancellationToken cancellationToken = default);

	/// <summary>
	/// Restores IsActive = true for a member deactivated by an OnDisconnectedAsync race (page refresh).
	/// Called from LiveSessionHub on reconnect before tracking the connection.
	/// </summary>
	Task ReactivateMemberAsync(long sessionId, long userId, CancellationToken cancellationToken = default);

	/// <summary>
	/// Returns the full post-session review transcript with per-turn voice analysis for the caller.
	/// </summary>
	Task<ApiResponse<SessionReviewResponseDto>> GetSessionReviewAsync(long sessionId, long userId, CancellationToken cancellationToken = default);
}
