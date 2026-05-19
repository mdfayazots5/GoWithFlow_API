using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GoWithFlow.Infrastructure.Data.Migrations
{
    public partial class RelaxImprovementSP_IncludeAbandoned : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
-------------------------------------------------------------------------------------------------------------
-- Created By       : Project AI Engineer
-- Date Created     : 19 May 2026
-- Description      : Relax session status filter in uspGetImprovementDataByUserId to include ABANDONED
--                    sessions so that progress page shows data during testing.
-- Version   Author               Date         Remarks
-------------------------------------------------------------------------------------------------------------
-- 1.0       Project AI Engineer  16 May 2026  Creation (COMPLETED only)
-- 1.1       Project AI Engineer  19 May 2026  Include ABANDONED for testing visibility
-------------------------------------------------------------------------------------------------------------
ALTER PROCEDURE dbo.uspGetImprovementDataByUserId
(
    @UserId                  BIGINT
)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        SELECT TOP (10)
            SessionDate = ISNULL(ses.EndedDate, ISNULL(ses.StartedDate, ses.DateCreated)),
            ses.SessionName,
            FluencyScore = CAST(ISNULL(AVG(CAST(va.FluencyScore AS DECIMAL(10,2))), 0) AS DECIMAL(5,2)),
            ConfidenceScore = CAST(ISNULL(AVG(CAST(va.ConfidenceScore AS DECIMAL(10,2))), 0) AS DECIMAL(5,2)),
            MistakeCount = SUM(ISNULL(geo.ErrorCount, 0) + ISNULL(pro.ErrorCount, 0))
        FROM dbo.tblSessionMember AS sem
        INNER JOIN dbo.tblSession AS ses
            ON ses.SessionId = sem.SessionId
           AND ses.IsDeleted = 0
        LEFT JOIN dbo.tblVoiceAnalysis AS va
            ON va.SessionId = ses.SessionId
           AND va.UserId = sem.UserId
           AND va.IsDeleted = 0
        OUTER APPLY
        (
            SELECT COUNT(1) AS ErrorCount
            FROM OPENJSON(ISNULL(va.GrammarErrorsJson, N'[]'))
        ) AS geo
        OUTER APPLY
        (
            SELECT COUNT(1) AS ErrorCount
            FROM OPENJSON(ISNULL(va.PronunciationJson, N'[]'))
        ) AS pro
        WHERE sem.UserId = @UserId
          AND sem.IsDeleted = 0
          AND ses.Status IN (N'COMPLETED', N'ABANDONED')
        GROUP BY
            ses.SessionId,
            ses.SessionName,
            ISNULL(ses.EndedDate, ISNULL(ses.StartedDate, ses.DateCreated))
        ORDER BY
            SessionDate DESC,
            ses.SessionId DESC;
    END TRY
    BEGIN CATCH
        THROW;
    END CATCH
END
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
ALTER PROCEDURE dbo.uspGetImprovementDataByUserId
(
    @UserId                  BIGINT
)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        SELECT TOP (10)
            SessionDate = ISNULL(ses.EndedDate, ISNULL(ses.StartedDate, ses.DateCreated)),
            ses.SessionName,
            FluencyScore = CAST(ISNULL(AVG(CAST(va.FluencyScore AS DECIMAL(10,2))), 0) AS DECIMAL(5,2)),
            ConfidenceScore = CAST(ISNULL(AVG(CAST(va.ConfidenceScore AS DECIMAL(10,2))), 0) AS DECIMAL(5,2)),
            MistakeCount = SUM(ISNULL(geo.ErrorCount, 0) + ISNULL(pro.ErrorCount, 0))
        FROM dbo.tblSessionMember AS sem
        INNER JOIN dbo.tblSession AS ses
            ON ses.SessionId = sem.SessionId
           AND ses.IsDeleted = 0
        LEFT JOIN dbo.tblVoiceAnalysis AS va
            ON va.SessionId = ses.SessionId
           AND va.UserId = sem.UserId
           AND va.IsDeleted = 0
        OUTER APPLY
        (
            SELECT COUNT(1) AS ErrorCount
            FROM OPENJSON(ISNULL(va.GrammarErrorsJson, N'[]'))
        ) AS geo
        OUTER APPLY
        (
            SELECT COUNT(1) AS ErrorCount
            FROM OPENJSON(ISNULL(va.PronunciationJson, N'[]'))
        ) AS pro
        WHERE sem.UserId = @UserId
          AND sem.IsDeleted = 0
          AND ses.Status = N'COMPLETED'
        GROUP BY
            ses.SessionId,
            ses.SessionName,
            ISNULL(ses.EndedDate, ISNULL(ses.StartedDate, ses.DateCreated))
        ORDER BY
            SessionDate DESC,
            ses.SessionId DESC;
    END TRY
    BEGIN CATCH
        THROW;
    END CATCH
END
");
        }
    }
}
