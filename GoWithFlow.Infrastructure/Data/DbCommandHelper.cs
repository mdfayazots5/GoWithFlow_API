using System.Data;
using System.Data.Common;
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
		return $"public.{baseName.ToLowerInvariant()}";
	}

	private static string NormalizeParameterName(string provider, string parameterName)
	{
		return DatabaseProviderNames.IsPostgreSql(provider)
			? "p_" + parameterName.TrimStart('@').ToLowerInvariant()
			: parameterName;
	}
}
