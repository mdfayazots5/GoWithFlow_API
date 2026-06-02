using GoWithFlow.Application.DTOs.Requests.User;
using GoWithFlow.Application.DTOs.Responses.User;

namespace GoWithFlow.Application.Interfaces.Repositories;

public interface IChallengeRepository
{
	Task<ActiveChallengeResponseDto> GetActiveChallengeAsync(long userId, CancellationToken cancellationToken = default);

	Task<long> InsertChallengeAttemptAsync(long challengeId, long userId, decimal score, string createdBy, string ipAddress, CancellationToken cancellationToken = default);

	Task<long> SetWeeklyChallengeAsync(long scriptId, string createdBy, string ipAddress, CancellationToken cancellationToken = default);
}
