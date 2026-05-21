-- ============================================================
-- 15_timestamp_type_contract_fix.sql
-- ============================================================
-- Fixes all 20 deployed functions whose RETURNS TABLE declared
-- TIMESTAMPTZ (timestamp with time zone) but the underlying
-- tbl* tables use TIMESTAMP (timestamp without time zone).
-- Error produced at runtime: 42804 — return type mismatch.
-- ============================================================

-- 1. uspgetadminnotebytargetuserid
DROP FUNCTION IF EXISTS uspgetadminnotebytargetuserid(bigint);
CREATE OR REPLACE FUNCTION uspgetadminnotebytargetuserid(p_targetuserid BIGINT)
RETURNS TABLE(
    adminnoteid  BIGINT,
    adminuserid  BIGINT,
    adminname    CHARACTER VARYING,
    notetext     CHARACTER VARYING,
    notedate     TIMESTAMP
) AS $function$
BEGIN
    RETURN QUERY
    SELECT an.adminnoteid, an.adminuserid, au.fullname::VARCHAR(128), an.notetext, an.notedate
    FROM tbladminnote AS an
    INNER JOIN tbluser AS au ON au.userid = an.adminuserid
    WHERE an.targetuserid = p_targetuserid
      AND an.isdeleted    = FALSE
    ORDER BY an.notedate DESC, an.adminnoteid DESC;
END;
$function$ LANGUAGE plpgsql;

-- 2. uspgetalluserbysearch
DROP FUNCTION IF EXISTS uspgetalluserbysearch(CHARACTER VARYING, CHARACTER VARYING, BOOLEAN, INTEGER, INTEGER);
CREATE OR REPLACE FUNCTION uspgetalluserbysearch(
    p_searchterm    CHARACTER VARYING DEFAULT NULL,
    p_agegroup      CHARACTER VARYING DEFAULT NULL,
    p_isactive      BOOLEAN           DEFAULT NULL,
    p_pagenumber    INTEGER           DEFAULT 1,
    p_pagesize      INTEGER           DEFAULT 10
) RETURNS TABLE(
    userid             BIGINT,
    fullname           CHARACTER VARYING,
    mobilenumber       CHARACTER VARYING,
    agegroup           CHARACTER VARYING,
    totalsessionsplayed INTEGER,
    dailystreakcount   INTEGER,
    lastlogindate      TIMESTAMP,
    isactive           BOOLEAN
) AS $function$
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
$function$ LANGUAGE plpgsql;

-- 3. uspgetimprovementdatabyuserid
DROP FUNCTION IF EXISTS uspgetimprovementdatabyuserid(BIGINT);
CREATE OR REPLACE FUNCTION uspgetimprovementdatabyuserid(p_userid BIGINT)
RETURNS TABLE(
    sessiondate     TIMESTAMP,
    sessionname     CHARACTER VARYING,
    fluencyscore    NUMERIC,
    confidencescore NUMERIC,
    mistakecount    BIGINT
) AS $function$
BEGIN
    RETURN QUERY
    SELECT
        COALESCE(ses.endeddate, COALESCE(ses.starteddate, ses.datecreated)) AS sessiondate,
        ses.sessionname::VARCHAR(128),
        CAST(COALESCE(AVG(CAST(va.fluencyscore AS NUMERIC(10,2))), 0) AS NUMERIC(5,2)),
        CAST(COALESCE(AVG(CAST(va.confidencescore AS NUMERIC(10,2))), 0) AS NUMERIC(5,2)),
        SUM(COALESCE(geo.errorcount, 0) + COALESCE(pro.errorcount, 0))
    FROM tblsessionmember AS sem
    INNER JOIN tblsession AS ses
        ON ses.sessionid = sem.sessionid AND ses.isdeleted = FALSE
    LEFT JOIN tblvoiceanalysis AS va
        ON va.sessionid = ses.sessionid AND va.userid = sem.userid AND va.isdeleted = FALSE
    CROSS JOIN LATERAL (
        SELECT COUNT(*)::BIGINT AS errorcount
        FROM jsonb_array_elements(COALESCE(va.grammarerrorsjson, '[]'::JSONB))
    ) AS geo
    CROSS JOIN LATERAL (
        SELECT COUNT(*)::BIGINT AS errorcount
        FROM jsonb_array_elements(COALESCE(va.pronunciationjson, '[]'::JSONB))
    ) AS pro
    WHERE sem.userid    = p_userid
      AND sem.isdeleted = FALSE
      AND ses.status IN ('COMPLETED', 'ABANDONED')
    GROUP BY ses.sessionid, ses.sessionname,
             COALESCE(ses.endeddate, COALESCE(ses.starteddate, ses.datecreated))
    ORDER BY 1 DESC, ses.sessionid DESC
    LIMIT 10;
END;
$function$ LANGUAGE plpgsql;

-- 4. uspgetmistakebyuseridwithfilter
DROP FUNCTION IF EXISTS uspgetmistakebyuseridwithfilter(BIGINT, CHARACTER VARYING, BOOLEAN, INTEGER, INTEGER);
CREATE OR REPLACE FUNCTION uspgetmistakebyuseridwithfilter(
    p_userid      BIGINT,
    p_mistaketype CHARACTER VARYING DEFAULT NULL,
    p_isresolved  BOOLEAN           DEFAULT NULL,
    p_pagenumber  INTEGER           DEFAULT 1,
    p_pagesize    INTEGER           DEFAULT 20
) RETURNS TABLE(
    mistakeid      BIGINT,
    userid         BIGINT,
    sessionid      BIGINT,
    utteranceid    BIGINT,
    scriptid       BIGINT,
    utterancetext  CHARACTER VARYING,
    spokentext     CHARACTER VARYING,
    mistaketype    CHARACTER VARYING,
    mistakedetail  CHARACTER VARYING,
    grammartag     CHARACTER VARYING,
    contexttag     CHARACTER VARYING,
    correctiontext CHARACTER VARYING,
    practicecount  INTEGER,
    isresolved     BOOLEAN,
    firstoccurrence TIMESTAMP,
    lastattempt    TIMESTAMP,
    sessionname    CHARACTER VARYING,
    scripttitle    CHARACTER VARYING
) AS $function$
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
$function$ LANGUAGE plpgsql;

-- 5. uspgetrecentactivitylist
DROP FUNCTION IF EXISTS uspgetrecentactivitylist(INTEGER);
CREATE OR REPLACE FUNCTION uspgetrecentactivitylist(p_topn INTEGER DEFAULT 10)
RETURNS TABLE(
    userfullname  CHARACTER VARYING,
    sessionname   CHARACTER VARYING,
    sessiondate   TIMESTAMP,
    fluencyscore  NUMERIC,
    mistakecount  INTEGER,
    sessionstatus CHARACTER VARYING
) AS $function$
BEGIN
    RETURN QUERY
    SELECT
        usr.fullname::VARCHAR(128),
        ses.sessionname::VARCHAR(128),
        COALESCE(ses.starteddate, ses.datecreated),
        CAST(COALESCE(vag.avgfluencyscore, 0) AS NUMERIC(5,2)),
        COALESCE(msg.mistakecount, 0)::INT,
        ses.status::VARCHAR(16)
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
$function$ LANGUAGE plpgsql;

-- 6. uspgetrefreshtokenbytoken
DROP FUNCTION IF EXISTS uspgetrefreshtokenbytoken(CHARACTER VARYING);
CREATE OR REPLACE FUNCTION uspgetrefreshtokenbytoken(p_token CHARACTER VARYING)
RETURNS TABLE(
    refreshtokenid BIGINT,
    userid         BIGINT,
    token          CHARACTER VARYING,
    expiresat      TIMESTAMP,
    isrevoked      BOOLEAN,
    revokedat      TIMESTAMP,
    deviceinfo     CHARACTER VARYING,
    tag            CHARACTER VARYING,
    comments       CHARACTER VARYING,
    sortorder      INTEGER,
    ipaddress      CHARACTER VARYING,
    createdby      CHARACTER VARYING,
    datecreated    TIMESTAMP,
    updatedby      CHARACTER VARYING,
    lastupdated    TIMESTAMP,
    deletedby      CHARACTER VARYING,
    datedeleted    TIMESTAMP,
    isdeleted      BOOLEAN
) AS $function$
BEGIN
    RETURN QUERY
    SELECT
        rft.refreshtokenid, rft.userid, rft.token, rft.expiresat,
        rft.isrevoked, rft.revokedat, rft.deviceinfo, rft.tag,
        rft.comments, rft.sortorder, rft.ipaddress, rft.createdby,
        rft.datecreated, rft.updatedby, rft.lastupdated,
        rft.deletedby, rft.datedeleted, rft.isdeleted
    FROM tblrefreshtoken AS rft
    WHERE rft.token     = p_token
      AND rft.isrevoked = FALSE
      AND rft.isdeleted = FALSE;
END;
$function$ LANGUAGE plpgsql;

-- 7. uspgetrepracticesessionbyrepracticesessionid
DROP FUNCTION IF EXISTS uspgetrepracticesessionbyrepracticesessionid(BIGINT);
CREATE OR REPLACE FUNCTION uspgetrepracticesessionbyrepracticesessionid(p_repracticesessionid BIGINT)
RETURNS TABLE(
    repracticesessionid BIGINT,
    sourcesessionid     BIGINT,
    status              CHARACTER VARYING,
    totalmistakes       INTEGER,
    completedrounds     INTEGER,
    improvementpercent  NUMERIC,
    generateddate       TIMESTAMP
) AS $function$
BEGIN
    RETURN QUERY
    SELECT
        rps.repracticesessionid, rps.sourcesessionid, rps.status::VARCHAR(16),
        rps.totalmistakes, rps.completedrounds, rps.improvementpercent, rps.generateddate
    FROM tblrepracticesession AS rps
    WHERE rps.repracticesessionid = p_repracticesessionid AND rps.isdeleted = FALSE;
END;
$function$ LANGUAGE plpgsql;

-- 8. uspgetrepracticesessionlistbyuserid
DROP FUNCTION IF EXISTS uspgetrepracticesessionlistbyuserid(BIGINT, CHARACTER VARYING, INTEGER, INTEGER);
CREATE OR REPLACE FUNCTION uspgetrepracticesessionlistbyuserid(
    p_userid     BIGINT,
    p_status     CHARACTER VARYING DEFAULT NULL,
    p_pagenumber INTEGER           DEFAULT 1,
    p_pagesize   INTEGER           DEFAULT 10
) RETURNS TABLE(
    repracticesessionid BIGINT,
    sourcesessionid     BIGINT,
    status              CHARACTER VARYING,
    totalmistakes       INTEGER,
    completedrounds     INTEGER,
    improvementpercent  NUMERIC,
    generateddate       TIMESTAMP
) AS $function$
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
$function$ LANGUAGE plpgsql;

-- 9. uspgetscriptbysearch
DROP FUNCTION IF EXISTS uspgetscriptbysearch(CHARACTER VARYING, CHARACTER VARYING, CHARACTER VARYING, CHARACTER VARYING, BOOLEAN, INTEGER, INTEGER);
CREATE OR REPLACE FUNCTION uspgetscriptbysearch(
    p_searchterm      CHARACTER VARYING DEFAULT NULL,
    p_category        CHARACTER VARYING DEFAULT NULL,
    p_grammarfocustag CHARACTER VARYING DEFAULT NULL,
    p_targetagegroup  CHARACTER VARYING DEFAULT NULL,
    p_isactive        BOOLEAN           DEFAULT NULL,
    p_pagenumber      INTEGER           DEFAULT 1,
    p_pagesize        INTEGER           DEFAULT 12
) RETURNS TABLE(
    scriptid        BIGINT,
    scripttitle     CHARACTER VARYING,
    category        CHARACTER VARYING,
    grammarfocustag CHARACTER VARYING,
    contexttag      CHARACTER VARYING,
    complexitylevel SMALLINT,
    targetagegroup  CHARACTER VARYING,
    utterancecount  INTEGER,
    isactive        BOOLEAN,
    uploadeddate    TIMESTAMP,
    version         INTEGER
) AS $function$
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
$function$ LANGUAGE plpgsql;

-- 10. uspgetscriptdetailbyscriptid
DROP FUNCTION IF EXISTS uspgetscriptdetailbyscriptid(BIGINT);
CREATE OR REPLACE FUNCTION uspgetscriptdetailbyscriptid(p_scriptid BIGINT)
RETURNS TABLE(
    scriptid          BIGINT,
    scripttitle       CHARACTER VARYING,
    category          CHARACTER VARYING,
    grammarfocustag   CHARACTER VARYING,
    contexttag        CHARACTER VARYING,
    complexitylevel   SMALLINT,
    targetagegroup    CHARACTER VARYING,
    hintlanguage      CHARACTER VARYING,
    isactive          BOOLEAN,
    uploadeddate      TIMESTAMP,
    uploadedbyuserid  BIGINT,
    version           INTEGER,
    utterancecount    INTEGER
) AS $function$
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
$function$ LANGUAGE plpgsql;

-- 11. uspgetscriptversionhistorybyscriptid
DROP FUNCTION IF EXISTS uspgetscriptversionhistorybyscriptid(BIGINT);
CREATE OR REPLACE FUNCTION uspgetscriptversionhistorybyscriptid(p_scriptid BIGINT)
RETURNS TABLE(
    scriptversionid  BIGINT,
    scriptid         BIGINT,
    versionnumber    INTEGER,
    versionnotes     CHARACTER VARYING,
    uploadedbyuserid BIGINT,
    uploadeddate     TIMESTAMP
) AS $function$
BEGIN
    RETURN QUERY
    SELECT sv.scriptversionid, sv.scriptid, sv.versionnumber,
           sv.versionnotes, sv.uploadedbyuserid, sv.uploadeddate
    FROM tblscriptversion AS sv
    WHERE sv.scriptid  = p_scriptid
      AND sv.isdeleted = FALSE
    ORDER BY sv.versionnumber DESC, sv.scriptversionid DESC;
END;
$function$ LANGUAGE plpgsql;

-- 12. uspgetsessiondetailbysessionid
DROP FUNCTION IF EXISTS uspgetsessiondetailbysessionid(BIGINT, BIGINT);
CREATE OR REPLACE FUNCTION uspgetsessiondetailbysessionid(
    p_sessionid BIGINT DEFAULT 0,
    p_userid    BIGINT DEFAULT 0
) RETURNS TABLE(
    sessionname  CHARACTER VARYING,
    sessionmode  CHARACTER VARYING,
    sessiondate  TIMESTAMP,
    duration     INTEGER,
    scripttitle  CHARACTER VARYING,
    membercount  INTEGER
) AS $function$
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
$function$ LANGUAGE plpgsql;

-- 13. uspgetsessionlistbyuserid
DROP FUNCTION IF EXISTS uspgetsessionlistbyuserid(BIGINT, CHARACTER VARYING, INTEGER, INTEGER);
CREATE OR REPLACE FUNCTION uspgetsessionlistbyuserid(
    p_userid       BIGINT,
    p_statusfilter CHARACTER VARYING DEFAULT NULL,
    p_pagenumber   INTEGER           DEFAULT 1,
    p_pagesize     INTEGER           DEFAULT 20
) RETURNS TABLE(
    sessionid    BIGINT,
    sessionname  CHARACTER VARYING,
    sessionmode  CHARACTER VARYING,
    sessiondate  TIMESTAMP,
    duration     INTEGER,
    fluencyscore NUMERIC,
    mistakecount INTEGER,
    status       CHARACTER VARYING,
    scripttitle  CHARACTER VARYING
) AS $function$
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
$function$ LANGUAGE plpgsql;

-- 14. uspgetunresolvedmistakebyuserid
DROP FUNCTION IF EXISTS uspgetunresolvedmistakebyuserid(BIGINT, BIGINT);
CREATE OR REPLACE FUNCTION uspgetunresolvedmistakebyuserid(
    p_userid          BIGINT,
    p_sourcesessionid BIGINT DEFAULT 0
) RETURNS TABLE(
    mistakeid       BIGINT,
    userid          BIGINT,
    sessionid       BIGINT,
    utteranceid     BIGINT,
    scriptid        BIGINT,
    utterancetext   CHARACTER VARYING,
    spokentext      CHARACTER VARYING,
    mistaketype     CHARACTER VARYING,
    mistakedetail   CHARACTER VARYING,
    grammartag      CHARACTER VARYING,
    contexttag      CHARACTER VARYING,
    correctiontext  CHARACTER VARYING,
    practicecount   INTEGER,
    isresolved      BOOLEAN,
    firstoccurrence TIMESTAMP,
    lastattempt     TIMESTAMP
) AS $function$
BEGIN
    RETURN QUERY
    SELECT
        mst.mistakeid, mst.userid, mst.sessionid, mst.utteranceid, mst.scriptid,
        mst.utterancetext, mst.spokentext, mst.mistaketype, mst.mistakedetail,
        mst.grammartag, mst.contexttag, mst.correctiontext,
        mst.practicecount, mst.isresolved, mst.firstoccurrence, mst.lastattempt
    FROM tblmistake AS mst
    WHERE mst.userid     = p_userid
      AND mst.isresolved = FALSE
      AND mst.isdeleted  = FALSE
      AND (p_sourcesessionid = 0 OR mst.sessionid = p_sourcesessionid)
    ORDER BY
        CASE mst.mistaketype
            WHEN 'GRAMMAR'       THEN 1
            WHEN 'PRONUNCIATION' THEN 2
            WHEN 'HESITATION'    THEN 3
            WHEN 'SPEED'         THEN 4
            WHEN 'SKIP'          THEN 5
            WHEN 'INCOMPLETE'    THEN 6
            ELSE 7
        END,
        mst.firstoccurrence DESC,
        mst.mistakeid DESC;
END;
$function$ LANGUAGE plpgsql;

-- 15. uspgetuserbadgebyuserid
DROP FUNCTION IF EXISTS uspgetuserbadgebyuserid(BIGINT);
CREATE OR REPLACE FUNCTION uspgetuserbadgebyuserid(p_userid BIGINT)
RETURNS TABLE(
    userbadgeid BIGINT,
    userid      BIGINT,
    badgecode   CHARACTER VARYING,
    badgename   CHARACTER VARYING,
    earneddate  TIMESTAMP
) AS $function$
BEGIN
    RETURN QUERY
    SELECT ub.userbadgeid, ub.userid, ub.badgecode, ub.badgename, ub.earneddate
    FROM tbluserbadge AS ub
    WHERE ub.userid    = p_userid
      AND ub.isdeleted = FALSE
    ORDER BY ub.earneddate DESC, ub.userbadgeid DESC;
END;
$function$ LANGUAGE plpgsql;

-- 16. uspgetuserprofilebyuserid
DROP FUNCTION IF EXISTS uspgetuserprofilebyuserid(BIGINT);
CREATE OR REPLACE FUNCTION uspgetuserprofilebyuserid(p_userid BIGINT)
RETURNS TABLE(
    userid                BIGINT,
    fullname              CHARACTER VARYING,
    mobilenumber          CHARACTER VARYING,
    email                 CHARACTER VARYING,
    agegroup              CHARACTER VARYING,
    preferredhintlanguage CHARACTER VARYING,
    avatarurl             CHARACTER VARYING,
    role                  CHARACTER VARYING,
    dailystreakcount      INTEGER,
    totalsessionsplayed   INTEGER,
    totalsessions         INTEGER,
    avgfluencyscore       NUMERIC,
    totalmistakesfixed    INTEGER,
    isactive              BOOLEAN,
    registrationdate      TIMESTAMP
) AS $function$
DECLARE
    v_totalsessions      INT := 0;
    v_totalmistakesfixed INT := 0;
BEGIN
    SELECT COUNT(1)::INT INTO v_totalsessions
    FROM tblsessionmember AS sem
    WHERE sem.userid = p_userid AND sem.isdeleted = FALSE;

    SELECT COUNT(1)::INT INTO v_totalmistakesfixed
    FROM tblmistake AS mis
    WHERE mis.userid = p_userid AND mis.isresolved = TRUE AND mis.isdeleted = FALSE;

    RETURN QUERY
    SELECT
        usr.userid, usr.fullname, usr.mobilenumber, usr.email,
        usr.agegroup, usr.preferredhintlanguage, usr.avatarurl, usr.role,
        usr.dailystreakcount, usr.totalsessionsplayed,
        CASE WHEN v_totalsessions > 0 THEN v_totalsessions ELSE usr.totalsessionsplayed END,
        CAST(COALESCE(
            (SELECT AVG(CAST(va.fluencyscore AS NUMERIC(10,2)))
             FROM tblvoiceanalysis AS va
             WHERE va.userid = usr.userid AND va.isdeleted = FALSE
               AND va.recordedat >= CURRENT_DATE - INTERVAL '30 days'),
            0
        ) AS NUMERIC(5,2)),
        v_totalmistakesfixed,
        usr.isactive, usr.registrationdate
    FROM tbluser AS usr
    WHERE usr.userid    = p_userid
      AND usr.isdeleted = FALSE;
END;
$function$ LANGUAGE plpgsql;

-- 17. uspgetuserreportsummarylist
-- Input params also changed from TIMESTAMPTZ to TIMESTAMP to match tbl* column types
DROP FUNCTION IF EXISTS uspgetuserreportsummarylist(TIMESTAMP WITH TIME ZONE, TIMESTAMP WITH TIME ZONE, BIGINT, INTEGER, INTEGER);
DROP FUNCTION IF EXISTS uspgetuserreportsummarylist(TIMESTAMP, TIMESTAMP, BIGINT, INTEGER, INTEGER);
CREATE OR REPLACE FUNCTION uspgetuserreportsummarylist(
    p_fromdate   TIMESTAMP DEFAULT NULL,
    p_todate     TIMESTAMP DEFAULT NULL,
    p_userid     BIGINT    DEFAULT 0,
    p_pagenumber INTEGER   DEFAULT 1,
    p_pagesize   INTEGER   DEFAULT 10
) RETURNS TABLE(
    userid                BIGINT,
    fullname              CHARACTER VARYING,
    totalsessions         BIGINT,
    avgfluencyscore       NUMERIC,
    mostcommonmistaketype CHARACTER VARYING,
    improvementpercent    NUMERIC,
    lastsessiondate       TIMESTAMP
) AS $function$
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
$function$ LANGUAGE plpgsql;

-- 18. uspgetvoiceanalysisbysessionid
DROP FUNCTION IF EXISTS uspgetvoiceanalysisbysessionid(BIGINT);
CREATE OR REPLACE FUNCTION uspgetvoiceanalysisbysessionid(p_sessionid BIGINT)
RETURNS TABLE(
    voiceanalysisid   BIGINT,
    sessionid         BIGINT,
    userid            BIGINT,
    fullname          CHARACTER VARYING,
    turnindex         INTEGER,
    utteranceid       BIGINT,
    transcribedtext   CHARACTER VARYING,
    expectedtext      CHARACTER VARYING,
    fluencyscore      NUMERIC,
    confidencescore   NUMERIC,
    speakingspeedwpm  INTEGER,
    pausecount        INTEGER,
    hesitationwords   CHARACTER VARYING,
    repeatedwords     CHARACTER VARYING,
    grammarerrorsjson JSONB,
    pronunciationjson JSONB,
    overallscore      NUMERIC,
    recordedat        TIMESTAMP
) AS $function$
BEGIN
    RETURN QUERY
    SELECT
        va.voiceanalysisid, va.sessionid, va.userid, usr.fullname::VARCHAR(128),
        va.turnindex, va.utteranceid, va.transcribedtext, va.expectedtext,
        va.fluencyscore, va.confidencescore, va.speakingspeedwpm, va.pausecount,
        va.hesitationwords, va.repeatedwords, va.grammarerrorsjson,
        va.pronunciationjson, va.overallscore, va.recordedat
    FROM tblvoiceanalysis AS va
    INNER JOIN tbluser AS usr ON usr.userid = va.userid AND usr.isdeleted = FALSE
    WHERE va.sessionid = p_sessionid
      AND va.isdeleted = FALSE
    ORDER BY va.turnindex ASC, va.voiceanalysisid ASC;
END;
$function$ LANGUAGE plpgsql;

-- 19. uspgetvoiceanalysisbyuserid
DROP FUNCTION IF EXISTS uspgetvoiceanalysisbyuserid(BIGINT, BIGINT);
CREATE OR REPLACE FUNCTION uspgetvoiceanalysisbyuserid(
    p_userid    BIGINT,
    p_sessionid BIGINT DEFAULT 0
) RETURNS TABLE(
    voiceanalysisid   BIGINT,
    sessionid         BIGINT,
    userid            BIGINT,
    fullname          CHARACTER VARYING,
    turnindex         INTEGER,
    utteranceid       BIGINT,
    transcribedtext   CHARACTER VARYING,
    expectedtext      CHARACTER VARYING,
    fluencyscore      NUMERIC,
    confidencescore   NUMERIC,
    speakingspeedwpm  INTEGER,
    pausecount        INTEGER,
    hesitationwords   CHARACTER VARYING,
    repeatedwords     CHARACTER VARYING,
    grammarerrorsjson JSONB,
    pronunciationjson JSONB,
    overallscore      NUMERIC,
    recordedat        TIMESTAMP
) AS $function$
BEGIN
    RETURN QUERY
    SELECT
        va.voiceanalysisid, va.sessionid, va.userid, usr.fullname::VARCHAR(128),
        va.turnindex, va.utteranceid, va.transcribedtext, va.expectedtext,
        va.fluencyscore, va.confidencescore, va.speakingspeedwpm, va.pausecount,
        va.hesitationwords, va.repeatedwords, va.grammarerrorsjson,
        va.pronunciationjson, va.overallscore, va.recordedat
    FROM tblvoiceanalysis AS va
    INNER JOIN tbluser AS usr ON usr.userid = va.userid AND usr.isdeleted = FALSE
    WHERE va.userid    = p_userid
      AND va.isdeleted = FALSE
      AND (p_sessionid = 0 OR va.sessionid = p_sessionid)
    ORDER BY va.recordedat DESC, va.voiceanalysisid DESC;
END;
$function$ LANGUAGE plpgsql;

-- 20. uspinsertadminnote
DROP FUNCTION IF EXISTS uspinsertadminnote(BIGINT, BIGINT, CHARACTER VARYING, CHARACTER VARYING, CHARACTER VARYING);
CREATE OR REPLACE FUNCTION uspinsertadminnote(
    p_adminuserid  BIGINT,
    p_targetuserid BIGINT,
    p_notetext     CHARACTER VARYING,
    p_createdby    CHARACTER VARYING,
    p_ipaddress    CHARACTER VARYING
) RETURNS TABLE(
    adminnodeid BIGINT,
    adminuserid BIGINT,
    adminname   CHARACTER VARYING,
    notetext    CHARACTER VARYING,
    notedate    TIMESTAMP
) AS $function$
DECLARE
    v_id BIGINT;
BEGIN
    INSERT INTO tbladminnote (adminuserid, targetuserid, notetext, createdby, ipaddress)
    VALUES (p_adminuserid, p_targetuserid, p_notetext, p_createdby, p_ipaddress)
    RETURNING tbladminnote.adminnodeid INTO v_id;

    RETURN QUERY
    SELECT an.adminnodeid, an.adminuserid, au.fullname::VARCHAR(128), an.notetext, an.notedate
    FROM tbladminnote AS an
    INNER JOIN tbluser AS au ON au.userid = an.adminuserid
    WHERE an.adminnodeid = v_id;
END;
$function$ LANGUAGE plpgsql;
