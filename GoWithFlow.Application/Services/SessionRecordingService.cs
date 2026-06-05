using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.Json;
using GoWithFlow.Application.Common;
using GoWithFlow.Application.DTOs.Responses.Admin;
using GoWithFlow.Application.Helpers;
using GoWithFlow.Application.Interfaces.Repositories;
using GoWithFlow.Application.Interfaces.Services;
using GoWithFlow.Application.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GoWithFlow.Application.Services;

/// <summary>
/// Builds ONE consolidated .m4a per session by concatenating the ordered per-turn audio segments
/// (shared with the personal Audio Archive) via ffmpeg. See SessionRecordingArchitecture.md.
/// </summary>
public sealed class SessionRecordingService : ISessionRecordingService
{
	private static readonly TimeSpan RetentionPeriod = TimeSpan.FromDays(90);

	private readonly ISessionRecordingRepository _recordingRepository;
	private readonly IAudioArchiveRepository _audioArchiveRepository;
	private readonly ISessionRepository _sessionRepository;
	private readonly IStorageService _storageService;
	private readonly ISessionRecordingMergeQueue _mergeQueue;
	private readonly CloudflareR2Settings _r2Settings;
	private readonly ILogger<SessionRecordingService> _logger;
	private readonly string _ffmpegPath;
	private readonly string _ffprobePath;

	public SessionRecordingService(
		ISessionRecordingRepository recordingRepository,
		IAudioArchiveRepository audioArchiveRepository,
		ISessionRepository sessionRepository,
		IStorageService storageService,
		ISessionRecordingMergeQueue mergeQueue,
		IOptions<CloudflareR2Settings> r2Options,
		IConfiguration configuration,
		ILogger<SessionRecordingService> logger)
	{
		_recordingRepository    = recordingRepository;
		_audioArchiveRepository = audioArchiveRepository;
		_sessionRepository      = sessionRepository;
		_storageService         = storageService;
		_mergeQueue             = mergeQueue;
		_r2Settings             = r2Options.Value;
		_logger                 = logger;
		_ffmpegPath  = configuration["Ffmpeg:FfmpegPath"]  ?? "ffmpeg";
		_ffprobePath = configuration["Ffmpeg:FfprobePath"] ?? "ffprobe";
	}

	public async Task EnsureRecordingQueuedAsync(long sessionId, CancellationToken cancellationToken = default)
	{
		try
		{
			if (!await _recordingRepository.GetRecordingEnabledAsync(sessionId, cancellationToken))
			{
				return;
			}

			var session = await _sessionRepository.GetSessionBySessionIdAsync(sessionId, cancellationToken);
			var startedAt = session?.StartedDate ?? DateTime.UtcNow;
			var expiresAt = DateTime.UtcNow.Add(RetentionPeriod);

			await _recordingRepository.CreateOrGetAsync(sessionId, startedAt, expiresAt, cancellationToken);
			await _mergeQueue.EnqueueAsync(sessionId, cancellationToken);

			_logger.LogInformation("Session recording queued for merge. SessionId={SessionId}", sessionId);
		}
		catch (Exception ex)
		{
			// Recording must never break session completion — log and move on.
			_logger.LogError(ex, "Failed to queue session recording. SessionId={SessionId}", sessionId);
		}
	}

	public async Task MergeSessionAsync(long sessionId, CancellationToken cancellationToken = default)
	{
		var row = await _recordingRepository.GetBySessionAsync(sessionId, cancellationToken);
		if (row is null)
		{
			return;
		}

		// Single-merge idempotency guard: only one worker proceeds; READY rows are skipped.
		if (!await _recordingRepository.TryClaimForMergeAsync(row.RecordingId, cancellationToken))
		{
			return;
		}

		var workDir = Path.Combine(Path.GetTempPath(), $"gwf_rec_{sessionId}_{Guid.NewGuid():N}");

		try
		{
			var segments = await _audioArchiveRepository.GetAllBySessionAsync(sessionId, cancellationToken);
			segments = segments.Where(s => StorageKeyBuilder.IsR2Key(s.AudioUrl)).ToList();

			if (segments.Count == 0)
			{
				await _recordingRepository.MarkFailedAsync(row.RecordingId, "No audio segments were captured for this session.", cancellationToken);
				return;
			}

			Directory.CreateDirectory(workDir);
			var bucket = _r2Settings.Buckets.Audio;

			var inputPaths = new List<string>(segments.Count);
			for (var i = 0; i < segments.Count; i++)
			{
				var key = segments[i].AudioUrl;
				var ext = Path.GetExtension(key);
				if (string.IsNullOrEmpty(ext)) ext = ".webm";
				var localPath = Path.Combine(workDir, $"seg_{i:D4}{ext}");

				await using (var fileStream = File.Create(localPath))
				{
					await _storageService.DownloadToAsync(bucket, key, fileStream, cancellationToken);
				}
				inputPaths.Add(localPath);
			}

			var outputPath = Path.Combine(workDir, $"session_{sessionId}.m4a");
			await RunFfmpegConcatAsync(inputPaths, outputPath, cancellationToken);

			var durationSecs = await ProbeDurationSecondsAsync(outputPath, cancellationToken);
			var sizeBytes = new FileInfo(outputPath).Length;

			var finalKey = StorageKeyBuilder.SessionRecordingFinal(sessionId);
			await using (var uploadStream = File.OpenRead(outputPath))
			{
				await _storageService.UploadAsync(uploadStream, bucket, finalKey, "audio/mp4", cancellationToken);
			}

			var participantsJson = BuildParticipantsJson(segments);

			await _recordingRepository.MarkReadyAsync(
				row.RecordingId, finalKey, "m4a", durationSecs, sizeBytes, segments.Count, participantsJson, cancellationToken);

			_logger.LogInformation(
				"Session recording merged. SessionId={SessionId} Segments={Segments} DurationSecs={Duration} SizeBytes={Size}",
				sessionId, segments.Count, durationSecs, sizeBytes);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Session recording merge failed. SessionId={SessionId}", sessionId);
			await _recordingRepository.MarkFailedAsync(row.RecordingId, Truncate(ex.Message, 512), CancellationToken.None);
		}
		finally
		{
			TryCleanup(workDir);
		}
	}

	public async Task<ApiResponse<SessionRecordingDto?>> GetAdminSessionRecordingAsync(long sessionId, CancellationToken cancellationToken = default)
	{
		if (sessionId <= 0)
		{
			return ApiResponse<SessionRecordingDto?>.FailureResult(new[] { "Invalid sessionId." }, "Validation failed.");
		}

		var row = await _recordingRepository.GetBySessionAsync(sessionId, cancellationToken);
		if (row is null)
		{
			return ApiResponse<SessionRecordingDto?>.SuccessResult(null, "No recording exists for this session.");
		}

		var dto = new SessionRecordingDto
		{
			RecordingId   = row.RecordingId,
			SessionId     = row.SessionId,
			Status        = row.Status,
			Format        = row.Format,
			DurationSecs  = row.DurationSecs,
			SizeBytes     = row.SizeBytes,
			SegmentCount  = row.SegmentCount,
			FailureReason = row.FailureReason,
			CreatedAt     = row.CreatedAt,
			CompletedAt   = row.CompletedAt,
			Participants  = ParseParticipants(row.ParticipantsJson)
		};

		if (string.Equals(row.Status, "READY", StringComparison.OrdinalIgnoreCase) && StorageKeyBuilder.IsR2Key(row.StorageKey))
		{
			dto.AudioUrl = await _storageService.GetPresignedUrlAsync(
				_r2Settings.Buckets.Audio, row.StorageKey!, _r2Settings.PresignedUrlExpiryMinutes.Audio, cancellationToken);
		}

		return ApiResponse<SessionRecordingDto?>.SuccessResult(dto, "Session recording retrieved.");
	}

	public async Task<ApiResponse<bool>> SetRecordingEnabledAsync(long sessionId, long hostUserId, bool enabled, CancellationToken cancellationToken = default)
	{
		if (sessionId <= 0 || hostUserId <= 0)
		{
			return ApiResponse<bool>.FailureResult(new[] { "Invalid parameters." }, "Validation failed.");
		}

		var updated = await _recordingRepository.SetRecordingEnabledAsync(sessionId, hostUserId, enabled, cancellationToken);
		if (!updated)
		{
			return ApiResponse<bool>.FailureResult(new[] { "Only the session host can change recording, or the session was not found." }, "Update failed.");
		}

		return ApiResponse<bool>.SuccessResult(enabled, enabled ? "Session recording enabled." : "Session recording disabled.");
	}

	// --- ffmpeg helpers -----------------------------------------------------------------

	private async Task RunFfmpegConcatAsync(IReadOnlyList<string> inputPaths, string outputPath, CancellationToken ct)
	{
		var psi = new ProcessStartInfo
		{
			FileName               = _ffmpegPath,
			RedirectStandardError  = true,
			RedirectStandardOutput = true,
			UseShellExecute        = false,
			CreateNoWindow         = true
		};

		psi.ArgumentList.Add("-y");
		foreach (var path in inputPaths)
		{
			psi.ArgumentList.Add("-i");
			psi.ArgumentList.Add(path);
		}

		// Concat the N audio inputs in order, then loudness-normalize the result.
		var filter = new StringBuilder();
		for (var i = 0; i < inputPaths.Count; i++)
		{
			filter.Append($"[{i}:a]");
		}
		filter.Append($"concat=n={inputPaths.Count}:v=0:a=1,loudnorm[out]");

		psi.ArgumentList.Add("-filter_complex");
		psi.ArgumentList.Add(filter.ToString());
		psi.ArgumentList.Add("-map");
		psi.ArgumentList.Add("[out]");
		psi.ArgumentList.Add("-ar"); psi.ArgumentList.Add("48000");
		psi.ArgumentList.Add("-ac"); psi.ArgumentList.Add("1");
		psi.ArgumentList.Add("-c:a"); psi.ArgumentList.Add("aac");
		psi.ArgumentList.Add("-b:a"); psi.ArgumentList.Add("96k");
		psi.ArgumentList.Add("-movflags"); psi.ArgumentList.Add("+faststart");
		psi.ArgumentList.Add(outputPath);

		await RunProcessAsync(psi, "ffmpeg concat", ct);

		if (!File.Exists(outputPath) || new FileInfo(outputPath).Length == 0)
		{
			throw new InvalidOperationException("ffmpeg produced no output file.");
		}
	}

	private async Task<int> ProbeDurationSecondsAsync(string filePath, CancellationToken ct)
	{
		var psi = new ProcessStartInfo
		{
			FileName               = _ffprobePath,
			RedirectStandardError  = true,
			RedirectStandardOutput = true,
			UseShellExecute        = false,
			CreateNoWindow         = true
		};
		psi.ArgumentList.Add("-v"); psi.ArgumentList.Add("error");
		psi.ArgumentList.Add("-show_entries"); psi.ArgumentList.Add("format=duration");
		psi.ArgumentList.Add("-of"); psi.ArgumentList.Add("default=noprint_wrappers=1:nokey=1");
		psi.ArgumentList.Add(filePath);

		var stdout = await RunProcessAsync(psi, "ffprobe duration", ct);
		return double.TryParse(stdout.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out var seconds)
			? (int)Math.Round(seconds)
			: 0;
	}

	private static async Task<string> RunProcessAsync(ProcessStartInfo psi, string label, CancellationToken ct)
	{
		using var process = new Process { StartInfo = psi };
		try
		{
			process.Start();
		}
		catch (Exception ex)
		{
			throw new InvalidOperationException($"{label}: failed to start '{psi.FileName}'. Is ffmpeg installed and on PATH?", ex);
		}

		var stdoutTask = process.StandardOutput.ReadToEndAsync(ct);
		var stderrTask = process.StandardError.ReadToEndAsync(ct);
		await process.WaitForExitAsync(ct);

		var stdout = await stdoutTask;
		var stderr = await stderrTask;

		if (process.ExitCode != 0)
		{
			throw new InvalidOperationException($"{label} exited with code {process.ExitCode}. {Truncate(stderr, 400)}");
		}

		return stdout;
	}

	// --- participants -------------------------------------------------------------------

	private static string BuildParticipantsJson(IEnumerable<DTOs.Responses.User.AudioArchiveItemDto> segments)
	{
		var participants = segments
			.GroupBy(s => s.UserId)
			.Select(g => new SessionRecordingParticipantDto
			{
				UserId = g.Key,
				Name   = g.Select(x => x.UserName).FirstOrDefault(n => !string.IsNullOrWhiteSpace(n)) ?? string.Empty,
				Turns  = g.Select(x => x.TurnIndex).Distinct().Count()
			})
			.OrderBy(p => p.Name)
			.ToList();

		return JsonSerializer.Serialize(participants);
	}

	private static List<SessionRecordingParticipantDto> ParseParticipants(string? json)
	{
		if (string.IsNullOrWhiteSpace(json))
		{
			return new List<SessionRecordingParticipantDto>();
		}

		try
		{
			return JsonSerializer.Deserialize<List<SessionRecordingParticipantDto>>(json) ?? new();
		}
		catch (JsonException)
		{
			return new List<SessionRecordingParticipantDto>();
		}
	}

	private static string Truncate(string value, int max)
		=> string.IsNullOrEmpty(value) || value.Length <= max ? value ?? string.Empty : value[..max];

	private void TryCleanup(string dir)
	{
		try
		{
			if (Directory.Exists(dir))
			{
				Directory.Delete(dir, recursive: true);
			}
		}
		catch (Exception ex)
		{
			_logger.LogWarning(ex, "Failed to clean up temp dir {Dir}", dir);
		}
	}
}
