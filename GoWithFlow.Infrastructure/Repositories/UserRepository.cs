using System.Data;
using System.Data.Common;
using GoWithFlow.Application.DTOs.Responses;
using GoWithFlow.Application.DTOs.Responses.LiveSession;
using GoWithFlow.Application.DTOs.Responses.Session;
using GoWithFlow.Application.DTOs.Responses.User;
using GoWithFlow.Application.Interfaces.Repositories;
using GoWithFlow.Domain.Entities;
using GoWithFlow.Infrastructure.Data;
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
		var connection = DbContext.Database.GetDbConnection();
		await EnsureConnectionOpenAsync(connection, cancellationToken);

		await using var command = CreateStoredProcedureCommand(connection, "dbo.uspGetUserByMobileNumber");
		command.Parameters.Add(CreateParameter("@MobileNumber", mobileNumber));

		await using var reader = await DbCommandHelper.ExecuteReaderAsync(command, cancellationToken);

		return await reader.ReadAsync(cancellationToken) ? MapUserFromReader(reader) : null;
	}

	public async Task<User?> GetByUserIdAsync(long userId, CancellationToken cancellationToken = default)
	{
		var connection = DbContext.Database.GetDbConnection();
		await EnsureConnectionOpenAsync(connection, cancellationToken);

		await using var command = CreateStoredProcedureCommand(connection, "dbo.uspGetUserByUserId");
		command.Parameters.Add(CreateParameter("@UserId", userId));

		await using var reader = await DbCommandHelper.ExecuteReaderAsync(command, cancellationToken);

		return await reader.ReadAsync(cancellationToken) ? MapUserFromReader(reader) : null;
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

		var result = await DbCommandHelper.ExecuteScalarAsync(command, cancellationToken);

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

		await DbCommandHelper.ExecuteNonQueryAsync(command, cancellationToken);
	}

	public async Task SoftDeleteUserAsync(long userId, string deletedBy, string ipAddress, CancellationToken cancellationToken = default)
	{
		var connection = DbContext.Database.GetDbConnection();
		await EnsureConnectionOpenAsync(connection, cancellationToken);

		await using var command = CreateStoredProcedureCommand(connection, "dbo.uspSoftDeleteUser");
		command.Parameters.Add(CreateParameter("@UserId", userId));
		command.Parameters.Add(CreateParameter("@DeletedBy", deletedBy));
		command.Parameters.Add(CreateParameter("@IPAddress", ipAddress));

		await DbCommandHelper.ExecuteNonQueryAsync(command, cancellationToken);
	}

	public async Task<UserProfileResponseDto?> GetUserProfileAsync(long userId, CancellationToken cancellationToken = default)
	{
		var connection = DbContext.Database.GetDbConnection();
		await EnsureConnectionOpenAsync(connection, cancellationToken);

		await using var command = CreateStoredProcedureCommand(connection, "dbo.uspGetUserProfileByUserId");
		command.Parameters.Add(CreateParameter("@UserId", userId));

		await using var reader = await DbCommandHelper.ExecuteReaderAsync(command, cancellationToken);

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

		UserDashboardResponseDto dashboard;

		await using (var reader = await DbCommandHelper.ExecuteReaderAsync(command, cancellationToken))
		{
			if (await reader.ReadAsync(cancellationToken) == false)
			{
				return null;
			}

			dashboard = new UserDashboardResponseDto
			{
				UserName = GetString(reader, "UserName"),
				CurrentStreak = GetInt32(reader, "CurrentStreak"),
				TodayDate = GetDateTime(reader, "TodayDate"),
				PendingRepracticeCount = GetInt32(reader, "PendingRepracticeCount")
			};

		}

		dashboard.RecentSessions = await GetRecentDashboardSessionsAsync(userId, cancellationToken);
		dashboard.PendingMistakes = await GetPendingDashboardMistakesAsync(userId, cancellationToken);

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

		await DbCommandHelper.ExecuteNonQueryAsync(command, cancellationToken);
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

		await DbCommandHelper.ExecuteNonQueryAsync(command, cancellationToken);
	}

	public async Task<StreakDataResponseDto> GetStreakDataAsync(long userId, CancellationToken cancellationToken = default)
	{
		var connection = DbContext.Database.GetDbConnection();
		await EnsureConnectionOpenAsync(connection, cancellationToken);

		await using var command = CreateStoredProcedureCommand(connection, "dbo.uspGetStreakDataByUserId");
		command.Parameters.Add(CreateParameter("@UserId", userId));

		await using var reader = await DbCommandHelper.ExecuteReaderAsync(command, cancellationToken);
		var response = new StreakDataResponseDto();

		if (await reader.ReadAsync(cancellationToken))
		{
			response.CurrentStreak = GetInt32(reader, "CurrentStreak");
			response.LongestStreak = GetInt32(reader, "LongestStreak");
		}

		await reader.CloseAsync();

		response.Last30Days = await DbContext.UserStreaks
			.AsNoTracking()
			.Where(userStreak => userStreak.UserId == userId && userStreak.IsDeleted == false)
			.OrderByDescending(userStreak => userStreak.StreakDate)
			.Take(30)
			.Select(userStreak => new DailyStreakDto
			{
				StreakDate = userStreak.StreakDate,
				SessionCount = userStreak.SessionCount,
				PracticeMinutes = userStreak.PracticeMinutes
			})
			.ToListAsync(cancellationToken);

		return response;
	}

	public async Task<List<UserBadgeDto>> GetBadgesAsync(long userId, CancellationToken cancellationToken = default)
	{
		var connection = DbContext.Database.GetDbConnection();
		await EnsureConnectionOpenAsync(connection, cancellationToken);

		await using var command = CreateStoredProcedureCommand(connection, "dbo.uspGetUserBadgeByUserId");
		command.Parameters.Add(CreateParameter("@UserId", userId));

		await using var reader = await DbCommandHelper.ExecuteReaderAsync(command, cancellationToken);
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

		await DbCommandHelper.ExecuteNonQueryAsync(command, cancellationToken);
	}

	public async Task<SessionDetailResponseDto?> GetSessionDetailAsync(long sessionId, long userId, CancellationToken cancellationToken = default)
	{
		var connection = DbContext.Database.GetDbConnection();
		await EnsureConnectionOpenAsync(connection, cancellationToken);

		await using var command = CreateStoredProcedureCommand(connection, "dbo.uspGetSessionDetailBySessionId");
		command.Parameters.Add(CreateParameter("@SessionId", sessionId));
		command.Parameters.Add(CreateParameter("@UserId", userId));

		await using var reader = await DbCommandHelper.ExecuteReaderAsync(command, cancellationToken);

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

		await reader.CloseAsync();

		response.MyPerformance = await GetSessionPerformanceSummaryAsync(sessionId, userId, cancellationToken);
		response.MyMistakes = await GetSessionMistakesAsync(sessionId, userId, response.SessionHeader.SessionName, response.SessionHeader.ScriptTitle, cancellationToken);
		response.ListenerFeedbackReceived = await GetSessionFeedbackCountsAsync(sessionId, userId, cancellationToken);
		response.AllMemberScores = await GetSessionMemberScoresAsync(sessionId, cancellationToken);

		return response;
	}

	private async Task<List<SessionListItemResponseDto>> GetRecentDashboardSessionsAsync(long userId, CancellationToken cancellationToken)
	{
		var sessions = await (
			from sessionMember in DbContext.SessionMembers.AsNoTracking()
			join session in DbContext.Sessions.AsNoTracking() on sessionMember.SessionId equals session.SessionId
			join script in DbContext.Scripts.AsNoTracking() on session.ScriptId equals script.ScriptId
			where sessionMember.UserId == userId
				&& sessionMember.IsDeleted == false
				&& session.IsDeleted == false
				&& script.IsDeleted == false
			select new
			{
				session.SessionId,
				session.SessionName,
				session.SessionMode,
				SessionDate = session.EndedDate ?? session.StartedDate ?? session.DateCreated,
				Duration = session.ActualDurationSec.HasValue && session.ActualDurationSec.Value > 0
					? (int)Math.Ceiling(session.ActualDurationSec.Value / 60.0)
					: session.SessionDuration,
				session.Status,
				script.ScriptTitle
			})
			.OrderByDescending(item => item.SessionDate)
			.ThenByDescending(item => item.SessionId)
			.Take(3)
			.ToListAsync(cancellationToken);

		var sessionIds = sessions.Select(item => item.SessionId).ToList();

		var voiceAverages = await DbContext.VoiceAnalyses
			.AsNoTracking()
			.Where(voiceAnalysis => voiceAnalysis.UserId == userId && sessionIds.Contains(voiceAnalysis.SessionId) && voiceAnalysis.IsDeleted == false)
			.GroupBy(voiceAnalysis => voiceAnalysis.SessionId)
			.Select(group => new
			{
				SessionId = group.Key,
				FluencyScore = (decimal?)group.Average(voiceAnalysis => voiceAnalysis.FluencyScore)
			})
			.ToDictionaryAsync(item => item.SessionId, item => item.FluencyScore, cancellationToken);

		var mistakeCounts = await DbContext.Mistakes
			.AsNoTracking()
			.Where(mistake => mistake.UserId == userId && sessionIds.Contains(mistake.SessionId) && mistake.IsDeleted == false)
			.GroupBy(mistake => mistake.SessionId)
			.Select(group => new
			{
				SessionId = group.Key,
				MistakeCount = group.Count()
			})
			.ToDictionaryAsync(item => item.SessionId, item => item.MistakeCount, cancellationToken);

		return sessions.Select(item => new SessionListItemResponseDto
		{
			SessionId = item.SessionId,
			SessionName = item.SessionName,
			SessionMode = item.SessionMode,
			SessionDate = item.SessionDate,
			Duration = item.Duration,
			FluencyScore = voiceAverages.TryGetValue(item.SessionId, out var fluencyScore) ? fluencyScore : null,
			MistakeCount = mistakeCounts.TryGetValue(item.SessionId, out var mistakeCount) ? mistakeCount : 0,
			Status = item.Status,
			ScriptTitle = item.ScriptTitle
		}).ToList();
	}

	private async Task<List<MistakeResponseDto>> GetPendingDashboardMistakesAsync(long userId, CancellationToken cancellationToken)
	{
		return await (
			from mistake in DbContext.Mistakes.AsNoTracking()
			join session in DbContext.Sessions.AsNoTracking() on mistake.SessionId equals session.SessionId into sessionJoin
			from session in sessionJoin.Where(item => item.IsDeleted == false).DefaultIfEmpty()
			join script in DbContext.Scripts.AsNoTracking() on mistake.ScriptId equals script.ScriptId into scriptJoin
			from script in scriptJoin.Where(item => item.IsDeleted == false).DefaultIfEmpty()
			where mistake.UserId == userId
				&& mistake.IsResolved == false
				&& mistake.IsDeleted == false
			orderby mistake.FirstOccurrence descending, mistake.MistakeId descending
			select new MistakeResponseDto
			{
				MistakeId = mistake.MistakeId,
				UserId = mistake.UserId,
				SessionId = mistake.SessionId,
				UtteranceId = mistake.UtteranceId,
				ScriptId = mistake.ScriptId,
				UtteranceText = mistake.UtteranceText,
				SpokenText = mistake.SpokenText,
				MistakeType = mistake.MistakeType,
				MistakeDetail = mistake.MistakeDetail,
				GrammarTag = mistake.GrammarTag,
				ContextTag = mistake.ContextTag,
				CorrectionText = mistake.CorrectionText,
				PracticeCount = mistake.PracticeCount,
				IsResolved = mistake.IsResolved,
				FirstOccurrence = mistake.FirstOccurrence,
				LastAttempt = mistake.LastAttempt,
				SessionName = session == null ? string.Empty : session.SessionName,
				ScriptTitle = script == null ? string.Empty : script.ScriptTitle
			})
			.Take(3)
			.ToListAsync(cancellationToken);
	}

	private async Task<PerformanceSummaryDto> GetSessionPerformanceSummaryAsync(long sessionId, long userId, CancellationToken cancellationToken)
	{
		var aggregate = await DbContext.VoiceAnalyses
			.AsNoTracking()
			.Where(voiceAnalysis => voiceAnalysis.SessionId == sessionId && voiceAnalysis.UserId == userId && voiceAnalysis.IsDeleted == false)
			.GroupBy(_ => 1)
			.Select(group => new PerformanceSummaryDto
			{
				FluencyScore = group.Average(voiceAnalysis => voiceAnalysis.FluencyScore),
				ConfidenceScore = group.Average(voiceAnalysis => voiceAnalysis.ConfidenceScore),
				SpeakingSpeedWpm = (int)group.Average(voiceAnalysis => voiceAnalysis.SpeakingSpeedWpm),
				PauseCount = group.Sum(voiceAnalysis => voiceAnalysis.PauseCount)
			})
			.FirstOrDefaultAsync(cancellationToken);

		return aggregate ?? new PerformanceSummaryDto();
	}

	private async Task<List<MistakeResponseDto>> GetSessionMistakesAsync(long sessionId, long userId, string sessionName, string scriptTitle, CancellationToken cancellationToken)
	{
		return await DbContext.Mistakes
			.AsNoTracking()
			.Where(mistake => mistake.SessionId == sessionId && mistake.UserId == userId && mistake.IsDeleted == false)
			.OrderBy(mistake => mistake.MistakeId)
			.Select(mistake => new MistakeResponseDto
			{
				MistakeId = mistake.MistakeId,
				UserId = userId,
				SessionId = sessionId,
				UtteranceId = mistake.UtteranceId,
				ScriptId = mistake.ScriptId,
				UtteranceText = mistake.UtteranceText,
				SpokenText = mistake.SpokenText,
				MistakeType = mistake.MistakeType,
				MistakeDetail = mistake.MistakeDetail,
				GrammarTag = mistake.GrammarTag,
				ContextTag = mistake.ContextTag,
				CorrectionText = mistake.CorrectionText,
				PracticeCount = mistake.PracticeCount,
				IsResolved = mistake.IsResolved,
				FirstOccurrence = mistake.FirstOccurrence,
				LastAttempt = mistake.LastAttempt,
				SessionName = sessionName,
				ScriptTitle = scriptTitle
			})
			.ToListAsync(cancellationToken);
	}

	private async Task<List<FeedbackCountDto>> GetSessionFeedbackCountsAsync(long sessionId, long userId, CancellationToken cancellationToken)
	{
		return await DbContext.ListenerFeedbacks
			.AsNoTracking()
			.Where(feedback => feedback.SessionId == sessionId && feedback.TargetUserId == userId && feedback.IsDeleted == false)
			.GroupBy(feedback => feedback.FeedbackTag)
			.OrderByDescending(group => group.Count())
			.Select(group => new FeedbackCountDto
			{
				FeedbackTag = group.Key,
				Count = group.Count()
			})
			.ToListAsync(cancellationToken);
	}

	private async Task<List<MemberScoreDto>> GetSessionMemberScoresAsync(long sessionId, CancellationToken cancellationToken)
	{
		var members = await (
			from sessionMember in DbContext.SessionMembers.AsNoTracking()
			join user in DbContext.Users.AsNoTracking() on sessionMember.UserId equals user.UserId
			where sessionMember.SessionId == sessionId
				&& sessionMember.IsDeleted == false
				&& user.IsDeleted == false
			orderby sessionMember.SlotIndex, sessionMember.SessionMemberId
			select new
			{
				user.UserId,
				user.FullName
			})
			.ToListAsync(cancellationToken);

		var userIds = members.Select(member => member.UserId).ToList();

		var voiceAggregates = await DbContext.VoiceAnalyses
			.AsNoTracking()
			.Where(voiceAnalysis => voiceAnalysis.SessionId == sessionId && userIds.Contains(voiceAnalysis.UserId) && voiceAnalysis.IsDeleted == false)
			.GroupBy(voiceAnalysis => voiceAnalysis.UserId)
			.Select(group => new
			{
				UserId = group.Key,
				FluencyScore = group.Average(voiceAnalysis => voiceAnalysis.FluencyScore),
				ConfidenceScore = group.Average(voiceAnalysis => voiceAnalysis.ConfidenceScore)
			})
			.ToDictionaryAsync(item => item.UserId, cancellationToken);

		var mistakeCounts = await DbContext.Mistakes
			.AsNoTracking()
			.Where(mistake => mistake.SessionId == sessionId && userIds.Contains(mistake.UserId) && mistake.IsDeleted == false)
			.GroupBy(mistake => mistake.UserId)
			.Select(group => new
			{
				UserId = group.Key,
				MistakeCount = group.Count()
			})
			.ToDictionaryAsync(item => item.UserId, item => item.MistakeCount, cancellationToken);

		var listenerRatings = await DbContext.ListenerFeedbacks
			.AsNoTracking()
			.Where(feedback => feedback.SessionId == sessionId && userIds.Contains(feedback.TargetUserId) && feedback.IsDeleted == false)
			.GroupBy(feedback => feedback.TargetUserId)
			.Select(group => new
			{
				UserId = group.Key,
				ListenerRating = group.Count()
			})
			.ToDictionaryAsync(item => item.UserId, item => item.ListenerRating, cancellationToken);

		return members.Select(member => new MemberScoreDto
		{
			UserId = member.UserId,
			FullName = member.FullName,
			FluencyScore = voiceAggregates.TryGetValue(member.UserId, out var voiceAggregate) ? voiceAggregate.FluencyScore : 0m,
			ConfidenceScore = voiceAggregates.TryGetValue(member.UserId, out voiceAggregate) ? voiceAggregate.ConfidenceScore : 0m,
			MistakeCount = mistakeCounts.TryGetValue(member.UserId, out var mistakeCount) ? mistakeCount : 0,
			ListenerRating = listenerRatings.TryGetValue(member.UserId, out var listenerRating) ? listenerRating : 0m
		}).ToList();
	}

	public async Task<List<SessionScoreDto>> GetImprovementSessionsAsync(long userId, CancellationToken cancellationToken = default)
	{
		var connection = DbContext.Database.GetDbConnection();
		await EnsureConnectionOpenAsync(connection, cancellationToken);

		await using var command = CreateStoredProcedureCommand(connection, "dbo.uspGetImprovementDataByUserId");
		command.Parameters.Add(CreateParameter("@UserId", userId));

		await using var reader = await DbCommandHelper.ExecuteReaderAsync(command, cancellationToken);
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

		await using var reader = await DbCommandHelper.ExecuteReaderAsync(command, cancellationToken);
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

		await using var reader = await DbCommandHelper.ExecuteReaderAsync(command, cancellationToken);
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

		await using var reader = await DbCommandHelper.ExecuteReaderAsync(command, cancellationToken);
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

	private static User MapUserFromReader(DbDataReader reader)
	{
		return new User
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
			CreatedBy = GetString(reader, "CreatedBy"),
			DateCreated = GetDateTime(reader, "DateCreated"),
			IPAddress = GetString(reader, "IPAddress"),
			UpdatedBy = GetNullableString(reader, "UpdatedBy"),
			LastUpdated = GetNullableDateTime(reader, "LastUpdated"),
			IsDeleted = GetBoolean(reader, "IsDeleted")
		};
	}

	private DbCommand CreateStoredProcedureCommand(DbConnection connection, string storedProcedureName)
	{
		var command = connection.CreateCommand();
		command.CommandText = DbCommandHelper.QualifyRoutineName(DbContext.DatabaseProvider, storedProcedureName);
		command.CommandType = CommandType.StoredProcedure;
		return command;
	}

	private DbParameter CreateParameter(string parameterName, object? value)
	{
		return DbCommandHelper.CreateParameter(DbContext.DatabaseProvider, parameterName, value);
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
