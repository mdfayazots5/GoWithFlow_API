using System.Data;
using System.Data.Common;
using GoWithFlow.Application.DTOs.Responses;
using GoWithFlow.Application.DTOs.Responses.LiveSession;
using GoWithFlow.Application.DTOs.Responses.Session;
using GoWithFlow.Application.DTOs.Responses.User;
using GoWithFlow.Application.Interfaces.Repositories;
using GoWithFlow.Domain.Entities;
using GoWithFlow.Infrastructure.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace GoWithFlow.Infrastructure.Repositories;

public sealed class UserRepository : GenericRepository<User>, IUserRepository
{
	public UserRepository(GoWithFlowDbContext dbContext)
		: base(dbContext)
	{
	}

	public async Task<User?> GetByMobileNumberAsync(string mobileNumber, CancellationToken cancellationToken = default)
	{
		return await DbContext.Users
			.FromSqlInterpolated($"EXEC dbo.uspGetUserByMobileNumber @MobileNumber = {mobileNumber}")
			.AsNoTracking()
			.FirstOrDefaultAsync(cancellationToken);
	}

	public async Task<User?> GetByUserIdAsync(long userId, CancellationToken cancellationToken = default)
	{
		return await DbContext.Users
			.FromSqlInterpolated($"EXEC dbo.uspGetUserByUserId @UserId = {userId}")
			.AsNoTracking()
			.FirstOrDefaultAsync(cancellationToken);
	}

	public async Task<long> InsertUserAsync(User user, CancellationToken cancellationToken = default)
	{
		var connection = DbContext.Database.GetDbConnection();
		await EnsureConnectionOpenAsync(connection, cancellationToken);

		await using var command = CreateStoredProcedureCommand(connection, "dbo.uspInsertUser");
		command.Parameters.Add(CreateParameter("@FullName", user.FullName));
		command.Parameters.Add(CreateParameter("@MobileNumber", user.MobileNumber));
		command.Parameters.Add(CreateParameter("@Email", user.Email));
		command.Parameters.Add(CreateParameter("@AgeGroup", user.AgeGroup));
		command.Parameters.Add(CreateParameter("@PreferredHintLanguage", user.PreferredHintLanguage));
		command.Parameters.Add(CreateParameter("@AvatarUrl", user.AvatarUrl));
		command.Parameters.Add(CreateParameter("@GroupCode", user.GroupCode));
		command.Parameters.Add(CreateParameter("@Role", user.Role));
		command.Parameters.Add(CreateParameter("@CreatedBy", user.CreatedBy));
		command.Parameters.Add(CreateParameter("@IPAddress", user.IPAddress));

		var result = await command.ExecuteScalarAsync(cancellationToken);

		return Convert.ToInt64(result);
	}

	public async Task UpdateLastLoginAsync(long userId, string updatedBy, string ipAddress, CancellationToken cancellationToken = default)
	{
		var connection = DbContext.Database.GetDbConnection();
		await EnsureConnectionOpenAsync(connection, cancellationToken);

		await using var command = CreateStoredProcedureCommand(connection, "dbo.uspUpdateUserLastLogin");
		command.Parameters.Add(CreateParameter("@UserId", userId));
		command.Parameters.Add(CreateParameter("@UpdatedBy", updatedBy));
		command.Parameters.Add(CreateParameter("@IPAddress", ipAddress));

		await command.ExecuteNonQueryAsync(cancellationToken);
	}

	public async Task SoftDeleteUserAsync(long userId, string deletedBy, string ipAddress, CancellationToken cancellationToken = default)
	{
		var connection = DbContext.Database.GetDbConnection();
		await EnsureConnectionOpenAsync(connection, cancellationToken);

		await using var command = CreateStoredProcedureCommand(connection, "dbo.uspSoftDeleteUser");
		command.Parameters.Add(CreateParameter("@UserId", userId));
		command.Parameters.Add(CreateParameter("@DeletedBy", deletedBy));
		command.Parameters.Add(CreateParameter("@IPAddress", ipAddress));

		await command.ExecuteNonQueryAsync(cancellationToken);
	}

	public async Task<UserProfileResponseDto?> GetUserProfileAsync(long userId, CancellationToken cancellationToken = default)
	{
		var connection = DbContext.Database.GetDbConnection();
		await EnsureConnectionOpenAsync(connection, cancellationToken);

		await using var command = CreateStoredProcedureCommand(connection, "dbo.uspGetUserProfileByUserId");
		command.Parameters.Add(CreateParameter("@UserId", userId));

		await using var reader = await command.ExecuteReaderAsync(cancellationToken);

		if (await reader.ReadAsync(cancellationToken) == false)
		{
			return null;
		}

		return new UserProfileResponseDto
		{
			UserId = GetInt64(reader, "UserId"),
			FullName = GetString(reader, "FullName"),
			MobileNumber = GetString(reader, "MobileNumber"),
			Email = GetNullableString(reader, "Email"),
			AgeGroup = GetString(reader, "AgeGroup"),
			PreferredHintLanguage = GetString(reader, "PreferredHintLanguage"),
			AvatarUrl = GetNullableString(reader, "AvatarUrl"),
			Role = GetString(reader, "Role"),
			DailyStreakCount = GetInt32(reader, "DailyStreakCount"),
			TotalSessionsPlayed = GetInt32(reader, "TotalSessionsPlayed"),
			TotalSessions = GetInt32(reader, "TotalSessions"),
			AvgFluencyScore = GetDecimal(reader, "AvgFluencyScore"),
			TotalMistakesFixed = GetInt32(reader, "TotalMistakesFixed"),
			IsActive = GetBoolean(reader, "IsActive"),
			RegistrationDate = GetDateTime(reader, "RegistrationDate")
		};
	}

	public async Task<UserDashboardResponseDto?> GetUserDashboardAsync(long userId, CancellationToken cancellationToken = default)
	{
		var connection = DbContext.Database.GetDbConnection();
		await EnsureConnectionOpenAsync(connection, cancellationToken);

		await using var command = CreateStoredProcedureCommand(connection, "dbo.uspGetUserDashboardSummaryByUserId");
		command.Parameters.Add(CreateParameter("@UserId", userId));

		await using var reader = await command.ExecuteReaderAsync(cancellationToken);

		if (await reader.ReadAsync(cancellationToken) == false)
		{
			return null;
		}

		var dashboard = new UserDashboardResponseDto
		{
			UserName = GetString(reader, "UserName"),
			CurrentStreak = GetInt32(reader, "CurrentStreak"),
			TodayDate = GetDateTime(reader, "TodayDate"),
			PendingRepracticeCount = GetInt32(reader, "PendingRepracticeCount")
		};

		var activeSessionId = GetNullableInt64(reader, "ActiveSessionId");

		if (activeSessionId.HasValue && activeSessionId.Value > 0)
		{
			dashboard.ActiveSession = new ActiveSessionBannerDto
			{
				SessionId = activeSessionId.Value,
				SessionName = GetString(reader, "ActiveSessionName"),
				Status = GetString(reader, "ActiveSessionStatus"),
				JoinCode = GetString(reader, "JoinCode")
			};
		}

		if (await reader.NextResultAsync(cancellationToken))
		{
			while (await reader.ReadAsync(cancellationToken))
			{
				dashboard.RecentSessions.Add(new SessionListItemResponseDto
				{
					SessionId = GetInt64(reader, "SessionId"),
					SessionName = GetString(reader, "SessionName"),
					SessionMode = GetString(reader, "SessionMode"),
					SessionDate = GetDateTime(reader, "SessionDate"),
					Duration = GetInt32(reader, "Duration"),
					FluencyScore = GetNullableDecimal(reader, "FluencyScore"),
					MistakeCount = GetInt32(reader, "MistakeCount"),
					Status = GetString(reader, "Status"),
					ScriptTitle = GetString(reader, "ScriptTitle")
				});
			}
		}

		if (await reader.NextResultAsync(cancellationToken))
		{
			while (await reader.ReadAsync(cancellationToken))
			{
				dashboard.PendingMistakes.Add(new MistakeResponseDto
				{
					MistakeId = GetInt64(reader, "MistakeId"),
					UserId = userId,
					SessionId = GetInt64(reader, "SessionId"),
					UtteranceId = GetInt64(reader, "UtteranceId"),
					ScriptId = GetInt64(reader, "ScriptId"),
					UtteranceText = GetString(reader, "UtteranceText"),
					SpokenText = GetNullableString(reader, "SpokenText"),
					MistakeType = GetString(reader, "MistakeType"),
					MistakeDetail = GetNullableString(reader, "MistakeDetail"),
					GrammarTag = GetNullableString(reader, "GrammarTag"),
					ContextTag = GetNullableString(reader, "ContextTag"),
					CorrectionText = GetNullableString(reader, "CorrectionText"),
					PracticeCount = GetInt32(reader, "PracticeCount"),
					IsResolved = GetBoolean(reader, "IsResolved"),
					FirstOccurrence = GetDateTime(reader, "FirstOccurrence"),
					LastAttempt = GetNullableDateTime(reader, "LastAttempt"),
					SessionName = GetString(reader, "SessionName"),
					ScriptTitle = GetString(reader, "ScriptTitle")
				});
			}
		}

		return dashboard;
	}

	public async Task UpdateUserProfileAsync(
		long userId,
		string fullName,
		string? email,
		string ageGroup,
		string preferredHintLanguage,
		string? avatarUrl,
		string updatedBy,
		string ipAddress,
		CancellationToken cancellationToken = default)
	{
		var connection = DbContext.Database.GetDbConnection();
		await EnsureConnectionOpenAsync(connection, cancellationToken);

		await using var command = CreateStoredProcedureCommand(connection, "dbo.uspUpdateUserProfile");
		command.Parameters.Add(CreateParameter("@UserId", userId));
		command.Parameters.Add(CreateParameter("@FullName", fullName));
		command.Parameters.Add(CreateParameter("@Email", email));
		command.Parameters.Add(CreateParameter("@AgeGroup", ageGroup));
		command.Parameters.Add(CreateParameter("@PreferredHintLanguage", preferredHintLanguage));
		command.Parameters.Add(CreateParameter("@AvatarUrl", avatarUrl));
		command.Parameters.Add(CreateParameter("@UpdatedBy", updatedBy));
		command.Parameters.Add(CreateParameter("@IPAddress", ipAddress));

		await command.ExecuteNonQueryAsync(cancellationToken);
	}

	public async Task UpsertUserStreakAsync(long userId, int practiceMinutes, string updatedBy, string ipAddress, CancellationToken cancellationToken = default)
	{
		var connection = DbContext.Database.GetDbConnection();
		await EnsureConnectionOpenAsync(connection, cancellationToken);

		await using var command = CreateStoredProcedureCommand(connection, "dbo.uspUpsertUserStreak");
		command.Parameters.Add(CreateParameter("@UserId", userId));
		command.Parameters.Add(CreateParameter("@PracticeMinutes", practiceMinutes));
		command.Parameters.Add(CreateParameter("@UpdatedBy", updatedBy));
		command.Parameters.Add(CreateParameter("@IPAddress", ipAddress));

		await command.ExecuteNonQueryAsync(cancellationToken);
	}

	public async Task<StreakDataResponseDto> GetStreakDataAsync(long userId, CancellationToken cancellationToken = default)
	{
		var connection = DbContext.Database.GetDbConnection();
		await EnsureConnectionOpenAsync(connection, cancellationToken);

		await using var command = CreateStoredProcedureCommand(connection, "dbo.uspGetStreakDataByUserId");
		command.Parameters.Add(CreateParameter("@UserId", userId));

		await using var reader = await command.ExecuteReaderAsync(cancellationToken);
		var response = new StreakDataResponseDto();

		if (await reader.ReadAsync(cancellationToken))
		{
			response.CurrentStreak = GetInt32(reader, "CurrentStreak");
			response.LongestStreak = GetInt32(reader, "LongestStreak");
		}

		if (await reader.NextResultAsync(cancellationToken))
		{
			while (await reader.ReadAsync(cancellationToken))
			{
				response.Last30Days.Add(new DailyStreakDto
				{
					StreakDate = GetDateTime(reader, "StreakDate"),
					SessionCount = GetInt32(reader, "SessionCount"),
					PracticeMinutes = GetInt32(reader, "PracticeMinutes")
				});
			}
		}

		return response;
	}

	public async Task<List<UserBadgeDto>> GetBadgesAsync(long userId, CancellationToken cancellationToken = default)
	{
		var connection = DbContext.Database.GetDbConnection();
		await EnsureConnectionOpenAsync(connection, cancellationToken);

		await using var command = CreateStoredProcedureCommand(connection, "dbo.uspGetUserBadgeByUserId");
		command.Parameters.Add(CreateParameter("@UserId", userId));

		await using var reader = await command.ExecuteReaderAsync(cancellationToken);
		var badges = new List<UserBadgeDto>();

		while (await reader.ReadAsync(cancellationToken))
		{
			badges.Add(new UserBadgeDto
			{
				BadgeCode = GetString(reader, "BadgeCode"),
				BadgeName = GetString(reader, "BadgeName"),
				EarnedDate = GetDateTime(reader, "EarnedDate"),
				IsEarned = true
			});
		}

		return badges;
	}

	public async Task CheckAndAwardBadgesAsync(long userId, string createdBy, string ipAddress, CancellationToken cancellationToken = default)
	{
		var connection = DbContext.Database.GetDbConnection();
		await EnsureConnectionOpenAsync(connection, cancellationToken);

		await using var command = CreateStoredProcedureCommand(connection, "dbo.uspCheckAndAwardBadge");
		command.Parameters.Add(CreateParameter("@UserId", userId));
		command.Parameters.Add(CreateParameter("@CreatedBy", createdBy));
		command.Parameters.Add(CreateParameter("@IPAddress", ipAddress));

		await command.ExecuteNonQueryAsync(cancellationToken);
	}

	public async Task<SessionDetailResponseDto?> GetSessionDetailAsync(long sessionId, long userId, CancellationToken cancellationToken = default)
	{
		var connection = DbContext.Database.GetDbConnection();
		await EnsureConnectionOpenAsync(connection, cancellationToken);

		await using var command = CreateStoredProcedureCommand(connection, "dbo.uspGetSessionDetailBySessionId");
		command.Parameters.Add(CreateParameter("@SessionId", sessionId));
		command.Parameters.Add(CreateParameter("@UserId", userId));

		await using var reader = await command.ExecuteReaderAsync(cancellationToken);

		if (await reader.ReadAsync(cancellationToken) == false)
		{
			return null;
		}

		var response = new SessionDetailResponseDto
		{
			SessionHeader = new SessionHeaderDto
			{
				SessionName = GetString(reader, "SessionName"),
				SessionMode = GetString(reader, "SessionMode"),
				SessionDate = GetDateTime(reader, "SessionDate"),
				Duration = GetInt32(reader, "Duration"),
				ScriptTitle = GetString(reader, "ScriptTitle"),
				MemberCount = GetInt32(reader, "MemberCount")
			}
		};

		if (await reader.NextResultAsync(cancellationToken) && await reader.ReadAsync(cancellationToken))
		{
			response.MyPerformance = new PerformanceSummaryDto
			{
				FluencyScore = GetDecimal(reader, "FluencyScore"),
				ConfidenceScore = GetDecimal(reader, "ConfidenceScore"),
				SpeakingSpeedWpm = GetInt32(reader, "SpeakingSpeedWpm"),
				PauseCount = GetInt32(reader, "PauseCount")
			};
		}

		if (await reader.NextResultAsync(cancellationToken))
		{
			while (await reader.ReadAsync(cancellationToken))
			{
				response.MyMistakes.Add(new MistakeResponseDto
				{
					MistakeId = GetInt64(reader, "MistakeId"),
					UserId = userId,
					SessionId = sessionId,
					UtteranceId = GetInt64(reader, "UtteranceId"),
					ScriptId = GetInt64(reader, "ScriptId"),
					UtteranceText = GetString(reader, "UtteranceText"),
					SpokenText = GetNullableString(reader, "SpokenText"),
					MistakeType = GetString(reader, "MistakeType"),
					MistakeDetail = GetNullableString(reader, "MistakeDetail"),
					GrammarTag = GetNullableString(reader, "GrammarTag"),
					ContextTag = GetNullableString(reader, "ContextTag"),
					CorrectionText = GetNullableString(reader, "CorrectionText"),
					PracticeCount = GetInt32(reader, "PracticeCount"),
					IsResolved = GetBoolean(reader, "IsResolved"),
					FirstOccurrence = GetDateTime(reader, "FirstOccurrence"),
					LastAttempt = GetNullableDateTime(reader, "LastAttempt"),
					SessionName = response.SessionHeader.SessionName,
					ScriptTitle = response.SessionHeader.ScriptTitle
				});
			}
		}

		if (await reader.NextResultAsync(cancellationToken))
		{
			while (await reader.ReadAsync(cancellationToken))
			{
				response.ListenerFeedbackReceived.Add(new FeedbackCountDto
				{
					FeedbackTag = GetString(reader, "FeedbackTag"),
					Count = GetInt32(reader, "Count")
				});
			}
		}

		if (await reader.NextResultAsync(cancellationToken))
		{
			while (await reader.ReadAsync(cancellationToken))
			{
				response.AllMemberScores.Add(new MemberScoreDto
				{
					UserId = GetInt64(reader, "UserId"),
					FullName = GetString(reader, "FullName"),
					FluencyScore = GetDecimal(reader, "FluencyScore"),
					ConfidenceScore = GetDecimal(reader, "ConfidenceScore"),
					MistakeCount = GetInt32(reader, "MistakeCount"),
					ListenerRating = GetDecimal(reader, "ListenerRating")
				});
			}
		}

		return response;
	}

	public async Task<List<SessionScoreDto>> GetImprovementSessionsAsync(long userId, CancellationToken cancellationToken = default)
	{
		var connection = DbContext.Database.GetDbConnection();
		await EnsureConnectionOpenAsync(connection, cancellationToken);

		await using var command = CreateStoredProcedureCommand(connection, "dbo.uspGetImprovementDataByUserId");
		command.Parameters.Add(CreateParameter("@UserId", userId));

		await using var reader = await command.ExecuteReaderAsync(cancellationToken);
		var sessions = new List<SessionScoreDto>();

		while (await reader.ReadAsync(cancellationToken))
		{
			sessions.Add(new SessionScoreDto
			{
				SessionDate = GetDateTime(reader, "SessionDate"),
				SessionName = GetString(reader, "SessionName"),
				FluencyScore = GetDecimal(reader, "FluencyScore"),
				ConfidenceScore = GetDecimal(reader, "ConfidenceScore"),
				MistakeCount = GetInt32(reader, "MistakeCount")
			});
		}

		return sessions;
	}

	public async Task<List<WeeklyScoreDto>> GetWeeklyFluencyScoresAsync(long userId, CancellationToken cancellationToken = default)
	{
		var connection = DbContext.Database.GetDbConnection();
		await EnsureConnectionOpenAsync(connection, cancellationToken);

		await using var command = CreateStoredProcedureCommand(connection, "dbo.uspGetWeeklyFluencyScoreByUserId");
		command.Parameters.Add(CreateParameter("@UserId", userId));

		await using var reader = await command.ExecuteReaderAsync(cancellationToken);
		var weeklyScores = new List<WeeklyScoreDto>();

		while (await reader.ReadAsync(cancellationToken))
		{
			weeklyScores.Add(new WeeklyScoreDto
			{
				WeekLabel = GetString(reader, "WeekLabel"),
				AvgFluencyScore = GetDecimal(reader, "AvgFluencyScore")
			});
		}

		return weeklyScores;
	}

	public async Task<List<GrammarProgressResponseDto>> GetGrammarProgressAsync(long userId, CancellationToken cancellationToken = default)
	{
		var connection = DbContext.Database.GetDbConnection();
		await EnsureConnectionOpenAsync(connection, cancellationToken);

		await using var command = CreateStoredProcedureCommand(connection, "dbo.uspGetGrammarProgressByUserId");
		command.Parameters.Add(CreateParameter("@UserId", userId));

		await using var reader = await command.ExecuteReaderAsync(cancellationToken);
		var items = new List<GrammarProgressResponseDto>();

		while (await reader.ReadAsync(cancellationToken))
		{
			items.Add(new GrammarProgressResponseDto
			{
				GrammarTag = GetString(reader, "GrammarTag"),
				TotalMistakes = GetInt32(reader, "TotalMistakes"),
				ResolvedMistakes = GetInt32(reader, "ResolvedMistakes"),
				ImprovementPercent = GetDecimal(reader, "ImprovementPercent"),
				ProgressBarValue = GetInt32(reader, "ProgressBarValue")
			});
		}

		return items;
	}

	public async Task<List<RepracticeSessionResponseDto>> GetRepracticeHistoryAsync(long userId, int pageNumber = 1, int pageSize = 10, CancellationToken cancellationToken = default)
	{
		var connection = DbContext.Database.GetDbConnection();
		await EnsureConnectionOpenAsync(connection, cancellationToken);

		await using var command = CreateStoredProcedureCommand(connection, "dbo.uspGetRepracticeSessionListByUserId");
		command.Parameters.Add(CreateParameter("@UserId", userId));
		command.Parameters.Add(CreateParameter("@Status", DBNull.Value));
		command.Parameters.Add(CreateParameter("@PageNumber", pageNumber));
		command.Parameters.Add(CreateParameter("@PageSize", pageSize));

		await using var reader = await command.ExecuteReaderAsync(cancellationToken);
		var items = new List<RepracticeSessionResponseDto>();

		while (await reader.ReadAsync(cancellationToken))
		{
			items.Add(new RepracticeSessionResponseDto
			{
				RepracticeSessionId = GetInt64(reader, "RepracticeSessionId"),
				SourceSessionId = GetInt64(reader, "SourceSessionId"),
				Status = GetString(reader, "Status"),
				TotalMistakes = GetInt32(reader, "TotalMistakes"),
				CompletedRounds = GetInt32(reader, "CompletedRounds"),
				ImprovementPercent = GetDecimal(reader, "ImprovementPercent"),
				GeneratedDate = GetDateTime(reader, "GeneratedDate")
			});
		}

		return items;
	}

	private static DbCommand CreateStoredProcedureCommand(DbConnection connection, string storedProcedureName)
	{
		var command = connection.CreateCommand();
		command.CommandText = storedProcedureName;
		command.CommandType = CommandType.StoredProcedure;
		return command;
	}

	private static SqlParameter CreateParameter(string parameterName, object? value)
	{
		return new SqlParameter(parameterName, value ?? DBNull.Value);
	}

	private static async Task EnsureConnectionOpenAsync(DbConnection connection, CancellationToken cancellationToken)
	{
		if (connection.State != ConnectionState.Open)
		{
			await connection.OpenAsync(cancellationToken);
		}
	}

	private static int GetOrdinal(DbDataReader reader, string columnName)
	{
		return reader.GetOrdinal(columnName);
	}

	private static string GetString(DbDataReader reader, string columnName)
	{
		var ordinal = GetOrdinal(reader, columnName);
		return reader.IsDBNull(ordinal) ? string.Empty : reader.GetString(ordinal);
	}

	private static string? GetNullableString(DbDataReader reader, string columnName)
	{
		var ordinal = GetOrdinal(reader, columnName);
		return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
	}

	private static bool GetBoolean(DbDataReader reader, string columnName)
	{
		var ordinal = GetOrdinal(reader, columnName);
		return reader.IsDBNull(ordinal) == false && reader.GetBoolean(ordinal);
	}

	private static int GetInt32(DbDataReader reader, string columnName)
	{
		var ordinal = GetOrdinal(reader, columnName);
		return reader.IsDBNull(ordinal) ? 0 : reader.GetInt32(ordinal);
	}

	private static long GetInt64(DbDataReader reader, string columnName)
	{
		var ordinal = GetOrdinal(reader, columnName);
		return reader.IsDBNull(ordinal) ? 0 : reader.GetInt64(ordinal);
	}

	private static decimal GetDecimal(DbDataReader reader, string columnName)
	{
		var ordinal = GetOrdinal(reader, columnName);
		return reader.IsDBNull(ordinal) ? 0 : reader.GetDecimal(ordinal);
	}

	private static DateTime GetDateTime(DbDataReader reader, string columnName)
	{
		var ordinal = GetOrdinal(reader, columnName);
		return reader.IsDBNull(ordinal) ? DateTime.MinValue : reader.GetDateTime(ordinal);
	}

	private static DateTime? GetNullableDateTime(DbDataReader reader, string columnName)
	{
		var ordinal = GetOrdinal(reader, columnName);
		return reader.IsDBNull(ordinal) ? null : reader.GetDateTime(ordinal);
	}

	private static long? GetNullableInt64(DbDataReader reader, string columnName)
	{
		var ordinal = GetOrdinal(reader, columnName);
		return reader.IsDBNull(ordinal) ? null : reader.GetInt64(ordinal);
	}

	private static decimal? GetNullableDecimal(DbDataReader reader, string columnName)
	{
		var ordinal = GetOrdinal(reader, columnName);
		return reader.IsDBNull(ordinal) ? null : reader.GetDecimal(ordinal);
	}
}
