-- Migration 41: Fix /api/mistakes/summary 500 for users with no mistakes (2026-06-18).
-- The aggregate has no GROUP BY, so it returns one row even when there are zero mistakes — but
-- SUM(CASE...) over an empty set is NULL, which the API mapped with a non-null Int32 read → 500.
-- Wrap the SUMs in COALESCE(...,0) so resolved/pending are always 0 (never NULL). Idempotent.
-- SQL Server equivalent: Backend/Docs/SqlServerSeed/41_fix_mistake_summary_null_sums.sql.

CREATE OR REPLACE FUNCTION uspgetmistakesummarybyuserid(
    p_userid BIGINT
) RETURNS TABLE (
    totalmistakes      BIGINT,
    resolvedmistakes   BIGINT,
    pendingmistakes    BIGINT,
    improvementpercent NUMERIC(5,2)
) AS $$
BEGIN
    RETURN QUERY
    SELECT
        COUNT(1),
        COALESCE(SUM(CASE WHEN mst.isresolved = TRUE  THEN 1 ELSE 0 END), 0),
        COALESCE(SUM(CASE WHEN mst.isresolved = FALSE THEN 1 ELSE 0 END), 0),
        CAST(CASE
            WHEN COUNT(1) = 0 THEN 0
            ELSE (SUM(CASE WHEN mst.isresolved = TRUE THEN 1 ELSE 0 END) * 100.0) / COUNT(1)
        END AS NUMERIC(5,2))
    FROM tblmistake AS mst
    WHERE mst.userid    = p_userid
      AND mst.isdeleted = FALSE;
END;
$$ LANGUAGE plpgsql;
