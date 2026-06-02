-- Migration 27: Fix storage key column name typos
-- Root cause: AddR2StorageKeys_Phase10.sql created both columns with
-- "storagkey" (missing trailing 'e' from "storage") instead of "storagekey".
-- EF ApplyProviderConventions lowercases C# names fully:
--   ExcelStorageKey → excelstoragekey  (15 chars: excel + storage + key)
--   AudioStorageKey → audiostoragekey  (15 chars: audio + storage + key)
-- The DB had excelstoragkey / audiostoragkey (14 chars each — missing 'e'),
-- causing PG error 42703 on any EF query that selects tblscript or tblvoiceanalysis.
-- Safe to re-run: existence check guards each RENAME.

DO $$
BEGIN
    -- Fix tblscript: excelstoragkey (14) → excelstoragekey (15)
    IF EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_schema = 'public'
          AND table_name   = 'tblscript'
          AND column_name  = 'excelstoragkey'
    ) THEN
        ALTER TABLE public.tblscript
            RENAME COLUMN excelstoragkey TO excelstoragekey;
    END IF;

    -- Fix tblvoiceanalysis: audiostoragkey (14) → audiostoragekey (15)
    IF EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_schema = 'public'
          AND table_name   = 'tblvoiceanalysis'
          AND column_name  = 'audiostoragkey'
    ) THEN
        ALTER TABLE public.tblvoiceanalysis
            RENAME COLUMN audiostoragkey TO audiostoragekey;
    END IF;
END $$;
