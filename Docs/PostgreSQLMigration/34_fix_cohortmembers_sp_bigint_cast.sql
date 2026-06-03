-- ============================================================
-- Migration 34: Fix uspGetCohortMembers BIGINT type mismatch
-- Root cause: sessioncount and totalmistakes were declared as
--   BIGINT in RETURNS TABLE, but AdminRepository.GetCohortMembersAsync
--   reads them with reader.GetInt32(), which Npgsql rejects for Int64.
--   Result: InvalidCastException → 500 Internal Server Error on
--   GET /api/admin/cohorts/{id}/members.
-- Fix: Cast COUNT aggregates to INT. Change lastlogindate from
--   TIMESTAMPTZ to TIMESTAMP to match the actual tbluser column type.
-- Safe: COUNT of sessions/mistakes will never overflow INT.
-- Apply: Run directly on Supabase SQL editor.
-- ============================================================

DROP FUNCTION IF EXISTS public.uspgetcohortmembers(BIGINT);

CREATE OR REPLACE FUNCTION public.uspgetcohortmembers(p_cohortid BIGINT)
RETURNS TABLE (
    userid              BIGINT,
    fullname            VARCHAR,
    mobilenumber        VARCHAR,
    agegroup            VARCHAR,
    isactive            BOOLEAN,
    dailystreakcount    INT,
    totalsessionsplayed INT,
    lastlogindate       TIMESTAMP,   -- was TIMESTAMPTZ; tbluser.lastlogindate is TIMESTAMP
    sessioncount        INT,         -- was BIGINT; caused InvalidCastException in GetInt32()
    avgfluencyscore     NUMERIC,
    totalmistakes       INT          -- was BIGINT; caused InvalidCastException in GetInt32()
) AS $$
BEGIN
    RETURN QUERY
    SELECT
        u.userid,
        u.fullname,
        u.mobilenumber,
        u.agegroup,
        u.isactive,
        u.dailystreakcount,
        u.totalsessionsplayed,
        u.lastlogindate,
        COALESCE(s.sessioncount, 0)::INT,
        COALESCE(s.avgfluency,   0::NUMERIC),
        COALESCE(m.mistakecount, 0)::INT
    FROM   public.tbluser u
    LEFT   JOIN (
        SELECT  sm.userid,
                COUNT(DISTINCT sm.sessionid)       AS sessioncount,
                COALESCE(AVG(va.overallscore), 0)  AS avgfluency
        FROM    public.tblsessionmember sm
        LEFT    JOIN public.tblvoiceanalysis va
                     ON va.sessionid = sm.sessionid
                    AND va.userid    = sm.userid
                    AND va.isdeleted = FALSE
        WHERE   sm.isdeleted = FALSE
        GROUP   BY sm.userid
    ) s ON s.userid = u.userid
    LEFT   JOIN (
        SELECT  userid,
                COUNT(*)::INT AS mistakecount
        FROM    public.tblmistake
        WHERE   isdeleted = FALSE
        GROUP   BY userid
    ) m ON m.userid = u.userid
    WHERE  u.cohortid  = p_cohortid
      AND  u.isdeleted = FALSE
    ORDER  BY u.fullname ASC;
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;
