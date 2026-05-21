using System.Data;
using System.Data.Common;
using System.Text.Json;
using GoWithFlow.Application.Common;
using GoWithFlow.Application.DTOs.Requests.Script;
using GoWithFlow.Application.DTOs.Responses.Script;
using GoWithFlow.Application.Interfaces.Repositories;
using GoWithFlow.Domain.Entities;
using GoWithFlow.Infrastructure.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Npgsql.EntityFrameworkCore.PostgreSQL;

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

		var result = await DbCommandHelper.ExecuteScalarAsync(command, cancellationToken);
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

		await DbCommandHelper.ExecuteNonQueryAsync(command, cancellationToken);
	}

	public async Task BulkInsertUtterancesAsync(long scriptId, IEnumerable<UtteranceParseDto> utterances, string createdBy, string ipAddress, CancellationToken cancellationToken = default)
	{
		await using var command = await CreateStoredProcedureCommandAsync("dbo.uspBulkInsertUtterance", cancellationToken);
		command.Parameters.Add(CreateParameter("@ScriptId", scriptId));
		command.Parameters.Add(CreateUtteranceTableParameter(utterances));
		command.Parameters.Add(CreateParameter("@CreatedBy", createdBy));
		command.Parameters.Add(CreateParameter("@IPAddress", ipAddress));

		await DbCommandHelper.ExecuteNonQueryAsync(command, cancellationToken);
	}

	public async Task UpdateScriptUtteranceCountAsync(long scriptId, string updatedBy, string ipAddress, CancellationToken cancellationToken = default)
	{
		await using var command = await CreateStoredProcedureCommandAsync("dbo.uspUpdateScriptUtteranceCount", cancellationToken);
		command.Parameters.Add(CreateParameter("@ScriptId", scriptId));
		command.Parameters.Add(CreateParameter("@UpdatedBy", updatedBy));
		command.Parameters.Add(CreateParameter("@IPAddress", ipAddress));

		await DbCommandHelper.ExecuteNonQueryAsync(command, cancellationToken);
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

		await using var reader = await DbCommandHelper.ExecuteReaderAsync(command, cancellationToken);
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

		await reader.CloseAsync();

		var totalCount = await CountScriptsAsync(dto, cancellationToken);

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

		await using var reader = await DbCommandHelper.ExecuteReaderAsync(command, cancellationToken);

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

		await reader.CloseAsync();

		result.Utterances = await _dbContext.Utterances
			.AsNoTracking()
			.Where(utterance => utterance.ScriptId == scriptId && utterance.IsDeleted == false)
			.OrderBy(utterance => utterance.SequenceId)
			.ThenBy(utterance => utterance.UtteranceId)
			.Select(utterance => new UtteranceResponseDto
			{
				UtteranceId = utterance.UtteranceId,
				ScriptId = utterance.ScriptId,
				SequenceId = utterance.SequenceId,
				SpeakerLabel = utterance.SpeakerLabel,
				EnglishText = utterance.EnglishText,
				HintText = utterance.HintText,
				GrammarTag = utterance.GrammarTag,
				ContextTag = utterance.ContextTag,
				FocusWord = utterance.FocusWord,
				PronunciationNote = utterance.PronunciationNote
			})
			.ToListAsync(cancellationToken);

		return result;
	}

	public async Task<List<ScriptVersionResponseDto>> GetVersionHistoryAsync(long scriptId, CancellationToken cancellationToken = default)
	{
		await using var command = await CreateStoredProcedureCommandAsync("dbo.uspGetScriptVersionHistoryByScriptId", cancellationToken);
		command.Parameters.Add(CreateParameter("@ScriptId", scriptId));

		await using var reader = await DbCommandHelper.ExecuteReaderAsync(command, cancellationToken);
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

		await DbCommandHelper.ExecuteNonQueryAsync(command, cancellationToken);
	}

	public async Task<long?> CheckScriptTitleExistsAsync(string scriptTitle, CancellationToken cancellationToken = default)
	{
		await using var command = await CreateStoredProcedureCommandAsync("dbo.uspCheckScriptTitleExists", cancellationToken);
		command.Parameters.Add(CreateParameter("@ScriptTitle", scriptTitle));

		var result = await DbCommandHelper.ExecuteScalarAsync(command, cancellationToken);

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

		var result = await DbCommandHelper.ExecuteScalarAsync(command, cancellationToken);
		return Convert.ToInt64(result);
	}

	public async Task SoftDeleteScriptAsync(long scriptId, string deletedBy, string ipAddress, CancellationToken cancellationToken = default)
	{
		await using var command = await CreateStoredProcedureCommandAsync("dbo.uspSoftDeleteScriptByScriptId", cancellationToken);
		command.Parameters.Add(CreateParameter("@ScriptId", scriptId));
		command.Parameters.Add(CreateParameter("@DeletedBy", deletedBy));
		command.Parameters.Add(CreateParameter("@IPAddress", ipAddress));

		await DbCommandHelper.ExecuteNonQueryAsync(command, cancellationToken);
	}

	private async Task<DbCommand> CreateStoredProcedureCommandAsync(string storedProcedureName, CancellationToken cancellationToken)
	{
		var connection = _dbContext.Database.GetDbConnection();

		if (connection.State != ConnectionState.Open)
		{
			await connection.OpenAsync(cancellationToken);
		}

		var command = connection.CreateCommand()!;
		command.CommandText = DbCommandHelper.QualifyRoutineName(_dbContext.DatabaseProvider, storedProcedureName);
		command.CommandType = CommandType.StoredProcedure;
		return command;
	}

	private DbParameter CreateUtteranceTableParameter(IEnumerable<UtteranceParseDto> utterances)
	{
		if (DatabaseProviderNames.IsPostgreSql(_dbContext.DatabaseProvider))
		{
			var payload = utterances.Select(utterance => new
			{
				utterance.SequenceId,
				utterance.SpeakerLabel,
				utterance.EnglishText,
				utterance.HintText,
				utterance.GrammarTag,
				utterance.ContextTag,
				utterance.FocusWord,
				utterance.PronunciationNote
			});

			return DbCommandHelper.CreateJsonParameter(
				_dbContext.DatabaseProvider,
				"@Utterances",
				JsonSerializer.Serialize(payload));
		}

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

	private DbParameter CreateParameter(string parameterName, object? value)
	{
		return DbCommandHelper.CreateParameter(_dbContext.DatabaseProvider, parameterName, value);
	}

	private async Task<int> CountScriptsAsync(ScriptSearchRequestDto dto, CancellationToken cancellationToken)
	{
		var query = _dbContext.Scripts.AsNoTracking().Where(script => script.IsDeleted == false);

		if (string.IsNullOrWhiteSpace(dto.SearchTerm) == false)
		{
			var searchTerm = dto.SearchTerm.Trim();

			if (DatabaseProviderNames.IsPostgreSql(_dbContext.DatabaseProvider))
			{
				query = query.Where(script =>
					EF.Functions.ILike(script.ScriptTitle, $"%{searchTerm}%") ||
					EF.Functions.ILike(script.Category, $"%{searchTerm}%") ||
					EF.Functions.ILike(script.ContextTag, $"%{searchTerm}%"));
			}
			else
			{
				query = query.Where(script =>
					script.ScriptTitle.Contains(searchTerm) ||
					script.Category.Contains(searchTerm) ||
					script.ContextTag.Contains(searchTerm));
			}
		}

		if (string.IsNullOrWhiteSpace(dto.Category) == false)
		{
			query = query.Where(script => script.Category == dto.Category);
		}

		if (string.IsNullOrWhiteSpace(dto.GrammarFocusTag) == false)
		{
			query = query.Where(script => script.GrammarFocusTag == dto.GrammarFocusTag);
		}

		if (string.IsNullOrWhiteSpace(dto.TargetAgeGroup) == false)
		{
			query = query.Where(script => script.TargetAgeGroup == dto.TargetAgeGroup);
		}

		if (dto.IsActive.HasValue)
		{
			query = query.Where(script => script.IsActive == dto.IsActive.Value);
		}

		return await query.CountAsync(cancellationToken);
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
