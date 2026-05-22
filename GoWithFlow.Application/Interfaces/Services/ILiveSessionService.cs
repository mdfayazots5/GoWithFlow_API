using GoWithFlow.Application.Common;
using GoWithFlow.Application.DTOs.Requests.LiveSession;
using GoWithFlow.Application.DTOs.Responses.LiveSession;

namespace GoWithFlow.Application.Interfaces.Services;

public interface ILiveSessionService
{
	Task<ApiResponse<TurnStateResponseDto>> GetCurrentTurnAsync(long sessionId, CancellationToken cancellationToken = default);

	Task<ApiResponse<TurnStateResponseDto>> ShiftTurnAsync(TurnShiftRequestDto dto, long userId, CancellationToken cancellationToken = default);

	Task<ApiResponse<VoiceAnalysisResponseDto>> SaveVoiceAnalysisAsync(SaveVoiceAnalysisRequestDto dto, long userId, CancellationToken cancellationToken = default);

	Task<ApiResponse<bool>> SubmitListenerFeedbackAsync(ListenerFeedbackRequestDto dto, long userId, CancellationToken cancellationToken = default);

	Task<ApiResponse<bool>> SubmitListenerFeedbackByTurnIndexAsync(long sessionId, string feedbackTag, int targetTurnIndex, long userId, CancellationToken cancellationToken = default);

	Task<ApiResponse<SessionSummaryResponseDto>> CompleteSessionAsync(long sessionId, CancellationToken cancellationToken = default);

	Task<ApiResponse<bool>> RequestReReadAsync(long sessionId, long userId, CancellationToken cancellationToken = default);

	Task MarkMemberLeftAsync(long sessionId, long userId, CancellationToken cancellationToken = default);
}
