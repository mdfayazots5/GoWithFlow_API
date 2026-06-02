-- Migration 21: Add tblcohort and cohortid FK to tbluser (Phase 3 Step 2)
-- Apply directly to Supabase (PostgreSQL).

CREATE TABLE IF NOT EXISTS public.tblcohort (
    cohortid    BIGSERIAL       NOT NULL,
    cohortname  VARCHAR(128)    NOT NULL,
    description VARCHAR(256)    NULL,
    isactive    BOOLEAN         NOT NULL DEFAULT TRUE,
    sortorder   INT             NOT NULL DEFAULT 0,
    tag         VARCHAR(64)     NULL,
    comments    VARCHAR(256)    NULL,
    ipaddress   VARCHAR(64)     NOT NULL DEFAULT '127.0.0.1',
    createdby   VARCHAR(128)    NOT NULL DEFAULT 'Admin',
    datecreated TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    updatedby   VARCHAR(128)    NULL,
    lastupdated TIMESTAMPTZ     NULL,
    deletedby   VARCHAR(128)    NULL,
    datedeleted TIMESTAMPTZ     NULL,
    isdeleted   BOOLEAN         NOT NULL DEFAULT FALSE,
    CONSTRAINT pk_tblcohort_cohortid PRIMARY KEY (cohortid)
);

CREATE INDEX IF NOT EXISTS idx_tblcohort_isactive ON public.tblcohort (isactive);

ALTER TABLE public.tbluser
    ADD COLUMN IF NOT EXISTS cohortid BIGINT NULL
        REFERENCES public.tblcohort(cohortid) ON DELETE SET NULL;

CREATE INDEX IF NOT EXISTS idx_tbluser_cohortid ON public.tbluser (cohortid);

-- uspinsertcohort = "uspInsertCohort".toLower()
CREATE OR REPLACE FUNCTION public.uspinsertcohort(
    p_cohortname  VARCHAR(128),
    p_description VARCHAR(256),
    p_createdby   VARCHAR(128),
    p_ipaddress   VARCHAR(64)
) RETURNS BIGINT AS $$
DECLARE v_id BIGINT;
BEGIN
    INSERT INTO public.tblcohort (cohortname, description, createdby, ipaddress)
    VALUES (p_cohortname, p_description, p_createdby, p_ipaddress)
    RETURNING cohortid INTO v_id;
    RETURN v_id;
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

-- uspgetallcohorts = "uspGetAllCohorts".toLower()
CREATE OR REPLACE FUNCTION public.uspgetallcohorts()
RETURNS TABLE (
    cohortid    BIGINT,
    cohortname  VARCHAR,
    description VARCHAR,
    isactive    BOOLEAN,
    datecreated TIMESTAMPTZ,
    membercount BIGINT
) AS $$
BEGIN
    RETURN QUERY
    SELECT
        c.cohortid,
        c.cohortname,
        c.description,
        c.isactive,
        c.datecreated,
        COUNT(u.userid)
    FROM   public.tblcohort c
    LEFT   JOIN public.tbluser u ON u.cohortid = c.cohortid AND u.isdeleted = FALSE
    WHERE  c.isdeleted = FALSE
    GROUP  BY c.cohortid, c.cohortname, c.description, c.isactive, c.datecreated
    ORDER  BY c.datecreated DESC;
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

-- uspassignusertocohort = "uspAssignUserToCohort".toLower()
CREATE OR REPLACE FUNCTION public.uspassignusertocohort(
    p_userid    BIGINT,
    p_cohortid  BIGINT,
    p_updatedby VARCHAR(128),
    p_ipaddress VARCHAR(64)
) RETURNS VOID AS $$
BEGIN
    UPDATE public.tbluser
    SET    cohortid    = p_cohortid,
           updatedby   = p_updatedby,
           lastupdated = NOW()
    WHERE  userid    = p_userid
      AND  isdeleted = FALSE;
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

-- uspgetcohortmembers = "uspGetCohortMembers".toLower()
CREATE OR REPLACE FUNCTION public.uspgetcohortmembers(p_cohortid BIGINT)
RETURNS TABLE (
    userid              BIGINT,
    fullname            VARCHAR,
    mobilenumber        VARCHAR,
    agegroup            VARCHAR,
    isactive            BOOLEAN,
    dailystreakcount    INT,
    totalsessionsplayed INT,
    lastlogindate       TIMESTAMPTZ,
    sessioncount        BIGINT,
    avgfluencyscore     NUMERIC,
    totalmistakes       BIGINT
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
        COALESCE(s.sessioncount, 0::BIGINT),
        COALESCE(s.avgfluency, 0::NUMERIC),
        COALESCE(m.mistakecount, 0::BIGINT)
    FROM   public.tbluser u
    LEFT   JOIN (
        SELECT  sm.userid,
                COUNT(DISTINCT sm.sessionid) AS sessioncount,
                COALESCE(AVG(va.overallscore), 0) AS avgfluency
        FROM   public.tblsessionmember sm
        LEFT   JOIN public.tblvoiceanalysis va ON va.sessionid = sm.sessionid
                                               AND va.userid = sm.userid
                                               AND va.isdeleted = FALSE
        WHERE  sm.isdeleted = FALSE
        GROUP  BY sm.userid
    ) s ON s.userid = u.userid
    LEFT   JOIN (
        SELECT  userid, COUNT(*) AS mistakecount
        FROM   public.tblmistake
        WHERE  isdeleted = FALSE
        GROUP  BY userid
    ) m ON m.userid = u.userid
    WHERE  u.cohortid  = p_cohortid
      AND  u.isdeleted = FALSE
    ORDER  BY u.fullname ASC;
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

-- uspgetcohortanalytics = "uspGetCohortAnalytics".toLower()
-- NOTE: Not called by backend on PostgreSQL — backend uses EF LINQ queries for analytics.
-- Included for reference only. Uses EF queries in AdminRepository.GetCohortAnalyticsAsync().
CREATE OR REPLACE FUNCTION public.uspgetcohortanalytics(p_cohortid BIGINT)
RETURNS TABLE (
    cohortid        BIGINT,
    cohortname      VARCHAR,
    description     VARCHAR,
    membercount     INT,
    avgfluencyscore NUMERIC,
    inactivecount   INT
) AS $$
BEGIN
    RETURN QUERY
    SELECT
        c.cohortid,
        c.cohortname::VARCHAR,
        c.description::VARCHAR,
        COUNT(u.userid)::INT,
        COALESCE(AVG(va.overallscore), 0)::NUMERIC,
        SUM(CASE WHEN u.lastlogindate < NOW() - INTERVAL '7 days' OR u.lastlogindate IS NULL THEN 1 ELSE 0 END)::INT
    FROM   public.tblcohort c
    LEFT   JOIN public.tbluser u ON u.cohortid = c.cohortid AND u.isdeleted = FALSE
    LEFT   JOIN public.tblsessionmember sm ON sm.userid = u.userid AND sm.isdeleted = FALSE
    LEFT   JOIN public.tblvoiceanalysis va ON va.sessionid = sm.sessionid AND va.userid = sm.userid
                                           AND va.isdeleted = FALSE
                                           AND va.datecreated >= NOW() - INTERVAL '30 days'
    WHERE  c.cohortid = p_cohortid AND c.isdeleted = FALSE
    GROUP  BY c.cohortid, c.cohortname, c.description;
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;
