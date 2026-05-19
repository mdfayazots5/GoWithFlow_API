using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace GoWithFlow.Infrastructure.Data;

public sealed class GoWithFlowDbContextFactory : IDesignTimeDbContextFactory<GoWithFlowDbContext>
{
	public GoWithFlowDbContext CreateDbContext(string[] args)
	{
		AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

		var configuration = BuildConfiguration();
		var requestedProvider = ParseProviderArgument(args);
		var provider = DatabaseProviderNames.Normalize(requestedProvider ?? configuration["DatabaseProvider"]);
		var connectionString = configuration.GetConnectionString(provider)
			?? throw new InvalidOperationException($"ConnectionStrings:{provider} is missing for design-time DbContext creation.");

		var optionsBuilder = new DbContextOptionsBuilder<GoWithFlowDbContext>();

		if (DatabaseProviderNames.IsPostgreSql(provider))
		{
			optionsBuilder.UseNpgsql(
				connectionString,
				options => options.MigrationsHistoryTable("__EFMigrationsHistory", "public"));
		}
		else
		{
			optionsBuilder.UseSqlServer(
				connectionString,
				options => options.MigrationsHistoryTable("__EFMigrationsHistory", "dbo"));
		}

		return new GoWithFlowDbContext(optionsBuilder.Options, new DatabaseProviderSettings(provider));
	}

	private static IConfigurationRoot BuildConfiguration()
	{
		var currentDirectory = Directory.GetCurrentDirectory();
		var apiDirectory = ResolveApiDirectory(currentDirectory);

		return new ConfigurationBuilder()
			.SetBasePath(apiDirectory)
			.AddJsonFile("appsettings.json", optional: false)
			.AddJsonFile("appsettings.Development.json", optional: true)
			.AddEnvironmentVariables()
			.Build();
	}

	private static string ResolveApiDirectory(string currentDirectory)
	{
		var candidates = new[]
		{
			Path.Combine(currentDirectory, "..", "GoWithFlow.API"),
			Path.Combine(currentDirectory, "GoWithFlow.API"),
			Path.Combine(currentDirectory, "..", "..", "GoWithFlow.API")
		};

		foreach (var candidate in candidates)
		{
			var fullPath = Path.GetFullPath(candidate);

			if (File.Exists(Path.Combine(fullPath, "appsettings.json")))
			{
				return fullPath;
			}
		}

		throw new InvalidOperationException("Could not locate GoWithFlow.API/appsettings.json for design-time DbContext creation.");
	}

	private static string? ParseProviderArgument(string[] args)
	{
		for (var index = 0; index < args.Length - 1; index++)
		{
			if (string.Equals(args[index], "--provider", StringComparison.OrdinalIgnoreCase))
			{
				return args[index + 1];
			}
		}

		return null;
	}
}
