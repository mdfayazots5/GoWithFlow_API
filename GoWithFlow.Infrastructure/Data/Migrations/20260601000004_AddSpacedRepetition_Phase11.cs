using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GoWithFlow.Infrastructure.Data.Migrations
{
    public partial class AddSpacedRepetition_Phase11 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add spaced repetition columns to tblMistake
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'tblMistake' AND COLUMN_NAME = 'ReviewStage')
BEGIN
    ALTER TABLE dbo.tblMistake
        ADD ReviewStage         TINYINT     NOT NULL DEFAULT 0,
            NextReviewDate      DATETIME2   NULL,
            ReviewIntervalDays  INT         NOT NULL DEFAULT 0;
END
");

            // uspScheduleMistakeReview — called when a mistake is first resolved.
            // Sets ReviewStage = 1, ReviewIntervalDays = 1, NextReviewDate = tomorrow.
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.objects WHERE type = 'P' AND name = 'uspScheduleMistakeReview')
    DROP PROCEDURE dbo.uspScheduleMistakeReview;
");
            migrationBuilder.Sql(@"
CREATE PROCEDURE dbo.uspScheduleMistakeReview
    @MistakeId  BIGINT,
    @UpdatedBy  NVARCHAR(128),
    @IPAddress  NVARCHAR(64)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.tblMistake
    SET    ReviewStage        = 1,
           ReviewIntervalDays = 1,
           NextReviewDate     = DATEADD(DAY, 1, CAST(GETDATE() AS DATE)),
           UpdatedBy          = @UpdatedBy,
           LastUpdated        = GETDATE()
    WHERE  MistakeId = @MistakeId
      AND  IsDeleted = 0
      AND  IsResolved = 1
      AND  ReviewStage = 0;
END
");

            // uspAdvanceMistakeReview — called when a review session is completed successfully.
            // Stage 1 → 2 (+3 days), 2 → 3 (+7 days), 3 → 4 (+14 days), 4 → 5 (+30 days, retained).
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.objects WHERE type = 'P' AND name = 'uspAdvanceMistakeReview')
    DROP PROCEDURE dbo.uspAdvanceMistakeReview;
");
            migrationBuilder.Sql(@"
CREATE PROCEDURE dbo.uspAdvanceMistakeReview
    @MistakeId  BIGINT,
    @UpdatedBy  NVARCHAR(128),
    @IPAddress  NVARCHAR(64)
AS
BEGIN
    SET NOCOUNT ON;

    DECLARE @CurrentStage   TINYINT;
    DECLARE @NextInterval   INT;
    DECLARE @NextStage      TINYINT;

    SELECT @CurrentStage = ReviewStage
    FROM   dbo.tblMistake
    WHERE  MistakeId = @MistakeId AND IsDeleted = 0;

    SET @NextStage    = CASE @CurrentStage
                            WHEN 1 THEN 2
                            WHEN 2 THEN 3
                            WHEN 3 THEN 4
                            WHEN 4 THEN 5
                            ELSE @CurrentStage
                        END;

    SET @NextInterval = CASE @NextStage
                            WHEN 2 THEN 3
                            WHEN 3 THEN 7
                            WHEN 4 THEN 14
                            WHEN 5 THEN 30
                            ELSE 0
                        END;

    UPDATE dbo.tblMistake
    SET    ReviewStage        = @NextStage,
           ReviewIntervalDays = @NextInterval,
           NextReviewDate     = CASE WHEN @NextStage < 5
                                     THEN DATEADD(DAY, @NextInterval, CAST(GETDATE() AS DATE))
                                     ELSE NULL END,
           UpdatedBy          = @UpdatedBy,
           LastUpdated        = GETDATE()
    WHERE  MistakeId = @MistakeId
      AND  IsDeleted = 0;
END
");

            // uspResetMistakeReview — called when the same grammar mistake recurs before its review date.
            // Resets to stage 1, +1 day.
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.objects WHERE type = 'P' AND name = 'uspResetMistakeReview')
    DROP PROCEDURE dbo.uspResetMistakeReview;
");
            migrationBuilder.Sql(@"
CREATE PROCEDURE dbo.uspResetMistakeReview
    @UserId     BIGINT,
    @GrammarTag NVARCHAR(64),
    @UpdatedBy  NVARCHAR(128),
    @IPAddress  NVARCHAR(64)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.tblMistake
    SET    ReviewStage        = 1,
           ReviewIntervalDays = 1,
           NextReviewDate     = DATEADD(DAY, 1, CAST(GETDATE() AS DATE)),
           UpdatedBy          = @UpdatedBy,
           LastUpdated        = GETDATE()
    WHERE  UserId     = @UserId
      AND  GrammarTag = @GrammarTag
      AND  IsResolved = 1
      AND  ReviewStage BETWEEN 1 AND 4
      AND  IsDeleted   = 0;
END
");

            // uspGetMistakesDueForReview — returns resolved mistakes whose NextReviewDate has arrived.
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.objects WHERE type = 'P' AND name = 'uspGetMistakesDueForReview')
    DROP PROCEDURE dbo.uspGetMistakesDueForReview;
");
            migrationBuilder.Sql(@"
CREATE PROCEDURE dbo.uspGetMistakesDueForReview
    @UserId BIGINT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        m.MistakeId,
        m.UserId,
        m.SessionId,
        m.UtteranceId,
        m.ScriptId,
        m.UtteranceText,
        m.SpokenText,
        m.MistakeType,
        m.MistakeDetail,
        m.GrammarTag,
        m.ContextTag,
        m.CorrectionText,
        m.PracticeCount,
        m.IsResolved,
        m.FirstOccurrence,
        m.LastAttempt,
        m.ReviewStage,
        m.NextReviewDate,
        m.ReviewIntervalDays,
        s.SessionName,
        sc.ScriptTitle
    FROM   dbo.tblMistake m
    INNER  JOIN dbo.tblSession s  ON s.SessionId  = m.SessionId
    INNER  JOIN dbo.tblScript  sc ON sc.ScriptId  = m.ScriptId
    WHERE  m.UserId        = @UserId
      AND  m.IsResolved    = 1
      AND  m.ReviewStage   BETWEEN 1 AND 4
      AND  m.NextReviewDate <= GETDATE()
      AND  m.IsDeleted     = 0
    ORDER  BY m.NextReviewDate ASC;
END
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.objects WHERE type = 'P' AND name = 'uspGetMistakesDueForReview')
    DROP PROCEDURE dbo.uspGetMistakesDueForReview;
IF EXISTS (SELECT 1 FROM sys.objects WHERE type = 'P' AND name = 'uspResetMistakeReview')
    DROP PROCEDURE dbo.uspResetMistakeReview;
IF EXISTS (SELECT 1 FROM sys.objects WHERE type = 'P' AND name = 'uspAdvanceMistakeReview')
    DROP PROCEDURE dbo.uspAdvanceMistakeReview;
IF EXISTS (SELECT 1 FROM sys.objects WHERE type = 'P' AND name = 'uspScheduleMistakeReview')
    DROP PROCEDURE dbo.uspScheduleMistakeReview;
");

            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'tblMistake' AND COLUMN_NAME = 'ReviewStage')
BEGIN
    ALTER TABLE dbo.tblMistake
        DROP COLUMN ReviewStage, NextReviewDate, ReviewIntervalDays;
END
");
        }
    }
}
