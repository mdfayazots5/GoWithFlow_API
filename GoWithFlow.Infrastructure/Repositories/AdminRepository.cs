using System.Data;
using System.Data.Common;
using GoWithFlow.Application.Common;
using GoWithFlow.Application.DTOs.Requests.Admin;
using GoWithFlow.Application.DTOs.Responses.Admin;
using GoWithFlow.Application.Interfaces.Repositories;
using GoWithFlow.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql.EntityFrameworkCore.PostgreSQL;

namespace GoWithFlow.Infrastructure.Repositories;

public sealed class AdminRepository : IAdminRepository
{
	private readonly GoWithFlowDbContext _dbContext;

	public AdminRepository(GoWithFlowDbContext dbContext)
	{
		_dbContext = dbContext;
	}

	public async Task<AdminDashboardResponseDto> GetDashboardSummaryAsync(CancellationToken cancellationToken = default)
	{
		await using var command = await CreateStoredProcedureCommandAsync("dbo.uspGetAdminDashboardSummary", cancellationToken);
		await using var reader = await DbCommandHelper.ExecuteReaderAsync(command, cancellationToken);

		if (await reader.ReadAsync(cancellationToken) == false)
		{
			return new AdminDashboardResponseDto();
		}

		return new AdminDashboardResponseDto
		{
			TotalUsers = GetInt32(reader, "TotalUsers"),
			ActiveSessionsToday = GetInt32(reader, "ActiveSessionsToday"),
			TotalScriptsUploaded = GetInt32(reader, "TotalScriptsUploaded"),
			TotalMistakesRecorded = GetInt32(reader, "TotalMistakesRecorded")
		};
	}

	public async Task<List<RecentActivityDto>> GetRecentActivitiesAsync(int topN, CancellationToken cancellationToken = default)
	{
		await using var command = await CreateStoredProcedureCommandAsync("dbo.uspGetRecentActivityList", cancellationToken);
		command.Parameters.Add(CreateParameter("@TopN", topN));

		await using var reader = await DbCommandHelper.ExecuteReaderAsync(command, cancellationToken);

		var items = new List<RecentActivityDto>();

		while (await reader.ReadAsync(cancellationToken))
		{
			items.Add(new RecentActivityDto
			{
				UserFullName = GetString(reader, "UserFullName"),
				SessionName = GetString(reader, "SessionName"),
				SessionDate = GetDateTime(reader, "SessionDate"),
				FluencyScore = GetDecimal(reader, "FluencyScore"),
				MistakeCount = GetInt32(reader, "MistakeCount"),
				SessionStatus = GetString(reader, "SessionStatus")
			});
		}

		return items;
	}

	public async Task<List<GrammarMistakeSummaryDto>> GetTopGrammarMistakesAsync(int topN, CancellationToken cancellationToken = default)
	{
		await using var command = await CreateStoredProcedureCommandAsync("dbo.uspGetTopGrammarMistakeType", cancellationToken);
		command.Parameters.Add(CreateParameter("@TopN", topN));

		await using var reader = await DbCommandHelper.ExecuteReaderAsync(command, cancellationToken);

		var items = new List<GrammarMistakeSummaryDto>();

		while (await reader.ReadAsync(cancellationToken))
		{
			items.Add(new GrammarMistakeSummaryDto
			{
				GrammarTag = GetString(reader, "GrammarTag"),
				UserCount = GetInt32(reader, "UserCount"),
				Percentage = GetDecimal(reader, "Percentage")
			});
		}

		return items;
	}

	public async Task<PagedResult<AdminUserListResponseDto>> GetUsersAsync(AdminUserSearchRequestDto dto, CancellationToken cancellationToken = default)
	{
		await using var command = await CreateStoredProcedureCommandAsync("dbo.uspGetAllUserBySearch", cancellationToken);
		command.Parameters.Add(CreateParameter("@SearchTerm", dto.SearchTerm));
		command.Parameters.Add(CreateParameter("@AgeGroup", dto.AgeGroup));
		command.Parameters.Add(CreateParameter("@IsActive", dto.IsActive));
		command.Parameters.Add(CreateParameter("@PageNumber", dto.PageNumber));
		command.Parameters.Add(CreateParameter("@PageSize", dto.PageSize));

		await using var reader = await DbCommandHelper.ExecuteReaderAsync(command, cancellationToken);

		var items = new List<AdminUserListResponseDto>();

		while (await reader.ReadAsync(cancellationToken))
		{
			items.Add(new AdminUserListResponseDto
			{
				UserId = GetInt64(reader, "UserId"),
				FullName = GetString(reader, "FullName"),
				MobileNumber = GetString(reader, "MobileNumber"),
				AgeGroup = GetString(reader, "AgeGroup"),
				TotalSessionsPlayed = GetInt32(reader, "TotalSessionsPlayed"),
				DailyStreakCount = GetInt32(reader, "DailyStreakCount"),
				LastLoginDate = GetNullableDateTime(reader, "LastLoginDate"),
				IsActive = GetBoolean(reader, "IsActive")
			});
		}

		// Close the reader before running EF Core CountAsync — Npgsql does not allow
		// a second query on the same connection while a reader is still open (no MARS).
		await reader.CloseAsync();

		var totalCount = await CountUsersAsync(dto, cancellationToken);

		return new PagedResult<AdminUserListResponseDto>
		{
			Items = items,
			TotalCount = totalCount,
			PageNumber = dto.PageNumber,
			PageSize = dto.PageSize
		};
	}

	public async Task<AdminUserDetailResponseDto?> GetUserDetailAsync(long userId, CancellationToken cancellationToken = default)
	{
		await using var command = await CreateStoredProcedureCommandAsync("dbo.uspGetUserDetailByUserId", cancellationToken);
		command.Parameters.Add(CreateParameter("@UserId", userId));

		await using var reader = await DbCommandHelper.ExecuteReaderAsync(command, cancellationToken);

		if (await reader.ReadAsync(cancellationToken) == false)
		{
			return null;
		}

		var result = new AdminUserDetailResponseDto
		{
			UserId = GetInt64(reader, "UserId"),
			FullName = GetString(reader, "FullName"),
			MobileNumber = GetString(reader, "MobileNumber"),
			Email = GetNullableString(reader, "Email"),
			PasswordHash = GetNullableString(reader, "PasswordHash"),
			AgeGroup = GetString(reader, "AgeGroup"),
			PreferredHintLanguage = GetString(reader, "PreferredHintLanguage"),
			AvatarUrl = GetNullableString(reader, "AvatarUrl"),
			GroupCode = GetNullableString(reader, "GroupCode"),
			Role = GetString(reader, "Role"),
			DailyStreakCount = GetInt32(reader, "DailyStreakCount"),
			TotalSessionsPlayed = GetInt32(reader, "TotalSessionsPlayed"),
			LastLoginDate = GetNullableDateTime(reader, "LastLoginDate"),
			IsActive = GetBoolean(reader, "IsActive"),
			RegistrationDate = GetDateTime(reader, "RegistrationDate"),
			Tag = GetNullableString(reader, "Tag"),
			Comments = GetNullableString(reader, "Comments"),
			SortOrder = GetInt32(reader, "SortOrder"),
			IPAddress = GetString(reader, "IPAddress"),
			CreatedBy = GetString(reader, "CreatedBy"),
			DateCreated = GetDateTime(reader, "DateCreated"),
			UpdatedBy = GetNullableString(reader, "UpdatedBy"),
			LastUpdated = GetNullableDateTime(reader, "LastUpdated"),
			DeletedBy = GetNullableString(reader, "DeletedBy"),
			DateDeleted = GetNullableDateTime(reader, "DateDeleted"),
			IsDeleted = GetBoolean(reader, "IsDeleted"),
			AvgFluencyScore = GetDecimal(reader, "AvgFluencyScore"),
			MostCommonMistakeType = GetString(reader, "MostCommonMistakeType")
		};

		result.RecentSessions = await GetRecentUserSessionsAsync(userId, 5, cancellationToken);

		return result;
	}

	public async Task UpdateUserStatusAsync(UpdateUserStatusRequestDto dto, string updatedBy, string ipAddress, CancellationToken cancellationToken = default)
	{
		await using var command = await CreateStoredProcedureCommandAsync("dbo.uspUpdateUserActiveStatusByUserId", cancellationToken);
		command.Parameters.Add(CreateParameter("@UserId", dto.UserId));
		command.Parameters.Add(CreateParameter("@IsActive", dto.IsActive));
		command.Parameters.Add(CreateParameter("@UpdatedBy", updatedBy));
		command.Parameters.Add(CreateParameter("@IPAddress", ipAddress));

		await DbCommandHelper.ExecuteNonQueryAsync(command, cancellationToken);
	}

	public async Task<AdminNoteResponseDto> AddAdminNoteAsync(long adminUserId, AdminNoteRequestDto dto, string createdBy, string ipAddress, CancellationToken cancellationToken = default)
	{
		await using var command = await CreateStoredProcedureCommandAsync("dbo.uspInsertAdminNote", cancellationToken);
		command.Parameters.Add(CreateParameter("@AdminUserId", adminUserId));
		command.Parameters.Add(CreateParameter("@TargetUserId", dto.TargetUserId));
		command.Parameters.Add(CreateParameter("@NoteText", dto.NoteText));
		command.Parameters.Add(CreateParameter("@CreatedBy", createdBy));
		command.Parameters.Add(CreateParameter("@IPAddress", ipAddress));

		await using var reader = await DbCommandHelper.ExecuteReaderAsync(command, cancellationToken);

		if (await reader.ReadAsync(cancellationToken) == false)
		{
			throw new InvalidOperationException("Admin note insert did not return a result.");
		}

		return ReadAdminNote(reader);
	}

	public async Task<List<AdminNoteResponseDto>> GetAdminNotesByUserAsync(long targetUserId, CancellationToken cancellationToken = default)
	{
		await using var command = await CreateStoredProcedureCommandAsync("dbo.uspGetAdminNoteByTargetUserId", cancellationToken);
		command.Parameters.Add(CreateParameter("@TargetUserId", targetUserId));

		await using var reader = await DbCommandHelper.ExecuteReaderAsync(command, cancellationToken);

		var items = new List<AdminNoteResponseDto>();

		while (await reader.ReadAsync(cancellationToken))
		{
			items.Add(ReadAdminNote(reader));
		}

		return items;
	}

	public async Task<PagedResult<AdminReportSummaryDto>> GetReportSummaryAsync(AdminReportFilterRequestDto dto, CancellationToken cancellationToken = default)
	{
		await using var command = await CreateStoredProcedureCommandAsync("dbo.uspGetUserReportSummaryList", cancellationToken);
		command.Parameters.Add(CreateParameter("@FromDate", dto.FromDate));
		command.Parameters.Add(CreateParameter("@ToDate", dto.ToDate));
		command.Parameters.Add(CreateParameter("@UserId", dto.UserId ?? 0));
		command.Parameters.Add(CreateParameter("@PageNumber", dto.PageNumber));
		command.Parameters.Add(CreateParameter("@PageSize", dto.PageSize));

		await using var reader = await DbCommandHelper.ExecuteReaderAsync(command, cancellationToken);

		var items = new List<AdminReportSummaryDto>();

		while (await reader.ReadAsync(cancellationToken))
		{
			items.Add(new AdminReportSummaryDto
			{
				UserId = GetInt64(reader, "UserId"),
				FullName = GetString(reader, "FullName"),
				TotalSessions = GetInt32(reader, "TotalSessions"),
				AvgFluencyScore = GetDecimal(reader, "AvgFluencyScore"),
				MostCommonMistakeType = GetString(reader, "MostCommonMistakeType"),
				ImprovementPercent = GetDecimal(reader, "ImprovementPercent"),
				LastSessionDate = GetNullableDateTime(reader, "LastSessionDate")
			});
		}

		await reader.CloseAsync();

		var totalCount = await _dbContext.Users
			.AsNoTracking()
			.CountAsync(user => user.IsDeleted == false && (dto.UserId.HasValue == false || dto.UserId == 0 || user.UserId == dto.UserId.Value), cancellationToken);

		return new PagedResult<AdminReportSummaryDto>
		{
			Items = items,
			TotalCount = totalCount,
			PageNumber = dto.PageNumber,
			PageSize = dto.PageSize
		};
	}

	public async Task<AdminUserFullReportDto?> GetUserFullReportAsync(long userId, CancellationToken cancellationToken = default)
	{
		await using var command = await CreateStoredProcedureCommandAsync("dbo.uspGetUserFullReportByUserId", cancellationToken);
		command.Parameters.Add(CreateParameter("@UserId", userId));

		await using var reader = await DbCommandHelper.ExecuteReaderAsync(command, cancellationToken);

		if (await reader.ReadAsync(cancellationToken) == false)
		{
			return null;
		}

		var result = new AdminUserFullReportDto
		{
			UserHeader = new AdminUserReportHeaderDto
			{
				UserId = GetInt64(reader, "UserId"),
				AvatarUrl = GetNullableString(reader, "AvatarUrl"),
				FullName = GetString(reader, "FullName"),
				DailyStreakCount = GetInt32(reader, "DailyStreakCount"),
				TotalSessions = GetInt32(reader, "TotalSessions"),
				AvgScore = GetDecimal(reader, "AvgScore")
			}
		};

		await reader.CloseAsync();

		result.SessionHistoryList = await GetRecentUserSessionsAsync(userId, null, cancellationToken);
		result.MistakeBreakdownList = await _dbContext.Mistakes
			.AsNoTracking()
			.Where(mistake => mistake.UserId == userId && mistake.IsDeleted == false && mistake.GrammarTag != null)
			.GroupBy(mistake => mistake.GrammarTag!)
			.OrderByDescending(group => group.Count())
			.ThenBy(group => group.Key)
			.Select(group => new AdminMistakeBreakdownDto
			{
				GrammarTag = group.Key,
				MistakeCount = group.Count()
			})
			.ToListAsync(cancellationToken);
		result.WeeklyScoreList = await GetWeeklyScoresAsync(userId, cancellationToken);

		return result;
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

	private DbParameter CreateParameter(string parameterName, object? value)
	{
		return DbCommandHelper.CreateParameter(_dbContext.DatabaseProvider, parameterName, value);
	}

	private static SessionSummaryDto ReadSessionSummary(DbDataReader reader)
	{
		return new SessionSummaryDto
		{
			SessionId = GetInt64(reader, "SessionId"),
			SessionName = GetString(reader, "SessionName"),
			Date = GetDateTime(reader, "Date"),
			Duration = GetInt32(reader, "Duration"),
			FluencyScore = GetDecimal(reader, "FluencyScore"),
			MistakeCount = GetInt32(reader, "MistakeCount")
		};
	}

	private static AdminNoteResponseDto ReadAdminNote(DbDataReader reader)
	{
		return new AdminNoteResponseDto
		{
			AdminNoteId = GetInt64(reader, "AdminNoteId"),
			AdminUserId = GetInt64(reader, "AdminUserId"),
			AdminName = GetString(reader, "AdminName"),
			NoteText = GetString(reader, "NoteText"),
			NoteDate = GetDateTime(reader, "NoteDate")
		};
	}

	private async Task<int> CountUsersAsync(AdminUserSearchRequestDto dto, CancellationToken cancellationToken)
	{
		var query = _dbContext.Users.AsNoTracking().Where(user => user.IsDeleted == false);

		if (string.IsNullOrWhiteSpace(dto.SearchTerm) == false)
		{
			var searchTerm = dto.SearchTerm.Trim();

			if (DatabaseProviderNames.IsPostgreSql(_dbContext.DatabaseProvider))
			{
				// Use ILike to match the SP's case-insensitive ILIKE search
				query = query.Where(user =>
					EF.Functions.ILike(user.FullName, $"%{searchTerm}%") ||
					EF.Functions.ILike(user.MobileNumber, $"%{searchTerm}%"));
			}
			else
			{
				query = query.Where(user =>
					user.FullName.Contains(searchTerm) ||
					user.MobileNumber.Contains(searchTerm));
			}
		}

		if (string.IsNullOrWhiteSpace(dto.AgeGroup) == false)
		{
			query = query.Where(user => user.AgeGroup == dto.AgeGroup);
		}

		if (dto.IsActive.HasValue)
		{
			query = query.Where(user => user.IsActive == dto.IsActive.Value);
		}

		return await query.CountAsync(cancellationToken);
	}

	private async Task<List<SessionSummaryDto>> GetRecentUserSessionsAsync(long userId, int? take, CancellationToken cancellationToken)
	{
		var sessions = await (
			from session in _dbContext.Sessions.AsNoTracking()
			where session.IsDeleted == false
				&& _dbContext.SessionMembers.Any(sessionMember =>
					sessionMember.SessionId == session.SessionId &&
					sessionMember.UserId == userId &&
					sessionMember.IsDeleted == false)
			select new
			{
				session.SessionId,
				session.SessionName,
				Date = session.EndedDate ?? session.StartedDate ?? session.DateCreated,
				Duration = session.ActualDurationSec.HasValue && session.ActualDurationSec.Value > 0
					? (int)Math.Ceiling(session.ActualDurationSec.Value / 60.0)
					: session.SessionDuration
			})
			.OrderByDescending(item => item.Date)
			.ThenByDescending(item => item.SessionId)
			.ToListAsync(cancellationToken);

		if (take.HasValue)
		{
			sessions = sessions.Take(take.Value).ToList();
		}

		var sessionIds = sessions.Select(item => item.SessionId).ToList();

		var voiceAverages = await _dbContext.VoiceAnalyses
			.AsNoTracking()
			.Where(voiceAnalysis => voiceAnalysis.UserId == userId && sessionIds.Contains(voiceAnalysis.SessionId) && voiceAnalysis.IsDeleted == false)
			.GroupBy(voiceAnalysis => voiceAnalysis.SessionId)
			.Select(group => new
			{
				SessionId = group.Key,
				FluencyScore = group.Average(voiceAnalysis => voiceAnalysis.FluencyScore)
			})
			.ToDictionaryAsync(item => item.SessionId, item => item.FluencyScore, cancellationToken);

		var mistakeCounts = await _dbContext.Mistakes
			.AsNoTracking()
			.Where(mistake => mistake.UserId == userId && sessionIds.Contains(mistake.SessionId) && mistake.IsDeleted == false)
			.GroupBy(mistake => mistake.SessionId)
			.Select(group => new
			{
				SessionId = group.Key,
				MistakeCount = group.Count()
			})
			.ToDictionaryAsync(item => item.SessionId, item => item.MistakeCount, cancellationToken);

		return sessions.Select(item => new SessionSummaryDto
		{
			SessionId = item.SessionId,
			SessionName = item.SessionName,
			Date = item.Date,
			Duration = item.Duration,
			FluencyScore = voiceAverages.TryGetValue(item.SessionId, out var fluencyScore) ? fluencyScore : 0m,
			MistakeCount = mistakeCounts.TryGetValue(item.SessionId, out var mistakeCount) ? mistakeCount : 0
		}).ToList();
	}

	private async Task<List<WeeklyScoreDto>> GetWeeklyScoresAsync(long userId, CancellationToken cancellationToken)
	{
		var weeklyScores = await _dbContext.VoiceAnalyses
			.AsNoTracking()
			.Where(voiceAnalysis => voiceAnalysis.UserId == userId && voiceAnalysis.IsDeleted == false)
			.GroupBy(voiceAnalysis => new
			{
				voiceAnalysis.RecordedAt.Year,
				voiceAnalysis.RecordedAt.Month,
				Week = ((voiceAnalysis.RecordedAt.Day - 1) / 7) + 1
			})
			.Select(group => new
			{
				group.Key.Year,
				group.Key.Month,
				group.Key.Week,
				AvgFluencyScore = group.Average(voiceAnalysis => voiceAnalysis.FluencyScore)
			})
			.OrderBy(item => item.Year)
			.ThenBy(item => item.Month)
			.ThenBy(item => item.Week)
			.ToListAsync(cancellationToken);

		return weeklyScores.Select(item => new WeeklyScoreDto
		{
			WeekLabel = $"{item.Year:D4}-{item.Month:D2}-W{item.Week}",
			AvgFluencyScore = item.AvgFluencyScore
		}).ToList();
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
		var ordinal = reader.GetOrdinal(columnName);
		return reader.GetInt64(ordinal);
	}

	private static int GetInt32(DbDataReader reader, string columnName)
	{
		var ordinal = reader.GetOrdinal(columnName);
		return reader.GetInt32(ordinal);
	}

	private static bool GetBoolean(DbDataReader reader, string columnName)
	{
		var ordinal = reader.GetOrdinal(columnName);
		return reader.GetBoolean(ordinal);
	}

	private static DateTime GetDateTime(DbDataReader reader, string columnName)
	{
		var ordinal = reader.GetOrdinal(columnName);
		return reader.GetDateTime(ordinal);
	}

	private static DateTime? GetNullableDateTime(DbDataReader reader, string columnName)
	{
		var ordinal = reader.GetOrdinal(columnName);
		return reader.IsDBNull(ordinal) ? null : reader.GetDateTime(ordinal);
	}

	private static decimal GetDecimal(DbDataReader reader, string columnName)
	{
		var ordinal = reader.GetOrdinal(columnName);
		return reader.GetDecimal(ordinal);
	}
}
