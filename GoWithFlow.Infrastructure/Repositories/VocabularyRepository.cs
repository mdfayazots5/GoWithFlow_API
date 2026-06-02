using System.Data;
using System.Data.Common;
using GoWithFlow.Application.DTOs.Responses.Vocabulary;
using GoWithFlow.Application.Interfaces.Repositories;
using GoWithFlow.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GoWithFlow.Infrastructure.Repositories;

public sealed class VocabularyRepository : IVocabularyRepository
{
	private readonly GoWithFlowDbContext _dbContext;
	private bool IsPostgres => DatabaseProviderNames.IsPostgreSql(_dbContext.DatabaseProvider);

	public VocabularyRepository(GoWithFlowDbContext dbContext)
	{
		_dbContext = dbContext;
	}

	public async Task UpsertVocabularyWordAsync(long userId, string focusWord, long sourceSessionId, bool wasProducedCorrectly, CancellationToken cancellationToken = default)
	{
		var connection = _dbContext.Database.GetDbConnection();
		await EnsureConnectionOpenAsync(connection, cancellationToken);

		string sql;
		if (IsPostgres)
		{
			// PostgreSQL: lowercase table/column names, boolean type, NOW()
			sql = @"
INSERT INTO public.tbluservocabulary
    (userid, focusword, sourcesessionid, dateintroduced, wasproducedcorrectly, timesencountered, createdby, ipaddress)
VALUES
    (@UserId, @FocusWord, @SourceSessionId, NOW(), @WasProducedCorrectly, 1, 'System', '127.0.0.1')
ON CONFLICT (userid, focusword, sourcesessionid) DO UPDATE SET
    timesencountered = tbluservocabulary.timesencountered + 1,
    wasproducedcorrectly = CASE WHEN @WasProducedCorrectly = TRUE THEN TRUE ELSE tbluservocabulary.wasproducedcorrectly END,
    lastupdated = NOW(),
    updatedby = 'System';";
		}
		else
		{
			// SQL Server: MERGE syntax, dbo prefix, GETDATE()
			sql = @"
MERGE dbo.tblUserVocabulary AS target
USING (SELECT @UserId AS UserId, @FocusWord AS FocusWord, @SourceSessionId AS SourceSessionId) AS source
ON target.UserId = source.UserId AND target.FocusWord = source.FocusWord AND target.SourceSessionId = source.SourceSessionId
WHEN MATCHED THEN
    UPDATE SET
        TimesEncountered = target.TimesEncountered + 1,
        WasProducedCorrectly = CASE WHEN @WasProducedCorrectly = 1 THEN 1 ELSE target.WasProducedCorrectly END,
        LastUpdated = GETDATE(), UpdatedBy = 'System'
WHEN NOT MATCHED THEN
    INSERT (UserId, FocusWord, SourceSessionId, DateIntroduced, WasProducedCorrectly, TimesEncountered, CreatedBy, IPAddress)
    VALUES (@UserId, @FocusWord, @SourceSessionId, GETDATE(), @WasProducedCorrectly, 1, 'System', '127.0.0.1');";
		}

		await using var command = connection.CreateCommand();
		command.CommandText = sql;
		command.CommandType = CommandType.Text;
		command.Parameters.Add(CreateParameter(command, "@UserId", userId));
		command.Parameters.Add(CreateParameter(command, "@FocusWord", focusWord));
		command.Parameters.Add(CreateParameter(command, "@SourceSessionId", sourceSessionId));
		command.Parameters.Add(CreateParameter(command, "@WasProducedCorrectly", wasProducedCorrectly));

		await command.ExecuteNonQueryAsync(cancellationToken);
	}

	public async Task<VocabularyBankResponseDto> GetVocabularyBankAsync(long userId, CancellationToken cancellationToken = default)
	{
		var connection = _dbContext.Database.GetDbConnection();
		await EnsureConnectionOpenAsync(connection, cancellationToken);

		string sql;
		if (IsPostgres)
		{
			sql = @"
SELECT
    focusword AS FocusWord,
    MIN(dateintroduced) AS DateIntroduced,
    SUM(timesencountered) AS TimesEncountered,
    SUM(CASE WHEN wasproducedcorrectly = TRUE THEN 1 ELSE 0 END) AS TimesCorrect,
    MAX(dateintroduced) AS LastSeen
FROM public.tbluservocabulary
WHERE userid = @UserId AND isdeleted = FALSE
GROUP BY focusword
ORDER BY MIN(dateintroduced) DESC;";
		}
		else
		{
			sql = @"
SELECT
    FocusWord,
    MIN(DateIntroduced) AS DateIntroduced,
    SUM(TimesEncountered) AS TimesEncountered,
    SUM(CASE WHEN WasProducedCorrectly = 1 THEN 1 ELSE 0 END) AS TimesCorrect,
    MAX(DateIntroduced) AS LastSeen
FROM dbo.tblUserVocabulary
WHERE UserId = @UserId AND IsDeleted = 0
GROUP BY FocusWord
ORDER BY MIN(DateIntroduced) DESC;";
		}

		await using var command = connection.CreateCommand();
		command.CommandText = sql;
		command.CommandType = CommandType.Text;
		command.Parameters.Add(CreateParameter(command, "@UserId", userId));

		await using var reader = await command.ExecuteReaderAsync(cancellationToken);
		var words = new List<VocabularyBankItemDto>();
		var reviewCutoff = DateTime.UtcNow.AddDays(-7);

		while (await reader.ReadAsync(cancellationToken))
		{
			var timesEncountered = reader.GetInt32(reader.GetOrdinal("TimesEncountered"));
			var timesCorrect     = reader.GetInt32(reader.GetOrdinal("TimesCorrect"));
			var lastSeen         = reader.GetDateTime(reader.GetOrdinal("LastSeen"));
			var correctRate      = timesEncountered > 0
				? Math.Round((decimal)timesCorrect * 100m / timesEncountered, 1)
				: 0m;

			words.Add(new VocabularyBankItemDto
			{
				FocusWord        = reader.GetString(reader.GetOrdinal("FocusWord")),
				DateIntroduced   = reader.GetDateTime(reader.GetOrdinal("DateIntroduced")),
				TimesEncountered = timesEncountered,
				TimesCorrect     = timesCorrect,
				CorrectRate      = correctRate,
				IsDueForReview   = lastSeen < reviewCutoff
			});
		}

		return new VocabularyBankResponseDto
		{
			TotalWords       = words.Count,
			DueForReviewCount = words.Count(w => w.IsDueForReview),
			Words            = words
		};
	}

	public async Task<(int TotalWords, int DueForReview)> GetVocabularyBankTotalsAsync(long userId, CancellationToken cancellationToken = default)
	{
		var connection = _dbContext.Database.GetDbConnection();
		await EnsureConnectionOpenAsync(connection, cancellationToken);

		string sql;
		if (IsPostgres)
		{
			sql = @"
SELECT
    (SELECT COUNT(DISTINCT focusword) FROM public.tbluservocabulary
     WHERE userid = @UserId AND isdeleted = FALSE) AS TotalWords,
    (SELECT COUNT(DISTINCT focusword) FROM public.tbluservocabulary
     WHERE userid = @UserId AND isdeleted = FALSE
       AND focusword NOT IN (
           SELECT DISTINCT focusword FROM public.tbluservocabulary
           WHERE userid = @UserId AND isdeleted = FALSE
             AND dateintroduced >= NOW() - INTERVAL '7 days'
       )) AS DueForReview;";
		}
		else
		{
			sql = @"
SELECT
    (SELECT COUNT(DISTINCT FocusWord) FROM dbo.tblUserVocabulary WHERE UserId = @UserId AND IsDeleted = 0) AS TotalWords,
    (SELECT COUNT(DISTINCT FocusWord) FROM dbo.tblUserVocabulary
     WHERE UserId = @UserId AND IsDeleted = 0
       AND FocusWord NOT IN (
           SELECT FocusWord FROM dbo.tblUserVocabulary
           WHERE UserId = @UserId AND IsDeleted = 0 AND DateIntroduced >= DATEADD(DAY, -7, GETUTCDATE())
       )) AS DueForReview;";
		}

		await using var command = connection.CreateCommand();
		command.CommandText = sql;
		command.CommandType = CommandType.Text;
		command.Parameters.Add(CreateParameter(command, "@UserId", userId));

		await using var reader = await command.ExecuteReaderAsync(cancellationToken);
		if (await reader.ReadAsync(cancellationToken))
		{
			return (reader.GetInt32(reader.GetOrdinal("TotalWords")), reader.GetInt32(reader.GetOrdinal("DueForReview")));
		}

		return (0, 0);
	}

	private static IDbDataParameter CreateParameter(IDbCommand command, string name, object? value)
	{
		var param = command.CreateParameter();
		param.ParameterName = name;
		param.Value = value ?? DBNull.Value;
		return param;
	}

	private static async Task EnsureConnectionOpenAsync(IDbConnection connection, CancellationToken cancellationToken)
	{
		if (connection.State != ConnectionState.Open)
		{
			await ((DbConnection)connection).OpenAsync(cancellationToken);
		}
	}
}
