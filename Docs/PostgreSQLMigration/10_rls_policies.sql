-- ============================================
-- File: 10_rls_policies.sql
-- Description: Enable Supabase Row Level Security and map legacy SQL Server user roles to Supabase auth identities.
-- Run order: 10 of 10
-- Dependencies: 01_extensions.sql, 02_schema.sql, 03_constraints_indexes.sql, 04_views.sql, 05_functions.sql, 06_stored_procedures.sql, 07_triggers.sql, 08_seed_data.sql, 09_sequences_reset.sql
-- ============================================
-- Tables migrated: 17
-- Views migrated: 0
-- Functions migrated: 0
-- Stored Procedures migrated: 79
-- Triggers migrated: 0
-- Indexes migrated: 33
-- Known incompatibilities: Legacy SQL Server authentication used bigint user ids and application-issued JWTs. Supabase auth.uid() needs an explicit bridge to mapped app users.
-- Manual review required: Backfill public.user_auth_map after importing users and before exposing authenticated client access.


BEGIN;

CREATE TABLE IF NOT EXISTS public.user_auth_map (
    auth_user_id uuid PRIMARY KEY REFERENCES auth.users (id) ON DELETE CASCADE,
    user_id bigint NOT NULL UNIQUE REFERENCES public.tbl_user (user_id) ON DELETE CASCADE,
    created_at timestamptz NOT NULL DEFAULT NOW()
);

ALTER TABLE public.user_auth_map ENABLE ROW LEVEL SECURITY;

CREATE OR REPLACE FUNCTION public.current_app_user_id()
RETURNS bigint
LANGUAGE sql
STABLE
AS $$
    SELECT uam.user_id
    FROM public.user_auth_map AS uam
    WHERE uam.auth_user_id = auth.uid()
    LIMIT 1;
$$;

CREATE OR REPLACE FUNCTION public.current_app_is_admin()
RETURNS boolean
LANGUAGE sql
STABLE
AS $$
    SELECT EXISTS (
        SELECT 1
        FROM public.user_auth_map AS uam
        JOIN public.tbl_user AS usr ON usr.user_id = uam.user_id
        WHERE uam.auth_user_id = auth.uid()
          AND usr.role = 'ADMIN'
          AND usr.is_deleted = FALSE
    );
$$;

DROP POLICY IF EXISTS user_auth_map_self_select ON public.user_auth_map;
CREATE POLICY user_auth_map_self_select ON public.user_auth_map
    FOR SELECT
    USING (auth.uid() = auth_user_id);

DROP POLICY IF EXISTS user_auth_map_service_manage ON public.user_auth_map;
CREATE POLICY user_auth_map_service_manage ON public.user_auth_map
    FOR ALL
    USING (auth.role() = 'service_role')
    WITH CHECK (auth.role() = 'service_role');

ALTER TABLE public.tbl_admin_note ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tbl_admin_note_admin_only ON public.tbl_admin_note;
CREATE POLICY tbl_admin_note_admin_only ON public.tbl_admin_note FOR ALL USING (public.current_app_is_admin() OR auth.role() = 'service_role') WITH CHECK (public.current_app_is_admin() OR auth.role() = 'service_role');

ALTER TABLE public.tbl_dashboard_metric ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tbl_dashboard_metric_admin_only ON public.tbl_dashboard_metric;
CREATE POLICY tbl_dashboard_metric_admin_only ON public.tbl_dashboard_metric FOR ALL USING (public.current_app_is_admin() OR auth.role() = 'service_role') WITH CHECK (public.current_app_is_admin() OR auth.role() = 'service_role');

ALTER TABLE public.tbl_listener_feedback ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tbl_listener_feedback_admin_only ON public.tbl_listener_feedback;
CREATE POLICY tbl_listener_feedback_admin_only ON public.tbl_listener_feedback FOR ALL USING (public.current_app_is_admin() OR auth.role() = 'service_role') WITH CHECK (public.current_app_is_admin() OR auth.role() = 'service_role');

ALTER TABLE public.tbl_mistake ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tbl_mistake_owner_select ON public.tbl_mistake;
CREATE POLICY tbl_mistake_owner_select ON public.tbl_mistake FOR SELECT USING (user_id = public.current_app_user_id() OR public.current_app_is_admin());
DROP POLICY IF EXISTS tbl_mistake_owner_modify ON public.tbl_mistake;
CREATE POLICY tbl_mistake_owner_modify ON public.tbl_mistake FOR ALL USING (user_id = public.current_app_user_id() OR public.current_app_is_admin()) WITH CHECK (user_id = public.current_app_user_id() OR public.current_app_is_admin());

ALTER TABLE public.tbl_refresh_token ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tbl_refresh_token_owner_select ON public.tbl_refresh_token;
CREATE POLICY tbl_refresh_token_owner_select ON public.tbl_refresh_token FOR SELECT USING (user_id = public.current_app_user_id() OR public.current_app_is_admin());
DROP POLICY IF EXISTS tbl_refresh_token_owner_modify ON public.tbl_refresh_token;
CREATE POLICY tbl_refresh_token_owner_modify ON public.tbl_refresh_token FOR ALL USING (user_id = public.current_app_user_id() OR public.current_app_is_admin()) WITH CHECK (user_id = public.current_app_user_id() OR public.current_app_is_admin());

ALTER TABLE public.tbl_repractice_session ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tbl_repractice_session_owner_select ON public.tbl_repractice_session;
CREATE POLICY tbl_repractice_session_owner_select ON public.tbl_repractice_session FOR SELECT USING (user_id = public.current_app_user_id() OR public.current_app_is_admin());
DROP POLICY IF EXISTS tbl_repractice_session_owner_modify ON public.tbl_repractice_session;
CREATE POLICY tbl_repractice_session_owner_modify ON public.tbl_repractice_session FOR ALL USING (user_id = public.current_app_user_id() OR public.current_app_is_admin()) WITH CHECK (user_id = public.current_app_user_id() OR public.current_app_is_admin());

ALTER TABLE public.tbl_repractice_utterance ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tbl_repractice_utterance_admin_only ON public.tbl_repractice_utterance;
CREATE POLICY tbl_repractice_utterance_admin_only ON public.tbl_repractice_utterance FOR ALL USING (public.current_app_is_admin() OR auth.role() = 'service_role') WITH CHECK (public.current_app_is_admin() OR auth.role() = 'service_role');

ALTER TABLE public.tbl_script ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tbl_script_admin_only ON public.tbl_script;
CREATE POLICY tbl_script_admin_only ON public.tbl_script FOR ALL USING (public.current_app_is_admin() OR auth.role() = 'service_role') WITH CHECK (public.current_app_is_admin() OR auth.role() = 'service_role');

ALTER TABLE public.tbl_script_version ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tbl_script_version_admin_only ON public.tbl_script_version;
CREATE POLICY tbl_script_version_admin_only ON public.tbl_script_version FOR ALL USING (public.current_app_is_admin() OR auth.role() = 'service_role') WITH CHECK (public.current_app_is_admin() OR auth.role() = 'service_role');

ALTER TABLE public.tbl_session ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tbl_session_admin_only ON public.tbl_session;
CREATE POLICY tbl_session_admin_only ON public.tbl_session FOR ALL USING (public.current_app_is_admin() OR auth.role() = 'service_role') WITH CHECK (public.current_app_is_admin() OR auth.role() = 'service_role');

ALTER TABLE public.tbl_session_member ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tbl_session_member_owner_select ON public.tbl_session_member;
CREATE POLICY tbl_session_member_owner_select ON public.tbl_session_member FOR SELECT USING (user_id = public.current_app_user_id() OR public.current_app_is_admin());
DROP POLICY IF EXISTS tbl_session_member_owner_modify ON public.tbl_session_member;
CREATE POLICY tbl_session_member_owner_modify ON public.tbl_session_member FOR ALL USING (user_id = public.current_app_user_id() OR public.current_app_is_admin()) WITH CHECK (user_id = public.current_app_user_id() OR public.current_app_is_admin());

ALTER TABLE public.tbl_turn_state ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tbl_turn_state_admin_only ON public.tbl_turn_state;
CREATE POLICY tbl_turn_state_admin_only ON public.tbl_turn_state FOR ALL USING (public.current_app_is_admin() OR auth.role() = 'service_role') WITH CHECK (public.current_app_is_admin() OR auth.role() = 'service_role');

ALTER TABLE public.tbl_user ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tbl_user_owner_select ON public.tbl_user;
CREATE POLICY tbl_user_owner_select ON public.tbl_user FOR SELECT USING (user_id = public.current_app_user_id() OR public.current_app_is_admin());
DROP POLICY IF EXISTS tbl_user_owner_modify ON public.tbl_user;
CREATE POLICY tbl_user_owner_modify ON public.tbl_user FOR ALL USING (user_id = public.current_app_user_id() OR public.current_app_is_admin()) WITH CHECK (user_id = public.current_app_user_id() OR public.current_app_is_admin());

ALTER TABLE public.tbl_user_badge ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tbl_user_badge_owner_select ON public.tbl_user_badge;
CREATE POLICY tbl_user_badge_owner_select ON public.tbl_user_badge FOR SELECT USING (user_id = public.current_app_user_id() OR public.current_app_is_admin());
DROP POLICY IF EXISTS tbl_user_badge_owner_modify ON public.tbl_user_badge;
CREATE POLICY tbl_user_badge_owner_modify ON public.tbl_user_badge FOR ALL USING (user_id = public.current_app_user_id() OR public.current_app_is_admin()) WITH CHECK (user_id = public.current_app_user_id() OR public.current_app_is_admin());

ALTER TABLE public.tbl_user_streak ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tbl_user_streak_owner_select ON public.tbl_user_streak;
CREATE POLICY tbl_user_streak_owner_select ON public.tbl_user_streak FOR SELECT USING (user_id = public.current_app_user_id() OR public.current_app_is_admin());
DROP POLICY IF EXISTS tbl_user_streak_owner_modify ON public.tbl_user_streak;
CREATE POLICY tbl_user_streak_owner_modify ON public.tbl_user_streak FOR ALL USING (user_id = public.current_app_user_id() OR public.current_app_is_admin()) WITH CHECK (user_id = public.current_app_user_id() OR public.current_app_is_admin());

ALTER TABLE public.tbl_utterance ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tbl_utterance_admin_only ON public.tbl_utterance;
CREATE POLICY tbl_utterance_admin_only ON public.tbl_utterance FOR ALL USING (public.current_app_is_admin() OR auth.role() = 'service_role') WITH CHECK (public.current_app_is_admin() OR auth.role() = 'service_role');

ALTER TABLE public.tbl_voice_analysis ENABLE ROW LEVEL SECURITY;
DROP POLICY IF EXISTS tbl_voice_analysis_owner_select ON public.tbl_voice_analysis;
CREATE POLICY tbl_voice_analysis_owner_select ON public.tbl_voice_analysis FOR SELECT USING (user_id = public.current_app_user_id() OR public.current_app_is_admin());
DROP POLICY IF EXISTS tbl_voice_analysis_owner_modify ON public.tbl_voice_analysis;
CREATE POLICY tbl_voice_analysis_owner_modify ON public.tbl_voice_analysis FOR ALL USING (user_id = public.current_app_user_id() OR public.current_app_is_admin()) WITH CHECK (user_id = public.current_app_user_id() OR public.current_app_is_admin());

COMMIT;
