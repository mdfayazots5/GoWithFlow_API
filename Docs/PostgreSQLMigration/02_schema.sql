-- ============================================
-- File: 02_schema.sql
-- Description: All CREATE TABLE statements converted from SQL Server GoWithFlowDB
--              Tables in FK dependency order (parents first)
-- Run order: 2 of 10
-- Dependencies: 01_extensions.sql
-- ============================================
-- Tables migrated: 18
--   tblUser, tblScript, tblRefreshToken, tblUserBadge, tblUserStreak,
--   tblDashboardMetric, tblOtpVerification, tblUtterance, tblSession,
--   tblScriptVersion, tblAdminNote, tblSessionMember, tblListenerFeedback,
--   tblTurnState, tblMistake, tblVoiceAnalysis, tblRepracticeSession,
--   tblRepracticeUtterance
-- Views migrated: 0
-- Functions migrated: N/A
-- Stored Procedures migrated: N/A
-- Triggers migrated: 0
-- Indexes migrated: N/A (see 03_constraints_indexes.sql)
-- Known incompatibilities:
--   - UtteranceTVP (SQL Server table-valued parameter) has no direct PostgreSQL
--     equivalent; replaced with temporary table approach in functions file
--   - tblOtpVerification did not exist as a table in the source DB but is
--     required by uspInsertOtpVerification and uspVerifyOtp SPs; schema inferred
--     from stored procedure parameters
-- Manual review required:
--   - tblOtpVerification schema inferred from SP usage — verify column types
-- ============================================

BEGIN;

SET search_path TO public;

-- ============================================
-- TABLE: tbluser
-- Root entity — no FK dependencies
-- ============================================
CREATE TABLE IF NOT EXISTS tbluser
(
    userid                  BIGINT GENERATED ALWAYS AS IDENTITY NOT NULL,
    fullname                VARCHAR(128) NOT NULL,
    mobilenumber            VARCHAR(16) NOT NULL,
    email                   VARCHAR(128) NULL,
    passwordhash            VARCHAR(512) NULL,
    agegroup                VARCHAR(32) NOT NULL,
    preferredhintlanguage   VARCHAR(32) NOT NULL,
    avatarurl               VARCHAR(256) NULL,
    groupcode               VARCHAR(32) NULL,
    role                    VARCHAR(16) NOT NULL DEFAULT 'USER',
    dailystreakcount        INTEGER NOT NULL DEFAULT 0,
    totalsessionsplayed     INTEGER NOT NULL DEFAULT 0,
    lastlogindate           TIMESTAMP NULL,
    isactive                BOOLEAN NOT NULL DEFAULT TRUE,
    registrationdate        TIMESTAMP NOT NULL DEFAULT NOW(),
    tag                     VARCHAR(64) NULL,
    comments                VARCHAR(256) NULL,
    sortorder               INTEGER NOT NULL DEFAULT 0,
    ipaddress               VARCHAR(64) NOT NULL DEFAULT '127.0.0.1',
    createdby               VARCHAR(128) NOT NULL DEFAULT 'Admin',
    datecreated             TIMESTAMP NOT NULL DEFAULT NOW(),
    updatedby               VARCHAR(128) NULL,
    lastupdated             TIMESTAMP NULL,
    deletedby               VARCHAR(128) NULL,
    datedeleted             TIMESTAMP NULL,
    isdeleted               BOOLEAN NOT NULL DEFAULT FALSE,

    CONSTRAINT pk_tbluser_userid PRIMARY KEY (userid)
);

-- ============================================
-- TABLE: tblscript
-- FK dependency: tbluser (uploadedbyuserid)
-- ============================================
CREATE TABLE IF NOT EXISTS tblscript
(
    scriptid                BIGINT GENERATED ALWAYS AS IDENTITY NOT NULL,
    scripttitle             VARCHAR(128) NOT NULL,
    category                VARCHAR(64) NOT NULL,
    grammarfocustag         VARCHAR(64) NOT NULL,
    contexttag              VARCHAR(64) NOT NULL,
    complexitylevel         SMALLINT NOT NULL,
    targetagegroup          VARCHAR(32) NOT NULL,
    hintlanguage            VARCHAR(32) NOT NULL,
    isactive                BOOLEAN NOT NULL DEFAULT TRUE,
    uploadeddate            TIMESTAMP NOT NULL DEFAULT NOW(),
    uploadedbyuserid        BIGINT NOT NULL,
    version                 INTEGER NOT NULL DEFAULT 1,
    utterancecount          INTEGER NOT NULL DEFAULT 0,
    tag                     VARCHAR(64) NULL,
    comments                VARCHAR(256) NULL,
    sortorder               INTEGER NOT NULL DEFAULT 0,
    ipaddress               VARCHAR(64) NOT NULL DEFAULT '127.0.0.1',
    createdby               VARCHAR(128) NOT NULL DEFAULT 'Admin',
    datecreated             TIMESTAMP NOT NULL DEFAULT NOW(),
    updatedby               VARCHAR(128) NULL,
    lastupdated             TIMESTAMP NULL,
    deletedby               VARCHAR(128) NULL,
    datedeleted             TIMESTAMP NULL,
    isdeleted               BOOLEAN NOT NULL DEFAULT FALSE,

    CONSTRAINT pk_tblscript_scriptid PRIMARY KEY (scriptid)
);

-- ============================================
-- TABLE: tblrefreshtoken
-- FK dependency: tbluser (userid)
-- ============================================
CREATE TABLE IF NOT EXISTS tblrefreshtoken
(
    refreshtokenid          BIGINT GENERATED ALWAYS AS IDENTITY NOT NULL,
    userid                  BIGINT NOT NULL,
    token                   VARCHAR(512) NOT NULL,
    expiresat               TIMESTAMP NOT NULL,
    isrevoked               BOOLEAN NOT NULL DEFAULT FALSE,
    revokedat               TIMESTAMP NULL,
    deviceinfo              VARCHAR(256) NULL,
    tag                     VARCHAR(64) NULL,
    comments                VARCHAR(256) NULL,
    sortorder               INTEGER NOT NULL DEFAULT 0,
    ipaddress               VARCHAR(64) NOT NULL DEFAULT '127.0.0.1',
    createdby               VARCHAR(128) NOT NULL DEFAULT 'Admin',
    datecreated             TIMESTAMP NOT NULL DEFAULT NOW(),
    updatedby               VARCHAR(128) NULL,
    lastupdated             TIMESTAMP NULL,
    deletedby               VARCHAR(128) NULL,
    datedeleted             TIMESTAMP NULL,
    isdeleted               BOOLEAN NOT NULL DEFAULT FALSE,

    CONSTRAINT pk_tblrefreshtoken_refreshtokenid PRIMARY KEY (refreshtokenid)
);

-- ============================================
-- TABLE: tbluserbadge
-- FK dependency: tbluser (userid)
-- ============================================
CREATE TABLE IF NOT EXISTS tbluserbadge
(
    userbadgeid             BIGINT GENERATED ALWAYS AS IDENTITY NOT NULL,
    userid                  BIGINT NOT NULL,
    badgecode               VARCHAR(64) NOT NULL,
    badgename               VARCHAR(128) NOT NULL,
    earneddate              TIMESTAMP NOT NULL DEFAULT NOW(),
    tag                     VARCHAR(64) NULL,
    comments                VARCHAR(256) NULL,
    sortorder               INTEGER NOT NULL DEFAULT 0,
    ipaddress               VARCHAR(64) NOT NULL DEFAULT '127.0.0.1',
    createdby               VARCHAR(128) NOT NULL DEFAULT 'Admin',
    datecreated             TIMESTAMP NOT NULL DEFAULT NOW(),
    updatedby               VARCHAR(128) NULL,
    lastupdated             TIMESTAMP NULL,
    deletedby               VARCHAR(128) NULL,
    datedeleted             TIMESTAMP NULL,
    isdeleted               BOOLEAN NOT NULL DEFAULT FALSE,

    CONSTRAINT pk_tbluserbadge_userbadgeid PRIMARY KEY (userbadgeid)
);

-- ============================================
-- TABLE: tbluserstreak
-- FK dependency: tbluser (userid)
-- ============================================
CREATE TABLE IF NOT EXISTS tbluserstreak
(
    userstreakid            BIGINT GENERATED ALWAYS AS IDENTITY NOT NULL,
    userid                  BIGINT NOT NULL,
    streakdate              DATE NOT NULL,
    sessioncount            INTEGER NOT NULL DEFAULT 0,
    practiceminutes         INTEGER NOT NULL DEFAULT 0,
    tag                     VARCHAR(64) NULL,
    comments                VARCHAR(256) NULL,
    sortorder               INTEGER NOT NULL DEFAULT 0,
    ipaddress               VARCHAR(64) NOT NULL DEFAULT '127.0.0.1',
    createdby               VARCHAR(128) NOT NULL DEFAULT 'Admin',
    datecreated             TIMESTAMP NOT NULL DEFAULT NOW(),
    updatedby               VARCHAR(128) NULL,
    lastupdated             TIMESTAMP NULL,
    deletedby               VARCHAR(128) NULL,
    datedeleted             TIMESTAMP NULL,
    isdeleted               BOOLEAN NOT NULL DEFAULT FALSE,

    CONSTRAINT pk_tbluserstreak_userstreakid PRIMARY KEY (userstreakid)
);

-- ============================================
-- TABLE: tbldashboardmetric
-- No FK dependencies
-- ============================================
CREATE TABLE IF NOT EXISTS tbldashboardmetric
(
    dashboardmetricid       BIGINT GENERATED ALWAYS AS IDENTITY NOT NULL,
    metricdate              DATE NOT NULL,
    totalusers              INTEGER NOT NULL DEFAULT 0,
    activesessionstoday     INTEGER NOT NULL DEFAULT 0,
    totalscriptsuploaded    INTEGER NOT NULL DEFAULT 0,
    totalmistakesrecorded   INTEGER NOT NULL DEFAULT 0,
    tag                     VARCHAR(64) NULL,
    comments                VARCHAR(256) NULL,
    sortorder               INTEGER NOT NULL DEFAULT 0,
    ipaddress               VARCHAR(64) NOT NULL DEFAULT '127.0.0.1',
    createdby               VARCHAR(128) NOT NULL DEFAULT 'Admin',
    datecreated             TIMESTAMP NOT NULL DEFAULT NOW(),
    updatedby               VARCHAR(128) NULL,
    lastupdated             TIMESTAMP NULL,
    deletedby               VARCHAR(128) NULL,
    datedeleted             TIMESTAMP NULL,
    isdeleted               BOOLEAN NOT NULL DEFAULT FALSE,

    CONSTRAINT pk_tbldashboardmetric_dashboardmetricid PRIMARY KEY (dashboardmetricid)
);

-- ============================================
-- TABLE: tblotpverification
-- No FK dependencies
-- Schema inferred from uspInsertOtpVerification and uspVerifyOtp
-- [MANUAL REVIEW REQUIRED: schema inferred — verify against application requirements]
-- ============================================
CREATE TABLE IF NOT EXISTS tblotpverification
(
    otpverificationid       BIGINT GENERATED ALWAYS AS IDENTITY NOT NULL,
    mobilenumber            VARCHAR(16) NOT NULL,
    otpcode                 VARCHAR(8) NOT NULL,
    expiresat               TIMESTAMP NOT NULL,
    isverified              BOOLEAN NOT NULL DEFAULT FALSE,
    verifiedat              TIMESTAMP NULL,
    attemptcount            INTEGER NOT NULL DEFAULT 0,
    tag                     VARCHAR(64) NULL,
    comments                VARCHAR(256) NULL,
    sortorder               INTEGER NOT NULL DEFAULT 0,
    ipaddress               VARCHAR(64) NOT NULL DEFAULT '127.0.0.1',
    createdby               VARCHAR(128) NOT NULL DEFAULT 'Admin',
    datecreated             TIMESTAMP NOT NULL DEFAULT NOW(),
    updatedby               VARCHAR(128) NULL,
    lastupdated             TIMESTAMP NULL,
    deletedby               VARCHAR(128) NULL,
    datedeleted             TIMESTAMP NULL,
    isdeleted               BOOLEAN NOT NULL DEFAULT FALSE,

    CONSTRAINT pk_tblotpverification_otpverificationid PRIMARY KEY (otpverificationid)
);

-- ============================================
-- TABLE: tblutterance
-- FK dependency: tblscript (scriptid)
-- ============================================
CREATE TABLE IF NOT EXISTS tblutterance
(
    utteranceid             BIGINT GENERATED ALWAYS AS IDENTITY NOT NULL,
    scriptid                BIGINT NOT NULL,
    sequenceid              INTEGER NOT NULL,
    speakerlabel            VARCHAR(64) NOT NULL,
    englishtext             VARCHAR(512) NOT NULL,
    hinttext                VARCHAR(512) NULL,
    grammartag              VARCHAR(64) NULL,
    contexttag              VARCHAR(64) NULL,
    focusword               VARCHAR(64) NULL,
    pronunciationnote       VARCHAR(256) NULL,
    tag                     VARCHAR(64) NULL,
    comments                VARCHAR(256) NULL,
    sortorder               INTEGER NOT NULL DEFAULT 0,
    ipaddress               VARCHAR(64) NOT NULL DEFAULT '127.0.0.1',
    createdby               VARCHAR(128) NOT NULL DEFAULT 'Admin',
    datecreated             TIMESTAMP NOT NULL DEFAULT NOW(),
    updatedby               VARCHAR(128) NULL,
    lastupdated             TIMESTAMP NULL,
    deletedby               VARCHAR(128) NULL,
    datedeleted             TIMESTAMP NULL,
    isdeleted               BOOLEAN NOT NULL DEFAULT FALSE,

    CONSTRAINT pk_tblutterance_utteranceid PRIMARY KEY (utteranceid)
);

-- ============================================
-- TABLE: tblsession
-- FK dependencies: tbluser (hostuserid), tblscript (scriptid)
-- ============================================
CREATE TABLE IF NOT EXISTS tblsession
(
    sessionid               BIGINT GENERATED ALWAYS AS IDENTITY NOT NULL,
    sessionname             VARCHAR(128) NOT NULL,
    joincode                VARCHAR(8) NOT NULL,
    sessionmode             VARCHAR(64) NOT NULL,
    maxmembers              SMALLINT NOT NULL DEFAULT 4,
    sessionduration         INTEGER NOT NULL,
    hostuserid              BIGINT NOT NULL,
    scriptid                BIGINT NOT NULL,
    status                  VARCHAR(16) NOT NULL DEFAULT 'LOBBY',
    roomexpiryminutes       INTEGER NULL,
    roomexpiresat           TIMESTAMP NULL,
    starteddate             TIMESTAMP NULL,
    endeddate               TIMESTAMP NULL,
    actualdurationsec       INTEGER NULL,
    tag                     VARCHAR(64) NULL,
    comments                VARCHAR(256) NULL,
    sortorder               INTEGER NOT NULL DEFAULT 0,
    ipaddress               VARCHAR(64) NOT NULL DEFAULT '127.0.0.1',
    createdby               VARCHAR(128) NOT NULL DEFAULT 'Admin',
    datecreated             TIMESTAMP NOT NULL DEFAULT NOW(),
    updatedby               VARCHAR(128) NULL,
    lastupdated             TIMESTAMP NULL,
    deletedby               VARCHAR(128) NULL,
    datedeleted             TIMESTAMP NULL,
    isdeleted               BOOLEAN NOT NULL DEFAULT FALSE,

    CONSTRAINT pk_tblsession_sessionid PRIMARY KEY (sessionid)
);

-- ============================================
-- TABLE: tblscriptversion
-- FK dependencies: tblscript (scriptid), tbluser (uploadedbyuserid)
-- ============================================
CREATE TABLE IF NOT EXISTS tblscriptversion
(
    scriptversionid         BIGINT GENERATED ALWAYS AS IDENTITY NOT NULL,
    scriptid                BIGINT NOT NULL,
    versionnumber           INTEGER NOT NULL,
    versionnotes            VARCHAR(256) NULL,
    uploadedbyuserid        BIGINT NOT NULL,
    uploadeddate            TIMESTAMP NOT NULL DEFAULT NOW(),
    tag                     VARCHAR(64) NULL,
    comments                VARCHAR(256) NULL,
    sortorder               INTEGER NOT NULL DEFAULT 0,
    ipaddress               VARCHAR(64) NOT NULL DEFAULT '127.0.0.1',
    createdby               VARCHAR(128) NOT NULL DEFAULT 'Admin',
    datecreated             TIMESTAMP NOT NULL DEFAULT NOW(),
    updatedby               VARCHAR(128) NULL,
    lastupdated             TIMESTAMP NULL,
    deletedby               VARCHAR(128) NULL,
    datedeleted             TIMESTAMP NULL,
    isdeleted               BOOLEAN NOT NULL DEFAULT FALSE,

    CONSTRAINT pk_tblscriptversion_scriptversionid PRIMARY KEY (scriptversionid)
);

-- ============================================
-- TABLE: tbladminnote
-- FK dependencies: tbluser (adminuserid, targetuserid)
-- ============================================
CREATE TABLE IF NOT EXISTS tbladminnote
(
    adminnoteid             BIGINT GENERATED ALWAYS AS IDENTITY NOT NULL,
    adminuserid             BIGINT NOT NULL,
    targetuserid            BIGINT NOT NULL,
    notetext                VARCHAR(512) NOT NULL,
    notedate                TIMESTAMP NOT NULL DEFAULT NOW(),
    tag                     VARCHAR(64) NULL,
    comments                VARCHAR(256) NULL,
    sortorder               INTEGER NOT NULL DEFAULT 0,
    ipaddress               VARCHAR(64) NOT NULL DEFAULT '127.0.0.1',
    createdby               VARCHAR(128) NOT NULL DEFAULT 'Admin',
    datecreated             TIMESTAMP NOT NULL DEFAULT NOW(),
    updatedby               VARCHAR(128) NULL,
    lastupdated             TIMESTAMP NULL,
    deletedby               VARCHAR(128) NULL,
    datedeleted             TIMESTAMP NULL,
    isdeleted               BOOLEAN NOT NULL DEFAULT FALSE,

    CONSTRAINT pk_tbladminnote_adminnoteid PRIMARY KEY (adminnoteid)
);

-- ============================================
-- TABLE: tblsessionmember
-- FK dependencies: tblsession (sessionid), tbluser (userid)
-- ============================================
CREATE TABLE IF NOT EXISTS tblsessionmember
(
    sessionmemberid         BIGINT GENERATED ALWAYS AS IDENTITY NOT NULL,
    sessionid               BIGINT NOT NULL,
    userid                  BIGINT NOT NULL,
    slotindex               SMALLINT NOT NULL,
    slotname                VARCHAR(64) NOT NULL,
    isready                 BOOLEAN NOT NULL DEFAULT FALSE,
    ishost                  BOOLEAN NOT NULL DEFAULT FALSE,
    joinedat                TIMESTAMP NULL,
    leftat                  TIMESTAMP NULL,
    isactive                BOOLEAN NOT NULL DEFAULT TRUE,
    tag                     VARCHAR(64) NULL,
    comments                VARCHAR(256) NULL,
    sortorder               INTEGER NOT NULL DEFAULT 0,
    ipaddress               VARCHAR(64) NOT NULL DEFAULT '127.0.0.1',
    createdby               VARCHAR(128) NOT NULL DEFAULT 'Admin',
    datecreated             TIMESTAMP NOT NULL DEFAULT NOW(),
    updatedby               VARCHAR(128) NULL,
    lastupdated             TIMESTAMP NULL,
    deletedby               VARCHAR(128) NULL,
    datedeleted             TIMESTAMP NULL,
    isdeleted               BOOLEAN NOT NULL DEFAULT FALSE,

    CONSTRAINT pk_tblsessionmember_sessionmemberid PRIMARY KEY (sessionmemberid)
);

-- ============================================
-- TABLE: tbllistenerfeedback
-- FK dependencies: tblsession (sessionid), tbluser (fromuserid, targetuserid)
-- ============================================
CREATE TABLE IF NOT EXISTS tbllistenerfeedback
(
    listenerfeedbackid      BIGINT GENERATED ALWAYS AS IDENTITY NOT NULL,
    sessionid               BIGINT NOT NULL,
    turnindex               INTEGER NOT NULL,
    fromuserid              BIGINT NOT NULL,
    targetuserid            BIGINT NOT NULL,
    feedbacktag             VARCHAR(32) NOT NULL,
    feedbackat              TIMESTAMP NOT NULL DEFAULT NOW(),
    tag                     VARCHAR(64) NULL,
    comments                VARCHAR(256) NULL,
    sortorder               INTEGER NOT NULL DEFAULT 0,
    ipaddress               VARCHAR(64) NOT NULL DEFAULT '127.0.0.1',
    createdby               VARCHAR(128) NOT NULL DEFAULT 'Admin',
    datecreated             TIMESTAMP NOT NULL DEFAULT NOW(),
    updatedby               VARCHAR(128) NULL,
    lastupdated             TIMESTAMP NULL,
    deletedby               VARCHAR(128) NULL,
    datedeleted             TIMESTAMP NULL,
    isdeleted               BOOLEAN NOT NULL DEFAULT FALSE,

    CONSTRAINT pk_tbllistenerfeedback_listenerfeedbackid PRIMARY KEY (listenerfeedbackid)
);

-- ============================================
-- TABLE: tblturnstate
-- FK dependencies: tblsession (sessionid), tbluser (activememberid), tblutterance (utteranceid)
-- ============================================
CREATE TABLE IF NOT EXISTS tblturnstate
(
    turnstateid             BIGINT GENERATED ALWAYS AS IDENTITY NOT NULL,
    sessionid               BIGINT NOT NULL,
    turnindex               INTEGER NOT NULL,
    totalturns              INTEGER NOT NULL,
    activememberid          BIGINT NOT NULL,
    activeslotindex         SMALLINT NOT NULL,
    utteranceid             BIGINT NOT NULL,
    rereadallowed           BOOLEAN NOT NULL DEFAULT TRUE,
    rereadcount             INTEGER NOT NULL DEFAULT 0,
    maxrereads              INTEGER NOT NULL DEFAULT 2,
    turnstatus              VARCHAR(16) NOT NULL DEFAULT 'ACTIVE',
    turnstartedat           TIMESTAMP NULL,
    turncompletedat         TIMESTAMP NULL,
    tag                     VARCHAR(64) NULL,
    comments                VARCHAR(256) NULL,
    sortorder               INTEGER NOT NULL DEFAULT 0,
    ipaddress               VARCHAR(64) NOT NULL DEFAULT '127.0.0.1',
    createdby               VARCHAR(128) NOT NULL DEFAULT 'Admin',
    datecreated             TIMESTAMP NOT NULL DEFAULT NOW(),
    updatedby               VARCHAR(128) NULL,
    lastupdated             TIMESTAMP NULL,
    deletedby               VARCHAR(128) NULL,
    datedeleted             TIMESTAMP NULL,
    isdeleted               BOOLEAN NOT NULL DEFAULT FALSE,

    CONSTRAINT pk_tblturnstate_turnstateid PRIMARY KEY (turnstateid)
);

-- ============================================
-- TABLE: tblmistake
-- FK dependencies: tbluser (userid), tblsession (sessionid),
--                  tblutterance (utteranceid), tblscript (scriptid)
-- ============================================
CREATE TABLE IF NOT EXISTS tblmistake
(
    mistakeid               BIGINT GENERATED ALWAYS AS IDENTITY NOT NULL,
    userid                  BIGINT NOT NULL,
    sessionid               BIGINT NOT NULL,
    utteranceid             BIGINT NOT NULL,
    scriptid                BIGINT NOT NULL,
    utterancetext           VARCHAR(512) NOT NULL,
    spokentext              VARCHAR(512) NULL,
    mistaketype             VARCHAR(32) NOT NULL,
    mistakedetail           VARCHAR(256) NULL,
    grammartag              VARCHAR(64) NULL,
    contexttag              VARCHAR(64) NULL,
    correctiontext          VARCHAR(512) NULL,
    practicecount           INTEGER NOT NULL DEFAULT 0,
    isresolved              BOOLEAN NOT NULL DEFAULT FALSE,
    firstoccurrence         TIMESTAMP NOT NULL DEFAULT NOW(),
    lastattempt             TIMESTAMP NULL,
    tag                     VARCHAR(64) NULL,
    comments                VARCHAR(256) NULL,
    sortorder               INTEGER NOT NULL DEFAULT 0,
    ipaddress               VARCHAR(64) NOT NULL DEFAULT '127.0.0.1',
    createdby               VARCHAR(128) NOT NULL DEFAULT 'Admin',
    datecreated             TIMESTAMP NOT NULL DEFAULT NOW(),
    updatedby               VARCHAR(128) NULL,
    lastupdated             TIMESTAMP NULL,
    deletedby               VARCHAR(128) NULL,
    datedeleted             TIMESTAMP NULL,
    isdeleted               BOOLEAN NOT NULL DEFAULT FALSE,

    CONSTRAINT pk_tblmistake_mistakeid PRIMARY KEY (mistakeid)
);

-- ============================================
-- TABLE: tblvoiceanalysis
-- FK dependencies: tblsession (sessionid), tbluser (userid), tblutterance (utteranceid)
-- Note: grammarerrorsjson and pronunciationjson stored as JSONB for indexability
--       and OPENJSON() compatibility via jsonb_array_elements()
-- ============================================
CREATE TABLE IF NOT EXISTS tblvoiceanalysis
(
    voiceanalysisid         BIGINT GENERATED ALWAYS AS IDENTITY NOT NULL,
    sessionid               BIGINT NOT NULL,
    userid                  BIGINT NOT NULL,
    turnindex               INTEGER NOT NULL,
    utteranceid             BIGINT NOT NULL,
    transcribedtext         VARCHAR(512) NULL,
    expectedtext            VARCHAR(512) NOT NULL,
    fluencyscore            DECIMAL(5,2) NOT NULL DEFAULT 0,
    confidencescore         DECIMAL(5,2) NOT NULL DEFAULT 0,
    speakingspeedwpm        INTEGER NOT NULL DEFAULT 0,
    pausecount              INTEGER NOT NULL DEFAULT 0,
    hesitationwords         VARCHAR(256) NULL,
    repeatedwords           VARCHAR(256) NULL,
    grammarerrorsjson       JSONB NULL,
    pronunciationjson       JSONB NULL,
    overallscore            DECIMAL(5,2) NOT NULL DEFAULT 0,
    recordedat              TIMESTAMP NOT NULL DEFAULT NOW(),
    tag                     VARCHAR(64) NULL,
    comments                VARCHAR(256) NULL,
    sortorder               INTEGER NOT NULL DEFAULT 0,
    ipaddress               VARCHAR(64) NOT NULL DEFAULT '127.0.0.1',
    createdby               VARCHAR(128) NOT NULL DEFAULT 'Admin',
    datecreated             TIMESTAMP NOT NULL DEFAULT NOW(),
    updatedby               VARCHAR(128) NULL,
    lastupdated             TIMESTAMP NULL,
    deletedby               VARCHAR(128) NULL,
    datedeleted             TIMESTAMP NULL,
    isdeleted               BOOLEAN NOT NULL DEFAULT FALSE,

    CONSTRAINT pk_tblvoiceanalysis_voiceanalysisid PRIMARY KEY (voiceanalysisid)
);

-- ============================================
-- TABLE: tblrepracticesession
-- FK dependencies: tbluser (userid), tblsession (sourcesessionid)
-- ============================================
CREATE TABLE IF NOT EXISTS tblrepracticesession
(
    repracticesessionid     BIGINT GENERATED ALWAYS AS IDENTITY NOT NULL,
    userid                  BIGINT NOT NULL,
    sourcesessionid         BIGINT NOT NULL,
    totalmistakes           INTEGER NOT NULL DEFAULT 0,
    completedrounds         INTEGER NOT NULL DEFAULT 0,
    improvementpercent      DECIMAL(5,2) NOT NULL DEFAULT 0,
    status                  VARCHAR(16) NOT NULL DEFAULT 'PENDING',
    generateddate           TIMESTAMP NOT NULL DEFAULT NOW(),
    tag                     VARCHAR(64) NULL,
    comments                VARCHAR(256) NULL,
    sortorder               INTEGER NOT NULL DEFAULT 0,
    ipaddress               VARCHAR(64) NOT NULL DEFAULT '127.0.0.1',
    createdby               VARCHAR(128) NOT NULL DEFAULT 'Admin',
    datecreated             TIMESTAMP NOT NULL DEFAULT NOW(),
    updatedby               VARCHAR(128) NULL,
    lastupdated             TIMESTAMP NULL,
    deletedby               VARCHAR(128) NULL,
    datedeleted             TIMESTAMP NULL,
    isdeleted               BOOLEAN NOT NULL DEFAULT FALSE,

    CONSTRAINT pk_tblrepracticesession_repracticesessionid PRIMARY KEY (repracticesessionid)
);

-- ============================================
-- TABLE: tblrepracticeutterance
-- FK dependencies: tblrepracticesession (repracticesessionid),
--                  tblmistake (mistakeid), tblutterance (originalutteranceid)
-- ============================================
CREATE TABLE IF NOT EXISTS tblrepracticeutterance
(
    repracticeutteranceid   BIGINT GENERATED ALWAYS AS IDENTITY NOT NULL,
    repracticesessionid     BIGINT NOT NULL,
    mistakeid               BIGINT NOT NULL,
    originalutteranceid     BIGINT NOT NULL,
    englishtext             VARCHAR(512) NOT NULL,
    hinttext                VARCHAR(512) NULL,
    mistaketype             VARCHAR(32) NOT NULL,
    mistakedetail           VARCHAR(256) NULL,
    correctionnote          VARCHAR(512) NULL,
    attemptcount            INTEGER NOT NULL DEFAULT 0,
    bestscore               DECIMAL(5,2) NOT NULL DEFAULT 0,
    lastscore               DECIMAL(5,2) NOT NULL DEFAULT 0,
    isresolved              BOOLEAN NOT NULL DEFAULT FALSE,
    tag                     VARCHAR(64) NULL,
    comments                VARCHAR(256) NULL,
    sortorder               INTEGER NOT NULL DEFAULT 0,
    ipaddress               VARCHAR(64) NOT NULL DEFAULT '127.0.0.1',
    createdby               VARCHAR(128) NOT NULL DEFAULT 'Admin',
    datecreated             TIMESTAMP NOT NULL DEFAULT NOW(),
    updatedby               VARCHAR(128) NULL,
    lastupdated             TIMESTAMP NULL,
    deletedby               VARCHAR(128) NULL,
    datedeleted             TIMESTAMP NULL,
    isdeleted               BOOLEAN NOT NULL DEFAULT FALSE,

    CONSTRAINT pk_tblrepracticeutterance_repracticeutteranceid PRIMARY KEY (repracticeutteranceid)
);

COMMIT;
