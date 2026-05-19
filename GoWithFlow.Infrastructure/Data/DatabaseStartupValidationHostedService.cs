using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GoWithFlow.Infrastructure.Data;

public sealed class DatabaseStartupValidationHostedService : IHostedService
{
	private readonly IServiceProvider _serviceProvider;
	private readonly ILogger<DatabaseStartupValidationHostedService> _logger;

	public DatabaseStartupValidationHostedService(
		IServiceProvider serviceProvider,
		ILogger<DatabaseStartupValidationHostedService> logger)
	{
		_serviceProvider = serviceProvider;
		_logger = logger;
	}

	public async Task StartAsync(CancellationToken cancellationToken)
	{
		await using var scope = _serviceProvider.CreateAsyncScope();
		var dbContext = scope.ServiceProvider.GetRequiredService<GoWithFlowDbContext>();
		var provider = dbContext.DatabaseProvider;

		try
		{
			var canConnect = await dbContext.Database.CanConnectAsync(cancellationToken);

			if (canConnect == false)
			{
				throw new InvalidOperationException(
					$"Database provider '{provider}' failed connectivity validation. context.Database.CanConnectAsync() returned false.");
			}

			_logger.LogInformation("Database provider: {DatabaseProvider} - Connection: OK", provider);
		}
		catch (Exception exception)
		{
			throw new InvalidOperationException(
				$"Database provider '{provider}' failed startup connection validation. See inner exception for details.",
				exception);
		}
	}

	public Task StopAsync(CancellationToken cancellationToken)
	{
		return Task.CompletedTask;
	}
}
