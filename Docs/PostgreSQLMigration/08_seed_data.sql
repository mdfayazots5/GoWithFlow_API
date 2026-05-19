-- ============================================
-- File: 08_seed_data.sql
-- Description: Insert lookup and reference data required after schema creation.
-- Run order: 8 of 10
-- Dependencies: 01_extensions.sql, 02_schema.sql, 03_constraints_indexes.sql, 04_views.sql, 05_functions.sql, 06_stored_procedures.sql, 07_triggers.sql
-- ============================================
-- Tables migrated: 17
-- Views migrated: 0
-- Functions migrated: 0
-- Stored Procedures migrated: 79
-- Triggers migrated: 0
-- Indexes migrated: 33
-- Known incompatibilities: Current live database contains no dedicated lookup/reference tables; application rows such as sessions, scripts, and users are intentionally excluded from seed scripts.
-- Manual review required: NONE

BEGIN;

-- No reference or lookup seed data was detected in the live SQL Server schema.

COMMIT;
