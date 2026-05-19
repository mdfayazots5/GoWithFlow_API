-- ============================================
-- File: 06_stored_procedures.sql
-- Description: Create PostgreSQL procedure entry points corresponding to the live SQL Server stored procedure catalog.
-- Run order: 6 of 10
-- Dependencies: 01_extensions.sql, 02_schema.sql, 03_constraints_indexes.sql, 04_views.sql, 05_functions.sql
-- ============================================
-- Tables migrated: 17
-- Views migrated: 0
-- Functions migrated: 0
-- Stored Procedures migrated: 79
-- Triggers migrated: 0
-- Indexes migrated: 33
-- Known incompatibilities: Automated procedural translation is unsafe for this catalog because it contains SQL Server-specific rowset procedures, TVPs, OPENJSON analytics, TOP queries, local transaction control, and residual OTP objects after table removal.
-- Manual review required: Every procedure body is emitted as an executable PostgreSQL stub that raises a manual-review exception until the SQL Server logic is rewritten and validated.


BEGIN;

-- Procedure: public.usp_bulk_insert_utterance
-- [MANUAL REVIEW REQUIRED: Uses explicit SQL Server transaction control inside the procedure body.]
-- [MANUAL REVIEW REQUIRED: Uses SQL Server exception syntax that must be rewritten for PL/pgSQL.]
CREATE OR REPLACE PROCEDURE public.usp_bulk_insert_utterance(IN p_script_id bigint, IN p_utterances jsonb, IN p_created_by varchar(128), IN p_ip_address varchar(64))
LANGUAGE plpgsql
AS $$
BEGIN
    RAISE EXCEPTION 'Manual review required: convert SQL Server procedure uspBulkInsertUtterance to validated PL/pgSQL before production use.';
EXCEPTION WHEN OTHERS THEN
    RAISE;
END;
$$;

-- Procedure: public.usp_calculate_improvement_percent_by_user_id
-- [MANUAL REVIEW REQUIRED: Uses SQL Server exception syntax that must be rewritten for PL/pgSQL.]
CREATE OR REPLACE PROCEDURE public.usp_calculate_improvement_percent_by_user_id(IN p_user_id bigint, INOUT p_result refcursor DEFAULT 'usp_calculate_improvement_percent_by_user_id_result')
LANGUAGE plpgsql
AS $$
BEGIN
    RAISE EXCEPTION 'Manual review required: convert SQL Server procedure uspCalculateImprovementPercentByUserId and return rows through refcursor p_result before enabling PostgreSQL clients.';
EXCEPTION WHEN OTHERS THEN
    RAISE;
END;
$$;

-- Procedure: public.usp_check_and_award_badge
-- [MANUAL REVIEW REQUIRED: Uses SQL Server exception syntax that must be rewritten for PL/pgSQL.]
CREATE OR REPLACE PROCEDURE public.usp_check_and_award_badge(IN p_user_id bigint, IN p_created_by varchar(128), IN p_ip_address varchar(64))
LANGUAGE plpgsql
AS $$
BEGIN
    RAISE EXCEPTION 'Manual review required: convert SQL Server procedure uspCheckAndAwardBadge to validated PL/pgSQL before production use.';
EXCEPTION WHEN OTHERS THEN
    RAISE;
END;
$$;

-- Procedure: public.usp_check_script_title_exists
-- [MANUAL REVIEW REQUIRED: Uses TOP and needs LIMIT/FETCH rewrites.]
-- [MANUAL REVIEW REQUIRED: Uses SQL Server exception syntax that must be rewritten for PL/pgSQL.]
CREATE OR REPLACE PROCEDURE public.usp_check_script_title_exists(IN p_script_title varchar(128), INOUT p_result refcursor DEFAULT 'usp_check_script_title_exists_result')
LANGUAGE plpgsql
AS $$
BEGIN
    RAISE EXCEPTION 'Manual review required: convert SQL Server procedure uspCheckScriptTitleExists and return rows through refcursor p_result before enabling PostgreSQL clients.';
EXCEPTION WHEN OTHERS THEN
    RAISE;
END;
$$;

-- Procedure: public.usp_export_user_report_data
-- [MANUAL REVIEW REQUIRED: Uses SQL Server exception syntax that must be rewritten for PL/pgSQL.]
-- [MANUAL REVIEW REQUIRED: Uses SQL Server date/time conversion functions that need PostgreSQL rewrites.]
CREATE OR REPLACE PROCEDURE public.usp_export_user_report_data(IN p_from_date timestamptz, IN p_to_date timestamptz, IN p_user_id bigint, INOUT p_result refcursor DEFAULT 'usp_export_user_report_data_result')
LANGUAGE plpgsql
AS $$
BEGIN
    RAISE EXCEPTION 'Manual review required: convert SQL Server procedure uspExportUserReportData and return rows through refcursor p_result before enabling PostgreSQL clients.';
EXCEPTION WHEN OTHERS THEN
    RAISE;
END;
$$;

-- Procedure: public.usp_get_admin_analytics_overview
-- [MANUAL REVIEW REQUIRED: Uses TOP and needs LIMIT/FETCH rewrites.]
-- [MANUAL REVIEW REQUIRED: Uses SQL Server exception syntax that must be rewritten for PL/pgSQL.]
-- [MANUAL REVIEW REQUIRED: Uses SQL Server date/time conversion functions that need PostgreSQL rewrites.]
CREATE OR REPLACE PROCEDURE public.usp_get_admin_analytics_overview(INOUT p_result refcursor DEFAULT 'usp_get_admin_analytics_overview_result')
LANGUAGE plpgsql
AS $$
BEGIN
    RAISE EXCEPTION 'Manual review required: convert SQL Server procedure uspGetAdminAnalyticsOverview and return rows through refcursor p_result before enabling PostgreSQL clients.';
EXCEPTION WHEN OTHERS THEN
    RAISE;
END;
$$;

-- Procedure: public.usp_get_admin_dashboard_summary
-- [MANUAL REVIEW REQUIRED: Uses SQL Server exception syntax that must be rewritten for PL/pgSQL.]
CREATE OR REPLACE PROCEDURE public.usp_get_admin_dashboard_summary(INOUT p_result refcursor DEFAULT 'usp_get_admin_dashboard_summary_result')
LANGUAGE plpgsql
AS $$
BEGIN
    RAISE EXCEPTION 'Manual review required: convert SQL Server procedure uspGetAdminDashboardSummary and return rows through refcursor p_result before enabling PostgreSQL clients.';
EXCEPTION WHEN OTHERS THEN
    RAISE;
END;
$$;

-- Procedure: public.usp_get_admin_note_by_target_user_id
-- [MANUAL REVIEW REQUIRED: Uses SQL Server exception syntax that must be rewritten for PL/pgSQL.]
CREATE OR REPLACE PROCEDURE public.usp_get_admin_note_by_target_user_id(IN p_target_user_id bigint, INOUT p_result refcursor DEFAULT 'usp_get_admin_note_by_target_user_id_result')
LANGUAGE plpgsql
AS $$
BEGIN
    RAISE EXCEPTION 'Manual review required: convert SQL Server procedure uspGetAdminNoteByTargetUserId and return rows through refcursor p_result before enabling PostgreSQL clients.';
EXCEPTION WHEN OTHERS THEN
    RAISE;
END;
$$;

-- Procedure: public.usp_get_all_mistake_type_count_by_user_id
-- [MANUAL REVIEW REQUIRED: Uses SQL Server exception syntax that must be rewritten for PL/pgSQL.]
CREATE OR REPLACE PROCEDURE public.usp_get_all_mistake_type_count_by_user_id(IN p_user_id bigint, INOUT p_result refcursor DEFAULT 'usp_get_all_mistake_type_count_by_user_id_result')
LANGUAGE plpgsql
AS $$
BEGIN
    RAISE EXCEPTION 'Manual review required: convert SQL Server procedure uspGetAllMistakeTypeCountByUserId and return rows through refcursor p_result before enabling PostgreSQL clients.';
EXCEPTION WHEN OTHERS THEN
    RAISE;
END;
$$;

-- Procedure: public.usp_get_all_user_by_search
-- [MANUAL REVIEW REQUIRED: Uses SQL Server exception syntax that must be rewritten for PL/pgSQL.]
CREATE OR REPLACE PROCEDURE public.usp_get_all_user_by_search(IN p_search_term varchar(128), IN p_age_group varchar(32), IN p_is_active boolean, IN p_page_number integer, IN p_page_size integer, INOUT p_result refcursor DEFAULT 'usp_get_all_user_by_search_result')
LANGUAGE plpgsql
AS $$
BEGIN
    RAISE EXCEPTION 'Manual review required: convert SQL Server procedure uspGetAllUserBySearch and return rows through refcursor p_result before enabling PostgreSQL clients.';
EXCEPTION WHEN OTHERS THEN
    RAISE;
END;
$$;

-- Procedure: public.usp_get_analytics_summary_by_user_id
-- [MANUAL REVIEW REQUIRED: Uses OPENJSON and needs JSONB/recordset rewrites.]
-- [MANUAL REVIEW REQUIRED: Uses TOP and needs LIMIT/FETCH rewrites.]
-- [MANUAL REVIEW REQUIRED: Uses SQL Server exception syntax that must be rewritten for PL/pgSQL.]
-- [MANUAL REVIEW REQUIRED: Uses SQL Server date/time conversion functions that need PostgreSQL rewrites.]
CREATE OR REPLACE PROCEDURE public.usp_get_analytics_summary_by_user_id(IN p_user_id bigint, INOUT p_result refcursor DEFAULT 'usp_get_analytics_summary_by_user_id_result')
LANGUAGE plpgsql
AS $$
BEGIN
    RAISE EXCEPTION 'Manual review required: convert SQL Server procedure uspGetAnalyticsSummaryByUserId and return rows through refcursor p_result before enabling PostgreSQL clients.';
EXCEPTION WHEN OTHERS THEN
    RAISE;
END;
$$;

-- Procedure: public.usp_get_available_slots_by_session_id
-- [MANUAL REVIEW REQUIRED: Uses TOP and needs LIMIT/FETCH rewrites.]
-- [MANUAL REVIEW REQUIRED: Uses SQL Server exception syntax that must be rewritten for PL/pgSQL.]
CREATE OR REPLACE PROCEDURE public.usp_get_available_slots_by_session_id(IN p_session_id bigint, INOUT p_result refcursor DEFAULT 'usp_get_available_slots_by_session_id_result')
LANGUAGE plpgsql
AS $$
BEGIN
    RAISE EXCEPTION 'Manual review required: convert SQL Server procedure uspGetAvailableSlotsBySessionId and return rows through refcursor p_result before enabling PostgreSQL clients.';
EXCEPTION WHEN OTHERS THEN
    RAISE;
END;
$$;

-- Procedure: public.usp_get_current_turn_by_session_id
-- [MANUAL REVIEW REQUIRED: Uses TOP and needs LIMIT/FETCH rewrites.]
-- [MANUAL REVIEW REQUIRED: Uses SQL Server exception syntax that must be rewritten for PL/pgSQL.]
CREATE OR REPLACE PROCEDURE public.usp_get_current_turn_by_session_id(IN p_session_id bigint, INOUT p_result refcursor DEFAULT 'usp_get_current_turn_by_session_id_result')
LANGUAGE plpgsql
AS $$
BEGIN
    RAISE EXCEPTION 'Manual review required: convert SQL Server procedure uspGetCurrentTurnBySessionId and return rows through refcursor p_result before enabling PostgreSQL clients.';
EXCEPTION WHEN OTHERS THEN
    RAISE;
END;
$$;

-- Procedure: public.usp_get_grammar_progress_by_user_id
-- [MANUAL REVIEW REQUIRED: Uses SQL Server exception syntax that must be rewritten for PL/pgSQL.]
CREATE OR REPLACE PROCEDURE public.usp_get_grammar_progress_by_user_id(IN p_user_id bigint, INOUT p_result refcursor DEFAULT 'usp_get_grammar_progress_by_user_id_result')
LANGUAGE plpgsql
AS $$
BEGIN
    RAISE EXCEPTION 'Manual review required: convert SQL Server procedure uspGetGrammarProgressByUserId and return rows through refcursor p_result before enabling PostgreSQL clients.';
EXCEPTION WHEN OTHERS THEN
    RAISE;
END;
$$;

-- Procedure: public.usp_get_improvement_data_by_user_id
-- [MANUAL REVIEW REQUIRED: Uses OPENJSON and needs JSONB/recordset rewrites.]
-- [MANUAL REVIEW REQUIRED: Uses TOP and needs LIMIT/FETCH rewrites.]
-- [MANUAL REVIEW REQUIRED: Uses SQL Server exception syntax that must be rewritten for PL/pgSQL.]
CREATE OR REPLACE PROCEDURE public.usp_get_improvement_data_by_user_id(IN p_user_id bigint, INOUT p_result refcursor DEFAULT 'usp_get_improvement_data_by_user_id_result')
LANGUAGE plpgsql
AS $$
BEGIN
    RAISE EXCEPTION 'Manual review required: convert SQL Server procedure uspGetImprovementDataByUserId and return rows through refcursor p_result before enabling PostgreSQL clients.';
EXCEPTION WHEN OTHERS THEN
    RAISE;
END;
$$;

-- Procedure: public.usp_get_listener_feedback_by_session_id
-- [MANUAL REVIEW REQUIRED: Uses SQL Server exception syntax that must be rewritten for PL/pgSQL.]
CREATE OR REPLACE PROCEDURE public.usp_get_listener_feedback_by_session_id(IN p_session_id bigint, INOUT p_result refcursor DEFAULT 'usp_get_listener_feedback_by_session_id_result')
LANGUAGE plpgsql
AS $$
BEGIN
    RAISE EXCEPTION 'Manual review required: convert SQL Server procedure uspGetListenerFeedbackBySessionId and return rows through refcursor p_result before enabling PostgreSQL clients.';
EXCEPTION WHEN OTHERS THEN
    RAISE;
END;
$$;

-- Procedure: public.usp_get_mistake_by_user_id_with_filter
-- [MANUAL REVIEW REQUIRED: Uses SQL Server exception syntax that must be rewritten for PL/pgSQL.]
CREATE OR REPLACE PROCEDURE public.usp_get_mistake_by_user_id_with_filter(IN p_user_id bigint, IN p_mistake_type varchar(32), IN p_is_resolved boolean, IN p_page_number integer, IN p_page_size integer, INOUT p_result refcursor DEFAULT 'usp_get_mistake_by_user_id_with_filter_result')
LANGUAGE plpgsql
AS $$
BEGIN
    RAISE EXCEPTION 'Manual review required: convert SQL Server procedure uspGetMistakeByUserIdWithFilter and return rows through refcursor p_result before enabling PostgreSQL clients.';
EXCEPTION WHEN OTHERS THEN
    RAISE;
END;
$$;

-- Procedure: public.usp_get_mistake_summary_by_user_id
-- [MANUAL REVIEW REQUIRED: Uses SQL Server exception syntax that must be rewritten for PL/pgSQL.]
CREATE OR REPLACE PROCEDURE public.usp_get_mistake_summary_by_user_id(IN p_user_id bigint, INOUT p_result refcursor DEFAULT 'usp_get_mistake_summary_by_user_id_result')
LANGUAGE plpgsql
AS $$
BEGIN
    RAISE EXCEPTION 'Manual review required: convert SQL Server procedure uspGetMistakeSummaryByUserId and return rows through refcursor p_result before enabling PostgreSQL clients.';
EXCEPTION WHEN OTHERS THEN
    RAISE;
END;
$$;

-- Procedure: public.usp_get_recent_activity_list
-- [MANUAL REVIEW REQUIRED: Uses TOP and needs LIMIT/FETCH rewrites.]
-- [MANUAL REVIEW REQUIRED: Uses SQL Server exception syntax that must be rewritten for PL/pgSQL.]
CREATE OR REPLACE PROCEDURE public.usp_get_recent_activity_list(IN p_top_n integer, INOUT p_result refcursor DEFAULT 'usp_get_recent_activity_list_result')
LANGUAGE plpgsql
AS $$
BEGIN
    RAISE EXCEPTION 'Manual review required: convert SQL Server procedure uspGetRecentActivityList and return rows through refcursor p_result before enabling PostgreSQL clients.';
EXCEPTION WHEN OTHERS THEN
    RAISE;
END;
$$;

-- Procedure: public.usp_get_refresh_token_by_token
-- [MANUAL REVIEW REQUIRED: Uses SQL Server exception syntax that must be rewritten for PL/pgSQL.]
CREATE OR REPLACE PROCEDURE public.usp_get_refresh_token_by_token(IN p_token varchar(512), INOUT p_result refcursor DEFAULT 'usp_get_refresh_token_by_token_result')
LANGUAGE plpgsql
AS $$
BEGIN
    RAISE EXCEPTION 'Manual review required: convert SQL Server procedure uspGetRefreshTokenByToken and return rows through refcursor p_result before enabling PostgreSQL clients.';
EXCEPTION WHEN OTHERS THEN
    RAISE;
END;
$$;

-- Procedure: public.usp_get_repractice_session_by_repractice_session_id
-- [MANUAL REVIEW REQUIRED: Uses SQL Server exception syntax that must be rewritten for PL/pgSQL.]
CREATE OR REPLACE PROCEDURE public.usp_get_repractice_session_by_repractice_session_id(IN p_repractice_session_id bigint, INOUT p_result refcursor DEFAULT 'usp_get_repractice_session_by_repractice_session_id_result')
LANGUAGE plpgsql
AS $$
BEGIN
    RAISE EXCEPTION 'Manual review required: convert SQL Server procedure uspGetRepracticeSessionByRepracticeSessionId and return rows through refcursor p_result before enabling PostgreSQL clients.';
EXCEPTION WHEN OTHERS THEN
    RAISE;
END;
$$;

-- Procedure: public.usp_get_repractice_session_list_by_user_id
-- [MANUAL REVIEW REQUIRED: Uses SQL Server exception syntax that must be rewritten for PL/pgSQL.]
CREATE OR REPLACE PROCEDURE public.usp_get_repractice_session_list_by_user_id(IN p_user_id bigint, IN p_status varchar(16), IN p_page_number integer, IN p_page_size integer, INOUT p_result refcursor DEFAULT 'usp_get_repractice_session_list_by_user_id_result')
LANGUAGE plpgsql
AS $$
BEGIN
    RAISE EXCEPTION 'Manual review required: convert SQL Server procedure uspGetRepracticeSessionListByUserId and return rows through refcursor p_result before enabling PostgreSQL clients.';
EXCEPTION WHEN OTHERS THEN
    RAISE;
END;
$$;

-- Procedure: public.usp_get_script_by_search
-- [MANUAL REVIEW REQUIRED: Uses SQL Server exception syntax that must be rewritten for PL/pgSQL.]
CREATE OR REPLACE PROCEDURE public.usp_get_script_by_search(IN p_search_term varchar(128), IN p_category varchar(64), IN p_grammar_focus_tag varchar(64), IN p_target_age_group varchar(32), IN p_is_active boolean, IN p_page_number integer, IN p_page_size integer, INOUT p_result refcursor DEFAULT 'usp_get_script_by_search_result')
LANGUAGE plpgsql
AS $$
BEGIN
    RAISE EXCEPTION 'Manual review required: convert SQL Server procedure uspGetScriptBySearch and return rows through refcursor p_result before enabling PostgreSQL clients.';
EXCEPTION WHEN OTHERS THEN
    RAISE;
END;
$$;

-- Procedure: public.usp_get_script_detail_by_script_id
-- [MANUAL REVIEW REQUIRED: Uses SQL Server exception syntax that must be rewritten for PL/pgSQL.]
CREATE OR REPLACE PROCEDURE public.usp_get_script_detail_by_script_id(IN p_script_id bigint, INOUT p_result refcursor DEFAULT 'usp_get_script_detail_by_script_id_result')
LANGUAGE plpgsql
AS $$
BEGIN
    RAISE EXCEPTION 'Manual review required: convert SQL Server procedure uspGetScriptDetailByScriptId and return rows through refcursor p_result before enabling PostgreSQL clients.';
EXCEPTION WHEN OTHERS THEN
    RAISE;
END;
$$;

-- Procedure: public.usp_get_script_version_history_by_script_id
-- [MANUAL REVIEW REQUIRED: Uses SQL Server exception syntax that must be rewritten for PL/pgSQL.]
CREATE OR REPLACE PROCEDURE public.usp_get_script_version_history_by_script_id(IN p_script_id bigint, INOUT p_result refcursor DEFAULT 'usp_get_script_version_history_by_script_id_result')
LANGUAGE plpgsql
AS $$
BEGIN
    RAISE EXCEPTION 'Manual review required: convert SQL Server procedure uspGetScriptVersionHistoryByScriptId and return rows through refcursor p_result before enabling PostgreSQL clients.';
EXCEPTION WHEN OTHERS THEN
    RAISE;
END;
$$;

-- Procedure: public.usp_get_session_by_join_code
-- [MANUAL REVIEW REQUIRED: Uses TOP and needs LIMIT/FETCH rewrites.]
-- [MANUAL REVIEW REQUIRED: Uses SQL Server exception syntax that must be rewritten for PL/pgSQL.]
CREATE OR REPLACE PROCEDURE public.usp_get_session_by_join_code(IN p_join_code varchar(8), INOUT p_result refcursor DEFAULT 'usp_get_session_by_join_code_result')
LANGUAGE plpgsql
AS $$
BEGIN
    RAISE EXCEPTION 'Manual review required: convert SQL Server procedure uspGetSessionByJoinCode and return rows through refcursor p_result before enabling PostgreSQL clients.';
EXCEPTION WHEN OTHERS THEN
    RAISE;
END;
$$;

-- Procedure: public.usp_get_session_by_session_id
-- [MANUAL REVIEW REQUIRED: Logic rewrite required from T-SQL to PL/pgSQL before production use.]
CREATE OR REPLACE PROCEDURE public.usp_get_session_by_session_id(IN p_session_id bigint, INOUT p_result refcursor DEFAULT 'usp_get_session_by_session_id_result')
LANGUAGE plpgsql
AS $$
BEGIN
    RAISE EXCEPTION 'Manual review required: convert SQL Server procedure uspGetSessionBySessionId and return rows through refcursor p_result before enabling PostgreSQL clients.';
EXCEPTION WHEN OTHERS THEN
    RAISE;
END;
$$;

-- Procedure: public.usp_get_session_completion_summary
-- [MANUAL REVIEW REQUIRED: Uses OPENJSON and needs JSONB/recordset rewrites.]
-- [MANUAL REVIEW REQUIRED: Uses SQL Server exception syntax that must be rewritten for PL/pgSQL.]
CREATE OR REPLACE PROCEDURE public.usp_get_session_completion_summary(IN p_session_id bigint, INOUT p_result refcursor DEFAULT 'usp_get_session_completion_summary_result')
LANGUAGE plpgsql
AS $$
BEGIN
    RAISE EXCEPTION 'Manual review required: convert SQL Server procedure uspGetSessionCompletionSummary and return rows through refcursor p_result before enabling PostgreSQL clients.';
EXCEPTION WHEN OTHERS THEN
    RAISE;
END;
$$;

-- Procedure: public.usp_get_session_detail_by_session_id
-- [MANUAL REVIEW REQUIRED: Uses NOLOCK hints that have no PostgreSQL equivalent.]
-- [MANUAL REVIEW REQUIRED: Uses SQL Server exception syntax that must be rewritten for PL/pgSQL.]
-- [MANUAL REVIEW REQUIRED: Returns multiple SQL Server result sets and needs a PostgreSQL cursor or JSON contract redesign.]
CREATE OR REPLACE PROCEDURE public.usp_get_session_detail_by_session_id(IN p_session_id bigint, IN p_user_id bigint, INOUT p_result refcursor DEFAULT 'usp_get_session_detail_by_session_id_result')
LANGUAGE plpgsql
AS $$
BEGIN
    RAISE EXCEPTION 'Manual review required: convert SQL Server procedure uspGetSessionDetailBySessionId and return rows through refcursor p_result before enabling PostgreSQL clients.';
EXCEPTION WHEN OTHERS THEN
    RAISE;
END;
$$;

-- Procedure: public.usp_get_session_list_by_user_id
-- [MANUAL REVIEW REQUIRED: Uses SQL Server exception syntax that must be rewritten for PL/pgSQL.]
CREATE OR REPLACE PROCEDURE public.usp_get_session_list_by_user_id(IN p_user_id bigint, IN p_status_filter varchar(16), IN p_page_number integer, IN p_page_size integer, INOUT p_result refcursor DEFAULT 'usp_get_session_list_by_user_id_result')
LANGUAGE plpgsql
AS $$
BEGIN
    RAISE EXCEPTION 'Manual review required: convert SQL Server procedure uspGetSessionListByUserId and return rows through refcursor p_result before enabling PostgreSQL clients.';
EXCEPTION WHEN OTHERS THEN
    RAISE;
END;
$$;

-- Procedure: public.usp_get_streak_data_by_user_id
-- [MANUAL REVIEW REQUIRED: Uses TOP and needs LIMIT/FETCH rewrites.]
-- [MANUAL REVIEW REQUIRED: Uses SQL Server exception syntax that must be rewritten for PL/pgSQL.]
-- [MANUAL REVIEW REQUIRED: Uses SQL Server date/time conversion functions that need PostgreSQL rewrites.]
CREATE OR REPLACE PROCEDURE public.usp_get_streak_data_by_user_id(IN p_user_id bigint, INOUT p_result refcursor DEFAULT 'usp_get_streak_data_by_user_id_result')
LANGUAGE plpgsql
AS $$
BEGIN
    RAISE EXCEPTION 'Manual review required: convert SQL Server procedure uspGetStreakDataByUserId and return rows through refcursor p_result before enabling PostgreSQL clients.';
EXCEPTION WHEN OTHERS THEN
    RAISE;
END;
$$;

-- Procedure: public.usp_get_top_grammar_mistake_type
-- [MANUAL REVIEW REQUIRED: Uses TOP and needs LIMIT/FETCH rewrites.]
-- [MANUAL REVIEW REQUIRED: Uses SQL Server exception syntax that must be rewritten for PL/pgSQL.]
CREATE OR REPLACE PROCEDURE public.usp_get_top_grammar_mistake_type(IN p_top_n integer, INOUT p_result refcursor DEFAULT 'usp_get_top_grammar_mistake_type_result')
LANGUAGE plpgsql
AS $$
BEGIN
    RAISE EXCEPTION 'Manual review required: convert SQL Server procedure uspGetTopGrammarMistakeType and return rows through refcursor p_result before enabling PostgreSQL clients.';
EXCEPTION WHEN OTHERS THEN
    RAISE;
END;
$$;

-- Procedure: public.usp_get_top_performer_list_by_session_id
-- [MANUAL REVIEW REQUIRED: Uses OPENJSON and needs JSONB/recordset rewrites.]
-- [MANUAL REVIEW REQUIRED: Uses SQL Server exception syntax that must be rewritten for PL/pgSQL.]
CREATE OR REPLACE PROCEDURE public.usp_get_top_performer_list_by_session_id(IN p_session_id bigint, INOUT p_result refcursor DEFAULT 'usp_get_top_performer_list_by_session_id_result')
LANGUAGE plpgsql
AS $$
BEGIN
    RAISE EXCEPTION 'Manual review required: convert SQL Server procedure uspGetTopPerformerListBySessionId and return rows through refcursor p_result before enabling PostgreSQL clients.';
EXCEPTION WHEN OTHERS THEN
    RAISE;
END;
$$;

-- Procedure: public.usp_get_unresolved_mistake_by_user_id
-- [MANUAL REVIEW REQUIRED: Uses SQL Server exception syntax that must be rewritten for PL/pgSQL.]
CREATE OR REPLACE PROCEDURE public.usp_get_unresolved_mistake_by_user_id(IN p_user_id bigint, IN p_source_session_id bigint, INOUT p_result refcursor DEFAULT 'usp_get_unresolved_mistake_by_user_id_result')
LANGUAGE plpgsql
AS $$
BEGIN
    RAISE EXCEPTION 'Manual review required: convert SQL Server procedure uspGetUnresolvedMistakeByUserId and return rows through refcursor p_result before enabling PostgreSQL clients.';
EXCEPTION WHEN OTHERS THEN
    RAISE;
END;
$$;

-- Procedure: public.usp_get_user_badge_by_user_id
-- [MANUAL REVIEW REQUIRED: Uses SQL Server exception syntax that must be rewritten for PL/pgSQL.]
CREATE OR REPLACE PROCEDURE public.usp_get_user_badge_by_user_id(IN p_user_id bigint, INOUT p_result refcursor DEFAULT 'usp_get_user_badge_by_user_id_result')
LANGUAGE plpgsql
AS $$
BEGIN
    RAISE EXCEPTION 'Manual review required: convert SQL Server procedure uspGetUserBadgeByUserId and return rows through refcursor p_result before enabling PostgreSQL clients.';
EXCEPTION WHEN OTHERS THEN
    RAISE;
END;
$$;

-- Procedure: public.usp_get_user_by_mobile_number
-- [MANUAL REVIEW REQUIRED: Uses SQL Server exception syntax that must be rewritten for PL/pgSQL.]
CREATE OR REPLACE PROCEDURE public.usp_get_user_by_mobile_number(IN p_mobile_number varchar(16), INOUT p_result refcursor DEFAULT 'usp_get_user_by_mobile_number_result')
LANGUAGE plpgsql
AS $$
BEGIN
    RAISE EXCEPTION 'Manual review required: convert SQL Server procedure uspGetUserByMobileNumber and return rows through refcursor p_result before enabling PostgreSQL clients.';
EXCEPTION WHEN OTHERS THEN
    RAISE;
END;
$$;

-- Procedure: public.usp_get_user_by_user_id
-- [MANUAL REVIEW REQUIRED: Uses SQL Server exception syntax that must be rewritten for PL/pgSQL.]
CREATE OR REPLACE PROCEDURE public.usp_get_user_by_user_id(IN p_user_id bigint, INOUT p_result refcursor DEFAULT 'usp_get_user_by_user_id_result')
LANGUAGE plpgsql
AS $$
BEGIN
    RAISE EXCEPTION 'Manual review required: convert SQL Server procedure uspGetUserByUserId and return rows through refcursor p_result before enabling PostgreSQL clients.';
EXCEPTION WHEN OTHERS THEN
    RAISE;
END;
$$;

-- Procedure: public.usp_get_user_dashboard_summary_by_user_id
-- [MANUAL REVIEW REQUIRED: Uses OPENJSON and needs JSONB/recordset rewrites.]
-- [MANUAL REVIEW REQUIRED: Uses TOP and needs LIMIT/FETCH rewrites.]
-- [MANUAL REVIEW REQUIRED: Uses SQL Server exception syntax that must be rewritten for PL/pgSQL.]
CREATE OR REPLACE PROCEDURE public.usp_get_user_dashboard_summary_by_user_id(IN p_user_id bigint, INOUT p_result refcursor DEFAULT 'usp_get_user_dashboard_summary_by_user_id_result')
LANGUAGE plpgsql
AS $$
BEGIN
    RAISE EXCEPTION 'Manual review required: convert SQL Server procedure uspGetUserDashboardSummaryByUserId and return rows through refcursor p_result before enabling PostgreSQL clients.';
EXCEPTION WHEN OTHERS THEN
    RAISE;
END;
$$;

-- Procedure: public.usp_get_user_detail_by_user_id
-- [MANUAL REVIEW REQUIRED: Uses TOP and needs LIMIT/FETCH rewrites.]
-- [MANUAL REVIEW REQUIRED: Uses SQL Server exception syntax that must be rewritten for PL/pgSQL.]
CREATE OR REPLACE PROCEDURE public.usp_get_user_detail_by_user_id(IN p_user_id bigint, INOUT p_result refcursor DEFAULT 'usp_get_user_detail_by_user_id_result')
LANGUAGE plpgsql
AS $$
BEGIN
    RAISE EXCEPTION 'Manual review required: convert SQL Server procedure uspGetUserDetailByUserId and return rows through refcursor p_result before enabling PostgreSQL clients.';
EXCEPTION WHEN OTHERS THEN
    RAISE;
END;
$$;

-- Procedure: public.usp_get_user_full_report_by_user_id
-- [MANUAL REVIEW REQUIRED: Uses SQL Server exception syntax that must be rewritten for PL/pgSQL.]
-- [MANUAL REVIEW REQUIRED: Uses SQL Server date/time conversion functions that need PostgreSQL rewrites.]
CREATE OR REPLACE PROCEDURE public.usp_get_user_full_report_by_user_id(IN p_user_id bigint, INOUT p_result refcursor DEFAULT 'usp_get_user_full_report_by_user_id_result')
LANGUAGE plpgsql
AS $$
BEGIN
    RAISE EXCEPTION 'Manual review required: convert SQL Server procedure uspGetUserFullReportByUserId and return rows through refcursor p_result before enabling PostgreSQL clients.';
EXCEPTION WHEN OTHERS THEN
    RAISE;
END;
$$;

-- Procedure: public.usp_get_user_profile_by_user_id
-- [MANUAL REVIEW REQUIRED: Uses SQL Server exception syntax that must be rewritten for PL/pgSQL.]
-- [MANUAL REVIEW REQUIRED: Uses SQL Server date/time conversion functions that need PostgreSQL rewrites.]
CREATE OR REPLACE PROCEDURE public.usp_get_user_profile_by_user_id(IN p_user_id bigint, INOUT p_result refcursor DEFAULT 'usp_get_user_profile_by_user_id_result')
LANGUAGE plpgsql
AS $$
BEGIN
    RAISE EXCEPTION 'Manual review required: convert SQL Server procedure uspGetUserProfileByUserId and return rows through refcursor p_result before enabling PostgreSQL clients.';
EXCEPTION WHEN OTHERS THEN
    RAISE;
END;
$$;

-- Procedure: public.usp_get_user_report_summary_list
-- [MANUAL REVIEW REQUIRED: Uses SQL Server exception syntax that must be rewritten for PL/pgSQL.]
-- [MANUAL REVIEW REQUIRED: Uses SQL Server date/time conversion functions that need PostgreSQL rewrites.]
CREATE OR REPLACE PROCEDURE public.usp_get_user_report_summary_list(IN p_from_date timestamptz, IN p_to_date timestamptz, IN p_user_id bigint, IN p_page_number integer, IN p_page_size integer, INOUT p_result refcursor DEFAULT 'usp_get_user_report_summary_list_result')
LANGUAGE plpgsql
AS $$
BEGIN
    RAISE EXCEPTION 'Manual review required: convert SQL Server procedure uspGetUserReportSummaryList and return rows through refcursor p_result before enabling PostgreSQL clients.';
EXCEPTION WHEN OTHERS THEN
    RAISE;
END;
$$;

-- Procedure: public.usp_get_voice_analysis_by_session_id
-- [MANUAL REVIEW REQUIRED: Uses SQL Server exception syntax that must be rewritten for PL/pgSQL.]
CREATE OR REPLACE PROCEDURE public.usp_get_voice_analysis_by_session_id(IN p_session_id bigint, INOUT p_result refcursor DEFAULT 'usp_get_voice_analysis_by_session_id_result')
LANGUAGE plpgsql
AS $$
BEGIN
    RAISE EXCEPTION 'Manual review required: convert SQL Server procedure uspGetVoiceAnalysisBySessionId and return rows through refcursor p_result before enabling PostgreSQL clients.';
EXCEPTION WHEN OTHERS THEN
    RAISE;
END;
$$;

-- Procedure: public.usp_get_voice_analysis_by_user_id
-- [MANUAL REVIEW REQUIRED: Uses SQL Server exception syntax that must be rewritten for PL/pgSQL.]
CREATE OR REPLACE PROCEDURE public.usp_get_voice_analysis_by_user_id(IN p_user_id bigint, IN p_session_id bigint, INOUT p_result refcursor DEFAULT 'usp_get_voice_analysis_by_user_id_result')
LANGUAGE plpgsql
AS $$
BEGIN
    RAISE EXCEPTION 'Manual review required: convert SQL Server procedure uspGetVoiceAnalysisByUserId and return rows through refcursor p_result before enabling PostgreSQL clients.';
EXCEPTION WHEN OTHERS THEN
    RAISE;
END;
$$;

-- Procedure: public.usp_get_weekly_fluency_score_by_user_id
-- [MANUAL REVIEW REQUIRED: Uses SQL Server exception syntax that must be rewritten for PL/pgSQL.]
-- [MANUAL REVIEW REQUIRED: Uses SQL Server date/time conversion functions that need PostgreSQL rewrites.]
CREATE OR REPLACE PROCEDURE public.usp_get_weekly_fluency_score_by_user_id(IN p_user_id bigint, INOUT p_result refcursor DEFAULT 'usp_get_weekly_fluency_score_by_user_id_result')
LANGUAGE plpgsql
AS $$
BEGIN
    RAISE EXCEPTION 'Manual review required: convert SQL Server procedure uspGetWeeklyFluencyScoreByUserId and return rows through refcursor p_result before enabling PostgreSQL clients.';
EXCEPTION WHEN OTHERS THEN
    RAISE;
END;
$$;

-- Procedure: public.usp_increment_re_read_count
-- [MANUAL REVIEW REQUIRED: Uses SQL Server exception syntax that must be rewritten for PL/pgSQL.]
CREATE OR REPLACE PROCEDURE public.usp_increment_re_read_count(IN p_turn_state_id bigint, IN p_updated_by varchar(128), IN p_ip_address varchar(64))
LANGUAGE plpgsql
AS $$
BEGIN
    RAISE EXCEPTION 'Manual review required: convert SQL Server procedure uspIncrementReReadCount to validated PL/pgSQL before production use.';
EXCEPTION WHEN OTHERS THEN
    RAISE;
END;
$$;

-- Procedure: public.usp_insert_admin_note
-- [MANUAL REVIEW REQUIRED: Uses SCOPE_IDENTITY and needs RETURNING/lastval review.]
-- [MANUAL REVIEW REQUIRED: Uses explicit SQL Server transaction control inside the procedure body.]
-- [MANUAL REVIEW REQUIRED: Uses SQL Server exception syntax that must be rewritten for PL/pgSQL.]
CREATE OR REPLACE PROCEDURE public.usp_insert_admin_note(IN p_admin_user_id bigint, IN p_target_user_id bigint, IN p_note_text varchar(512), IN p_created_by varchar(128), IN p_ip_address varchar(64), INOUT p_result refcursor DEFAULT 'usp_insert_admin_note_result')
LANGUAGE plpgsql
AS $$
BEGIN
    RAISE EXCEPTION 'Manual review required: convert SQL Server procedure uspInsertAdminNote and return rows through refcursor p_result before enabling PostgreSQL clients.';
EXCEPTION WHEN OTHERS THEN
    RAISE;
END;
$$;

-- Procedure: public.usp_insert_listener_feedback
-- [MANUAL REVIEW REQUIRED: Uses SQL Server exception syntax that must be rewritten for PL/pgSQL.]
CREATE OR REPLACE PROCEDURE public.usp_insert_listener_feedback(IN p_session_id bigint, IN p_turn_index integer, IN p_from_user_id bigint, IN p_target_user_id bigint, IN p_feedback_tag varchar(32), IN p_created_by varchar(128), IN p_ip_address varchar(64))
LANGUAGE plpgsql
AS $$
BEGIN
    RAISE EXCEPTION 'Manual review required: convert SQL Server procedure uspInsertListenerFeedback to validated PL/pgSQL before production use.';
EXCEPTION WHEN OTHERS THEN
    RAISE;
END;
$$;

-- Procedure: public.usp_insert_mistake
-- [MANUAL REVIEW REQUIRED: Uses SCOPE_IDENTITY and needs RETURNING/lastval review.]
-- [MANUAL REVIEW REQUIRED: Uses SQL Server exception syntax that must be rewritten for PL/pgSQL.]
CREATE OR REPLACE PROCEDURE public.usp_insert_mistake(IN p_user_id bigint, IN p_session_id bigint, IN p_utterance_id bigint, IN p_script_id bigint, IN p_utterance_text varchar(512), IN p_spoken_text varchar(512), IN p_mistake_type varchar(32), IN p_mistake_detail varchar(256), IN p_grammar_tag varchar(64), IN p_context_tag varchar(64), IN p_correction_text varchar(512), IN p_created_by varchar(128), IN p_ip_address varchar(64), INOUT p_result refcursor DEFAULT 'usp_insert_mistake_result')
LANGUAGE plpgsql
AS $$
BEGIN
    RAISE EXCEPTION 'Manual review required: convert SQL Server procedure uspInsertMistake and return rows through refcursor p_result before enabling PostgreSQL clients.';
EXCEPTION WHEN OTHERS THEN
    RAISE;
END;
$$;

-- Procedure: public.usp_insert_otp_verification
-- [MANUAL REVIEW REQUIRED: Uses SCOPE_IDENTITY and needs RETURNING/lastval review.]
-- [MANUAL REVIEW REQUIRED: Uses explicit SQL Server transaction control inside the procedure body.]
-- [MANUAL REVIEW REQUIRED: Uses SQL Server exception syntax that must be rewritten for PL/pgSQL.]
-- [MANUAL REVIEW REQUIRED: References the removed tblOtpVerification flow and must be redesigned before PostgreSQL deployment.]
CREATE OR REPLACE PROCEDURE public.usp_insert_otp_verification(IN p_mobile_number varchar(16), IN p_otp_code varchar(8), IN p_expires_at timestamptz, IN p_created_by varchar(128), IN p_ip_address varchar(64), INOUT p_result refcursor DEFAULT 'usp_insert_otp_verification_result')
LANGUAGE plpgsql
AS $$
BEGIN
    RAISE EXCEPTION 'Manual review required: convert SQL Server procedure uspInsertOtpVerification and return rows through refcursor p_result before enabling PostgreSQL clients.';
EXCEPTION WHEN OTHERS THEN
    RAISE;
END;
$$;

-- Procedure: public.usp_insert_refresh_token
-- [MANUAL REVIEW REQUIRED: Uses SCOPE_IDENTITY and needs RETURNING/lastval review.]
-- [MANUAL REVIEW REQUIRED: Uses explicit SQL Server transaction control inside the procedure body.]
-- [MANUAL REVIEW REQUIRED: Uses SQL Server exception syntax that must be rewritten for PL/pgSQL.]
CREATE OR REPLACE PROCEDURE public.usp_insert_refresh_token(IN p_user_id bigint, IN p_token varchar(512), IN p_expires_at timestamptz, IN p_device_info varchar(256), IN p_created_by varchar(128), IN p_ip_address varchar(64), INOUT p_result refcursor DEFAULT 'usp_insert_refresh_token_result')
LANGUAGE plpgsql
AS $$
BEGIN
    RAISE EXCEPTION 'Manual review required: convert SQL Server procedure uspInsertRefreshToken and return rows through refcursor p_result before enabling PostgreSQL clients.';
EXCEPTION WHEN OTHERS THEN
    RAISE;
END;
$$;

-- Procedure: public.usp_insert_repractice_session
-- [MANUAL REVIEW REQUIRED: Uses SCOPE_IDENTITY and needs RETURNING/lastval review.]
-- [MANUAL REVIEW REQUIRED: Uses SQL Server exception syntax that must be rewritten for PL/pgSQL.]
CREATE OR REPLACE PROCEDURE public.usp_insert_repractice_session(IN p_user_id bigint, IN p_source_session_id bigint, IN p_total_mistakes integer, IN p_created_by varchar(128), IN p_ip_address varchar(64), INOUT p_result refcursor DEFAULT 'usp_insert_repractice_session_result')
LANGUAGE plpgsql
AS $$
BEGIN
    RAISE EXCEPTION 'Manual review required: convert SQL Server procedure uspInsertRepracticeSession and return rows through refcursor p_result before enabling PostgreSQL clients.';
EXCEPTION WHEN OTHERS THEN
    RAISE;
END;
$$;

-- Procedure: public.usp_insert_repractice_utterance
-- [MANUAL REVIEW REQUIRED: Uses SCOPE_IDENTITY and needs RETURNING/lastval review.]
-- [MANUAL REVIEW REQUIRED: Uses SQL Server exception syntax that must be rewritten for PL/pgSQL.]
CREATE OR REPLACE PROCEDURE public.usp_insert_repractice_utterance(IN p_repractice_session_id bigint, IN p_mistake_id bigint, IN p_original_utterance_id bigint, IN p_english_text varchar(512), IN p_hint_text varchar(512), IN p_mistake_type varchar(32), IN p_mistake_detail varchar(256), IN p_correction_note varchar(512), IN p_created_by varchar(128), IN p_ip_address varchar(64), INOUT p_result refcursor DEFAULT 'usp_insert_repractice_utterance_result')
LANGUAGE plpgsql
AS $$
BEGIN
    RAISE EXCEPTION 'Manual review required: convert SQL Server procedure uspInsertRepracticeUtterance and return rows through refcursor p_result before enabling PostgreSQL clients.';
EXCEPTION WHEN OTHERS THEN
    RAISE;
END;
$$;

-- Procedure: public.usp_insert_script
-- [MANUAL REVIEW REQUIRED: Uses SCOPE_IDENTITY and needs RETURNING/lastval review.]
-- [MANUAL REVIEW REQUIRED: Uses explicit SQL Server transaction control inside the procedure body.]
-- [MANUAL REVIEW REQUIRED: Uses SQL Server exception syntax that must be rewritten for PL/pgSQL.]
CREATE OR REPLACE PROCEDURE public.usp_insert_script(IN p_script_title varchar(128), IN p_category varchar(64), IN p_grammar_focus_tag varchar(64), IN p_context_tag varchar(64), IN p_complexity_level smallint, IN p_target_age_group varchar(32), IN p_hint_language varchar(32), IN p_is_active boolean, IN p_uploaded_by_user_id bigint, IN p_version integer, IN p_created_by varchar(128), IN p_ip_address varchar(64), INOUT p_result refcursor DEFAULT 'usp_insert_script_result')
LANGUAGE plpgsql
AS $$
BEGIN
    RAISE EXCEPTION 'Manual review required: convert SQL Server procedure uspInsertScript and return rows through refcursor p_result before enabling PostgreSQL clients.';
EXCEPTION WHEN OTHERS THEN
    RAISE;
END;
$$;

-- Procedure: public.usp_insert_script_version
-- [MANUAL REVIEW REQUIRED: Uses SCOPE_IDENTITY and needs RETURNING/lastval review.]
-- [MANUAL REVIEW REQUIRED: Uses explicit SQL Server transaction control inside the procedure body.]
-- [MANUAL REVIEW REQUIRED: Uses SQL Server exception syntax that must be rewritten for PL/pgSQL.]
CREATE OR REPLACE PROCEDURE public.usp_insert_script_version(IN p_script_id bigint, IN p_version_number integer, IN p_version_notes varchar(256), IN p_uploaded_by_user_id bigint, IN p_created_by varchar(128), IN p_ip_address varchar(64), INOUT p_result refcursor DEFAULT 'usp_insert_script_version_result')
LANGUAGE plpgsql
AS $$
BEGIN
    RAISE EXCEPTION 'Manual review required: convert SQL Server procedure uspInsertScriptVersion and return rows through refcursor p_result before enabling PostgreSQL clients.';
EXCEPTION WHEN OTHERS THEN
    RAISE;
END;
$$;

-- Procedure: public.usp_insert_session
-- [MANUAL REVIEW REQUIRED: Uses SCOPE_IDENTITY and needs RETURNING/lastval review.]
-- [MANUAL REVIEW REQUIRED: Uses explicit SQL Server transaction control inside the procedure body.]
-- [MANUAL REVIEW REQUIRED: Uses SQL Server exception syntax that must be rewritten for PL/pgSQL.]
-- [MANUAL REVIEW REQUIRED: Uses SQL Server date/time conversion functions that need PostgreSQL rewrites.]
CREATE OR REPLACE PROCEDURE public.usp_insert_session(IN p_session_name varchar(128), IN p_session_mode varchar(64), IN p_max_members smallint, IN p_session_duration integer, IN p_host_user_id bigint, IN p_script_id bigint, IN p_room_expiry_minutes integer, IN p_created_by varchar(128), IN p_ip_address varchar(64), IN p_session_id bigint, IN p_join_code varchar(8))
LANGUAGE plpgsql
AS $$
BEGIN
    RAISE EXCEPTION 'Manual review required: convert SQL Server procedure uspInsertSession to validated PL/pgSQL before production use.';
EXCEPTION WHEN OTHERS THEN
    RAISE;
END;
$$;

-- Procedure: public.usp_insert_session_member
-- [MANUAL REVIEW REQUIRED: Uses SCOPE_IDENTITY and needs RETURNING/lastval review.]
-- [MANUAL REVIEW REQUIRED: Uses explicit SQL Server transaction control inside the procedure body.]
-- [MANUAL REVIEW REQUIRED: Uses SQL Server exception syntax that must be rewritten for PL/pgSQL.]
CREATE OR REPLACE PROCEDURE public.usp_insert_session_member(IN p_session_id bigint, IN p_user_id bigint, IN p_slot_index smallint, IN p_slot_name varchar(64), IN p_is_host boolean, IN p_created_by varchar(128), IN p_ip_address varchar(64), INOUT p_result refcursor DEFAULT 'usp_insert_session_member_result')
LANGUAGE plpgsql
AS $$
BEGIN
    RAISE EXCEPTION 'Manual review required: convert SQL Server procedure uspInsertSessionMember and return rows through refcursor p_result before enabling PostgreSQL clients.';
EXCEPTION WHEN OTHERS THEN
    RAISE;
END;
$$;

-- Procedure: public.usp_insert_turn_state
-- [MANUAL REVIEW REQUIRED: Uses SCOPE_IDENTITY and needs RETURNING/lastval review.]
-- [MANUAL REVIEW REQUIRED: Uses SQL Server exception syntax that must be rewritten for PL/pgSQL.]
CREATE OR REPLACE PROCEDURE public.usp_insert_turn_state(IN p_session_id bigint, IN p_turn_index integer, IN p_total_turns integer, IN p_active_member_id bigint, IN p_active_slot_index smallint, IN p_utterance_id bigint, IN p_max_re_reads integer, IN p_created_by varchar(128), IN p_ip_address varchar(64), INOUT p_result refcursor DEFAULT 'usp_insert_turn_state_result')
LANGUAGE plpgsql
AS $$
BEGIN
    RAISE EXCEPTION 'Manual review required: convert SQL Server procedure uspInsertTurnState and return rows through refcursor p_result before enabling PostgreSQL clients.';
EXCEPTION WHEN OTHERS THEN
    RAISE;
END;
$$;

-- Procedure: public.usp_insert_user
-- [MANUAL REVIEW REQUIRED: Uses SCOPE_IDENTITY and needs RETURNING/lastval review.]
-- [MANUAL REVIEW REQUIRED: Uses explicit SQL Server transaction control inside the procedure body.]
-- [MANUAL REVIEW REQUIRED: Uses SQL Server exception syntax that must be rewritten for PL/pgSQL.]
CREATE OR REPLACE PROCEDURE public.usp_insert_user(IN p_full_name varchar(128), IN p_mobile_number varchar(16), IN p_email varchar(128), IN p_age_group varchar(32), IN p_preferred_hint_language varchar(32), IN p_avatar_url varchar(256), IN p_group_code varchar(32), IN p_role varchar(16), IN p_created_by varchar(128), IN p_ip_address varchar(64), INOUT p_result refcursor DEFAULT 'usp_insert_user_result')
LANGUAGE plpgsql
AS $$
BEGIN
    RAISE EXCEPTION 'Manual review required: convert SQL Server procedure uspInsertUser and return rows through refcursor p_result before enabling PostgreSQL clients.';
EXCEPTION WHEN OTHERS THEN
    RAISE;
END;
$$;

-- Procedure: public.usp_insert_user_badge
-- [MANUAL REVIEW REQUIRED: Uses SQL Server exception syntax that must be rewritten for PL/pgSQL.]
CREATE OR REPLACE PROCEDURE public.usp_insert_user_badge(IN p_user_id bigint, IN p_badge_code varchar(64), IN p_badge_name varchar(128), IN p_created_by varchar(128), IN p_ip_address varchar(64))
LANGUAGE plpgsql
AS $$
BEGIN
    RAISE EXCEPTION 'Manual review required: convert SQL Server procedure uspInsertUserBadge to validated PL/pgSQL before production use.';
EXCEPTION WHEN OTHERS THEN
    RAISE;
END;
$$;

-- Procedure: public.usp_insert_utterance
-- [MANUAL REVIEW REQUIRED: Uses explicit SQL Server transaction control inside the procedure body.]
-- [MANUAL REVIEW REQUIRED: Uses SQL Server exception syntax that must be rewritten for PL/pgSQL.]
CREATE OR REPLACE PROCEDURE public.usp_insert_utterance(IN p_script_id bigint, IN p_sequence_id integer, IN p_speaker_label varchar(64), IN p_english_text varchar(512), IN p_hint_text varchar(512), IN p_grammar_tag varchar(64), IN p_context_tag varchar(64), IN p_focus_word varchar(64), IN p_pronunciation_note varchar(256), IN p_created_by varchar(128), IN p_ip_address varchar(64))
LANGUAGE plpgsql
AS $$
BEGIN
    RAISE EXCEPTION 'Manual review required: convert SQL Server procedure uspInsertUtterance to validated PL/pgSQL before production use.';
EXCEPTION WHEN OTHERS THEN
    RAISE;
END;
$$;

-- Procedure: public.usp_insert_voice_analysis
-- [MANUAL REVIEW REQUIRED: Uses SCOPE_IDENTITY and needs RETURNING/lastval review.]
-- [MANUAL REVIEW REQUIRED: Uses SQL Server exception syntax that must be rewritten for PL/pgSQL.]
CREATE OR REPLACE PROCEDURE public.usp_insert_voice_analysis(IN p_session_id bigint, IN p_user_id bigint, IN p_turn_index integer, IN p_utterance_id bigint, IN p_transcribed_text varchar(512), IN p_expected_text varchar(512), IN p_fluency_score numeric(5,2), IN p_confidence_score numeric(5,2), IN p_speaking_speed_wpm integer, IN p_pause_count integer, IN p_hesitation_words varchar(256), IN p_repeated_words varchar(256), IN p_grammar_errors_json varchar(512), IN p_pronunciation_json varchar(512), IN p_overall_score numeric(5,2), IN p_created_by varchar(128), IN p_ip_address varchar(64), INOUT p_result refcursor DEFAULT 'usp_insert_voice_analysis_result')
LANGUAGE plpgsql
AS $$
BEGIN
    RAISE EXCEPTION 'Manual review required: convert SQL Server procedure uspInsertVoiceAnalysis and return rows through refcursor p_result before enabling PostgreSQL clients.';
EXCEPTION WHEN OTHERS THEN
    RAISE;
END;
$$;

-- Procedure: public.usp_revoke_refresh_token
-- [MANUAL REVIEW REQUIRED: Uses explicit SQL Server transaction control inside the procedure body.]
-- [MANUAL REVIEW REQUIRED: Uses SQL Server exception syntax that must be rewritten for PL/pgSQL.]
CREATE OR REPLACE PROCEDURE public.usp_revoke_refresh_token(IN p_token varchar(512), IN p_updated_by varchar(128), IN p_ip_address varchar(64))
LANGUAGE plpgsql
AS $$
BEGIN
    RAISE EXCEPTION 'Manual review required: convert SQL Server procedure uspRevokeRefreshToken to validated PL/pgSQL before production use.';
EXCEPTION WHEN OTHERS THEN
    RAISE;
END;
$$;

-- Procedure: public.usp_soft_delete_script_by_script_id
-- [MANUAL REVIEW REQUIRED: Uses explicit SQL Server transaction control inside the procedure body.]
-- [MANUAL REVIEW REQUIRED: Uses SQL Server exception syntax that must be rewritten for PL/pgSQL.]
CREATE OR REPLACE PROCEDURE public.usp_soft_delete_script_by_script_id(IN p_script_id bigint, IN p_deleted_by varchar(128), IN p_ip_address varchar(64))
LANGUAGE plpgsql
AS $$
BEGIN
    RAISE EXCEPTION 'Manual review required: convert SQL Server procedure uspSoftDeleteScriptByScriptId to validated PL/pgSQL before production use.';
EXCEPTION WHEN OTHERS THEN
    RAISE;
END;
$$;

-- Procedure: public.usp_soft_delete_user
-- [MANUAL REVIEW REQUIRED: Uses explicit SQL Server transaction control inside the procedure body.]
-- [MANUAL REVIEW REQUIRED: Uses SQL Server exception syntax that must be rewritten for PL/pgSQL.]
CREATE OR REPLACE PROCEDURE public.usp_soft_delete_user(IN p_user_id bigint, IN p_deleted_by varchar(128), IN p_ip_address varchar(64))
LANGUAGE plpgsql
AS $$
BEGIN
    RAISE EXCEPTION 'Manual review required: convert SQL Server procedure uspSoftDeleteUser to validated PL/pgSQL before production use.';
EXCEPTION WHEN OTHERS THEN
    RAISE;
END;
$$;

-- Procedure: public.usp_update_repractice_session_status
-- [MANUAL REVIEW REQUIRED: Uses SQL Server exception syntax that must be rewritten for PL/pgSQL.]
CREATE OR REPLACE PROCEDURE public.usp_update_repractice_session_status(IN p_repractice_session_id bigint, IN p_status varchar(16), IN p_improvement_percent numeric(5,2), IN p_updated_by varchar(128), IN p_ip_address varchar(64))
LANGUAGE plpgsql
AS $$
BEGIN
    RAISE EXCEPTION 'Manual review required: convert SQL Server procedure uspUpdateRepracticeSessionStatus to validated PL/pgSQL before production use.';
EXCEPTION WHEN OTHERS THEN
    RAISE;
END;
$$;

-- Procedure: public.usp_update_repractice_utterance_attempt
-- [MANUAL REVIEW REQUIRED: Uses SQL Server exception syntax that must be rewritten for PL/pgSQL.]
CREATE OR REPLACE PROCEDURE public.usp_update_repractice_utterance_attempt(IN p_repractice_utterance_id bigint, IN p_score numeric(5,2), IN p_updated_by varchar(128), IN p_ip_address varchar(64))
LANGUAGE plpgsql
AS $$
BEGIN
    RAISE EXCEPTION 'Manual review required: convert SQL Server procedure uspUpdateRepracticeUtteranceAttempt to validated PL/pgSQL before production use.';
EXCEPTION WHEN OTHERS THEN
    RAISE;
END;
$$;

-- Procedure: public.usp_update_script_active_status_by_script_id
-- [MANUAL REVIEW REQUIRED: Uses explicit SQL Server transaction control inside the procedure body.]
-- [MANUAL REVIEW REQUIRED: Uses SQL Server exception syntax that must be rewritten for PL/pgSQL.]
CREATE OR REPLACE PROCEDURE public.usp_update_script_active_status_by_script_id(IN p_script_id bigint, IN p_is_active boolean, IN p_updated_by varchar(128), IN p_ip_address varchar(64))
LANGUAGE plpgsql
AS $$
BEGIN
    RAISE EXCEPTION 'Manual review required: convert SQL Server procedure uspUpdateScriptActiveStatusByScriptId to validated PL/pgSQL before production use.';
EXCEPTION WHEN OTHERS THEN
    RAISE;
END;
$$;

-- Procedure: public.usp_update_script_utterance_count
-- [MANUAL REVIEW REQUIRED: Uses explicit SQL Server transaction control inside the procedure body.]
-- [MANUAL REVIEW REQUIRED: Uses SQL Server exception syntax that must be rewritten for PL/pgSQL.]
CREATE OR REPLACE PROCEDURE public.usp_update_script_utterance_count(IN p_script_id bigint, IN p_updated_by varchar(128), IN p_ip_address varchar(64))
LANGUAGE plpgsql
AS $$
BEGIN
    RAISE EXCEPTION 'Manual review required: convert SQL Server procedure uspUpdateScriptUtteranceCount to validated PL/pgSQL before production use.';
EXCEPTION WHEN OTHERS THEN
    RAISE;
END;
$$;

-- Procedure: public.usp_update_session_member_left
-- [MANUAL REVIEW REQUIRED: Uses explicit SQL Server transaction control inside the procedure body.]
-- [MANUAL REVIEW REQUIRED: Uses SQL Server exception syntax that must be rewritten for PL/pgSQL.]
CREATE OR REPLACE PROCEDURE public.usp_update_session_member_left(IN p_session_id bigint, IN p_user_id bigint, IN p_updated_by varchar(128), IN p_ip_address varchar(64))
LANGUAGE plpgsql
AS $$
BEGIN
    RAISE EXCEPTION 'Manual review required: convert SQL Server procedure uspUpdateSessionMemberLeft to validated PL/pgSQL before production use.';
EXCEPTION WHEN OTHERS THEN
    RAISE;
END;
$$;

-- Procedure: public.usp_update_session_member_ready_status
-- [MANUAL REVIEW REQUIRED: Uses SQL Server exception syntax that must be rewritten for PL/pgSQL.]
CREATE OR REPLACE PROCEDURE public.usp_update_session_member_ready_status(IN p_session_id bigint, IN p_user_id bigint, IN p_is_ready boolean, IN p_updated_by varchar(128), IN p_ip_address varchar(64))
LANGUAGE plpgsql
AS $$
BEGIN
    RAISE EXCEPTION 'Manual review required: convert SQL Server procedure uspUpdateSessionMemberReadyStatus to validated PL/pgSQL before production use.';
EXCEPTION WHEN OTHERS THEN
    RAISE;
END;
$$;

-- Procedure: public.usp_update_session_status
-- [MANUAL REVIEW REQUIRED: Uses SQL Server exception syntax that must be rewritten for PL/pgSQL.]
-- [MANUAL REVIEW REQUIRED: Uses SQL Server date/time conversion functions that need PostgreSQL rewrites.]
CREATE OR REPLACE PROCEDURE public.usp_update_session_status(IN p_session_id bigint, IN p_status varchar(16), IN p_updated_by varchar(128), IN p_ip_address varchar(64))
LANGUAGE plpgsql
AS $$
BEGIN
    RAISE EXCEPTION 'Manual review required: convert SQL Server procedure uspUpdateSessionStatus to validated PL/pgSQL before production use.';
EXCEPTION WHEN OTHERS THEN
    RAISE;
END;
$$;

-- Procedure: public.usp_update_turn_status_by_turn_state_id
-- [MANUAL REVIEW REQUIRED: Uses SQL Server exception syntax that must be rewritten for PL/pgSQL.]
CREATE OR REPLACE PROCEDURE public.usp_update_turn_status_by_turn_state_id(IN p_turn_state_id bigint, IN p_turn_status varchar(16), IN p_updated_by varchar(128), IN p_ip_address varchar(64))
LANGUAGE plpgsql
AS $$
BEGIN
    RAISE EXCEPTION 'Manual review required: convert SQL Server procedure uspUpdateTurnStatusByTurnStateId to validated PL/pgSQL before production use.';
EXCEPTION WHEN OTHERS THEN
    RAISE;
END;
$$;

-- Procedure: public.usp_update_user_active_status_by_user_id
-- [MANUAL REVIEW REQUIRED: Uses explicit SQL Server transaction control inside the procedure body.]
-- [MANUAL REVIEW REQUIRED: Uses SQL Server exception syntax that must be rewritten for PL/pgSQL.]
CREATE OR REPLACE PROCEDURE public.usp_update_user_active_status_by_user_id(IN p_user_id bigint, IN p_is_active boolean, IN p_updated_by varchar(128), IN p_ip_address varchar(64))
LANGUAGE plpgsql
AS $$
BEGIN
    RAISE EXCEPTION 'Manual review required: convert SQL Server procedure uspUpdateUserActiveStatusByUserId to validated PL/pgSQL before production use.';
EXCEPTION WHEN OTHERS THEN
    RAISE;
END;
$$;

-- Procedure: public.usp_update_user_last_login
-- [MANUAL REVIEW REQUIRED: Uses explicit SQL Server transaction control inside the procedure body.]
-- [MANUAL REVIEW REQUIRED: Uses SQL Server exception syntax that must be rewritten for PL/pgSQL.]
CREATE OR REPLACE PROCEDURE public.usp_update_user_last_login(IN p_user_id bigint, IN p_updated_by varchar(128), IN p_ip_address varchar(64))
LANGUAGE plpgsql
AS $$
BEGIN
    RAISE EXCEPTION 'Manual review required: convert SQL Server procedure uspUpdateUserLastLogin to validated PL/pgSQL before production use.';
EXCEPTION WHEN OTHERS THEN
    RAISE;
END;
$$;

-- Procedure: public.usp_update_user_profile
-- [MANUAL REVIEW REQUIRED: Uses SQL Server exception syntax that must be rewritten for PL/pgSQL.]
CREATE OR REPLACE PROCEDURE public.usp_update_user_profile(IN p_user_id bigint, IN p_full_name varchar(128), IN p_email varchar(128), IN p_age_group varchar(32), IN p_preferred_hint_language varchar(32), IN p_avatar_url varchar(256), IN p_updated_by varchar(128), IN p_ip_address varchar(64))
LANGUAGE plpgsql
AS $$
BEGIN
    RAISE EXCEPTION 'Manual review required: convert SQL Server procedure uspUpdateUserProfile to validated PL/pgSQL before production use.';
EXCEPTION WHEN OTHERS THEN
    RAISE;
END;
$$;

-- Procedure: public.usp_upsert_user_streak
-- [MANUAL REVIEW REQUIRED: Uses explicit SQL Server transaction control inside the procedure body.]
-- [MANUAL REVIEW REQUIRED: Uses SQL Server exception syntax that must be rewritten for PL/pgSQL.]
-- [MANUAL REVIEW REQUIRED: Uses SQL Server date/time conversion functions that need PostgreSQL rewrites.]
CREATE OR REPLACE PROCEDURE public.usp_upsert_user_streak(IN p_user_id bigint, IN p_practice_minutes integer, IN p_updated_by varchar(128), IN p_ip_address varchar(64))
LANGUAGE plpgsql
AS $$
BEGIN
    RAISE EXCEPTION 'Manual review required: convert SQL Server procedure uspUpsertUserStreak to validated PL/pgSQL before production use.';
EXCEPTION WHEN OTHERS THEN
    RAISE;
END;
$$;

-- Procedure: public.usp_validate_join_code
-- [MANUAL REVIEW REQUIRED: Uses TOP and needs LIMIT/FETCH rewrites.]
CREATE OR REPLACE PROCEDURE public.usp_validate_join_code(IN p_join_code varchar(8), IN p_is_valid boolean, IN p_session_id bigint, IN p_session_name varchar(128), IN p_status varchar(16), IN p_current_member_count integer)
LANGUAGE plpgsql
AS $$
BEGIN
    RAISE EXCEPTION 'Manual review required: convert SQL Server procedure uspValidateJoinCode to validated PL/pgSQL before production use.';
EXCEPTION WHEN OTHERS THEN
    RAISE;
END;
$$;

-- Procedure: public.usp_verify_otp
-- [MANUAL REVIEW REQUIRED: Uses TOP and needs LIMIT/FETCH rewrites.]
-- [MANUAL REVIEW REQUIRED: Uses explicit SQL Server transaction control inside the procedure body.]
-- [MANUAL REVIEW REQUIRED: Uses SQL Server exception syntax that must be rewritten for PL/pgSQL.]
-- [MANUAL REVIEW REQUIRED: References the removed tblOtpVerification flow and must be redesigned before PostgreSQL deployment.]
CREATE OR REPLACE PROCEDURE public.usp_verify_otp(IN p_mobile_number varchar(16), IN p_otp_code varchar(8), IN p_updated_by varchar(128), IN p_ip_address varchar(64), IN p_is_valid boolean, IN p_user_id bigint, INOUT p_result refcursor DEFAULT 'usp_verify_otp_result')
LANGUAGE plpgsql
AS $$
BEGIN
    RAISE EXCEPTION 'Manual review required: convert SQL Server procedure uspVerifyOtp and return rows through refcursor p_result before enabling PostgreSQL clients.';
EXCEPTION WHEN OTHERS THEN
    RAISE;
END;
$$;

COMMIT;
