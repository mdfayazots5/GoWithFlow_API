-- Phase 17: AI Voice Participant — schema & seed (SQL Server parity)
-- See Backend/Docs/Dev/AIVoiceParticipantArchitecture.md (Phase 0).
-- Production runs PostgreSQL (Supabase); this file keeps the SQL Server schema in sync for
-- local/dev parity. No behavior change — columns + reserved system user only.
--
-- The AI participant is modeled as a NON-HUMAN session member holding a slot. To satisfy the
-- existing tblSessionMember.UserId FK without making it nullable (which would ripple through
-- many SPs), AI member rows point at ONE reserved system user seeded below. Because UserId is
-- IDENTITY (env-specific), application code resolves this user by its sentinel MobileNumber
-- ('AI_PARTICIPANT'), never by a hard-coded id.

-- 1) Flag AI-held slots on the session member.
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.tblSessionMember') AND name = N'IsAi')
BEGIN
    ALTER TABLE dbo.tblSessionMember ADD IsAi BIT NOT NULL CONSTRAINT DF_tblSessionMember_IsAi DEFAULT(0);
END
GO

-- 2) Per-session AI configuration (all nullable; populated only when AI is enabled at creation).
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.tblSession') AND name = N'AiEnabled')
BEGIN
    ALTER TABLE dbo.tblSession ADD AiEnabled BIT NULL;
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.tblSession') AND name = N'AiVoiceGender')
BEGIN
    ALTER TABLE dbo.tblSession ADD AiVoiceGender NVARCHAR(8) NULL;   -- 'Male' | 'Female'
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.tblSession') AND name = N'AiVoiceName')
BEGIN
    ALTER TABLE dbo.tblSession ADD AiVoiceName NVARCHAR(32) NULL;    -- named Indian voice id (aarav|ananya|vikram|meera|rohan|priya); added 2026-06-18
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.tblSession') AND name = N'AiSpeechRate')
BEGIN
    ALTER TABLE dbo.tblSession ADD AiSpeechRate DECIMAL(3,2) NULL;   -- TTS rate multiplier, e.g. 0.75 / 1.00 / 1.25
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.tblSession') AND name = N'AiQuestionDelaySec')
BEGIN
    ALTER TABLE dbo.tblSession ADD AiQuestionDelaySec INT NULL;      -- pause before AI reads next line
END
GO

-- 3) Reserved system user that backs every AI member's FK. Idempotent on the sentinel mobile number.
IF NOT EXISTS (SELECT 1 FROM dbo.tblUser WHERE MobileNumber = N'AI_PARTICIPANT')
BEGIN
    INSERT INTO dbo.tblUser
        (FullName, MobileNumber, Email, PasswordHash, AgeGroup, PreferredHintLanguage, AvatarUrl,
         GroupCode, Role, DailyStreakCount, TotalSessionsPlayed, LastLoginDate, IsActive, RegistrationDate,
         SortOrder, IPAddress, CreatedBy, DateCreated, IsDeleted)
    VALUES
        (N'AI Voice Participant', N'AI_PARTICIPANT', NULL, NULL, N'All', N'None', NULL,
         NULL, N'SYSTEM', 0, 0, NULL, 1, SYSUTCDATETIME(),
         0, N'127.0.0.1', N'System', SYSUTCDATETIME(), 0);
END
GO
