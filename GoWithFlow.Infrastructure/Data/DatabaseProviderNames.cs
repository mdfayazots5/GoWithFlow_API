namespace GoWithFlow.Infrastructure.Data;

public static class DatabaseProviderNames
{
	public const string SqlServer = "SqlServer";
	public const string PostgreSQL = "PostgreSQL";

	public static string Normalize(string? provider)
	{
		if (string.Equals(provider, SqlServer, StringComparison.OrdinalIgnoreCase))
		{
			return SqlServer;
		}

		if (string.Equals(provider, PostgreSQL, StringComparison.OrdinalIgnoreCase))
		{
			return PostgreSQL;
		}

		throw new InvalidOperationException(
			$"Invalid DatabaseProvider '{provider ?? "<null>"}'. Supported values are '{SqlServer}' and '{PostgreSQL}'.");
	}

	public static bool IsPostgreSql(string provider)
	{
		return string.Equals(provider, PostgreSQL, StringComparison.OrdinalIgnoreCase);
	}
}
