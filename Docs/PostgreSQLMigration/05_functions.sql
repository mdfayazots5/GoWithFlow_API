-- ============================================
-- File: 05_functions.sql
-- Description: Create PostgreSQL functions converted from SQL Server scalar and table-valued functions.
-- Run order: 5 of 10
-- Dependencies: 01_extensions.sql, 02_schema.sql, 03_constraints_indexes.sql, 04_views.sql
-- ============================================
-- Tables migrated: 17
-- Views migrated: 0
-- Functions migrated: 0
-- Stored Procedures migrated: 79
-- Triggers migrated: 0
-- Indexes migrated: 33
-- Known incompatibilities: SQL Server user-defined table type dbo.UtteranceTVP has no direct PostgreSQL equivalent and is consumed as JSONB by the migrated bulk-load procedure.
-- Manual review required: Validate JSON payload contract used in public.usp_bulk_insert_utterance against the original dbo.UtteranceTVP column order.

BEGIN;

-- No user-defined SQL Server scalar or table-valued functions exist in GoWithFlowDB.

COMMIT;
