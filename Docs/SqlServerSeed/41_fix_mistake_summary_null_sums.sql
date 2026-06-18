-- SQL Server seed 41: fix /api/mistakes/summary 500 for users with no mistakes (2026-06-18).
-- PostgreSQL equivalent: Backend/Docs/PostgreSQLMigration/41_fix_mistake_summary_null_sums.sql.
-- SUM(CASE...) over zero rows returns NULL; ISNULL(...,0) makes resolved/pending always 0.
-- Idempotent (CREATE OR ALTER). UNAPPLIED unless SQL Server becomes the active provider.

CREATE OR ALTER PROCEDURE dbo.uspGetMistakeSummaryByUserId
    @UserId BIGINT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        COUNT(1) AS TotalMistakes,
        ISNULL(SUM(CASE WHEN mst.IsResolved = 1 THEN 1 ELSE 0 END), 0) AS ResolvedMistakes,
        ISNULL(SUM(CASE WHEN mst.IsResolved = 0 THEN 1 ELSE 0 END), 0) AS PendingMistakes,
        CAST(CASE
            WHEN COUNT(1) = 0 THEN 0
            ELSE (SUM(CASE WHEN mst.IsResolved = 1 THEN 1 ELSE 0 END) * 100.0) / COUNT(1)
        END AS DECIMAL(5,2)) AS ImprovementPercent
    FROM dbo.tblMistake AS mst
    WHERE mst.UserId    = @UserId
      AND mst.IsDeleted = 0;
END
GO
