using System.Data;
using System.Data.Common;
using GoWithFlow.Application.Common;
using GoWithFlow.Application.DTOs.Requests.Script;
using GoWithFlow.Application.DTOs.Responses.Script;
using GoWithFlow.Application.Interfaces.Repositories;
using GoWithFlow.Domain.Entities;
using GoWithFlow.Infrastructure.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace GoWithFlow.Infrastructure.Repositories;

public sealed class ScriptRepository : IScriptRepository
{
	private readonly GoWithFlowDbContext _dbContext;

	public ScriptRepository(GoWithFlowDbContext dbContext)
	{
		_dbContext = dbContext;
	}

	public async Task<long> InsertScriptAsync(Script script, CancellationToken cancellationToken = default)
	{
		await using var command = await CreateStoredProcedureCommandAsync("dbo.uspInsertScript", cancellationToken);
		command.Parameters.Add(CreateParameter("@ScriptTitle", script.ScriptTitle));
		command.Parameters.Add(CreateParameter("@Category", script.Category));
		command.Parameters.Add(CreateParameter("@GrammarFocusTag", script.GrammarFocusTag));
		command.Parameters.Add(CreateParameter("@ContextTag", script.ContextTag));
		command.Parameters.Add(CreateParameter("@ComplexityLevel", script.ComplexityLevel));
		command.Parameters.Add(CreateParameter("@TargetAgeGroup", script.TargetAgeGroup));
		command.Parameters.Add(CreateParameter("@HintLanguage", script.HintLanguage));
		command.Parameters.Add(CreateParameter("@IsActive", script.IsActive));
		command.Parameters.Add(CreateParameter("@UploadedByUserId", script.UploadedByUserId));
		command.Parameters.Add(CreateParameter("@Version", script.Version));
		command.Parameters.Add(CreateParameter("@CreatedBy", script.CreatedBy));
		command.Parameters.Add(CreateParameter("@IPAddress", script.IPAddress));

		var result = await command.ExecuteScalarAsync(cancellationToken);
		return Convert.ToInt64(result);
	}

	public async Task InsertUtteranceAsync(Utterance utterance, CancellationToken cancellationToken = default)
	{
		await using var command = await CreateStoredProcedureCommandAsync("dbo.uspInsertUtterance", cancellationToken);
		command.Parameters.Add(CreateParameter("@ScriptId", utterance.ScriptId));
		command.Parameters.Add(CreateParameter("@SequenceId", utterance.SequenceId));
		command.Parameters.Add(CreateParameter("@SpeakerLabel", utterance.SpeakerLabel));
		command.Parameters.Add(CreateParameter("@EnglishText", utterance.EnglishText));
		command.Parameters.Add(CreateParameter("@HintText", utterance.HintText));
		command.Parameters.Add(CreateParameter("@GrammarTag", utterance.GrammarTag));
		command.Parameters.Add(CreateParameter("@ContextTag", utterance.ContextTag));
		command.Parameters.Add(CreateParameter("@FocusWord", utterance.FocusWord));
		command.Parameters.Add(CreateParameter("@PronunciationNote", utterance.PronunciationNote));
		command.Parameters.Add(CreateParameter("@CreatedBy", utterance.CreatedBy));
		command.Parameters.Add(CreateParameter("@IPAddress", utterance.IPAddress));

		await command.ExecuteNonQueryAsync(cancellationToken);
	}

	public async Task BulkInsertUtterancesAsync(long scriptId, IEnumerable<UtteranceParseDto> utterances, string createdBy, string ipAddress, CancellationToken cancellationToken = default)
	{
		await using var command = await CreateStoredProcedureCommandAsync("dbo.uspBulkInsertUtterance", cancellationToken);
		command.Parameters.Add(CreateParameter("@ScriptId", scriptId));
		command.Parameters.Add(CreateUtteranceTableParameter(utterances));
		command.Parameters.Add(CreateParameter("@CreatedBy", createdBy));
		command.Parameters.Add(CreateParameter("@IPAddress", ipAddress));

		await command.ExecuteNonQueryAsync(cancellationToken);
	}

	public async Task UpdateScriptUtteranceCountAsync(long scriptId, string updatedBy, string ipAddress, CancellationToken cancellationToken = default)
	{
		await using var command = await CreateStoredProcedureCommandAsync("dbo.uspUpdateScriptUtteranceCount", cancellationToken);
		command.Parameters.Add(CreateParameter("@ScriptId", scriptId));
		command.Parameters.Add(CreateParameter("@UpdatedBy", updatedBy));
		command.Parameters.Add(CreateParameter("@IPAddress", ipAddress));

		await command.ExecuteNonQueryAsync(cancellationToken);
	}

	public async Task<PagedResult<ScriptListItemResponseDto>> GetScriptsAsync(ScriptSearchRequestDto dto, CancellationToken cancellationToken = default)
	{
		await using var command = await CreateStoredProcedureCommandAsync("dbo.uspGetScriptBySearch", cancellationToken);
		command.Parameters.Add(CreateParameter("@SearchTerm", dto.SearchTerm));
		command.Parameters.Add(CreateParameter("@Category", dto.Category));
		command.Parameters.Add(CreateParameter("@GrammarFocusTag", dto.GrammarFocusTag));
		command.Parameters.Add(CreateParameter("@TargetAgeGroup", dto.TargetAgeGroup));
		command.Parameters.Add(CreateParameter("@IsActive", dto.IsActive));
		command.Parameters.Add(CreateParameter("@PageNumber", dto.PageNumber));
		command.Parameters.Add(CreateParameter("@PageSize", dto.PageSize));

		await using var reader = await command.ExecuteReaderAsync(cancellationToken);
		var items = new List<ScriptListItemResponseDto>();

		while (await reader.ReadAsync(cancellationToken))
		{
			items.Add(new ScriptListItemResponseDto
			{
				ScriptId = GetInt64(reader, "ScriptId"),
				ScriptTitle = GetString(reader, "ScriptTitle"),
				Category = GetString(reader, "Category"),
				GrammarFocusTag = GetString(reader, "GrammarFocusTag"),
				ContextTag = GetString(reader, "ContextTag"),
				ComplexityLevel = GetByte(reader, "ComplexityLevel"),
				TargetAgeGroup = GetString(reader, "TargetAgeGroup"),
				UtteranceCount = GetInt32(reader, "UtteranceCount"),
				IsActive = GetBoolean(reader, "IsActive"),
				UploadedDate = GetDateTime(reader, "UploadedDate"),
				Version = GetInt32(reader, "Version")
			});
		}

		var totalCount = await ReadTotalCountAsync(reader, cancellationToken);

		return new PagedResult<ScriptListItemResponseDto>
		{
			Items = items,
			TotalCount = totalCount,
			PageNumber = dto.PageNumber,
			PageSize = dto.PageSize
		};
	}

	public async Task<ScriptDetailResponseDto?> GetScriptByIdAsync(long scriptId, CancellationToken cancellationToken = default)
	{
		await using var command = await CreateStoredProcedureCommandAsync("dbo.uspGetScriptDetailByScriptId", cancellationToken);
		command.Parameters.Add(CreateParameter("@ScriptId", scriptId));

		await using var reader = await command.ExecuteReaderAsync(cancellationToken);

		if (await reader.ReadAsync(cancellationToken) == false)
		{
			return null;
		}

		var result = new ScriptDetailResponseDto
		{
			ScriptId = GetInt64(reader, "ScriptId"),
			ScriptTitle = GetString(reader, "ScriptTitle"),
			Category = GetString(reader, "Category"),
			GrammarFocusTag = GetString(reader, "GrammarFocusTag"),
			ContextTag = GetString(reader, "ContextTag"),
			ComplexityLevel = GetByte(reader, "ComplexityLevel"),
			TargetAgeGroup = GetString(reader, "TargetAgeGroup"),
			HintLanguage = GetString(reader, "HintLanguage"),
			IsActive = GetBoolean(reader, "IsActive"),
			UploadedDate = GetDateTime(reader, "UploadedDate"),
			UploadedByUserId = GetInt64(reader, "UploadedByUserId"),
			Version = GetInt32(reader, "Version"),
			UtteranceCount = GetInt32(reader, "UtteranceCount")
		};

		if (await reader.NextResultAsync(cancellationToken))
		{
			while (await reader.ReadAsync(cancellationToken))
			{
				result.Utterances.Add(new UtteranceResponseDto
				{
					UtteranceId = GetInt64(reader, "UtteranceId"),
					ScriptId = GetInt64(reader, "ScriptId"),
					SequenceId = GetInt32(reader, "SequenceId"),
					SpeakerLabel = GetString(reader, "SpeakerLabel"),
					EnglishText = GetString(reader, "EnglishText"),
					HintText = GetNullableString(reader, "HintText"),
					GrammarTag = GetNullableString(reader, "GrammarTag"),
					ContextTag = GetNullableString(reader, "ContextTag"),
					FocusWord = GetNullableString(reader, "FocusWord"),
					PronunciationNote = GetNullableString(reader, "PronunciationNote")
				});
			}
		}

		return result;
	}

	public async Task<List<ScriptVersionResponseDto>> GetVersionHistoryAsync(long scriptId, CancellationToken cancellationToken = default)
	{
		await using var command = await CreateStoredProcedureCommandAsync("dbo.uspGetScriptVersionHistoryByScriptId", cancellationToken);
		command.Parameters.Add(CreateParameter("@ScriptId", scriptId));

		await using var reader = await command.ExecuteReaderAsync(cancellationToken);
		var items = new List<ScriptVersionResponseDto>();

		while (await reader.ReadAsync(cancellationToken))
		{
			items.Add(new ScriptVersionResponseDto
			{
				ScriptVersionId = GetInt64(reader, "ScriptVersionId"),
				ScriptId = GetInt64(reader, "ScriptId"),
				VersionNumber = GetInt32(reader, "VersionNumber"),
				VersionNotes = GetNullableString(reader, "VersionNotes"),
				UploadedByUserId = GetInt64(reader, "UploadedByUserId"),
				UploadedDate = GetDateTime(reader, "UploadedDate")
			});
		}

		return items;
	}

	public async Task UpdateScriptStatusAsync(ScriptStatusUpdateRequestDto dto, string updatedBy, string ipAddress, CancellationToken cancellationToken = default)
	{
		await using var command = await CreateStoredProcedureCommandAsync("dbo.uspUpdateScriptActiveStatusByScriptId", cancellationToken);
		command.Parameters.Add(CreateParameter("@ScriptId", dto.ScriptId));
		command.Parameters.Add(CreateParameter("@IsActive", dto.IsActive));
		command.Parameters.Add(CreateParameter("@UpdatedBy", updatedBy));
		command.Parameters.Add(CreateParameter("@IPAddress", ipAddress));

		await command.ExecuteNonQueryAsync(cancellationToken);
	}

	public async Task<long?> CheckScriptTitleExistsAsync(string scriptTitle, CancellationToken cancellationToken = default)
	{
		await using var command = await CreateStoredProcedureCommandAsync("dbo.uspCheckScriptTitleExists", cancellationToken);
		command.Parameters.Add(CreateParameter("@ScriptTitle", scriptTitle));

		var result = await command.ExecuteScalarAsync(cancellationToken);

		if (result is null || result == DBNull.Value)
		{
			return null;
		}

		return Convert.ToInt64(result);
	}

	public async Task<int> GetLatestVersionByTitleAsync(string scriptTitle, CancellationToken cancellationToken = default)
	{
		return await _dbContext.Scripts
			.Where(script => script.IsDeleted == false && script.ScriptTitle == scriptTitle)
			.Select(script => (int?)script.Version)
			.MaxAsync(cancellationToken) ?? 0;
	}

	public async Task<long> InsertScriptVersionAsync(ScriptVersion scriptVersion, CancellationToken cancellationToken = default)
	{
		await using var command = await CreateStoredProcedureCommandAsync("dbo.uspInsertScriptVersion", cancellationToken);
		command.Parameters.Add(CreateParameter("@ScriptId", scriptVersion.ScriptId));
		command.Parameters.Add(CreateParameter("@VersionNumber", scriptVersion.VersionNumber));
		command.Parameters.Add(CreateParameter("@VersionNotes", scriptVersion.VersionNotes));
		command.Parameters.Add(CreateParameter("@UploadedByUserId", scriptVersion.UploadedByUserId));
		command.Parameters.Add(CreateParameter("@CreatedBy", scriptVersion.CreatedBy));
		command.Parameters.Add(CreateParameter("@IPAddress", scriptVersion.IPAddress));

		var result = await command.ExecuteScalarAsync(cancellationToken);
		return Convert.ToInt64(result);
	}

	public async Task SoftDeleteScriptAsync(long scriptId, string deletedBy, string ipAddress, CancellationToken cancellationToken = default)
	{
		await using var command = await CreateStoredProcedureCommandAsync("dbo.uspSoftDeleteScriptByScriptId", cancellationToken);
		command.Parameters.Add(CreateParameter("@ScriptId", scriptId));
		command.Parameters.Add(CreateParameter("@DeletedBy", deletedBy));
		command.Parameters.Add(CreateParameter("@IPAddress", ipAddress));

		await command.ExecuteNonQueryAsync(cancellationToken);
	}

	private async Task<DbCommand> CreateStoredProcedureCommandAsync(string storedProcedureName, CancellationToken cancellationToken)
	{
		var connection = _dbContext.Database.GetDbConnection();

		if (connection.State != ConnectionState.Open)
		{
			await connection.OpenAsync(cancellationToken);
		}

		var command = connection.CreateCommand()!;
		command.CommandText = storedProcedureName;
		command.CommandType = CommandType.StoredProcedure;
		return command;
	}

	private static SqlParameter CreateUtteranceTableParameter(IEnumerable<UtteranceParseDto> utterances)
	{
		var dataTable = new DataTable();
		dataTable.Columns.Add("SequenceId", typeof(int));
		dataTable.Columns.Add("SpeakerLabel", typeof(string));
		dataTable.Columns.Add("EnglishText", typeof(string));
		dataTable.Columns.Add("HintText", typeof(string));
		dataTable.Columns.Add("GrammarTag", typeof(string));
		dataTable.Columns.Add("ContextTag", typeof(string));
		dataTable.Columns.Add("FocusWord", typeof(string));
		dataTable.Columns.Add("PronunciationNote", typeof(string));

		foreach (var utterance in utterances)
		{
			dataTable.Rows.Add(
				utterance.SequenceId,
				utterance.SpeakerLabel,
				utterance.EnglishText,
				utterance.HintText ?? (object)DBNull.Value,
				utterance.GrammarTag ?? (object)DBNull.Value,
				utterance.ContextTag ?? (object)DBNull.Value,
				utterance.FocusWord ?? (object)DBNull.Value,
				utterance.PronunciationNote ?? (object)DBNull.Value);
		}

		return new SqlParameter("@Utterances", SqlDbType.Structured)
		{
			TypeName = "dbo.UtteranceTVP",
			Value = dataTable
		};
	}

	private static SqlParameter CreateParameter(string parameterName, object? value)
	{
		return new SqlParameter(parameterName, value ?? DBNull.Value);
	}

	private static async Task<int> ReadTotalCountAsync(DbDataReader reader, CancellationToken cancellationToken)
	{
		if (await reader.NextResultAsync(cancellationToken) == false)
		{
			return 0;
		}

		if (await reader.ReadAsync(cancellationToken) == false)
		{
			return 0;
		}

		return GetInt32(reader, "TotalCount");
	}

	private static string GetString(DbDataReader reader, string columnName)
	{
		var ordinal = reader.GetOrdinal(columnName);
		return reader.IsDBNull(ordinal) ? string.Empty : reader.GetString(ordinal);
	}

	private static string? GetNullableString(DbDataReader reader, string columnName)
	{
		var ordinal = reader.GetOrdinal(columnName);
		return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
	}

	private static long GetInt64(DbDataReader reader, string columnName)
	{
		return reader.GetInt64(reader.GetOrdinal(columnName));
	}

	private static int GetInt32(DbDataReader reader, string columnName)
	{
		return reader.GetInt32(reader.GetOrdinal(columnName));
	}

	private static byte GetByte(DbDataReader reader, string columnName)
	{
		return reader.GetByte(reader.GetOrdinal(columnName));
	}

	private static bool GetBoolean(DbDataReader reader, string columnName)
	{
		return reader.GetBoolean(reader.GetOrdinal(columnName));
	}

	private static DateTime GetDateTime(DbDataReader reader, string columnName)
	{
		return reader.GetDateTime(reader.GetOrdinal(columnName));
	}
}
