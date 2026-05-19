-- ============================================
-- File: 09_sequences_reset.sql
-- Description: Reset PostgreSQL serial sequences after data loads.
-- Run order: 9 of 10
-- Dependencies: 01_extensions.sql, 02_schema.sql, 03_constraints_indexes.sql, 04_views.sql, 05_functions.sql, 06_stored_procedures.sql, 07_triggers.sql, 08_seed_data.sql
-- ============================================
-- Tables migrated: 17
-- Views migrated: 0
-- Functions migrated: 0
-- Stored Procedures migrated: 79
-- Triggers migrated: 0
-- Indexes migrated: 33
-- Known incompatibilities: NONE
-- Manual review required: Run after bulk data loads; empty tables intentionally reset to 1 via COALESCE.


BEGIN;

SELECT setval(pg_get_serial_sequence('public.tbl_admin_note', 'admin_note_id'), COALESCE((SELECT MAX(admin_note_id) FROM public.tbl_admin_note), 1), TRUE);
SELECT setval(pg_get_serial_sequence('public.tbl_dashboard_metric', 'dashboard_metric_id'), COALESCE((SELECT MAX(dashboard_metric_id) FROM public.tbl_dashboard_metric), 1), TRUE);
SELECT setval(pg_get_serial_sequence('public.tbl_listener_feedback', 'listener_feedback_id'), COALESCE((SELECT MAX(listener_feedback_id) FROM public.tbl_listener_feedback), 1), TRUE);
SELECT setval(pg_get_serial_sequence('public.tbl_mistake', 'mistake_id'), COALESCE((SELECT MAX(mistake_id) FROM public.tbl_mistake), 1), TRUE);
SELECT setval(pg_get_serial_sequence('public.tbl_refresh_token', 'refresh_token_id'), COALESCE((SELECT MAX(refresh_token_id) FROM public.tbl_refresh_token), 1), TRUE);
SELECT setval(pg_get_serial_sequence('public.tbl_repractice_session', 'repractice_session_id'), COALESCE((SELECT MAX(repractice_session_id) FROM public.tbl_repractice_session), 1), TRUE);
SELECT setval(pg_get_serial_sequence('public.tbl_repractice_utterance', 'repractice_utterance_id'), COALESCE((SELECT MAX(repractice_utterance_id) FROM public.tbl_repractice_utterance), 1), TRUE);
SELECT setval(pg_get_serial_sequence('public.tbl_script', 'script_id'), COALESCE((SELECT MAX(script_id) FROM public.tbl_script), 1), TRUE);
SELECT setval(pg_get_serial_sequence('public.tbl_script_version', 'script_version_id'), COALESCE((SELECT MAX(script_version_id) FROM public.tbl_script_version), 1), TRUE);
SELECT setval(pg_get_serial_sequence('public.tbl_session', 'session_id'), COALESCE((SELECT MAX(session_id) FROM public.tbl_session), 1), TRUE);
SELECT setval(pg_get_serial_sequence('public.tbl_session_member', 'session_member_id'), COALESCE((SELECT MAX(session_member_id) FROM public.tbl_session_member), 1), TRUE);
SELECT setval(pg_get_serial_sequence('public.tbl_turn_state', 'turn_state_id'), COALESCE((SELECT MAX(turn_state_id) FROM public.tbl_turn_state), 1), TRUE);
SELECT setval(pg_get_serial_sequence('public.tbl_user', 'user_id'), COALESCE((SELECT MAX(user_id) FROM public.tbl_user), 1), TRUE);
SELECT setval(pg_get_serial_sequence('public.tbl_user_badge', 'user_badge_id'), COALESCE((SELECT MAX(user_badge_id) FROM public.tbl_user_badge), 1), TRUE);
SELECT setval(pg_get_serial_sequence('public.tbl_user_streak', 'user_streak_id'), COALESCE((SELECT MAX(user_streak_id) FROM public.tbl_user_streak), 1), TRUE);
SELECT setval(pg_get_serial_sequence('public.tbl_utterance', 'utterance_id'), COALESCE((SELECT MAX(utterance_id) FROM public.tbl_utterance), 1), TRUE);
SELECT setval(pg_get_serial_sequence('public.tbl_voice_analysis', 'voice_analysis_id'), COALESCE((SELECT MAX(voice_analysis_id) FROM public.tbl_voice_analysis), 1), TRUE);

COMMIT;
