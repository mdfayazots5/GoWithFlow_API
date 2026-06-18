-- Seed 39: Question & Answer "Hard Words" practice aid — SQL Server parity (2026-06-18).
-- Production runs PostgreSQL (Supabase); this keeps the SQL Server schema in sync for local/dev parity
-- and is UNAPPLIED unless SQL Server becomes the active provider. Mirror of
-- Backend/Docs/PostgreSQLMigration/39_add_hard_words.sql. Additive + idempotent.
--
-- Adds tblUtterance.HardWords + tblSession.ShowHardWords, extends the UtteranceTVP table type and the
-- utterance insert procs to carry HardWords, and uspSetSessionAiConfig to persist ShowHardWords.
-- The TVP + proc bodies below are reconstructed to match the C# TVP column order
-- (ScriptRepository.CreateUtteranceTableParameter) and the PostgreSQL function semantics.

-- 1) New columns.
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.tblUtterance') AND name = N'HardWords')
BEGIN
    ALTER TABLE dbo.tblUtterance ADD HardWords NVARCHAR(1024) NULL;   -- Q&A Column I: 'word:meaning | word:meaning'
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.tblSession') AND name = N'ShowHardWords')
BEGIN
    ALTER TABLE dbo.tblSession ADD ShowHardWords BIT NOT NULL CONSTRAINT DF_tblSession_ShowHardWords DEFAULT(0);
END
GO

-- 2) Recreate the UtteranceTVP table type with the added HardWords column.
--    The proc that consumes it must be dropped first (a type cannot be altered while referenced).
IF OBJECT_ID(N'dbo.uspBulkInsertUtterance', N'P') IS NOT NULL
    DROP PROCEDURE dbo.uspBulkInsertUtterance;
GO

IF EXISTS (SELECT 1 FROM sys.types WHERE name = N'UtteranceTVP' AND is_table_type = 1)
    DROP TYPE dbo.UtteranceTVP;
GO

CREATE TYPE dbo.UtteranceTVP AS TABLE
(
    SequenceId        INT            NOT NULL,
    SpeakerLabel      NVARCHAR(64)   NOT NULL,
    EnglishText       NVARCHAR(512)  NOT NULL,
    HintText          NVARCHAR(512)  NULL,
    GrammarTag        NVARCHAR(64)   NULL,
    ContextTag        NVARCHAR(64)   NULL,
    FocusWord         NVARCHAR(64)   NULL,
    PronunciationNote NVARCHAR(256)  NULL,
    HardWords         NVARCHAR(1024) NULL
);
GO

CREATE PROCEDURE dbo.uspBulkInsertUtterance
    @ScriptId    BIGINT,
    @Utterances  dbo.UtteranceTVP READONLY,
    @CreatedBy   NVARCHAR(128),
    @IPAddress   NVARCHAR(64)
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.tblUtterance
        (ScriptId, SequenceId, SpeakerLabel, EnglishText, HintText,
         GrammarTag, ContextTag, FocusWord, PronunciationNote, HardWords, CreatedBy, IPAddress)
    SELECT
        @ScriptId, SequenceId, SpeakerLabel, EnglishText, HintText,
        GrammarTag, ContextTag, FocusWord, PronunciationNote, HardWords, @CreatedBy, @IPAddress
    FROM @Utterances;
END
GO

-- 3) Single insert — add @HardWords (after @PronunciationNote, matching the C# parameter order).
CREATE OR ALTER PROCEDURE dbo.uspInsertUtterance
    @ScriptId          BIGINT,
    @SequenceId        INT,
    @SpeakerLabel      NVARCHAR(64),
    @EnglishText       NVARCHAR(512),
    @HintText          NVARCHAR(512)  = NULL,
    @GrammarTag        NVARCHAR(64)   = NULL,
    @ContextTag        NVARCHAR(64)   = NULL,
    @FocusWord         NVARCHAR(64)   = NULL,
    @PronunciationNote NVARCHAR(256)  = NULL,
    @HardWords         NVARCHAR(1024) = NULL,
    @CreatedBy         NVARCHAR(128)  = NULL,
    @IPAddress         NVARCHAR(64)   = NULL
AS
BEGIN
    SET NOCOUNT ON;

    INSERT INTO dbo.tblUtterance
        (ScriptId, SequenceId, SpeakerLabel, EnglishText, HintText,
         GrammarTag, ContextTag, FocusWord, PronunciationNote, HardWords, CreatedBy, IPAddress)
    VALUES
        (@ScriptId, @SequenceId, @SpeakerLabel, @EnglishText, @HintText,
         @GrammarTag, @ContextTag, @FocusWord, @PronunciationNote, @HardWords, @CreatedBy, @IPAddress);
END
GO

-- 4) Session AI config — add @ShowHardWords (after @AiQuestionDelaySec, matching the C# parameter order).
CREATE OR ALTER PROCEDURE dbo.uspSetSessionAiConfig
    @SessionId          BIGINT,
    @AiEnabled          BIT,
    @AiVoiceGender      NVARCHAR(8),
    @AiVoiceName        NVARCHAR(32),
    @AiSpeechRate       DECIMAL(3,2),
    @AiQuestionDelaySec INT,
    @ShowHardWords      BIT,
    @UpdatedBy          NVARCHAR(128),
    @IPAddress          NVARCHAR(64)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE dbo.tblSession
    SET AiEnabled          = @AiEnabled,
        AiVoiceGender      = @AiVoiceGender,
        AiVoiceName        = @AiVoiceName,
        AiSpeechRate       = @AiSpeechRate,
        AiQuestionDelaySec = @AiQuestionDelaySec,
        ShowHardWords      = @ShowHardWords,
        UpdatedBy          = @UpdatedBy,
        LastUpdated        = SYSUTCDATETIME()
    WHERE SessionId = @SessionId AND IsDeleted = 0;
END
GO
