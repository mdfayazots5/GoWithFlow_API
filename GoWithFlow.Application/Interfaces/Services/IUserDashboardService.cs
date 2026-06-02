using GoWithFlow.Application.Common;
using GoWithFlow.Application.DTOs.Responses.User;

namespace GoWithFlow.Application.Interfaces.Services;

public interface IUserDashboardService
{
	Task<ApiResponse<UserDashboardResponseDto>> GetDashboardAsync(long userId, CancellationToken cancellationToken = default);

	Task<ApiResponse<WeeklyReportResponseDto>> GetWeeklyReportAsync(long userId, CancellationToken cancellationToken = default);

	Task<ApiResponse<GuidedLearningPathResponseDto>> GetGuidedLearningPathAsync(long userId, CancellationToken cancellationToken = default);

	Task<ApiResponse<InterviewPerformanceDashboardResponseDto>> GetInterviewPerformanceDashboardAsync(long userId, CancellationToken cancellationToken = default);

	Task<ApiResponse<PronunciationTimelineResponseDto>> GetPronunciationTimelineAsync(long userId, CancellationToken cancellationToken = default);
}
