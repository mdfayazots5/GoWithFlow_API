using GoWithFlow.Application.Common;
using GoWithFlow.Application.DTOs.Responses.User;
using Microsoft.AspNetCore.Http;

namespace GoWithFlow.Application.Interfaces.Services;

public interface IAudioArchiveService
{
	Task<ApiResponse<AudioArchiveItemDto>> UploadClipAsync(IFormFile file, long sessionId, int turnIndex, long userId, string ipAddress, CancellationToken cancellationToken = default);

	Task<ApiResponse<List<AudioArchiveItemDto>>> GetSessionClipsAsync(long sessionId, long userId, CancellationToken cancellationToken = default);

	Task<ApiResponse<bool>> DeleteClipAsync(long archiveId, long userId, string deletedBy, CancellationToken cancellationToken = default);
}
