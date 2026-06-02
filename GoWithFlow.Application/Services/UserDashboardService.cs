using GoWithFlow.Application.Common;
using GoWithFlow.Application.DTOs.Responses.User;
using GoWithFlow.Application.Interfaces.Repositories;
using GoWithFlow.Application.Interfaces.Services;

namespace GoWithFlow.Application.Services;

public sealed class UserDashboardService : IUserDashboardService
{
	private readonly IUserRepository _userRepository;

	public UserDashboardService(IUserRepository userRepository)
	{
		_userRepository = userRepository;
	}

	public async Task<ApiResponse<UserDashboardResponseDto>> GetDashboardAsync(long userId, CancellationToken cancellationToken = default)
	{
		if (userId <= 0)
		{
			return ApiResponse<UserDashboardResponseDto>.FailureResult(new[] { "UserId must be greater than zero." }, "Validation failed.");
		}

		var dashboard = await _userRepository.GetUserDashboardAsync(userId, cancellationToken);

		if (dashboard is null)
		{
			return ApiResponse<UserDashboardResponseDto>.FailureResult(new[] { "User dashboard was not found." }, "User dashboard not found.");
		}

		return ApiResponse<UserDashboardResponseDto>.SuccessResult(dashboard, "User dashboard retrieved successfully.");
	}

	public async Task<ApiResponse<WeeklyReportResponseDto>> GetWeeklyReportAsync(long userId, CancellationToken cancellationToken = default)
	{
		if (userId <= 0)
		{
			return ApiResponse<WeeklyReportResponseDto>.FailureResult(new[] { "UserId must be greater than zero." }, "Validation failed.");
		}

		var report = await _userRepository.GetWeeklyReportAsync(userId, cancellationToken);
		return ApiResponse<WeeklyReportResponseDto>.SuccessResult(report, "Weekly report retrieved successfully.");
	}

	public async Task<ApiResponse<GuidedLearningPathResponseDto>> GetGuidedLearningPathAsync(long userId, CancellationToken cancellationToken = default)
	{
		if (userId <= 0)
		{
			return ApiResponse<GuidedLearningPathResponseDto>.FailureResult(new[] { "UserId must be greater than zero." }, "Validation failed.");
		}

		var path = await _userRepository.GetGuidedLearningPathAsync(userId, cancellationToken);
		return ApiResponse<GuidedLearningPathResponseDto>.SuccessResult(path, "Guided learning path retrieved successfully.");
	}

	public async Task<ApiResponse<InterviewPerformanceDashboardResponseDto>> GetInterviewPerformanceDashboardAsync(long userId, CancellationToken cancellationToken = default)
	{
		if (userId <= 0)
		{
			return ApiResponse<InterviewPerformanceDashboardResponseDto>.FailureResult(new[] { "UserId must be greater than zero." }, "Validation failed.");
		}

		var dashboard = await _userRepository.GetInterviewPerformanceDashboardAsync(userId, cancellationToken);
		return ApiResponse<InterviewPerformanceDashboardResponseDto>.SuccessResult(dashboard, "Interview performance dashboard retrieved successfully.");
	}

	public async Task<ApiResponse<PronunciationTimelineResponseDto>> GetPronunciationTimelineAsync(long userId, CancellationToken cancellationToken = default)
	{
		if (userId <= 0)
		{
			return ApiResponse<PronunciationTimelineResponseDto>.FailureResult(new[] { "UserId must be greater than zero." }, "Validation failed.");
		}

		var timeline = await _userRepository.GetPronunciationTimelineAsync(userId, cancellationToken);
		return ApiResponse<PronunciationTimelineResponseDto>.SuccessResult(timeline, "Pronunciation timeline retrieved successfully.");
	}
}
