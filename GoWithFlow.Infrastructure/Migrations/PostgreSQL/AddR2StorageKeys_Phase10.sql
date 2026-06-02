-- Phase 10: Add R2 storage key columns
-- PostgreSQL (Supabase) — column names are all lowercase per EF convention.
-- ExcelStorageKey (C#) → excelstoragekey (PG)  [excel + storage + key = 15 chars]
-- AudioStorageKey (C#) → audiostoragekey (PG)  [audio + storage + key = 15 chars]
-- NOTE: original file had excelstoragkey / audiostoragkey (14 chars, missing 'e').
-- That typo is corrected here. Existing DBs must run migration 27 to rename the columns.

ALTER TABLE public.tblscript
  ADD COLUMN IF NOT EXISTS excelstoragekey VARCHAR(256) NULL;

ALTER TABLE public.tblvoiceanalysis
  ADD COLUMN IF NOT EXISTS audiostoragekey VARCHAR(256) NULL;

-- AvatarUrl in tbluser already existed (avatarurl column).
-- After Phase 10 it stores an R2 object key (e.g. avatars/42/1718000000000.jpg)
-- instead of a URL path. No structural change required.
