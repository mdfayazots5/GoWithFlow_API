-- Migration 30: Fix uspgetalluserbysearch REFCURSOR regression introduced in migration 29
-- Root cause: migration 29 reverted the function to RETURNS SETOF REFCURSOR, but the C# reader
-- uses a simple reader.ReadAsync() loop (written for migration 13's RETURNS TABLE contract).
-- A REFCURSOR function called via SELECT * FROM fn() returns cursor name strings, not data rows,
-- causing System.IndexOutOfRangeException: 'Field not found in row: UserId'.
-- Fix: restore RETURNS TABLE contract with avatarurl included (same column set as migration 29 data cursor).

DROP FUNCTION IF EXISTS uspgetalluserbysearch(VARCHAR, VARCHAR, BOOLEAN, INT, INT);

CREATE OR REPLACE FUNCTION uspgetalluserbysearch(
    p_searchterm VARCHAR(128) DEFAULT NULL,
    p_agegroup   VARCHAR(32)  DEFAULT NULL,
    p_isactive   BOOLEAN      DEFAULT NULL,
    p_pagenumber INT          DEFAULT 1,
    p_pagesize   INT          DEFAULT 10
) RETURNS TABLE (
    userid              BIGINT,
    fullname            VARCHAR(128),
    mobilenumber        VARCHAR(16),
    agegroup            VARCHAR(32),
    totalsessionsplayed INT,
    dailystreakcount    INT,
    lastlogindate       TIMESTAMP,
    isactive            BOOLEAN,
    avatarurl           VARCHAR(256)
) AS $$
DECLARE
    v_offset INT := (p_pagenumber - 1) * p_pagesize;
BEGIN
    RETURN QUERY
    SELECT
        usr.userid,
        usr.fullname,
        usr.mobilenumber,
        usr.agegroup,
        usr.totalsessionsplayed,
        usr.dailystreakcount,
        usr.lastlogindate,
        usr.isactive,
        usr.avatarurl
    FROM tbluser AS usr
    WHERE usr.isdeleted = FALSE
      AND (p_searchterm IS NULL
           OR usr.fullname     ILIKE '%' || p_searchterm || '%'
           OR usr.mobilenumber ILIKE '%' || p_searchterm || '%')
      AND (p_agegroup IS NULL OR usr.agegroup = p_agegroup)
      AND (p_isactive IS NULL OR usr.isactive = p_isactive)
    ORDER BY usr.datecreated DESC, usr.userid DESC
    LIMIT p_pagesize OFFSET v_offset;
END;
$$ LANGUAGE plpgsql;
