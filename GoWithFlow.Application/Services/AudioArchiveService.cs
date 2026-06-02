using GoWithFlow.Application.Common;
using GoWithFlow.Application.DTOs.Responses.User;
using GoWithFlow.Application.Helpers;
using GoWithFlow.Application.Interfaces.Repositories;
using GoWithFlow.Application.Interfaces.Services;
using GoWithFlow.Application.Settings;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace GoWithFlow.Application.Services;

public sealed class AudioArchiveService : IAudioArchiveService
{
	private readonly IAudioArchiveRepository _repository;
	private readonly IStorageService _storageService;
	private readonly CloudflareR2Settings _r2Settings;

	// Retention period: 90 days
	private static readonly TimeSpan RetentionPeriod = TimeSpan.FromDays(90);

	// 10 MB max per clip
	private const long MaxFileSizeBytes = 10 * 1024 * 1024;

	public AudioArchiveService(
		IAudioArchiveRepository repository,
		IStorageService storageService,
		IOptions<CloudflareR2Settings> r2Options)
	{
		_repository    = repository;
		_storageService = storageService;
		_r2Settings    = r2Options.Value;
	}

	public async Task<ApiResponse<AudioArchiveItemDto>> UploadClipAsync(
		IFormFile file, long sessionId, int turnIndex, long userId,
		string ipAddress, CancellationToken cancellationToken = default)
	{
		if (file is null || file.Length == 0)
			return ApiResponse<AudioArchiveItemDto>.FailureResult(new[] { "Audio file is required." }, "Upload failed.");

		if (file.Length > MaxFileSizeBytes)
			return ApiResponse<AudioArchiveItemDto>.FailureResult(new[] { "Audio file exceeds 10 MB limit." }, "Upload failed.");

		var objectKey = StorageKeyBuilder.AudioArchiveClip(sessionId, turnIndex, userId);
		var bucket    = _r2Settings.Buckets.Audio;

		await using var stream = file.OpenReadStream();
		await _storageService.UploadAsync(stream, bucket, objectKey, file.ContentType ?? "audio/webm", cancellationToken);

		var expiresAt = DateTime.UtcNow.Add(RetentionPeriod);
		var archiveId = await _repository.InsertAsync(
			sessionId, userId, turnIndex, objectKey, 0, expiresAt,
			userId.ToString(), ipAddress, cancellationToken);

		var presignedUrl = await _storageService.GetPresignedUrlAsync(
			bucket, objectKey, _r2Settings.PresignedUrlExpiryMinutes.Audio, cancellationToken);

		return ApiResponse<AudioArchiveItemDto>.SuccessResult(new AudioArchiveItemDto
		{
			ArchiveId    = archiveId,
			SessionId    = sessionId,
			TurnIndex    = turnIndex,
			AudioUrl     = presignedUrl,
			DurationSecs = 0,
			ExpiresAt    = expiresAt,
			DateCreated  = DateTime.UtcNow
		}, "Audio clip archived successfully.");
	}

	public async Task<ApiResponse<List<AudioArchiveItemDto>>> GetSessionClipsAsync(
		long sessionId, long userId, CancellationToken cancellationToken = default)
	{
		if (sessionId <= 0 || userId <= 0)
			return ApiResponse<List<AudioArchiveItemDto>>.FailureResult(new[] { "Invalid parameters." }, "Validation failed.");

		var items = await _repository.GetBySessionAndUserAsync(sessionId, userId, cancellationToken);

		// AudioUrl from DB is the R2 object key — replace with a fresh presigned URL
		var bucket = _r2Settings.Buckets.Audio;
		foreach (var item in items)
		{
			if (StorageKeyBuilder.IsR2Key(item.AudioUrl))
			{
				item.AudioUrl = await _storageService.GetPresignedUrlAsync(
					bucket, item.AudioUrl, _r2Settings.PresignedUrlExpiryMinutes.Audio, cancellationToken);
			}
		}

		return ApiResponse<List<AudioArchiveItemDto>>.SuccessResult(items, "Audio clips retrieved.");
	}

	public async Task<ApiResponse<bool>> DeleteClipAsync(
		long archiveId, long userId, string deletedBy, CancellationToken cancellationToken = default)
	{
		if (archiveId <= 0 || userId <= 0)
			return ApiResponse<bool>.FailureResult(new[] { "Invalid parameters." }, "Validation failed.");

		var storageKey = await _repository.GetStorageKeyAsync(archiveId, userId, cancellationToken);

		await _repository.DeleteAsync(archiveId, userId, deletedBy, cancellationToken);

		if (StorageKeyBuilder.IsR2Key(storageKey))
		{
			await _storageService.DeleteAsync(_r2Settings.Buckets.Audio, storageKey!, cancellationToken);
		}

		return ApiResponse<bool>.SuccessResult(true, "Audio clip deleted.");
	}
}
