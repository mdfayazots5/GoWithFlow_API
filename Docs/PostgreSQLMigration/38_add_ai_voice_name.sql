-- Migration 38: Add the named Indian voice persona to the AI Voice Participant (2026-06-18).
-- Adds tblsession.aivoicename and extends uspsetsessionaiconfig to persist it.
-- Additive + idempotent. SQL Server parity: Migrations/SqlServer/AddAIVoiceParticipant_Phase17*.sql.

-- 1) New column (legacy aivoicegender kept for back-compat).
ALTER TABLE public.tblsession
  ADD COLUMN IF NOT EXISTS aivoicename VARCHAR(32) NULL;

-- 2) Recreate the config function with the added p_aivoicename (positional 4th arg, after gender).
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
