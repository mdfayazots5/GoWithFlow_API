using GoWithFlow.Application.Common;
using GoWithFlow.Application.DTOs.Requests.User;
using GoWithFlow.Application.DTOs.Responses.User;

namespace GoWithFlow.Application.Interfaces.Services;

public interface IMistakeService
{
	Task<ApiResponse<bool>> SaveMistakesFromSessionAsync(long sessionId, long userId, CancellationToken cancellationToken = default);

	Task<ApiResponse<PagedResult<MistakeResponseDto>>> GetMistakesAsync(MistakeFilterRequestDto dto, long userId, CancellationToken cancellationToken = default);

	Task<ApiResponse<MistakeSummaryResponseDto>> GetMistakeSummaryAsync(long userId, CancellationToken cancellationToken = default);

	Task<ApiResponse<List<GrammarProgressResponseDto>>> GetGrammarProgressAsync(long userId, CancellationToken cancellationToken = default);
}
