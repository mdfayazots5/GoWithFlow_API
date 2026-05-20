-- ============================================
-- File: 09_sequences_reset.sql
-- Description: Reset GENERATED ALWAYS AS IDENTITY sequences for all 18 tables
--              after bulk data load from SQL Server migration.
-- Run order: 9 of 10  (run AFTER data load, BEFORE application start)
-- Dependencies: 02_schema.sql, data load completed
-- ============================================
-- Usage:
--   Run this script after loading migrated data to ensure sequences start
--   above the highest existing ID, preventing primary key conflicts.
-- ============================================

BEGIN;

SET search_path TO public;

-- tblUser
SELECT SETVAL(
    PG_GET_SERIAL_SEQUENCE('tbluser', 'userid'),
    COALESCE((SELECT MAX(userid) FROM tbluser), 0) + 1,
    FALSE
);

-- tblScript
SELECT SETVAL(
    PG_GET_SERIAL_SEQUENCE('tblscript', 'scriptid'),
    COALESCE((SELECT MAX(scriptid) FROM tblscript), 0) + 1,
    FALSE
);

-- tblScriptVersion
SELECT SETVAL(
    PG_GET_SERIAL_SEQUENCE('tblscriptversion', 'scriptversionid'),
    COALESCE((SELECT MAX(scriptversionid) FROM tblscriptversion), 0) + 1,
    FALSE
);

-- tblUtterance
SELECT SETVAL(
    PG_GET_SERIAL_SEQUENCE('tblutterance', 'utteranceid'),
    COALESCE((SELECT MAX(utteranceid) FROM tblutterance), 0) + 1,
    FALSE
);

-- tblRefreshToken
SELECT SETVAL(
    PG_GET_SERIAL_SEQUENCE('tblrefreshtoken', 'refreshtokenid'),
    COALESCE((SELECT MAX(refreshtokenid) FROM tblrefreshtoken), 0) + 1,
    FALSE
);

-- tblOtpVerification
SELECT SETVAL(
    PG_GET_SERIAL_SEQUENCE('tblotpverification', 'otpverificationid'),
    COALESCE((SELECT MAX(otpverificationid) FROM tblotpverification), 0) + 1,
    FALSE
);

-- tblSession
SELECT SETVAL(
    PG_GET_SERIAL_SEQUENCE('tblsession', 'sessionid'),
    COALESCE((SELECT MAX(sessionid) FROM tblsession), 0) + 1,
    FALSE
);

-- tblSessionMember
SELECT SETVAL(
    PG_GET_SERIAL_SEQUENCE('tblsessionmember', 'sessionmemberid'),
    COALESCE((SELECT MAX(sessionmemberid) FROM tblsessionmember), 0) + 1,
    FALSE
);

-- tblTurnState
SELECT SETVAL(
    PG_GET_SERIAL_SEQUENCE('tblturnstate', 'turnstateid'),
    COALESCE((SELECT MAX(turnstateid) FROM tblturnstate), 0) + 1,
    FALSE
);

-- tblMistake
SELECT SETVAL(
    PG_GET_SERIAL_SEQUENCE('tblmistake', 'mistakeid'),
    COALESCE((SELECT MAX(mistakeid) FROM tblmistake), 0) + 1,
    FALSE
);

-- tblVoiceAnalysis
SELECT SETVAL(
    PG_GET_SERIAL_SEQUENCE('tblvoiceanalysis', 'voiceanalysisid'),
    COALESCE((SELECT MAX(voiceanalysisid) FROM tblvoiceanalysis), 0) + 1,
    FALSE
);

-- tblRepracticeSession
SELECT SETVAL(
    PG_GET_SERIAL_SEQUENCE('tblrepracticesession', 'repracticesessionid'),
    COALESCE((SELECT MAX(repracticesessionid) FROM tblrepracticesession), 0) + 1,
    FALSE
);

-- tblRepracticeUtterance
SELECT SETVAL(
    PG_GET_SERIAL_SEQUENCE('tblrepracticeutterance', 'repracticeutteranceid'),
    COALESCE((SELECT MAX(repracticeutteranceid) FROM tblrepracticeutterance), 0) + 1,
    FALSE
);

-- tblAdminNote
SELECT SETVAL(
    PG_GET_SERIAL_SEQUENCE('tbladminnote', 'adminnoteid'),
    COALESCE((SELECT MAX(adminnoteid) FROM tbladminnote), 0) + 1,
    FALSE
);

-- tblListenerFeedback
SELECT SETVAL(
    PG_GET_SERIAL_SEQUENCE('tbllistenerfeedback', 'listenfeedbackid'),
    COALESCE((SELECT MAX(listenfeedbackid) FROM tbllistenerfeedback), 0) + 1,
    FALSE
);

-- tblUserBadge
SELECT SETVAL(
    PG_GET_SERIAL_SEQUENCE('tbluserbadge', 'userbadgeid'),
    COALESCE((SELECT MAX(userbadgeid) FROM tbluserbadge), 0) + 1,
    FALSE
);

-- tblUserStreak
SELECT SETVAL(
    PG_GET_SERIAL_SEQUENCE('tbluserstreak', 'userstreakid'),
    COALESCE((SELECT MAX(userstreakid) FROM tbluserstreak), 0) + 1,
    FALSE
);

-- tblDashboardMetric
SELECT SETVAL(
    PG_GET_SERIAL_SEQUENCE('tbldashboardmetric', 'dashboardmetricid'),
    COALESCE((SELECT MAX(dashboardmetricid) FROM tbldashboardmetric), 0) + 1,
    FALSE
);

COMMIT;
