using System.Data;
using System.Data.Common;
using System.Text;
using Microsoft.Data.SqlClient;
using Npgsql;
using NpgsqlTypes;

namespace GoWithFlow.Infrastructure.Data;

public static class DbCommandHelper
{
	public static DbParameter CreateParameter(string provider, string parameterName, object? value)
	{
		return CreateParameter(provider, parameterName, value, null, ParameterDirection.Input, 0);
	}

	public static DbParameter CreateParameter(
		string provider,
		string parameterName,
		object? value,
		DbType? dbType,
		ParameterDirection direction,
		int size)
	{
		var normalizedName = NormalizeParameterName(provider, parameterName);
		DbParameter parameter = DatabaseProviderNames.IsPostgreSql(provider)
			? new NpgsqlParameter()
			: new SqlParameter();

		parameter.ParameterName = normalizedName;
		parameter.Value = value ?? DBNull.Value;
		parameter.Direction = direction;

		if (dbType.HasValue)
		{
			parameter.DbType = dbType.Value;
		}

		if (size > 0)
		{
			parameter.Size = size;
		}

		return parameter;
	}

	public static DbParameter CreateJsonParameter(string provider, string parameterName, string json)
	{
		if (DatabaseProviderNames.IsPostgreSql(provider))
		{
			return new NpgsqlParameter(NormalizeParameterName(provider, parameterName), NpgsqlDbType.Jsonb)
			{
				Value = json
			};
		}

		return new SqlParameter(parameterName, json);
	}

	public static string QualifyRoutineName(string provider, string sqlServerRoutineName)
	{
		if (DatabaseProviderNames.IsPostgreSql(provider) == false)
		{
			return sqlServerRoutineName;
		}

		var baseName = sqlServerRoutineName.Split('.', StringSplitOptions.RemoveEmptyEntries).Last();
		return $"public.{ToSnakeCase(baseName)}";
	}

	private static string NormalizeParameterName(string provider, string parameterName)
	{
		return DatabaseProviderNames.IsPostgreSql(provider)
			? parameterName.TrimStart('@')
			: parameterName;
	}

	private static string ToSnakeCase(string value)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			return value;
		}

		var builder = new StringBuilder(value.Length + 8);

		for (var index = 0; index < value.Length; index++)
		{
			var character = value[index];

			if (char.IsUpper(character))
			{
				var hasPrevious = index > 0;
				var nextIsLower = index + 1 < value.Length && char.IsLower(value[index + 1]);
				var previousIsLowerOrDigit = hasPrevious && (char.IsLower(value[index - 1]) || char.IsDigit(value[index - 1]));

				if (hasPrevious && (previousIsLowerOrDigit || nextIsLower))
				{
					builder.Append('_');
				}

				builder.Append(char.ToLowerInvariant(character));
			}
			else
			{
				builder.Append(character);
			}
		}

		return builder.ToString();
	}
}
