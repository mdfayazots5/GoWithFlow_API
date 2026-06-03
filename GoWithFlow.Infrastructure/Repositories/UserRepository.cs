using System.Data;
using System.Data.Common;
using System.Text.Json;
using GoWithFlow.Application.DTOs.Requests.LiveSession;
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

	public async Task UpdateUserByAdminAsync(
		long userId,
		string fullName,
		string mobileNumber,
		string? email,
		string ageGroup,
		string preferredHintLanguage,
		string? avatarUrl,
		string? passwordHash,
		CancellationToken cancellationToken = default)
	{
		var connection = DbContext.Database.GetDbConnection();
		await EnsureConnectionOpenAsync(connection, cancellationToken);

		await using var cmd = connection.CreateCommand();

		// Build SQL using provider-correct lowercase (PostgreSQL) or PascalCase (SQL Server) identifiers.
		// Never use EF Core Update() for this entity — EF Core quotes PascalCase names which
		// PostgreSQL treats as case-sensitive, causing "relation not found" errors.
		if (IsPostgres)
		{
			var sql = "UPDATE tbluser SET fullname=@fn, mobilenumber=@mn, email=@em, " +
			          "agegroup=@ag, preferredhintlanguage=@phl, avatarurl=@au, " +
			          "updatedby='Admin', lastupdated=NOW()" +
			          (passwordHash is not null ? ", passwordhash=@pw" : "") +
			          " WHERE userid=@id AND isdeleted=FALSE";
			cmd.CommandText = sql;
		}
		else
		{
			var sql = "UPDATE dbo.tblUser SET FullName=@fn, MobileNumber=@mn, Email=@em, " +
			          "AgeGroup=@ag, PreferredHintLanguage=@phl, AvatarUrl=@au, " +
			          "UpdatedBy='Admin', LastUpdated=GETDATE()" +
			          (passwordHash is not null ? ", PasswordHash=@pw" : "") +
			          " WHERE UserId=@id AND IsDeleted=0";
			cmd.CommandText = sql;
		}

		// Use cmd.CreateParameter() directly — NOT the CreateParameter() helper, which normalizes
		// names to "p_" prefix for stored procedures. This is a raw SQL command; parameter names
		// must match the placeholders in the SQL string exactly.
		void AddParam(string name, object? value)
		{
			var p = cmd.CreateParameter();
			p.ParameterName = name;
			p.Value = value ?? DBNull.Value;
			cmd.Parameters.Add(p);
		}

		AddParam("fn",  fullName);
		AddParam("mn",  mobileNumber);
		AddParam("em",  email);
		AddParam("ag",  ageGroup);
		AddParam("phl", preferredHintLanguage);
		AddParam("au",  avatarUrl);
		AddParam("id",  userId);

		if (passwordHash is not null)
			AddParam("pw", passwordHash);

		await DbCommandHelper.ExecuteNonQueryAsync(cmd, cancellationToken);
	}

	public async Task UpdateAvatarUrlAsync(long userId, string objectKey, CancellationToken cancellationToken = default)
	{
		var connection = DbContext.Database.GetDbConnection();
		await EnsureConnectionOpenAsync(connection, cancellationToken);

		await using var cmd = connection.CreateCommand();
		cmd.CommandText = IsPostgres
			? "UPDATE tbluser SET avatarurl=@key, lastupdated=NOW() WHERE userid=@id AND isdeleted=FALSE"
			: "UPDATE dbo.tblUser SET AvatarUrl=@key, LastUpdated=GETDATE() WHERE UserId=@id AND IsDeleted=0";
		cmd.Parameters.Add(CreateParameter("@key", objectKey));
		cmd.Parameters.Add(CreateParameter("@id",  userId));

		await DbCommandHelper.ExecuteNonQueryAsync(cmd, cancellationToken);
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

		// Also check milestone / certificate badges
		await using var milestoneCommand = CreateStoredProcedureCommand(connection, "dbo.uspCheckAndAwardMilestoneBadge");
		milestoneCommand.Parameters.Add(CreateParameter("@UserId", userId));
		milestoneCommand.Parameters.Add(CreateParameter("@CreatedBy", createdBy));
		milestoneCommand.Parameters.Add(CreateParameter("@IPAddress", ipAddress));

		await DbCommandHelper.ExecuteNonQueryAsync(milestoneCommand, cancellationToken);
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
				user.FullName,
				user.AvatarUrl
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
			AvatarUrl = member.AvatarUrl,
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

	public async Task<WeeklyReportResponseDto> GetWeeklyReportAsync(long userId, CancellationToken cancellationToken = default)
	{
		var now = DateTime.UtcNow;
		// Week starts on Monday
		var dayOffset = (int)now.DayOfWeek == 0 ? 6 : (int)now.DayOfWeek - 1;
		var weekStart = now.Date.AddDays(-dayOffset);
		var prevWeekStart = weekStart.AddDays(-7);

		// Sessions this week: sessions where the user was a member, session COMPLETED
		var thisWeekSessionIds = await (
			from sm in DbContext.SessionMembers.AsNoTracking()
			join s in DbContext.Sessions.AsNoTracking() on sm.SessionId equals s.SessionId
			where sm.UserId == userId
				&& sm.IsDeleted == false
				&& s.IsDeleted == false
				&& s.Status == "COMPLETED"
				&& (s.EndedDate ?? s.StartedDate ?? s.DateCreated) >= weekStart
			select s.SessionId
		).Distinct().ToListAsync(cancellationToken);

		// Avg fluency this week vs last week
		var thisWeekFluency = await DbContext.VoiceAnalyses.AsNoTracking()
			.Where(v => v.UserId == userId && v.IsDeleted == false && v.RecordedAt >= weekStart)
			.Select(v => (decimal?)v.FluencyScore)
			.AverageAsync(cancellationToken) ?? 0m;

		var prevWeekFluency = await DbContext.VoiceAnalyses.AsNoTracking()
			.Where(v => v.UserId == userId && v.IsDeleted == false && v.RecordedAt >= prevWeekStart && v.RecordedAt < weekStart)
			.Select(v => (decimal?)v.FluencyScore)
			.AverageAsync(cancellationToken) ?? 0m;

		// Practice minutes this week: sum ActualDurationSec from sessions
		var practiceSeconds = await (
			from sm in DbContext.SessionMembers.AsNoTracking()
			join s in DbContext.Sessions.AsNoTracking() on sm.SessionId equals s.SessionId
			where sm.UserId == userId
				&& sm.IsDeleted == false
				&& s.IsDeleted == false
				&& s.Status == "COMPLETED"
				&& (s.EndedDate ?? s.StartedDate ?? s.DateCreated) >= weekStart
			select (int?)(s.ActualDurationSec ?? s.SessionDuration * 60)
		).SumAsync(cancellationToken) ?? 0;

		// Errors this week
		var errorsThisWeek = await DbContext.Mistakes.AsNoTracking()
			.CountAsync(m => m.UserId == userId && m.IsDeleted == false && m.FirstOccurrence >= weekStart, cancellationToken);

		// Errors resolved this week (resolved via repractice)
		var errorsResolvedThisWeek = await DbContext.Mistakes.AsNoTracking()
			.CountAsync(m => m.UserId == userId && m.IsDeleted == false && m.IsResolved == true && m.LastAttempt >= weekStart, cancellationToken);

		// Weakest grammar tag this week
		var weakestTag = await DbContext.Mistakes.AsNoTracking()
			.Where(m => m.UserId == userId && m.IsDeleted == false && m.FirstOccurrence >= weekStart && !string.IsNullOrEmpty(m.GrammarTag))
			.GroupBy(m => m.GrammarTag)
			.OrderByDescending(g => g.Count())
			.Select(g => g.Key)
			.FirstOrDefaultAsync(cancellationToken) ?? string.Empty;

		// Top improvement: fluency delta
		var fluencyDelta = Math.Round(thisWeekFluency - prevWeekFluency, 1);
		var topImprovement = fluencyDelta > 0
			? $"Fluency score +{fluencyDelta} pts vs last week"
			: fluencyDelta < 0
				? $"Fluency score {fluencyDelta} pts vs last week"
				: "Maintaining current fluency level";

		// Script recommendation 1: target weakest grammar tag
		var rec1 = await DbContext.Scripts.AsNoTracking()
			.Where(s => s.IsActive && s.IsDeleted == false
				&& (string.IsNullOrEmpty(weakestTag) || s.GrammarFocusTag == weakestTag))
			.OrderByDescending(s => s.UploadedDate)
			.Select(s => new { s.ScriptId, s.ScriptTitle })
			.FirstOrDefaultAsync(cancellationToken);

		// Script recommendation 2: a different category for variety
		var usedCategories = await (
			from sm in DbContext.SessionMembers.AsNoTracking()
			join s in DbContext.Sessions.AsNoTracking() on sm.SessionId equals s.SessionId
			where sm.UserId == userId && sm.IsDeleted == false && s.IsDeleted == false
				&& (s.EndedDate ?? s.StartedDate ?? s.DateCreated) >= now.AddDays(-14)
			select s.SessionMode
		).Distinct().ToListAsync(cancellationToken);

		var rec2 = await DbContext.Scripts.AsNoTracking()
			.Where(s => s.IsActive && s.IsDeleted == false
				&& (rec1 == null || s.ScriptId != rec1.ScriptId)
				&& !usedCategories.Contains(s.Category))
			.OrderByDescending(s => s.UploadedDate)
			.Select(s => new { s.ScriptId, s.ScriptTitle })
			.FirstOrDefaultAsync(cancellationToken);

		// Last session for re-engagement
		var lastSession = await (
			from sm in DbContext.SessionMembers.AsNoTracking()
			join s in DbContext.Sessions.AsNoTracking() on sm.SessionId equals s.SessionId
			where sm.UserId == userId && sm.IsDeleted == false && s.IsDeleted == false && s.Status == "COMPLETED"
			orderby (s.EndedDate ?? s.StartedDate ?? s.DateCreated) descending
			select (DateTime?)(s.EndedDate ?? s.StartedDate ?? s.DateCreated)
		).FirstOrDefaultAsync(cancellationToken);

		return new WeeklyReportResponseDto
		{
			SessionsThisWeek           = thisWeekSessionIds.Count,
			PracticeMinutesThisWeek    = practiceSeconds / 60,
			ErrorsDetectedThisWeek     = errorsThisWeek,
			ErrorsResolvedThisWeek     = errorsResolvedThisWeek,
			TopImprovementMetric       = topImprovement,
			WeakestGrammarTag          = weakestTag,
			RecommendedScript1Id       = rec1?.ScriptId ?? 0,
			RecommendedScript1Title    = rec1?.ScriptTitle ?? string.Empty,
			RecommendedScript2Id       = rec2?.ScriptId ?? 0,
			RecommendedScript2Title    = rec2?.ScriptTitle ?? string.Empty,
			IsReengagement             = thisWeekSessionIds.Count == 0,
			LastSessionDate            = lastSession
		};
	}

	public async Task<GuidedLearningPathResponseDto> GetGuidedLearningPathAsync(long userId, CancellationToken cancellationToken = default)
	{
		var recommendations = new List<LearningPathRecommendationDto>();
		var cutoff14Days = DateTime.UtcNow.AddDays(-14);

		// ── Rule 0 (highest priority): Goal-type-aware recommendation ─────────
		// PM Phase 2 Step 4 Req #3: recommendations must be filtered to scripts
		// relevant to the user's active goal type.
		string? activeGoalType = null;
		{
			var conn = DbContext.Database.GetDbConnection();
			await EnsureConnectionOpenAsync(conn, cancellationToken);
			await using var goalCmd = conn.CreateCommand();
			goalCmd.CommandText = IsPostgres
				? "SELECT goaltype FROM public.tblusergoal WHERE userid = @UserId AND isactive = TRUE AND isdeleted = FALSE ORDER BY datecreated DESC LIMIT 1"
				: "SELECT TOP 1 GoalType FROM dbo.tblUserGoal WHERE UserId = @UserId AND IsActive = 1 AND IsDeleted = 0 ORDER BY DateCreated DESC";
			var gp = goalCmd.CreateParameter();
			gp.ParameterName = "@UserId";
			gp.Value         = userId;
			goalCmd.Parameters.Add(gp);
			var scalar = await goalCmd.ExecuteScalarAsync(cancellationToken);
			if (scalar is not null && scalar != DBNull.Value)
				activeGoalType = scalar.ToString();
		}

		if (!string.IsNullOrEmpty(activeGoalType))
		{
			var goalCategories = activeGoalType switch
			{
				"interview"  => new[] { "Mock Interview",     "MockInterview"     },
				"grammar"    => new[] { "Grammar Drill",      "GrammarDrill"      },
				"vocabulary" => new[] { "Vocabulary Sprint",  "VocabularySprint"  },
				"fluency"    => new[] { "Fluency Drill",      "FluencyDrill"      },
				_            => Array.Empty<string>()
			};

			if (goalCategories.Length > 0)
			{
				var goalScript = await DbContext.Scripts.AsNoTracking()
					.Where(s => s.IsActive && s.IsDeleted == false && goalCategories.Contains(s.Category))
					.OrderByDescending(s => s.UploadedDate)
					.Select(s => new { s.ScriptId, s.ScriptTitle, s.Category, s.ComplexityLevel })
					.FirstOrDefaultAsync(cancellationToken);

				if (goalScript is not null)
				{
					var goalReason = activeGoalType switch
					{
						"interview"  => "Recommended for your Job Interview goal.",
						"grammar"    => "Recommended for your Better Grammar goal.",
						"vocabulary" => "Recommended for your Build Vocabulary goal.",
						"fluency"    => "Recommended for your Speak Fluently goal.",
						_            => "Recommended based on your active goal."
					};
					recommendations.Add(new LearningPathRecommendationDto
					{
						ScriptId           = goalScript.ScriptId,
						ScriptTitle        = goalScript.ScriptTitle,
						Category           = goalScript.Category,
						ComplexityLevel    = goalScript.ComplexityLevel,
						ReasonText         = goalReason,
						RecommendationType = "goal"
					});
				}
			}
		}

		// Rule 1: Unresolved mistakes → recommend RepracticeRound for top GrammarTag
		var topErrorTag = await DbContext.Mistakes.AsNoTracking()
			.Where(m => m.UserId == userId && m.IsDeleted == false && m.IsResolved == false && !string.IsNullOrEmpty(m.GrammarTag))
			.GroupBy(m => m.GrammarTag)
			.OrderByDescending(g => g.Count())
			.Select(g => g.Key)
			.FirstOrDefaultAsync(cancellationToken);

		if (!string.IsNullOrEmpty(topErrorTag))
		{
			var repracticeScript = await DbContext.Scripts.AsNoTracking()
				.Where(s => s.IsActive && s.IsDeleted == false
					&& (s.Category == "Repractice Round" || s.Category == "Repetition")
					&& s.GrammarFocusTag == topErrorTag)
				.OrderByDescending(s => s.UploadedDate)
				.Select(s => new { s.ScriptId, s.ScriptTitle, s.Category, s.ComplexityLevel })
				.FirstOrDefaultAsync(cancellationToken);

			if (repracticeScript is not null)
			{
				recommendations.Add(new LearningPathRecommendationDto
				{
					ScriptId           = repracticeScript.ScriptId,
					ScriptTitle        = repracticeScript.ScriptTitle,
					Category           = repracticeScript.Category,
					ComplexityLevel    = repracticeScript.ComplexityLevel,
					ReasonText         = $"You have unresolved mistakes in {topErrorTag}. Practice to fix them.",
					RecommendationType = "repractice"
				});
			}
		}

		// Rule 2: Last session in any category scored below 65 → repeat same category/level
		var lowScoreSession = await (
			from sm in DbContext.SessionMembers.AsNoTracking()
			join s in DbContext.Sessions.AsNoTracking() on sm.SessionId equals s.SessionId
			join va in DbContext.VoiceAnalyses.AsNoTracking() on new { sm.SessionId, sm.UserId } equals new { va.SessionId, va.UserId }
			where sm.UserId == userId && sm.IsDeleted == false && s.IsDeleted == false && va.IsDeleted == false
				&& s.Status == "COMPLETED"
			group new { s.SessionMode, va.FluencyScore } by new { s.SessionMode } into g
			select new { g.Key.SessionMode, AvgFluency = g.Average(x => (decimal)x.FluencyScore) }
		).Where(x => x.AvgFluency < 65).FirstOrDefaultAsync(cancellationToken);

		if (lowScoreSession is not null && recommendations.Count < 3)
		{
			var repeatScript = await DbContext.Scripts.AsNoTracking()
				.Where(s => s.IsActive && s.IsDeleted == false
					&& (s.Category == lowScoreSession.SessionMode || s.Category == "Grammar Drill"))
				.OrderByDescending(s => s.UploadedDate)
				.Select(s => new { s.ScriptId, s.ScriptTitle, s.Category, s.ComplexityLevel })
				.FirstOrDefaultAsync(cancellationToken);

			if (repeatScript is not null && recommendations.All(r => r.ScriptId != repeatScript.ScriptId))
			{
				recommendations.Add(new LearningPathRecommendationDto
				{
					ScriptId           = repeatScript.ScriptId,
					ScriptTitle        = repeatScript.ScriptTitle,
					Category           = repeatScript.Category,
					ComplexityLevel    = repeatScript.ComplexityLevel,
					ReasonText         = $"Your recent {lowScoreSession.SessionMode} sessions scored below 65. Extra practice will help.",
					RecommendationType = "low_score"
				});
			}
		}

		// Rule 4: User hasn't tried a category in 14+ days → suggest it for variety
		if (recommendations.Count < 3)
		{
			var recentCategories = await (
				from sm in DbContext.SessionMembers.AsNoTracking()
				join s in DbContext.Sessions.AsNoTracking() on sm.SessionId equals s.SessionId
				where sm.UserId == userId && sm.IsDeleted == false && s.IsDeleted == false
					&& (s.EndedDate ?? s.StartedDate ?? s.DateCreated) >= cutoff14Days
				select s.SessionMode
			).Distinct().ToListAsync(cancellationToken);

			var varietyScript = await DbContext.Scripts.AsNoTracking()
				.Where(s => s.IsActive && s.IsDeleted == false
					&& !recentCategories.Contains(s.Category)
					&& recommendations.Select(r => r.ScriptId).All(id => id != s.ScriptId))
				.OrderByDescending(s => s.UploadedDate)
				.Select(s => new { s.ScriptId, s.ScriptTitle, s.Category, s.ComplexityLevel })
				.FirstOrDefaultAsync(cancellationToken);

			if (varietyScript is not null)
			{
				recommendations.Add(new LearningPathRecommendationDto
				{
					ScriptId           = varietyScript.ScriptId,
					ScriptTitle        = varietyScript.ScriptTitle,
					Category           = varietyScript.Category,
					ComplexityLevel    = varietyScript.ComplexityLevel,
					ReasonText         = $"You haven't tried {varietyScript.Category} recently. Mix it up!",
					RecommendationType = "variety"
				});
			}
		}

		// Fallback: if no recommendations yet, pick the most recent active script
		if (recommendations.Count == 0)
		{
			var fallbackScript = await DbContext.Scripts.AsNoTracking()
				.Where(s => s.IsActive && s.IsDeleted == false)
				.OrderByDescending(s => s.UploadedDate)
				.Select(s => new { s.ScriptId, s.ScriptTitle, s.Category, s.ComplexityLevel })
				.FirstOrDefaultAsync(cancellationToken);

			if (fallbackScript is not null)
			{
				recommendations.Add(new LearningPathRecommendationDto
				{
					ScriptId           = fallbackScript.ScriptId,
					ScriptTitle        = fallbackScript.ScriptTitle,
					Category           = fallbackScript.Category,
					ComplexityLevel    = fallbackScript.ComplexityLevel,
					ReasonText         = "Start a new practice session!",
					RecommendationType = "variety"
				});
			}
		}

		return new GuidedLearningPathResponseDto { Recommendations = recommendations.Take(3).ToList() };
	}

	public async Task<InterviewPerformanceDashboardResponseDto> GetInterviewPerformanceDashboardAsync(long userId, CancellationToken cancellationToken = default)
	{
		var mockCategories = new[] { "Mock Interview", "Interview" };

		// All completed MockInterview session IDs where this user participated
		var mockSessionIds = await (
			from sm in DbContext.SessionMembers.AsNoTracking()
			join s in DbContext.Sessions.AsNoTracking() on sm.SessionId equals s.SessionId
			where sm.UserId == userId && sm.IsDeleted == false && s.IsDeleted == false
				&& s.Status == "COMPLETED"
				&& mockCategories.Contains(s.SessionMode)
			orderby (s.EndedDate ?? s.StartedDate ?? s.DateCreated) descending
			select new { s.SessionId, Date = s.EndedDate ?? s.StartedDate ?? s.DateCreated }
		).ToListAsync(cancellationToken);

		if (mockSessionIds.Count == 0)
		{
			return new InterviewPerformanceDashboardResponseDto { HasData = false };
		}

		var sessionIdList = mockSessionIds.Select(x => x.SessionId).ToList();

		// Candidate voice analysis: join VoiceAnalysis → Utterance → SpeakerLabel = "Candidate"
		var candidateAnalysis = await (
			from va in DbContext.VoiceAnalyses.AsNoTracking()
			join u in DbContext.Utterances.AsNoTracking() on va.UtteranceId equals u.UtteranceId
			where va.UserId == userId && va.IsDeleted == false
				&& sessionIdList.Contains(va.SessionId)
				&& u.SpeakerLabel == "Candidate"
			select new
			{
				va.SessionId,
				va.FluencyScore,
				va.ConfidenceScore,
				va.SpeakingSpeedWpm,
				u.FocusWord
			}
		).ToListAsync(cancellationToken);

		// Grammar errors from tblMistake for Candidate turns in these sessions
		var grammarMistakes = await DbContext.Mistakes.AsNoTracking()
			.Where(m => m.UserId == userId && m.IsDeleted == false
				&& sessionIdList.Contains(m.SessionId)
				&& !string.IsNullOrEmpty(m.GrammarTag))
			.Select(m => new { m.SessionId, m.GrammarTag })
			.ToListAsync(cancellationToken);

		// Build per-session metrics
		var sessionDateMap = mockSessionIds.ToDictionary(x => x.SessionId, x => x.Date);
		var sessionMistakeCounts = grammarMistakes.GroupBy(m => m.SessionId).ToDictionary(g => g.Key, g => g.Count());

		var sessionMetrics = candidateAnalysis
			.GroupBy(a => a.SessionId)
			.Select(g => new
			{
				SessionId       = g.Key,
				AvgFluency      = g.Average(a => (decimal)a.FluencyScore),
				AvgConfidence   = g.Average(a => (decimal)a.ConfidenceScore),
				AvgSpeedWpm     = g.Average(a => (decimal)a.SpeakingSpeedWpm),
				MistakeCount    = sessionMistakeCounts.TryGetValue(g.Key, out var mc) ? mc : 0,
				SessionDate     = sessionDateMap.TryGetValue(g.Key, out var d) ? d : DateTime.MinValue
			})
			.OrderByDescending(x => x.SessionDate)
			.ToList();

		// Take last 5 sessions for the composite score
		var last5 = sessionMetrics.Take(5).ToList();

		decimal ComputeReadiness(decimal fluency, decimal confidence, int mistakes, decimal speedWpm)
		{
			var fluencyScore     = Math.Min(fluency, 100m);
			var confidenceScore  = Math.Min(confidence, 100m);
			var errorRate        = Math.Min(mistakes / 5.0m, 1.0m);  // 5 errors per session = 100% error rate
			var errorScore       = (1m - errorRate) * 100m;
			var speedScore       = Math.Min(speedWpm / 120m, 1.0m) * 100m;  // 120 WPM = full answer-length credit

			return Math.Round(fluencyScore * 0.40m + confidenceScore * 0.25m + errorScore * 0.20m + speedScore * 0.15m, 1);
		}

		var timeline = sessionMetrics
			.Take(10)
			.Select(sm => new InterviewSessionTimelineDto
			{
				SessionId      = sm.SessionId,
				SessionDate    = sm.SessionDate,
				FluencyScore   = Math.Round(sm.AvgFluency, 1),
				ConfidenceScore= Math.Round(sm.AvgConfidence, 1),
				MistakeCount   = sm.MistakeCount,
				ReadinessScore = ComputeReadiness(sm.AvgFluency, sm.AvgConfidence, sm.MistakeCount, sm.AvgSpeedWpm)
			})
			.OrderBy(t => t.SessionDate)
			.ToList();

		var overallReadiness = last5.Count > 0
			? Math.Round(last5.Average(sm => ComputeReadiness(sm.AvgFluency, sm.AvgConfidence, sm.MistakeCount, sm.AvgSpeedWpm)), 1)
			: 0m;

		// Trend: compare first half vs second half of timeline
		string readinessTrend = "Stable";
		if (timeline.Count >= 3)
		{
			var midpoint  = timeline.Count / 2;
			var firstHalf = timeline.Take(midpoint).Average(t => t.ReadinessScore);
			var secondHalf= timeline.Skip(midpoint).Average(t => t.ReadinessScore);
			if (secondHalf - firstHalf > 3m)       readinessTrend = "Improving";
			else if (firstHalf - secondHalf > 3m)  readinessTrend = "Declining";
		}

		// Top 3 grammar error tags
		var topGrammarErrors = grammarMistakes
			.GroupBy(m => m.GrammarTag)
			.OrderByDescending(g => g.Count())
			.Take(3)
			.Select(g => new InterviewGrammarWeaknessDto { GrammarTag = g.Key!, ErrorCount = g.Count() })
			.ToList();

		// FocusWord performance (Candidate turns only)
		var focusWordGroups = candidateAnalysis
			.Where(a => !string.IsNullOrEmpty(a.FocusWord))
			.GroupBy(a => a.FocusWord!)
			.Select(g => new InterviewFocusWordDto
			{
				FocusWord    = g.Key,
				TimesSpoken  = g.Count(),
				TimesCorrect = g.Count(a => a.FluencyScore >= 70),
				CorrectRate  = g.Count() > 0 ? Math.Round(g.Count(a => a.FluencyScore >= 70) * 100m / g.Count(), 1) : 0m
			})
			.OrderBy(fw => fw.CorrectRate)
			.Take(10)
			.ToList();

		// Answer length trend (WPM across sessions)
		string answerLengthTrend = "Stable";
		if (sessionMetrics.Count >= 3)
		{
			var midpoint   = sessionMetrics.Count / 2;
			var firstHalf  = sessionMetrics.Skip(midpoint).Average(s => s.AvgSpeedWpm);  // older
			var secondHalf = sessionMetrics.Take(midpoint).Average(s => s.AvgSpeedWpm);  // newer
			if (secondHalf - firstHalf > 5m)      answerLengthTrend = "Improving";
			else if (firstHalf - secondHalf > 5m) answerLengthTrend = "Declining";
		}

		var avgSpeedOverall = sessionMetrics.Count > 0 ? Math.Round(sessionMetrics.Average(s => s.AvgSpeedWpm), 1) : 0m;

		// Recommended script targeting weakest grammar tag
		long   recScriptId     = 0;
		string recScriptTitle  = string.Empty;
		string recScriptReason = string.Empty;

		var weakestTag = topGrammarErrors.FirstOrDefault()?.GrammarTag;
		if (!string.IsNullOrEmpty(weakestTag))
		{
			var rec = await DbContext.Scripts.AsNoTracking()
				.Where(s => s.IsActive && s.IsDeleted == false
					&& mockCategories.Contains(s.Category)
					&& s.GrammarFocusTag == weakestTag)
				.OrderByDescending(s => s.UploadedDate)
				.Select(s => new { s.ScriptId, s.ScriptTitle, s.GrammarFocusTag })
				.FirstOrDefaultAsync(cancellationToken);

			if (rec is not null)
			{
				recScriptId     = rec.ScriptId;
				recScriptTitle  = rec.ScriptTitle;
				recScriptReason = $"Target your most frequent error: {weakestTag}";
			}
		}

		if (recScriptId == 0)
		{
			var fallback = await DbContext.Scripts.AsNoTracking()
				.Where(s => s.IsActive && s.IsDeleted == false && mockCategories.Contains(s.Category))
				.OrderByDescending(s => s.UploadedDate)
				.Select(s => new { s.ScriptId, s.ScriptTitle })
				.FirstOrDefaultAsync(cancellationToken);

			if (fallback is not null)
			{
				recScriptId     = fallback.ScriptId;
				recScriptTitle  = fallback.ScriptTitle;
				recScriptReason = "Practice more Mock Interview sessions to build your score.";
			}
		}

		return new InterviewPerformanceDashboardResponseDto
		{
			HasData                 = true,
			InterviewReadinessScore = overallReadiness,
			ReadinessTrend          = readinessTrend,
			TotalMockSessions       = mockSessionIds.Count,
			SessionTimeline         = timeline,
			TopGrammarErrors        = topGrammarErrors,
			FocusWordPerformance    = focusWordGroups,
			AnswerLengthTrend       = answerLengthTrend,
			AvgAnswerSpeedWpm       = avgSpeedOverall,
			RecommendedScriptId     = recScriptId,
			RecommendedScriptTitle  = recScriptTitle,
			RecommendedScriptReason = recScriptReason
		};
	}

	public async Task<PronunciationTimelineResponseDto> GetPronunciationTimelineAsync(long userId, CancellationToken cancellationToken = default)
	{
		// Fetch last 30 sessions of voice analysis for this user (most recent first)
		var rawRows = await DbContext.VoiceAnalyses.AsNoTracking()
			.Where(va => va.UserId == userId && va.IsDeleted == false && va.PronunciationJson != null)
			.OrderByDescending(va => va.RecordedAt)
			.Take(300)  // up to 300 turns → typically ~30 sessions
			.Select(va => new { va.SessionId, va.PronunciationJson, va.RecordedAt })
			.ToListAsync(cancellationToken);

		if (rawRows.Count == 0)
		{
			return new PronunciationTimelineResponseDto { HasData = false };
		}

		// Parse pronunciation issues per session
		var sessionIssues = new Dictionary<long, (DateTime Date, List<string> Words, List<string> Notes)>();
		foreach (var row in rawRows)
		{
			var issues = DeserializePronunciationJson(row.PronunciationJson);
			if (issues.Count == 0) continue;

			if (!sessionIssues.TryGetValue(row.SessionId, out var existing))
			{
				sessionIssues[row.SessionId] = (row.RecordedAt, new List<string>(), new List<string>());
				existing = sessionIssues[row.SessionId];
			}

			foreach (var issue in issues)
			{
				if (!string.IsNullOrWhiteSpace(issue.Word))
				{
					existing.Words.Add(issue.Word.ToLowerInvariant());
					existing.Notes.Add(issue.IssueNote ?? string.Empty);
				}
			}
		}

		if (sessionIssues.Count == 0)
		{
			return new PronunciationTimelineResponseDto { HasData = false };
		}

		// Build word → session-entries map
		var wordSessionMap = new Dictionary<string, List<PronunciationSessionEntryDto>>(StringComparer.OrdinalIgnoreCase);
		var wordNoteMap    = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

		foreach (var (sessionId, (date, words, notes)) in sessionIssues.OrderBy(x => x.Value.Date))
		{
			for (var i = 0; i < words.Count; i++)
			{
				var word = words[i];
				if (!wordSessionMap.ContainsKey(word))
				{
					wordSessionMap[word] = new List<PronunciationSessionEntryDto>();
					wordNoteMap[word] = notes.Count > i ? notes[i] : string.Empty;
				}
				// Only add one entry per session per word
				if (wordSessionMap[word].All(e => e.SessionId != sessionId))
				{
					wordSessionMap[word].Add(new PronunciationSessionEntryDto
					{
						SessionId   = sessionId,
						SessionDate = date,
						HadIssue    = true,
						IssueNote   = notes.Count > i ? notes[i] : string.Empty
					});
				}
			}
		}

		// Get IPA reference + practice scripts for top problem words
		var topWords = wordSessionMap
			.OrderByDescending(kv => kv.Value.Count)
			.Take(20)
			.Select(kv => kv.Key)
			.ToList();

		var ipaMap = await DbContext.Utterances.AsNoTracking()
			.Where(u => u.FocusWord != null && topWords.Contains(u.FocusWord) && u.PronunciationNote != null)
			.Select(u => new { u.FocusWord, u.PronunciationNote })
			.Distinct()
			.ToListAsync(cancellationToken);

		var ipaDictionary = ipaMap
			.GroupBy(x => x.FocusWord!, StringComparer.OrdinalIgnoreCase)
			.ToDictionary(g => g.Key, g => g.First().PronunciationNote ?? string.Empty, StringComparer.OrdinalIgnoreCase);

		// Practice scripts for top words
		var practiceScriptMap = new Dictionary<string, (long ScriptId, string Title)>(StringComparer.OrdinalIgnoreCase);
		foreach (var word in topWords)
		{
			var script = await DbContext.Scripts.AsNoTracking()
				.Where(s => s.IsActive && s.IsDeleted == false
					&& DbContext.Utterances.Any(u => u.ScriptId == s.ScriptId && u.FocusWord == word))
				.OrderByDescending(s => s.UploadedDate)
				.Select(s => new { s.ScriptId, s.ScriptTitle })
				.FirstOrDefaultAsync(cancellationToken);

			if (script is not null)
			{
				practiceScriptMap[word] = (script.ScriptId, script.ScriptTitle);
			}
		}

		// Determine last-10-session IDs for persistence check
		var last10SessionIds = sessionIssues.Keys
			.OrderByDescending(id => sessionIssues[id].Date)
			.Take(10)
			.ToHashSet();

		// Build problem word list
		var problemWords = wordSessionMap
			.OrderByDescending(kv => kv.Value.Count)
			.Take(20)
			.Select(kv =>
			{
				var word    = kv.Key;
				var entries = kv.Value.OrderBy(e => e.SessionDate).ToList();
				var occurrencesInLast10 = entries.Count(e => last10SessionIds.Contains(e.SessionId));
				var isPersistent = occurrencesInLast10 >= 3;
				practiceScriptMap.TryGetValue(word, out var scriptInfo);

				return new PronunciationProblemWordDto
				{
					Word              = word,
					TotalOccurrences  = entries.Count,
					IpaReference      = ipaDictionary.TryGetValue(word, out var ipa) ? ipa : string.Empty,
					IsPersistent      = isPersistent,
					PracticeScriptId  = scriptInfo.ScriptId,
					PracticeScriptTitle = scriptInfo.Title ?? string.Empty,
					SessionHistory    = entries
				};
			})
			.ToList();

		var topPersistent = problemWords.Where(w => w.IsPersistent).Take(5).ToList();

		return new PronunciationTimelineResponseDto
		{
			HasData           = true,
			ProblemWords      = problemWords,
			TopPersistentWords = topPersistent
		};
	}

	private bool IsPostgres => DatabaseProviderNames.IsPostgreSql(DbContext.DatabaseProvider);

	public async Task SetUserGoalAsync(long userId, string goalType, int timelineWeeks, string detectedLevel, decimal startingScore, CancellationToken cancellationToken = default)
	{
		var connection = DbContext.Database.GetDbConnection();
		await EnsureConnectionOpenAsync(connection, cancellationToken);

		// Deactivate existing active goal (provider-aware)
		await using var deactivateCmd = connection.CreateCommand();
		deactivateCmd.CommandText = IsPostgres
			? "UPDATE public.tblusergoal SET isactive = FALSE, updatedby = 'System', lastupdated = NOW() WHERE userid = @UserId AND isactive = TRUE AND isdeleted = FALSE"
			: "UPDATE dbo.tblUserGoal SET IsActive = 0, UpdatedBy = 'System', LastUpdated = GETDATE() WHERE UserId = @UserId AND IsActive = 1 AND IsDeleted = 0";
		var pUserId = deactivateCmd.CreateParameter();
		pUserId.ParameterName = "@UserId";
		pUserId.Value = userId;
		deactivateCmd.Parameters.Add(pUserId);
		await deactivateCmd.ExecuteNonQueryAsync(cancellationToken);

		var targetDate = DateTime.UtcNow.AddDays(timelineWeeks * 7);

		await using var insertCmd = connection.CreateCommand();
		insertCmd.CommandText = IsPostgres
			? @"INSERT INTO public.tblusergoal (userid, goaltype, timelineweeks, startdate, targetdate, detectedlevel, startingscore, isactive)
VALUES (@UserId, @GoalType, @TimelineWeeks, NOW(), @TargetDate, @DetectedLevel, @StartingScore, TRUE)"
			: @"
INSERT INTO dbo.tblUserGoal (UserId, GoalType, TimelineWeeks, StartDate, TargetDate, DetectedLevel, StartingScore, IsActive)
VALUES (@UserId, @GoalType, @TimelineWeeks, GETDATE(), @TargetDate, @DetectedLevel, @StartingScore, 1)";

		var addParam = (string name, object value) =>
		{
			var p = insertCmd.CreateParameter();
			p.ParameterName = name;
			p.Value = value;
			insertCmd.Parameters.Add(p);
		};
		addParam("@UserId",       userId);
		addParam("@GoalType",     goalType);
		addParam("@TimelineWeeks", timelineWeeks);
		addParam("@TargetDate",   targetDate);
		addParam("@DetectedLevel", detectedLevel);
		addParam("@StartingScore", startingScore);

		await insertCmd.ExecuteNonQueryAsync(cancellationToken);
	}

	public async Task<GoalProgressResponseDto> GetGoalProgressAsync(long userId, CancellationToken cancellationToken = default)
	{
		var connection = DbContext.Database.GetDbConnection();
		await EnsureConnectionOpenAsync(connection, cancellationToken);

		await using var cmd = connection.CreateCommand();
		cmd.CommandText = IsPostgres
			? @"SELECT usergoalid, goaltype, timelineweeks, startdate, targetdate, detectedlevel, startingscore
FROM public.tblusergoal
WHERE userid = @UserId AND isactive = TRUE AND isdeleted = FALSE
ORDER BY datecreated DESC LIMIT 1"
			: @"SELECT TOP 1 UserGoalId, GoalType, TimelineWeeks, StartDate, TargetDate, DetectedLevel, StartingScore
FROM dbo.tblUserGoal
WHERE UserId = @UserId AND IsActive = 1 AND IsDeleted = 0
ORDER BY DateCreated DESC";
		var pUserId = cmd.CreateParameter();
		pUserId.ParameterName = "@UserId";
		pUserId.Value = userId;
		cmd.Parameters.Add(pUserId);

		await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
		if (!await reader.ReadAsync(cancellationToken))
		{
			return new GoalProgressResponseDto { HasActiveGoal = false };
		}

		// Column names differ by provider — use ordinal-based reading
		var goalTypeOrd      = IsPostgres ? reader.GetOrdinal("goaltype")      : reader.GetOrdinal("GoalType");
		var timelineWeeksOrd = IsPostgres ? reader.GetOrdinal("timelineweeks") : reader.GetOrdinal("TimelineWeeks");
		var startDateOrd     = IsPostgres ? reader.GetOrdinal("startdate")     : reader.GetOrdinal("StartDate");
		var targetDateOrd    = IsPostgres ? reader.GetOrdinal("targetdate")    : reader.GetOrdinal("TargetDate");
		var detectedLevelOrd = IsPostgres ? reader.GetOrdinal("detectedlevel") : reader.GetOrdinal("DetectedLevel");
		var startingScoreOrd = IsPostgres ? reader.GetOrdinal("startingscore") : reader.GetOrdinal("StartingScore");

		var goalType      = reader.GetString(goalTypeOrd);
		var timelineWeeks = reader.GetInt32(timelineWeeksOrd);
		var startDate     = reader.GetDateTime(startDateOrd);
		var targetDate    = reader.GetDateTime(targetDateOrd);
		var detectedLevel = reader.GetString(detectedLevelOrd);
		var startingScore = reader.GetDecimal(startingScoreOrd);
		await reader.CloseAsync();

		// Calculate current score based on goal type
		var sessionsSinceGoal = await (
			from sm in DbContext.SessionMembers.AsNoTracking()
			join s in DbContext.Sessions.AsNoTracking() on sm.SessionId equals s.SessionId
			where sm.UserId == userId && sm.IsDeleted == false && s.IsDeleted == false
				&& s.Status == "COMPLETED"
				&& (s.EndedDate ?? s.StartedDate ?? s.DateCreated) >= startDate
			select s.SessionId
		).CountAsync(cancellationToken);

		decimal currentScore = 0m;
		if (goalType == "interview")
		{
			var mockCategories = new[] { "Mock Interview", "Interview" };
			currentScore = await (
				from va in DbContext.VoiceAnalyses.AsNoTracking()
				join s in DbContext.Sessions.AsNoTracking() on va.SessionId equals s.SessionId
				where va.UserId == userId && va.IsDeleted == false && s.IsDeleted == false
					&& mockCategories.Contains(s.SessionMode)
					&& va.RecordedAt >= startDate
				select (decimal?)va.OverallScore
			).AverageAsync(cancellationToken) ?? 0m;
		}
		else if (goalType == "vocabulary")
		{
			// Vocabulary goal: count unique words practiced correctly (proxy = count from any VocabSprint session)
			currentScore = startingScore + sessionsSinceGoal * 3; // rough proxy until vocabulary bank is queried
		}
		else
		{
			// grammar / fluency: avg fluency score since goal set
			currentScore = await (
				from va in DbContext.VoiceAnalyses.AsNoTracking()
				join s in DbContext.Sessions.AsNoTracking() on va.SessionId equals s.SessionId
				where va.UserId == userId && va.IsDeleted == false && s.IsDeleted == false
					&& s.Status == "COMPLETED"
					&& va.RecordedAt >= startDate
				select (decimal?)va.FluencyScore
			).AverageAsync(cancellationToken) ?? startingScore;
		}

		currentScore = Math.Round(currentScore, 1);

		decimal targetScore = goalType switch
		{
			"interview"  => Math.Min(startingScore + 25m, 95m),
			"grammar"    => Math.Min(startingScore + 20m, 90m),
			"vocabulary" => startingScore + (timelineWeeks * 10m),
			"fluency"    => Math.Min(startingScore + 20m, 95m),
			_            => Math.Min(startingScore + 20m, 90m)
		};

		var trend = currentScore > startingScore + 2m ? "Improving"
				  : currentScore < startingScore - 2m ? "Declining"
				  : "Stable";

		var sessionsTarget = timelineWeeks * 3;  // 3 sessions per week
		var progressPercent = sessionsTarget > 0
			? Math.Min(Math.Round(sessionsSinceGoal * 100m / sessionsTarget, 0), 100m)
			: 0m;

		var weeksElapsed = (DateTime.UtcNow - startDate).TotalDays / 7.0;
		var weeksRemaining = Math.Max(0, (int)Math.Ceiling(timelineWeeks - weeksElapsed));

		var goalLabels = new Dictionary<string, string>
		{
			["interview"]  = "Prepare for a job interview",
			["grammar"]    = "Improve my grammar",
			["vocabulary"] = "Build vocabulary",
			["fluency"]    = "Speak more fluently"
		};

		var metricLabels = new Dictionary<string, string>
		{
			["interview"]  = "Interview Readiness Score",
			["grammar"]    = "Average Fluency Score",
			["vocabulary"] = "Words practiced",
			["fluency"]    = "Average Fluency Score"
		};

		var plans = new Dictionary<string, string>
		{
			["interview"]  = "3 sessions/week — 2×Mock Interview + 1×Grammar Drill",
			["grammar"]    = "3 sessions/week — 2×Grammar Drill + 1×Repractice Round",
			["vocabulary"] = "3 sessions/week — 2×Vocabulary Sprint + 1×Fluency Drill",
			["fluency"]    = "3 sessions/week — 2×Fluency Drill + 1×Roleplay"
		};

		return new GoalProgressResponseDto
		{
			HasActiveGoal        = true,
			GoalType             = goalType,
			GoalLabel            = goalLabels.TryGetValue(goalType, out var lbl) ? lbl : goalType,
			TimelineWeeks        = timelineWeeks,
			StartDate            = startDate,
			TargetDate           = targetDate,
			DetectedLevel        = detectedLevel,
			SessionsCompleted    = sessionsSinceGoal,
			SessionsTarget       = sessionsTarget,
			PrimaryMetricLabel   = metricLabels.TryGetValue(goalType, out var ml) ? ml : "Fluency Score",
			StartingScore        = startingScore,
			CurrentScore         = currentScore,
			TargetScore          = targetScore,
			TrendLabel           = trend,
			EstimatedWeeksRemaining = weeksRemaining,
			RecommendedPlan      = plans.TryGetValue(goalType, out var plan) ? plan : "3 sessions/week",
			ProgressPercent      = progressPercent
		};
	}

	public async Task<List<UserSearchResultDto>> SearchUsersByNameAsync(string searchTerm, long excludeUserId, CancellationToken cancellationToken = default)
	{
		var connection = DbContext.Database.GetDbConnection();
		await EnsureConnectionOpenAsync(connection, cancellationToken);

		await using var command = CreateStoredProcedureCommand(connection, "dbo.uspSearchUsersByName");
		command.Parameters.Add(CreateParameter("@SearchTerm", searchTerm));
		command.Parameters.Add(CreateParameter("@ExcludeUserId", excludeUserId));

		var results = new List<UserSearchResultDto>();

		await using var reader = await DbCommandHelper.ExecuteReaderAsync(command, cancellationToken);

		while (await reader.ReadAsync(cancellationToken))
		{
			var userIdOrd  = reader.GetOrdinal(IsPostgres ? "userid"   : "UserId");
			var nameOrd    = reader.GetOrdinal(IsPostgres ? "fullname" : "FullName");
			var avatarOrd  = reader.GetOrdinal(IsPostgres ? "avatarurl" : "AvatarUrl");

			results.Add(new UserSearchResultDto
			{
				UserId    = Convert.ToInt64(reader.GetValue(userIdOrd)),
				FullName  = reader.IsDBNull(nameOrd)   ? string.Empty : reader.GetString(nameOrd),
				AvatarUrl = reader.IsDBNull(avatarOrd) ? null         : reader.GetString(avatarOrd)
			});
		}

		return results;
	}

	private static List<PronunciationIssueDto> DeserializePronunciationJson(string? json)
	{
		if (string.IsNullOrWhiteSpace(json)) return new List<PronunciationIssueDto>();
		try { return JsonSerializer.Deserialize<List<PronunciationIssueDto>>(json) ?? new List<PronunciationIssueDto>(); }
		catch { return new List<PronunciationIssueDto>(); }
	}
}
