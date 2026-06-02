using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GoWithFlow.Infrastructure.Data.Migrations
{
    public partial class AddMilestoneCertificates_Phase14 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // uspCheckAndAwardMilestoneBadge — evaluates 6 category-specific certificate criteria
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.objects WHERE type = 'P' AND name = 'uspCheckAndAwardMilestoneBadge')
    DROP PROCEDURE dbo.uspCheckAndAwardMilestoneBadge;
");
            migrationBuilder.Sql(@"
CREATE PROCEDURE dbo.uspCheckAndAwardMilestoneBadge
    @UserId     BIGINT,
    @CreatedBy  NVARCHAR(128),
    @IPAddress  NVARCHAR(64)
AS
BEGIN
    SET NOCOUNT ON;

    -- GRAMMAR_FOUNDATION: 10 GrammarDrill sessions across 5+ distinct GrammarFocusTags
    IF NOT EXISTS (SELECT 1 FROM dbo.tblUserBadge WHERE UserId = @UserId AND BadgeCode = 'GRAMMAR_FOUNDATION')
    BEGIN
        IF (
            SELECT COUNT(DISTINCT s.GrammarFocusTag)
            FROM   dbo.tblSession ss
            INNER  JOIN dbo.tblScript s ON s.ScriptId = ss.ScriptId
            INNER  JOIN dbo.tblSessionMember sm ON sm.SessionId = ss.SessionId AND sm.UserId = @UserId AND sm.IsDeleted = 0
            WHERE  ss.Category IN ('Grammar Drill', 'GrammarDrill')
              AND  ss.Status = 'COMPLETED'
              AND  ss.IsDeleted = 0
            HAVING COUNT(DISTINCT ss.SessionId) >= 10
        ) >= 5
        BEGIN
            INSERT INTO dbo.tblUserBadge (UserId, BadgeCode, BadgeName, CreatedBy, IPAddress)
            VALUES (@UserId, 'GRAMMAR_FOUNDATION', 'Grammar Foundation', @CreatedBy, @IPAddress);
        END
    END

    -- INTERVIEW_READY: avg Candidate FluencyScore >= 75 across 5+ MockInterview completed sessions
    IF NOT EXISTS (SELECT 1 FROM dbo.tblUserBadge WHERE UserId = @UserId AND BadgeCode = 'INTERVIEW_READY')
    BEGIN
        IF (
            SELECT COUNT(DISTINCT ss.SessionId)
            FROM   dbo.tblSession ss
            INNER  JOIN dbo.tblScript s ON s.ScriptId = ss.ScriptId
            INNER  JOIN dbo.tblSessionMember sm ON sm.SessionId = ss.SessionId AND sm.UserId = @UserId AND sm.IsDeleted = 0
            INNER  JOIN dbo.tblVoiceAnalysis va ON va.SessionId = ss.SessionId AND va.UserId = @UserId AND va.IsDeleted = 0
            WHERE  ss.Category IN ('Mock Interview', 'MockInterview')
              AND  ss.Status = 'COMPLETED'
              AND  ss.IsDeleted = 0
            HAVING AVG(CAST(va.FluencyScore AS DECIMAL(5,2))) >= 75
        ) >= 5
        BEGIN
            INSERT INTO dbo.tblUserBadge (UserId, BadgeCode, BadgeName, CreatedBy, IPAddress)
            VALUES (@UserId, 'INTERVIEW_READY', 'Interview Ready', @CreatedBy, @IPAddress);
        END
    END

    -- FLUENCY_MILESTONE: 8 FluencyDrill sessions with avg WPM between 80 and 120
    IF NOT EXISTS (SELECT 1 FROM dbo.tblUserBadge WHERE UserId = @UserId AND BadgeCode = 'FLUENCY_MILESTONE')
    BEGIN
        DECLARE @FluentSessions INT = (
            SELECT COUNT(DISTINCT ss.SessionId)
            FROM   dbo.tblSession ss
            INNER  JOIN dbo.tblScript s ON s.ScriptId = ss.ScriptId
            INNER  JOIN dbo.tblSessionMember sm ON sm.SessionId = ss.SessionId AND sm.UserId = @UserId AND sm.IsDeleted = 0
            INNER  JOIN dbo.tblVoiceAnalysis va ON va.SessionId = ss.SessionId AND va.UserId = @UserId AND va.IsDeleted = 0
            WHERE  ss.Category IN ('Fluency Drill', 'FluencyDrill')
              AND  ss.Status = 'COMPLETED'
              AND  ss.IsDeleted = 0
            HAVING AVG(CAST(va.SpeakingSpeedWpm AS DECIMAL(7,2))) BETWEEN 80 AND 120
        );

        IF @FluentSessions >= 8
        BEGIN
            INSERT INTO dbo.tblUserBadge (UserId, BadgeCode, BadgeName, CreatedBy, IPAddress)
            VALUES (@UserId, 'FLUENCY_MILESTONE', 'Fluency Milestone', @CreatedBy, @IPAddress);
        END
    END

    -- GRAMMAR_CORRECTOR: resolved 10 distinct GrammarTag mistake types
    IF NOT EXISTS (SELECT 1 FROM dbo.tblUserBadge WHERE UserId = @UserId AND BadgeCode = 'GRAMMAR_CORRECTOR')
    BEGIN
        DECLARE @ResolvedTagCount INT = (
            SELECT COUNT(DISTINCT GrammarTag)
            FROM   dbo.tblMistake
            WHERE  UserId    = @UserId
              AND  IsResolved = 1
              AND  GrammarTag IS NOT NULL
              AND  IsDeleted  = 0
        );

        IF @ResolvedTagCount >= 10
        BEGIN
            INSERT INTO dbo.tblUserBadge (UserId, BadgeCode, BadgeName, CreatedBy, IPAddress)
            VALUES (@UserId, 'GRAMMAR_CORRECTOR', 'Grammar Corrector', @CreatedBy, @IPAddress);
        END
    END

    -- SCENARIO_MASTER: 10 Roleplay sessions across 5 distinct ContextTags with avg FluencyScore >= 70
    IF NOT EXISTS (SELECT 1 FROM dbo.tblUserBadge WHERE UserId = @UserId AND BadgeCode = 'SCENARIO_MASTER')
    BEGIN
        IF (
            SELECT COUNT(DISTINCT s.ContextTag)
            FROM   dbo.tblSession ss
            INNER  JOIN dbo.tblScript s ON s.ScriptId = ss.ScriptId
            INNER  JOIN dbo.tblSessionMember sm ON sm.SessionId = ss.SessionId AND sm.UserId = @UserId AND sm.IsDeleted = 0
            INNER  JOIN dbo.tblVoiceAnalysis va ON va.SessionId = ss.SessionId AND va.UserId = @UserId AND va.IsDeleted = 0
            WHERE  ss.Category IN ('Roleplay', 'Role Play')
              AND  ss.Status = 'COMPLETED'
              AND  ss.IsDeleted = 0
            HAVING COUNT(DISTINCT ss.SessionId) >= 10
              AND  AVG(CAST(va.FluencyScore AS DECIMAL(5,2))) >= 70
        ) >= 5
        BEGIN
            INSERT INTO dbo.tblUserBadge (UserId, BadgeCode, BadgeName, CreatedBy, IPAddress)
            VALUES (@UserId, 'SCENARIO_MASTER', 'Scenario Master', @CreatedBy, @IPAddress);
        END
    END

    -- VOCABULARY_BUILDER: 100 vocabulary words in bank
    -- Requires tblUserVocabulary table from Phase 1 Step 2
    IF NOT EXISTS (SELECT 1 FROM dbo.tblUserBadge WHERE UserId = @UserId AND BadgeCode = 'VOCABULARY_BUILDER')
    BEGIN
        IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'tblUserVocabulary')
        BEGIN
            DECLARE @VocabCount INT = (
                SELECT COUNT(*)
                FROM   dbo.tblUserVocabulary
                WHERE  UserId        = @UserId
                  AND  IsDeleted     = 0
                  AND  CorrectCount  > 0
            );

            IF @VocabCount >= 100
            BEGIN
                INSERT INTO dbo.tblUserBadge (UserId, BadgeCode, BadgeName, CreatedBy, IPAddress)
                VALUES (@UserId, 'VOCABULARY_BUILDER', 'Vocabulary Builder', @CreatedBy, @IPAddress);
            END
        END
    END

END
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.objects WHERE type = 'P' AND name = 'uspCheckAndAwardMilestoneBadge')
    DROP PROCEDURE dbo.uspCheckAndAwardMilestoneBadge;
");
        }
    }
}
