using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GoWithFlow.Infrastructure.Data.Migrations
{
    public partial class AddSpeakingChallenge_Phase13 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // tblWeeklyChallenge — one active challenge per week
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'tblWeeklyChallenge')
BEGIN
    CREATE TABLE dbo.tblWeeklyChallenge (
        ChallengeId     BIGINT          IDENTITY(1,1)   NOT NULL,
        ScriptId        BIGINT          NOT NULL,
        WeekStartDate   DATETIME2       NOT NULL,
        WeekEndDate     DATETIME2       NOT NULL,
        IsActive        BIT             NOT NULL        DEFAULT 1,
        SortOrder       INT             NOT NULL        DEFAULT 0,
        IPAddress       NVARCHAR(64)    NOT NULL        DEFAULT '127.0.0.1',
        CreatedBy       NVARCHAR(128)   NOT NULL        DEFAULT 'Admin',
        DateCreated     DATETIME2       NOT NULL        DEFAULT GETDATE(),
        UpdatedBy       NVARCHAR(128)   NULL,
        LastUpdated     DATETIME2       NULL,
        DeletedBy       NVARCHAR(128)   NULL,
        DateDeleted     DATETIME2       NULL,
        IsDeleted       BIT             NOT NULL        DEFAULT 0,
        CONSTRAINT PK_tblWeeklyChallenge_ChallengeId PRIMARY KEY (ChallengeId),
        CONSTRAINT FK_tblWeeklyChallenge_ScriptId_tblScript_ScriptId
            FOREIGN KEY (ScriptId) REFERENCES dbo.tblScript(ScriptId)
    );

    CREATE INDEX IDX_tblWeeklyChallenge_IsActive       ON dbo.tblWeeklyChallenge (IsActive);
    CREATE INDEX IDX_tblWeeklyChallenge_WeekStartDate   ON dbo.tblWeeklyChallenge (WeekStartDate);
END
");

            // tblChallengeAttempt — tracks each user's attempts; best score drives the leaderboard
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'tblChallengeAttempt')
BEGIN
    CREATE TABLE dbo.tblChallengeAttempt (
        AttemptId       BIGINT          IDENTITY(1,1)   NOT NULL,
        ChallengeId     BIGINT          NOT NULL,
        UserId          BIGINT          NOT NULL,
        FluencyScore    DECIMAL(5,2)    NOT NULL        DEFAULT 0,
        AttemptDate     DATETIME2       NOT NULL        DEFAULT GETDATE(),
        SortOrder       INT             NOT NULL        DEFAULT 0,
        IPAddress       NVARCHAR(64)    NOT NULL        DEFAULT '127.0.0.1',
        CreatedBy       NVARCHAR(128)   NOT NULL        DEFAULT 'System',
        DateCreated     DATETIME2       NOT NULL        DEFAULT GETDATE(),
        UpdatedBy       NVARCHAR(128)   NULL,
        LastUpdated     DATETIME2       NULL,
        DeletedBy       NVARCHAR(128)   NULL,
        DateDeleted     DATETIME2       NULL,
        IsDeleted       BIT             NOT NULL        DEFAULT 0,
        CONSTRAINT PK_tblChallengeAttempt_AttemptId PRIMARY KEY (AttemptId),
        CONSTRAINT FK_tblChallengeAttempt_ChallengeId_tblWeeklyChallenge_ChallengeId
            FOREIGN KEY (ChallengeId) REFERENCES dbo.tblWeeklyChallenge(ChallengeId),
        CONSTRAINT FK_tblChallengeAttempt_UserId_tblUser_UserId
            FOREIGN KEY (UserId) REFERENCES dbo.tblUser(UserId)
    );

    CREATE INDEX IDX_tblChallengeAttempt_ChallengeId     ON dbo.tblChallengeAttempt (ChallengeId);
    CREATE INDEX IDX_tblChallengeAttempt_UserId           ON dbo.tblChallengeAttempt (UserId);
    CREATE INDEX IDX_tblChallengeAttempt_ChallengeId_User ON dbo.tblChallengeAttempt (ChallengeId, UserId);
END
");

            // uspGetActiveChallenge — returns the current active weekly challenge + user's best score + leaderboard
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.objects WHERE type = 'P' AND name = 'uspGetActiveChallenge')
    DROP PROCEDURE dbo.uspGetActiveChallenge;
");
            migrationBuilder.Sql(@"
CREATE PROCEDURE dbo.uspGetActiveChallenge
    @UserId BIGINT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @Now DATETIME2 = GETDATE();

    -- Challenge header + user's best score
    SELECT
        wc.ChallengeId,
        wc.ScriptId,
        s.ScriptTitle,
        s.Category,
        s.ComplexityLevel,
        wc.WeekStartDate,
        wc.WeekEndDate,
        DATEDIFF(DAY, @Now, wc.WeekEndDate)     AS DaysRemaining,
        ISNULL(best.BestScore, 0)                AS UserBestScore,
        ISNULL(att.AttemptCount, 0)              AS UserAttemptCount
    FROM   dbo.tblWeeklyChallenge wc
    INNER  JOIN dbo.tblScript s ON s.ScriptId = wc.ScriptId
    LEFT   JOIN (
        SELECT  ChallengeId, MAX(FluencyScore) AS BestScore
        FROM   dbo.tblChallengeAttempt
        WHERE  UserId = @UserId AND IsDeleted = 0
        GROUP  BY ChallengeId
    ) best ON best.ChallengeId = wc.ChallengeId
    LEFT   JOIN (
        SELECT  ChallengeId, COUNT(*) AS AttemptCount
        FROM   dbo.tblChallengeAttempt
        WHERE  UserId = @UserId AND IsDeleted = 0
        GROUP  BY ChallengeId
    ) att ON att.ChallengeId = wc.ChallengeId
    WHERE  wc.IsActive    = 1
      AND  wc.IsDeleted   = 0
      AND  wc.WeekStartDate <= @Now
      AND  wc.WeekEndDate   >= @Now;

    -- Leaderboard: top 10 scores for this week's challenge
    SELECT TOP 10
        u.FullName,
        MAX(ca.FluencyScore) AS BestScore,
        ROW_NUMBER() OVER (ORDER BY MAX(ca.FluencyScore) DESC) AS Rank
    FROM   dbo.tblChallengeAttempt ca
    INNER  JOIN dbo.tblUser u ON u.UserId = ca.UserId
    INNER  JOIN dbo.tblWeeklyChallenge wc ON wc.ChallengeId = ca.ChallengeId
    WHERE  wc.IsActive  = 1
      AND  wc.IsDeleted = 0
      AND  wc.WeekStartDate <= @Now
      AND  wc.WeekEndDate   >= @Now
      AND  ca.IsDeleted = 0
    GROUP  BY u.UserId, u.FullName
    ORDER  BY MAX(ca.FluencyScore) DESC;
END
");

            // uspInsertChallengeAttempt — records an attempt (or updates best score)
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.objects WHERE type = 'P' AND name = 'uspInsertChallengeAttempt')
    DROP PROCEDURE dbo.uspInsertChallengeAttempt;
");
            migrationBuilder.Sql(@"
CREATE PROCEDURE dbo.uspInsertChallengeAttempt
    @ChallengeId BIGINT,
    @UserId      BIGINT,
    @Score       DECIMAL(5,2),
    @CreatedBy   NVARCHAR(128),
    @IPAddress   NVARCHAR(64)
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.tblChallengeAttempt (ChallengeId, UserId, FluencyScore, CreatedBy, IPAddress)
    VALUES (@ChallengeId, @UserId, @Score, @CreatedBy, @IPAddress);

    SELECT SCOPE_IDENTITY() AS AttemptId;
END
");

            // uspSetWeeklyChallenge — admin sets a script as this week's challenge
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.objects WHERE type = 'P' AND name = 'uspSetWeeklyChallenge')
    DROP PROCEDURE dbo.uspSetWeeklyChallenge;
");
            migrationBuilder.Sql(@"
CREATE PROCEDURE dbo.uspSetWeeklyChallenge
    @ScriptId  BIGINT,
    @CreatedBy NVARCHAR(128),
    @IPAddress NVARCHAR(64)
AS
BEGIN
    SET NOCOUNT ON;

    -- Deactivate any existing active challenge
    UPDATE dbo.tblWeeklyChallenge
    SET    IsActive    = 0,
           UpdatedBy   = @CreatedBy,
           LastUpdated = GETDATE()
    WHERE  IsActive = 1 AND IsDeleted = 0;

    -- Create new challenge: Mon–Sun of current week
    DECLARE @Monday DATETIME2 = DATEADD(DAY, 2 - DATEPART(WEEKDAY, GETDATE()), CAST(GETDATE() AS DATE));
    DECLARE @Sunday DATETIME2 = DATEADD(DAY, 6, @Monday);
    SET @Sunday = DATEADD(SECOND, 86399, @Sunday);

    INSERT INTO dbo.tblWeeklyChallenge (ScriptId, WeekStartDate, WeekEndDate, CreatedBy, IPAddress)
    VALUES (@ScriptId, @Monday, @Sunday, @CreatedBy, @IPAddress);

    SELECT SCOPE_IDENTITY() AS ChallengeId;
END
");

            // Award challenge badge: top 20% — stored proc for post-challenge badge award
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.objects WHERE type = 'P' AND name = 'uspAwardChallengeBadge')
    DROP PROCEDURE dbo.uspAwardChallengeBadge;
");
            migrationBuilder.Sql(@"
CREATE PROCEDURE dbo.uspAwardChallengeBadge
    @ChallengeId BIGINT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @TotalParticipants INT;
    DECLARE @Top20Threshold INT;

    SELECT @TotalParticipants = COUNT(DISTINCT UserId)
    FROM   dbo.tblChallengeAttempt
    WHERE  ChallengeId = @ChallengeId AND IsDeleted = 0;

    SET @Top20Threshold = CEILING(@TotalParticipants * 0.2);

    -- Find top 20% by best score
    WITH Ranked AS (
        SELECT
            UserId,
            MAX(FluencyScore) AS BestScore,
            DENSE_RANK() OVER (ORDER BY MAX(FluencyScore) DESC) AS Rnk
        FROM   dbo.tblChallengeAttempt
        WHERE  ChallengeId = @ChallengeId AND IsDeleted = 0
        GROUP  BY UserId
    )
    INSERT INTO dbo.tblUserBadge (UserId, BadgeCode, BadgeName)
    SELECT
        r.UserId,
        'WEEKLY_CHAMPION',
        'Weekly Champion'
    FROM   Ranked r
    WHERE  r.Rnk <= @Top20Threshold
      AND  NOT EXISTS (
            SELECT 1 FROM dbo.tblUserBadge ub
            WHERE ub.UserId = r.UserId AND ub.BadgeCode = 'WEEKLY_CHAMPION'
              AND ub.EarnedDate >= DATEADD(DAY, -7, GETDATE())
            );
END
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.objects WHERE type = 'P' AND name = 'uspAwardChallengeBadge')    DROP PROCEDURE dbo.uspAwardChallengeBadge;
IF EXISTS (SELECT 1 FROM sys.objects WHERE type = 'P' AND name = 'uspSetWeeklyChallenge')     DROP PROCEDURE dbo.uspSetWeeklyChallenge;
IF EXISTS (SELECT 1 FROM sys.objects WHERE type = 'P' AND name = 'uspInsertChallengeAttempt') DROP PROCEDURE dbo.uspInsertChallengeAttempt;
IF EXISTS (SELECT 1 FROM sys.objects WHERE type = 'P' AND name = 'uspGetActiveChallenge')     DROP PROCEDURE dbo.uspGetActiveChallenge;
");

            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'tblChallengeAttempt')
    DROP TABLE dbo.tblChallengeAttempt;
IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'tblWeeklyChallenge')
    DROP TABLE dbo.tblWeeklyChallenge;
");
        }
    }
}
