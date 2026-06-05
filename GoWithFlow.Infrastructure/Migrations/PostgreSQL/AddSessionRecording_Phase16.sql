-- Phase 16: Consolidated Session Recording
-- PostgreSQL (Supabase) — column names are all lowercase per EF/raw-SQL convention.
--
-- Replaces the fragmented per-turn admin recordings view with ONE consolidated
-- recording per session, merged server-side from the existing per-turn audio segments
-- (tblaudioarchive). See Backend/Docs/Dev/SessionRecordingArchitecture.md.

-- 1) Persist the host "Record Session" flag on the session itself.
--    (Previously this lived only in the host's browser localStorage — the backend had no
--    way to know recording was enabled. Without this column no merge can be triggered.)
ALTER TABLE public.tblsession
  ADD COLUMN IF NOT EXISTS recordingenabled BOOLEAN NOT NULL DEFAULT FALSE;

-- 2) One consolidated recording per session.
CREATE TABLE IF NOT EXISTS public.tblsessionrecording (
    recordingid       BIGSERIAL    PRIMARY KEY,
    sessionid         BIGINT       NOT NULL,
    storagekey        VARCHAR(512) NULL,          -- final R2 key; NULL until merge succeeds
    status            VARCHAR(20)  NOT NULL DEFAULT 'PENDING_MERGE',
                                                  -- CAPTURING|PENDING_MERGE|PROCESSING|READY|FAILED
    format            VARCHAR(8)   NULL,
    durationsecs      INT          NULL,
    sizebytes         BIGINT       NULL,
    segmentcount      INT          NULL,
    participantsjson  JSONB        NULL,          -- [{userId,name,turns}] denormalized for admin
    failurereason     VARCHAR(512) NULL,
    attemptcount      INT          NOT NULL DEFAULT 0,
    createdat         TIMESTAMP    NOT NULL DEFAULT (now() AT TIME ZONE 'utc'),  -- = session start
    completedat       TIMESTAMP    NULL,
    expiresat         TIMESTAMP    NULL,
    isdeleted         BOOLEAN      NOT NULL DEFAULT FALSE
);

-- Exactly one (non-deleted) recording per session.
CREATE UNIQUE INDEX IF NOT EXISTS ux_tblsessionrecording_sessionid
    ON public.tblsessionrecording (sessionid)
    WHERE isdeleted = FALSE;

-- Worker scan: find rows still needing a merge / retry.
CREATE INDEX IF NOT EXISTS ix_tblsessionrecording_status
    ON public.tblsessionrecording (status)
    WHERE isdeleted = FALSE;
