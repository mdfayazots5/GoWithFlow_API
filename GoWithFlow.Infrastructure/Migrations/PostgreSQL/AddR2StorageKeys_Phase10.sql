-- Phase 10: Add R2 storage key columns
-- PostgreSQL (Supabase) — column names are all lowercase per EF convention.
-- ExcelStorageKey (C#) → excelstoragkey (PG)
-- AudioStorageKey (C#) → audiostoragkey (PG)

ALTER TABLE public.tblscript
  ADD COLUMN IF NOT EXISTS excelstoragkey VARCHAR(256) NULL;

ALTER TABLE public.tblvoiceanalysis
  ADD COLUMN IF NOT EXISTS audiostoragkey VARCHAR(256) NULL;

-- AvatarUrl in tbluser already exists (avatarurl column).
-- After Phase 10 it stores an R2 object key (e.g. avatars/42/1718000000000.jpg)
-- instead of a URL path. No structural change required.
