-- Migration 41: Question & Answer "Show key words while answering" toggle (2026-06-18).
-- Adds tblsession.showhardwordsinanswer (per-session, Q&A-only) so the question's key words can stay
-- visible on the candidate's own answer turn. Extends uspsetsessionaiconfig to persist it (Q&A always
-- runs with AI on, so the flag rides the existing AI-config write, right after showhardwords).
-- Additive + idempotent. SQL Server parity: Backend/Docs/SqlServerSeed/41_add_hard_words_in_answer.sql.

-- 1) New column.
ALTER TABLE public.tblsession
  ADD COLUMN IF NOT EXISTS showhardwordsinanswer BOOLEAN NOT NULL DEFAULT FALSE;

-- 2) Session AI config — add p_showhardwordsinanswer (positional, after p_showhardwords). Signature
--    changes → drop the previous (Migration 39) signature first.
DROP FUNCTION IF EXISTS uspsetsessionaiconfig(BIGINT, BOOLEAN, VARCHAR, VARCHAR, DECIMAL, INT, BOOLEAN, VARCHAR, VARCHAR);

CREATE OR REPLACE FUNCTION uspsetsessionaiconfig(
    p_sessionid             BIGINT,
    p_aienabled             BOOLEAN,
    p_aivoicegender         VARCHAR(8),
    p_aivoicename           VARCHAR(32),
    p_aispeechrate          DECIMAL(3,2),
    p_aiquestiondelaysec    INT,
    p_showhardwords         BOOLEAN,
    p_showhardwordsinanswer BOOLEAN,
    p_updatedby             VARCHAR(128),
    p_ipaddress             VARCHAR(64)
) RETURNS VOID AS $$
BEGIN
    UPDATE tblsession
    SET aienabled             = p_aienabled,
        aivoicegender         = p_aivoicegender,
        aivoicename           = p_aivoicename,
        aispeechrate          = p_aispeechrate,
        aiquestiondelaysec    = p_aiquestiondelaysec,
        showhardwords         = p_showhardwords,
        showhardwordsinanswer = p_showhardwordsinanswer,
        updatedby             = p_updatedby,
        lastupdated           = NOW(),
        ipaddress             = p_ipaddress
    WHERE sessionid = p_sessionid
      AND isdeleted = FALSE;
END;
$$ LANGUAGE plpgsql;
