-- Phase 10: Add R2 storage key columns
-- Run this script against the SQL Server database.

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.tblScript') AND name = 'ExcelStorageKey')
BEGIN
    ALTER TABLE dbo.tblScript
        ADD ExcelStorageKey NVARCHAR(256) NULL;
END

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.tblVoiceAnalysis') AND name = 'AudioStorageKey')
BEGIN
    ALTER TABLE dbo.tblVoiceAnalysis
        ADD AudioStorageKey NVARCHAR(256) NULL;
END

-- AvatarUrl in tblUser already exists — no change needed.
