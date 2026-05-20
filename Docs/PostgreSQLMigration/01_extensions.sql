-- ============================================
-- File: 01_extensions.sql
-- Description: Enable required PostgreSQL extensions for GoWithFlow migration
-- Run order: 1 of 10
-- Dependencies: None
-- ============================================
-- Tables migrated: N/A
-- Views migrated: N/A
-- Functions migrated: N/A
-- Stored Procedures migrated: N/A
-- Triggers migrated: N/A
-- Indexes migrated: N/A
-- Known incompatibilities: NONE
-- Manual review required: NONE
-- ============================================

BEGIN;

-- Required for gen_random_uuid() used as default for UUID columns
CREATE EXTENSION IF NOT EXISTS "pgcrypto";

-- Alternative UUID extension (uuid-ossp provides uuid_generate_v4())
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- Trigram indexes for ILIKE full-text search performance on VARCHAR columns
CREATE EXTENSION IF NOT EXISTS "pg_trgm";

-- Case-insensitive text type (available if needed for email columns)
CREATE EXTENSION IF NOT EXISTS "citext";

-- Set default search path to public schema
SET search_path TO public;

COMMIT;
