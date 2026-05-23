using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GoWithFlow.Infrastructure.Data.Migrations
{
    /// <summary>
    /// Fixes 42804: structure of query does not match function result type in
    /// uspgetimprovementdatabyuserid on PostgreSQL (Supabase).
    ///
    /// Root cause (confirmed by live DB execution):
    ///   SUM(BIGINT) in PostgreSQL returns NUMERIC — not BIGINT.
    ///   The old RETURNS TABLE declared mistakecount BIGINT, causing the runtime 42804 mismatch.
    ///
    /// Fix:
    ///   1. Change RETURNS TABLE mistakecount BIGINT → INTEGER (C# GetInt32 compatible).
    ///   2. Add ::INTEGER cast on the SUM result (SUM → NUMERIC → ::INTEGER → int4).
    ///   3. Add ::TIMESTAMP cast on sessiondate (explicit contract match).
    ///   4. Incorporate ABANDONED status (mirrors SQL Server migration 20260523000001).
    ///
    /// Note: Migration 20260523000001 (SQL Server ALTER PROCEDURE) now has a PostgreSQL guard
    ///       so it is a no-op on PostgreSQL connections and will not block this migration.
    /// </summary>
    public partial class FixImprovementSP_PostgreSQLTypeSafety : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // PostgreSQL only — SQL Server equivalent is in migration 20260523000001
            if (migrationBuilder.ActiveProvider != "Npgsql.EntityFrameworkCore.PostgreSQL")
            {
                return;
            }

            migrationBuilder.Sql(@"
-- ------------------------------------------------------------------------------------------------------------
-- Created By   : Project AI Engineer
-- Date Created : 23 May 2026
-- Description  : Fix type-safety contract for uspgetimprovementdatabyuserid on PostgreSQL.
--                Root cause: SUM(BIGINT) returns NUMERIC in PostgreSQL (not BIGINT).
--                Fix: mistakecount declared as INTEGER; SUM result cast to ::INTEGER.
-- Version  Author               Date         Remarks
-- ------------------------------------------------------------------------------------------------------------
-- 1.0      Project AI Engineer  23 May 2026  Fixes 500 on GET /api/users/progress (verified on Supabase)
-- ------------------------------------------------------------------------------------------------------------
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
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // PostgreSQL only — SQL Server equivalent is in migration 20260523000001
            if (migrationBuilder.ActiveProvider != "Npgsql.EntityFrameworkCore.PostgreSQL")
            {
                return;
            }

            // Revert: COMPLETED-only filter, pre-fix BIGINT declaration (intentionally broken for rollback)
            migrationBuilder.Sql(@"
DROP FUNCTION IF EXISTS uspgetimprovementdatabyuserid(BIGINT);
CREATE OR REPLACE FUNCTION uspgetimprovementdatabyuserid(p_userid BIGINT)
RETURNS TABLE(
    sessiondate     TIMESTAMP,
    sessionname     CHARACTER VARYING,
    fluencyscore    NUMERIC,
    confidencescore NUMERIC,
    mistakecount    BIGINT
) AS $function$
BEGIN
    RETURN QUERY
    SELECT
        COALESCE(ses.endeddate, COALESCE(ses.starteddate, ses.datecreated))::TIMESTAMP,
        ses.sessionname::CHARACTER VARYING,
        CAST(COALESCE(AVG(CAST(va.fluencyscore    AS NUMERIC(10,2))), 0) AS NUMERIC),
        CAST(COALESCE(AVG(CAST(va.confidencescore AS NUMERIC(10,2))), 0) AS NUMERIC),
        SUM(COALESCE(geo.errorcount, 0) + COALESCE(pro.errorcount, 0))
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
      AND ses.status = 'COMPLETED'
    GROUP BY ses.sessionid, ses.sessionname,
             COALESCE(ses.endeddate, COALESCE(ses.starteddate, ses.datecreated))
    ORDER BY 1 DESC, ses.sessionid DESC
    LIMIT 10;
END;
$function$ LANGUAGE plpgsql;
");
        }
    }
}
