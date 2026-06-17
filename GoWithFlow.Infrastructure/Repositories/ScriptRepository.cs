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

	public async Task<List<ScriptAnalyticsItemDto>> GetScriptAnalyticsAsync(string? categoryFilter, CancellationToken cancellationToken = default)
	{
		var cutoff60Days = DateTime.UtcNow.AddDays(-60);

		var query = _dbContext.Scripts.AsNoTracking()
			.Where(s => s.IsDeleted == false);

		if (!string.IsNullOrWhiteSpace(categoryFilter))
			query = query.Where(s => s.Category == categoryFilter);

		var scripts = await query
			.OrderByDescending(s => s.UploadedDate)
			.Select(s => new { s.ScriptId, s.ScriptTitle, s.Category })
			.ToListAsync(cancellationToken);

		var scriptIds = scripts.Select(s => s.ScriptId).ToList();

		// Per-session metrics for all scripts at once
		var sessionData = await (
			from s in _dbContext.Sessions.AsNoTracking()
			where scriptIds.Contains(s.ScriptId) && s.IsDeleted == false
			select new { s.ScriptId, s.SessionId, s.Status, s.ActualDurationSec, s.StartedDate, s.EndedDate, s.DateCreated }
		).ToListAsync(cancellationToken);

		var sessionIds = sessionData.Select(s => s.SessionId).ToList();

		// Avg fluency and mistake counts
		var fluencyBySession = await _dbContext.VoiceAnalyses.AsNoTracking()
			.Where(va => sessionIds.Contains(va.SessionId) && va.IsDeleted == false)
			.GroupBy(va => va.SessionId)
			.Select(g => new { SessionId = g.Key, AvgFluency = g.Average(va => (decimal)va.FluencyScore) })
			.ToListAsync(cancellationToken);

		var mistakesBySession = await _dbContext.Mistakes.AsNoTracking()
			.Where(m => sessionIds.Contains(m.SessionId) && m.IsDeleted == false)
			.GroupBy(m => m.SessionId)
			.Select(g => new { SessionId = g.Key, Count = g.Count() })
			.ToListAsync(cancellationToken);

		// ReRead counts per session
		var rereadBySession = await _dbContext.TurnStates.AsNoTracking()
			.Where(t => sessionIds.Contains(t.SessionId) && t.IsDeleted == false)
			.GroupBy(t => t.SessionId)
			.Select(g => new { SessionId = g.Key, AvgReRead = g.Average(t => (decimal)t.ReReadCount) })
			.ToListAsync(cancellationToken);

		// RepracticeSession conversion (sessions that generated at least 1 repractice)
		var repracticeSessionIds = await _dbContext.RepracticeSessions.AsNoTracking()
			.Where(r => sessionIds.Contains(r.SourceSessionId) && r.IsDeleted == false)
			.Select(r => r.SourceSessionId)
			.Distinct()
			.ToListAsync(cancellationToken);

		var fluencyMap   = fluencyBySession.ToDictionary(x => x.SessionId, x => x.AvgFluency);
		var mistakeMap   = mistakesBySession.ToDictionary(x => x.SessionId, x => (decimal)x.Count);
		var rereadMap    = rereadBySession.ToDictionary(x => x.SessionId, x => x.AvgReRead);
		var repracticeSet = repracticeSessionIds.ToHashSet();

		var result = new List<ScriptAnalyticsItemDto>();

		foreach (var script in scripts)
		{
			var scriptSessions = sessionData.Where(s => s.ScriptId == script.ScriptId).ToList();
			var total          = scriptSessions.Count;
			var completed      = scriptSessions.Count(s => s.Status == "COMPLETED");

			var completionRate    = total > 0 ? Math.Round(completed * 100m / total, 1) : 0m;
			var avgFluency        = scriptSessions.Count > 0
									? Math.Round(scriptSessions.Where(s => fluencyMap.ContainsKey(s.SessionId)).Select(s => fluencyMap[s.SessionId]).DefaultIfEmpty(0m).Average(), 1)
									: 0m;
			var avgMistakes       = scriptSessions.Count > 0
									? Math.Round(scriptSessions.Where(s => mistakeMap.ContainsKey(s.SessionId)).Select(s => mistakeMap[s.SessionId]).DefaultIfEmpty(0m).Average(), 1)
									: 0m;
			var avgDuration       = scriptSessions.Count > 0
									? Math.Round(scriptSessions.Where(s => s.ActualDurationSec.HasValue && s.ActualDurationSec.Value > 0).Select(s => (decimal)s.ActualDurationSec!.Value / 60m).DefaultIfEmpty(0m).Average(), 1)
									: 0m;
			var avgReRead         = scriptSessions.Count > 0
									? Math.Round(scriptSessions.Where(s => rereadMap.ContainsKey(s.SessionId)).Select(s => rereadMap[s.SessionId]).DefaultIfEmpty(0m).Average(), 2)
									: 0m;
			var repracticeCount   = scriptSessions.Count(s => repracticeSet.Contains(s.SessionId));
			var conversionRate    = total > 0 ? Math.Round(repracticeCount * 100m / total, 1) : 0m;

			var lastUsed = scriptSessions
				.Select(s => s.EndedDate ?? s.StartedDate ?? s.DateCreated)
				.OrderByDescending(d => d)
				.FirstOrDefault();

			result.Add(new ScriptAnalyticsItemDto
			{
				ScriptId                 = script.ScriptId,
				ScriptTitle              = script.ScriptTitle,
				Category                 = script.Category,
				TotalSessionsStarted     = total,
				CompletionRate           = completionRate,
				AvgFluencyScore          = avgFluency,
				AvgMistakeCount          = avgMistakes,
				AvgDurationMinutes       = avgDuration,
				AvgReReadRate            = avgReRead,
				RepracticeConversionRate = conversionRate,
				LastUsedDate             = lastUsed == default ? null : lastUsed,
				IsInactive               = lastUsed == default || lastUsed < cutoff60Days
			});
		}

		return result;
	}

	public async Task RollbackScriptVersionAsync(long scriptId, int versionNumber, string updatedBy, CancellationToken cancellationToken = default)
	{
		// Find the target version entry (contains VersionNotes for reference, but utterances were stored in the original upload)
		var targetVersion = await _dbContext.ScriptVersions.AsNoTracking()
			.Where(v => v.ScriptId == scriptId && v.VersionNumber == versionNumber && v.IsDeleted == false)
			.FirstOrDefaultAsync(cancellationToken)
			?? throw new KeyNotFoundException($"Version {versionNumber} not found for script {scriptId}.");

		// Increment version on tblScript to indicate rollback event
		var script = await _dbContext.Scripts
			.Where(s => s.ScriptId == scriptId && s.IsDeleted == false)
			.FirstOrDefaultAsync(cancellationToken)
			?? throw new KeyNotFoundException($"Script {scriptId} not found.");

		script.Version += 1;
		script.UpdatedBy = updatedBy;
		script.LastUpdated = DateTime.UtcNow;

		// Insert a new version record noting the rollback
		_dbContext.ScriptVersions.Add(new ScriptVersion
		{
			ScriptId = scriptId,
			VersionNumber = script.Version,
			VersionNotes = $"Rolled back to version {versionNumber}",
			UploadedByUserId = script.UploadedByUserId,
			CreatedBy = updatedBy
		});

		await _dbContext.SaveChangesAsync(cancellationToken);
	}

	private static readonly List<string> ApprovedGrammarTagList = new()
	{
		"Present Simple", "Present Continuous", "Present Perfect", "Present Perfect Continuous",
		"Past Simple", "Past Continuous", "Past Perfect",
		"Future Simple", "Future Continuous", "Future Perfect",
		"Modal Verbs", "Passive Voice", "Conditionals", "Reported Speech",
		"Gerunds", "Infinitives", "Articles", "Prepositions", "Subject-Verb Agreement",
		"STAR Method", "Formal Register", "Question Forms", "Contrast", "Vocabulary", "General Fluency"
	};

	public async Task<ScriptPromptDataResponseDto> GetPromptDataForCategoryAsync(string category, CancellationToken cancellationToken = default)
	{
		var aliases = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		var upper = category.Trim().ToUpperInvariant();

		switch (upper)
		{
			case "MOCK INTERVIEW":    aliases.Add("Mock Interview"); aliases.Add("Interview"); break;
			case "VOCABULARY SPRINT": aliases.Add("Vocabulary Sprint"); aliases.Add("Vocabulary"); break;
			case "REPRACTICE ROUND":  aliases.Add("Repractice Round"); aliases.Add("Repetition"); break;
			default: aliases.Add(category.Trim()); break;
		}

		// 1. Fetch base prompt from DB (tblscriptprompttemplate — seeded from ExcelTemplateStandard.md)
		var connection = _dbContext.Database.GetDbConnection();
		await EnsureConnectionOpenAsync(connection, cancellationToken);

		string basePrompt   = string.Empty;
		string sourceRef    = string.Empty;
		int    promptVersion = 1;

		var canonicalCategory = aliases.First();  // first entry = canonical name
		var tableName = IsPostgres
			? "public.tblscriptprompttemplate"
			: "dbo.tblScriptPromptTemplate";
		var catCol    = IsPostgres ? "category"    : "Category";
		var activeCol = IsPostgres ? "isactive"    : "IsActive";
		var deletedCol= IsPostgres ? "isdeleted"   : "IsDeleted";
		var textCol   = IsPostgres ? "prompttext"  : "PromptText";
		var sourceCol = IsPostgres ? "sourceref"   : "SourceRef";
		var verCol    = IsPostgres ? "version"     : "Version";

		await using var promptCmd = connection.CreateCommand();
		promptCmd.CommandText = IsPostgres
			? $"SELECT prompttext, sourceref, version FROM {tableName} WHERE category = @Cat AND isactive = TRUE AND isdeleted = FALSE LIMIT 1"
			: $"SELECT TOP 1 PromptText, SourceRef, Version FROM {tableName} WHERE Category = @Cat AND IsActive = 1 AND IsDeleted = 0";
		var pCat = promptCmd.CreateParameter();
		pCat.ParameterName = "@Cat";
		pCat.Value = canonicalCategory;
		promptCmd.Parameters.Add(pCat);

		await using (var promptReader = await promptCmd.ExecuteReaderAsync(cancellationToken))
		{
			if (await promptReader.ReadAsync(cancellationToken))
			{
				basePrompt    = promptReader.GetString(0);
				sourceRef     = promptReader.GetString(1);
				promptVersion = promptReader.GetInt32(2);
			}
		}

		// 2. Get live tag data from tblscript for this category
		var activeScripts = await _dbContext.Scripts.AsNoTracking()
			.Where(s => s.IsDeleted == false && s.IsActive && aliases.Contains(s.Category))
			.ToListAsync(cancellationToken);

		var grammarTagsInUse = activeScripts
			.Where(s => !string.IsNullOrWhiteSpace(s.GrammarFocusTag))
			.Select(s => s.GrammarFocusTag!)
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.OrderBy(t => t)
			.ToList();

		var contextTagsInUse = activeScripts
			.Where(s => !string.IsNullOrWhiteSpace(s.ContextTag))
			.Select(s => s.ContextTag!)
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.OrderBy(t => t)
			.ToList();

		// 3. Enrich base prompt with live DB tag data
		var enrichmentBlock = new System.Text.StringBuilder();
		enrichmentBlock.AppendLine();
		enrichmentBlock.AppendLine("─────────────────────────────────────────────────────────────");
		enrichmentBlock.AppendLine("LIVE DATA FROM PLATFORM DATABASE:");
		enrichmentBlock.AppendLine($"Active scripts in this category: {activeScripts.Count}");

		if (grammarTagsInUse.Count > 0)
		{
			enrichmentBlock.AppendLine($"GrammarFocusTag values already in use: {string.Join(", ", grammarTagsInUse)}");
			enrichmentBlock.AppendLine("→ Prefer one of these to maintain consistency, or add a new valid tag.");
		}
		else
		{
			enrichmentBlock.AppendLine("GrammarFocusTag values in use: (none yet — first script for this category)");
		}

		if (contextTagsInUse.Count > 0)
		{
			enrichmentBlock.AppendLine($"ContextTag values already in use: {string.Join(", ", contextTagsInUse)}");
			enrichmentBlock.AppendLine("→ Prefer one of these, or add a new valid context.");
		}
		else
		{
			enrichmentBlock.AppendLine("ContextTag values in use: (none yet — choose any realistic context)");
		}

		if (activeScripts.Count > 0)
		{
			enrichmentBlock.AppendLine($"→ Your script must be DISTINCT from the {activeScripts.Count} existing script(s) in this category.");
		}

		enrichmentBlock.AppendLine("─────────────────────────────────────────────────────────────");

		var fullPrompt = string.IsNullOrWhiteSpace(basePrompt)
			? $"[Prompt template not found in DB for category: {canonicalCategory}]"
			: basePrompt.TrimEnd() + enrichmentBlock.ToString();

		var (speakerLabels, minRows, maxRows, mandatoryColumns) = upper switch
		{
			"MOCK INTERVIEW"    => ("Interviewer / Candidate",  20, 50, "G (FocusWord) required; E (GrammarTag) required"),
			"QUESTION & ANSWER" => ("Interviewer / Candidate",  16, 40, "Interviewer rows = questions; Candidate rows = model answer (HIDDEN from candidate on-screen). E (GrammarTag) required"),
			"VOCABULARY SPRINT" => ("Tutor / Learner",          20, 40, "D (HintText) required; G (FocusWord) required; H (PronunciationNote) required on Tutor rows"),
			"FLUENCY DRILL"     => ("Speaker A / Speaker B",    30, 60, "E, G, H must be left blank"),
			"REPRACTICE ROUND"  => ("Coach / Learner",          14, 28, "D (HintText) required; E (GrammarTag) required — same value on ALL rows"),
			"ROLEPLAY"          => ("Two role-based names (e.g. Passenger, Check-In Agent)", 16, 40, "Use real-world role names — not Speaker A/B"),
			_                   => ("Speaker A / Speaker B",    12, 30, "E (GrammarTag) required — same value on ALL rows")
		};

		return new ScriptPromptDataResponseDto
		{
			GrammarTagsInUse    = grammarTagsInUse,
			ContextTagsInUse    = contextTagsInUse,
			ApprovedGrammarTags = ApprovedGrammarTagList,
			SpeakerLabels       = speakerLabels,
			MinRows             = minRows,
			MaxRows             = maxRows,
			MandatoryColumns    = mandatoryColumns,
			ActiveScriptCount   = activeScripts.Count,
			FullPrompt          = fullPrompt
		};
	}

	private bool IsPostgres => DatabaseProviderNames.IsPostgreSql(_dbContext.DatabaseProvider);

	private static async Task EnsureConnectionOpenAsync(System.Data.IDbConnection connection, CancellationToken cancellationToken)
	{
		if (connection.State != System.Data.ConnectionState.Open)
			await ((System.Data.Common.DbConnection)connection).OpenAsync(cancellationToken);
	}

	public async Task<long> DuplicateScriptAsync(long scriptId, string createdBy, CancellationToken cancellationToken = default)
	{
		var original = await _dbContext.Scripts.AsNoTracking()
			.Where(s => s.ScriptId == scriptId && s.IsDeleted == false)
			.FirstOrDefaultAsync(cancellationToken)
			?? throw new KeyNotFoundException($"Script {scriptId} not found.");

		var utterances = await _dbContext.Utterances.AsNoTracking()
			.Where(u => u.ScriptId == scriptId && u.IsDeleted == false)
			.OrderBy(u => u.SequenceId)
			.ToListAsync(cancellationToken);

		var duplicate = new Script
		{
			ScriptTitle          = $"{original.ScriptTitle} — Copy",
			Category             = original.Category,
			GrammarFocusTag      = original.GrammarFocusTag,
			ContextTag           = original.ContextTag,
			ComplexityLevel      = original.ComplexityLevel,
			TargetAgeGroup       = original.TargetAgeGroup,
			HintLanguage         = original.HintLanguage,
			IsActive             = false,  // draft until admin activates
			UploadedByUserId     = original.UploadedByUserId,
			Version              = 1,
			UtteranceCount       = original.UtteranceCount,
			CreatedBy            = createdBy
		};

		_dbContext.Scripts.Add(duplicate);
		await _dbContext.SaveChangesAsync(cancellationToken);

		var newScriptId = duplicate.ScriptId;

		// Copy utterances
		foreach (var u in utterances)
		{
			_dbContext.Utterances.Add(new Utterance
			{
				ScriptId          = newScriptId,
				SequenceId        = u.SequenceId,
				SpeakerLabel      = u.SpeakerLabel,
				EnglishText       = u.EnglishText,
				HintText          = u.HintText,
				GrammarTag        = u.GrammarTag,
				ContextTag        = u.ContextTag,
				FocusWord         = u.FocusWord,
				PronunciationNote = u.PronunciationNote,
				CreatedBy         = createdBy
			});
		}

		await _dbContext.SaveChangesAsync(cancellationToken);

		// Add version 1 record
		_dbContext.ScriptVersions.Add(new ScriptVersion
		{
			ScriptId         = newScriptId,
			VersionNumber    = 1,
			VersionNotes     = $"Duplicated from script {scriptId}",
			UploadedByUserId = original.UploadedByUserId,
			CreatedBy        = createdBy
		});

		await _dbContext.SaveChangesAsync(cancellationToken);

		return newScriptId;
	}

	public async Task UpdateScriptExcelKeyAsync(long scriptId, string excelStorageKey, CancellationToken cancellationToken = default)
	{
		var script = await _dbContext.Scripts.FindAsync(new object[] { scriptId }, cancellationToken);
		if (script is null) return;

		script.ExcelStorageKey = excelStorageKey;
		await _dbContext.SaveChangesAsync(cancellationToken);
	}

	public async Task<string?> GetScriptExcelKeyAsync(long scriptId, CancellationToken cancellationToken = default)
	{
		var script = await _dbContext.Scripts
			.AsNoTracking()
			.Where(s => s.ScriptId == scriptId)
			.Select(s => s.ExcelStorageKey)
			.FirstOrDefaultAsync(cancellationToken);

		return script;
	}
}
