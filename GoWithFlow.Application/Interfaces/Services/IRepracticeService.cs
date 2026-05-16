using GoWithFlow.Application.Common;
using GoWithFlow.Application.DTOs.Requests.User;
using GoWithFlow.Application.DTOs.Responses.User;

namespace GoWithFlow.Application.Interfaces.Services;

public interface IRepracticeService
{
	Task<ApiResponse<RepracticeSessionResponseDto>> GenerateRepracticeSessionAsync(GenerateRepracticeRequestDto dto, long userId, CancellationToken cancellationToken = default);

	Task<ApiResponse<RepracticeSessionResponseDto>> GetRepracticeSessionAsync(long repracticeSessionId, long userId, CancellationToken cancellationToken = default);

	Task<ApiResponse<PagedResult<RepracticeSessionResponseDto>>> GetRepracticeHistoryAsync(long userId, int pageNumber, int pageSize, CancellationToken cancellationToken = default);

	Task<ApiResponse<bool>> UpdateAttemptAsync(UpdateAttemptRequestDto dto, long userId, CancellationToken cancellationToken = default);

	Task<ApiResponse<bool>> CompleteRepracticeSessionAsync(long repracticeSessionId, long userId, CancellationToken cancellationToken = default);

	Task<ApiResponse<decimal>> GetImprovementPercentageAsync(long userId, CancellationToken cancellationToken = default);
}
