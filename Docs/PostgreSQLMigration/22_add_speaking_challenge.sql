-- Migration 22: Add tblweeklychallenge and tblchallengeattempt (Phase 3 Step 3)
-- Apply directly to Supabase (PostgreSQL).

CREATE TABLE IF NOT EXISTS public.tblweeklychallenge (
    challengeid     BIGSERIAL       NOT NULL,
    scriptid        BIGINT          NOT NULL REFERENCES public.tblscript(scriptid),
    weekstartdate   TIMESTAMPTZ     NOT NULL,
    weekenddate     TIMESTAMPTZ     NOT NULL,
    isactive        BOOLEAN         NOT NULL DEFAULT TRUE,
    tag             VARCHAR(64)     NULL,
    comments        VARCHAR(256)    NULL,
    sortorder       INT             NOT NULL DEFAULT 0,
    ipaddress       VARCHAR(64)     NOT NULL DEFAULT '127.0.0.1',
    createdby       VARCHAR(128)    NOT NULL DEFAULT 'Admin',
    datecreated     TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    updatedby       VARCHAR(128)    NULL,
    lastupdated     TIMESTAMPTZ     NULL,
    deletedby       VARCHAR(128)    NULL,
    datedeleted     TIMESTAMPTZ     NULL,
    isdeleted       BOOLEAN         NOT NULL DEFAULT FALSE,
    CONSTRAINT pk_tblweeklychallenge_challengeid PRIMARY KEY (challengeid)
);

CREATE INDEX IF NOT EXISTS idx_tblweeklychallenge_isactive    ON public.tblweeklychallenge (isactive);
CREATE INDEX IF NOT EXISTS idx_tblweeklychallenge_weekstart   ON public.tblweeklychallenge (weekstartdate);

CREATE TABLE IF NOT EXISTS public.tblchallengeattempt (
    attemptid       BIGSERIAL       NOT NULL,
    challengeid     BIGINT          NOT NULL REFERENCES public.tblweeklychallenge(challengeid),
    userid          BIGINT          NOT NULL REFERENCES public.tbluser(userid),
    fluencyscore    NUMERIC(5,2)    NOT NULL DEFAULT 0,
    attemptdate     TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    tag             VARCHAR(64)     NULL,
    comments        VARCHAR(256)    NULL,
    sortorder       INT             NOT NULL DEFAULT 0,
    ipaddress       VARCHAR(64)     NOT NULL DEFAULT '127.0.0.1',
    createdby       VARCHAR(128)    NOT NULL DEFAULT 'System',
    datecreated     TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    updatedby       VARCHAR(128)    NULL,
    lastupdated     TIMESTAMPTZ     NULL,
    deletedby       VARCHAR(128)    NULL,
    datedeleted     TIMESTAMPTZ     NULL,
    isdeleted       BOOLEAN         NOT NULL DEFAULT FALSE,
    CONSTRAINT pk_tblchallengeattempt_attemptid PRIMARY KEY (attemptid)
);

CREATE INDEX IF NOT EXISTS idx_tblchallengeattempt_challengeid      ON public.tblchallengeattempt (challengeid);
CREATE INDEX IF NOT EXISTS idx_tblchallengeattempt_userid            ON public.tblchallengeattempt (userid);
CREATE INDEX IF NOT EXISTS idx_tblchallengeattempt_challenge_user    ON public.tblchallengeattempt (challengeid, userid);

-- uspgetactivechallenge = "uspGetActiveChallenge".toLower()
-- NOTE: Backend uses EF LINQ queries for PostgreSQL. This function is for SQL Server only.
-- PostgreSQL path in ChallengeRepository.GetActiveChallengeAsync uses EF directly.
CREATE OR REPLACE FUNCTION public.uspgetactivechallenge(p_userid BIGINT)
RETURNS TABLE (
    challengeid       BIGINT,
    scriptid          BIGINT,
    scripttitle       VARCHAR,
    category          VARCHAR,
    complexitylevel   INT,
    weekstartdate     TIMESTAMPTZ,
    weekenddate       TIMESTAMPTZ,
    daysremaining     INT,
    userbestscore     NUMERIC,
    userattemptcount  BIGINT
) AS $$
BEGIN
    RETURN QUERY
    SELECT
        wc.challengeid,
        wc.scriptid,
        s.scripttitle::VARCHAR,
        s.category::VARCHAR,
        s.complexitylevel::INT,
        wc.weekstartdate,
        wc.weekenddate,
        EXTRACT(DAY FROM wc.weekenddate - NOW())::INT,
        COALESCE(best.bestscore, 0::NUMERIC),
        COALESCE(att.attemptcount, 0::BIGINT)
    FROM   public.tblweeklychallenge wc
    INNER  JOIN public.tblscript s ON s.scriptid = wc.scriptid
    LEFT   JOIN (
        SELECT  challengeid, MAX(fluencyscore) AS bestscore
        FROM   public.tblchallengeattempt
        WHERE  userid = p_userid AND isdeleted = FALSE
        GROUP  BY challengeid
    ) best ON best.challengeid = wc.challengeid
    LEFT   JOIN (
        SELECT  challengeid, COUNT(*)::BIGINT AS attemptcount
        FROM   public.tblchallengeattempt
        WHERE  userid = p_userid AND isdeleted = FALSE
        GROUP  BY challengeid
    ) att ON att.challengeid = wc.challengeid
    WHERE  wc.isactive = TRUE AND wc.isdeleted = FALSE
      AND  wc.weekstartdate <= NOW() AND wc.weekenddate >= NOW();
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

-- uspinsertchallengeattempt = "uspInsertChallengeAttempt".toLower()
CREATE OR REPLACE FUNCTION public.uspinsertchallengeattempt(
    p_challengeid BIGINT,
    p_userid      BIGINT,
    p_score       NUMERIC(5,2),
    p_createdby   VARCHAR(128),
    p_ipaddress   VARCHAR(64)
) RETURNS BIGINT AS $$
DECLARE v_id BIGINT;
BEGIN
    INSERT INTO public.tblchallengeattempt (challengeid, userid, fluencyscore, createdby, ipaddress)
    VALUES (p_challengeid, p_userid, p_score, p_createdby, p_ipaddress)
    RETURNING attemptid INTO v_id;
    RETURN v_id;
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

-- uspsetweeklychallenge = "uspSetWeeklyChallenge".toLower()
CREATE OR REPLACE FUNCTION public.uspsetweeklychallenge(
    p_scriptid  BIGINT,
    p_createdby VARCHAR(128),
    p_ipaddress VARCHAR(64)
) RETURNS BIGINT AS $$
DECLARE
    v_id     BIGINT;
    v_monday TIMESTAMPTZ;
    v_sunday TIMESTAMPTZ;
BEGIN
    UPDATE public.tblweeklychallenge
    SET    isactive    = FALSE,
           updatedby   = p_createdby,
           lastupdated = NOW()
    WHERE  isactive = TRUE AND isdeleted = FALSE;

    v_monday := DATE_TRUNC('week', NOW())::TIMESTAMPTZ;
    v_sunday := v_monday + INTERVAL '6 days 23 hours 59 minutes 59 seconds';

    INSERT INTO public.tblweeklychallenge (scriptid, weekstartdate, weekenddate, createdby, ipaddress)
    VALUES (p_scriptid, v_monday, v_sunday, p_createdby, p_ipaddress)
    RETURNING challengeid INTO v_id;

    RETURN v_id;
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;
