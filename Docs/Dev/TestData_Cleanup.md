# GoWithFlow — Test Data Cleanup & Environment Reset Guide

**Version:** 1.0  
**Database:** PostgreSQL (Supabase)  
**Safe for:** Dev resets, staging resets, production maintenance  
**Users:** Never deleted. User accounts survive every cleanup mode.

---

## Safety Rules — Read Before Running Anything

| Rule | Detail |
|---|---|
| **Never delete `tbluser`** | User accounts are permanent. Clean their activity data, not the accounts. |
| **Always run inside a transaction** | Every script below wraps in `BEGIN / COMMIT`. On error, `ROLLBACK` is automatic. |
| **Verify row counts first** | Run the pre-check query in Step 1 before any delete. |
| **Scripts are optional** | Two modes are provided. Mode A deletes all scripts. Mode B deletes only test-tagged ones. |
| **Sequences must be reset after** | After any bulk delete, run the sequence reset in Step 7 to prevent PK gaps causing application confusion. |

---

## Table Inventory — What Exists, What It Holds

### Structural / Config Tables — Do Not Delete Unless Resetting Entirely

| Table | Holds | Delete In |
|---|---|---|
| `tbluser` | User accounts | **NEVER** |
| `tblscriptprompttemplate` | Admin-configured Claude prompt templates | Optional (structural config) |
| `tblcohort` | Cohort/batch definitions | Optional (Step 6) |

### Transactional Tables — Always Deleted in a Full Reset

| Table | Holds | FK Parents |
|---|---|---|
| `tblrepracticeutterance` | Per-utterance repractice attempt detail | `tblrepracticesession`, `tblmistake`, `tblutterance` |
| `tblrepracticesession` | Repractice session generated from a live session | `tbluser`, `tblsession` |
| `tblchallengeattempt` | User attempt on a weekly challenge | `tblweeklychallenge`, `tbluser`, `tblscript` |
| `tblmistake` | Mistakes flagged during live sessions | `tbluser`, `tblsession`, `tblutterance`, `tblscript` |
| `tblvoiceanalysis` | Voice/fluency/grammar analysis per turn | `tblsession`, `tbluser`, `tblutterance` |
| `tbllistenerfeedback` | Real-time listener reactions during a session | `tblsession`, `tbluser` |
| `tblturnstate` | Turn-by-turn state during a live session | `tblsession`, `tbluser`, `tblutterance` |
| `tblsessionmember` | Who was in which session slot | `tblsession`, `tbluser` |
| `tblsessioninvitation` | Push-based role assignment invitations | `tblsession`, `tbluser` |
| `tblaudioarchive` | R2 audio recording references per session | `tblsession`, `tbluser` |
| `tblsessionrecording` | Consolidated session recording rows (added post-migration-31; no enforced FK) | (none enforced) |
| `tbluservocabulary` | Focus words extracted from sessions | `tbluser`, `tblsession` |
| `tblsession` | Session records (lobby → completed) | `tbluser`, `tblscript` |
| `tblweeklychallenge` | Weekly speaking challenge definitions | `tblscript` |
| `tblscriptversion` | Script version history | `tblscript`, `tbluser` |
| `tblutterance` | Lines/utterances belonging to a script | `tblscript` |
| `tblscript` | Scripts (Mode A: all / Mode B: test-tagged only) | `tbluser` |
| `tblrefreshtoken` | JWT refresh tokens | `tbluser` |
| `tbluserbadge` | Badges earned by users | `tbluser` |
| `tbluserstreak` | Daily streak records | `tbluser` |
| `tbladminnote` | Admin notes written about users | `tbluser` |
| `tblusergoal` | Learning goals set by users | `tbluser` |
| `tbldashboardmetric` | Daily dashboard metric snapshots | none |

---

## FK Dependency Tree

Reading direction: child → parent (child must be deleted first)

```
tblrepracticeutterance → tblrepracticesession → tblsession → tblscript → tbluser
                       → tblmistake          → tblsession
                       → tblutterance        → tblscript

tblchallengeattempt    → tblweeklychallenge  → tblscript
                       → tbluser
                       → tblscript

tblmistake             → tblsession, tblutterance, tblscript, tbluser
tblvoiceanalysis       → tblsession, tblutterance, tbluser
tbllistenerfeedback    → tblsession, tbluser
tblturnstate           → tblsession, tblutterance, tbluser
tblsessionmember       → tblsession, tbluser
tblsessioninvitation   → tblsession, tbluser
tblaudioarchive        → tblsession, tbluser
tbluservocabulary      → tblsession, tbluser

tblsession             → tblscript, tbluser

tblweeklychallenge     → tblscript
tblscriptversion       → tblscript, tbluser
tblutterance           → tblscript

tblscript              → tbluser (uploadedbyuserid)

tblrefreshtoken        → tbluser
tbluserbadge           → tbluser
tbluserstreak          → tbluser
tbladminnote           → tbluser (adminuserid + targetuserid)
tblusergoal            → tbluser

tblcohort              ← tbluser.cohortid FK (ON DELETE SET NULL)

tbldashboardmetric     → none
```

---

## Step-by-Step Cleanup Instructions

### Step 1 — Pre-Check: Count Rows Before Deleting

Run this first. Save the output. Confirm numbers are as expected before proceeding.

```sql
SELECT 'tblrepracticeutterance' AS tbl, COUNT(*) FROM tblrepracticeutterance
UNION ALL SELECT 'tblrepracticesession',  COUNT(*) FROM tblrepracticesession
UNION ALL SELECT 'tblchallengeattempt',   COUNT(*) FROM tblchallengeattempt
UNION ALL SELECT 'tblmistake',            COUNT(*) FROM tblmistake
UNION ALL SELECT 'tblvoiceanalysis',      COUNT(*) FROM tblvoiceanalysis
UNION ALL SELECT 'tbllistenerfeedback',   COUNT(*) FROM tbllistenerfeedback
UNION ALL SELECT 'tblturnstate',          COUNT(*) FROM tblturnstate
UNION ALL SELECT 'tblsessionmember',      COUNT(*) FROM tblsessionmember
UNION ALL SELECT 'tblsessioninvitation',  COUNT(*) FROM tblsessioninvitation
UNION ALL SELECT 'tblaudioarchive',       COUNT(*) FROM tblaudioarchive
UNION ALL SELECT 'tbluservocabulary',     COUNT(*) FROM tbluservocabulary
UNION ALL SELECT 'tblsession',            COUNT(*) FROM tblsession
UNION ALL SELECT 'tblweeklychallenge',    COUNT(*) FROM tblweeklychallenge
UNION ALL SELECT 'tblscriptversion',      COUNT(*) FROM tblscriptversion
UNION ALL SELECT 'tblutterance',          COUNT(*) FROM tblutterance
UNION ALL SELECT 'tblscript',             COUNT(*) FROM tblscript
UNION ALL SELECT 'tblrefreshtoken',       COUNT(*) FROM tblrefreshtoken
UNION ALL SELECT 'tbluserbadge',          COUNT(*) FROM tbluserbadge
UNION ALL SELECT 'tbluserstreak',         COUNT(*) FROM tbluserstreak
UNION ALL SELECT 'tbladminnote',          COUNT(*) FROM tbladminnote
UNION ALL SELECT 'tblusergoal',           COUNT(*) FROM tblusergoal
UNION ALL SELECT 'tbldashboardmetric',    COUNT(*) FROM tbldashboardmetric
UNION ALL SELECT 'tblcohort',             COUNT(*) FROM tblcohort
UNION ALL SELECT 'tbluser (PRESERVED)',   COUNT(*) FROM tbluser
ORDER BY tbl;
```

---

### Step 2 — Choose Your Cleanup Mode

| Mode | What It Does | When to Use |
|---|---|---|
| **Mode A — Full Reset** | Deletes ALL scripts and all transactional data. Users preserved. | Dev env reset, staging reset before new data load |
| **Mode B — Selective** | Deletes only scripts tagged `'TEST'` and all transactional data referencing them. Other scripts survive. | Partial cleanup; keep production scripts, remove test-only scripts |

---

### Step 3A — Full Reset Script (Mode A)

Deletes all transactional data and all scripts. Users are untouched.

```sql
BEGIN;

-- ── Layer 1: Deepest leaves ──────────────────────────────────────────────────
DELETE FROM public.tblrepracticeutterance;

-- ── Layer 2: Repractice + challenge attempts ─────────────────────────────────
DELETE FROM public.tblrepracticesession;
DELETE FROM public.tblchallengeattempt;

-- ── Layer 3: All session children ────────────────────────────────────────────
DELETE FROM public.tblmistake;
DELETE FROM public.tblvoiceanalysis;
DELETE FROM public.tbllistenerfeedback;
DELETE FROM public.tblturnstate;
DELETE FROM public.tblsessionmember;
DELETE FROM public.tblsessioninvitation;
DELETE FROM public.tblaudioarchive;
DELETE FROM public.tblsessionrecording;   -- added 2026-06-18 (post-migration-31 table; no FK, holds consolidated session recordings)
DELETE FROM public.tbluservocabulary;

-- ── Layer 4: Sessions ─────────────────────────────────────────────────────────
DELETE FROM public.tblsession;

-- ── Layer 5: Script children ─────────────────────────────────────────────────
DELETE FROM public.tblweeklychallenge;
DELETE FROM public.tblscriptversion;
DELETE FROM public.tblutterance;

-- ── Layer 6: Scripts (ALL) ───────────────────────────────────────────────────
DELETE FROM public.tblscript;

-- ── Layer 7: User activity data ──────────────────────────────────────────────
DELETE FROM public.tblrefreshtoken;
DELETE FROM public.tbluserbadge;
DELETE FROM public.tbluserstreak;
DELETE FROM public.tbladminnote;
DELETE FROM public.tblusergoal;

-- ── Layer 8: Metrics and auth records ────────────────────────────────────────
DELETE FROM public.tbldashboardmetric;

-- ── Layer 9: User counter reset (users preserved, counters zeroed) ───────────
UPDATE public.tbluser
SET    dailystreakcount    = 0,
       totalsessionsplayed = 0,
       lastlogindate       = NULL;

COMMIT;
```

---

### Step 3B — Selective Reset Script (Mode B)

Deletes only test-tagged scripts and all data referencing them. Untouched scripts survive.

```sql
BEGIN;

-- Identify test script IDs
CREATE TEMP TABLE _test_script_ids AS
SELECT scriptid FROM public.tblscript
WHERE  LOWER(tag) = 'test'
   OR  LOWER(scripttitle) LIKE '%test%'
   OR  LOWER(comments) LIKE '%test%';

-- ── Layer 1 ──────────────────────────────────────────────────────────────────
DELETE FROM public.tblrepracticeutterance
WHERE  repracticesessionid IN (
    SELECT rps.repracticesessionid
    FROM   public.tblrepracticesession rps
    JOIN   public.tblsession           s   ON s.sessionid = rps.sourcesessionid
    WHERE  s.scriptid IN (SELECT scriptid FROM _test_script_ids)
);

-- ── Layer 2 ──────────────────────────────────────────────────────────────────
DELETE FROM public.tblrepracticesession
WHERE  sourcesessionid IN (
    SELECT sessionid FROM public.tblsession
    WHERE  scriptid IN (SELECT scriptid FROM _test_script_ids)
);

DELETE FROM public.tblchallengeattempt
WHERE  scriptid IN (SELECT scriptid FROM _test_script_ids);

-- ── Layer 3 ──────────────────────────────────────────────────────────────────
DELETE FROM public.tblmistake
WHERE  sessionid IN (
    SELECT sessionid FROM public.tblsession
    WHERE  scriptid IN (SELECT scriptid FROM _test_script_ids)
);

DELETE FROM public.tblvoiceanalysis
WHERE  sessionid IN (
    SELECT sessionid FROM public.tblsession
    WHERE  scriptid IN (SELECT scriptid FROM _test_script_ids)
);

DELETE FROM public.tbllistenerfeedback
WHERE  sessionid IN (
    SELECT sessionid FROM public.tblsession
    WHERE  scriptid IN (SELECT scriptid FROM _test_script_ids)
);

DELETE FROM public.tblturnstate
WHERE  sessionid IN (
    SELECT sessionid FROM public.tblsession
    WHERE  scriptid IN (SELECT scriptid FROM _test_script_ids)
);

DELETE FROM public.tblsessionmember
WHERE  sessionid IN (
    SELECT sessionid FROM public.tblsession
    WHERE  scriptid IN (SELECT scriptid FROM _test_script_ids)
);

DELETE FROM public.tblsessioninvitation
WHERE  sessionid IN (
    SELECT sessionid FROM public.tblsession
    WHERE  scriptid IN (SELECT scriptid FROM _test_script_ids)
);

DELETE FROM public.tblaudioarchive
WHERE  sessionid IN (
    SELECT sessionid FROM public.tblsession
    WHERE  scriptid IN (SELECT scriptid FROM _test_script_ids)
);

DELETE FROM public.tbluservocabulary
WHERE  sourcesessionid IN (
    SELECT sessionid FROM public.tblsession
    WHERE  scriptid IN (SELECT scriptid FROM _test_script_ids)
);

-- ── Layer 4 ──────────────────────────────────────────────────────────────────
DELETE FROM public.tblsession
WHERE  scriptid IN (SELECT scriptid FROM _test_script_ids);

-- ── Layer 5 ──────────────────────────────────────────────────────────────────
DELETE FROM public.tblweeklychallenge
WHERE  scriptid IN (SELECT scriptid FROM _test_script_ids);

DELETE FROM public.tblscriptversion
WHERE  scriptid IN (SELECT scriptid FROM _test_script_ids);

DELETE FROM public.tblutterance
WHERE  scriptid IN (SELECT scriptid FROM _test_script_ids);

-- ── Layer 6 ──────────────────────────────────────────────────────────────────
DELETE FROM public.tblscript
WHERE  scriptid IN (SELECT scriptid FROM _test_script_ids);

DROP TABLE _test_script_ids;

COMMIT;
```

> **Tagging convention for future test scripts:** When uploading test scripts during development, set `tag = 'TEST'` on the `tblscript` row. Mode B will pick them up automatically.

---

### Step 4 — Optional: Delete Cohorts

Only run this if test cohorts were created. User `cohortid` FK is `ON DELETE SET NULL` — it self-clears.

```sql
BEGIN;

-- Unlink users from cohorts first (safe even if FK cascade handles it)
UPDATE public.tbluser
SET    cohortid = NULL
WHERE  cohortid IS NOT NULL;

-- Delete all cohorts
DELETE FROM public.tblcohort;

COMMIT;
```

---

### Step 5 — Optional: Delete Script Prompt Templates

Only run if test prompt templates were created. These are admin-configured and usually kept.

```sql
BEGIN;
DELETE FROM public.tblscriptprompttemplate;
COMMIT;
```

---

### Step 6 — Post-Delete Verification

Re-run Step 1 and confirm all targeted tables are now empty. Key assertions:

```sql
-- All of these should return 0 after a full reset
SELECT COUNT(*) FROM public.tblsession;        -- expect: 0
SELECT COUNT(*) FROM public.tblmistake;        -- expect: 0
SELECT COUNT(*) FROM public.tblvoiceanalysis;  -- expect: 0
SELECT COUNT(*) FROM public.tblscript;         -- expect: 0 (Mode A) or N remaining (Mode B)

-- This must always be > 0
SELECT COUNT(*) FROM public.tbluser;           -- expect: unchanged from pre-check
```

---

### Step 7 — Sequence Reset

Run after any bulk delete to keep identity sequences consistent. Prevents gaps and application-side confusion on first insert after reset.

> **Use this generic block** (replaced the old hard-coded per-column list on 2026-06-18 — that list had a
> wrong PK name for `tblsessioninvitation` and missed new tables). It resets EVERY sequence in `public`
> to its own table's `MAX(pk)+1`, so emptied tables go to 1 and **`tbluser` correctly stays above its
> real max** (users preserved). No column names to maintain; covers any future table automatically.

```sql
DO $$
DECLARE r RECORD; mx BIGINT;
BEGIN
  FOR r IN
    SELECT s.relname AS seqname, t.relname AS tabname, a.attname AS colname
    FROM pg_class s
    JOIN pg_depend d ON d.objid = s.oid AND d.deptype IN ('a','i')
    JOIN pg_class t ON t.oid = d.refobjid
    JOIN pg_attribute a ON a.attrelid = t.oid AND a.attnum = d.refobjsubid
    WHERE s.relkind = 'S' AND t.relnamespace = 'public'::regnamespace
  LOOP
    EXECUTE format('SELECT COALESCE(MAX(%I),0)+1 FROM public.%I', r.colname, r.tabname) INTO mx;
    EXECUTE format('SELECT setval(%L, %s, false)', 'public.'||r.seqname, mx);
  END LOOP;
END $$;
```

---

## Full Run Checklist

```
[ ] 1. Run Step 1 (pre-check) and save row counts
[ ] 2. Decide: Mode A (full) or Mode B (selective)
[ ] 3. Run Step 3A or 3B
[ ] 4. Run Step 4 if cohorts should be cleared
[ ] 5. Run Step 5 if prompt templates should be cleared
[ ] 6. Run Step 6 (verify empty tables, confirm tbluser untouched)
[ ] 7. Run Step 7 (sequence reset)
[ ] 8. Restart API server (clears in-memory caches and SignalR state)
```

---

## Production Maintenance Notes

**Partial production cleanup** (remove only expired/stale data, not full reset):

```sql
-- Revoke expired refresh tokens older than 30 days
DELETE FROM public.tblrefreshtoken
WHERE  expiresat < NOW() - INTERVAL '30 days';

-- Remove dashboard metric snapshots older than 90 days
DELETE FROM public.tbldashboardmetric
WHERE  metricdate < CURRENT_DATE - INTERVAL '90 days';
```

**R2 storage note:** `tblaudioarchive` holds references to Cloudflare R2 objects via `audiostoragkey`. Deleting rows from `tblaudioarchive` does not delete the R2 objects. If performing a full reset, purge the R2 bucket separately through the Cloudflare dashboard or R2 API before or after running this script.

---

## Revision Log

| Date | Change | Author |
|---|---|---|
| 2026-06-03 | Initial version — full schema coverage through migration 31 | Project AI Engineer |
| 2026-06-18 | **Mode A executed on production (Supabase)** — wiped all transactional data + scripts, users preserved (7). Added `tblsessionrecording` to Mode A + inventory; replaced Step 7 with the generic sequence-reset block (old list had a wrong `tblsessioninvitation` PK name). | Project AI Engineer |
| 2026-06-18 | **Deep wipe (Steps 4 + 5) executed** — also cleared `tblcohort` (1→0, users unlinked via `cohortid = NULL`) and `tblscriptprompttemplate` (7→0). Only `tbluser` (7) remains. NOTE: deleting prompt templates removed the seeded category prompts incl. Q&A — re-run `PostgreSQLMigration/19_*` + `37_*` to restore the DB-sourced admin prompts (upload still works via the client `buildClaudePrompt` fallback meanwhile). | Project AI Engineer |
