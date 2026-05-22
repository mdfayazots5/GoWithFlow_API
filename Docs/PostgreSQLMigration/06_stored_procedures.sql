-- ============================================
-- File: 06_stored_procedures.sql
-- Description: All stored procedures converted from SQL Server GoWithFlowDB to PL/pgSQL
-- Run order: 6 of 10
-- Dependencies: 01_extensions.sql, 02_schema.sql, 03_constraints_indexes.sql
-- ============================================
-- Stored Procedures migrated: 79
-- Conversion rules applied:
--   GETDATE()              → NOW()
--   ISNULL(x,y)            → COALESCE(x,y)
--   LIKE '%x%'             → ILIKE '%x%'
--   IsDeleted = 0          → isdeleted = FALSE
--   TOP(n)                 → LIMIT n
--   OFFSET x ROWS FETCH NEXT y ROWS ONLY → LIMIT y OFFSET x
--   SCOPE_IDENTITY()       → RETURNING id INTO var
--   THROW 5xxxx,'msg',1    → RAISE EXCEPTION 'msg' USING ERRCODE='P0001'
--   EXEC sp(...)           → PERFORM sp(...)
--   OUTER APPLY(...)       → CROSS JOIN LATERAL(...)
--   OPENJSON(json)         → jsonb_array_elements(json::JSONB)
--   DATEDIFF(SECOND,a,b)   → EXTRACT(EPOCH FROM (b-a))::INT
--   DATEADD(MINUTE,n,dt)   → dt + (n * INTERVAL '1 minute')
--   DATEADD(DAY,n,dt)      → dt + (n * INTERVAL '1 day')
--   DATEFROMPARTS(y,m,1)   → DATE_TRUNC('month',NOW())::DATE
--   DATEADD(WEEK,DATEDIFF(WEEK,0,d),0) → DATE_TRUNC('week',d)::DATE
--   DATEPART(ISO_WEEK,d)   → EXTRACT(WEEK FROM d)::INT
--   CONVERT(NVARCHAR,d,23) → TO_CHAR(d,'YYYY-MM-DD')
--   WITH (NOLOCK)          → removed
--   SET NOCOUNT ON         → removed
--   SET XACT_ABORT ON      → removed
--   TVP UtteranceTVP       → JSONB array parameter
--   OUTPUT params          → OUT parameters on FUNCTION
--   Multiple result sets   → SETOF REFCURSOR
-- ============================================

BEGIN;

SET search_path TO public;

-- ============================================
-- BATCH 1: INSERT PROCEDURES
-- ============================================

-- --------------------------------------------
-- uspBulkInsertUtterance
-- TVP replaced with JSONB array.
-- Each element: {SequenceId,SpeakerLabel,EnglishText,HintText,GrammarTag,ContextTag,FocusWord,PronunciationNote}
-- --------------------------------------------
CREATE OR REPLACE FUNCTION uspbulkinsertutterance(
    p_scriptid    BIGINT,
    p_utterances  JSONB,
    p_createdby   VARCHAR(128),
    p_ipaddress   VARCHAR(64)
) RETURNS VOID AS $$
BEGIN
    INSERT INTO tblutterance (
        scriptid, sequenceid, speakerlabel, englishtext, hinttext,
        grammartag, contexttag, focusword, pronunciationnote, createdby, ipaddress
    )
    SELECT
        p_scriptid,
        (elem->>'SequenceId')::INT,
        elem->>'SpeakerLabel',
        elem->>'EnglishText',
        elem->>'HintText',
        elem->>'GrammarTag',
        elem->>'ContextTag',
        elem->>'FocusWord',
        elem->>'PronunciationNote',
        p_createdby,
        p_ipaddress
    FROM jsonb_array_elements(p_utterances) AS elem;
END;
$$ LANGUAGE plpgsql;

-- --------------------------------------------
-- uspInsertAdminNote
-- --------------------------------------------
CREATE OR REPLACE FUNCTION uspinsertadminnote(
    p_adminuserid  BIGINT,
    p_targetuserid BIGINT,
    p_notetext     VARCHAR(512),
    p_createdby    VARCHAR(128),
    p_ipaddress    VARCHAR(64)
) RETURNS TABLE (
    adminnodeid  BIGINT,
    adminuserid  BIGINT,
    adminname    VARCHAR(128),
    notetext     VARCHAR(512),
    notedate     TIMESTAMPTZ
) AS $$
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
$$ LANGUAGE plpgsql;

-- --------------------------------------------
-- uspInsertListenerFeedback
-- --------------------------------------------
CREATE OR REPLACE FUNCTION uspinsertlistenerfeedback(
    p_sessionid    BIGINT,
    p_turnindex    INT,
    p_fromuserid   BIGINT,
    p_targetuserid BIGINT,
    p_feedbacktag  VARCHAR(32),
    p_createdby    VARCHAR(128),
    p_ipaddress    VARCHAR(64)
) RETURNS VOID AS $$
BEGIN
    INSERT INTO tbllistenerfeedback (
        sessionid, turnindex, fromuserid, targetuserid, feedbacktag, createdby, ipaddress
    )
    VALUES (
        p_sessionid, p_turnindex, p_fromuserid, p_targetuserid, p_feedbacktag, p_createdby, p_ipaddress
    );
END;
$$ LANGUAGE plpgsql;

-- --------------------------------------------
-- uspInsertMistake
-- --------------------------------------------
CREATE OR REPLACE FUNCTION uspinsertmistake(
    p_userid        BIGINT,
    p_sessionid     BIGINT,
    p_utteranceid   BIGINT,
    p_scriptid      BIGINT,
    p_utterancetext VARCHAR(512),
    p_spokentext    VARCHAR(512) DEFAULT NULL,
    p_mistaketype   VARCHAR(32)  DEFAULT NULL,
    p_mistakedetail VARCHAR(256) DEFAULT NULL,
    p_grammartag    VARCHAR(64)  DEFAULT NULL,
    p_contexttag    VARCHAR(64)  DEFAULT NULL,
    p_correctiontext VARCHAR(512) DEFAULT NULL,
    p_createdby     VARCHAR(128) DEFAULT NULL,
    p_ipaddress     VARCHAR(64)  DEFAULT NULL
) RETURNS TABLE (mistakeid BIGINT) AS $$
BEGIN
    RETURN QUERY
    INSERT INTO tblmistake (
        userid, sessionid, utteranceid, scriptid, utterancetext, spokentext,
        mistaketype, mistakedetail, grammartag, contexttag, correctiontext,
        createdby, ipaddress
    )
    VALUES (
        p_userid, p_sessionid, p_utteranceid, p_scriptid, p_utterancetext, p_spokentext,
        p_mistaketype, p_mistakedetail, p_grammartag, p_contexttag, p_correctiontext,
        p_createdby, p_ipaddress
    )
    RETURNING tblmistake.mistakeid;
END;
$$ LANGUAGE plpgsql;

-- --------------------------------------------
-- uspInsertOtpVerification
-- --------------------------------------------
CREATE OR REPLACE FUNCTION uspinsertotpverification(
    p_mobilenumber VARCHAR(16),
    p_otpcode      VARCHAR(8),
    p_expiresat    TIMESTAMPTZ,
    p_createdby    VARCHAR(128),
    p_ipaddress    VARCHAR(64)
) RETURNS TABLE (otpverificationid BIGINT) AS $$
BEGIN
    RETURN QUERY
    INSERT INTO tblotpverification (mobilenumber, otpcode, expiresat, createdby, ipaddress)
    VALUES (p_mobilenumber, p_otpcode, p_expiresat, p_createdby, p_ipaddress)
    RETURNING tblotpverification.otpverificationid;
END;
$$ LANGUAGE plpgsql;

-- --------------------------------------------
-- uspInsertRefreshToken
-- --------------------------------------------
CREATE OR REPLACE FUNCTION uspinsertrefreshtoken(
    p_userid     BIGINT,
    p_token      VARCHAR(512),
    p_expiresat  TIMESTAMPTZ,
    p_deviceinfo VARCHAR(256) DEFAULT NULL,
    p_createdby  VARCHAR(128) DEFAULT NULL,
    p_ipaddress  VARCHAR(64)  DEFAULT NULL
) RETURNS TABLE (refreshtokenid BIGINT) AS $$
BEGIN
    RETURN QUERY
    INSERT INTO tblrefreshtoken (userid, token, expiresat, deviceinfo, createdby, ipaddress)
    VALUES (p_userid, p_token, p_expiresat, p_deviceinfo, p_createdby, p_ipaddress)
    RETURNING tblrefreshtoken.refreshtokenid;
END;
$$ LANGUAGE plpgsql;

-- --------------------------------------------
-- uspInsertRepracticeSession
-- --------------------------------------------
CREATE OR REPLACE FUNCTION uspinsertrepracticesession(
    p_userid          BIGINT,
    p_sourcesessionid BIGINT,
    p_totalmistakes   INT,
    p_createdby       VARCHAR(128),
    p_ipaddress       VARCHAR(64)
) RETURNS TABLE (repracticesessionid BIGINT) AS $$
BEGIN
    RETURN QUERY
    INSERT INTO tblrepracticesession (userid, sourcesessionid, totalmistakes, createdby, ipaddress)
    VALUES (p_userid, p_sourcesessionid, p_totalmistakes, p_createdby, p_ipaddress)
    RETURNING tblrepracticesession.repracticesessionid;
END;
$$ LANGUAGE plpgsql;

-- --------------------------------------------
-- uspInsertRepracticeUtterance
-- --------------------------------------------
CREATE OR REPLACE FUNCTION uspinsertrepracticeutterance(
    p_repracticesessionid BIGINT,
    p_mistakeid           BIGINT,
    p_originalutteranceid BIGINT,
    p_englishtext         VARCHAR(512),
    p_hinttext            VARCHAR(512) DEFAULT NULL,
    p_mistaketype         VARCHAR(32)  DEFAULT NULL,
    p_mistakedetail       VARCHAR(256) DEFAULT NULL,
    p_correctionnote      VARCHAR(512) DEFAULT NULL,
    p_createdby           VARCHAR(128) DEFAULT NULL,
    p_ipaddress           VARCHAR(64)  DEFAULT NULL
) RETURNS TABLE (repracticeutteranceid BIGINT) AS $$
BEGIN
    RETURN QUERY
    INSERT INTO tblrepracticeutterance (
        repracticesessionid, mistakeid, originalutteranceid, englishtext, hinttext,
        mistaketype, mistakedetail, correctionnote, createdby, ipaddress
    )
    VALUES (
        p_repracticesessionid, p_mistakeid, p_originalutteranceid, p_englishtext, p_hinttext,
        p_mistaketype, p_mistakedetail, p_correctionnote, p_createdby, p_ipaddress
    )
    RETURNING tblrepracticeutterance.repracticeutteranceid;
END;
$$ LANGUAGE plpgsql;

-- --------------------------------------------
-- uspInsertScript
-- --------------------------------------------
CREATE OR REPLACE FUNCTION uspinsertscript(
    p_scripttitle      VARCHAR(128),
    p_category         VARCHAR(64),
    p_grammarfocustag  VARCHAR(64),
    p_contexttag       VARCHAR(64),
    p_complexitylevel  SMALLINT,
    p_targetagegroup   VARCHAR(32),
    p_hintlanguage     VARCHAR(32),
    p_isactive         BOOLEAN     DEFAULT TRUE,
    p_uploadedbyuserid BIGINT      DEFAULT NULL,
    p_version          INT         DEFAULT 1,
    p_createdby        VARCHAR(128) DEFAULT NULL,
    p_ipaddress        VARCHAR(64)  DEFAULT NULL
) RETURNS TABLE (scriptid BIGINT) AS $$
BEGIN
    RETURN QUERY
    INSERT INTO tblscript (
        scripttitle, category, grammarfocustag, contexttag, complexitylevel,
        targetagegroup, hintlanguage, isactive, uploadedbyuserid, version,
        createdby, ipaddress
    )
    VALUES (
        p_scripttitle, p_category, p_grammarfocustag, p_contexttag, p_complexitylevel,
        p_targetagegroup, p_hintlanguage, p_isactive, p_uploadedbyuserid, p_version,
        p_createdby, p_ipaddress
    )
    RETURNING tblscript.scriptid;
END;
$$ LANGUAGE plpgsql;

-- --------------------------------------------
-- uspInsertScriptVersion
-- --------------------------------------------
CREATE OR REPLACE FUNCTION uspinsertscriptversion(
    p_scriptid         BIGINT,
    p_versionnumber    INT,
    p_versionnotes     VARCHAR(256) DEFAULT NULL,
    p_uploadedbyuserid BIGINT       DEFAULT NULL,
    p_createdby        VARCHAR(128) DEFAULT NULL,
    p_ipaddress        VARCHAR(64)  DEFAULT NULL
) RETURNS TABLE (scriptversionid BIGINT) AS $$
BEGIN
    RETURN QUERY
    INSERT INTO tblscriptversion (
        scriptid, versionnumber, versionnotes, uploadedbyuserid, createdby, ipaddress
    )
    VALUES (
        p_scriptid, p_versionnumber, p_versionnotes, p_uploadedbyuserid, p_createdby, p_ipaddress
    )
    RETURNING tblscriptversion.scriptversionid;
END;
$$ LANGUAGE plpgsql;

-- --------------------------------------------
-- uspInsertSession
-- Generates a unique 6-char alphanumeric JoinCode then inserts.
-- SQL Server OUTPUT params → PostgreSQL OUT params.
-- --------------------------------------------
CREATE OR REPLACE FUNCTION uspinsertsession(
    p_sessionname       VARCHAR(128),
    p_sessionmode       VARCHAR(64),
    p_maxmembers        SMALLINT,
    p_sessionduration   INT,
    p_hostuserid        BIGINT,
    p_scriptid          BIGINT,
    p_roomexpiryminutes INT,
    p_createdby         VARCHAR(128),
    p_ipaddress         VARCHAR(64),
    OUT p_sessionid     BIGINT,
    OUT p_joincode      VARCHAR(8)
) RETURNS RECORD AS $$
DECLARE
    v_charset TEXT := 'ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789';
    v_attempt INT  := 0;
    v_i       INT;
BEGIN
    p_sessionid := 0;
    p_joincode  := '';

    LOOP
        v_attempt  := v_attempt + 1;
        p_joincode := '';

        FOR v_i IN 1..6 LOOP
            p_joincode := p_joincode || SUBSTRING(v_charset, (FLOOR(RANDOM() * 36))::INT + 1, 1);
        END LOOP;

        EXIT WHEN NOT EXISTS (
            SELECT 1 FROM tblsession WHERE joincode = p_joincode AND isdeleted = FALSE
        );

        IF v_attempt >= 50 THEN
            RAISE EXCEPTION 'Unable to generate a unique JoinCode.' USING ERRCODE = 'P0001';
        END IF;
    END LOOP;

    INSERT INTO tblsession (
        sessionname, joincode, sessionmode, maxmembers, sessionduration,
        hostuserid, scriptid, status, roomexpiryminutes, roomexpiresat,
        createdby, ipaddress
    )
    VALUES (
        p_sessionname, p_joincode, p_sessionmode, p_maxmembers, p_sessionduration,
        p_hostuserid, p_scriptid, 'LOBBY', p_roomexpiryminutes,
        NOW() + (p_roomexpiryminutes * INTERVAL '1 minute'),
        p_createdby, p_ipaddress
    )
    RETURNING tblsession.sessionid INTO p_sessionid;
END;
$$ LANGUAGE plpgsql;

-- --------------------------------------------
-- uspInsertSessionMember
-- --------------------------------------------
CREATE OR REPLACE FUNCTION uspinsertsessionmember(
    p_sessionid BIGINT,
    p_userid    BIGINT,
    p_slotindex SMALLINT,
    p_slotname  VARCHAR(64),
    p_ishost    BOOLEAN,
    p_createdby VARCHAR(128),
    p_ipaddress VARCHAR(64)
) RETURNS TABLE (sessionmemberid BIGINT) AS $$
BEGIN
    IF EXISTS (
        SELECT 1 FROM tblsessionmember
        WHERE sessionid = p_sessionid AND slotindex = p_slotindex
          AND isdeleted = FALSE AND isactive = TRUE
    ) THEN
        RAISE EXCEPTION 'The requested session slot is already occupied.' USING ERRCODE = 'P0001';
    END IF;

    IF EXISTS (
        SELECT 1 FROM tblsessionmember
        WHERE sessionid = p_sessionid AND userid = p_userid
          AND isdeleted = FALSE AND isactive = TRUE
    ) THEN
        RAISE EXCEPTION 'The user is already an active member of this session.' USING ERRCODE = 'P0001';
    END IF;

    RETURN QUERY
    INSERT INTO tblsessionmember (
        sessionid, userid, slotindex, slotname, isready, ishost,
        joinedat, isactive, createdby, ipaddress
    )
    VALUES (
        p_sessionid, p_userid, p_slotindex, p_slotname,
        CASE WHEN p_ishost THEN TRUE ELSE FALSE END,
        p_ishost, NOW(), TRUE, p_createdby, p_ipaddress
    )
    RETURNING tblsessionmember.sessionmemberid;
END;
$$ LANGUAGE plpgsql;

-- --------------------------------------------
-- uspInsertTurnState
-- --------------------------------------------
CREATE OR REPLACE FUNCTION uspinsertturnstate(
    p_sessionid      BIGINT,
    p_turnindex      INT,
    p_totalturns     INT,
    p_activememberid BIGINT,
    p_activeslotindex SMALLINT,
    p_utteranceid    BIGINT,
    p_maxrereads     INT,
    p_createdby      VARCHAR(128),
    p_ipaddress      VARCHAR(64)
) RETURNS TABLE (turnstateid BIGINT) AS $$
BEGIN
    RETURN QUERY
    INSERT INTO tblturnstate (
        sessionid, turnindex, totalturns, activememberid, activeslotindex,
        utteranceid, maxrereads, turnstatus, turnstartedat, createdby, ipaddress
    )
    VALUES (
        p_sessionid, p_turnindex, p_totalturns, p_activememberid, p_activeslotindex,
        p_utteranceid, p_maxrereads, 'ACTIVE', NOW(), p_createdby, p_ipaddress
    )
    RETURNING tblturnstate.turnstateid;
END;
$$ LANGUAGE plpgsql;

-- --------------------------------------------
-- uspInsertUser
-- --------------------------------------------
CREATE OR REPLACE FUNCTION uspinsertuser(
    p_fullname              VARCHAR(128),
    p_mobilenumber          VARCHAR(16),
    p_email                 VARCHAR(128) DEFAULT NULL,
    p_agegroup              VARCHAR(32)  DEFAULT NULL,
    p_preferredhintlanguage VARCHAR(32)  DEFAULT NULL,
    p_avatarurl             VARCHAR(256) DEFAULT NULL,
    p_groupcode             VARCHAR(32)  DEFAULT NULL,
    p_role                  VARCHAR(16)  DEFAULT 'USER',
    p_createdby             VARCHAR(128) DEFAULT NULL,
    p_ipaddress             VARCHAR(64)  DEFAULT NULL
) RETURNS TABLE (userid BIGINT) AS $$
BEGIN
    IF EXISTS (
        SELECT 1 FROM tbluser WHERE mobilenumber = p_mobilenumber AND isdeleted = FALSE
    ) THEN
        RAISE EXCEPTION 'Mobile number is already registered.' USING ERRCODE = 'P0001';
    END IF;

    RETURN QUERY
    INSERT INTO tbluser (
        fullname, mobilenumber, email, passwordhash, agegroup,
        preferredhintlanguage, avatarurl, groupcode, role,
        createdby, ipaddress
    )
    VALUES (
        p_fullname, p_mobilenumber, p_email, NULL, p_agegroup,
        p_preferredhintlanguage, p_avatarurl, p_groupcode, p_role,
        p_createdby, p_ipaddress
    )
    RETURNING tbluser.userid;
END;
$$ LANGUAGE plpgsql;

-- --------------------------------------------
-- uspInsertUserBadge
-- Inserts only if badge not already earned.
-- --------------------------------------------
CREATE OR REPLACE FUNCTION uspinsertuserbadge(
    p_userid    BIGINT,
    p_badgecode VARCHAR(64),
    p_badgename VARCHAR(128),
    p_createdby VARCHAR(128),
    p_ipaddress VARCHAR(64)
) RETURNS VOID AS $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM tbluserbadge
        WHERE userid = p_userid AND badgecode = p_badgecode AND isdeleted = FALSE
    ) THEN
        INSERT INTO tbluserbadge (userid, badgecode, badgename, createdby, ipaddress)
        VALUES (p_userid, p_badgecode, p_badgename, p_createdby, p_ipaddress);
    END IF;
END;
$$ LANGUAGE plpgsql;

-- --------------------------------------------
-- uspInsertUtterance
-- --------------------------------------------
CREATE OR REPLACE FUNCTION uspinsertutterance(
    p_scriptid         BIGINT,
    p_sequenceid       INT,
    p_speakerlabel     VARCHAR(64),
    p_englishtext      VARCHAR(512),
    p_hinttext         VARCHAR(512) DEFAULT NULL,
    p_grammartag       VARCHAR(64)  DEFAULT NULL,
    p_contexttag       VARCHAR(64)  DEFAULT NULL,
    p_focusword        VARCHAR(64)  DEFAULT NULL,
    p_pronunciationnote VARCHAR(256) DEFAULT NULL,
    p_createdby        VARCHAR(128) DEFAULT NULL,
    p_ipaddress        VARCHAR(64)  DEFAULT NULL
) RETURNS VOID AS $$
BEGIN
    INSERT INTO tblutterance (
        scriptid, sequenceid, speakerlabel, englishtext, hinttext,
        grammartag, contexttag, focusword, pronunciationnote, createdby, ipaddress
    )
    VALUES (
        p_scriptid, p_sequenceid, p_speakerlabel, p_englishtext, p_hinttext,
        p_grammartag, p_contexttag, p_focusword, p_pronunciationnote, p_createdby, p_ipaddress
    );
END;
$$ LANGUAGE plpgsql;

-- --------------------------------------------
-- uspInsertVoiceAnalysis
-- --------------------------------------------
CREATE OR REPLACE FUNCTION uspinsertvoiceanalysis(
    p_sessionid         BIGINT,
    p_userid            BIGINT,
    p_turnindex         INT,
    p_utteranceid       BIGINT,
    p_transcribedtext   VARCHAR(512) DEFAULT NULL,
    p_expectedtext      VARCHAR(512) DEFAULT NULL,
    p_fluencyscore      NUMERIC(5,2) DEFAULT NULL,
    p_confidencescore   NUMERIC(5,2) DEFAULT NULL,
    p_speakingspeedwpm  INT          DEFAULT NULL,
    p_pausecount        INT          DEFAULT NULL,
    p_hesitationwords   VARCHAR(256) DEFAULT NULL,
    p_repeatedwords     VARCHAR(256) DEFAULT NULL,
    p_grammarerrorsjson JSONB        DEFAULT NULL,
    p_pronunciationjson JSONB        DEFAULT NULL,
    p_overallscore      NUMERIC(5,2) DEFAULT NULL,
    p_createdby         VARCHAR(128) DEFAULT NULL,
    p_ipaddress         VARCHAR(64)  DEFAULT NULL
) RETURNS TABLE (voiceanalysisid BIGINT) AS $$
BEGIN
    RETURN QUERY
    INSERT INTO tblvoiceanalysis (
        sessionid, userid, turnindex, utteranceid, transcribedtext, expectedtext,
        fluencyscore, confidencescore, speakingspeedwpm, pausecount,
        hesitationwords, repeatedwords, grammarerrorsjson, pronunciationjson,
        overallscore, createdby, ipaddress
    )
    VALUES (
        p_sessionid, p_userid, p_turnindex, p_utteranceid, p_transcribedtext, p_expectedtext,
        p_fluencyscore, p_confidencescore, p_speakingspeedwpm, p_pausecount,
        p_hesitationwords, p_repeatedwords, p_grammarerrorsjson, p_pronunciationjson,
        p_overallscore, p_createdby, p_ipaddress
    )
    RETURNING tblvoiceanalysis.voiceanalysisid;
END;
$$ LANGUAGE plpgsql;

-- ============================================
-- BATCH 2: UPDATE / DELETE / REVOKE PROCEDURES
-- ============================================

-- --------------------------------------------
-- uspRevokeRefreshToken
-- --------------------------------------------
CREATE OR REPLACE FUNCTION usprovkerefreshtoken(
    p_token     VARCHAR(512),
    p_updatedby VARCHAR(128),
    p_ipaddress VARCHAR(64)
) RETURNS VOID AS $$
BEGIN
    UPDATE tblrefreshtoken
    SET isrevoked   = TRUE,
        revokedat   = NOW(),
        updatedby   = p_updatedby,
        lastupdated = NOW(),
        ipaddress   = p_ipaddress
    WHERE token     = p_token
      AND isrevoked = FALSE
      AND isdeleted = FALSE;
END;
$$ LANGUAGE plpgsql;

-- --------------------------------------------
-- uspSoftDeleteScriptByScriptId
-- --------------------------------------------
CREATE OR REPLACE FUNCTION uspsoftdeletescriptbyscriptid(
    p_scriptid  BIGINT,
    p_deletedby VARCHAR(128),
    p_ipaddress VARCHAR(64)
) RETURNS VOID AS $$
BEGIN
    UPDATE tblscript
    SET isdeleted   = TRUE,
        datedeleted = NOW(),
        deletedby   = p_deletedby,
        updatedby   = p_deletedby,
        lastupdated = NOW(),
        ipaddress   = p_ipaddress
    WHERE scriptid  = p_scriptid
      AND isdeleted = FALSE;

    UPDATE tblutterance
    SET isdeleted   = TRUE,
        datedeleted = NOW(),
        deletedby   = p_deletedby,
        updatedby   = p_deletedby,
        lastupdated = NOW(),
        ipaddress   = p_ipaddress
    WHERE scriptid  = p_scriptid
      AND isdeleted = FALSE;
END;
$$ LANGUAGE plpgsql;

-- --------------------------------------------
-- uspSoftDeleteUser
-- --------------------------------------------
CREATE OR REPLACE FUNCTION uspsoftdeleteuser(
    p_userid    BIGINT,
    p_deletedby VARCHAR(128),
    p_ipaddress VARCHAR(64)
) RETURNS VOID AS $$
BEGIN
    UPDATE tbluser
    SET isdeleted   = TRUE,
        datedeleted = NOW(),
        deletedby   = p_deletedby,
        updatedby   = p_deletedby,
        lastupdated = NOW(),
        ipaddress   = p_ipaddress
    WHERE userid    = p_userid
      AND isdeleted = FALSE;
END;
$$ LANGUAGE plpgsql;

-- --------------------------------------------
-- uspUpdateRepracticeSessionStatus
-- --------------------------------------------
CREATE OR REPLACE FUNCTION uspupdaterepracticesessionstatus(
    p_repracticesessionid BIGINT,
    p_status              VARCHAR(16),
    p_improvementpercent  NUMERIC(5,2),
    p_updatedby           VARCHAR(128),
    p_ipaddress           VARCHAR(64)
) RETURNS VOID AS $$
BEGIN
    UPDATE tblrepracticesession
    SET status             = p_status,
        improvementpercent = p_improvementpercent,
        completedrounds    = CASE WHEN p_status = 'COMPLETED' THEN completedrounds + 1 ELSE completedrounds END,
        updatedby          = p_updatedby,
        lastupdated        = NOW(),
        ipaddress          = p_ipaddress
    WHERE repracticesessionid = p_repracticesessionid
      AND isdeleted = FALSE;
END;
$$ LANGUAGE plpgsql;

-- --------------------------------------------
-- uspUpdateRepracticeUtteranceAttempt
-- Resolves utterance and linked mistake after two consecutive scores > 80.
-- --------------------------------------------
CREATE OR REPLACE FUNCTION uspupdaterepracticeutteranceattempt(
    p_repracticeutteranceid BIGINT,
    p_score                 NUMERIC(5,2),
    p_updatedby             VARCHAR(128),
    p_ipaddress             VARCHAR(64)
) RETURNS VOID AS $$
DECLARE
    v_mistakeid        BIGINT          := 0;
    v_previouslastscore NUMERIC(5,2)  := 0;
    v_shouldresolve    BOOLEAN         := FALSE;
BEGIN
    SELECT rpu.mistakeid, rpu.lastscore
    INTO v_mistakeid, v_previouslastscore
    FROM tblrepracticeutterance AS rpu
    WHERE rpu.repracticeutteranceid = p_repracticeutteranceid
      AND rpu.isdeleted = FALSE;

    IF v_mistakeid IS NULL OR v_mistakeid = 0 THEN
        RAISE EXCEPTION 'Repractice utterance was not found.' USING ERRCODE = 'P0001';
    END IF;

    v_shouldresolve := (p_score > 80 AND COALESCE(v_previouslastscore, 0) > 80);

    UPDATE tblrepracticeutterance
    SET attemptcount = attemptcount + 1,
        lastscore    = p_score,
        bestscore    = CASE WHEN p_score > bestscore THEN p_score ELSE bestscore END,
        isresolved   = CASE WHEN v_shouldresolve THEN TRUE ELSE isresolved END,
        updatedby    = p_updatedby,
        lastupdated  = NOW(),
        ipaddress    = p_ipaddress
    WHERE repracticeutteranceid = p_repracticeutteranceid
      AND isdeleted = FALSE;

    UPDATE tblmistake
    SET practicecount = practicecount + 1,
        isresolved    = CASE WHEN v_shouldresolve THEN TRUE ELSE isresolved END,
        lastattempt   = NOW(),
        updatedby     = p_updatedby,
        lastupdated   = NOW(),
        ipaddress     = p_ipaddress
    WHERE mistakeid   = v_mistakeid
      AND isdeleted   = FALSE;
END;
$$ LANGUAGE plpgsql;

-- --------------------------------------------
-- uspUpdateScriptActiveStatusByScriptId
-- --------------------------------------------
CREATE OR REPLACE FUNCTION uspupdatescriptactivestatusbyscriptid(
    p_scriptid  BIGINT,
    p_isactive  BOOLEAN,
    p_updatedby VARCHAR(128),
    p_ipaddress VARCHAR(64)
) RETURNS VOID AS $$
BEGIN
    UPDATE tblscript
    SET isactive    = p_isactive,
        updatedby   = p_updatedby,
        lastupdated = NOW(),
        ipaddress   = p_ipaddress
    WHERE scriptid  = p_scriptid
      AND isdeleted = FALSE;
END;
$$ LANGUAGE plpgsql;

-- --------------------------------------------
-- uspUpdateScriptUtteranceCount
-- --------------------------------------------
CREATE OR REPLACE FUNCTION uspupdatescriptutterancecount(
    p_scriptid  BIGINT,
    p_updatedby VARCHAR(128),
    p_ipaddress VARCHAR(64)
) RETURNS VOID AS $$
BEGIN
    UPDATE tblscript
    SET utterancecount = (
            SELECT COUNT(1) FROM tblutterance AS ut
            WHERE ut.scriptid = p_scriptid AND ut.isdeleted = FALSE
        ),
        updatedby   = p_updatedby,
        lastupdated = NOW(),
        ipaddress   = p_ipaddress
    WHERE scriptid  = p_scriptid
      AND isdeleted = FALSE;
END;
$$ LANGUAGE plpgsql;

-- --------------------------------------------
-- uspUpdateSessionStatus
-- Sets StartedDate on ACTIVE, EndedDate and ActualDurationSec on COMPLETED/ABANDONED.
-- --------------------------------------------
CREATE OR REPLACE FUNCTION uspupdatesessionstatus(
    p_sessionid BIGINT,
    p_status    VARCHAR(16),
    p_updatedby VARCHAR(128),
    p_ipaddress VARCHAR(64)
) RETURNS VOID AS $$
BEGIN
    UPDATE tblsession
    SET status     = p_status,
        starteddate = CASE
            WHEN p_status = 'ACTIVE' AND starteddate IS NULL THEN NOW()
            ELSE starteddate
        END,
        endeddate = CASE
            WHEN p_status IN ('COMPLETED', 'ABANDONED') THEN NOW()
            ELSE endeddate
        END,
        actualdurationsec = CASE
            WHEN p_status IN ('COMPLETED', 'ABANDONED') AND starteddate IS NOT NULL
                THEN EXTRACT(EPOCH FROM (NOW() - starteddate))::INT
            ELSE actualdurationsec
        END,
        updatedby   = p_updatedby,
        lastupdated = NOW(),
        ipaddress   = p_ipaddress
    WHERE sessionid = p_sessionid
      AND isdeleted = FALSE;
END;
$$ LANGUAGE plpgsql;

-- --------------------------------------------
-- uspUpdateSessionMemberLeft
-- Marks member inactive. Abandons session if host leaves or no members remain.
-- --------------------------------------------
CREATE OR REPLACE FUNCTION uspupdatesessionmemberleft(
    p_sessionid BIGINT,
    p_userid    BIGINT,
    p_updatedby VARCHAR(128),
    p_ipaddress VARCHAR(64)
) RETURNS VOID AS $$
DECLARE
    v_hostuserid        BIGINT := 0;
    v_activemembercount INT    := 0;
BEGIN
    UPDATE tblsessionmember
    SET leftat      = NOW(),
        isactive    = FALSE,
        isready     = FALSE,
        updatedby   = p_updatedby,
        lastupdated = NOW(),
        ipaddress   = p_ipaddress
    WHERE sessionid = p_sessionid
      AND userid    = p_userid
      AND isdeleted = FALSE
      AND isactive  = TRUE;

    SELECT ses.hostuserid INTO v_hostuserid
    FROM tblsession AS ses
    WHERE ses.sessionid = p_sessionid AND ses.isdeleted = FALSE;

    SELECT COUNT(1) INTO v_activemembercount
    FROM tblsessionmember AS sem
    WHERE sem.sessionid = p_sessionid AND sem.isdeleted = FALSE AND sem.isactive = TRUE;

    IF v_hostuserid = p_userid OR v_activemembercount = 0 THEN
        PERFORM uspupdatesessionstatus(p_sessionid, 'ABANDONED', p_updatedby, p_ipaddress);
    END IF;
END;
$$ LANGUAGE plpgsql;

-- --------------------------------------------
-- uspUpdateSessionMemberReadyStatus
-- --------------------------------------------
CREATE OR REPLACE FUNCTION uspupdatesessionmemberreadystatus(
    p_sessionid BIGINT,
    p_userid    BIGINT,
    p_isready   BOOLEAN,
    p_updatedby VARCHAR(128),
    p_ipaddress VARCHAR(64)
) RETURNS VOID AS $$
BEGIN
    UPDATE tblsessionmember
    SET isready     = p_isready,
        updatedby   = p_updatedby,
        lastupdated = NOW(),
        ipaddress   = p_ipaddress
    WHERE sessionid = p_sessionid
      AND userid    = p_userid
      AND isdeleted = FALSE
      AND isactive  = TRUE;
END;
$$ LANGUAGE plpgsql;

-- --------------------------------------------
-- uspUpdateTurnStatusByTurnStateId
-- --------------------------------------------
CREATE OR REPLACE FUNCTION uspupdateturnstatusbyturnstateid(
    p_turnstateid BIGINT,
    p_turnstatus  VARCHAR(16),
    p_updatedby   VARCHAR(128),
    p_ipaddress   VARCHAR(64)
) RETURNS VOID AS $$
BEGIN
    UPDATE tblturnstate
    SET turnstatus      = p_turnstatus,
        turncompletedat = NOW(),
        updatedby       = p_updatedby,
        lastupdated     = NOW(),
        ipaddress       = p_ipaddress
    WHERE turnstateid   = p_turnstateid
      AND isdeleted     = FALSE;
END;
$$ LANGUAGE plpgsql;

-- --------------------------------------------
-- uspUpdateUserActiveStatusByUserId
-- --------------------------------------------
CREATE OR REPLACE FUNCTION uspupdateuseractivestatusbyuserid(
    p_userid    BIGINT,
    p_isactive  BOOLEAN,
    p_updatedby VARCHAR(128),
    p_ipaddress VARCHAR(64)
) RETURNS VOID AS $$
BEGIN
    UPDATE tbluser
    SET isactive    = p_isactive,
        updatedby   = p_updatedby,
        lastupdated = NOW(),
        ipaddress   = p_ipaddress
    WHERE userid    = p_userid
      AND isdeleted = FALSE;
END;
$$ LANGUAGE plpgsql;

-- --------------------------------------------
-- uspUpdateUserLastLogin
-- --------------------------------------------
CREATE OR REPLACE FUNCTION uspupdateuserlastlogin(
    p_userid    BIGINT,
    p_updatedby VARCHAR(128),
    p_ipaddress VARCHAR(64)
) RETURNS VOID AS $$
BEGIN
    UPDATE tbluser
    SET lastlogindate = NOW(),
        updatedby     = p_updatedby,
        lastupdated   = NOW(),
        ipaddress     = p_ipaddress
    WHERE userid      = p_userid
      AND isdeleted   = FALSE;
END;
$$ LANGUAGE plpgsql;

-- --------------------------------------------
-- uspUpdateUserProfile
-- --------------------------------------------
CREATE OR REPLACE FUNCTION uspupdateuserprofile(
    p_userid                BIGINT,
    p_fullname              VARCHAR(128),
    p_email                 VARCHAR(128) DEFAULT NULL,
    p_agegroup              VARCHAR(32)  DEFAULT NULL,
    p_preferredhintlanguage VARCHAR(32)  DEFAULT NULL,
    p_avatarurl             VARCHAR(256) DEFAULT NULL,
    p_updatedby             VARCHAR(128) DEFAULT NULL,
    p_ipaddress             VARCHAR(64)  DEFAULT NULL
) RETURNS VOID AS $$
BEGIN
    UPDATE tbluser
    SET fullname              = p_fullname,
        email                 = p_email,
        agegroup              = p_agegroup,
        preferredhintlanguage = p_preferredhintlanguage,
        avatarurl             = p_avatarurl,
        updatedby             = p_updatedby,
        lastupdated           = NOW(),
        ipaddress             = p_ipaddress
    WHERE userid    = p_userid
      AND isdeleted = FALSE;
END;
$$ LANGUAGE plpgsql;

-- --------------------------------------------
-- uspIncrementReReadCount
-- --------------------------------------------
CREATE OR REPLACE FUNCTION uspincrementrereadcount(
    p_turnstateid BIGINT,
    p_updatedby   VARCHAR(128),
    p_ipaddress   VARCHAR(64)
) RETURNS VOID AS $$
BEGIN
    UPDATE tblturnstate
    SET rereadcount  = rereadcount + 1,
        rereadallowed = CASE
            WHEN rereadcount + 1 >= maxrereads THEN FALSE
            ELSE TRUE
        END,
        updatedby    = p_updatedby,
        lastupdated  = NOW(),
        ipaddress    = p_ipaddress
    WHERE turnstateid = p_turnstateid
      AND isdeleted   = FALSE
      AND rereadcount < maxrereads;
END;
$$ LANGUAGE plpgsql;

-- --------------------------------------------
-- uspUpsertUserStreak
-- Insert today's streak row or increment if exists. Updates tbluser streak counts.
-- --------------------------------------------
CREATE OR REPLACE FUNCTION uspupsertuserstreak(
    p_userid          BIGINT,
    p_practiceminutes INT,
    p_updatedby       VARCHAR(128),
    p_ipaddress       VARCHAR(64)
) RETURNS VOID AS $$
DECLARE
    v_today          DATE := CURRENT_DATE;
    v_laststreakdate DATE;
BEGIN
    SELECT MAX(usrs.streakdate) INTO v_laststreakdate
    FROM tbluserstreak AS usrs
    WHERE usrs.userid = p_userid AND usrs.isdeleted = FALSE;

    IF EXISTS (
        SELECT 1 FROM tbluserstreak
        WHERE userid = p_userid AND streakdate = v_today AND isdeleted = FALSE
    ) THEN
        UPDATE tbluserstreak
        SET sessioncount    = sessioncount + 1,
            practiceminutes = practiceminutes + p_practiceminutes,
            updatedby       = p_updatedby,
            lastupdated     = NOW(),
            ipaddress       = p_ipaddress
        WHERE userid      = p_userid
          AND streakdate  = v_today
          AND isdeleted   = FALSE;
    ELSE
        INSERT INTO tbluserstreak (userid, streakdate, sessioncount, practiceminutes, createdby, ipaddress)
        VALUES (p_userid, v_today, 1, p_practiceminutes, p_updatedby, p_ipaddress);
    END IF;

    UPDATE tbluser
    SET dailystreakcount = CASE
            WHEN v_laststreakdate IS NULL                        THEN 1
            WHEN v_laststreakdate = v_today                      THEN dailystreakcount
            WHEN v_laststreakdate = v_today - INTERVAL '1 day'  THEN dailystreakcount + 1
            ELSE 1
        END,
        totalsessionsplayed = totalsessionsplayed + 1,
        updatedby           = p_updatedby,
        lastupdated         = NOW(),
        ipaddress           = p_ipaddress
    WHERE userid    = p_userid
      AND isdeleted = FALSE;
END;
$$ LANGUAGE plpgsql;

-- ============================================
-- BATCH 3: SIMPLE SELECT PROCEDURES (SINGLE RESULT SET)
-- ============================================

-- --------------------------------------------
-- uspCalculateImprovementPercentByUserId
-- --------------------------------------------
CREATE OR REPLACE FUNCTION uspcalculateimprovementpercentbyuserid(
    p_userid BIGINT
) RETURNS TABLE (improvementpercent NUMERIC(5,2)) AS $$
BEGIN
    RETURN QUERY
    SELECT CAST(
        CASE
            WHEN COUNT(1) = 0 THEN 0
            ELSE (SUM(CASE WHEN mst.isresolved = TRUE THEN 1 ELSE 0 END) * 100.0) / COUNT(1)
        END AS NUMERIC(5,2)
    ) AS improvementpercent
    FROM tblmistake AS mst
    WHERE mst.userid    = p_userid
      AND mst.isdeleted = FALSE;
END;
$$ LANGUAGE plpgsql;

-- --------------------------------------------
-- uspCheckScriptTitleExists
-- --------------------------------------------
CREATE OR REPLACE FUNCTION uspcheckscripttitleexists(
    p_scripttitle VARCHAR(128)
) RETURNS TABLE (scriptid BIGINT) AS $$
BEGIN
    RETURN QUERY
    SELECT scr.scriptid
    FROM tblscript AS scr
    WHERE scr.scripttitle = p_scripttitle
      AND scr.isdeleted   = FALSE
    ORDER BY scr.version DESC, scr.scriptid DESC
    LIMIT 1;
END;
$$ LANGUAGE plpgsql;

-- --------------------------------------------
-- uspGetAdminAnalyticsOverview
-- DATEFROMPARTS(YEAR(GETDATE()),MONTH(GETDATE()),1) → DATE_TRUNC('month',NOW())::DATE
-- --------------------------------------------
CREATE OR REPLACE FUNCTION uspgetadminanalyticsoverview()
RETURNS TABLE (
    totalsessionsthismonth           INT,
    avgfluencyscoreallusers          NUMERIC(5,2),
    mostimproveduserid               BIGINT,
    mostimprovedusername             VARCHAR(128),
    mostimprovedresolvedmistakecount INT,
    moststruggeledgrammartag         VARCHAR(64)
) AS $$
BEGIN
    RETURN QUERY
    WITH weeklyresolvedmistake AS (
        SELECT mst.userid, COUNT(1)::INT AS resolvedmistakecount
        FROM tblmistake AS mst
        WHERE mst.isdeleted  = FALSE
          AND mst.isresolved = TRUE
          AND mst.lastupdated >= CURRENT_DATE - INTERVAL '7 days'
        GROUP BY mst.userid
    ),
    topimproveduser AS (
        SELECT wrm.userid, usr.fullname, wrm.resolvedmistakecount
        FROM weeklyresolvedmistake AS wrm
        INNER JOIN tbluser AS usr ON usr.userid = wrm.userid AND usr.isdeleted = FALSE
        ORDER BY wrm.resolvedmistakecount DESC, usr.fullname ASC
        LIMIT 1
    )
    SELECT
        (SELECT COUNT(1)::INT FROM tblsession AS ses
         WHERE ses.isdeleted = FALSE
           AND COALESCE(ses.endeddate, COALESCE(ses.starteddate, ses.datecreated)) >= DATE_TRUNC('month', NOW())::DATE
        ) AS totalsessionsthismonth,
        (SELECT CAST(COALESCE(AVG(CAST(va.fluencyscore AS NUMERIC(10,2))), 0) AS NUMERIC(5,2))
         FROM tblvoiceanalysis AS va WHERE va.isdeleted = FALSE
        ) AS avgfluencyscoreallusers,
        COALESCE(tiu.userid, 0)              AS mostimproveduserid,
        COALESCE(tiu.fullname, '')           AS mostimprovedusername,
        COALESCE(tiu.resolvedmistakecount, 0) AS mostimprovedresolvedmistakecount,
        (SELECT COALESCE(mst.grammartag, '')
         FROM tblmistake AS mst
         WHERE mst.isdeleted = FALSE AND mst.isresolved = FALSE AND mst.grammartag IS NOT NULL
         GROUP BY mst.grammartag
         ORDER BY COUNT(1) DESC, mst.grammartag ASC
         LIMIT 1
        )::VARCHAR(64) AS moststruggeledgrammartag
    FROM (SELECT 1) AS anchor
    LEFT JOIN topimproveduser AS tiu ON TRUE;
END;
$$ LANGUAGE plpgsql;

-- --------------------------------------------
-- uspGetAdminDashboardSummary
-- --------------------------------------------
CREATE OR REPLACE FUNCTION uspgetadmindashboardsummary()
RETURNS TABLE (
    totalusers            INT,
    activesessionstoday   INT,
    totalscriptsuploaded  INT,
    totalmistakesrecorded INT
) AS $$
BEGIN
    RETURN QUERY
    SELECT
        (SELECT COUNT(1)::INT FROM tbluser AS usr WHERE usr.isdeleted = FALSE AND usr.isactive = TRUE),
        (SELECT COUNT(1)::INT FROM tblsession AS ses
         WHERE ses.isdeleted = FALSE AND ses.status = 'ACTIVE'
           AND ses.datecreated::DATE = CURRENT_DATE),
        (SELECT COUNT(1)::INT FROM tblscript AS scr WHERE scr.isdeleted = FALSE),
        (SELECT COUNT(1)::INT FROM tblmistake AS mst WHERE mst.isdeleted = FALSE);
END;
$$ LANGUAGE plpgsql;

-- --------------------------------------------
-- uspGetAdminNoteByTargetUserId
-- --------------------------------------------
CREATE OR REPLACE FUNCTION uspgetadminnotebyTargetuserid(
    p_targetuserid BIGINT
) RETURNS TABLE (
    adminnoteid BIGINT,
    adminuserid BIGINT,
    adminname   VARCHAR(128),
    notetext    VARCHAR(512),
    notedate    TIMESTAMPTZ
) AS $$
BEGIN
    RETURN QUERY
    SELECT an.adminnoteid, an.adminuserid, au.fullname::VARCHAR(128), an.notetext, an.notedate
    FROM tbladminnote AS an
    INNER JOIN tbluser AS au ON au.userid = an.adminuserid
    WHERE an.targetuserid = p_targetuserid
      AND an.isdeleted    = FALSE
    ORDER BY an.notedate DESC, an.adminnoteid DESC;
END;
$$ LANGUAGE plpgsql;

-- --------------------------------------------
-- uspGetAllMistakeTypeCountByUserId
-- --------------------------------------------
CREATE OR REPLACE FUNCTION uspgetallmistaketypecountbyuserid(
    p_userid BIGINT
) RETURNS TABLE (
    mistaketype VARCHAR(32),
    totalcount  BIGINT
) AS $$
BEGIN
    RETURN QUERY
    SELECT mst.mistaketype, COUNT(1) AS totalcount
    FROM tblmistake AS mst
    WHERE mst.userid    = p_userid
      AND mst.isdeleted = FALSE
    GROUP BY mst.mistaketype
    ORDER BY COUNT(1) DESC, mst.mistaketype ASC;
END;
$$ LANGUAGE plpgsql;

-- --------------------------------------------
-- uspGetCurrentTurnBySessionId
-- --------------------------------------------
CREATE OR REPLACE FUNCTION uspgetcurrentturnbysessionid(
    p_sessionid BIGINT
) RETURNS TABLE (
    sessionid        BIGINT,
    turnindex        INT,
    totalturns       INT,
    activememberid   BIGINT,
    activemembername VARCHAR(128),
    activeslotindex  SMALLINT,
    utteranceid      BIGINT,
    scriptid         BIGINT,
    sequenceid       INT,
    speakerlabel     VARCHAR(64),
    englishtext      VARCHAR(512),
    hinttext         VARCHAR(512),
    grammartag       VARCHAR(64),
    contexttag       VARCHAR(64),
    focusword        VARCHAR(64),
    pronunciationnote VARCHAR(256),
    rereadallowed    BOOLEAN,
    rereadcount      INT,
    maxrereads       INT
) AS $$
BEGIN
    RETURN QUERY
    SELECT
        ts.sessionid, ts.turnindex, ts.totalturns,
        ts.activememberid, usr.fullname::VARCHAR(128),
        ts.activeslotindex, ts.utteranceid,
        ut.scriptid, ut.sequenceid, ut.speakerlabel,
        ut.englishtext, ut.hinttext, ut.grammartag, ut.contexttag,
        ut.focusword, ut.pronunciationnote,
        ts.rereadallowed, ts.rereadcount, ts.maxrereads
    FROM tblturnstate AS ts
    INNER JOIN tbluser AS usr ON usr.userid = ts.activememberid AND usr.isdeleted = FALSE
    INNER JOIN tblutterance AS ut ON ut.utteranceid = ts.utteranceid AND ut.isdeleted = FALSE
    WHERE ts.sessionid  = p_sessionid
      AND ts.turnstatus = 'ACTIVE'
      AND ts.isdeleted  = FALSE
    ORDER BY ts.turnindex DESC, ts.turnstateid DESC
    LIMIT 1;
END;
$$ LANGUAGE plpgsql;

-- --------------------------------------------
-- uspGetGrammarProgressByUserId
-- --------------------------------------------
CREATE OR REPLACE FUNCTION uspgetgrammarprogressbyuserid(
    p_userid BIGINT
) RETURNS TABLE (
    grammartag         TEXT,
    totalmistakes      BIGINT,
    resolvedmistakes   BIGINT,
    improvementpercent NUMERIC(5,2),
    progressbarvalue   INT
) AS $$
BEGIN
    RETURN QUERY
    SELECT
        COALESCE(NULLIF(mst.grammartag, ''), 'General') AS grammartag,
        COUNT(1)                                         AS totalmistakes,
        SUM(CASE WHEN mst.isresolved = TRUE THEN 1 ELSE 0 END) AS resolvedmistakes,
        CAST(CASE
            WHEN COUNT(1) = 0 THEN 0
            ELSE (SUM(CASE WHEN mst.isresolved = TRUE THEN 1 ELSE 0 END) * 100.0) / COUNT(1)
        END AS NUMERIC(5,2)) AS improvementpercent,
        CAST(ROUND(CASE
            WHEN COUNT(1) = 0 THEN 0
            ELSE (SUM(CASE WHEN mst.isresolved = TRUE THEN 1 ELSE 0 END) * 100.0) / COUNT(1)
        END) AS INT) AS progressbarvalue
    FROM tblmistake AS mst
    WHERE mst.userid    = p_userid
      AND mst.isdeleted = FALSE
    GROUP BY COALESCE(NULLIF(mst.grammartag, ''), 'General')
    ORDER BY 1 ASC;
END;
$$ LANGUAGE plpgsql;

-- --------------------------------------------
-- uspGetListenerFeedbackBySessionId
-- --------------------------------------------
CREATE OR REPLACE FUNCTION uspgetlistenerfeedbackbysessionid(
    p_sessionid BIGINT
) RETURNS TABLE (
    targetuserid  BIGINT,
    fullname      VARCHAR(128),
    feedbacktag   VARCHAR(32),
    feedbackcount BIGINT
) AS $$
BEGIN
    RETURN QUERY
    SELECT lf.targetuserid, usr.fullname::VARCHAR(128), lf.feedbacktag, COUNT(1) AS feedbackcount
    FROM tbllistenerfeedback AS lf
    INNER JOIN tbluser AS usr ON usr.userid = lf.targetuserid AND usr.isdeleted = FALSE
    WHERE lf.sessionid = p_sessionid
      AND lf.isdeleted = FALSE
    GROUP BY lf.targetuserid, usr.fullname, lf.feedbacktag
    ORDER BY lf.targetuserid ASC, lf.feedbacktag ASC;
END;
$$ LANGUAGE plpgsql;

-- --------------------------------------------
-- uspGetMistakeSummaryByUserId
-- --------------------------------------------
CREATE OR REPLACE FUNCTION uspgetmistakesummarybyuserid(
    p_userid BIGINT
) RETURNS TABLE (
    totalmistakes      BIGINT,
    resolvedmistakes   BIGINT,
    pendingmistakes    BIGINT,
    improvementpercent NUMERIC(5,2)
) AS $$
BEGIN
    RETURN QUERY
    SELECT
        COUNT(1),
        SUM(CASE WHEN mst.isresolved = TRUE THEN 1 ELSE 0 END),
        SUM(CASE WHEN mst.isresolved = FALSE THEN 1 ELSE 0 END),
        CAST(CASE
            WHEN COUNT(1) = 0 THEN 0
            ELSE (SUM(CASE WHEN mst.isresolved = TRUE THEN 1 ELSE 0 END) * 100.0) / COUNT(1)
        END AS NUMERIC(5,2))
    FROM tblmistake AS mst
    WHERE mst.userid    = p_userid
      AND mst.isdeleted = FALSE;
END;
$$ LANGUAGE plpgsql;

-- --------------------------------------------
-- uspGetRecentActivityList
-- TOP(@TopN) → LIMIT p_topn
-- --------------------------------------------
CREATE OR REPLACE FUNCTION uspgetrecentactivitylist(
    p_topn INT DEFAULT 10
) RETURNS TABLE (
    userfullname   VARCHAR(128),
    sessionname    VARCHAR(128),
    sessiondate    TIMESTAMPTZ,
    fluencyscore   NUMERIC(5,2),
    mistakecount   INT,
    sessionstatus  VARCHAR(16)
) AS $$
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
$$ LANGUAGE plpgsql;

-- --------------------------------------------
-- uspGetRefreshTokenByToken
-- --------------------------------------------
CREATE OR REPLACE FUNCTION uspgetrefreshtokenbytoken(
    p_token VARCHAR(512)
) RETURNS TABLE (
    refreshtokenid BIGINT,
    userid         BIGINT,
    token          VARCHAR(512),
    expiresat      TIMESTAMPTZ,
    isrevoked      BOOLEAN,
    revokedat      TIMESTAMPTZ,
    deviceinfo     VARCHAR(256),
    tag            VARCHAR(64),
    comments       VARCHAR(256),
    sortorder      INT,
    ipaddress      VARCHAR(64),
    createdby      VARCHAR(128),
    datecreated    TIMESTAMPTZ,
    updatedby      VARCHAR(128),
    lastupdated    TIMESTAMPTZ,
    deletedby      VARCHAR(128),
    datedeleted    TIMESTAMPTZ,
    isdeleted      BOOLEAN
) AS $$
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
$$ LANGUAGE plpgsql;

-- --------------------------------------------
-- uspGetScriptVersionHistoryByScriptId
-- --------------------------------------------
CREATE OR REPLACE FUNCTION uspgetscriptversionhistorybyscriptid(
    p_scriptid BIGINT
) RETURNS TABLE (
    scriptversionid    BIGINT,
    scriptid           BIGINT,
    versionnumber      INT,
    versionnotes       VARCHAR(256),
    uploadedbyuserid   BIGINT,
    uploadeddate       TIMESTAMPTZ
) AS $$
BEGIN
    RETURN QUERY
    SELECT sv.scriptversionid, sv.scriptid, sv.versionnumber,
           sv.versionnotes, sv.uploadedbyuserid, sv.uploadeddate
    FROM tblscriptversion AS sv
    WHERE sv.scriptid   = p_scriptid
      AND sv.isdeleted  = FALSE
    ORDER BY sv.versionnumber DESC, sv.scriptversionid DESC;
END;
$$ LANGUAGE plpgsql;

-- --------------------------------------------
-- uspGetTopGrammarMistakeType
-- TOP(@TopN) → LIMIT p_topn
-- --------------------------------------------
CREATE OR REPLACE FUNCTION uspgettopgrammarmistaketypes(
    p_topn INT DEFAULT 5
) RETURNS TABLE (
    grammartag  VARCHAR(64),
    usercount   BIGINT,
    percentage  NUMERIC(5,2)
) AS $$
DECLARE
    v_totalaffectedusers INT := 0;
BEGIN
    SELECT COUNT(DISTINCT mst.userid)::INT INTO v_totalaffectedusers
    FROM tblmistake AS mst
    WHERE mst.isdeleted = FALSE AND mst.grammartag IS NOT NULL;

    RETURN QUERY
    SELECT
        mst.grammartag::VARCHAR(64),
        COUNT(DISTINCT mst.userid) AS usercount,
        CAST(CASE
            WHEN v_totalaffectedusers = 0 THEN 0
            ELSE (COUNT(DISTINCT mst.userid) * 100.0) / v_totalaffectedusers
        END AS NUMERIC(5,2)) AS percentage
    FROM tblmistake AS mst
    WHERE mst.isdeleted = FALSE AND mst.grammartag IS NOT NULL
    GROUP BY mst.grammartag
    ORDER BY COUNT(DISTINCT mst.userid) DESC, mst.grammartag ASC
    LIMIT p_topn;
END;
$$ LANGUAGE plpgsql;

-- --------------------------------------------
-- uspGetUnresolvedMistakeByUserId
-- --------------------------------------------
CREATE OR REPLACE FUNCTION uspgetunresolvedmistakebyuserid(
    p_userid          BIGINT,
    p_sourcesessionid BIGINT DEFAULT 0
) RETURNS TABLE (
    mistakeid        BIGINT,
    userid           BIGINT,
    sessionid        BIGINT,
    utteranceid      BIGINT,
    scriptid         BIGINT,
    utterancetext    VARCHAR(512),
    spokentext       VARCHAR(512),
    mistaketype      VARCHAR(32),
    mistakedetail    VARCHAR(256),
    grammartag       VARCHAR(64),
    contexttag       VARCHAR(64),
    correctiontext   VARCHAR(512),
    practicecount    INT,
    isresolved       BOOLEAN,
    firstoccurrence  TIMESTAMPTZ,
    lastattempt      TIMESTAMPTZ
) AS $$
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
            WHEN 'GRAMMAR'      THEN 1
            WHEN 'PRONUNCIATION' THEN 2
            WHEN 'HESITATION'   THEN 3
            WHEN 'SPEED'        THEN 4
            WHEN 'SKIP'         THEN 5
            WHEN 'INCOMPLETE'   THEN 6
            ELSE 7
        END,
        mst.firstoccurrence DESC,
        mst.mistakeid DESC;
END;
$$ LANGUAGE plpgsql;

-- --------------------------------------------
-- uspGetUserBadgeByUserId
-- --------------------------------------------
CREATE OR REPLACE FUNCTION uspgetuserbadgebyuserid(
    p_userid BIGINT
) RETURNS TABLE (
    userbadgeid BIGINT,
    userid      BIGINT,
    badgecode   VARCHAR(64),
    badgename   VARCHAR(128),
    earneddate  TIMESTAMPTZ
) AS $$
BEGIN
    RETURN QUERY
    SELECT ub.userbadgeid, ub.userid, ub.badgecode, ub.badgename, ub.earneddate
    FROM tbluserbadge AS ub
    WHERE ub.userid    = p_userid
      AND ub.isdeleted = FALSE
    ORDER BY ub.earneddate DESC, ub.userbadgeid DESC;
END;
$$ LANGUAGE plpgsql;

-- --------------------------------------------
-- uspGetUserByMobileNumber
-- --------------------------------------------
CREATE OR REPLACE FUNCTION uspgetuserbymobilenumber(
    p_mobilenumber VARCHAR(16)
) RETURNS TABLE (
    userid                  BIGINT,
    fullname                VARCHAR(128),
    mobilenumber            VARCHAR(16),
    email                   VARCHAR(128),
    passwordhash            VARCHAR(256),
    agegroup                VARCHAR(32),
    preferredhintlanguage   VARCHAR(32),
    avatarurl               VARCHAR(256),
    groupcode               VARCHAR(32),
    role                    VARCHAR(16),
    dailystreakcount        INT,
    totalsessionsplayed     INT,
    lastlogindate           TIMESTAMPTZ,
    isactive                BOOLEAN,
    registrationdate        TIMESTAMPTZ,
    tag                     VARCHAR(64),
    comments                VARCHAR(256),
    sortorder               INT,
    ipaddress               VARCHAR(64),
    createdby               VARCHAR(128),
    datecreated             TIMESTAMPTZ,
    updatedby               VARCHAR(128),
    lastupdated             TIMESTAMPTZ,
    deletedby               VARCHAR(128),
    datedeleted             TIMESTAMPTZ,
    isdeleted               BOOLEAN
) AS $$
BEGIN
    RETURN QUERY
    SELECT
        usr.userid, usr.fullname, usr.mobilenumber, usr.email, usr.passwordhash,
        usr.agegroup, usr.preferredhintlanguage, usr.avatarurl, usr.groupcode, usr.role,
        usr.dailystreakcount, usr.totalsessionsplayed, usr.lastlogindate,
        usr.isactive, usr.registrationdate, usr.tag, usr.comments, usr.sortorder,
        usr.ipaddress, usr.createdby, usr.datecreated, usr.updatedby, usr.lastupdated,
        usr.deletedby, usr.datedeleted, usr.isdeleted
    FROM tbluser AS usr
    WHERE usr.mobilenumber = p_mobilenumber
      AND usr.isdeleted    = FALSE
      AND usr.isactive     = TRUE;
END;
$$ LANGUAGE plpgsql;

-- --------------------------------------------
-- uspGetUserByUserId
-- --------------------------------------------
CREATE OR REPLACE FUNCTION uspgetuserbyuserid(
    p_userid BIGINT
) RETURNS TABLE (
    userid                  BIGINT,
    fullname                VARCHAR(128),
    mobilenumber            VARCHAR(16),
    email                   VARCHAR(128),
    passwordhash            VARCHAR(256),
    agegroup                VARCHAR(32),
    preferredhintlanguage   VARCHAR(32),
    avatarurl               VARCHAR(256),
    groupcode               VARCHAR(32),
    role                    VARCHAR(16),
    dailystreakcount        INT,
    totalsessionsplayed     INT,
    lastlogindate           TIMESTAMPTZ,
    isactive                BOOLEAN,
    registrationdate        TIMESTAMPTZ,
    tag                     VARCHAR(64),
    comments                VARCHAR(256),
    sortorder               INT,
    ipaddress               VARCHAR(64),
    createdby               VARCHAR(128),
    datecreated             TIMESTAMPTZ,
    updatedby               VARCHAR(128),
    lastupdated             TIMESTAMPTZ,
    deletedby               VARCHAR(128),
    datedeleted             TIMESTAMPTZ,
    isdeleted               BOOLEAN
) AS $$
BEGIN
    RETURN QUERY
    SELECT
        usr.userid, usr.fullname, usr.mobilenumber, usr.email, usr.passwordhash,
        usr.agegroup, usr.preferredhintlanguage, usr.avatarurl, usr.groupcode, usr.role,
        usr.dailystreakcount, usr.totalsessionsplayed, usr.lastlogindate,
        usr.isactive, usr.registrationdate, usr.tag, usr.comments, usr.sortorder,
        usr.ipaddress, usr.createdby, usr.datecreated, usr.updatedby, usr.lastupdated,
        usr.deletedby, usr.datedeleted, usr.isdeleted
    FROM tbluser AS usr
    WHERE usr.userid    = p_userid
      AND usr.isdeleted = FALSE;
END;
$$ LANGUAGE plpgsql;

-- --------------------------------------------
-- uspGetVoiceAnalysisBySessionId
-- --------------------------------------------
CREATE OR REPLACE FUNCTION uspgetvoiceanalysisbysessionid(
    p_sessionid BIGINT
) RETURNS TABLE (
    voiceanalysisid   BIGINT,
    sessionid         BIGINT,
    userid            BIGINT,
    fullname          VARCHAR(128),
    turnindex         INT,
    utteranceid       BIGINT,
    transcribedtext   VARCHAR(512),
    expectedtext      VARCHAR(512),
    fluencyscore      NUMERIC(5,2),
    confidencescore   NUMERIC(5,2),
    speakingspeedwpm  INT,
    pausecount        INT,
    hesitationwords   VARCHAR(256),
    repeatedwords     VARCHAR(256),
    grammarerrorsjson JSONB,
    pronunciationjson JSONB,
    overallscore      NUMERIC(5,2),
    recordedat        TIMESTAMPTZ
) AS $$
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
$$ LANGUAGE plpgsql;

-- --------------------------------------------
-- uspGetVoiceAnalysisByUserId
-- --------------------------------------------
CREATE OR REPLACE FUNCTION uspgetvoiceanalysisbyuserid(
    p_userid    BIGINT,
    p_sessionid BIGINT DEFAULT 0
) RETURNS TABLE (
    voiceanalysisid   BIGINT,
    sessionid         BIGINT,
    userid            BIGINT,
    fullname          VARCHAR(128),
    turnindex         INT,
    utteranceid       BIGINT,
    transcribedtext   VARCHAR(512),
    expectedtext      VARCHAR(512),
    fluencyscore      NUMERIC(5,2),
    confidencescore   NUMERIC(5,2),
    speakingspeedwpm  INT,
    pausecount        INT,
    hesitationwords   VARCHAR(256),
    repeatedwords     VARCHAR(256),
    grammarerrorsjson JSONB,
    pronunciationjson JSONB,
    overallscore      NUMERIC(5,2),
    recordedat        TIMESTAMPTZ
) AS $$
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
$$ LANGUAGE plpgsql;

-- --------------------------------------------
-- uspGetWeeklyFluencyScoreByUserId
-- DATEPART(WEEKDAY) + week-start → DATE_TRUNC('week', ...)
-- DATEPART(ISO_WEEK) → EXTRACT(WEEK FROM ...)
-- WeekLabel → TO_CHAR(d,'IYYY-"W"IW')
-- --------------------------------------------
CREATE OR REPLACE FUNCTION uspgetweeklyfluencyscorebyuserid(
    p_userid BIGINT
) RETURNS TABLE (
    weeklabel      TEXT,
    avgfluencyscore NUMERIC(5,2)
) AS $$
BEGIN
    RETURN QUERY
    WITH weeklyaggregate AS (
        SELECT
            DATE_TRUNC('week', va.recordedat)::DATE AS weekstart,
            CAST(COALESCE(AVG(CAST(va.fluencyscore AS NUMERIC(10,2))), 0) AS NUMERIC(5,2)) AS avgfluencyscore
        FROM tblvoiceanalysis AS va
        WHERE va.userid    = p_userid
          AND va.isdeleted = FALSE
          AND va.recordedat >= CURRENT_DATE - INTERVAL '4 weeks'
        GROUP BY DATE_TRUNC('week', va.recordedat)::DATE
    )
    SELECT
        TO_CHAR(wa.weekstart, 'IYYY-"W"IW') AS weeklabel,
        wa.avgfluencyscore
    FROM weeklyaggregate AS wa
    ORDER BY wa.weekstart DESC;
END;
$$ LANGUAGE plpgsql;

-- --------------------------------------------
-- uspGetUserProfileByUserId
-- --------------------------------------------
CREATE OR REPLACE FUNCTION uspgetuserprofilebyuserid(
    p_userid BIGINT
) RETURNS TABLE (
    userid                BIGINT,
    fullname              VARCHAR(128),
    mobilenumber          VARCHAR(16),
    email                 VARCHAR(128),
    agegroup              VARCHAR(32),
    preferredhintlanguage VARCHAR(32),
    avatarurl             VARCHAR(256),
    role                  VARCHAR(16),
    dailystreakcount      INT,
    totalsessionsplayed   INT,
    totalsessions         INT,
    avgfluencyscore       NUMERIC(5,2),
    totalmistakesfixed    INT,
    isactive              BOOLEAN,
    registrationdate      TIMESTAMPTZ
) AS $$
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
$$ LANGUAGE plpgsql;

-- --------------------------------------------
-- uspCheckAndAwardBadge
-- EXEC dbo.uspInsertUserBadge → PERFORM uspinsertuserbadge(...)
-- OBJECT_ID checks removed (tables always exist in PG)
-- --------------------------------------------
CREATE OR REPLACE FUNCTION uspcheckandawardbadge(
    p_userid    BIGINT,
    p_createdby VARCHAR(128),
    p_ipaddress VARCHAR(64)
) RETURNS VOID AS $$
DECLARE
    v_dailystreakcount      INT := 0;
    v_totalsessionsplayed   INT := 0;
    v_resolvedmistakes      INT := 0;
BEGIN
    SELECT usr.dailystreakcount, usr.totalsessionsplayed
    INTO v_dailystreakcount, v_totalsessionsplayed
    FROM tbluser AS usr
    WHERE usr.userid = p_userid AND usr.isdeleted = FALSE;

    SELECT COUNT(1)::INT INTO v_resolvedmistakes
    FROM tblmistake AS mis
    WHERE mis.userid = p_userid AND mis.isresolved = TRUE AND mis.isdeleted = FALSE;

    IF v_dailystreakcount >= 7 THEN
        PERFORM uspinsertuserbadge(p_userid, '7_DAY_STREAK', '7-Day Streak', p_createdby, p_ipaddress);
    END IF;

    IF v_totalsessionsplayed >= 10 THEN
        PERFORM uspinsertuserbadge(p_userid, '10_SESSIONS', '10 Sessions', p_createdby, p_ipaddress);
    END IF;

    IF v_resolvedmistakes >= 50 THEN
        PERFORM uspinsertuserbadge(p_userid, '50_MISTAKES_FIXED', '50 Mistakes Fixed', p_createdby, p_ipaddress);
    END IF;
END;
$$ LANGUAGE plpgsql;

-- ============================================
-- BATCH 4: ANALYTICS / OPENJSON / OUTER APPLY PROCEDURES
-- OUTER APPLY (SELECT COUNT(*) FROM OPENJSON(...)) →
--   CROSS JOIN LATERAL (SELECT COUNT(*) FROM jsonb_array_elements(...))
-- ============================================

-- --------------------------------------------
-- uspGetAnalyticsSummaryByUserId
-- DATEADD(WEEK,DATEDIFF(WEEK,0,d),0) → DATE_TRUNC('week',d)::DATE
-- --------------------------------------------
CREATE OR REPLACE FUNCTION uspgetanalyticssummarybyuserid(
    p_userid BIGINT
) RETURNS TABLE (
    weeklabel      TEXT,
    avgfluencyscore NUMERIC(5,2),
    sessioncount   BIGINT,
    mistakecount   BIGINT
) AS $$
BEGIN
    RETURN QUERY
    WITH weeklysession AS (
        SELECT
            DATE_TRUNC('week', COALESCE(ses.endeddate, COALESCE(ses.starteddate, ses.datecreated)))::DATE AS weekstartdate,
            ses.sessionid,
            CAST(COALESCE(AVG(CAST(va.fluencyscore AS NUMERIC(10,2))), 0) AS NUMERIC(5,2)) AS avgfluencyscore,
            SUM(COALESCE(geo.errorcount, 0) + COALESCE(pro.errorcount, 0)) AS mistakecount
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
          AND COALESCE(ses.endeddate, COALESCE(ses.starteddate, ses.datecreated)) >= CURRENT_DATE - INTERVAL '4 weeks'
        GROUP BY
            DATE_TRUNC('week', COALESCE(ses.endeddate, COALESCE(ses.starteddate, ses.datecreated)))::DATE,
            ses.sessionid
    )
    SELECT
        TO_CHAR(ws.weekstartdate, 'YYYY-MM-DD') AS weeklabel,
        CAST(AVG(ws.avgfluencyscore) AS NUMERIC(5,2)),
        COUNT(1),
        SUM(ws.mistakecount)
    FROM weeklysession AS ws
    GROUP BY ws.weekstartdate
    ORDER BY ws.weekstartdate DESC
    LIMIT 4;
END;
$$ LANGUAGE plpgsql;

-- --------------------------------------------
-- uspGetImprovementDataByUserId
-- OUTER APPLY + OPENJSON → CROSS JOIN LATERAL + jsonb_array_elements
-- --------------------------------------------
CREATE OR REPLACE FUNCTION uspgetimprovementdatabyuserid(
    p_userid BIGINT
) RETURNS TABLE (
    sessiondate     TIMESTAMPTZ,
    sessionname     VARCHAR(128),
    fluencyscore    NUMERIC(5,2),
    confidencescore NUMERIC(5,2),
    mistakecount    BIGINT
) AS $$
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
$$ LANGUAGE plpgsql;

-- --------------------------------------------
-- uspGetTopPerformerListBySessionId
-- OUTER APPLY + OPENJSON → CROSS JOIN LATERAL
-- --------------------------------------------
CREATE OR REPLACE FUNCTION uspgettopperformerlistbysessionid(
    p_sessionid BIGINT
) RETURNS TABLE (
    ranknumber      BIGINT,
    userid          BIGINT,
    fullname        VARCHAR(128),
    sessionname     VARCHAR(128),
    avgfluencyscore NUMERIC(5,2),
    mistakecount    BIGINT
) AS $$
BEGIN
    RETURN QUERY
    WITH rankedmember AS (
        SELECT
            ROW_NUMBER() OVER (
                ORDER BY
                    CAST(COALESCE(AVG(CAST(va.fluencyscore AS NUMERIC(10,2))), 0) AS NUMERIC(5,2)) DESC,
                    usr.fullname ASC
            ) AS ranknumber,
            usr.userid,
            usr.fullname,
            ses.sessionname,
            CAST(COALESCE(AVG(CAST(va.fluencyscore AS NUMERIC(10,2))), 0) AS NUMERIC(5,2)) AS avgfluencyscore,
            SUM(COALESCE(geo.errorcount, 0) + COALESCE(pro.errorcount, 0)) AS mistakecount
        FROM tblsessionmember AS sem
        INNER JOIN tbluser AS usr ON usr.userid = sem.userid AND usr.isdeleted = FALSE
        INNER JOIN tblsession AS ses ON ses.sessionid = sem.sessionid AND ses.isdeleted = FALSE
        LEFT JOIN tblvoiceanalysis AS va
            ON va.sessionid = sem.sessionid AND va.userid = sem.userid AND va.isdeleted = FALSE
        CROSS JOIN LATERAL (
            SELECT COUNT(*)::BIGINT AS errorcount
            FROM jsonb_array_elements(COALESCE(va.grammarerrorsjson, '[]'::JSONB))
        ) AS geo
        CROSS JOIN LATERAL (
            SELECT COUNT(*)::BIGINT AS errorcount
            FROM jsonb_array_elements(COALESCE(va.pronunciationjson, '[]'::JSONB))
        ) AS pro
        WHERE sem.sessionid = p_sessionid
          AND sem.isdeleted = FALSE
        GROUP BY usr.userid, usr.fullname, ses.sessionname
    )
    SELECT rm.ranknumber, rm.userid, rm.fullname::VARCHAR(128),
           rm.sessionname::VARCHAR(128), rm.avgfluencyscore, rm.mistakecount
    FROM rankedmember AS rm
    ORDER BY rm.ranknumber ASC;
END;
$$ LANGUAGE plpgsql;

-- --------------------------------------------
-- uspGetSessionCompletionSummary
-- Returns 2 result sets via SETOF REFCURSOR:
--   cursor 1: member scores
--   cursor 2: session summary
-- --------------------------------------------
CREATE OR REPLACE FUNCTION uspgetsessioncompletionsummary(
    p_sessionid BIGINT
) RETURNS SETOF REFCURSOR AS $$
DECLARE
    ref1 REFCURSOR;
    ref2 REFCURSOR;
BEGIN
    OPEN ref1 FOR
    WITH voiceaggregate AS (
        SELECT
            va.userid,
            CAST(COALESCE(AVG(CAST(va.fluencyscore AS NUMERIC(10,2))), 0) AS NUMERIC(5,2)) AS fluencyscore,
            CAST(COALESCE(AVG(CAST(va.confidencescore AS NUMERIC(10,2))), 0) AS NUMERIC(5,2)) AS confidencescore,
            SUM(COALESCE(err.errorcount, 0)) AS mistakecount
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
        usr.fullname,
        COALESCE(va.fluencyscore, 0),
        COALESCE(va.confidencescore, 0),
        COALESCE(va.mistakecount, 0),
        COALESCE(fa.listenerrating, 0)
    FROM tblsessionmember AS sem
    INNER JOIN tbluser AS usr ON usr.userid = sem.userid AND usr.isdeleted = FALSE
    LEFT JOIN voiceaggregate AS va ON va.userid = sem.userid
    LEFT JOIN feedbackaggregate AS fa ON fa.userid = sem.userid
    WHERE sem.sessionid = p_sessionid AND sem.isdeleted = FALSE
    ORDER BY sem.slotindex ASC, sem.sessionmemberid ASC;
    RETURN NEXT ref1;

    OPEN ref2 FOR
    WITH voiceaggregate AS (
        SELECT SUM(COALESCE(err.errorcount, 0)) AS totalmistakecount
        FROM tblvoiceanalysis AS va
        CROSS JOIN LATERAL (
            SELECT COUNT(*)::BIGINT AS errorcount
            FROM jsonb_array_elements(COALESCE(va.grammarerrorsjson, '[]'::JSONB))
        ) AS err
        WHERE va.sessionid = p_sessionid AND va.isdeleted = FALSE
    )
    SELECT
        COALESCE(scr.utterancecount, 0)         AS totalturns,
        scr.scripttitle,
        scr.grammarfocustag,
        COALESCE(va.totalmistakecount, 0)       AS totalmistakesallmembers
    FROM tblsession AS ses
    INNER JOIN tblscript AS scr ON scr.scriptid = ses.scriptid AND scr.isdeleted = FALSE
    CROSS JOIN voiceaggregate AS va
    WHERE ses.sessionid = p_sessionid AND ses.isdeleted = FALSE;
    RETURN NEXT ref2;
END;
$$ LANGUAGE plpgsql;

-- --------------------------------------------
-- uspGetStreakDataByUserId
-- Returns 2 result sets via SETOF REFCURSOR:
--   cursor 1: current streak + longest streak
--   cursor 2: last 30 streak days
-- DATEADD(DAY,-ROW_NUMBER()OVER(...),date) → date - interval * rn
-- --------------------------------------------
CREATE OR REPLACE FUNCTION uspgetstreakdatabyuserid(
    p_userid BIGINT
) RETURNS SETOF REFCURSOR AS $$
DECLARE
    ref1 REFCURSOR;
    ref2 REFCURSOR;
BEGIN
    OPEN ref1 FOR
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
    longeststreak AS (
        SELECT COUNT(1)::INT AS streaklength
        FROM groupedstreak
        GROUP BY streakgroup
    )
    SELECT
        COALESCE((SELECT usr.dailystreakcount FROM tbluser AS usr
                  WHERE usr.userid = p_userid AND usr.isdeleted = FALSE), 0) AS currentstreak,
        COALESCE((SELECT MAX(ls.streaklength) FROM longeststreak AS ls), 0) AS longeststreak;
    RETURN NEXT ref1;

    OPEN ref2 FOR
    SELECT usrs.streakdate, usrs.sessioncount, usrs.practiceminutes
    FROM tbluserstreak AS usrs
    WHERE usrs.userid = p_userid AND usrs.isdeleted = FALSE
    ORDER BY usrs.streakdate DESC
    LIMIT 30;
    RETURN NEXT ref2;
END;
$$ LANGUAGE plpgsql;

-- ============================================
-- BATCH 5: PAGINATION (2 RESULT SETS) + OUTPUT PARAM PROCEDURES
-- Each pagination SP returns SETOF REFCURSOR:
--   cursor 1: data rows
--   cursor 2: TotalCount
-- ============================================

-- --------------------------------------------
-- uspGetAllUserBySearch
-- --------------------------------------------
CREATE OR REPLACE FUNCTION uspgetallusersbysearch(
    p_searchterm VARCHAR(128) DEFAULT NULL,
    p_agegroup   VARCHAR(32)  DEFAULT NULL,
    p_isactive   BOOLEAN      DEFAULT NULL,
    p_pagenumber INT          DEFAULT 1,
    p_pagesize   INT          DEFAULT 10
) RETURNS SETOF REFCURSOR AS $$
DECLARE
    ref1   REFCURSOR;
    ref2   REFCURSOR;
    v_offset INT := (p_pagenumber - 1) * p_pagesize;
BEGIN
    OPEN ref1 FOR
    SELECT
        usr.userid, usr.fullname, usr.mobilenumber, usr.agegroup,
        usr.totalsessionsplayed, usr.dailystreakcount,
        usr.lastlogindate, usr.isactive
    FROM tbluser AS usr
    WHERE usr.isdeleted = FALSE
      AND (p_searchterm IS NULL
           OR usr.fullname     ILIKE '%' || p_searchterm || '%'
           OR usr.mobilenumber ILIKE '%' || p_searchterm || '%')
      AND (p_agegroup IS NULL OR usr.agegroup = p_agegroup)
      AND (p_isactive IS NULL OR usr.isactive = p_isactive)
    ORDER BY usr.datecreated DESC, usr.userid DESC
    LIMIT p_pagesize OFFSET v_offset;
    RETURN NEXT ref1;

    OPEN ref2 FOR
    SELECT COUNT(1) AS totalcount
    FROM tbluser AS usr
    WHERE usr.isdeleted = FALSE
      AND (p_searchterm IS NULL
           OR usr.fullname     ILIKE '%' || p_searchterm || '%'
           OR usr.mobilenumber ILIKE '%' || p_searchterm || '%')
      AND (p_agegroup IS NULL OR usr.agegroup = p_agegroup)
      AND (p_isactive IS NULL OR usr.isactive = p_isactive);
    RETURN NEXT ref2;
END;
$$ LANGUAGE plpgsql;

-- --------------------------------------------
-- uspGetMistakeByUserIdWithFilter
-- --------------------------------------------
CREATE OR REPLACE FUNCTION uspgetmistakebyuserwithfilter(
    p_userid      BIGINT,
    p_mistaketype VARCHAR(32) DEFAULT NULL,
    p_isresolved  BOOLEAN     DEFAULT NULL,
    p_pagenumber  INT         DEFAULT 1,
    p_pagesize    INT         DEFAULT 20
) RETURNS SETOF REFCURSOR AS $$
DECLARE
    ref1     REFCURSOR;
    ref2     REFCURSOR;
    v_offset INT := (p_pagenumber - 1) * p_pagesize;
BEGIN
    OPEN ref1 FOR
    SELECT
        mst.mistakeid, mst.userid, mst.sessionid, mst.utteranceid, mst.scriptid,
        mst.utterancetext, mst.spokentext, mst.mistaketype, mst.mistakedetail,
        mst.grammartag, mst.contexttag, mst.correctiontext,
        mst.practicecount, mst.isresolved, mst.firstoccurrence, mst.lastattempt,
        ses.sessionname, scr.scripttitle
    FROM tblmistake AS mst
    LEFT JOIN tblsession AS ses ON ses.sessionid = mst.sessionid AND ses.isdeleted = FALSE
    LEFT JOIN tblscript  AS scr ON scr.scriptid  = mst.scriptid  AND scr.isdeleted = FALSE
    WHERE mst.userid    = p_userid
      AND mst.isdeleted = FALSE
      AND (p_mistaketype IS NULL OR mst.mistaketype = p_mistaketype)
      AND (p_isresolved  IS NULL OR mst.isresolved  = p_isresolved)
    ORDER BY mst.firstoccurrence DESC, mst.mistakeid DESC
    LIMIT p_pagesize OFFSET v_offset;
    RETURN NEXT ref1;

    OPEN ref2 FOR
    SELECT COUNT(1) AS totalcount
    FROM tblmistake AS mst
    WHERE mst.userid    = p_userid
      AND mst.isdeleted = FALSE
      AND (p_mistaketype IS NULL OR mst.mistaketype = p_mistaketype)
      AND (p_isresolved  IS NULL OR mst.isresolved  = p_isresolved);
    RETURN NEXT ref2;
END;
$$ LANGUAGE plpgsql;

-- --------------------------------------------
-- uspGetRepracticeSessionListByUserId
-- --------------------------------------------
CREATE OR REPLACE FUNCTION uspgetrepracticesessionlistbyuserid(
    p_userid     BIGINT,
    p_status     VARCHAR(16) DEFAULT NULL,
    p_pagenumber INT         DEFAULT 1,
    p_pagesize   INT         DEFAULT 10
) RETURNS SETOF REFCURSOR AS $$
DECLARE
    ref1     REFCURSOR;
    ref2     REFCURSOR;
    v_offset INT := (p_pagenumber - 1) * p_pagesize;
BEGIN
    OPEN ref1 FOR
    SELECT
        rps.repracticesessionid, rps.sourcesessionid, rps.status,
        rps.totalmistakes, rps.completedrounds,
        rps.improvementpercent, rps.generateddate
    FROM tblrepracticesession AS rps
    WHERE rps.userid    = p_userid
      AND rps.isdeleted = FALSE
      AND (p_status IS NULL OR rps.status = p_status)
    ORDER BY rps.generateddate DESC, rps.repracticesessionid DESC
    LIMIT p_pagesize OFFSET v_offset;
    RETURN NEXT ref1;

    OPEN ref2 FOR
    SELECT COUNT(1) AS totalcount
    FROM tblrepracticesession AS rps
    WHERE rps.userid    = p_userid
      AND rps.isdeleted = FALSE
      AND (p_status IS NULL OR rps.status = p_status);
    RETURN NEXT ref2;
END;
$$ LANGUAGE plpgsql;

-- --------------------------------------------
-- uspGetScriptBySearch
-- --------------------------------------------
CREATE OR REPLACE FUNCTION uspgetscriptbysearch(
    p_searchterm     VARCHAR(128) DEFAULT NULL,
    p_category       VARCHAR(64)  DEFAULT NULL,
    p_grammarfocustag VARCHAR(64) DEFAULT NULL,
    p_targetagegroup VARCHAR(32)  DEFAULT NULL,
    p_isactive       BOOLEAN      DEFAULT NULL,
    p_pagenumber     INT          DEFAULT 1,
    p_pagesize       INT          DEFAULT 12
) RETURNS SETOF REFCURSOR AS $$
DECLARE
    ref1     REFCURSOR;
    ref2     REFCURSOR;
    v_offset INT := (p_pagenumber - 1) * p_pagesize;
BEGIN
    OPEN ref1 FOR
    SELECT
        scr.scriptid, scr.scripttitle, scr.category, scr.grammarfocustag,
        scr.contexttag, scr.complexitylevel, scr.targetagegroup,
        scr.utterancecount, scr.isactive, scr.uploadeddate, scr.version
    FROM tblscript AS scr
    WHERE scr.isdeleted = FALSE
      AND (p_searchterm      IS NULL OR scr.scripttitle    ILIKE '%' || p_searchterm || '%')
      AND (p_category        IS NULL OR scr.category        = p_category)
      AND (p_grammarfocustag IS NULL OR scr.grammarfocustag = p_grammarfocustag)
      AND (p_targetagegroup  IS NULL OR scr.targetagegroup  = p_targetagegroup)
      AND (p_isactive        IS NULL OR scr.isactive        = p_isactive)
    ORDER BY scr.uploadeddate DESC, scr.scriptid DESC
    LIMIT p_pagesize OFFSET v_offset;
    RETURN NEXT ref1;

    OPEN ref2 FOR
    SELECT COUNT(1) AS totalcount
    FROM tblscript AS scr
    WHERE scr.isdeleted = FALSE
      AND (p_searchterm      IS NULL OR scr.scripttitle    ILIKE '%' || p_searchterm || '%')
      AND (p_category        IS NULL OR scr.category        = p_category)
      AND (p_grammarfocustag IS NULL OR scr.grammarfocustag = p_grammarfocustag)
      AND (p_targetagegroup  IS NULL OR scr.targetagegroup  = p_targetagegroup)
      AND (p_isactive        IS NULL OR scr.isactive        = p_isactive);
    RETURN NEXT ref2;
END;
$$ LANGUAGE plpgsql;

-- --------------------------------------------
-- uspGetSessionListByUserId
-- --------------------------------------------
CREATE OR REPLACE FUNCTION uspgetsessionlistbyuserid(
    p_userid       BIGINT,
    p_statusfilter VARCHAR(16) DEFAULT NULL,
    p_pagenumber   INT         DEFAULT 1,
    p_pagesize     INT         DEFAULT 20
) RETURNS SETOF REFCURSOR AS $$
DECLARE
    ref1     REFCURSOR;
    ref2     REFCURSOR;
    v_offset INT := (p_pagenumber - 1) * p_pagesize;
BEGIN
    OPEN ref1 FOR
    SELECT
        ses.sessionid, ses.sessionname, ses.sessionmode,
        COALESCE(ses.starteddate, ses.datecreated) AS sessiondate,
        ses.sessionduration AS duration,
        NULL::NUMERIC(10,2) AS fluencyscore,
        0::INT              AS mistakecount,
        ses.status, scr.scripttitle
    FROM tblsessionmember AS sem
    INNER JOIN tblsession AS ses ON ses.sessionid = sem.sessionid AND ses.isdeleted = FALSE
    INNER JOIN tblscript  AS scr ON scr.scriptid  = ses.scriptid  AND scr.isdeleted = FALSE
    WHERE sem.userid    = p_userid
      AND sem.isdeleted = FALSE
      AND (p_statusfilter IS NULL OR ses.status = p_statusfilter)
    ORDER BY COALESCE(ses.starteddate, ses.datecreated) DESC, ses.sessionid DESC
    LIMIT p_pagesize OFFSET v_offset;
    RETURN NEXT ref1;

    OPEN ref2 FOR
    SELECT COUNT(1) AS totalcount
    FROM tblsessionmember AS sem
    INNER JOIN tblsession AS ses ON ses.sessionid = sem.sessionid AND ses.isdeleted = FALSE
    WHERE sem.userid    = p_userid
      AND sem.isdeleted = FALSE
      AND (p_statusfilter IS NULL OR ses.status = p_statusfilter);
    RETURN NEXT ref2;
END;
$$ LANGUAGE plpgsql;

-- --------------------------------------------
-- uspGetUserReportSummaryList
-- --------------------------------------------
CREATE OR REPLACE FUNCTION uspgetuserreportsummarylist(
    p_fromdate   TIMESTAMPTZ DEFAULT NULL,
    p_todate     TIMESTAMPTZ DEFAULT NULL,
    p_userid     BIGINT      DEFAULT 0,
    p_pagenumber INT         DEFAULT 1,
    p_pagesize   INT         DEFAULT 10
) RETURNS SETOF REFCURSOR AS $$
DECLARE
    ref1     REFCURSOR;
    ref2     REFCURSOR;
    v_offset INT := (p_pagenumber - 1) * p_pagesize;
BEGIN
    OPEN ref1 FOR
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
          AND (p_todate   IS NULL OR COALESCE(ses.starteddate, ses.datecreated) <  p_todate + INTERVAL '1 day')
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
          AND (p_todate   IS NULL OR COALESCE(ses.starteddate, ses.datecreated) <  p_todate + INTERVAL '1 day')
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
                  AND (p_todate   IS NULL OR COALESCE(ses.starteddate, ses.datecreated) <  p_todate + INTERVAL '1 day')
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
        ur.userid, ur.fullname, ur.totalsessions, ur.avgfluencyscore,
        COALESCE(um.mostcommonmistaketype, '') AS mostcommonmistaketype,
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
    RETURN NEXT ref1;

    OPEN ref2 FOR
    SELECT COUNT(1) AS totalcount
    FROM tbluser AS usr
    WHERE usr.isdeleted = FALSE
      AND (p_userid = 0 OR usr.userid = p_userid);
    RETURN NEXT ref2;
END;
$$ LANGUAGE plpgsql;

-- --------------------------------------------
-- uspValidateJoinCode
-- SQL Server OUTPUT params → PostgreSQL OUT params
-- --------------------------------------------
CREATE OR REPLACE FUNCTION uspvalidatejoincode(
    p_joincode              VARCHAR(8),
    OUT p_isvalid           BOOLEAN,
    OUT p_sessionid         BIGINT,
    OUT p_sessionname       VARCHAR(128),
    OUT p_status            VARCHAR(16),
    OUT p_currentmembercount INT
) RETURNS RECORD AS $$
DECLARE
    v_maxmembers SMALLINT := 0;
    v_tempid     BIGINT   := 0;
BEGIN
    p_isvalid            := FALSE;
    p_sessionid          := 0;
    p_sessionname        := '';
    p_status             := '';
    p_currentmembercount := 0;

    SELECT ses.sessionid, ses.sessionname, ses.status, ses.maxmembers
    INTO v_tempid, p_sessionname, p_status, v_maxmembers
    FROM tblsession AS ses
    WHERE ses.joincode  = p_joincode
      AND ses.isdeleted = FALSE
      AND ses.status    = 'LOBBY'
      AND (ses.roomexpiresat IS NULL OR ses.roomexpiresat > NOW())
    ORDER BY ses.sessionid DESC
    LIMIT 1;

    IF v_tempid IS NULL OR v_tempid = 0 THEN
        RETURN;
    END IF;

    SELECT COUNT(sem.sessionmemberid)::INT INTO p_currentmembercount
    FROM tblsessionmember AS sem
    WHERE sem.sessionid = v_tempid AND sem.isdeleted = FALSE AND sem.isactive = TRUE;

    IF p_currentmembercount < v_maxmembers THEN
        p_isvalid   := TRUE;
        p_sessionid := v_tempid;
    END IF;
END;
$$ LANGUAGE plpgsql;

-- --------------------------------------------
-- uspVerifyOtp
-- SQL Server OUTPUT params → PostgreSQL OUT params
-- Multiple COMMIT TRAN → single transaction in caller; early returns just set OUT values
-- --------------------------------------------
CREATE OR REPLACE FUNCTION uspverifyotp(
    p_mobilenumber VARCHAR(16),
    p_otpcode      VARCHAR(8),
    p_updatedby    VARCHAR(128),
    p_ipaddress    VARCHAR(64),
    OUT p_isvalid  BOOLEAN,
    OUT p_userid   BIGINT
) RETURNS RECORD AS $$
DECLARE
    v_otpverificationid BIGINT      := 0;
    v_storedotpcode     VARCHAR(8)  := '';
    v_expiresat         TIMESTAMPTZ := NULL;
    v_attemptcount      INT         := 0;
    v_maxattempts       INT         := 3;
BEGIN
    p_isvalid := FALSE;
    p_userid  := 0;

    SELECT otp.otpverificationid, otp.otpcode, otp.expiresat, otp.attemptcount
    INTO v_otpverificationid, v_storedotpcode, v_expiresat, v_attemptcount
    FROM tblotpverification AS otp
    WHERE otp.mobilenumber = p_mobilenumber
      AND otp.isverified   = FALSE
      AND otp.isdeleted    = FALSE
    ORDER BY otp.otpverificationid DESC
    LIMIT 1;

    IF v_otpverificationid IS NULL OR v_otpverificationid = 0 THEN
        RETURN;
    END IF;

    UPDATE tblotpverification
    SET attemptcount = attemptcount + 1,
        updatedby    = p_updatedby,
        lastupdated  = NOW(),
        ipaddress    = p_ipaddress
    WHERE otpverificationid = v_otpverificationid;

    IF v_attemptcount >= v_maxattempts THEN RETURN; END IF;
    IF v_expiresat <= NOW()            THEN RETURN; END IF;
    IF v_storedotpcode <> p_otpcode    THEN RETURN; END IF;

    UPDATE tblotpverification
    SET isverified  = TRUE,
        verifiedat  = NOW(),
        updatedby   = p_updatedby,
        lastupdated = NOW(),
        ipaddress   = p_ipaddress
    WHERE otpverificationid = v_otpverificationid;

    SELECT usr.userid INTO p_userid
    FROM tbluser AS usr
    WHERE usr.mobilenumber = p_mobilenumber
      AND usr.isdeleted    = FALSE
      AND usr.isactive     = TRUE
    LIMIT 1;

    p_isvalid := TRUE;
    p_userid  := COALESCE(p_userid, 0);
END;
$$ LANGUAGE plpgsql;

-- ============================================
-- BATCH 6: COMPLEX MULTI-RESULT-SET PROCEDURES
-- ============================================

-- --------------------------------------------
-- uspGetRepracticeSessionByRepracticeSessionId
-- Returns 2 cursors: header + utterances
-- --------------------------------------------
CREATE OR REPLACE FUNCTION uspgetrepracticesessionbyid(
    p_repracticesessionid BIGINT
) RETURNS SETOF REFCURSOR AS $$
DECLARE
    ref1 REFCURSOR;
    ref2 REFCURSOR;
BEGIN
    OPEN ref1 FOR
    SELECT rps.repracticesessionid, rps.sourcesessionid, rps.status,
           rps.totalmistakes, rps.completedrounds,
           rps.improvementpercent, rps.generateddate
    FROM tblrepracticesession AS rps
    WHERE rps.repracticesessionid = p_repracticesessionid AND rps.isdeleted = FALSE;
    RETURN NEXT ref1;

    OPEN ref2 FOR
    SELECT rpu.repracticeutteranceid, rpu.mistakeid, rpu.originalutteranceid,
           rpu.englishtext, rpu.hinttext, rpu.mistaketype, rpu.mistakedetail,
           rpu.correctionnote, rpu.attemptcount, rpu.bestscore,
           rpu.lastscore, rpu.isresolved
    FROM tblrepracticeutterance AS rpu
    WHERE rpu.repracticesessionid = p_repracticesessionid AND rpu.isdeleted = FALSE
    ORDER BY rpu.sortorder ASC, rpu.repracticeutteranceid ASC;
    RETURN NEXT ref2;
END;
$$ LANGUAGE plpgsql;

-- --------------------------------------------
-- uspGetScriptDetailByScriptId
-- Returns 2 cursors: script metadata + utterances
-- --------------------------------------------
CREATE OR REPLACE FUNCTION uspgetscriptdetailbyscriptid(
    p_scriptid BIGINT
) RETURNS SETOF REFCURSOR AS $$
DECLARE
    ref1 REFCURSOR;
    ref2 REFCURSOR;
BEGIN
    OPEN ref1 FOR
    SELECT scr.scriptid, scr.scripttitle, scr.category, scr.grammarfocustag,
           scr.contexttag, scr.complexitylevel, scr.targetagegroup,
           scr.hintlanguage, scr.isactive, scr.uploadeddate,
           scr.uploadedbyuserid, scr.version, scr.utterancecount
    FROM tblscript AS scr
    WHERE scr.scriptid = p_scriptid AND scr.isdeleted = FALSE;
    RETURN NEXT ref1;

    OPEN ref2 FOR
    SELECT ut.utteranceid, ut.scriptid, ut.sequenceid, ut.speakerlabel,
           ut.englishtext, ut.hinttext, ut.grammartag, ut.contexttag,
           ut.focusword, ut.pronunciationnote
    FROM tblutterance AS ut
    WHERE ut.scriptid = p_scriptid AND ut.isdeleted = FALSE
    ORDER BY ut.sequenceid ASC, ut.utteranceid ASC;
    RETURN NEXT ref2;
END;
$$ LANGUAGE plpgsql;

-- --------------------------------------------
-- uspGetSessionByJoinCode
-- Returns session overview only; slot rows are loaded through uspGetAvailableSlotsBySessionId
-- --------------------------------------------
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

-- --------------------------------------------
-- uspGetSessionBySessionId
-- Returns 2 cursors: session info + active members
-- --------------------------------------------
CREATE OR REPLACE FUNCTION uspgetsessionbysessionid(
    p_sessionid BIGINT
) RETURNS SETOF REFCURSOR AS $$
DECLARE
    ref1 REFCURSOR;
    ref2 REFCURSOR;
BEGIN
    OPEN ref1 FOR
    SELECT ses.sessionid, ses.sessionname, ses.joincode, ses.sessionmode,
           scr.scripttitle, ses.maxmembers, ses.sessionduration
    FROM tblsession AS ses
    INNER JOIN tblscript AS scr ON scr.scriptid = ses.scriptid AND scr.isdeleted = FALSE
    WHERE ses.sessionid = p_sessionid AND ses.isdeleted = FALSE;
    RETURN NEXT ref1;

    OPEN ref2 FOR
    SELECT usr.userid, usr.fullname, usr.avatarurl,
           sem.slotindex, sem.slotname, sem.isready, sem.ishost
    FROM tblsessionmember AS sem
    INNER JOIN tbluser AS usr ON usr.userid = sem.userid AND usr.isdeleted = FALSE
    WHERE sem.sessionid = p_sessionid AND sem.isdeleted = FALSE AND sem.isactive = TRUE
    ORDER BY sem.slotindex ASC;
    RETURN NEXT ref2;
END;
$$ LANGUAGE plpgsql;

-- --------------------------------------------
-- uspGetAvailableSlotsBySessionId
-- TOP(@MaxMembers) in CTE → LIMIT v_maxmembers in subquery
-- --------------------------------------------
CREATE OR REPLACE FUNCTION uspgetavailableslotsbysessionid(
    p_sessionid BIGINT
) RETURNS TABLE (
    slotindex    SMALLINT,
    slotname     VARCHAR(64),
    isoccupied   BOOLEAN,
    userfullname VARCHAR(128),
    isready      BOOLEAN
) AS $$
DECLARE
    v_scriptid   BIGINT   := 0;
    v_maxmembers SMALLINT := 0;
BEGIN
    SELECT ses.scriptid, ses.maxmembers INTO v_scriptid, v_maxmembers
    FROM tblsession AS ses
    WHERE ses.sessionid = p_sessionid AND ses.isdeleted = FALSE;

    RETURN QUERY
    WITH speakerslot AS (
        SELECT ROW_NUMBER() OVER (ORDER BY lim.minsequenceid ASC, lim.speakerlabel ASC)::SMALLINT AS slotindex,
               lim.speakerlabel AS slotname
        FROM (
            SELECT ut.speakerlabel, MIN(ut.sequenceid) AS minsequenceid
            FROM tblutterance AS ut
            WHERE ut.scriptid = v_scriptid AND ut.isdeleted = FALSE
            GROUP BY ut.speakerlabel
            ORDER BY MIN(ut.sequenceid) ASC, ut.speakerlabel ASC
            LIMIT v_maxmembers
        ) AS lim
    )
    SELECT
        sps.slotindex,
        sps.slotname::VARCHAR(64),
        (sem.sessionmemberid IS NOT NULL)::BOOLEAN AS isoccupied,
        usr.fullname::VARCHAR(128)                  AS userfullname,
        COALESCE(sem.isready, FALSE)                AS isready
    FROM speakerslot AS sps
    LEFT JOIN tblsessionmember AS sem
        ON sem.sessionid = p_sessionid AND sem.slotindex = sps.slotindex
       AND sem.isdeleted = FALSE AND sem.isactive = TRUE
    LEFT JOIN tbluser AS usr ON usr.userid = sem.userid AND usr.isdeleted = FALSE
    ORDER BY sps.slotindex ASC;
END;
$$ LANGUAGE plpgsql;

-- --------------------------------------------
-- uspGetSessionDetailBySessionId
-- Returns 5 cursors (original had 5 result sets):
--   1: session header
--   2: my performance
--   3: my mistakes
--   4: listener feedback received
--   5: all member scores
-- --------------------------------------------
CREATE OR REPLACE FUNCTION uspgetsessiondetailbysessionid(
    p_sessionid BIGINT DEFAULT 0,
    p_userid    BIGINT DEFAULT 0
) RETURNS SETOF REFCURSOR AS $$
DECLARE
    ref1 REFCURSOR;
    ref2 REFCURSOR;
    ref3 REFCURSOR;
    ref4 REFCURSOR;
    ref5 REFCURSOR;
BEGIN
    -- cursor 1: session header
    OPEN ref1 FOR
    SELECT
        s.sessionname, s.sessionmode,
        s.datecreated AS sessiondate,
        s.sessionduration AS duration,
        sc.scripttitle,
        (SELECT COUNT(1) FROM tblsessionmember sm2
         WHERE sm2.sessionid = s.sessionid AND sm2.isdeleted = FALSE) AS membercount
    FROM tblsession s
    INNER JOIN tblscript sc ON sc.scriptid = s.scriptid AND sc.isdeleted = FALSE
    WHERE s.sessionid = p_sessionid AND s.isdeleted = FALSE;
    RETURN NEXT ref1;

    -- cursor 2: my performance
    OPEN ref2 FOR
    SELECT
        COALESCE(AVG(va.fluencyscore), 0)        AS fluencyscore,
        COALESCE(AVG(va.confidencescore), 0)     AS confidencescore,
        COALESCE(AVG(va.speakingspeedwpm), 0)    AS speakingspeedwpm,
        COALESCE(SUM(va.pausecount), 0)          AS pausecount
    FROM tblvoiceanalysis va
    WHERE va.sessionid = p_sessionid
      AND va.userid    = p_userid
      AND va.isdeleted = FALSE;
    RETURN NEXT ref2;

    -- cursor 3: my mistakes
    OPEN ref3 FOR
    SELECT
        m.mistakeid, m.utteranceid, m.scriptid,
        m.utterancetext, m.spokentext, m.mistaketype, m.mistakedetail,
        m.grammartag, m.contexttag, m.correctiontext,
        m.practicecount, m.isresolved, m.firstoccurrence, m.lastattempt
    FROM tblmistake m
    WHERE m.sessionid = p_sessionid
      AND m.userid    = p_userid
      AND m.isdeleted = FALSE
    ORDER BY m.mistakeid ASC;
    RETURN NEXT ref3;

    -- cursor 4: listener feedback received
    OPEN ref4 FOR
    SELECT lf.feedbacktag, COUNT(1) AS count
    FROM tbllistenerfeedback lf
    WHERE lf.sessionid    = p_sessionid
      AND lf.targetuserid = p_userid
      AND lf.isdeleted    = FALSE
    GROUP BY lf.feedbacktag
    ORDER BY COUNT(1) DESC;
    RETURN NEXT ref4;

    -- cursor 5: all member scores
    OPEN ref5 FOR
    SELECT
        u.userid, u.fullname,
        COALESCE(AVG(va.fluencyscore), 0)                              AS fluencyscore,
        COALESCE(AVG(va.confidencescore), 0)                           AS confidencescore,
        COUNT(DISTINCT m.mistakeid)                                    AS mistakecount,
        CAST(COALESCE(MAX(lf_agg.feedbackcount), 0) AS NUMERIC(18,6)) AS listenerrating
    FROM tblsessionmember sm
    INNER JOIN tbluser u ON u.userid = sm.userid AND u.isdeleted = FALSE
    LEFT JOIN tblvoiceanalysis va
        ON va.sessionid = sm.sessionid AND va.userid = sm.userid AND va.isdeleted = FALSE
    LEFT JOIN tblmistake m
        ON m.sessionid = sm.sessionid AND m.userid = sm.userid AND m.isdeleted = FALSE
    LEFT JOIN (
        SELECT lf2.targetuserid, COUNT(1) AS feedbackcount
        FROM tbllistenerfeedback lf2
        WHERE lf2.sessionid = p_sessionid AND lf2.isdeleted = FALSE
        GROUP BY lf2.targetuserid
    ) lf_agg ON lf_agg.targetuserid = u.userid
    WHERE sm.sessionid = p_sessionid AND sm.isdeleted = FALSE
    GROUP BY u.userid, u.fullname
    ORDER BY COALESCE(AVG(va.fluencyscore), 0) DESC;
    RETURN NEXT ref5;
END;
$$ LANGUAGE plpgsql;

-- --------------------------------------------
-- uspGetUserDashboardSummaryByUserId
-- Returns 3 cursors: dashboard summary + recent sessions + pending mistakes
-- OUTER APPLY TOP(1) → LEFT JOIN LATERAL ... LIMIT 1
-- --------------------------------------------
CREATE OR REPLACE FUNCTION uspgetuserdashboardsummarybyuserid(
    p_userid BIGINT
) RETURNS SETOF REFCURSOR AS $$
DECLARE
    ref1 REFCURSOR;
    ref2 REFCURSOR;
    ref3 REFCURSOR;
BEGIN
    -- cursor 1: dashboard summary + active session banner
    OPEN ref1 FOR
    SELECT
        usr.fullname AS username,
        usr.dailystreakcount AS currentstreak,
        CURRENT_DATE AS todaydate,
        (SELECT COUNT(1) FROM tblrepracticesession AS rps
         WHERE rps.userid = usr.userid AND rps.status = 'PENDING' AND rps.isdeleted = FALSE
        ) AS pendingrepracticecount,
        activesession.sessionid     AS activesessionid,
        activesession.sessionname   AS activesessionname,
        activesession.status        AS activesessionstatus,
        activesession.joincode
    FROM tbluser AS usr
    LEFT JOIN LATERAL (
        SELECT ses.sessionid, ses.sessionname, ses.status, ses.joincode
        FROM tblsessionmember AS sem
        INNER JOIN tblsession AS ses
            ON ses.sessionid = sem.sessionid AND ses.isdeleted = FALSE
        WHERE sem.userid    = usr.userid
          AND sem.isdeleted = FALSE
          AND sem.isactive  = TRUE
          AND ses.status IN ('LOBBY', 'ACTIVE')
        ORDER BY
            CASE WHEN ses.status = 'ACTIVE' THEN 0 ELSE 1 END,
            COALESCE(ses.starteddate, COALESCE(ses.roomexpiresat, ses.datecreated)) DESC,
            ses.sessionid DESC
        LIMIT 1
    ) AS activesession ON TRUE
    WHERE usr.userid    = p_userid
      AND usr.isdeleted = FALSE
      AND usr.isactive  = TRUE;
    RETURN NEXT ref1;

    -- cursor 2: recent 3 sessions
    OPEN ref2 FOR
    WITH recentsession AS (
        SELECT
            ses.sessionid, ses.sessionname, ses.sessionmode,
            COALESCE(ses.endeddate, COALESCE(ses.starteddate, ses.datecreated)) AS sessiondate,
            CASE
                WHEN ses.actualdurationsec IS NOT NULL AND ses.actualdurationsec > 0
                    THEN CEIL(ses.actualdurationsec / 60.0)::INT
                ELSE ses.sessionduration
            END AS duration,
            CAST(COALESCE(AVG(CAST(va.fluencyscore AS NUMERIC(10,2))), 0) AS NUMERIC(5,2)) AS fluencyscore,
            SUM(COALESCE(geo.errorcount, 0) + COALESCE(pro.errorcount, 0)) AS mistakecount,
            ses.status, scr.scripttitle
        FROM tblsessionmember AS sem
        INNER JOIN tblsession AS ses ON ses.sessionid = sem.sessionid AND ses.isdeleted = FALSE
        INNER JOIN tblscript  AS scr ON scr.scriptid  = ses.scriptid  AND scr.isdeleted = FALSE
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
        GROUP BY ses.sessionid, ses.sessionname, ses.sessionmode,
                 COALESCE(ses.endeddate, COALESCE(ses.starteddate, ses.datecreated)),
                 CASE WHEN ses.actualdurationsec IS NOT NULL AND ses.actualdurationsec > 0
                      THEN CEIL(ses.actualdurationsec / 60.0)::INT
                      ELSE ses.sessionduration END,
                 ses.status, scr.scripttitle
        ORDER BY COALESCE(ses.endeddate, COALESCE(ses.starteddate, ses.datecreated)) DESC,
                 ses.sessionid DESC
        LIMIT 3
    )
    SELECT rs.sessionid, rs.sessionname, rs.sessionmode, rs.sessiondate,
           rs.duration, rs.fluencyscore, rs.mistakecount, rs.status, rs.scripttitle
    FROM recentsession AS rs
    ORDER BY rs.sessiondate DESC, rs.sessionid DESC;
    RETURN NEXT ref2;

    -- cursor 3: top 3 pending mistakes
    OPEN ref3 FOR
    SELECT
        mst.mistakeid, mst.sessionid, mst.utteranceid, mst.scriptid,
        mst.utterancetext, mst.spokentext, mst.mistaketype, mst.mistakedetail,
        mst.grammartag, mst.contexttag, mst.correctiontext,
        mst.practicecount, mst.isresolved, mst.firstoccurrence, mst.lastattempt,
        ses.sessionname, scr.scripttitle
    FROM tblmistake AS mst
    LEFT JOIN tblsession AS ses ON ses.sessionid = mst.sessionid AND ses.isdeleted = FALSE
    LEFT JOIN tblscript  AS scr ON scr.scriptid  = mst.scriptid  AND scr.isdeleted = FALSE
    WHERE mst.userid     = p_userid
      AND mst.isresolved = FALSE
      AND mst.isdeleted  = FALSE
    ORDER BY mst.firstoccurrence DESC, mst.mistakeid DESC
    LIMIT 3;
    RETURN NEXT ref3;
END;
$$ LANGUAGE plpgsql;

-- --------------------------------------------
-- uspGetUserDetailByUserId
-- Returns 2 cursors: user detail with aggregates + recent 5 sessions
-- --------------------------------------------
CREATE OR REPLACE FUNCTION uspgetuserdetailbyuserid(
    p_userid BIGINT
) RETURNS SETOF REFCURSOR AS $$
DECLARE
    ref1 REFCURSOR;
    ref2 REFCURSOR;
BEGIN
    OPEN ref1 FOR
    SELECT
        usr.userid, usr.fullname, usr.mobilenumber, usr.email, usr.passwordhash,
        usr.agegroup, usr.preferredhintlanguage, usr.avatarurl, usr.groupcode, usr.role,
        usr.dailystreakcount, usr.totalsessionsplayed, usr.lastlogindate,
        usr.isactive, usr.registrationdate, usr.tag, usr.comments, usr.sortorder,
        usr.ipaddress, usr.createdby, usr.datecreated, usr.updatedby, usr.lastupdated,
        usr.deletedby, usr.datedeleted, usr.isdeleted,
        CAST(COALESCE(va.avgfluencyscore, 0) AS NUMERIC(5,2)) AS avgfluencyscore,
        COALESCE(mm.mostcommonmistaketype, '') AS mostcommonmistaketype
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
                   ROW_NUMBER() OVER (PARTITION BY counted.userid
                       ORDER BY counted.mistakecount DESC, counted.mistaketype ASC) AS rownumber
            FROM (
                SELECT mst.userid, mst.mistaketype, COUNT(1) AS mistakecount
                FROM tblmistake AS mst WHERE mst.isdeleted = FALSE
                GROUP BY mst.userid, mst.mistaketype
            ) AS counted
        ) AS ranked
        WHERE ranked.rownumber = 1
    ) AS mm ON mm.userid = usr.userid
    WHERE usr.userid = p_userid AND usr.isdeleted = FALSE;
    RETURN NEXT ref1;

    OPEN ref2 FOR
    SELECT
        ses.sessionid, ses.sessionname,
        COALESCE(ses.endeddate, COALESCE(ses.starteddate, ses.datecreated)) AS date,
        CASE
            WHEN ses.actualdurationsec IS NOT NULL AND ses.actualdurationsec > 0
                THEN CEIL(ses.actualdurationsec / 60.0)::INT
            ELSE ses.sessionduration
        END AS duration,
        CAST(COALESCE(va.avgfluencyscore, 0) AS NUMERIC(5,2)) AS fluencyscore,
        COALESCE(ms.mistakecount, 0) AS mistakecount
    FROM tblsession AS ses
    LEFT JOIN (
        SELECT va.sessionid, va.userid, AVG(va.fluencyscore) AS avgfluencyscore
        FROM tblvoiceanalysis AS va WHERE va.isdeleted = FALSE
        GROUP BY va.sessionid, va.userid
    ) AS va ON va.sessionid = ses.sessionid AND va.userid = p_userid
    LEFT JOIN (
        SELECT mst.sessionid, mst.userid, COUNT(1) AS mistakecount
        FROM tblmistake AS mst WHERE mst.isdeleted = FALSE
        GROUP BY mst.sessionid, mst.userid
    ) AS ms ON ms.sessionid = ses.sessionid AND ms.userid = p_userid
    WHERE ses.isdeleted = FALSE
      AND EXISTS (
          SELECT 1 FROM tblsessionmember AS sm
          WHERE sm.sessionid = ses.sessionid AND sm.userid = p_userid AND sm.isdeleted = FALSE
      )
    ORDER BY COALESCE(ses.endeddate, COALESCE(ses.starteddate, ses.datecreated)) DESC, ses.sessionid DESC
    LIMIT 5;
    RETURN NEXT ref2;
END;
$$ LANGUAGE plpgsql;

-- --------------------------------------------
-- uspGetUserFullReportByUserId
-- Returns 4 cursors: profile summary + sessions + grammar tags + weekly scores
-- --------------------------------------------
CREATE OR REPLACE FUNCTION uspgetuserfulreportbyuserid(
    p_userid BIGINT
) RETURNS SETOF REFCURSOR AS $$
DECLARE
    ref1 REFCURSOR;
    ref2 REFCURSOR;
    ref3 REFCURSOR;
    ref4 REFCURSOR;
BEGIN
    -- cursor 1: user summary
    OPEN ref1 FOR
    SELECT
        usr.userid, usr.avatarurl, usr.fullname, usr.dailystreakcount,
        COUNT(DISTINCT sm.sessionid) AS totalsessions,
        CAST(COALESCE(AVG(va.fluencyscore), 0) AS NUMERIC(5,2)) AS avgscore
    FROM tbluser AS usr
    LEFT JOIN tblsessionmember AS sm ON sm.userid = usr.userid AND sm.isdeleted = FALSE
    LEFT JOIN tblvoiceanalysis AS va ON va.userid = usr.userid AND va.isdeleted = FALSE
    WHERE usr.userid = p_userid AND usr.isdeleted = FALSE
    GROUP BY usr.userid, usr.avatarurl, usr.fullname, usr.dailystreakcount;
    RETURN NEXT ref1;

    -- cursor 2: all sessions
    OPEN ref2 FOR
    SELECT
        ses.sessionid, ses.sessionname,
        COALESCE(ses.endeddate, COALESCE(ses.starteddate, ses.datecreated)) AS date,
        CASE
            WHEN ses.actualdurationsec IS NOT NULL AND ses.actualdurationsec > 0
                THEN CEIL(ses.actualdurationsec / 60.0)::INT
            ELSE ses.sessionduration
        END AS duration,
        CAST(COALESCE(vag.avgfluencyscore, 0) AS NUMERIC(5,2)) AS fluencyscore,
        COALESCE(msg.mistakecount, 0) AS mistakecount
    FROM tblsession AS ses
    INNER JOIN tblsessionmember AS sm
        ON sm.sessionid = ses.sessionid AND sm.userid = p_userid AND sm.isdeleted = FALSE
    LEFT JOIN (
        SELECT va.sessionid, va.userid, AVG(va.fluencyscore) AS avgfluencyscore
        FROM tblvoiceanalysis AS va WHERE va.isdeleted = FALSE
        GROUP BY va.sessionid, va.userid
    ) AS vag ON vag.sessionid = ses.sessionid AND vag.userid = p_userid
    LEFT JOIN (
        SELECT mst.sessionid, mst.userid, COUNT(1) AS mistakecount
        FROM tblmistake AS mst WHERE mst.isdeleted = FALSE
        GROUP BY mst.sessionid, mst.userid
    ) AS msg ON msg.sessionid = ses.sessionid AND msg.userid = p_userid
    WHERE ses.isdeleted = FALSE
    ORDER BY COALESCE(ses.endeddate, COALESCE(ses.starteddate, ses.datecreated)) DESC, ses.sessionid DESC;
    RETURN NEXT ref2;

    -- cursor 3: grammar tag breakdown
    OPEN ref3 FOR
    SELECT COALESCE(mst.grammartag, '') AS grammartag, COUNT(1) AS mistakecount
    FROM tblmistake AS mst
    WHERE mst.userid = p_userid AND mst.isdeleted = FALSE AND mst.grammartag IS NOT NULL
    GROUP BY mst.grammartag
    ORDER BY COUNT(1) DESC, mst.grammartag ASC;
    RETURN NEXT ref3;

    -- cursor 4: weekly fluency (last 4 weeks)
    OPEN ref4 FOR
    WITH weeklyscore AS (
        SELECT
            DATE_TRUNC('week', COALESCE(ses.starteddate, ses.datecreated))::DATE AS weekstartdate,
            AVG(va.fluencyscore) AS avgfluencyscore
        FROM tblvoiceanalysis AS va
        INNER JOIN tblsession AS ses ON ses.sessionid = va.sessionid AND ses.isdeleted = FALSE
        WHERE va.userid    = p_userid
          AND va.isdeleted = FALSE
          AND COALESCE(ses.starteddate, ses.datecreated) >= NOW() - INTERVAL '4 weeks'
        GROUP BY DATE_TRUNC('week', COALESCE(ses.starteddate, ses.datecreated))::DATE
    )
    SELECT
        TO_CHAR(ws.weekstartdate, 'YYYY-MM-DD') AS weeklabel,
        CAST(ws.avgfluencyscore AS NUMERIC(5,2)) AS avgfluencyscore
    FROM weeklyscore AS ws
    ORDER BY ws.weekstartdate ASC;
    RETURN NEXT ref4;
END;
$$ LANGUAGE plpgsql;

-- --------------------------------------------
-- uspExportUserReportData
-- Returns 2 cursors: summary per user + session detail rows
-- --------------------------------------------
CREATE OR REPLACE FUNCTION uspexportuserreportdata(
    p_fromdate TIMESTAMPTZ DEFAULT NULL,
    p_todate   TIMESTAMPTZ DEFAULT NULL,
    p_userid   BIGINT      DEFAULT NULL
) RETURNS SETOF REFCURSOR AS $$
DECLARE
    ref1 REFCURSOR;
    ref2 REFCURSOR;
BEGIN
    OPEN ref1 FOR
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
          AND (p_todate   IS NULL OR COALESCE(ses.starteddate, ses.datecreated) <  p_todate + INTERVAL '1 day')
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
          AND (p_todate   IS NULL OR COALESCE(ses.starteddate, ses.datecreated) <  p_todate + INTERVAL '1 day')
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
        ur.userid, ur.fullname, ur.totalsessions, ur.avgscore,
        COALESCE(ms.totalmistakes, 0) AS totalmistakes,
        CAST(CASE
            WHEN COALESCE(ms.totalmistakes, 0) = 0 THEN 0
            ELSE (COALESCE(ms.resolvedmistakes, 0) * 100.0) / ms.totalmistakes
        END AS NUMERIC(6,2)) AS improvementpercent
    FROM userreport AS ur
    LEFT JOIN mistakesummary AS ms ON ms.userid = ur.userid
    ORDER BY ur.fullname ASC, ur.userid ASC;
    RETURN NEXT ref1;

    OPEN ref2 FOR
    SELECT
        usr.userid, usr.fullname,
        ses.sessionid, ses.sessionname,
        COALESCE(ses.starteddate, ses.datecreated) AS sessiondate,
        CAST(COALESCE(vag.avgfluencyscore, 0) AS NUMERIC(5,2)) AS fluencyscore,
        COALESCE(msg.mistakecount, 0) AS mistakecount,
        ses.status
    FROM tbluser AS usr
    INNER JOIN tblsessionmember AS sm ON sm.userid = usr.userid AND sm.isdeleted = FALSE
    INNER JOIN tblsession AS ses ON ses.sessionid = sm.sessionid AND ses.isdeleted = FALSE
    LEFT JOIN (
        SELECT va.sessionid, va.userid, AVG(va.fluencyscore) AS avgfluencyscore
        FROM tblvoiceanalysis AS va WHERE va.isdeleted = FALSE
        GROUP BY va.sessionid, va.userid
    ) AS vag ON vag.sessionid = ses.sessionid AND vag.userid = usr.userid
    LEFT JOIN (
        SELECT mst.sessionid, mst.userid, COUNT(1) AS mistakecount
        FROM tblmistake AS mst WHERE mst.isdeleted = FALSE
        GROUP BY mst.sessionid, mst.userid
    ) AS msg ON msg.sessionid = ses.sessionid AND msg.userid = usr.userid
    WHERE usr.isdeleted = FALSE
      AND (p_userid   IS NULL OR usr.userid = p_userid)
      AND (p_fromdate IS NULL OR COALESCE(ses.starteddate, ses.datecreated) >= p_fromdate)
      AND (p_todate   IS NULL OR COALESCE(ses.starteddate, ses.datecreated) <  p_todate + INTERVAL '1 day')
    ORDER BY usr.fullname ASC,
             COALESCE(ses.starteddate, ses.datecreated) DESC,
             ses.sessionid DESC;
    RETURN NEXT ref2;
END;
$$ LANGUAGE plpgsql;

COMMIT;
