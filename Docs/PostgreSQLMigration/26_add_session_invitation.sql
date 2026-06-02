-- ============================================================
-- Migration 26: Session Invitation Module
-- Adds: tblSessionInvitation, ScheduledAt on tblSession,
--        and all supporting stored procedures
-- ============================================================

-- Add ScheduledAt to tblSession
ALTER TABLE tblsession ADD COLUMN IF NOT EXISTS scheduledat TIMESTAMP WITHOUT TIME ZONE NULL;

-- tblSessionInvitation
CREATE TABLE IF NOT EXISTS tblsessioninvitation (
    invitationid    BIGSERIAL       PRIMARY KEY,
    sessionid       BIGINT          NOT NULL REFERENCES tblsession(sessionid),
    userid          BIGINT          NOT NULL REFERENCES tbluser(userid),
    slotindex       SMALLINT        NOT NULL,
    slotname        VARCHAR(64)     NOT NULL,
    status          VARCHAR(16)     NOT NULL DEFAULT 'PENDING',
    sentat          TIMESTAMP       NOT NULL DEFAULT NOW(),
    respondedat     TIMESTAMP       NULL,
    expiresat       TIMESTAMP       NULL,
    sortorder       INT             NOT NULL DEFAULT 0,
    tag             VARCHAR(64)     NULL,
    comments        VARCHAR(256)    NULL,
    ipaddress       VARCHAR(64)     NOT NULL DEFAULT '127.0.0.1',
    createdby       VARCHAR(128)    NOT NULL DEFAULT 'Admin',
    datecreated     TIMESTAMP       NOT NULL DEFAULT NOW(),
    updatedby       VARCHAR(128)    NULL,
    lastupdated     TIMESTAMP       NULL,
    deletedby       VARCHAR(128)    NULL,
    datedeleted     TIMESTAMP       NULL,
    isdeleted       BOOLEAN         NOT NULL DEFAULT FALSE
);

CREATE INDEX IF NOT EXISTS idx_tblsessioninvitation_sessionid ON tblsessioninvitation (sessionid);
CREATE INDEX IF NOT EXISTS idx_tblsessioninvitation_userid    ON tblsessioninvitation (userid);

-- uspInsertSessionInvitation
CREATE OR REPLACE FUNCTION public.uspinsertsessioninvitation(
    p_sessionid   BIGINT,
    p_userid      BIGINT,
    p_slotindex   SMALLINT,
    p_slotname    VARCHAR(64),
    p_expiresat   TIMESTAMP,
    p_createdby   VARCHAR(128),
    p_ipaddress   VARCHAR(64)
) RETURNS TABLE(invitationid BIGINT) AS $$
BEGIN
    IF EXISTS (
        SELECT 1 FROM tblsessioninvitation
        WHERE sessionid = p_sessionid AND userid = p_userid AND status = 'PENDING' AND isdeleted = FALSE
    ) THEN
        UPDATE tblsessioninvitation
        SET    slotindex   = p_slotindex,
               slotname    = p_slotname,
               expiresat   = p_expiresat,
               sentat      = NOW(),
               respondedat = NULL,
               updatedby   = p_createdby,
               lastupdated = NOW()
        WHERE  sessionid = p_sessionid AND userid = p_userid AND status = 'PENDING' AND isdeleted = FALSE;

        RETURN QUERY
        SELECT i.invitationid FROM tblsessioninvitation i
        WHERE  i.sessionid = p_sessionid AND i.userid = p_userid AND i.status = 'PENDING' AND i.isdeleted = FALSE;
    ELSE
        RETURN QUERY
        INSERT INTO tblsessioninvitation (sessionid, userid, slotindex, slotname, status, sentat, expiresat, createdby, ipaddress)
        VALUES (p_sessionid, p_userid, p_slotindex, p_slotname, 'PENDING', NOW(), p_expiresat, p_createdby, p_ipaddress)
        RETURNING tblsessioninvitation.invitationid;
    END IF;
END;
$$ LANGUAGE plpgsql;

-- uspUpdateInvitationStatus
CREATE OR REPLACE FUNCTION public.uspupdateinvitationstatus(
    p_invitationid BIGINT,
    p_userid       BIGINT,
    p_status       VARCHAR(16),
    p_updatedby    VARCHAR(128),
    p_ipaddress    VARCHAR(64)
) RETURNS VOID AS $$
BEGIN
    UPDATE tblsessioninvitation
    SET    status      = p_status,
           respondedat = NOW(),
           updatedby   = p_updatedby,
           lastupdated = NOW(),
           ipaddress   = p_ipaddress
    WHERE  invitationid = p_invitationid
      AND  userid       = p_userid
      AND  isdeleted    = FALSE;
END;
$$ LANGUAGE plpgsql;

-- uspCancelSessionInvitation
CREATE OR REPLACE FUNCTION public.uspcancelsessioninvitation(
    p_invitationid BIGINT,
    p_sessionid    BIGINT,
    p_updatedby    VARCHAR(128)
) RETURNS VOID AS $$
BEGIN
    UPDATE tblsessioninvitation
    SET    status      = 'CANCELLED',
           updatedby   = p_updatedby,
           lastupdated = NOW()
    WHERE  invitationid = p_invitationid
      AND  sessionid    = p_sessionid
      AND  isdeleted    = FALSE;
END;
$$ LANGUAGE plpgsql;

-- uspGetInvitationsBySessionId
CREATE OR REPLACE FUNCTION public.uspgetinvitationsbysessionid(p_sessionid BIGINT)
RETURNS TABLE(
    invitationid BIGINT, sessionid BIGINT, userid BIGINT,
    slotindex SMALLINT, slotname VARCHAR, status VARCHAR,
    sentat TIMESTAMP, respondedat TIMESTAMP, expiresat TIMESTAMP,
    fullname VARCHAR, avatarurl VARCHAR
) AS $$
BEGIN
    RETURN QUERY
    SELECT  inv.invitationid, inv.sessionid, inv.userid,
            inv.slotindex, inv.slotname, inv.status,
            inv.sentat, inv.respondedat, inv.expiresat,
            u.fullname, u.avatarurl
    FROM    tblsessioninvitation inv
    INNER   JOIN tbluser u ON u.userid = inv.userid AND u.isdeleted = FALSE
    WHERE   inv.sessionid = p_sessionid AND inv.isdeleted = FALSE
    ORDER   BY inv.slotindex, inv.invitationid;
END;
$$ LANGUAGE plpgsql;

-- uspGetPendingInvitationsByUserId
CREATE OR REPLACE FUNCTION public.uspgetpendinginvitationsbyuserid(p_userid BIGINT)
RETURNS TABLE(
    invitationid BIGINT, sessionid BIGINT, slotindex SMALLINT, slotname VARCHAR,
    status VARCHAR, sentat TIMESTAMP, expiresat TIMESTAMP,
    sessionname VARCHAR, sessionmode VARCHAR, sessionduration INT, scheduledat TIMESTAMP,
    hostname VARCHAR, hostavatarurl VARCHAR
) AS $$
BEGIN
    RETURN QUERY
    SELECT  inv.invitationid, inv.sessionid, inv.slotindex, inv.slotname,
            inv.status, inv.sentat, inv.expiresat,
            ses.sessionname, ses.sessionmode, ses.sessionduration, ses.scheduledat,
            host.fullname AS hostname, host.avatarurl AS hostavatarurl
    FROM    tblsessioninvitation inv
    INNER   JOIN tblsession ses  ON ses.sessionid  = inv.sessionid  AND ses.isdeleted  = FALSE
    INNER   JOIN tbluser    host ON host.userid     = ses.hostuserid AND host.isdeleted = FALSE
    WHERE   inv.userid    = p_userid
      AND   inv.isdeleted = FALSE
      AND   inv.status    = 'PENDING'
      AND   (inv.expiresat IS NULL OR inv.expiresat > NOW())
    ORDER   BY inv.sentat DESC;
END;
$$ LANGUAGE plpgsql;

-- uspSearchUsersByName
CREATE OR REPLACE FUNCTION public.uspsearchusersbyname(
    p_searchterm     VARCHAR(128),
    p_excludeuserid  BIGINT
) RETURNS TABLE(userid BIGINT, fullname VARCHAR, avatarurl VARCHAR) AS $$
BEGIN
    RETURN QUERY
    SELECT  u.userid, u.fullname, u.avatarurl
    FROM    tbluser u
    WHERE   u.isdeleted = FALSE
      AND   u.isactive  = TRUE
      AND   u.userid   <> p_excludeuserid
      AND   u.fullname  ILIKE '%' || p_searchterm || '%'
    ORDER   BY u.fullname ASC
    LIMIT   20;
END;
$$ LANGUAGE plpgsql;
