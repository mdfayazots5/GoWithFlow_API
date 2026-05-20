-- --------------------------------------------
-- Provider-safe tabular routine replacements
-- --------------------------------------------
-- Supersedes cursor-based PostgreSQL routines for the
-- repository entry points still called by the API.

DROP FUNCTION IF EXISTS uspgetstreakdatabyuserid(BIGINT);
CREATE OR REPLACE FUNCTION uspgetstreakdatabyuserid(
    p_userid BIGINT
) RETURNS TABLE (
    currentstreak INT,
    longeststreak INT
) AS $$
BEGIN
    RETURN QUERY
    WITH distinctstreakday AS (
        SELECT DISTINCT usrs.streakdate
        FROM tbluserstreak AS usrs
        WHERE usrs.userid = p_userid AND usrs.isdeleted = FALSE
    ),
    groupedstreak AS (
        SELECT
            dsd.streakdate,
            dsd.streakdate - (ROW_NUMBER() OVER (ORDER BY dsd.streakdate))::INT AS streakgroup
        FROM distinctstreakday AS dsd
    ),
    longeststreakset AS (
        SELECT COUNT(1)::INT AS streaklength
        FROM groupedstreak
        GROUP BY streakgroup
    )
    SELECT
        COALESCE((SELECT usr.dailystreakcount FROM tbluser AS usr
                  WHERE usr.userid = p_userid AND usr.isdeleted = FALSE), 0) AS currentstreak,
        COALESCE((SELECT MAX(ls.streaklength) FROM longeststreakset AS ls), 0) AS longeststreak;
END;
$$ LANGUAGE plpgsql;

DROP FUNCTION IF EXISTS uspgetsessioncompletionsummary(BIGINT);
CREATE OR REPLACE FUNCTION uspgetsessioncompletionsummary(
    p_sessionid BIGINT
) RETURNS TABLE (
    userid BIGINT,
    fullname VARCHAR(128),
    fluencyscore NUMERIC(5,2),
    confidencescore NUMERIC(5,2),
    mistakecount INT,
    listenerrating NUMERIC(5,2)
) AS $$
BEGIN
    RETURN QUERY
    WITH voiceaggregate AS (
        SELECT
            va.userid,
            CAST(COALESCE(AVG(CAST(va.fluencyscore AS NUMERIC(10,2))), 0) AS NUMERIC(5,2)) AS fluencyscore,
            CAST(COALESCE(AVG(CAST(va.confidencescore AS NUMERIC(10,2))), 0) AS NUMERIC(5,2)) AS confidencescore,
            SUM(COALESCE(err.errorcount, 0))::INT AS mistakecount
        FROM tblvoiceanalysis AS va
        CROSS JOIN LATERAL (
            SELECT COUNT(*)::BIGINT AS errorcount
            FROM jsonb_array_elements(COALESCE(va.grammarerrorsjson, '[]'::JSONB))
        ) AS err
        WHERE va.sessionid = p_sessionid AND va.isdeleted = FALSE
        GROUP BY va.userid
    ),
    feedbackaggregate AS (
        SELECT
            lf.targetuserid AS userid,
            CAST(COALESCE(AVG(CASE WHEN lf.feedbacktag = 'Good' THEN 1.0 ELSE 0.0 END), 0) AS NUMERIC(5,2)) AS listenerrating
        FROM tbllistenerfeedback AS lf
        WHERE lf.sessionid = p_sessionid AND lf.isdeleted = FALSE
        GROUP BY lf.targetuserid
    )
    SELECT
        sem.userid,
        usr.fullname::VARCHAR(128),
        COALESCE(va.fluencyscore, 0)::NUMERIC(5,2),
        COALESCE(va.confidencescore, 0)::NUMERIC(5,2),
        COALESCE(va.mistakecount, 0)::INT,
        COALESCE(fa.listenerrating, 0)::NUMERIC(5,2)
    FROM tblsessionmember AS sem
    INNER JOIN tbluser AS usr ON usr.userid = sem.userid AND usr.isdeleted = FALSE
    LEFT JOIN voiceaggregate AS va ON va.userid = sem.userid
    LEFT JOIN feedbackaggregate AS fa ON fa.userid = sem.userid
    WHERE sem.sessionid = p_sessionid AND sem.isdeleted = FALSE
    ORDER BY sem.slotindex ASC, sem.sessionmemberid ASC;
END;
$$ LANGUAGE plpgsql;

DROP FUNCTION IF EXISTS uspgetalluserbysearch(VARCHAR, VARCHAR, BOOLEAN, INT, INT);
CREATE OR REPLACE FUNCTION uspgetalluserbysearch(
    p_searchterm VARCHAR(128) DEFAULT NULL,
    p_agegroup   VARCHAR(32)  DEFAULT NULL,
    p_isactive   BOOLEAN      DEFAULT NULL,
    p_pagenumber INT          DEFAULT 1,
    p_pagesize   INT          DEFAULT 10
) RETURNS TABLE (
    userid BIGINT,
    fullname VARCHAR(128),
    mobilenumber VARCHAR(16),
    agegroup VARCHAR(32),
    totalsessionsplayed INT,
    dailystreakcount INT,
    lastlogindate TIMESTAMPTZ,
    isactive BOOLEAN
) AS $$
DECLARE
    v_offset INT := (p_pagenumber - 1) * p_pagesize;
BEGIN
    RETURN QUERY
    SELECT
        usr.userid, usr.fullname::VARCHAR(128), usr.mobilenumber::VARCHAR(16), usr.agegroup::VARCHAR(32),
        usr.totalsessionsplayed, usr.dailystreakcount, usr.lastlogindate, usr.isactive
    FROM tbluser AS usr
    WHERE usr.isdeleted = FALSE
      AND (p_searchterm IS NULL
           OR usr.fullname ILIKE '%' || p_searchterm || '%'
           OR usr.mobilenumber ILIKE '%' || p_searchterm || '%')
      AND (p_agegroup IS NULL OR usr.agegroup = p_agegroup)
      AND (p_isactive IS NULL OR usr.isactive = p_isactive)
    ORDER BY usr.datecreated DESC, usr.userid DESC
    LIMIT p_pagesize OFFSET v_offset;
END;
$$ LANGUAGE plpgsql;

DROP FUNCTION IF EXISTS uspgetmistakebyuseridwithfilter(BIGINT, VARCHAR, BOOLEAN, INT, INT);
CREATE OR REPLACE FUNCTION uspgetmistakebyuseridwithfilter(
    p_userid BIGINT,
    p_mistaketype VARCHAR(32) DEFAULT NULL,
    p_isresolved BOOLEAN DEFAULT NULL,
    p_pagenumber INT DEFAULT 1,
    p_pagesize INT DEFAULT 20
) RETURNS TABLE (
    mistakeid BIGINT,
    userid BIGINT,
    sessionid BIGINT,
    utteranceid BIGINT,
    scriptid BIGINT,
    utterancetext VARCHAR(512),
    spokentext VARCHAR(512),
    mistaketype VARCHAR(32),
    mistakedetail VARCHAR(256),
    grammartag VARCHAR(64),
    contexttag VARCHAR(64),
    correctiontext VARCHAR(512),
    practicecount INT,
    isresolved BOOLEAN,
    firstoccurrence TIMESTAMPTZ,
    lastattempt TIMESTAMPTZ,
    sessionname VARCHAR(128),
    scripttitle VARCHAR(128)
) AS $$
DECLARE
    v_offset INT := (p_pagenumber - 1) * p_pagesize;
BEGIN
    RETURN QUERY
    SELECT
        mst.mistakeid, mst.userid, mst.sessionid, mst.utteranceid, mst.scriptid,
        mst.utterancetext::VARCHAR(512), mst.spokentext::VARCHAR(512), mst.mistaketype::VARCHAR(32), mst.mistakedetail::VARCHAR(256),
        mst.grammartag::VARCHAR(64), mst.contexttag::VARCHAR(64), mst.correctiontext::VARCHAR(512),
        mst.practicecount, mst.isresolved, mst.firstoccurrence, mst.lastattempt,
        ses.sessionname::VARCHAR(128), scr.scripttitle::VARCHAR(128)
    FROM tblmistake AS mst
    LEFT JOIN tblsession AS ses ON ses.sessionid = mst.sessionid AND ses.isdeleted = FALSE
    LEFT JOIN tblscript AS scr ON scr.scriptid = mst.scriptid AND scr.isdeleted = FALSE
    WHERE mst.userid = p_userid
      AND mst.isdeleted = FALSE
      AND (p_mistaketype IS NULL OR mst.mistaketype = p_mistaketype)
      AND (p_isresolved IS NULL OR mst.isresolved = p_isresolved)
    ORDER BY mst.firstoccurrence DESC, mst.mistakeid DESC
    LIMIT p_pagesize OFFSET v_offset;
END;
$$ LANGUAGE plpgsql;

DROP FUNCTION IF EXISTS uspgetrepracticesessionbyrepracticesessionid(BIGINT);
CREATE OR REPLACE FUNCTION uspgetrepracticesessionbyrepracticesessionid(
    p_repracticesessionid BIGINT
) RETURNS TABLE (
    repracticesessionid BIGINT,
    sourcesessionid BIGINT,
    status VARCHAR(16),
    totalmistakes INT,
    completedrounds INT,
    improvementpercent NUMERIC(5,2),
    generateddate TIMESTAMPTZ
) AS $$
BEGIN
    RETURN QUERY
    SELECT
        rps.repracticesessionid, rps.sourcesessionid, rps.status::VARCHAR(16),
        rps.totalmistakes, rps.completedrounds, rps.improvementpercent, rps.generateddate
    FROM tblrepracticesession AS rps
    WHERE rps.repracticesessionid = p_repracticesessionid AND rps.isdeleted = FALSE;
END;
$$ LANGUAGE plpgsql;

DROP FUNCTION IF EXISTS uspgetrepracticesessionlistbyuserid(BIGINT, VARCHAR, INT, INT);
CREATE OR REPLACE FUNCTION uspgetrepracticesessionlistbyuserid(
    p_userid BIGINT,
    p_status VARCHAR(16) DEFAULT NULL,
    p_pagenumber INT DEFAULT 1,
    p_pagesize INT DEFAULT 10
) RETURNS TABLE (
    repracticesessionid BIGINT,
    sourcesessionid BIGINT,
    status VARCHAR(16),
    totalmistakes INT,
    completedrounds INT,
    improvementpercent NUMERIC(5,2),
    generateddate TIMESTAMPTZ
) AS $$
DECLARE
    v_offset INT := (p_pagenumber - 1) * p_pagesize;
BEGIN
    RETURN QUERY
    SELECT
        rps.repracticesessionid, rps.sourcesessionid, rps.status::VARCHAR(16),
        rps.totalmistakes, rps.completedrounds, rps.improvementpercent, rps.generateddate
    FROM tblrepracticesession AS rps
    WHERE rps.userid = p_userid
      AND rps.isdeleted = FALSE
      AND (p_status IS NULL OR rps.status = p_status)
    ORDER BY rps.generateddate DESC, rps.repracticesessionid DESC
    LIMIT p_pagesize OFFSET v_offset;
END;
$$ LANGUAGE plpgsql;

DROP FUNCTION IF EXISTS uspgetscriptbysearch(VARCHAR, VARCHAR, VARCHAR, VARCHAR, BOOLEAN, INT, INT);
CREATE OR REPLACE FUNCTION uspgetscriptbysearch(
    p_searchterm VARCHAR(128) DEFAULT NULL,
    p_category VARCHAR(64) DEFAULT NULL,
    p_grammarfocustag VARCHAR(64) DEFAULT NULL,
    p_targetagegroup VARCHAR(32) DEFAULT NULL,
    p_isactive BOOLEAN DEFAULT NULL,
    p_pagenumber INT DEFAULT 1,
    p_pagesize INT DEFAULT 12
) RETURNS TABLE (
    scriptid BIGINT,
    scripttitle VARCHAR(128),
    category VARCHAR(64),
    grammarfocustag VARCHAR(64),
    contexttag VARCHAR(64),
    complexitylevel SMALLINT,
    targetagegroup VARCHAR(32),
    utterancecount INT,
    isactive BOOLEAN,
    uploadeddate TIMESTAMPTZ,
    version INT
) AS $$
DECLARE
    v_offset INT := (p_pagenumber - 1) * p_pagesize;
BEGIN
    RETURN QUERY
    SELECT
        scr.scriptid, scr.scripttitle::VARCHAR(128), scr.category::VARCHAR(64), scr.grammarfocustag::VARCHAR(64),
        scr.contexttag::VARCHAR(64), scr.complexitylevel, scr.targetagegroup::VARCHAR(32),
        scr.utterancecount, scr.isactive, scr.uploadeddate, scr.version
    FROM tblscript AS scr
    WHERE scr.isdeleted = FALSE
      AND (p_searchterm IS NULL OR scr.scripttitle ILIKE '%' || p_searchterm || '%')
      AND (p_category IS NULL OR scr.category = p_category)
      AND (p_grammarfocustag IS NULL OR scr.grammarfocustag = p_grammarfocustag)
      AND (p_targetagegroup IS NULL OR scr.targetagegroup = p_targetagegroup)
      AND (p_isactive IS NULL OR scr.isactive = p_isactive)
    ORDER BY scr.uploadeddate DESC, scr.scriptid DESC
    LIMIT p_pagesize OFFSET v_offset;
END;
$$ LANGUAGE plpgsql;

DROP FUNCTION IF EXISTS uspgetscriptdetailbyscriptid(BIGINT);
CREATE OR REPLACE FUNCTION uspgetscriptdetailbyscriptid(
    p_scriptid BIGINT
) RETURNS TABLE (
    scriptid BIGINT,
    scripttitle VARCHAR(128),
    category VARCHAR(64),
    grammarfocustag VARCHAR(64),
    contexttag VARCHAR(64),
    complexitylevel SMALLINT,
    targetagegroup VARCHAR(32),
    hintlanguage VARCHAR(32),
    isactive BOOLEAN,
    uploadeddate TIMESTAMPTZ,
    uploadedbyuserid BIGINT,
    version INT,
    utterancecount INT
) AS $$
BEGIN
    RETURN QUERY
    SELECT
        scr.scriptid, scr.scripttitle::VARCHAR(128), scr.category::VARCHAR(64), scr.grammarfocustag::VARCHAR(64),
        scr.contexttag::VARCHAR(64), scr.complexitylevel, scr.targetagegroup::VARCHAR(32),
        scr.hintlanguage::VARCHAR(32), scr.isactive, scr.uploadeddate,
        scr.uploadedbyuserid, scr.version, scr.utterancecount
    FROM tblscript AS scr
    WHERE scr.scriptid = p_scriptid AND scr.isdeleted = FALSE;
END;
$$ LANGUAGE plpgsql;

DROP FUNCTION IF EXISTS uspgetsessionbyjoincode(VARCHAR);
CREATE OR REPLACE FUNCTION uspgetsessionbyjoincode(
    p_joincode VARCHAR(8)
) RETURNS TABLE (
    sessionid BIGINT,
    sessionname VARCHAR(128),
    sessionmode VARCHAR(64),
    scripttitle VARCHAR(128),
    scriptgrammartag VARCHAR(64),
    duration INT,
    maxmembers SMALLINT,
    currentmembercount INT,
    status VARCHAR(16)
) AS $$
DECLARE
    v_sessionid BIGINT := 0;
BEGIN
    SELECT ses.sessionid
    INTO v_sessionid
    FROM tblsession AS ses
    WHERE ses.joincode = p_joincode
      AND ses.isdeleted = FALSE
      AND ses.status IN ('LOBBY', 'ACTIVE')
      AND (ses.roomexpiresat IS NULL OR ses.roomexpiresat > NOW())
    ORDER BY ses.sessionid DESC
    LIMIT 1;

    IF v_sessionid IS NULL OR v_sessionid = 0 THEN
        RETURN;
    END IF;

    RETURN QUERY
    SELECT
        ses.sessionid, ses.sessionname::VARCHAR(128), ses.sessionmode::VARCHAR(64),
        scr.scripttitle::VARCHAR(128), scr.grammarfocustag::VARCHAR(64),
        ses.sessionduration, ses.maxmembers, COUNT(sem.sessionmemberid)::INT, ses.status::VARCHAR(16)
    FROM tblsession AS ses
    INNER JOIN tblscript AS scr ON scr.scriptid = ses.scriptid AND scr.isdeleted = FALSE
    LEFT JOIN tblsessionmember AS sem
        ON sem.sessionid = ses.sessionid AND sem.isdeleted = FALSE AND sem.isactive = TRUE
    WHERE ses.sessionid = v_sessionid
    GROUP BY ses.sessionid, ses.sessionname, ses.sessionmode, scr.scripttitle, scr.grammarfocustag,
             ses.sessionduration, ses.maxmembers, ses.status;
END;
$$ LANGUAGE plpgsql;

DROP FUNCTION IF EXISTS uspgetsessionbysessionid(BIGINT);
CREATE OR REPLACE FUNCTION uspgetsessionbysessionid(
    p_sessionid BIGINT
) RETURNS TABLE (
    sessionid BIGINT,
    sessionname VARCHAR(128),
    joincode VARCHAR(8),
    sessionmode VARCHAR(64),
    scripttitle VARCHAR(128),
    maxmembers SMALLINT,
    sessionduration INT,
    status VARCHAR(16)
) AS $$
BEGIN
    RETURN QUERY
    SELECT
        ses.sessionid, ses.sessionname::VARCHAR(128), ses.joincode::VARCHAR(8), ses.sessionmode::VARCHAR(64),
        scr.scripttitle::VARCHAR(128), ses.maxmembers, ses.sessionduration, ses.status::VARCHAR(16)
    FROM tblsession AS ses
    INNER JOIN tblscript AS scr ON scr.scriptid = ses.scriptid AND scr.isdeleted = FALSE
    WHERE ses.sessionid = p_sessionid AND ses.isdeleted = FALSE;
END;
$$ LANGUAGE plpgsql;

DROP FUNCTION IF EXISTS uspgetsessiondetailbysessionid(BIGINT, BIGINT);
CREATE OR REPLACE FUNCTION uspgetsessiondetailbysessionid(
    p_sessionid BIGINT DEFAULT 0,
    p_userid BIGINT DEFAULT 0
) RETURNS TABLE (
    sessionname VARCHAR(128),
    sessionmode VARCHAR(64),
    sessiondate TIMESTAMPTZ,
    duration INT,
    scripttitle VARCHAR(128),
    membercount INT
) AS $$
BEGIN
    RETURN QUERY
    SELECT
        s.sessionname::VARCHAR(128), s.sessionmode::VARCHAR(64),
        s.datecreated AS sessiondate,
        s.sessionduration AS duration,
        sc.scripttitle::VARCHAR(128),
        (SELECT COUNT(1)::INT FROM tblsessionmember sm2 WHERE sm2.sessionid = s.sessionid AND sm2.isdeleted = FALSE) AS membercount
    FROM tblsession AS s
    INNER JOIN tblscript AS sc ON sc.scriptid = s.scriptid AND sc.isdeleted = FALSE
    WHERE s.sessionid = p_sessionid AND s.isdeleted = FALSE;
END;
$$ LANGUAGE plpgsql;

DROP FUNCTION IF EXISTS uspgetsessionlistbyuserid(BIGINT, VARCHAR, INT, INT);
CREATE OR REPLACE FUNCTION uspgetsessionlistbyuserid(
    p_userid BIGINT,
    p_statusfilter VARCHAR(16) DEFAULT NULL,
    p_pagenumber INT DEFAULT 1,
    p_pagesize INT DEFAULT 20
) RETURNS TABLE (
    sessionid BIGINT,
    sessionname VARCHAR(128),
    sessionmode VARCHAR(64),
    sessiondate TIMESTAMPTZ,
    duration INT,
    fluencyscore NUMERIC(10,2),
    mistakecount INT,
    status VARCHAR(16),
    scripttitle VARCHAR(128)
) AS $$
DECLARE
    v_offset INT := (p_pagenumber - 1) * p_pagesize;
BEGIN
    RETURN QUERY
    SELECT
        ses.sessionid, ses.sessionname::VARCHAR(128), ses.sessionmode::VARCHAR(64),
        COALESCE(ses.starteddate, ses.datecreated) AS sessiondate,
        ses.sessionduration AS duration,
        NULL::NUMERIC(10,2) AS fluencyscore,
        0::INT AS mistakecount,
        ses.status::VARCHAR(16), scr.scripttitle::VARCHAR(128)
    FROM tblsessionmember AS sem
    INNER JOIN tblsession AS ses ON ses.sessionid = sem.sessionid AND ses.isdeleted = FALSE
    INNER JOIN tblscript AS scr ON scr.scriptid = ses.scriptid AND scr.isdeleted = FALSE
    WHERE sem.userid = p_userid
      AND sem.isdeleted = FALSE
      AND (p_statusfilter IS NULL OR ses.status = p_statusfilter)
    ORDER BY COALESCE(ses.starteddate, ses.datecreated) DESC, ses.sessionid DESC
    LIMIT p_pagesize OFFSET v_offset;
END;
$$ LANGUAGE plpgsql;

DROP FUNCTION IF EXISTS uspgetuserreportsummarylist(TIMESTAMPTZ, TIMESTAMPTZ, BIGINT, INT, INT);
CREATE OR REPLACE FUNCTION uspgetuserreportsummarylist(
    p_fromdate TIMESTAMPTZ DEFAULT NULL,
    p_todate TIMESTAMPTZ DEFAULT NULL,
    p_userid BIGINT DEFAULT 0,
    p_pagenumber INT DEFAULT 1,
    p_pagesize INT DEFAULT 10
) RETURNS TABLE (
    userid BIGINT,
    fullname VARCHAR(128),
    totalsessions BIGINT,
    avgfluencyscore NUMERIC(5,2),
    mostcommonmistaketype VARCHAR(32),
    improvementpercent NUMERIC(6,2),
    lastsessiondate TIMESTAMPTZ
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
            usr.userid, usr.fullname,
            COUNT(DISTINCT sa.sessionid) AS totalsessions,
            CAST(COALESCE(AVG(sa.avgfluencyscore), 0) AS NUMERIC(5,2)) AS avgfluencyscore,
            MAX(sa.sessiondate) AS lastsessiondate
        FROM tbluser AS usr
        LEFT JOIN sessionaverage AS sa ON sa.userid = usr.userid
        WHERE usr.isdeleted = FALSE
          AND (p_userid = 0 OR usr.userid = p_userid)
        GROUP BY usr.userid, usr.fullname
    )
    SELECT
        ur.userid, ur.fullname::VARCHAR(128), ur.totalsessions,
        ur.avgfluencyscore::NUMERIC(5,2),
        COALESCE(um.mostcommonmistaketype, '')::VARCHAR(32),
        CAST(CASE
            WHEN COALESCE(ma.totalmistakecount, 0) = 0 THEN 0
            ELSE (COALESCE(ma.resolvedmistakecount, 0) * 100.0) / ma.totalmistakecount
        END AS NUMERIC(6,2)) AS improvementpercent,
        ur.lastsessiondate
    FROM userreport AS ur
    LEFT JOIN usermistake AS um ON um.userid = ur.userid
    LEFT JOIN mistakeaggregate AS ma ON ma.userid = ur.userid
    ORDER BY ur.lastsessiondate DESC, ur.userid DESC
    LIMIT p_pagesize OFFSET v_offset;
END;
$$ LANGUAGE plpgsql;

DROP FUNCTION IF EXISTS uspgetuserdashboardsummarybyuserid(BIGINT);
CREATE OR REPLACE FUNCTION uspgetuserdashboardsummarybyuserid(
    p_userid BIGINT
) RETURNS TABLE (
    username VARCHAR(128),
    currentstreak INT,
    todaydate DATE,
    pendingrepracticecount BIGINT,
    activesessionid BIGINT,
    activesessionname VARCHAR(128),
    activesessionstatus VARCHAR(16),
    joincode VARCHAR(8)
) AS $$
BEGIN
    RETURN QUERY
    SELECT
        usr.fullname::VARCHAR(128) AS username,
        usr.dailystreakcount AS currentstreak,
        CURRENT_DATE AS todaydate,
        (SELECT COUNT(1) FROM tblrepracticesession AS rps
         WHERE rps.userid = usr.userid AND rps.status = 'PENDING' AND rps.isdeleted = FALSE) AS pendingrepracticecount,
        activesession.sessionid AS activesessionid,
        activesession.sessionname::VARCHAR(128) AS activesessionname,
        activesession.status::VARCHAR(16) AS activesessionstatus,
        activesession.joincode::VARCHAR(8)
    FROM tbluser AS usr
    LEFT JOIN LATERAL (
        SELECT ses.sessionid, ses.sessionname, ses.status, ses.joincode
        FROM tblsessionmember AS sem
        INNER JOIN tblsession AS ses ON ses.sessionid = sem.sessionid AND ses.isdeleted = FALSE
        WHERE sem.userid = usr.userid
          AND sem.isdeleted = FALSE
          AND sem.isactive = TRUE
          AND ses.status IN ('LOBBY', 'ACTIVE')
        ORDER BY
            CASE WHEN ses.status = 'ACTIVE' THEN 0 ELSE 1 END,
            COALESCE(ses.starteddate, COALESCE(ses.roomexpiresat, ses.datecreated)) DESC,
            ses.sessionid DESC
        LIMIT 1
    ) AS activesession ON TRUE
    WHERE usr.userid = p_userid
      AND usr.isdeleted = FALSE
      AND usr.isactive = TRUE;
END;
$$ LANGUAGE plpgsql;

DROP FUNCTION IF EXISTS uspgetuserdetailbyuserid(BIGINT);
CREATE OR REPLACE FUNCTION uspgetuserdetailbyuserid(
    p_userid BIGINT
) RETURNS TABLE (
    userid BIGINT,
    fullname VARCHAR(128),
    mobilenumber VARCHAR(16),
    email VARCHAR(128),
    passwordhash VARCHAR(256),
    agegroup VARCHAR(32),
    preferredhintlanguage VARCHAR(32),
    avatarurl VARCHAR(256),
    groupcode VARCHAR(32),
    role VARCHAR(16),
    dailystreakcount INT,
    totalsessionsplayed INT,
    lastlogindate TIMESTAMPTZ,
    isactive BOOLEAN,
    registrationdate TIMESTAMPTZ,
    tag VARCHAR(64),
    comments VARCHAR(256),
    sortorder INT,
    ipaddress VARCHAR(64),
    createdby VARCHAR(128),
    datecreated TIMESTAMPTZ,
    updatedby VARCHAR(128),
    lastupdated TIMESTAMPTZ,
    deletedby VARCHAR(128),
    datedeleted TIMESTAMPTZ,
    isdeleted BOOLEAN,
    avgfluencyscore NUMERIC(5,2),
    mostcommonmistaketype VARCHAR(32)
) AS $$
BEGIN
    RETURN QUERY
    SELECT
        usr.userid, usr.fullname::VARCHAR(128), usr.mobilenumber::VARCHAR(16), usr.email::VARCHAR(128), usr.passwordhash::VARCHAR(256),
        usr.agegroup::VARCHAR(32), usr.preferredhintlanguage::VARCHAR(32), usr.avatarurl::VARCHAR(256), usr.groupcode::VARCHAR(32), usr.role::VARCHAR(16),
        usr.dailystreakcount, usr.totalsessionsplayed, usr.lastlogindate,
        usr.isactive, usr.registrationdate, usr.tag::VARCHAR(64), usr.comments::VARCHAR(256), usr.sortorder,
        usr.ipaddress::VARCHAR(64), usr.createdby::VARCHAR(128), usr.datecreated, usr.updatedby::VARCHAR(128), usr.lastupdated,
        usr.deletedby::VARCHAR(128), usr.datedeleted, usr.isdeleted,
        CAST(COALESCE(va.avgfluencyscore, 0) AS NUMERIC(5,2)) AS avgfluencyscore,
        COALESCE(mm.mostcommonmistaketype, '')::VARCHAR(32) AS mostcommonmistaketype
    FROM tbluser AS usr
    LEFT JOIN (
        SELECT va.userid, AVG(va.fluencyscore) AS avgfluencyscore
        FROM tblvoiceanalysis AS va WHERE va.isdeleted = FALSE
        GROUP BY va.userid
    ) AS va ON va.userid = usr.userid
    LEFT JOIN (
        SELECT ranked.userid, ranked.mistaketype AS mostcommonmistaketype
        FROM (
            SELECT counted.userid, counted.mistaketype,
                   ROW_NUMBER() OVER (PARTITION BY counted.userid ORDER BY counted.mistakecount DESC, counted.mistaketype ASC) AS rownumber
            FROM (
                SELECT mst.userid, mst.mistaketype, COUNT(1) AS mistakecount
                FROM tblmistake AS mst WHERE mst.isdeleted = FALSE
                GROUP BY mst.userid, mst.mistaketype
            ) AS counted
        ) AS ranked
        WHERE ranked.rownumber = 1
    ) AS mm ON mm.userid = usr.userid
    WHERE usr.userid = p_userid AND usr.isdeleted = FALSE;
END;
$$ LANGUAGE plpgsql;

DROP FUNCTION IF EXISTS uspgetuserfullreportbyuserid(BIGINT);
CREATE OR REPLACE FUNCTION uspgetuserfullreportbyuserid(
    p_userid BIGINT
) RETURNS TABLE (
    userid BIGINT,
    avatarurl VARCHAR(256),
    fullname VARCHAR(128),
    dailystreakcount INT,
    totalsessions BIGINT,
    avgscore NUMERIC(5,2)
) AS $$
BEGIN
    RETURN QUERY
    SELECT
        usr.userid, usr.avatarurl::VARCHAR(256), usr.fullname::VARCHAR(128), usr.dailystreakcount,
        COUNT(DISTINCT sm.sessionid) AS totalsessions,
        CAST(COALESCE(AVG(va.fluencyscore), 0) AS NUMERIC(5,2)) AS avgscore
    FROM tbluser AS usr
    LEFT JOIN tblsessionmember AS sm ON sm.userid = usr.userid AND sm.isdeleted = FALSE
    LEFT JOIN tblvoiceanalysis AS va ON va.userid = usr.userid AND va.isdeleted = FALSE
    WHERE usr.userid = p_userid AND usr.isdeleted = FALSE
    GROUP BY usr.userid, usr.avatarurl, usr.fullname, usr.dailystreakcount;
END;
$$ LANGUAGE plpgsql;

DROP FUNCTION IF EXISTS uspexportuserreportdata(TIMESTAMPTZ, TIMESTAMPTZ, BIGINT);
CREATE OR REPLACE FUNCTION uspexportuserreportdata(
    p_fromdate TIMESTAMPTZ DEFAULT NULL,
    p_todate TIMESTAMPTZ DEFAULT NULL,
    p_userid BIGINT DEFAULT NULL
) RETURNS TABLE (
    userid BIGINT,
    fullname VARCHAR(128),
    totalsessions BIGINT,
    avgscore NUMERIC(5,2),
    totalmistakes BIGINT,
    improvementpercent NUMERIC(6,2)
) AS $$
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
          AND (p_userid IS NULL OR sm.userid = p_userid)
          AND (p_fromdate IS NULL OR COALESCE(ses.starteddate, ses.datecreated) >= p_fromdate)
          AND (p_todate IS NULL OR COALESCE(ses.starteddate, ses.datecreated) < p_todate + INTERVAL '1 day')
        GROUP BY sm.userid, ses.sessionid, COALESCE(ses.starteddate, ses.datecreated)
    ),
    mistakesummary AS (
        SELECT
            mst.userid,
            COUNT(1) AS totalmistakes,
            SUM(CASE WHEN mst.isresolved = TRUE THEN 1 ELSE 0 END) AS resolvedmistakes
        FROM tblmistake AS mst
        INNER JOIN tblsession AS ses ON ses.sessionid = mst.sessionid AND ses.isdeleted = FALSE
        WHERE mst.isdeleted = FALSE
          AND (p_userid IS NULL OR mst.userid = p_userid)
          AND (p_fromdate IS NULL OR COALESCE(ses.starteddate, ses.datecreated) >= p_fromdate)
          AND (p_todate IS NULL OR COALESCE(ses.starteddate, ses.datecreated) < p_todate + INTERVAL '1 day')
        GROUP BY mst.userid
    ),
    userreport AS (
        SELECT
            usr.userid, usr.fullname,
            COUNT(DISTINCT sa.sessionid) AS totalsessions,
            CAST(COALESCE(AVG(sa.avgfluencyscore), 0) AS NUMERIC(5,2)) AS avgscore
        FROM tbluser AS usr
        LEFT JOIN sessionaverage AS sa ON sa.userid = usr.userid
        WHERE usr.isdeleted = FALSE
          AND (p_userid IS NULL OR usr.userid = p_userid)
        GROUP BY usr.userid, usr.fullname
    )
    SELECT
        ur.userid, ur.fullname::VARCHAR(128), ur.totalsessions, ur.avgscore::NUMERIC(5,2),
        COALESCE(ms.totalmistakes, 0),
        CAST(CASE
            WHEN COALESCE(ms.totalmistakes, 0) = 0 THEN 0
            ELSE (COALESCE(ms.resolvedmistakes, 0) * 100.0) / ms.totalmistakes
        END AS NUMERIC(6,2)) AS improvementpercent
    FROM userreport AS ur
    LEFT JOIN mistakesummary AS ms ON ms.userid = ur.userid
    ORDER BY ur.fullname ASC, ur.userid ASC;
END;
$$ LANGUAGE plpgsql;
