using GoWithFlow.Application.Common;
using GoWithFlow.Application.DTOs.Requests.User;
using GoWithFlow.Application.DTOs.Responses.User;
using GoWithFlow.Domain.Entities;

namespace GoWithFlow.Application.Interfaces.Repositories;

public interface IMistakeRepository
{
	Task<List<VoiceAnalysis>> GetVoiceAnalysesBySessionAndUserIdAsync(long sessionId, long userId, CancellationToken cancellationToken = default);

	Task<bool> MistakeExistsAsync(long userId, long sessionId, long utteranceId, string mistakeType, string? mistakeDetail, CancellationToken cancellationToken = default);

	Task<long> InsertMistakeAsync(Mistake mistake, CancellationToken cancellationToken = default);

	Task<PagedResult<MistakeResponseDto>> GetMistakesAsync(MistakeFilterRequestDto dto, long userId, CancellationToken cancellationToken = default);

	Task<MistakeSummaryResponseDto> GetMistakeSummaryAsync(long userId, CancellationToken cancellationToken = default);

	Task<List<GrammarProgressResponseDto>> GetGrammarProgressAsync(long userId, CancellationToken cancellationToken = default);

	Task<List<Mistake>> GetUnresolvedMistakesAsync(long userId, long sourceSessionId, CancellationToken cancellationToken = default);
}
