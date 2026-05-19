-- ============================================
-- File: 01_extensions.sql
-- Description: Enable PostgreSQL extensions required by the migrated schema and Supabase integration layer.
-- Run order: 1 of 10
-- Dependencies: NONE
-- ============================================
-- Tables migrated: 17
-- Views migrated: 0
-- Functions migrated: 0
-- Stored Procedures migrated: 79
-- Triggers migrated: 0
-- Indexes migrated: 33
-- Known incompatibilities: NONE
-- Manual review required: NONE

BEGIN;

CREATE EXTENSION IF NOT EXISTS pgcrypto;

COMMIT;
