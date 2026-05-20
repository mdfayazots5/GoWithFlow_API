using System.Data;
using System.Data.Common;
using Microsoft.Data.SqlClient;
using Npgsql;
using NpgsqlTypes;

namespace GoWithFlow.Infrastructure.Data;

public static class DbCommandHelper
{
	private enum PostgreSqlRoutineExecutionMode
	{
		ReaderOrScalar,
		NonQuery
	}

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

	public static Task<DbDataReader> ExecuteReaderAsync(DbCommand command, CancellationToken cancellationToken = default)
	{
		PreparePostgreSqlFunctionInvocation(command, PostgreSqlRoutineExecutionMode.ReaderOrScalar);
		return command.ExecuteReaderAsync(cancellationToken);
	}

	public static Task<object?> ExecuteScalarAsync(DbCommand command, CancellationToken cancellationToken = default)
	{
		PreparePostgreSqlFunctionInvocation(command, PostgreSqlRoutineExecutionMode.ReaderOrScalar);
		return command.ExecuteScalarAsync(cancellationToken);
	}

	public static Task<int> ExecuteNonQueryAsync(DbCommand command, CancellationToken cancellationToken = default)
	{
		if (command is NpgsqlCommand && command.CommandType == CommandType.StoredProcedure)
		{
			return ExecutePostgreSqlNonQueryAsync(command, cancellationToken);
		}

		return command.ExecuteNonQueryAsync(cancellationToken);
	}

	private static void PreparePostgreSqlFunctionInvocation(DbCommand command, PostgreSqlRoutineExecutionMode executionMode)
	{
		if (command is not NpgsqlCommand || command.CommandType != CommandType.StoredProcedure)
		{
			return;
		}

		var parameterInvocations = command.Parameters
			.Cast<DbParameter>()
			.Where(IsInputParameter)
			.Select(parameter => $"{parameter.ParameterName} => @{parameter.ParameterName}");
		var parameterList = string.Join(", ", parameterInvocations);

		command.CommandType = CommandType.Text;
		command.CommandText = executionMode == PostgreSqlRoutineExecutionMode.NonQuery
			? $"SELECT {command.CommandText}({parameterList});"
			: $"SELECT * FROM {command.CommandText}({parameterList});";
	}

	private static async Task<int> ExecutePostgreSqlNonQueryAsync(DbCommand command, CancellationToken cancellationToken)
	{
		var hasOutputParameters = command.Parameters
			.Cast<DbParameter>()
			.Any(parameter => parameter.Direction is ParameterDirection.Output or ParameterDirection.InputOutput or ParameterDirection.ReturnValue);

		PreparePostgreSqlFunctionInvocation(command, PostgreSqlRoutineExecutionMode.NonQuery);

		if (hasOutputParameters == false)
		{
			return await command.ExecuteNonQueryAsync(cancellationToken);
		}

		await using var reader = await command.ExecuteReaderAsync(cancellationToken);
		if (await reader.ReadAsync(cancellationToken) == false)
		{
			return 0;
		}

		foreach (var parameter in command.Parameters.Cast<DbParameter>().Where(parameter => parameter.Direction is ParameterDirection.Output or ParameterDirection.InputOutput or ParameterDirection.ReturnValue))
		{
			var columnName = NormalizeParameterName(DatabaseProviderNames.PostgreSQL, parameter.ParameterName);
			var ordinal = reader.GetOrdinal(columnName);
			parameter.Value = reader.IsDBNull(ordinal) ? DBNull.Value : reader.GetValue(ordinal);
		}

		return 0;
	}

	private static bool IsInputParameter(DbParameter parameter)
	{
		return parameter.Direction is ParameterDirection.Input or ParameterDirection.InputOutput;
	}

	private static string NormalizeParameterName(string provider, string parameterName)
	{
		return DatabaseProviderNames.IsPostgreSql(provider)
			? "p_" + parameterName.TrimStart('@').ToLowerInvariant()
			: parameterName;
	}
}
