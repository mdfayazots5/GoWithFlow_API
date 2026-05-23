-- ============================================================================================================
-- File         : 16_fix_improvementsp_type_mismatch.sql
-- Created By   : Project AI Engineer
-- Date Created : 23 May 2026
-- Description  : Fix PostgreSQL error 42804 "structure of query does not match function result type"
--                in uspgetimprovementdatabyuserid.
--
-- Root Cause (confirmed by live DB test):
--   The RETURNS TABLE declared mistakecount BIGINT.
--   But SUM(BIGINT) in PostgreSQL returns NUMERIC (not BIGINT), causing the 42804 mismatch.
--
--   Proof: SELECT pg_typeof(SUM(1::BIGINT)) → numeric
--
-- Fix Applied:
--   1. Change RETURNS TABLE mistakecount BIGINT → INTEGER (matching C# GetInt32 call in UserRepository).
--   2. Add ::INTEGER cast on the SUM result so PostgreSQL returns int4 not numeric.
--   3. Add ::TIMESTAMP cast on sessiondate — ensures explicit match with RETURNS TABLE declaration.
--   4. Remove ::BIGINT from COUNT(*) laterals — COUNT(*) is already bigint; unnecesary cast removed.
--   5. Incorporates ABANDONED status filter (previously SQL Server-only in EF migration 20260523000001).
--
-- EF Migration equivalent : 20260523000002_FixImprovementSP_PostgreSQLTypeSafety.cs
-- Applied to Supabase     : 23 May 2026 (verified: function returns 10 rows cleanly)
-- ============================================================================================================

DROP FUNCTION IF EXISTS uspgetimprovementdatabyuserid(BIGINT);

CREATE OR REPLACE FUNCTION uspgetimprovementdatabyuserid(p_userid BIGINT)
RETURNS TABLE(
    sessiondate     TIMESTAMP,
    sessionname     CHARACTER VARYING,
    fluencyscore    NUMERIC,
    confidencescore NUMERIC,
    mistakecount    INTEGER
) AS $function$
BEGIN
    RETURN QUERY
    SELECT
        COALESCE(ses.endeddate, COALESCE(ses.starteddate, ses.datecreated))::TIMESTAMP  AS sessiondate,
        ses.sessionname::CHARACTER VARYING,
        CAST(COALESCE(AVG(CAST(va.fluencyscore    AS NUMERIC(10,2))), 0) AS NUMERIC)   AS fluencyscore,
        CAST(COALESCE(AVG(CAST(va.confidencescore AS NUMERIC(10,2))), 0) AS NUMERIC)   AS confidencescore,
        SUM(COALESCE(geo.errorcount, 0) + COALESCE(pro.errorcount, 0))::INTEGER        AS mistakecount
    FROM tblsessionmember AS sem
    INNER JOIN tblsession AS ses
        ON ses.sessionid = sem.sessionid AND ses.isdeleted = FALSE
    LEFT JOIN tblvoiceanalysis AS va
        ON va.sessionid = ses.sessionid AND va.userid = sem.userid AND va.isdeleted = FALSE
    CROSS JOIN LATERAL (
        SELECT COUNT(*) AS errorcount
        FROM jsonb_array_elements(COALESCE(va.grammarerrorsjson, '[]'::JSONB))
    ) AS geo
    CROSS JOIN LATERAL (
        SELECT COUNT(*) AS errorcount
        FROM jsonb_array_elements(COALESCE(va.pronunciationjson, '[]'::JSONB))
    ) AS pro
    WHERE sem.userid    = p_userid
      AND sem.isdeleted = FALSE
      AND ses.status IN ('COMPLETED', 'ABANDONED')
    GROUP BY ses.sessionid, ses.sessionname,
             COALESCE(ses.endeddate, COALESCE(ses.starteddate, ses.datecreated))
    ORDER BY 1 DESC, ses.sessionid DESC
    LIMIT 10;
END;
$function$ LANGUAGE plpgsql;
