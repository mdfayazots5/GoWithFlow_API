-- --------------------------------------------
-- Auth/user result contract fixes
-- --------------------------------------------
-- Fixes PostgreSQL return-shape drift:
--   1. tblUser.PasswordHash is VARCHAR(512) but older functions declared VARCHAR(256) → 42804
--   2. All timestamp columns in tbluser are TIMESTAMP (without time zone) but functions
--      declared TIMESTAMPTZ (with time zone) → 42804 at RETURN QUERY time.

DROP FUNCTION IF EXISTS uspgetuserbymobilenumber(VARCHAR);
DROP FUNCTION IF EXISTS uspgetuserbymobilenumber(VARCHAR(16));
CREATE OR REPLACE FUNCTION uspgetuserbymobilenumber(
    p_mobilenumber VARCHAR(16)
) RETURNS TABLE (
    userid BIGINT,
    fullname VARCHAR(128),
    mobilenumber VARCHAR(16),
    email VARCHAR(128),
    passwordhash VARCHAR(512),
    agegroup VARCHAR(32),
    preferredhintlanguage VARCHAR(32),
    avatarurl VARCHAR(256),
    groupcode VARCHAR(32),
    role VARCHAR(16),
    dailystreakcount INT,
    totalsessionsplayed INT,
    lastlogindate TIMESTAMP,
    isactive BOOLEAN,
    registrationdate TIMESTAMP,
    tag VARCHAR(64),
    comments VARCHAR(256),
    sortorder INT,
    ipaddress VARCHAR(64),
    createdby VARCHAR(128),
    datecreated TIMESTAMP,
    updatedby VARCHAR(128),
    lastupdated TIMESTAMP,
    deletedby VARCHAR(128),
    datedeleted TIMESTAMP,
    isdeleted BOOLEAN
) AS $$
BEGIN
    RETURN QUERY
    SELECT
        usr.userid,
        usr.fullname::VARCHAR(128),
        usr.mobilenumber::VARCHAR(16),
        usr.email::VARCHAR(128),
        usr.passwordhash::VARCHAR(512),
        usr.agegroup::VARCHAR(32),
        usr.preferredhintlanguage::VARCHAR(32),
        usr.avatarurl::VARCHAR(256),
        usr.groupcode::VARCHAR(32),
        usr.role::VARCHAR(16),
        usr.dailystreakcount,
        usr.totalsessionsplayed,
        usr.lastlogindate,
        usr.isactive,
        usr.registrationdate,
        usr.tag::VARCHAR(64),
        usr.comments::VARCHAR(256),
        usr.sortorder,
        usr.ipaddress::VARCHAR(64),
        usr.createdby::VARCHAR(128),
        usr.datecreated,
        usr.updatedby::VARCHAR(128),
        usr.lastupdated,
        usr.deletedby::VARCHAR(128),
        usr.datedeleted,
        usr.isdeleted
    FROM tbluser AS usr
    WHERE usr.mobilenumber = p_mobilenumber
      AND usr.isdeleted = FALSE
      AND usr.isactive = TRUE;
END;
$$ LANGUAGE plpgsql;

DROP FUNCTION IF EXISTS uspgetuserbyuserid(BIGINT);
CREATE OR REPLACE FUNCTION uspgetuserbyuserid(
    p_userid BIGINT
) RETURNS TABLE (
    userid BIGINT,
    fullname VARCHAR(128),
    mobilenumber VARCHAR(16),
    email VARCHAR(128),
    passwordhash VARCHAR(512),
    agegroup VARCHAR(32),
    preferredhintlanguage VARCHAR(32),
    avatarurl VARCHAR(256),
    groupcode VARCHAR(32),
    role VARCHAR(16),
    dailystreakcount INT,
    totalsessionsplayed INT,
    lastlogindate TIMESTAMP,
    isactive BOOLEAN,
    registrationdate TIMESTAMP,
    tag VARCHAR(64),
    comments VARCHAR(256),
    sortorder INT,
    ipaddress VARCHAR(64),
    createdby VARCHAR(128),
    datecreated TIMESTAMP,
    updatedby VARCHAR(128),
    lastupdated TIMESTAMP,
    deletedby VARCHAR(128),
    datedeleted TIMESTAMP,
    isdeleted BOOLEAN
) AS $$
BEGIN
    RETURN QUERY
    SELECT
        usr.userid,
        usr.fullname::VARCHAR(128),
        usr.mobilenumber::VARCHAR(16),
        usr.email::VARCHAR(128),
        usr.passwordhash::VARCHAR(512),
        usr.agegroup::VARCHAR(32),
        usr.preferredhintlanguage::VARCHAR(32),
        usr.avatarurl::VARCHAR(256),
        usr.groupcode::VARCHAR(32),
        usr.role::VARCHAR(16),
        usr.dailystreakcount,
        usr.totalsessionsplayed,
        usr.lastlogindate,
        usr.isactive,
        usr.registrationdate,
        usr.tag::VARCHAR(64),
        usr.comments::VARCHAR(256),
        usr.sortorder,
        usr.ipaddress::VARCHAR(64),
        usr.createdby::VARCHAR(128),
        usr.datecreated,
        usr.updatedby::VARCHAR(128),
        usr.lastupdated,
        usr.deletedby::VARCHAR(128),
        usr.datedeleted,
        usr.isdeleted
    FROM tbluser AS usr
    WHERE usr.userid = p_userid
      AND usr.isdeleted = FALSE;
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
    passwordhash VARCHAR(512),
    agegroup VARCHAR(32),
    preferredhintlanguage VARCHAR(32),
    avatarurl VARCHAR(256),
    groupcode VARCHAR(32),
    role VARCHAR(16),
    dailystreakcount INT,
    totalsessionsplayed INT,
    lastlogindate TIMESTAMP,
    isactive BOOLEAN,
    registrationdate TIMESTAMP,
    tag VARCHAR(64),
    comments VARCHAR(256),
    sortorder INT,
    ipaddress VARCHAR(64),
    createdby VARCHAR(128),
    datecreated TIMESTAMP,
    updatedby VARCHAR(128),
    lastupdated TIMESTAMP,
    deletedby VARCHAR(128),
    datedeleted TIMESTAMP,
    isdeleted BOOLEAN,
    avgfluencyscore NUMERIC(5,2),
    mostcommonmistaketype VARCHAR(32)
) AS $$
BEGIN
    RETURN QUERY
    SELECT
        usr.userid,
        usr.fullname::VARCHAR(128),
        usr.mobilenumber::VARCHAR(16),
        usr.email::VARCHAR(128),
        usr.passwordhash::VARCHAR(512),
        usr.agegroup::VARCHAR(32),
        usr.preferredhintlanguage::VARCHAR(32),
        usr.avatarurl::VARCHAR(256),
        usr.groupcode::VARCHAR(32),
        usr.role::VARCHAR(16),
        usr.dailystreakcount,
        usr.totalsessionsplayed,
        usr.lastlogindate,
        usr.isactive,
        usr.registrationdate,
        usr.tag::VARCHAR(64),
        usr.comments::VARCHAR(256),
        usr.sortorder,
        usr.ipaddress::VARCHAR(64),
        usr.createdby::VARCHAR(128),
        usr.datecreated,
        usr.updatedby::VARCHAR(128),
        usr.lastupdated,
        usr.deletedby::VARCHAR(128),
        usr.datedeleted,
        usr.isdeleted,
        CAST(COALESCE(va.avgfluencyscore, 0) AS NUMERIC(5,2)) AS avgfluencyscore,
        COALESCE(mm.mostcommonmistaketype, '')::VARCHAR(32) AS mostcommonmistaketype
    FROM tbluser AS usr
    LEFT JOIN (
        SELECT va.userid, AVG(va.fluencyscore) AS avgfluencyscore
        FROM tblvoiceanalysis AS va
        WHERE va.isdeleted = FALSE
        GROUP BY va.userid
    ) AS va ON va.userid = usr.userid
    LEFT JOIN (
        SELECT ranked.userid, ranked.mistaketype AS mostcommonmistaketype
        FROM (
            SELECT
                counted.userid,
                counted.mistaketype,
                ROW_NUMBER() OVER (
                    PARTITION BY counted.userid
                    ORDER BY counted.mistakecount DESC, counted.mistaketype ASC
                ) AS rownumber
            FROM (
                SELECT mst.userid, mst.mistaketype, COUNT(1) AS mistakecount
                FROM tblmistake AS mst
                WHERE mst.isdeleted = FALSE
                GROUP BY mst.userid, mst.mistaketype
            ) AS counted
        ) AS ranked
        WHERE ranked.rownumber = 1
    ) AS mm ON mm.userid = usr.userid
    WHERE usr.userid = p_userid
      AND usr.isdeleted = FALSE;
END;
$$ LANGUAGE plpgsql;
