using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GoWithFlow.Infrastructure.Data.Migrations
{
    public partial class AddCohortManagement_Phase12 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create tblCohort
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'tblCohort')
BEGIN
    CREATE TABLE dbo.tblCohort (
        CohortId    BIGINT          IDENTITY(1,1)   NOT NULL,
        CohortName  NVARCHAR(128)   NOT NULL,
        Description NVARCHAR(256)   NULL,
        IsActive    BIT             NOT NULL        DEFAULT 1,
        SortOrder   INT             NOT NULL        DEFAULT 0,
        Tag         NVARCHAR(64)    NULL,
        Comments    NVARCHAR(256)   NULL,
        IPAddress   NVARCHAR(64)    NOT NULL        DEFAULT '127.0.0.1',
        CreatedBy   NVARCHAR(128)   NOT NULL        DEFAULT 'Admin',
        DateCreated DATETIME2       NOT NULL        DEFAULT GETDATE(),
        UpdatedBy   NVARCHAR(128)   NULL,
        LastUpdated DATETIME2       NULL,
        DeletedBy   NVARCHAR(128)   NULL,
        DateDeleted DATETIME2       NULL,
        IsDeleted   BIT             NOT NULL        DEFAULT 0,
        CONSTRAINT PK_tblCohort_CohortId PRIMARY KEY (CohortId)
    );

    CREATE INDEX IDX_tblCohort_IsActive ON dbo.tblCohort (IsActive);
END
");

            // Add CohortId FK to tblUser
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'tblUser' AND COLUMN_NAME = 'CohortId')
BEGIN
    ALTER TABLE dbo.tblUser ADD CohortId BIGINT NULL;

    ALTER TABLE dbo.tblUser
        ADD CONSTRAINT FK_tblUser_CohortId_tblCohort_CohortId
            FOREIGN KEY (CohortId) REFERENCES dbo.tblCohort(CohortId) ON DELETE SET NULL;

    CREATE INDEX IDX_tblUser_CohortId ON dbo.tblUser (CohortId);
END
");

            // uspInsertCohort
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.objects WHERE type = 'P' AND name = 'uspInsertCohort')
    DROP PROCEDURE dbo.uspInsertCohort;
");
            migrationBuilder.Sql(@"
CREATE PROCEDURE dbo.uspInsertCohort
    @CohortName  NVARCHAR(128),
    @Description NVARCHAR(256),
    @CreatedBy   NVARCHAR(128),
    @IPAddress   NVARCHAR(64)
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.tblCohort (CohortName, Description, CreatedBy, IPAddress)
    VALUES (@CohortName, @Description, @CreatedBy, @IPAddress);

    SELECT SCOPE_IDENTITY() AS CohortId;
END
");

            // uspGetAllCohorts
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.objects WHERE type = 'P' AND name = 'uspGetAllCohorts')
    DROP PROCEDURE dbo.uspGetAllCohorts;
");
            migrationBuilder.Sql(@"
CREATE PROCEDURE dbo.uspGetAllCohorts
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        c.CohortId,
        c.CohortName,
        c.Description,
        c.IsActive,
        c.DateCreated,
        COUNT(u.UserId) AS MemberCount
    FROM   dbo.tblCohort c
    LEFT   JOIN dbo.tblUser u ON u.CohortId = c.CohortId AND u.IsDeleted = 0
    WHERE  c.IsDeleted = 0
    GROUP  BY c.CohortId, c.CohortName, c.Description, c.IsActive, c.DateCreated
    ORDER  BY c.DateCreated DESC;
END
");

            // uspAssignUserToCohort
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.objects WHERE type = 'P' AND name = 'uspAssignUserToCohort')
    DROP PROCEDURE dbo.uspAssignUserToCohort;
");
            migrationBuilder.Sql(@"
CREATE PROCEDURE dbo.uspAssignUserToCohort
    @UserId    BIGINT,
    @CohortId  BIGINT,
    @UpdatedBy NVARCHAR(128),
    @IPAddress NVARCHAR(64)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.tblUser
    SET    CohortId    = @CohortId,
           UpdatedBy   = @UpdatedBy,
           LastUpdated = GETDATE()
    WHERE  UserId    = @UserId
      AND  IsDeleted = 0;
END
");

            // uspGetCohortMembers
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.objects WHERE type = 'P' AND name = 'uspGetCohortMembers')
    DROP PROCEDURE dbo.uspGetCohortMembers;
");
            migrationBuilder.Sql(@"
CREATE PROCEDURE dbo.uspGetCohortMembers
    @CohortId BIGINT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        u.UserId,
        u.FullName,
        u.MobileNumber,
        u.AgeGroup,
        u.IsActive,
        u.DailyStreakCount,
        u.TotalSessionsPlayed,
        u.LastLoginDate,
        ISNULL(s.SessionCount, 0)  AS SessionCount,
        ISNULL(s.AvgFluency,   0)  AS AvgFluencyScore,
        ISNULL(m.MistakeCount, 0)  AS TotalMistakes
    FROM   dbo.tblUser u
    LEFT   JOIN (
        SELECT  sm.UserId,
                COUNT(DISTINCT sm.SessionId) AS SessionCount,
                ISNULL(AVG(va.OverallScore), 0) AS AvgFluency
        FROM   dbo.tblSessionMember sm
        LEFT   JOIN dbo.tblVoiceAnalysis va ON va.SessionId = sm.SessionId AND va.UserId = sm.UserId AND va.IsDeleted = 0
        WHERE  sm.IsDeleted = 0
        GROUP  BY sm.UserId
    ) s ON s.UserId = u.UserId
    LEFT   JOIN (
        SELECT  UserId, COUNT(*) AS MistakeCount
        FROM   dbo.tblMistake
        WHERE  IsDeleted = 0
        GROUP  BY UserId
    ) m ON m.UserId = u.UserId
    WHERE  u.CohortId  = @CohortId
      AND  u.IsDeleted = 0
    ORDER  BY u.FullName ASC;
END
");

            // uspGetCohortAnalytics
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.objects WHERE type = 'P' AND name = 'uspGetCohortAnalytics')
    DROP PROCEDURE dbo.uspGetCohortAnalytics;
");
            migrationBuilder.Sql(@"
CREATE PROCEDURE dbo.uspGetCohortAnalytics
    @CohortId BIGINT
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @CutoffDate DATETIME2 = DATEADD(DAY, -30, GETDATE());
    DECLARE @InactiveCutoff DATETIME2 = DATEADD(DAY, -7, GETDATE());

    -- Cohort header
    SELECT
        c.CohortId,
        c.CohortName,
        c.Description,
        COUNT(u.UserId)                         AS MemberCount,
        ISNULL(AVG(va.OverallScore), 0)         AS AvgFluencyScore,
        SUM(CASE WHEN u.LastLoginDate < @InactiveCutoff OR u.LastLoginDate IS NULL THEN 1 ELSE 0 END) AS InactiveCount
    FROM   dbo.tblCohort c
    LEFT   JOIN dbo.tblUser u ON u.CohortId = c.CohortId AND u.IsDeleted = 0
    LEFT   JOIN dbo.tblSessionMember sm ON sm.UserId = u.UserId AND sm.IsDeleted = 0
    LEFT   JOIN dbo.tblVoiceAnalysis va ON va.SessionId = sm.SessionId AND va.UserId = sm.UserId AND va.IsDeleted = 0
                                       AND va.DateCreated >= @CutoffDate
    WHERE  c.CohortId = @CohortId AND c.IsDeleted = 0
    GROUP  BY c.CohortId, c.CohortName, c.Description;

    -- Top grammar mistakes across cohort
    SELECT TOP 5
        m.GrammarTag,
        COUNT(*) AS MistakeCount
    FROM   dbo.tblMistake m
    INNER  JOIN dbo.tblUser u ON u.UserId = m.UserId AND u.CohortId = @CohortId AND u.IsDeleted = 0
    WHERE  m.GrammarTag IS NOT NULL
      AND  m.IsDeleted = 0
      AND  m.DateCreated >= @CutoffDate
    GROUP  BY m.GrammarTag
    ORDER  BY COUNT(*) DESC;

    -- Most improved member (largest AvgFluencyScore delta: last 15 days vs prior 15 days)
    SELECT TOP 1
        u.UserId,
        u.FullName,
        ISNULL(curr.AvgScore, 0) - ISNULL(prev.AvgScore, 0) AS ImprovementDelta
    FROM   dbo.tblUser u
    LEFT   JOIN (
        SELECT  va.UserId, AVG(va.OverallScore) AS AvgScore
        FROM   dbo.tblVoiceAnalysis va
        WHERE  va.DateCreated >= DATEADD(DAY, -15, GETDATE()) AND va.IsDeleted = 0
        GROUP  BY va.UserId
    ) curr ON curr.UserId = u.UserId
    LEFT   JOIN (
        SELECT  va.UserId, AVG(va.OverallScore) AS AvgScore
        FROM   dbo.tblVoiceAnalysis va
        WHERE  va.DateCreated BETWEEN DATEADD(DAY, -30, GETDATE()) AND DATEADD(DAY, -15, GETDATE())
          AND  va.IsDeleted = 0
        GROUP  BY va.UserId
    ) prev ON prev.UserId = u.UserId
    WHERE  u.CohortId  = @CohortId AND u.IsDeleted = 0
    ORDER  BY (ISNULL(curr.AvgScore, 0) - ISNULL(prev.AvgScore, 0)) DESC;
END
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.objects WHERE type = 'P' AND name = 'uspGetCohortAnalytics')   DROP PROCEDURE dbo.uspGetCohortAnalytics;
IF EXISTS (SELECT 1 FROM sys.objects WHERE type = 'P' AND name = 'uspGetCohortMembers')     DROP PROCEDURE dbo.uspGetCohortMembers;
IF EXISTS (SELECT 1 FROM sys.objects WHERE type = 'P' AND name = 'uspAssignUserToCohort')   DROP PROCEDURE dbo.uspAssignUserToCohort;
IF EXISTS (SELECT 1 FROM sys.objects WHERE type = 'P' AND name = 'uspGetAllCohorts')        DROP PROCEDURE dbo.uspGetAllCohorts;
IF EXISTS (SELECT 1 FROM sys.objects WHERE type = 'P' AND name = 'uspInsertCohort')         DROP PROCEDURE dbo.uspInsertCohort;
");

            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'tblUser' AND COLUMN_NAME = 'CohortId')
BEGIN
    ALTER TABLE dbo.tblUser DROP CONSTRAINT IF EXISTS FK_tblUser_CohortId_tblCohort_CohortId;
    DROP INDEX IF EXISTS IDX_tblUser_CohortId ON dbo.tblUser;
    ALTER TABLE dbo.tblUser DROP COLUMN CohortId;
END
");

            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'tblCohort')
    DROP TABLE dbo.tblCohort;
");
        }
    }
}
