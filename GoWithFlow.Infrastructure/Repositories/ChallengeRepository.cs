using System.Data;
using System.Data.Common;
using GoWithFlow.Application.DTOs.Requests.User;
using GoWithFlow.Application.DTOs.Responses.User;
using GoWithFlow.Application.Interfaces.Repositories;
using GoWithFlow.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GoWithFlow.Infrastructure.Repositories;

public sealed class ChallengeRepository : IChallengeRepository
{
	private readonly GoWithFlowDbContext _dbContext;

	public ChallengeRepository(GoWithFlowDbContext dbContext)
	{
		_dbContext = dbContext;
	}

	public async Task<ActiveChallengeResponseDto> GetActiveChallengeAsync(long userId, CancellationToken cancellationToken = default)
	{
		var now = DateTime.UtcNow;

		// For SQL Server: use SP with two result sets
		// For PostgreSQL: use EF queries (provider-safe)
		if (DatabaseProviderNames.IsPostgreSql(_dbContext.DatabaseProvider))
		{
			return await GetActiveChallengeViaEfAsync(userId, now, cancellationToken);
		}

		await using var command = await CreateStoredProcedureCommandAsync("dbo.uspGetActiveChallenge", cancellationToken);
		command.Parameters.Add(CreateParameter("@UserId", userId));

		await using var reader = await DbCommandHelper.ExecuteReaderAsync(command, cancellationToken);

		var response = new ActiveChallengeResponseDto();

		if (await reader.ReadAsync(cancellationToken))
		{
			response.HasActiveChallenge = true;
			response.ChallengeId       = GetInt64(reader, "ChallengeId");
			response.ScriptId          = GetInt64(reader, "ScriptId");
			response.ScriptTitle       = GetString(reader, "ScriptTitle");
			response.Category          = GetString(reader, "Category");
			response.ComplexityLevel   = GetInt32(reader, "ComplexityLevel");
			response.WeekStartDate     = GetDateTime(reader, "WeekStartDate");
			response.WeekEndDate       = GetDateTime(reader, "WeekEndDate");
			response.DaysRemaining     = GetInt32(reader, "DaysRemaining");
			response.UserBestScore     = GetDecimal(reader, "UserBestScore");
			response.UserAttemptCount  = GetInt32(reader, "UserAttemptCount");
		}

		await reader.CloseAsync();

		if (response.HasActiveChallenge)
		{
			await reader.NextResultAsync(cancellationToken);

			while (await reader.ReadAsync(cancellationToken))
			{
				response.Leaderboard.Add(new ChallengeLeaderboardEntryDto
				{
					Rank      = GetInt32(reader, "Rank"),
					FullName  = GetString(reader, "FullName"),
					BestScore = GetDecimal(reader, "BestScore")
				});
			}
		}

		return response;
	}

	private async Task<ActiveChallengeResponseDto> GetActiveChallengeViaEfAsync(long userId, DateTime now, CancellationToken cancellationToken)
	{
		var response = new ActiveChallengeResponseDto();

		var challenge = await _dbContext.WeeklyChallenges
			.AsNoTracking()
			.Include(c => c.Script)
			.Where(c => c.IsActive && !c.IsDeleted && c.WeekStartDate <= now && c.WeekEndDate >= now)
			.FirstOrDefaultAsync(cancellationToken);

		if (challenge is null) return response;

		response.HasActiveChallenge = true;
		response.ChallengeId       = challenge.ChallengeId;
		response.ScriptId          = challenge.ScriptId;
		response.ScriptTitle       = challenge.Script?.ScriptTitle ?? string.Empty;
		response.Category          = challenge.Script?.Category ?? string.Empty;
		response.ComplexityLevel   = challenge.Script?.ComplexityLevel ?? 1;
		response.WeekStartDate     = challenge.WeekStartDate;
		response.WeekEndDate       = challenge.WeekEndDate;
		response.DaysRemaining     = Math.Max(0, (int)(challenge.WeekEndDate - now).TotalDays);

		var attempts = await _dbContext.ChallengeAttempts
			.AsNoTracking()
			.Where(a => a.ChallengeId == challenge.ChallengeId && !a.IsDeleted)
			.ToListAsync(cancellationToken);

		response.UserBestScore    = attempts.Where(a => a.UserId == userId).Select(a => a.FluencyScore).DefaultIfEmpty(0m).Max();
		response.UserAttemptCount = attempts.Count(a => a.UserId == userId);

		// Fetch user names for leaderboard
		var leaderboardUserIds = attempts
			.GroupBy(a => a.UserId)
			.OrderByDescending(g => g.Max(a => a.FluencyScore))
			.Take(10)
			.Select(g => g.Key)
			.ToList();

		var userNames = await _dbContext.Users.AsNoTracking()
			.Where(u => leaderboardUserIds.Contains(u.UserId))
			.Select(u => new { u.UserId, u.FullName })
			.ToDictionaryAsync(u => u.UserId, u => u.FullName, cancellationToken);

		response.Leaderboard = attempts
			.GroupBy(a => a.UserId)
			.Select(g => new { UserId = g.Key, BestScore = g.Max(a => a.FluencyScore) })
			.OrderByDescending(x => x.BestScore)
			.Take(10)
			.Select((x, i) => new ChallengeLeaderboardEntryDto
			{
				Rank      = i + 1,
				FullName  = userNames.GetValueOrDefault(x.UserId, "User"),
				BestScore = x.BestScore
			})
			.ToList();

		return response;
	}

	public async Task<long> InsertChallengeAttemptAsync(long challengeId, long userId, decimal score, string createdBy, string ipAddress, CancellationToken cancellationToken = default)
	{
		if (DatabaseProviderNames.IsPostgreSql(_dbContext.DatabaseProvider))
		{
			var attempt = new ChallengeAttempt
			{
				ChallengeId  = challengeId,
				UserId       = userId,
				FluencyScore = score,
				AttemptDate  = DateTime.UtcNow,
				CreatedBy    = createdBy,
				IPAddress    = ipAddress
			};
			_dbContext.ChallengeAttempts.Add(attempt);
			await _dbContext.SaveChangesAsync(cancellationToken);
			return attempt.AttemptId;
		}

		await using var command = await CreateStoredProcedureCommandAsync("dbo.uspInsertChallengeAttempt", cancellationToken);
		command.Parameters.Add(CreateParameter("@ChallengeId", challengeId));
		command.Parameters.Add(CreateParameter("@UserId", userId));
		command.Parameters.Add(CreateParameter("@Score", score));
		command.Parameters.Add(CreateParameter("@CreatedBy", createdBy));
		command.Parameters.Add(CreateParameter("@IPAddress", ipAddress));

		var result = await DbCommandHelper.ExecuteScalarAsync(command, cancellationToken);
		return Convert.ToInt64(result);
	}

	public async Task<long> SetWeeklyChallengeAsync(long scriptId, string createdBy, string ipAddress, CancellationToken cancellationToken = default)
	{
		if (DatabaseProviderNames.IsPostgreSql(_dbContext.DatabaseProvider))
		{
			// Deactivate existing
			var existing = await _dbContext.WeeklyChallenges
				.Where(c => c.IsActive && !c.IsDeleted)
				.ToListAsync(cancellationToken);
			foreach (var c in existing) { c.IsActive = false; c.UpdatedBy = createdBy; }

			var monday = DateTime.UtcNow.AddDays(-(int)DateTime.UtcNow.DayOfWeek + 1).Date;
			var sunday = monday.AddDays(6).AddSeconds(86399);

			var newChallenge = new WeeklyChallenge
			{
				ScriptId      = scriptId,
				WeekStartDate = monday,
				WeekEndDate   = sunday,
				IsActive      = true,
				CreatedBy     = createdBy,
				IPAddress     = ipAddress
			};
			_dbContext.WeeklyChallenges.Add(newChallenge);
			await _dbContext.SaveChangesAsync(cancellationToken);
			return newChallenge.ChallengeId;
		}

		await using var command = await CreateStoredProcedureCommandAsync("dbo.uspSetWeeklyChallenge", cancellationToken);
		command.Parameters.Add(CreateParameter("@ScriptId", scriptId));
		command.Parameters.Add(CreateParameter("@CreatedBy", createdBy));
		command.Parameters.Add(CreateParameter("@IPAddress", ipAddress));

		var result = await DbCommandHelper.ExecuteScalarAsync(command, cancellationToken);
		return Convert.ToInt64(result);
	}

	private async Task<DbCommand> CreateStoredProcedureCommandAsync(string storedProcedureName, CancellationToken cancellationToken)
	{
		var connection = _dbContext.Database.GetDbConnection();
		if (connection.State != ConnectionState.Open)
			await connection.OpenAsync(cancellationToken);

		var command = connection.CreateCommand();
		command.CommandText = DbCommandHelper.QualifyRoutineName(_dbContext.DatabaseProvider, storedProcedureName);
		command.CommandType = CommandType.StoredProcedure;
		return command;
	}

	private DbParameter CreateParameter(string parameterName, object? value)
		=> DbCommandHelper.CreateParameter(_dbContext.DatabaseProvider, parameterName, value);

	private static string GetString(DbDataReader reader, string columnName)
	{
		var ordinal = reader.GetOrdinal(columnName);
		return reader.IsDBNull(ordinal) ? string.Empty : reader.GetString(ordinal);
	}

	private static long GetInt64(DbDataReader reader, string columnName)
		=> reader.GetInt64(reader.GetOrdinal(columnName));

	private static int GetInt32(DbDataReader reader, string columnName)
		=> reader.GetInt32(reader.GetOrdinal(columnName));

	private static decimal GetDecimal(DbDataReader reader, string columnName)
		=> reader.GetDecimal(reader.GetOrdinal(columnName));

	private static DateTime GetDateTime(DbDataReader reader, string columnName)
		=> reader.GetDateTime(reader.GetOrdinal(columnName));
}
