using GoWithFlow.Application.Common;
using GoWithFlow.Application.DTOs.Responses.User;
using GoWithFlow.Application.Interfaces.Repositories;
using GoWithFlow.Application.Interfaces.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace GoWithFlow.Application.Services;

public sealed class AudioArchiveService : IAudioArchiveService
{
	private readonly IAudioArchiveRepository _repository;
	private readonly IConfiguration _config;
	private readonly string _storagePath;

	// Retention period: 90 days
	private static readonly TimeSpan RetentionPeriod = TimeSpan.FromDays(90);

	// 10 MB max per clip
	private const long MaxFileSizeBytes = 10 * 1024 * 1024;

	public AudioArchiveService(IAudioArchiveRepository repository, IConfiguration config)
	{
		_repository  = repository;
		_config      = config;
		_storagePath = config["AudioArchive:StoragePath"] ?? "audio-archive";
	}

	public async Task<ApiResponse<AudioArchiveItemDto>> UploadClipAsync(IFormFile file, long sessionId, int turnIndex, long userId, string ipAddress, CancellationToken cancellationToken = default)
	{
		if (file is null || file.Length == 0)
			return ApiResponse<AudioArchiveItemDto>.FailureResult(new[] { "Audio file is required." }, "Upload failed.");

		if (file.Length > MaxFileSizeBytes)
			return ApiResponse<AudioArchiveItemDto>.FailureResult(new[] { "Audio file exceeds 10 MB limit." }, "Upload failed.");

		// Build a scoped storage path: audio-archive/{userId}/{sessionId}/turn_{turnIndex}_{timestamp}.webm
		var dir       = Path.Combine(_storagePath, userId.ToString(), sessionId.ToString());
		Directory.CreateDirectory(dir);
		var fileName  = $"turn_{turnIndex}_{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}.webm";
		var filePath  = Path.Combine(dir, fileName);
		var storageKey = Path.Combine(userId.ToString(), sessionId.ToString(), fileName);

		await using (var fs = new FileStream(filePath, FileMode.Create))
		{
			await file.CopyToAsync(fs, cancellationToken);
		}

		var expiresAt = DateTime.UtcNow.Add(RetentionPeriod);
		var archiveId = await _repository.InsertAsync(sessionId, userId, turnIndex, storageKey, 0, expiresAt, userId.ToString(), ipAddress, cancellationToken);

		var item = new AudioArchiveItemDto
		{
			ArchiveId    = archiveId,
			SessionId    = sessionId,
			TurnIndex    = turnIndex,
			AudioUrl     = storageKey,
			DurationSecs = 0,
			ExpiresAt    = expiresAt,
			DateCreated  = DateTime.UtcNow
		};

		return ApiResponse<AudioArchiveItemDto>.SuccessResult(item, "Audio clip archived successfully.");
	}

	public async Task<ApiResponse<List<AudioArchiveItemDto>>> GetSessionClipsAsync(long sessionId, long userId, CancellationToken cancellationToken = default)
	{
		if (sessionId <= 0 || userId <= 0)
			return ApiResponse<List<AudioArchiveItemDto>>.FailureResult(new[] { "Invalid parameters." }, "Validation failed.");

		var items = await _repository.GetBySessionAndUserAsync(sessionId, userId, cancellationToken);
		return ApiResponse<List<AudioArchiveItemDto>>.SuccessResult(items, "Audio clips retrieved.");
	}

	public async Task<ApiResponse<bool>> DeleteClipAsync(long archiveId, long userId, string deletedBy, CancellationToken cancellationToken = default)
	{
		if (archiveId <= 0 || userId <= 0)
			return ApiResponse<bool>.FailureResult(new[] { "Invalid parameters." }, "Validation failed.");

		// Get storage key so we can delete the file too
		var storageKey = await _repository.GetStorageKeyAsync(archiveId, userId, cancellationToken);

		await _repository.DeleteAsync(archiveId, userId, deletedBy, cancellationToken);

		if (storageKey is not null)
		{
			var filePath = Path.Combine(_storagePath, storageKey);
			if (File.Exists(filePath))
			{
				try { File.Delete(filePath); } catch { /* best-effort */ }
			}
		}

		return ApiResponse<bool>.SuccessResult(true, "Audio clip deleted.");
	}
}
