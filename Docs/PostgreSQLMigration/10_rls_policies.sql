-- ============================================
-- File: 10_rls_policies.sql
-- Description: Supabase Row Level Security (RLS) policies for GoWithFlow
-- Run order: 10 of 10
-- Dependencies: 02_schema.sql
-- ============================================
-- RLS Strategy:
--   - Auth users access their own data via auth.uid() mapped to tbluser.userid
--   - Admin role bypasses RLS via service_role key (Supabase default)
--   - All application writes go through stored functions (06_stored_procedures.sql)
--     which run as SECURITY DEFINER — RLS is enforced at the API layer,
--     not duplicated at the DB layer for write paths.
--   - READ policies are the primary protection layer for direct queries.
-- ============================================
-- NOTE: These policies assume the application sets a JWT claim
--   current_setting('request.jwt.claims', true)::jsonb->>'sub'
--   to the tbluser.userid value (as text) on login.
--   Adjust the helper function below if your JWT structure differs.
-- ============================================

BEGIN;

SET search_path TO public;

-- ============================================
-- HELPER FUNCTION: get current user id from JWT
-- ============================================
CREATE OR REPLACE FUNCTION current_user_id() RETURNS BIGINT AS $$
BEGIN
    RETURN (
        current_setting('request.jwt.claims', true)::JSONB ->> 'userid'
    )::BIGINT;
EXCEPTION
    WHEN OTHERS THEN RETURN NULL;
END;
$$ LANGUAGE plpgsql STABLE SECURITY DEFINER;

-- ============================================
-- ENABLE RLS ON ALL TABLES
-- ============================================

ALTER TABLE tbluser              ENABLE ROW LEVEL SECURITY;
ALTER TABLE tblscript            ENABLE ROW LEVEL SECURITY;
ALTER TABLE tblscriptversion     ENABLE ROW LEVEL SECURITY;
ALTER TABLE tblutterance         ENABLE ROW LEVEL SECURITY;
ALTER TABLE tblrefreshtoken      ENABLE ROW LEVEL SECURITY;
ALTER TABLE tblotpverification   ENABLE ROW LEVEL SECURITY;
ALTER TABLE tblsession           ENABLE ROW LEVEL SECURITY;
ALTER TABLE tblsessionmember     ENABLE ROW LEVEL SECURITY;
ALTER TABLE tblturnstate         ENABLE ROW LEVEL SECURITY;
ALTER TABLE tblmistake           ENABLE ROW LEVEL SECURITY;
ALTER TABLE tblvoiceanalysis     ENABLE ROW LEVEL SECURITY;
ALTER TABLE tblrepracticesession ENABLE ROW LEVEL SECURITY;
ALTER TABLE tblrepracticeutterance ENABLE ROW LEVEL SECURITY;
ALTER TABLE tbladminnote         ENABLE ROW LEVEL SECURITY;
ALTER TABLE tbllistenerfeedback  ENABLE ROW LEVEL SECURITY;
ALTER TABLE tbluserbadge         ENABLE ROW LEVEL SECURITY;
ALTER TABLE tbluserstreak        ENABLE ROW LEVEL SECURITY;
ALTER TABLE tbldashboardmetric   ENABLE ROW LEVEL SECURITY;

-- ============================================
-- tblUser
-- Users can read/update their own row.
-- Admin can read all (enforced via service_role in application).
-- ============================================
CREATE POLICY policy_tbluser_select ON tbluser
    FOR SELECT USING (userid = current_user_id() OR isdeleted = FALSE);

CREATE POLICY policy_tbluser_update ON tbluser
    FOR UPDATE USING (userid = current_user_id());

-- ============================================
-- tblScript
-- All authenticated users can read active scripts.
-- Writes go through SPs (SECURITY DEFINER).
-- ============================================
CREATE POLICY policy_tblscript_select ON tblscript
    FOR SELECT USING (isdeleted = FALSE);

-- ============================================
-- tblScriptVersion
-- Readable by all authenticated users.
-- ============================================
CREATE POLICY policy_tblscriptversion_select ON tblscriptversion
    FOR SELECT USING (isdeleted = FALSE);

-- ============================================
-- tblUtterance
-- Readable by all authenticated users.
-- ============================================
CREATE POLICY policy_tblutterance_select ON tblutterance
    FOR SELECT USING (isdeleted = FALSE);

-- ============================================
-- tblRefreshToken
-- Users can only see their own tokens.
-- ============================================
CREATE POLICY policy_tblrefreshtoken_select ON tblrefreshtoken
    FOR SELECT USING (userid = current_user_id() AND isdeleted = FALSE);

-- ============================================
-- tblOtpVerification
-- No direct read access — all access via SPs.
-- ============================================
CREATE POLICY policy_tblotpverification_select ON tblotpverification
    FOR SELECT USING (FALSE);

-- ============================================
-- tblSession
-- Users can see sessions they are a member of, or LOBBY sessions.
-- ============================================
CREATE POLICY policy_tblsession_select ON tblsession
    FOR SELECT USING (
        isdeleted = FALSE
        AND (
            status = 'LOBBY'
            OR hostuserid = current_user_id()
            OR EXISTS (
                SELECT 1 FROM tblsessionmember sm
                WHERE sm.sessionid = tblsession.sessionid
                  AND sm.userid    = current_user_id()
                  AND sm.isdeleted = FALSE
            )
        )
    );

-- ============================================
-- tblSessionMember
-- Users can see members of sessions they belong to.
-- ============================================
CREATE POLICY policy_tblsessionmember_select ON tblsessionmember
    FOR SELECT USING (
        isdeleted = FALSE
        AND EXISTS (
            SELECT 1 FROM tblsessionmember sm2
            WHERE sm2.sessionid = tblsessionmember.sessionid
              AND sm2.userid    = current_user_id()
              AND sm2.isdeleted = FALSE
        )
    );

-- ============================================
-- tblTurnState
-- Users can see turn state for their sessions.
-- ============================================
CREATE POLICY policy_tblturnstate_select ON tblturnstate
    FOR SELECT USING (
        isdeleted = FALSE
        AND EXISTS (
            SELECT 1 FROM tblsessionmember sm
            WHERE sm.sessionid = tblturnstate.sessionid
              AND sm.userid    = current_user_id()
              AND sm.isdeleted = FALSE
        )
    );

-- ============================================
-- tblMistake
-- Users can only see their own mistakes.
-- ============================================
CREATE POLICY policy_tblmistake_select ON tblmistake
    FOR SELECT USING (userid = current_user_id() AND isdeleted = FALSE);

-- ============================================
-- tblVoiceAnalysis
-- Users can see their own analysis. Session members can see all in their session.
-- ============================================
CREATE POLICY policy_tblvoiceanalysis_select ON tblvoiceanalysis
    FOR SELECT USING (
        isdeleted = FALSE
        AND (
            userid = current_user_id()
            OR EXISTS (
                SELECT 1 FROM tblsessionmember sm
                WHERE sm.sessionid = tblvoiceanalysis.sessionid
                  AND sm.userid    = current_user_id()
                  AND sm.isdeleted = FALSE
            )
        )
    );

-- ============================================
-- tblRepracticeSession
-- Users can only see their own repractice sessions.
-- ============================================
CREATE POLICY policy_tblrepracticesession_select ON tblrepracticesession
    FOR SELECT USING (userid = current_user_id() AND isdeleted = FALSE);

-- ============================================
-- tblRepracticeUtterance
-- Users can access utterances in their own repractice sessions.
-- ============================================
CREATE POLICY policy_tblrepracticeutterance_select ON tblrepracticeutterance
    FOR SELECT USING (
        isdeleted = FALSE
        AND EXISTS (
            SELECT 1 FROM tblrepracticesession rps
            WHERE rps.repracticesessionid = tblrepracticeutterance.repracticesessionid
              AND rps.userid    = current_user_id()
              AND rps.isdeleted = FALSE
        )
    );

-- ============================================
-- tblAdminNote
-- No direct user access — admin only via service_role.
-- ============================================
CREATE POLICY policy_tbladminnote_select ON tbladminnote
    FOR SELECT USING (FALSE);

-- ============================================
-- tblListenerFeedback
-- Session members can see feedback in their sessions.
-- ============================================
CREATE POLICY policy_tbllistenerfeedback_select ON tbllistenerfeedback
    FOR SELECT USING (
        isdeleted = FALSE
        AND EXISTS (
            SELECT 1 FROM tblsessionmember sm
            WHERE sm.sessionid = tbllistenerfeedback.sessionid
              AND sm.userid    = current_user_id()
              AND sm.isdeleted = FALSE
        )
    );

-- ============================================
-- tblUserBadge
-- Users can only see their own badges.
-- ============================================
CREATE POLICY policy_tbluserbadge_select ON tbluserbadge
    FOR SELECT USING (userid = current_user_id() AND isdeleted = FALSE);

-- ============================================
-- tblUserStreak
-- Users can only see their own streak rows.
-- ============================================
CREATE POLICY policy_tbluserstreak_select ON tbluserstreak
    FOR SELECT USING (userid = current_user_id() AND isdeleted = FALSE);

-- ============================================
-- tblDashboardMetric
-- No direct user access — admin/system only via service_role.
-- ============================================
CREATE POLICY policy_tbldashboardmetric_select ON tbldashboardmetric
    FOR SELECT USING (FALSE);

COMMIT;
