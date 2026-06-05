using GoWithFlow.Application.Interfaces.Repositories;
using GoWithFlow.Application.Interfaces.Services;

namespace GoWithFlow.API.BackgroundServices;

/// <summary>
/// Consumes the session-recording merge queue and runs each merge in its own DI scope.
/// Processes sequentially to bound ffmpeg CPU/memory on the API host. On startup it re-enqueues
/// any PENDING_MERGE/FAILED rows so jobs lost to a restart are recovered.
/// </summary>
public sealed class SessionRecordingMergeWorker : BackgroundService
{
	private const int MaxMergeAttempts = 3;

	private readonly ISessionRecordingMergeQueue _queue;
	private readonly IServiceScopeFactory _scopeFactory;
	private readonly ILogger<SessionRecordingMergeWorker> _logger;

	public SessionRecordingMergeWorker(
		ISessionRecordingMergeQueue queue,
		IServiceScopeFactory scopeFactory,
		ILogger<SessionRecordingMergeWorker> logger)
	{
		_queue        = queue;
		_scopeFactory = scopeFactory;
		_logger       = logger;
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		await RecoverPendingAsync(stoppingToken);

		while (!stoppingToken.IsCancellationRequested)
		{
			long sessionId;
			try
			{
				sessionId = await _queue.DequeueAsync(stoppingToken);
			}
			catch (OperationCanceledException)
			{
				break;
			}

			try
			{
				using var scope = _scopeFactory.CreateScope();
				var service = scope.ServiceProvider.GetRequiredService<ISessionRecordingService>();
				await service.MergeSessionAsync(sessionId, stoppingToken);
			}
			catch (Exception ex)
			{
				// MergeSessionAsync already marks the row FAILED on its own errors; this guards the
				// worker loop from crashing on anything unexpected (e.g. scope resolution).
				_logger.LogError(ex, "Unhandled error merging session recording. SessionId={SessionId}", sessionId);
			}
		}
	}

	private async Task RecoverPendingAsync(CancellationToken ct)
	{
		try
		{
			using var scope = _scopeFactory.CreateScope();
			var repository = scope.ServiceProvider.GetRequiredService<ISessionRecordingRepository>();
			var pending = await repository.GetResumableSessionIdsAsync(MaxMergeAttempts, ct);

			foreach (var sessionId in pending)
			{
				await _queue.EnqueueAsync(sessionId, ct);
			}

			if (pending.Count > 0)
			{
				_logger.LogInformation("Re-enqueued {Count} pending session recording(s) on startup.", pending.Count);
			}
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to recover pending session recordings on startup.");
		}
	}
}
