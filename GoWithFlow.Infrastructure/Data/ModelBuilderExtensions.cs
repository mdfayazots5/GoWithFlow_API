using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace GoWithFlow.Infrastructure.Data;

public static class ModelBuilderExtensions
{
	public static void ApplyProviderConventions(this ModelBuilder modelBuilder, string provider)
	{
		foreach (var entityType in modelBuilder.Model.GetEntityTypes())
		{
			if (entityType.ClrType is null)
			{
				continue;
			}

			var entityBuilder = modelBuilder.Entity(entityType.ClrType);

			// PostgreSQL: lowercase table names to match SQL-script-created schema (e.g. "tblUser" → "tbluser")
			if (DatabaseProviderNames.IsPostgreSql(provider))
			{
				var tableName = entityType.GetTableName();
				if (!string.IsNullOrEmpty(tableName))
				{
					entityBuilder.ToTable(tableName.ToLowerInvariant());
				}
			}

			foreach (var property in entityType.GetProperties())
			{
				var clrType = Nullable.GetUnderlyingType(property.ClrType) ?? property.ClrType;
				var propertyBuilder = entityBuilder.Property(property.Name);

				if (clrType == typeof(DateTime))
				{
					propertyBuilder.HasColumnType(ColumnTypeHelper.Timestamp(provider));

					if (HasCurrentTimestampDefault(property))
					{
						propertyBuilder.HasDefaultValueSql(ColumnTypeHelper.CurrentTimestampSql(provider));
					}
				}
				else if (clrType == typeof(DateOnly))
				{
					propertyBuilder.HasColumnType(ColumnTypeHelper.Date(provider));
				}
				else if (clrType == typeof(decimal))
				{
					propertyBuilder.HasColumnType(ResolveDecimalColumnType(provider, property.Name));
				}
				else if (clrType == typeof(string) && property.Name.EndsWith("Json", StringComparison.Ordinal))
				{
					propertyBuilder.HasColumnType(ColumnTypeHelper.Json(provider));
				}

				// PostgreSQL: lowercase column names to match SQL-script-created schema (e.g. "UserId" → "userid")
				if (DatabaseProviderNames.IsPostgreSql(provider))
				{
					propertyBuilder.HasColumnName(property.Name.ToLowerInvariant());
				}
			}

			foreach (var index in entityType.GetIndexes())
			{
				var filter = index.GetFilter();

				if (string.IsNullOrWhiteSpace(filter))
				{
					continue;
				}

				if (string.Equals(filter, "[IsDeleted] = 0", StringComparison.OrdinalIgnoreCase))
				{
					index.SetFilter(ColumnTypeHelper.SoftDeleteFilter(provider));
					continue;
				}

				if (string.Equals(filter, "[IsDeleted] = 0 AND [IsActive] = 1", StringComparison.OrdinalIgnoreCase))
				{
					index.SetFilter(ColumnTypeHelper.ActiveSoftDeleteFilter(provider));
				}
			}
		}
	}

	private static bool HasCurrentTimestampDefault(IMutableProperty property)
	{
		return string.Equals(property.GetDefaultValueSql(), "GETDATE()", StringComparison.OrdinalIgnoreCase) ||
			string.Equals(property.GetDefaultValueSql(), "NOW()", StringComparison.OrdinalIgnoreCase);
	}

	private static string ResolveDecimalColumnType(string provider, string propertyName)
	{
		return propertyName switch
		{
			"FluencyScore" or
			"ConfidenceScore" or
			"OverallScore" or
			"ImprovementPercent" or
			"BestScore" or
			"LastScore" => ColumnTypeHelper.Decimal(provider, 5, 2),
			_ => ColumnTypeHelper.Money(provider)
		};
	}
}
