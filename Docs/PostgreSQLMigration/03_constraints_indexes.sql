-- ============================================
-- File: 03_constraints_indexes.sql
-- Description: Add foreign keys and secondary indexes converted from the live SQL Server catalog.
-- Run order: 3 of 10
-- Dependencies: 01_extensions.sql, 02_schema.sql
-- ============================================
-- Tables migrated: 17
-- Views migrated: 0
-- Functions migrated: 0
-- Stored Procedures migrated: 79
-- Triggers migrated: 0
-- Indexes migrated: 33
-- Known incompatibilities: Filtered indexes translated to partial indexes; verify predicate selectivity under PostgreSQL planner.
-- Manual review required: Composite unique indexes from SQL Server filtered indexes need workload validation after migration.


BEGIN;

ALTER TABLE public.tbl_admin_note ADD CONSTRAINT fk_tbl_admin_note_admin_user_id_tbl_user_user_id FOREIGN KEY (admin_user_id) REFERENCES public.tbl_user (user_id);
ALTER TABLE public.tbl_admin_note ADD CONSTRAINT fk_tbl_admin_note_target_user_id_tbl_user_user_id FOREIGN KEY (target_user_id) REFERENCES public.tbl_user (user_id);
ALTER TABLE public.tbl_listener_feedback ADD CONSTRAINT fk_tbl_listener_feedback_from_user_id_tbl_user_user_id FOREIGN KEY (from_user_id) REFERENCES public.tbl_user (user_id);
ALTER TABLE public.tbl_listener_feedback ADD CONSTRAINT fk_tbl_listener_feedback_session_id_tbl_session_session_id FOREIGN KEY (session_id) REFERENCES public.tbl_session (session_id);
ALTER TABLE public.tbl_listener_feedback ADD CONSTRAINT fk_tbl_listener_feedback_target_user_id_tbl_user_user_id FOREIGN KEY (target_user_id) REFERENCES public.tbl_user (user_id);
ALTER TABLE public.tbl_mistake ADD CONSTRAINT fk_tbl_mistake_script_id_tbl_script_script_id FOREIGN KEY (script_id) REFERENCES public.tbl_script (script_id);
ALTER TABLE public.tbl_mistake ADD CONSTRAINT fk_tbl_mistake_session_id_tbl_session_session_id FOREIGN KEY (session_id) REFERENCES public.tbl_session (session_id);
ALTER TABLE public.tbl_mistake ADD CONSTRAINT fk_tbl_mistake_user_id_tbl_user_user_id FOREIGN KEY (user_id) REFERENCES public.tbl_user (user_id);
ALTER TABLE public.tbl_mistake ADD CONSTRAINT fk_tbl_mistake_utterance_id_tbl_utterance_utterance_id FOREIGN KEY (utterance_id) REFERENCES public.tbl_utterance (utterance_id);
ALTER TABLE public.tbl_refresh_token ADD CONSTRAINT fk_tbl_refresh_token_user_id_tbl_user_user_id FOREIGN KEY (user_id) REFERENCES public.tbl_user (user_id);
ALTER TABLE public.tbl_repractice_session ADD CONSTRAINT fk_tbl_repractice_session_source_session_id_tbl_session_session_id FOREIGN KEY (source_session_id) REFERENCES public.tbl_session (session_id);
ALTER TABLE public.tbl_repractice_session ADD CONSTRAINT fk_tbl_repractice_session_user_id_tbl_user_user_id FOREIGN KEY (user_id) REFERENCES public.tbl_user (user_id);
ALTER TABLE public.tbl_repractice_utterance ADD CONSTRAINT fk_tbl_repractice_utterance_mistake_id_tbl_mistake_mistake_id FOREIGN KEY (mistake_id) REFERENCES public.tbl_mistake (mistake_id);
ALTER TABLE public.tbl_repractice_utterance ADD CONSTRAINT fk_tbl_repractice_utterance_original_utterance_id_tbl_utterance_utterance_id FOREIGN KEY (original_utterance_id) REFERENCES public.tbl_utterance (utterance_id);
ALTER TABLE public.tbl_repractice_utterance ADD CONSTRAINT fk_tbl_repractice_utterance_repractice_session_id_tbl_repractice_session_repractice_session_id FOREIGN KEY (repractice_session_id) REFERENCES public.tbl_repractice_session (repractice_session_id);
ALTER TABLE public.tbl_script ADD CONSTRAINT fk_tbl_script_uploaded_by_user_id_tbl_user_user_id FOREIGN KEY (uploaded_by_user_id) REFERENCES public.tbl_user (user_id);
ALTER TABLE public.tbl_script_version ADD CONSTRAINT fk_tbl_script_version_script_id_tbl_script_script_id FOREIGN KEY (script_id) REFERENCES public.tbl_script (script_id);
ALTER TABLE public.tbl_script_version ADD CONSTRAINT fk_tbl_script_version_uploaded_by_user_id_tbl_user_user_id FOREIGN KEY (uploaded_by_user_id) REFERENCES public.tbl_user (user_id);
ALTER TABLE public.tbl_session ADD CONSTRAINT fk_tbl_session_host_user_id_tbl_user_user_id FOREIGN KEY (host_user_id) REFERENCES public.tbl_user (user_id);
ALTER TABLE public.tbl_session ADD CONSTRAINT fk_tbl_session_script_id_tbl_script_script_id FOREIGN KEY (script_id) REFERENCES public.tbl_script (script_id);
ALTER TABLE public.tbl_session_member ADD CONSTRAINT fk_tbl_session_member_session_id_tbl_session_session_id FOREIGN KEY (session_id) REFERENCES public.tbl_session (session_id);
ALTER TABLE public.tbl_session_member ADD CONSTRAINT fk_tbl_session_member_user_id_tbl_user_user_id FOREIGN KEY (user_id) REFERENCES public.tbl_user (user_id);
ALTER TABLE public.tbl_turn_state ADD CONSTRAINT fk_tbl_turn_state_active_member_id_tbl_user_user_id FOREIGN KEY (active_member_id) REFERENCES public.tbl_user (user_id);
ALTER TABLE public.tbl_turn_state ADD CONSTRAINT fk_tbl_turn_state_session_id_tbl_session_session_id FOREIGN KEY (session_id) REFERENCES public.tbl_session (session_id);
ALTER TABLE public.tbl_turn_state ADD CONSTRAINT fk_tbl_turn_state_utterance_id_tbl_utterance_utterance_id FOREIGN KEY (utterance_id) REFERENCES public.tbl_utterance (utterance_id);
ALTER TABLE public.tbl_user_badge ADD CONSTRAINT fk_tbl_user_badge_user_id_tbl_user_user_id FOREIGN KEY (user_id) REFERENCES public.tbl_user (user_id);
ALTER TABLE public.tbl_user_streak ADD CONSTRAINT fk_tbl_user_streak_user_id_tbl_user_user_id FOREIGN KEY (user_id) REFERENCES public.tbl_user (user_id);
ALTER TABLE public.tbl_utterance ADD CONSTRAINT fk_tbl_utterance_script_id_tbl_script_script_id FOREIGN KEY (script_id) REFERENCES public.tbl_script (script_id);
ALTER TABLE public.tbl_voice_analysis ADD CONSTRAINT fk_tbl_voice_analysis_session_id_tbl_session_session_id FOREIGN KEY (session_id) REFERENCES public.tbl_session (session_id);
ALTER TABLE public.tbl_voice_analysis ADD CONSTRAINT fk_tbl_voice_analysis_user_id_tbl_user_user_id FOREIGN KEY (user_id) REFERENCES public.tbl_user (user_id);
ALTER TABLE public.tbl_voice_analysis ADD CONSTRAINT fk_tbl_voice_analysis_utterance_id_tbl_utterance_utterance_id FOREIGN KEY (utterance_id) REFERENCES public.tbl_utterance (utterance_id);

CREATE INDEX IF NOT EXISTS idx_tbl_admin_note_target_user_id ON public.tbl_admin_note (target_user_id);
CREATE INDEX IF NOT EXISTS idx_tbl_listener_feedback_session_id_turn_index ON public.tbl_listener_feedback (session_id, turn_index);
CREATE INDEX IF NOT EXISTS idx_tbl_mistake_grammar_tag ON public.tbl_mistake (grammar_tag);
CREATE INDEX IF NOT EXISTS idx_tbl_mistake_session_id ON public.tbl_mistake (session_id);
CREATE INDEX IF NOT EXISTS idx_tbl_mistake_user_id ON public.tbl_mistake (user_id);
CREATE INDEX IF NOT EXISTS idx_tbl_mistake_user_id_is_resolved ON public.tbl_mistake (user_id, is_resolved);
CREATE INDEX IF NOT EXISTS idx_tbl_mistake_user_id_mistake_type_is_resolved ON public.tbl_mistake (user_id, mistake_type, is_resolved) WHERE (is_deleted = FALSE);
CREATE INDEX IF NOT EXISTS idx_tbl_refresh_token_user_id ON public.tbl_refresh_token (user_id);
CREATE INDEX IF NOT EXISTS idx_tbl_repractice_session_user_id ON public.tbl_repractice_session (user_id);
CREATE INDEX IF NOT EXISTS idx_tbl_repractice_session_user_id_status ON public.tbl_repractice_session (user_id, status) WHERE (is_deleted = FALSE);
CREATE INDEX IF NOT EXISTS idx_tbl_repractice_utterance_repractice_session_id ON public.tbl_repractice_utterance (repractice_session_id);
CREATE INDEX IF NOT EXISTS idx_tbl_script_category ON public.tbl_script (category);
CREATE INDEX IF NOT EXISTS idx_tbl_script_grammar_focus_tag ON public.tbl_script (grammar_focus_tag);
CREATE INDEX IF NOT EXISTS idx_tbl_script_is_active ON public.tbl_script (is_active);
CREATE INDEX IF NOT EXISTS idx_tbl_script_version_script_id ON public.tbl_script_version (script_id);
CREATE INDEX IF NOT EXISTS idx_tbl_session_host_user_id ON public.tbl_session (host_user_id);
CREATE INDEX IF NOT EXISTS idx_tbl_session_join_code ON public.tbl_session (join_code);
CREATE INDEX IF NOT EXISTS idx_tbl_session_status ON public.tbl_session (status);
CREATE INDEX IF NOT EXISTS idx_tbl_session_status_is_deleted ON public.tbl_session (status) WHERE (is_deleted = FALSE);
CREATE UNIQUE INDEX IF NOT EXISTS uk_tbl_session_join_code ON public.tbl_session (join_code) WHERE (is_deleted = FALSE);
CREATE INDEX IF NOT EXISTS idx_tbl_session_member_session_id ON public.tbl_session_member (session_id);
CREATE INDEX IF NOT EXISTS idx_tbl_session_member_session_id_is_active ON public.tbl_session_member (session_id, is_active) WHERE (is_deleted = FALSE);
CREATE INDEX IF NOT EXISTS idx_tbl_session_member_user_id ON public.tbl_session_member (user_id);
CREATE UNIQUE INDEX IF NOT EXISTS uk_tbl_session_member_session_id_slot_index ON public.tbl_session_member (session_id, slot_index) WHERE (is_deleted = FALSE AND is_active = TRUE);
CREATE INDEX IF NOT EXISTS idx_tbl_turn_state_session_id ON public.tbl_turn_state (session_id);
CREATE INDEX IF NOT EXISTS idx_tbl_turn_state_session_id_turn_index ON public.tbl_turn_state (session_id, turn_index);
CREATE INDEX IF NOT EXISTS idx_tbl_user_badge_user_id ON public.tbl_user_badge (user_id);
CREATE INDEX IF NOT EXISTS idx_tbl_user_streak_user_id ON public.tbl_user_streak (user_id);
CREATE INDEX IF NOT EXISTS idx_tbl_utterance_script_id ON public.tbl_utterance (script_id);
CREATE UNIQUE INDEX IF NOT EXISTS uk_tbl_utterance_script_id_sequence_id ON public.tbl_utterance (script_id, sequence_id);
CREATE INDEX IF NOT EXISTS idx_tbl_voice_analysis_session_id ON public.tbl_voice_analysis (session_id);
CREATE INDEX IF NOT EXISTS idx_tbl_voice_analysis_session_id_user_id ON public.tbl_voice_analysis (session_id, user_id) WHERE (is_deleted = FALSE);
CREATE INDEX IF NOT EXISTS idx_tbl_voice_analysis_user_id ON public.tbl_voice_analysis (user_id);

COMMIT;
