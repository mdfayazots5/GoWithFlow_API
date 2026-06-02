using GoWithFlow.Application.DTOs.Responses.User;

namespace GoWithFlow.Application.Interfaces.Repositories;

public interface IAudioArchiveRepository
{
	Task<long> InsertAsync(long sessionId, long userId, int turnIndex, string storageKey, int durationSecs, DateTime expiresAt, string createdBy, string ipAddress, CancellationToken cancellationToken = default);

	Task<List<AudioArchiveItemDto>> GetBySessionAndUserAsync(long sessionId, long userId, CancellationToken cancellationToken = default);

	Task DeleteAsync(long archiveId, long userId, string deletedBy, CancellationToken cancellationToken = default);

	Task<string?> GetStorageKeyAsync(long archiveId, long userId, CancellationToken cancellationToken = default);
}
