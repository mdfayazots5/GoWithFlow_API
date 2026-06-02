-- Migration 24: Add tblaudioarchive (Phase 3 Step 5)
-- Apply directly to Supabase (PostgreSQL).

CREATE TABLE IF NOT EXISTS public.tblaudioarchive (
    archiveid       BIGSERIAL       NOT NULL,
    sessionid       BIGINT          NOT NULL REFERENCES public.tblsession(sessionid),
    userid          BIGINT          NOT NULL REFERENCES public.tbluser(userid),
    turnindex       INT             NOT NULL,
    storagekey      VARCHAR(512)    NOT NULL,
    durationsecs    INT             NOT NULL DEFAULT 0,
    expiresat       TIMESTAMPTZ     NOT NULL,
    sortorder       INT             NOT NULL DEFAULT 0,
    ipaddress       VARCHAR(64)     NOT NULL DEFAULT '127.0.0.1',
    createdby       VARCHAR(128)    NOT NULL DEFAULT 'System',
    datecreated     TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    updatedby       VARCHAR(128)    NULL,
    lastupdated     TIMESTAMPTZ     NULL,
    deletedby       VARCHAR(128)    NULL,
    datedeleted     TIMESTAMPTZ     NULL,
    isdeleted       BOOLEAN         NOT NULL DEFAULT FALSE,
    CONSTRAINT pk_tblaudioarchive_archiveid PRIMARY KEY (archiveid)
);

CREATE INDEX IF NOT EXISTS idx_tblaudioarchive_session_user ON public.tblaudioarchive (sessionid, userid);
CREATE INDEX IF NOT EXISTS idx_tblaudioarchive_userid       ON public.tblaudioarchive (userid);
CREATE INDEX IF NOT EXISTS idx_tblaudioarchive_expiresat    ON public.tblaudioarchive (expiresat);

-- uspinsertaudioarchive = "uspInsertAudioArchive".toLower()
CREATE OR REPLACE FUNCTION public.uspinsertaudioarchive(
    p_sessionid    BIGINT,
    p_userid       BIGINT,
    p_turnindex    INT,
    p_storagekey   VARCHAR(512),
    p_durationsecs INT,
    p_expiresat    TIMESTAMPTZ,
    p_createdby    VARCHAR(128),
    p_ipaddress    VARCHAR(64)
) RETURNS BIGINT AS $$
DECLARE v_id BIGINT;
BEGIN
    INSERT INTO public.tblaudioarchive (sessionid, userid, turnindex, storagekey, durationsecs, expiresat, createdby, ipaddress)
    VALUES (p_sessionid, p_userid, p_turnindex, p_storagekey, p_durationsecs, p_expiresat, p_createdby, p_ipaddress)
    RETURNING archiveid INTO v_id;
    RETURN v_id;
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

-- uspgetaudioarchivebysessionanduser = "uspGetAudioArchiveBySessionAndUser".toLower()
CREATE OR REPLACE FUNCTION public.uspgetaudioarchivebysessionanduser(p_sessionid BIGINT, p_userid BIGINT)
RETURNS TABLE (
    archiveid   BIGINT,
    sessionid   BIGINT,
    userid      BIGINT,
    turnindex   INT,
    storagekey  VARCHAR,
    durationsecs INT,
    expiresat   TIMESTAMPTZ,
    datecreated TIMESTAMPTZ
) AS $$
BEGIN
    RETURN QUERY
    SELECT a.archiveid, a.sessionid, a.userid, a.turnindex, a.storagekey,
           a.durationsecs, a.expiresat, a.datecreated
    FROM   public.tblaudioarchive a
    WHERE  a.sessionid = p_sessionid AND a.userid = p_userid
      AND  a.isdeleted = FALSE AND a.expiresat >= NOW()
    ORDER  BY a.turnindex ASC;
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

-- uspdeleteaudioarchive = "uspDeleteAudioArchive".toLower()
CREATE OR REPLACE FUNCTION public.uspdeleteaudioarchive(
    p_archiveid BIGINT,
    p_userid    BIGINT,
    p_deletedby VARCHAR(128)
) RETURNS VOID AS $$
BEGIN
    UPDATE public.tblaudioarchive
    SET    isdeleted   = TRUE,
           deletedby   = p_deletedby,
           datedeleted = NOW()
    WHERE  archiveid = p_archiveid AND userid = p_userid AND isdeleted = FALSE;
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;
