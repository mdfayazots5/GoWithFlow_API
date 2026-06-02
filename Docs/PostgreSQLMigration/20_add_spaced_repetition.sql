-- Migration 20: Add spaced repetition columns + functions to tblmistake (Phase 3 Step 1)
-- Apply directly to Supabase (PostgreSQL).

-- Add columns to tblmistake
ALTER TABLE public.tblmistake
    ADD COLUMN IF NOT EXISTS reviewstage         SMALLINT    NOT NULL DEFAULT 0,
    ADD COLUMN IF NOT EXISTS nextreviewdate       TIMESTAMPTZ NULL,
    ADD COLUMN IF NOT EXISTS reviewintervaldays   INT         NOT NULL DEFAULT 0;

-- uspschedulemistakereview = "uspScheduleMistakeReview".toLower()
CREATE OR REPLACE FUNCTION public.uspschedulemistakereview(
    p_mistakeid BIGINT,
    p_updatedby VARCHAR(128),
    p_ipaddress VARCHAR(64)
) RETURNS VOID AS $$
BEGIN
    UPDATE public.tblmistake
    SET    reviewstage       = 1,
           reviewintervaldays = 1,
           nextreviewdate    = (CURRENT_DATE + INTERVAL '1 day')::TIMESTAMPTZ,
           updatedby         = p_updatedby,
           lastupdated       = NOW()
    WHERE  mistakeid   = p_mistakeid
      AND  isdeleted   = FALSE
      AND  isresolved  = TRUE
      AND  reviewstage = 0;
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

-- uspadvancemistakereview = "uspAdvanceMistakeReview".toLower()
CREATE OR REPLACE FUNCTION public.uspadvancemistakereview(
    p_mistakeid BIGINT,
    p_updatedby VARCHAR(128),
    p_ipaddress VARCHAR(64)
) RETURNS VOID AS $$
DECLARE
    v_currentstage SMALLINT;
    v_nextstage    SMALLINT;
    v_nextinterval INT;
BEGIN
    SELECT reviewstage INTO v_currentstage
    FROM   public.tblmistake
    WHERE  mistakeid = p_mistakeid AND isdeleted = FALSE;

    v_nextstage := CASE v_currentstage
        WHEN 1 THEN 2
        WHEN 2 THEN 3
        WHEN 3 THEN 4
        WHEN 4 THEN 5
        ELSE v_currentstage
    END;

    v_nextinterval := CASE v_nextstage
        WHEN 2 THEN 3
        WHEN 3 THEN 7
        WHEN 4 THEN 14
        WHEN 5 THEN 30
        ELSE 0
    END;

    UPDATE public.tblmistake
    SET    reviewstage       = v_nextstage,
           reviewintervaldays = v_nextinterval,
           nextreviewdate    = CASE WHEN v_nextstage < 5
                                    THEN (CURRENT_DATE + (v_nextinterval || ' days')::INTERVAL)::TIMESTAMPTZ
                                    ELSE NULL END,
           updatedby         = p_updatedby,
           lastupdated       = NOW()
    WHERE  mistakeid = p_mistakeid AND isdeleted = FALSE;
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

-- uspresetmistakereview = "uspResetMistakeReview".toLower()
CREATE OR REPLACE FUNCTION public.uspresetmistakereview(
    p_userid    BIGINT,
    p_grammartag VARCHAR(64),
    p_updatedby VARCHAR(128),
    p_ipaddress VARCHAR(64)
) RETURNS VOID AS $$
BEGIN
    UPDATE public.tblmistake
    SET    reviewstage       = 1,
           reviewintervaldays = 1,
           nextreviewdate    = (CURRENT_DATE + INTERVAL '1 day')::TIMESTAMPTZ,
           updatedby         = p_updatedby,
           lastupdated       = NOW()
    WHERE  userid       = p_userid
      AND  grammartag   = p_grammartag
      AND  isresolved   = TRUE
      AND  reviewstage  BETWEEN 1 AND 4
      AND  isdeleted    = FALSE;
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

-- uspgetmistakesdueforreview = "uspGetMistakesDueForReview".toLower()
CREATE OR REPLACE FUNCTION public.uspgetmistakesdueforreview(
    p_userid BIGINT
) RETURNS TABLE (
    mistakeid           BIGINT,
    userid              BIGINT,
    sessionid           BIGINT,
    utteranceid         BIGINT,
    scriptid            BIGINT,
    utterancetext       VARCHAR,
    spokentext          VARCHAR,
    mistaketype         VARCHAR,
    mistakedetail       VARCHAR,
    grammartag          VARCHAR,
    contexttag          VARCHAR,
    correctiontext      VARCHAR,
    practicecount       INT,
    isresolved          BOOLEAN,
    firstoccurrence     TIMESTAMPTZ,
    lastattempt         TIMESTAMPTZ,
    reviewstage         SMALLINT,
    nextreviewdate      TIMESTAMPTZ,
    reviewintervaldays  INT,
    sessionname         VARCHAR,
    scripttitle         VARCHAR
) AS $$
BEGIN
    RETURN QUERY
    SELECT
        m.mistakeid,
        m.userid,
        m.sessionid,
        m.utteranceid,
        m.scriptid,
        m.utterancetext,
        m.spokentext,
        m.mistaketype,
        m.mistakedetail,
        m.grammartag,
        m.contexttag,
        m.correctiontext,
        m.practicecount,
        m.isresolved,
        m.firstoccurrence,
        m.lastattempt,
        m.reviewstage,
        m.nextreviewdate,
        m.reviewintervaldays,
        s.sessionname::VARCHAR,
        sc.scripttitle::VARCHAR
    FROM   public.tblmistake m
    INNER  JOIN public.tblsession s  ON s.sessionid  = m.sessionid
    INNER  JOIN public.tblscript  sc ON sc.scriptid  = m.scriptid
    WHERE  m.userid       = p_userid
      AND  m.isresolved   = TRUE
      AND  m.reviewstage  BETWEEN 1 AND 4
      AND  m.nextreviewdate <= NOW()
      AND  m.isdeleted    = FALSE
    ORDER  BY m.nextreviewdate ASC;
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;
