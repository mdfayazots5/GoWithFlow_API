-- Migration 25: Fix two drift issues introduced in migrations 20 and 22.
-- Apply to any DB that ran migrations 20–24 with the original (broken) files.
-- Safe to run on a fresh DB (IF NOT EXISTS / DROP+CREATE guard all statements).

-- ============================================================
-- FIX 1: tblweeklychallenge missing tag + comments columns
-- Root cause: migration 22 CREATE TABLE omitted these BaseAuditEntity columns.
-- EF Core maps BaseAuditEntity.Comments → column "comments"; without it,
-- any EF LINQ query against WeeklyChallenges throws PG error 42703.
-- ============================================================
ALTER TABLE public.tblweeklychallenge
    ADD COLUMN IF NOT EXISTS tag      VARCHAR(64)  NULL,
    ADD COLUMN IF NOT EXISTS comments VARCHAR(256) NULL;

-- ============================================================
-- FIX 2: tblchallengeattempt missing tag + comments columns
-- Same root cause as FIX 1.
-- ============================================================
ALTER TABLE public.tblchallengeattempt
    ADD COLUMN IF NOT EXISTS tag      VARCHAR(64)  NULL,
    ADD COLUMN IF NOT EXISTS comments VARCHAR(256) NULL;

-- ============================================================
-- FIX 3: uspgetmistakesdueforreview return-type mismatch (PG 42804)
-- Root cause: migration 20 declared firstoccurrence / lastattempt as
-- TIMESTAMPTZ but tblmistake stores them as TIMESTAMP (no timezone),
-- matching the project-wide convention established in migration 15.
-- nextreviewdate was added in migration 20 as TIMESTAMPTZ and stays that way.
-- Must DROP before CREATE OR REPLACE because RETURNS TABLE columns changed.
-- ============================================================
DROP FUNCTION IF EXISTS public.uspgetmistakesdueforreview(BIGINT);

CREATE OR REPLACE FUNCTION public.uspgetmistakesdueforreview(
    p_userid BIGINT
) RETURNS TABLE (
    mistakeid           BIGINT,
    userid              BIGINT,
    sessionid           BIGINT,
    utteranceid         BIGINT,
    scriptid            BIGINT,
    utterancetext       CHARACTER VARYING,
    spokentext          CHARACTER VARYING,
    mistaketype         CHARACTER VARYING,
    mistakedetail       CHARACTER VARYING,
    grammartag          CHARACTER VARYING,
    contexttag          CHARACTER VARYING,
    correctiontext      CHARACTER VARYING,
    practicecount       INTEGER,
    isresolved          BOOLEAN,
    firstoccurrence     TIMESTAMP,
    lastattempt         TIMESTAMP,
    reviewstage         SMALLINT,
    nextreviewdate      TIMESTAMP WITH TIME ZONE,
    reviewintervaldays  INTEGER,
    sessionname         CHARACTER VARYING,
    scripttitle         CHARACTER VARYING
) AS $function$
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
    INNER  JOIN public.tblsession s  ON s.sessionid = m.sessionid
    INNER  JOIN public.tblscript  sc ON sc.scriptid = m.scriptid
    WHERE  m.userid       = p_userid
      AND  m.isresolved   = TRUE
      AND  m.reviewstage  BETWEEN 1 AND 4
      AND  m.nextreviewdate <= NOW()
      AND  m.isdeleted    = FALSE
    ORDER  BY m.nextreviewdate ASC;
END;
$function$ LANGUAGE plpgsql SECURITY DEFINER;
