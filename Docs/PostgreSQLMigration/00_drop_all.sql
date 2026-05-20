-- ============================================
-- File: 00_drop_all.sql
-- Description: Drop ALL objects created by the 10 migration scripts.
--              Run this to fully reset the schema before re-running
--              01_extensions.sql through 10_rls_policies.sql.
-- Run order: BEFORE 01 (reset only)
-- ============================================
-- WARNING: This is destructive and irreversible.
--          All tables, data, functions, indexes, sequences,
--          and RLS policies will be permanently removed.
-- ============================================

BEGIN;

SET search_path TO public;

-- ============================================
-- STEP 1: DROP ALL FUNCTIONS / STORED PROCEDURES
-- Dynamically drops every function in the public schema
-- (covers all 79 SPs + helper functions from RLS)
-- ============================================
DO $$
DECLARE
    r RECORD;
BEGIN
    FOR r IN
        SELECT p.oid::regprocedure::TEXT AS func_sig
        FROM   pg_proc p
        JOIN   pg_namespace n ON n.oid = p.pronamespace
        WHERE  n.nspname = 'public'
    LOOP
        EXECUTE 'DROP FUNCTION IF EXISTS ' || r.func_sig || ' CASCADE';
    END LOOP;
END;
$$;

-- ============================================
-- STEP 2: DROP ALL TABLES
-- Reverse FK dependency order.
-- CASCADE removes dependent indexes, sequences,
-- constraints, and RLS policies automatically.
-- ============================================

DROP TABLE IF EXISTS tblrepracticeutterance  CASCADE;
DROP TABLE IF EXISTS tblrepracticesession    CASCADE;
DROP TABLE IF EXISTS tblvoiceanalysis        CASCADE;
DROP TABLE IF EXISTS tblmistake              CASCADE;
DROP TABLE IF EXISTS tblturnstate            CASCADE;
DROP TABLE IF EXISTS tbllistenerfeedback     CASCADE;
DROP TABLE IF EXISTS tblsessionmember        CASCADE;
DROP TABLE IF EXISTS tbladminnote            CASCADE;
DROP TABLE IF EXISTS tblscriptversion        CASCADE;
DROP TABLE IF EXISTS tblsession              CASCADE;
DROP TABLE IF EXISTS tblutterance            CASCADE;
DROP TABLE IF EXISTS tbldashboardmetric      CASCADE;
DROP TABLE IF EXISTS tbluserstreak           CASCADE;
DROP TABLE IF EXISTS tbluserbadge            CASCADE;
DROP TABLE IF EXISTS tblrefreshtoken         CASCADE;
DROP TABLE IF EXISTS tblotpverification      CASCADE;
DROP TABLE IF EXISTS tblscript               CASCADE;
DROP TABLE IF EXISTS tbluser                 CASCADE;

-- ============================================
-- STEP 3: DROP EXTENSIONS
-- Comment these out if other databases on the
-- same server depend on these extensions.
-- ============================================

DROP EXTENSION IF EXISTS pg_trgm    CASCADE;
DROP EXTENSION IF EXISTS citext      CASCADE;
DROP EXTENSION IF EXISTS "uuid-ossp" CASCADE;
DROP EXTENSION IF EXISTS pgcrypto    CASCADE;

COMMIT;
