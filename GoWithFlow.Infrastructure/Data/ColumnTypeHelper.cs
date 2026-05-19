namespace GoWithFlow.Infrastructure.Data;

public static class ColumnTypeHelper
{
	public static string Money(string provider)
		=> DatabaseProviderNames.IsPostgreSql(provider) ? "numeric(19,4)" : "money";

	public static string Decimal(string provider, int precision, int scale)
		=> DatabaseProviderNames.IsPostgreSql(provider)
			? $"numeric({precision},{scale})"
			: $"decimal({precision},{scale})";

	public static string LargeText(string provider)
		=> DatabaseProviderNames.IsPostgreSql(provider) ? "text" : "nvarchar(max)";

	public static string Json(string provider)
		=> DatabaseProviderNames.IsPostgreSql(provider) ? "jsonb" : "nvarchar(max)";

	public static string Timestamp(string provider)
		=> DatabaseProviderNames.IsPostgreSql(provider) ? "timestamp without time zone" : "datetime2";

	public static string Date(string provider)
		=> "date";

	public static string CurrentTimestampSql(string provider)
		=> DatabaseProviderNames.IsPostgreSql(provider) ? "NOW()" : "GETDATE()";

	public static string SoftDeleteFilter(string provider)
		=> DatabaseProviderNames.IsPostgreSql(provider) ? "\"IsDeleted\" = FALSE" : "[IsDeleted] = 0";

	public static string ActiveSoftDeleteFilter(string provider)
		=> DatabaseProviderNames.IsPostgreSql(provider)
			? "\"IsDeleted\" = FALSE AND \"IsActive\" = TRUE"
			: "[IsDeleted] = 0 AND [IsActive] = 1";
}
