-- Seed 41: Question & Answer "Show key words while answering" toggle — SQL Server parity (2026-06-18).
-- Production runs PostgreSQL (Supabase); this keeps the SQL Server schema in sync for local/dev parity
-- and is UNAPPLIED unless SQL Server becomes the active provider. Mirror of
-- Backend/Docs/PostgreSQLMigration/41_add_hard_words_in_answer.sql. Additive + idempotent.
--
-- Adds tblSession.ShowHardWordsInAnswer and extends uspSetSessionAiConfig to persist it
-- (after @ShowHardWords, matching the C# parameter order in SessionRepository).

-- 1) New column.
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.tblSession') AND name = N'ShowHardWordsInAnswer')
BEGIN
    ALTER TABLE dbo.tblSession ADD ShowHardWordsInAnswer BIT NOT NULL CONSTRAINT DF_tblSession_ShowHardWordsInAnswer DEFAULT(0);
END
GO

-- 2) Session AI config — add @ShowHardWordsInAnswer (after @ShowHardWords, matching the C# parameter order).
CREATE OR ALTER PROCEDURE dbo.uspSetSessionAiConfig
    @SessionId             BIGINT,
    @AiEnabled             BIT,
    @AiVoiceGender         NVARCHAR(8),
    @AiVoiceName           NVARCHAR(32),
    @AiSpeechRate          DECIMAL(3,2),
    @AiQuestionDelaySec    INT,
    @ShowHardWords         BIT,
    @ShowHardWordsInAnswer BIT,
    @UpdatedBy             NVARCHAR(128),
    @IPAddress             NVARCHAR(64)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.tblSession
    SET AiEnabled             = @AiEnabled,
        AiVoiceGender         = @AiVoiceGender,
        AiVoiceName           = @AiVoiceName,
        AiSpeechRate          = @AiSpeechRate,
        AiQuestionDelaySec    = @AiQuestionDelaySec,
        ShowHardWords         = @ShowHardWords,
        ShowHardWordsInAnswer = @ShowHardWordsInAnswer,
        UpdatedBy             = @UpdatedBy,
        LastUpdated           = SYSUTCDATETIME()
    WHERE SessionId = @SessionId AND IsDeleted = 0;
END
GO
