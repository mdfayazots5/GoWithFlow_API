-- Phase 17 (Phase 1 of build plan): AI Voice Participant — stored functions
-- PostgreSQL (Supabase). Run AFTER AddAIVoiceParticipant_Phase17.sql (needs the isai +
-- ai* columns). See Backend/Docs/Dev/AIVoiceParticipantArchitecture.md.
--
-- These are ADDITIVE functions — the core uspinsertsession / uspinsertsessionmember are left
-- untouched so human create/join flows carry zero regression risk.

-- --------------------------------------------
-- uspInsertAiSessionMember
-- Inserts one AI-held member (isai=TRUE, isready=TRUE, ishost=FALSE).
-- NO duplicate-user guard (unlike uspinsertsessionmember): the single reserved AI system user
-- can legitimately hold multiple slots in one session (multi-role scripts). The slot-occupied
-- guard still applies.
-- --------------------------------------------
CREATE OR REPLACE FUNCTION uspinsertaisessionmember(
    p_sessionid BIGINT,
    p_userid    BIGINT,
    p_slotindex SMALLINT,
    p_slotname  VARCHAR(64),
    p_createdby VARCHAR(128),
    p_ipaddress VARCHAR(64)
) RETURNS VOID AS $$
BEGIN
    IF EXISTS (
        SELECT 1 FROM tblsessionmember
        WHERE sessionid = p_sessionid AND slotindex = p_slotindex
          AND isdeleted = FALSE AND isactive = TRUE
    ) THEN
        RAISE EXCEPTION 'The requested session slot is already occupied.' USING ERRCODE = 'P0001';
    END IF;

    INSERT INTO tblsessionmember (
        sessionid, userid, slotindex, slotname, isready, ishost, isai,
        joinedat, isactive, createdby, ipaddress
    )
    VALUES (
        p_sessionid, p_userid, p_slotindex, p_slotname, TRUE, FALSE, TRUE,
        NOW(), TRUE, p_createdby, p_ipaddress
    );
END;
$$ LANGUAGE plpgsql;

-- --------------------------------------------
-- uspSetSessionAiConfig
-- Persists the per-session AI config on tblsession. Called only when AI is enabled, so the
-- three settings are always non-null (enforced by CreateSessionRequestValidator).
-- --------------------------------------------
-- Drop the prior 7-arg overload before recreating with the added p_aivoicename (2026-06-18).
DROP FUNCTION IF EXISTS uspsetsessionaiconfig(BIGINT, BOOLEAN, VARCHAR, DECIMAL, INT, VARCHAR, VARCHAR);

CREATE OR REPLACE FUNCTION uspsetsessionaiconfig(
    p_sessionid          BIGINT,
    p_aienabled          BOOLEAN,
    p_aivoicegender      VARCHAR(8),
    p_aivoicename        VARCHAR(32),
    p_aispeechrate       DECIMAL(3,2),
    p_aiquestiondelaysec INT,
    p_updatedby          VARCHAR(128),
    p_ipaddress          VARCHAR(64)
) RETURNS VOID AS $$
BEGIN
    UPDATE tblsession
    SET aienabled          = p_aienabled,
        aivoicegender      = p_aivoicegender,
        aivoicename        = p_aivoicename,
        aispeechrate       = p_aispeechrate,
        aiquestiondelaysec = p_aiquestiondelaysec,
        updatedby          = p_updatedby,
        lastupdated        = NOW(),
        ipaddress          = p_ipaddress
    WHERE sessionid = p_sessionid
      AND isdeleted = FALSE;
END;
$$ LANGUAGE plpgsql;
