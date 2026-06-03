-- Migration 32: Fix type mismatch in uspgetrecentactivitylist introduced by migration 31.
-- Migration 31 declared sessiondate as TIMESTAMPTZ but tblsession.starteddate / datecreated
-- are timestamp without time zone. PostgreSQL 42804 rejects TIMESTAMP→TIMESTAMPTZ in RETURNS TABLE.
-- All column types are verified against live DB before this migration was written.
--
-- Verified live column types:
--   tblsession.sessionname    VARCHAR(128)
--   tblsession.starteddate    TIMESTAMP (no tz)
--   tblsession.datecreated    TIMESTAMP (no tz)
--   tblsession.status         VARCHAR(16)
--   tblvoiceanalysis.fluencyscore  NUMERIC(5,2)
--   tbluser.fullname          VARCHAR(128)
--   tbluser.avatarurl         VARCHAR(256)

DROP FUNCTION IF EXISTS uspgetrecentactivitylist(INT);

CREATE OR REPLACE FUNCTION uspgetrecentactivitylist(
    p_topn INT DEFAULT 10
) RETURNS TABLE (
    userfullname   VARCHAR(128),
    sessionname    VARCHAR(128),
    sessiondate    TIMESTAMP,
    fluencyscore   NUMERIC(5,2),
    mistakecount   INT,
    sessionstatus  VARCHAR(16),
    avatarurl      VARCHAR(256)
) AS $$
BEGIN
    RETURN QUERY
    SELECT
        usr.fullname::VARCHAR(128),
        ses.sessionname::VARCHAR(128),
        COALESCE(ses.starteddate, ses.datecreated),
        CAST(COALESCE(vag.avgfluencyscore, 0) AS NUMERIC(5,2)),
        COALESCE(msg.mistakecount, 0)::INT,
        ses.status::VARCHAR(16),
        usr.avatarurl::VARCHAR(256)
    FROM tblsession AS ses
    INNER JOIN tbluser AS usr ON usr.userid = ses.hostuserid
    LEFT JOIN (
        SELECT va.sessionid, AVG(va.fluencyscore) AS avgfluencyscore
        FROM tblvoiceanalysis AS va
        WHERE va.isdeleted = FALSE
        GROUP BY va.sessionid
    ) AS vag ON vag.sessionid = ses.sessionid
    LEFT JOIN (
        SELECT ms.sessionid, COUNT(1)::INT AS mistakecount
        FROM tblmistake AS ms
        WHERE ms.isdeleted = FALSE
        GROUP BY ms.sessionid
    ) AS msg ON msg.sessionid = ses.sessionid
    WHERE ses.isdeleted = FALSE
    ORDER BY COALESCE(ses.starteddate, ses.datecreated) DESC, ses.sessionid DESC
    LIMIT p_topn;
END;
$$ LANGUAGE plpgsql;
