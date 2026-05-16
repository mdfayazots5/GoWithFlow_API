using GoWithFlow.Application.Common;
using GoWithFlow.Application.DTOs.Responses.User;
using GoWithFlow.Domain.Entities;

namespace GoWithFlow.Application.Interfaces.Repositories;

public interface IRepracticeRepository
{
	Task<long> InsertRepracticeSessionAsync(RepracticeSession repracticeSession, CancellationToken cancellationToken = default);

	Task<long> InsertRepracticeUtteranceAsync(RepracticeUtterance repracticeUtterance, CancellationToken cancellationToken = default);

	Task<RepracticeSessionResponseDto?> GetRepracticeSessionByIdAsync(long repracticeSessionId, CancellationToken cancellationToken = default);

	Task<PagedResult<RepracticeSessionResponseDto>> GetRepracticeHistoryAsync(long userId, int pageNumber, int pageSize, CancellationToken cancellationToken = default);

	Task<RepracticeSession?> GetRepracticeSessionEntityAsync(long repracticeSessionId, CancellationToken cancellationToken = default);

	Task<RepracticeUtterance?> GetRepracticeUtteranceEntityAsync(long repracticeUtteranceId, CancellationToken cancellationToken = default);

	Task UpdateRepracticeUtteranceAttemptAsync(long repracticeUtteranceId, decimal score, string updatedBy, string ipAddress, CancellationToken cancellationToken = default);

	Task<decimal> CalculateImprovementPercentageAsync(long userId, CancellationToken cancellationToken = default);

	Task UpdateRepracticeSessionStatusAsync(long repracticeSessionId, string status, decimal improvementPercent, string updatedBy, string ipAddress, CancellationToken cancellationToken = default);
}
