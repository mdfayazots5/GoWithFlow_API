using GoWithFlow.Domain.Entities;

namespace GoWithFlow.Application.Interfaces.Repositories;

public interface IRefreshTokenRepository : IGenericRepository<RefreshToken>
{
	Task<long> InsertRefreshTokenAsync(RefreshToken refreshToken, CancellationToken cancellationToken = default);

	Task<RefreshToken?> GetByTokenAsync(string token, CancellationToken cancellationToken = default);

	Task RevokeRefreshTokenAsync(string token, string updatedBy, string ipAddress, CancellationToken cancellationToken = default);
}
