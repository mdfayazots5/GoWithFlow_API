-- ============================================================
-- Migration 35: Add avatarurl to uspGetCohortMembers
-- Reason: CohortMemberDto.AvatarUrl added; SP must return the
--   column so the repository reader can populate it.
--   Frontend cohort detail page uses app-user-avatar which
--   needs avatarUrl to show photos instead of initials.
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
    lastlogindate       TIMESTAMP,
    avatarurl           VARCHAR,
    sessioncount        INT,
    avgfluencyscore     NUMERIC,
    totalmistakes       INT
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
        u.avatarurl,
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
        SELECT  mk.userid,
                COUNT(*)::INT AS mistakecount
        FROM    public.tblmistake mk
        WHERE   mk.isdeleted = FALSE
        GROUP   BY mk.userid
    ) m ON m.userid = u.userid
    WHERE  u.cohortid  = p_cohortid
      AND  u.isdeleted = FALSE
    ORDER  BY u.fullname ASC;
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;
