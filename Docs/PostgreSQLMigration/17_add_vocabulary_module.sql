-- Migration 17: Add tbluservocabulary for Phase 1 Vocabulary Retention Tracker
-- Apply this script directly to Supabase PostgreSQL.
-- Corresponding EF migration: 20260601000001_AddVocabularyModule_Phase8

CREATE TABLE IF NOT EXISTS public.tbluservocabulary (
    uservocabularyid    BIGSERIAL       NOT NULL,
    userid              BIGINT          NOT NULL,
    focusword           VARCHAR(64)     NOT NULL,
    sourcesessionid     BIGINT          NOT NULL,
    dateintroduced      TIMESTAMPTZ     NOT NULL    DEFAULT NOW(),
    wasproducedcorrectly BOOLEAN        NOT NULL    DEFAULT FALSE,
    timesencountered    INTEGER         NOT NULL    DEFAULT 1,
    tag                 VARCHAR(64)     NULL,
    comments            VARCHAR(256)    NULL,
    sortorder           INTEGER         NOT NULL    DEFAULT 0,
    ipaddress           VARCHAR(64)     NOT NULL    DEFAULT '127.0.0.1',
    createdby           VARCHAR(128)    NOT NULL    DEFAULT 'System',
    datecreated         TIMESTAMPTZ     NOT NULL    DEFAULT NOW(),
    updatedby           VARCHAR(128)    NULL,
    lastupdated         TIMESTAMPTZ     NULL,
    deletedby           VARCHAR(128)    NULL,
    datedeleted         TIMESTAMPTZ     NULL,
    isdeleted           BOOLEAN         NOT NULL    DEFAULT FALSE,
    CONSTRAINT pk_tbluservocabulary_uservocabularyid PRIMARY KEY (uservocabularyid),
    CONSTRAINT fk_tbluservocabulary_userid_tbluser_userid
        FOREIGN KEY (userid) REFERENCES public.tbluser(userid) ON DELETE CASCADE,
    CONSTRAINT fk_tbluservocabulary_sourcesessionid_tblsession_sessionid
        FOREIGN KEY (sourcesessionid) REFERENCES public.tblsession(sessionid),
    CONSTRAINT uk_tbluservocabulary_userid_focusword_sessionid
        UNIQUE (userid, focusword, sourcesessionid)
);

CREATE INDEX IF NOT EXISTS idx_tbluservocabulary_userid
    ON public.tbluservocabulary (userid);

CREATE INDEX IF NOT EXISTS idx_tbluservocabulary_userid_focusword
    ON public.tbluservocabulary (userid, focusword);
