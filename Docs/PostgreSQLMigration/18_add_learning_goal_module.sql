-- Migration 18: Add tblusergoal for Learning Goals (Phase 2 Step 4)
-- Apply directly to Supabase (PostgreSQL).

CREATE TABLE IF NOT EXISTS public.tblusergoal (
    usergoalid      BIGSERIAL       NOT NULL,
    userid          BIGINT          NOT NULL,
    goaltype        VARCHAR(64)     NOT NULL,
    timelineweeks   INT             NOT NULL,
    startdate       TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    targetdate      TIMESTAMPTZ     NOT NULL,
    detectedlevel   VARCHAR(32)     NOT NULL DEFAULT 'Intermediate',
    startingscore   NUMERIC(5,2)    NOT NULL DEFAULT 0,
    isactive        BOOLEAN         NOT NULL DEFAULT TRUE,
    sortorder       INT             NOT NULL DEFAULT 0,
    ipaddress       VARCHAR(64)     NOT NULL DEFAULT '127.0.0.1',
    createdby       VARCHAR(128)    NOT NULL DEFAULT 'System',
    datecreated     TIMESTAMPTZ     NOT NULL DEFAULT NOW(),
    updatedby       VARCHAR(128)    NULL,
    lastupdated     TIMESTAMPTZ     NULL,
    deletedby       VARCHAR(128)    NULL,
    datedeleted     TIMESTAMPTZ     NULL,
    isdeleted       BOOLEAN         NOT NULL DEFAULT FALSE,
    CONSTRAINT pk_tblusergoal_usergoalid PRIMARY KEY (usergoalid),
    CONSTRAINT fk_tblusergoal_userid_tbluser_userid
        FOREIGN KEY (userid) REFERENCES public.tbluser(userid) ON DELETE CASCADE
);

CREATE INDEX IF NOT EXISTS idx_tblusergoal_userid
    ON public.tblusergoal (userid);

CREATE INDEX IF NOT EXISTS idx_tblusergoal_userid_isactive
    ON public.tblusergoal (userid, isactive);
