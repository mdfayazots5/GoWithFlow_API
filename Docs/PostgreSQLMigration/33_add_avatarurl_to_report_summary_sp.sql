-- Migration 33: Add avatarurl to uspgetuserreportsummarylist
-- tbluser is already in the userreport CTE. avatarurl added to CTE select,
-- CTE group by, and final SELECT. RETURNS TABLE types match the live function
-- declaration exactly (unqualified character varying / numeric) to avoid 42804.
-- avatarurl is an R2 key; presigned URL resolution happens in AdminService.

DROP FUNCTION IF EXISTS uspgetuserreportsummarylist(TIMESTAMP, TIMESTAMP, BIGINT, INT, INT);

CREATE OR REPLACE FUNCTION uspgetuserreportsummarylist(
    p_fromdate   TIMESTAMP DEFAULT NULL,
    p_todate     TIMESTAMP DEFAULT NULL,
    p_userid     BIGINT    DEFAULT 0,
    p_pagenumber INT       DEFAULT 1,
    p_pagesize   INT       DEFAULT 10
) RETURNS TABLE (
    userid                BIGINT,
    fullname              CHARACTER VARYING,
    totalsessions         BIGINT,
    avgfluencyscore       NUMERIC,
    mostcommonmistaketype CHARACTER VARYING,
    improvementpercent    NUMERIC,
    lastsessiondate       TIMESTAMP,
    avatarurl             CHARACTER VARYING
) AS $$
DECLARE
    v_offset INT := (p_pagenumber - 1) * p_pagesize;
BEGIN
    RETURN QUERY
    WITH sessionaverage AS (
        SELECT
            sm.userid, ses.sessionid,
            COALESCE(ses.starteddate, ses.datecreated) AS sessiondate,
            AVG(va.fluencyscore) AS avgfluencyscore
        FROM tblsessionmember AS sm
        INNER JOIN tblsession AS ses ON ses.sessionid = sm.sessionid AND ses.isdeleted = FALSE
        LEFT JOIN tblvoiceanalysis AS va
            ON va.sessionid = ses.sessionid AND va.userid = sm.userid AND va.isdeleted = FALSE
        WHERE sm.isdeleted = FALSE
          AND (p_fromdate IS NULL OR COALESCE(ses.starteddate, ses.datecreated) >= p_fromdate)
          AND (p_todate IS NULL OR COALESCE(ses.starteddate, ses.datecreated) < p_todate + INTERVAL '1 day')
        GROUP BY sm.userid, ses.sessionid, COALESCE(ses.starteddate, ses.datecreated)
    ),
    mistakeaggregate AS (
        SELECT
            mst.userid,
            COUNT(1) AS totalmistakecount,
            SUM(CASE WHEN mst.isresolved = TRUE THEN 1 ELSE 0 END) AS resolvedmistakecount
        FROM tblmistake AS mst
        INNER JOIN tblsession AS ses ON ses.sessionid = mst.sessionid AND ses.isdeleted = FALSE
        WHERE mst.isdeleted = FALSE
          AND (p_fromdate IS NULL OR COALESCE(ses.starteddate, ses.datecreated) >= p_fromdate)
          AND (p_todate IS NULL OR COALESCE(ses.starteddate, ses.datecreated) < p_todate + INTERVAL '1 day')
        GROUP BY mst.userid
    ),
    usermistake AS (
        SELECT ranked.userid, ranked.mistaketype AS mostcommonmistaketype
        FROM (
            SELECT counted.userid, counted.mistaketype,
                   ROW_NUMBER() OVER (PARTITION BY counted.userid ORDER BY counted.mistakecount DESC, counted.mistaketype ASC) AS rownumber
            FROM (
                SELECT mst.userid, mst.mistaketype, COUNT(1) AS mistakecount
                FROM tblmistake AS mst
                INNER JOIN tblsession AS ses ON ses.sessionid = mst.sessionid AND ses.isdeleted = FALSE
                WHERE mst.isdeleted = FALSE
                  AND (p_fromdate IS NULL OR COALESCE(ses.starteddate, ses.datecreated) >= p_fromdate)
                  AND (p_todate IS NULL OR COALESCE(ses.starteddate, ses.datecreated) < p_todate + INTERVAL '1 day')
                GROUP BY mst.userid, mst.mistaketype
            ) AS counted
        ) AS ranked
        WHERE ranked.rownumber = 1
    ),
    userreport AS (
        SELECT
            usr.userid, usr.fullname, usr.avatarurl,
            COUNT(DISTINCT sa.sessionid) AS totalsessions,
            CAST(COALESCE(AVG(sa.avgfluencyscore), 0) AS NUMERIC(5,2)) AS avgfluencyscore,
            MAX(sa.sessiondate) AS lastsessiondate
        FROM tbluser AS usr
        LEFT JOIN sessionaverage AS sa ON sa.userid = usr.userid
        WHERE usr.isdeleted = FALSE
          AND (p_userid = 0 OR usr.userid = p_userid)
        GROUP BY usr.userid, usr.fullname, usr.avatarurl
    )
    SELECT
        ur.userid, ur.fullname::VARCHAR(128), ur.totalsessions,
        ur.avgfluencyscore::NUMERIC(5,2),
        COALESCE(um.mostcommonmistaketype, '')::VARCHAR(32),
        CAST(CASE
            WHEN COALESCE(ma.totalmistakecount, 0) = 0 THEN 0
            ELSE (COALESCE(ma.resolvedmistakecount, 0) * 100.0) / ma.totalmistakecount
        END AS NUMERIC(6,2)) AS improvementpercent,
        ur.lastsessiondate,
        ur.avatarurl
    FROM userreport AS ur
    LEFT JOIN usermistake AS um ON um.userid = ur.userid
    LEFT JOIN mistakeaggregate AS ma ON ma.userid = ur.userid
    ORDER BY ur.lastsessiondate DESC, ur.userid DESC
    LIMIT p_pagesize OFFSET v_offset;
END;
$$ LANGUAGE plpgsql;
