-- Migration 39: Question & Answer "Hard Words" practice aid (2026-06-18).
-- Adds tblutterance.hardwords (Excel Column I: pipe-separated word:meaning pairs, Q&A Interviewer rows)
-- and tblsession.showhardwords (per-session toggle). Extends the utterance insert routines to carry
-- hardwords, and uspsetsessionaiconfig to persist showhardwords (Q&A always runs with AI on, so the
-- flag rides the existing AI-config write). Additive + idempotent.
-- SQL Server parity: Backend/Docs/SqlServerSeed/39_add_hard_words.sql.

-- 1) New columns.
ALTER TABLE public.tblutterance
  ADD COLUMN IF NOT EXISTS hardwords VARCHAR(1024) NULL;

ALTER TABLE public.tblsession
  ADD COLUMN IF NOT EXISTS showhardwords BOOLEAN NOT NULL DEFAULT FALSE;

-- 2) Bulk insert (JSONB) — read the optional HardWords element. Signature unchanged.
CREATE OR REPLACE FUNCTION uspbulkinsertutterance(
    p_scriptid    BIGINT,
    p_utterances  JSONB,
    p_createdby   VARCHAR(128),
    p_ipaddress   VARCHAR(64)
) RETURNS VOID AS $$
BEGIN
    INSERT INTO tblutterance (
        scriptid, sequenceid, speakerlabel, englishtext, hinttext,
        grammartag, contexttag, focusword, pronunciationnote, hardwords, createdby, ipaddress
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
        elem->>'HardWords',
        p_createdby,
        p_ipaddress
    FROM jsonb_array_elements(p_utterances) AS elem;
END;
$$ LANGUAGE plpgsql;

-- 3) Single insert — add p_hardwords (positional, after pronunciationnote). Signature changes → drop first.
DROP FUNCTION IF EXISTS uspinsertutterance(BIGINT, INT, VARCHAR, VARCHAR, VARCHAR, VARCHAR, VARCHAR, VARCHAR, VARCHAR, VARCHAR, VARCHAR);

CREATE OR REPLACE FUNCTION uspinsertutterance(
    p_scriptid          BIGINT,
    p_sequenceid        INT,
    p_speakerlabel      VARCHAR(64),
    p_englishtext       VARCHAR(512),
    p_hinttext          VARCHAR(512)  DEFAULT NULL,
    p_grammartag        VARCHAR(64)   DEFAULT NULL,
    p_contexttag        VARCHAR(64)   DEFAULT NULL,
    p_focusword         VARCHAR(64)   DEFAULT NULL,
    p_pronunciationnote VARCHAR(256)  DEFAULT NULL,
    p_hardwords         VARCHAR(1024) DEFAULT NULL,
    p_createdby         VARCHAR(128)  DEFAULT NULL,
    p_ipaddress         VARCHAR(64)   DEFAULT NULL
) RETURNS VOID AS $$
BEGIN
    INSERT INTO tblutterance (
        scriptid, sequenceid, speakerlabel, englishtext, hinttext,
        grammartag, contexttag, focusword, pronunciationnote, hardwords, createdby, ipaddress
    )
    VALUES (
        p_scriptid, p_sequenceid, p_speakerlabel, p_englishtext, p_hinttext,
        p_grammartag, p_contexttag, p_focusword, p_pronunciationnote, p_hardwords, p_createdby, p_ipaddress
    );
END;
$$ LANGUAGE plpgsql;

-- 4) Session AI config — add p_showhardwords (positional, after aiquestiondelaysec). Signature changes → drop first.
DROP FUNCTION IF EXISTS uspsetsessionaiconfig(BIGINT, BOOLEAN, VARCHAR, VARCHAR, DECIMAL, INT, VARCHAR, VARCHAR);

CREATE OR REPLACE FUNCTION uspsetsessionaiconfig(
    p_sessionid          BIGINT,
    p_aienabled          BOOLEAN,
    p_aivoicegender      VARCHAR(8),
    p_aivoicename        VARCHAR(32),
    p_aispeechrate       DECIMAL(3,2),
    p_aiquestiondelaysec INT,
    p_showhardwords      BOOLEAN,
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
        showhardwords      = p_showhardwords,
        updatedby          = p_updatedby,
        lastupdated        = NOW(),
        ipaddress          = p_ipaddress
    WHERE sessionid = p_sessionid
      AND isdeleted = FALSE;
END;
$$ LANGUAGE plpgsql;
