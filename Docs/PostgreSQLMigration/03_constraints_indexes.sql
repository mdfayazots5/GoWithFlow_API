-- ============================================
-- File: 03_constraints_indexes.sql
-- Description: All foreign key constraints, unique constraints, and indexes
--              Converted from SQL Server GoWithFlowDB
-- Run order: 3 of 10
-- Dependencies: 01_extensions.sql, 02_schema.sql
-- ============================================
-- Tables migrated: N/A
-- Views migrated: N/A
-- Functions migrated: N/A
-- Stored Procedures migrated: N/A
-- Triggers migrated: N/A
-- Indexes migrated: 37
--   (31 FK indexes + 4 unique constraints + 2 composite unique indexes already
--    in index list + partial filtered indexes converted to WHERE isdeleted = FALSE)
-- Known incompatibilities: NONE
-- Manual review required: NONE
-- ============================================

BEGIN;

SET search_path TO public;

-- ============================================
-- FOREIGN KEY CONSTRAINTS
-- ============================================

-- tblAdminNote FKs
ALTER TABLE tbladminnote
    ADD CONSTRAINT fk_tbladminnote_adminuserid_tbluser_userid
    FOREIGN KEY (adminuserid) REFERENCES tbluser(userid);

ALTER TABLE tbladminnote
    ADD CONSTRAINT fk_tbladminnote_targetuserid_tbluser_userid
    FOREIGN KEY (targetuserid) REFERENCES tbluser(userid);

-- tblListenerFeedback FKs
ALTER TABLE tbllistenerfeedback
    ADD CONSTRAINT fk_tbllistenerfeedback_fromuserid_tbluser_userid
    FOREIGN KEY (fromuserid) REFERENCES tbluser(userid);

ALTER TABLE tbllistenerfeedback
    ADD CONSTRAINT fk_tbllistenerfeedback_sessionid_tblsession_sessionid
    FOREIGN KEY (sessionid) REFERENCES tblsession(sessionid);

ALTER TABLE tbllistenerfeedback
    ADD CONSTRAINT fk_tbllistenerfeedback_targetuserid_tbluser_userid
    FOREIGN KEY (targetuserid) REFERENCES tbluser(userid);

-- tblMistake FKs
ALTER TABLE tblmistake
    ADD CONSTRAINT fk_tblmistake_scriptid_tblscript_scriptid
    FOREIGN KEY (scriptid) REFERENCES tblscript(scriptid);

ALTER TABLE tblmistake
    ADD CONSTRAINT fk_tblmistake_sessionid_tblsession_sessionid
    FOREIGN KEY (sessionid) REFERENCES tblsession(sessionid);

ALTER TABLE tblmistake
    ADD CONSTRAINT fk_tblmistake_userid_tbluser_userid
    FOREIGN KEY (userid) REFERENCES tbluser(userid);

ALTER TABLE tblmistake
    ADD CONSTRAINT fk_tblmistake_utteranceid_tblutterance_utteranceid
    FOREIGN KEY (utteranceid) REFERENCES tblutterance(utteranceid);

-- tblRefreshToken FKs
ALTER TABLE tblrefreshtoken
    ADD CONSTRAINT fk_tblrefreshtoken_userid_tbluser_userid
    FOREIGN KEY (userid) REFERENCES tbluser(userid);

-- tblRepracticeSession FKs
ALTER TABLE tblrepracticesession
    ADD CONSTRAINT fk_tblrepracticesession_sourcesessionid_tblsession_sessionid
    FOREIGN KEY (sourcesessionid) REFERENCES tblsession(sessionid);

ALTER TABLE tblrepracticesession
    ADD CONSTRAINT fk_tblrepracticesession_userid_tbluser_userid
    FOREIGN KEY (userid) REFERENCES tbluser(userid);

-- tblRepracticeUtterance FKs
ALTER TABLE tblrepracticeutterance
    ADD CONSTRAINT fk_tblrepracticeutterance_mistakeid_tblmistake_mistakeid
    FOREIGN KEY (mistakeid) REFERENCES tblmistake(mistakeid);

ALTER TABLE tblrepracticeutterance
    ADD CONSTRAINT fk_tblrepracticeutterance_originalutteranceid_tblutterance_utteranceid
    FOREIGN KEY (originalutteranceid) REFERENCES tblutterance(utteranceid);

ALTER TABLE tblrepracticeutterance
    ADD CONSTRAINT fk_tblrepracticeutterance_repracticesessionid_tblrepracticesession_repracticesessionid
    FOREIGN KEY (repracticesessionid) REFERENCES tblrepracticesession(repracticesessionid);

-- tblScript FKs
ALTER TABLE tblscript
    ADD CONSTRAINT fk_tblscript_uploadedbyuserid_tbluser_userid
    FOREIGN KEY (uploadedbyuserid) REFERENCES tbluser(userid);

-- tblScriptVersion FKs
ALTER TABLE tblscriptversion
    ADD CONSTRAINT fk_tblscriptversion_scriptid_tblscript_scriptid
    FOREIGN KEY (scriptid) REFERENCES tblscript(scriptid);

ALTER TABLE tblscriptversion
    ADD CONSTRAINT fk_tblscriptversion_uploadedbyuserid_tbluser_userid
    FOREIGN KEY (uploadedbyuserid) REFERENCES tbluser(userid);

-- tblSession FKs
ALTER TABLE tblsession
    ADD CONSTRAINT fk_tblsession_hostuserid_tbluser_userid
    FOREIGN KEY (hostuserid) REFERENCES tbluser(userid);

ALTER TABLE tblsession
    ADD CONSTRAINT fk_tblsession_scriptid_tblscript_scriptid
    FOREIGN KEY (scriptid) REFERENCES tblscript(scriptid);

-- tblSessionMember FKs
ALTER TABLE tblsessionmember
    ADD CONSTRAINT fk_tblsessionmember_sessionid_tblsession_sessionid
    FOREIGN KEY (sessionid) REFERENCES tblsession(sessionid);

ALTER TABLE tblsessionmember
    ADD CONSTRAINT fk_tblsessionmember_userid_tbluser_userid
    FOREIGN KEY (userid) REFERENCES tbluser(userid);

-- tblTurnState FKs
ALTER TABLE tblturnstate
    ADD CONSTRAINT fk_tblturnstate_activememberid_tbluser_userid
    FOREIGN KEY (activememberid) REFERENCES tbluser(userid);

ALTER TABLE tblturnstate
    ADD CONSTRAINT fk_tblturnstate_sessionid_tblsession_sessionid
    FOREIGN KEY (sessionid) REFERENCES tblsession(sessionid);

ALTER TABLE tblturnstate
    ADD CONSTRAINT fk_tblturnstate_utteranceid_tblutterance_utteranceid
    FOREIGN KEY (utteranceid) REFERENCES tblutterance(utteranceid);

-- tblUserBadge FKs
ALTER TABLE tbluserbadge
    ADD CONSTRAINT fk_tbluserbadge_userid_tbluser_userid
    FOREIGN KEY (userid) REFERENCES tbluser(userid);

-- tblUserStreak FKs
ALTER TABLE tbluserstreak
    ADD CONSTRAINT fk_tbluserstreak_userid_tbluser_userid
    FOREIGN KEY (userid) REFERENCES tbluser(userid);

-- tblUtterance FKs
ALTER TABLE tblutterance
    ADD CONSTRAINT fk_tblutterance_scriptid_tblscript_scriptid
    FOREIGN KEY (scriptid) REFERENCES tblscript(scriptid);

-- tblVoiceAnalysis FKs
ALTER TABLE tblvoiceanalysis
    ADD CONSTRAINT fk_tblvoiceanalysis_sessionid_tblsession_sessionid
    FOREIGN KEY (sessionid) REFERENCES tblsession(sessionid);

ALTER TABLE tblvoiceanalysis
    ADD CONSTRAINT fk_tblvoiceanalysis_userid_tbluser_userid
    FOREIGN KEY (userid) REFERENCES tbluser(userid);

ALTER TABLE tblvoiceanalysis
    ADD CONSTRAINT fk_tblvoiceanalysis_utteranceid_tblutterance_utteranceid
    FOREIGN KEY (utteranceid) REFERENCES tblutterance(utteranceid);

-- ============================================
-- UNIQUE CONSTRAINTS
-- ============================================

-- UK_tblDashboardMetric_MetricDate
ALTER TABLE tbldashboardmetric
    ADD CONSTRAINT uk_tbldashboardmetric_metricdate UNIQUE (metricdate);

-- UK_tblUser_MobileNumber
ALTER TABLE tbluser
    ADD CONSTRAINT uk_tbluser_mobilenumber UNIQUE (mobilenumber);

-- UK_tblUserBadge_UserId_BadgeCode
ALTER TABLE tbluserbadge
    ADD CONSTRAINT uk_tbluserbadge_userid_badgecode UNIQUE (userid, badgecode);

-- UK_tblUserStreak_UserId_StreakDate
ALTER TABLE tbluserstreak
    ADD CONSTRAINT uk_tbluserstreak_userid_streakdate UNIQUE (userid, streakdate);

-- UK_tblSession_JoinCode (filtered in SQL Server: WHERE IsDeleted = 0)
-- PostgreSQL partial unique index equivalent
CREATE UNIQUE INDEX IF NOT EXISTS uk_tblsession_joincode
    ON tblsession (joincode)
    WHERE isdeleted = FALSE;

-- UK_tblSessionMember_SessionId_SlotIndex (filtered: WHERE IsDeleted=0 AND IsActive=1)
CREATE UNIQUE INDEX IF NOT EXISTS uk_tblsessionmember_sessionid_slotindex
    ON tblsessionmember (sessionid, slotindex)
    WHERE isdeleted = FALSE AND isactive = TRUE;

-- UK_tblUtterance_ScriptId_SequenceId
ALTER TABLE tblutterance
    ADD CONSTRAINT uk_tblutterance_scriptid_sequenceid UNIQUE (scriptid, sequenceid);

-- ============================================
-- NON-CLUSTERED INDEXES
-- All converted with partial WHERE isdeleted = FALSE where source had filtered index
-- ============================================

-- tblAdminNote
CREATE INDEX IF NOT EXISTS idx_tbladminnote_targetuserid
    ON tbladminnote (targetuserid);

-- tblListenerFeedback
CREATE INDEX IF NOT EXISTS idx_tbllistenerfeedback_sessionid_turnindex
    ON tbllistenerfeedback (sessionid, turnindex);

-- tblMistake
CREATE INDEX IF NOT EXISTS idx_tblmistake_grammartag
    ON tblmistake (grammartag);

CREATE INDEX IF NOT EXISTS idx_tblmistake_sessionid
    ON tblmistake (sessionid);

CREATE INDEX IF NOT EXISTS idx_tblmistake_userid
    ON tblmistake (userid);

CREATE INDEX IF NOT EXISTS idx_tblmistake_userid_isresolved
    ON tblmistake (userid, isresolved);

-- Filtered index: SQL Server WHERE IsDeleted = 0
CREATE INDEX IF NOT EXISTS idx_tblmistake_userid_mistaketype_isresolved
    ON tblmistake (userid, mistaketype, isresolved)
    WHERE isdeleted = FALSE;

-- tblRefreshToken
CREATE INDEX IF NOT EXISTS idx_tblrefreshtoken_userid
    ON tblrefreshtoken (userid);

-- tblRepracticeSession
CREATE INDEX IF NOT EXISTS idx_tblrepracticesession_userid
    ON tblrepracticesession (userid);

-- Filtered index
CREATE INDEX IF NOT EXISTS idx_tblrepracticesession_userid_status
    ON tblrepracticesession (userid, status)
    WHERE isdeleted = FALSE;

-- tblRepracticeUtterance
CREATE INDEX IF NOT EXISTS idx_tblrepracticeutterance_repracticesessionid
    ON tblrepracticeutterance (repracticesessionid);

-- tblScript
CREATE INDEX IF NOT EXISTS idx_tblscript_category
    ON tblscript (category);

CREATE INDEX IF NOT EXISTS idx_tblscript_grammarfocustag
    ON tblscript (grammarfocustag);

CREATE INDEX IF NOT EXISTS idx_tblscript_isactive
    ON tblscript (isactive);

-- tblScriptVersion
CREATE INDEX IF NOT EXISTS idx_tblscriptversion_scriptid
    ON tblscriptversion (scriptid);

-- tblSession
CREATE INDEX IF NOT EXISTS idx_tblsession_hostuserid
    ON tblsession (hostuserid);

CREATE INDEX IF NOT EXISTS idx_tblsession_joincode
    ON tblsession (joincode);

CREATE INDEX IF NOT EXISTS idx_tblsession_status
    ON tblsession (status);

-- Filtered index
CREATE INDEX IF NOT EXISTS idx_tblsession_status_isdeleted
    ON tblsession (status)
    WHERE isdeleted = FALSE;

-- tblSessionMember
CREATE INDEX IF NOT EXISTS idx_tblsessionmember_sessionid
    ON tblsessionmember (sessionid);

-- Filtered index
CREATE INDEX IF NOT EXISTS idx_tblsessionmember_sessionid_isactive
    ON tblsessionmember (sessionid, isactive)
    WHERE isdeleted = FALSE;

CREATE INDEX IF NOT EXISTS idx_tblsessionmember_userid
    ON tblsessionmember (userid);

-- tblTurnState
CREATE INDEX IF NOT EXISTS idx_tblturnstate_sessionid
    ON tblturnstate (sessionid);

CREATE INDEX IF NOT EXISTS idx_tblturnstate_sessionid_turnindex
    ON tblturnstate (sessionid, turnindex);

-- tblUserBadge
CREATE INDEX IF NOT EXISTS idx_tbluserbadge_userid
    ON tbluserbadge (userid);

-- tblUserStreak
CREATE INDEX IF NOT EXISTS idx_tbluserstreak_userid
    ON tbluserstreak (userid);

-- tblUtterance
CREATE INDEX IF NOT EXISTS idx_tblutterance_scriptid
    ON tblutterance (scriptid);

-- tblVoiceAnalysis
CREATE INDEX IF NOT EXISTS idx_tblvoiceanalysis_sessionid
    ON tblvoiceanalysis (sessionid);

-- Filtered index
CREATE INDEX IF NOT EXISTS idx_tblvoiceanalysis_sessionid_userid
    ON tblvoiceanalysis (sessionid, userid)
    WHERE isdeleted = FALSE;

CREATE INDEX IF NOT EXISTS idx_tblvoiceanalysis_userid
    ON tblvoiceanalysis (userid);

-- ============================================
-- TRIGRAM INDEXES for ILIKE search performance
-- These replace SQL Server's case-insensitive LIKE behaviour
-- ============================================

CREATE INDEX IF NOT EXISTS idx_tbluser_fullname_trgm
    ON tbluser USING gin (fullname gin_trgm_ops);

CREATE INDEX IF NOT EXISTS idx_tbluser_mobilenumber_trgm
    ON tbluser USING gin (mobilenumber gin_trgm_ops);

CREATE INDEX IF NOT EXISTS idx_tblscript_scripttitle_trgm
    ON tblscript USING gin (scripttitle gin_trgm_ops);

COMMIT;
