using GoWithFlow.Application.Interfaces.Repositories;
using GoWithFlow.Application.Interfaces.Services;
using GoWithFlow.Application.Settings;
using Microsoft.Extensions.Options;

namespace GoWithFlow.API.BackgroundServices;

/// <summary>
/// Periodically removes consolidated session recordings past their 90-day ExpiresAt: deletes the
/// R2 object then soft-deletes the row. Runs once on startup, then daily.
/// </summary>
public sealed class SessionRecordingRetentionWorker : BackgroundService
{
	private static readonly TimeSpan Interval = TimeSpan.FromHours(24);
	private const int BatchSize = 200;

	private readonly IServiceScopeFactory _scopeFactory;
	private readonly IStorageService _storageService;
	private readonly CloudflareR2Settings _r2Settings;
	private readonly ILogger<SessionRecordingRetentionWorker> _logger;

	public SessionRecordingRetentionWorker(
		IServiceScopeFactory scopeFactory,
		IStorageService storageService,
		IOptions<CloudflareR2Settings> r2Options,
		ILogger<SessionRecordingRetentionWorker> logger)
	{
		_scopeFactory   = scopeFactory;
		_storageService = storageService;
		_r2Settings     = r2Options.Value;
		_logger         = logger;
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		using var timer = new PeriodicTimer(Interval);
		do
		{
			try
			{
				await PurgeExpiredAsync(stoppingToken);
			}
			catch (OperationCanceledException)
			{
				break;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Session recording retention sweep failed.");
			}
		}
		while (await timer.WaitForNextTickAsync(stoppingToken));
	}

	private async Task PurgeExpiredAsync(CancellationToken ct)
	{
		using var scope = _scopeFactory.CreateScope();
		var repository = scope.ServiceProvider.GetRequiredService<ISessionRecordingRepository>();
		var expired = await repository.GetExpiredAsync(BatchSize, ct);
		if (expired.Count == 0)
		{
			return;
		}

		foreach (var (recordingId, storageKey) in expired)
		{
			if (!string.IsNullOrEmpty(storageKey))
			{
				await _storageService.DeleteAsync(_r2Settings.Buckets.Audio, storageKey, ct);
			}
			await repository.SoftDeleteAsync(recordingId, ct);
		}

		_logger.LogInformation("Retention: purged {Count} expired session recording(s).", expired.Count);
	}
}
