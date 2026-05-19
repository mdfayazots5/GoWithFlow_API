using System.Data;
using System.Data.Common;
using GoWithFlow.Application.Common;
using GoWithFlow.Application.DTOs.Requests.Admin;
using GoWithFlow.Application.DTOs.Responses.Admin;
using GoWithFlow.Application.Interfaces.Repositories;
using GoWithFlow.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

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
		await using var reader = await command.ExecuteReaderAsync(cancellationToken);

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

		await using var reader = await command.ExecuteReaderAsync(cancellationToken);

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

		await using var reader = await command.ExecuteReaderAsync(cancellationToken);

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

		await using var reader = await command.ExecuteReaderAsync(cancellationToken);

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

		var totalCount = await ReadTotalCountAsync(reader, cancellationToken);

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

		await using var reader = await command.ExecuteReaderAsync(cancellationToken);

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

		if (await reader.NextResultAsync(cancellationToken))
		{
			while (await reader.ReadAsync(cancellationToken))
			{
				result.RecentSessions.Add(ReadSessionSummary(reader));
			}
		}

		return result;
	}

	public async Task UpdateUserStatusAsync(UpdateUserStatusRequestDto dto, string updatedBy, string ipAddress, CancellationToken cancellationToken = default)
	{
		await using var command = await CreateStoredProcedureCommandAsync("dbo.uspUpdateUserActiveStatusByUserId", cancellationToken);
		command.Parameters.Add(CreateParameter("@UserId", dto.UserId));
		command.Parameters.Add(CreateParameter("@IsActive", dto.IsActive));
		command.Parameters.Add(CreateParameter("@UpdatedBy", updatedBy));
		command.Parameters.Add(CreateParameter("@IPAddress", ipAddress));

		await command.ExecuteNonQueryAsync(cancellationToken);
	}

	public async Task<AdminNoteResponseDto> AddAdminNoteAsync(long adminUserId, AdminNoteRequestDto dto, string createdBy, string ipAddress, CancellationToken cancellationToken = default)
	{
		await using var command = await CreateStoredProcedureCommandAsync("dbo.uspInsertAdminNote", cancellationToken);
		command.Parameters.Add(CreateParameter("@AdminUserId", adminUserId));
		command.Parameters.Add(CreateParameter("@TargetUserId", dto.TargetUserId));
		command.Parameters.Add(CreateParameter("@NoteText", dto.NoteText));
		command.Parameters.Add(CreateParameter("@CreatedBy", createdBy));
		command.Parameters.Add(CreateParameter("@IPAddress", ipAddress));

		await using var reader = await command.ExecuteReaderAsync(cancellationToken);

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

		await using var reader = await command.ExecuteReaderAsync(cancellationToken);

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

		await using var reader = await command.ExecuteReaderAsync(cancellationToken);

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

		var totalCount = await ReadTotalCountAsync(reader, cancellationToken);

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

		await using var reader = await command.ExecuteReaderAsync(cancellationToken);

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

		if (await reader.NextResultAsync(cancellationToken))
		{
			while (await reader.ReadAsync(cancellationToken))
			{
				result.SessionHistoryList.Add(ReadSessionSummary(reader));
			}
		}

		if (await reader.NextResultAsync(cancellationToken))
		{
			while (await reader.ReadAsync(cancellationToken))
			{
				result.MistakeBreakdownList.Add(new AdminMistakeBreakdownDto
				{
					GrammarTag = GetString(reader, "GrammarTag"),
					MistakeCount = GetInt32(reader, "MistakeCount")
				});
			}
		}

		if (await reader.NextResultAsync(cancellationToken))
		{
			while (await reader.ReadAsync(cancellationToken))
			{
				result.WeeklyScoreList.Add(new WeeklyScoreDto
				{
					WeekLabel = GetString(reader, "WeekLabel"),
					AvgFluencyScore = GetDecimal(reader, "AvgFluencyScore")
				});
			}
		}

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
