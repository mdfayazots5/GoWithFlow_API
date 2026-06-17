-- Phase 17 (Phase 1 of build plan): AI Voice Participant — stored procedures (SQL Server parity)
-- Run AFTER AddAIVoiceParticipant_Phase17.sql (needs the IsAi + Ai* columns).
-- Production runs PostgreSQL; this keeps the SQL Server schema in sync for local/dev parity.
--
-- ADDITIVE procedures — the core uspInsertSession / uspInsertSessionMember are left untouched so
-- human create/join flows carry zero regression risk.

-- Inserts one AI-held member (IsAi=1, IsReady=1, IsHost=0). NO duplicate-user guard, so the single
-- reserved AI system user can hold multiple slots in one session. Slot-occupied guard still applies.
CREATE OR ALTER PROCEDURE dbo.uspInsertAiSessionMember
    @SessionId BIGINT,
    @UserId    BIGINT,
    @SlotIndex TINYINT,
    @SlotName  NVARCHAR(64),
    @CreatedBy NVARCHAR(128),
    @IPAddress NVARCHAR(64)
AS
BEGIN
    SET NOCOUNT ON;

    IF EXISTS (
        SELECT 1 FROM dbo.tblSessionMember
        WHERE SessionId = @SessionId AND SlotIndex = @SlotIndex
          AND IsDeleted = 0 AND IsActive = 1)
    BEGIN
        THROW 50000, 'The requested session slot is already occupied.', 1;
    END

    INSERT INTO dbo.tblSessionMember
        (SessionId, UserId, SlotIndex, SlotName, IsReady, IsHost, IsAi, JoinedAt, IsActive, CreatedBy, IPAddress)
    VALUES
        (@SessionId, @UserId, @SlotIndex, @SlotName, 1, 0, 1, SYSUTCDATETIME(), 1, @CreatedBy, @IPAddress);
END
GO

-- Persists the per-session AI config on tblSession. Called only when AI is enabled, so the three
-- settings are always non-null (enforced by CreateSessionRequestValidator).
CREATE OR ALTER PROCEDURE dbo.uspSetSessionAiConfig
    @SessionId          BIGINT,
    @AiEnabled          BIT,
    @AiVoiceGender      NVARCHAR(8),
    @AiVoiceName        NVARCHAR(32),
    @AiSpeechRate       DECIMAL(3,2),
    @AiQuestionDelaySec INT,
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
        UpdatedBy          = @UpdatedBy,
        LastUpdated        = SYSUTCDATETIME()
    WHERE SessionId = @SessionId AND IsDeleted = 0;
END
GO
