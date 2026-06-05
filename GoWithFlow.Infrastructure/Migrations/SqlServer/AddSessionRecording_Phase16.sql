-- Phase 16: Consolidated Session Recording (SQL Server parity)
-- See Backend/Docs/Dev/SessionRecordingArchitecture.md. Production runs PostgreSQL (Supabase);
-- this file keeps the SQL Server schema in sync for local/dev parity.

-- 1) Persist the host "Record Session" flag on the session itself.
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID(N'dbo.tblSession') AND name = N'RecordingEnabled')
BEGIN
    ALTER TABLE dbo.tblSession ADD RecordingEnabled BIT NOT NULL CONSTRAINT DF_tblSession_RecordingEnabled DEFAULT(0);
END
GO

-- 2) One consolidated recording per session.
IF OBJECT_ID(N'dbo.tblSessionRecording', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.tblSessionRecording (
        RecordingId      BIGINT        IDENTITY(1,1) PRIMARY KEY,
        SessionId        BIGINT        NOT NULL,
        StorageKey       NVARCHAR(512) NULL,
        Status           NVARCHAR(20)  NOT NULL CONSTRAINT DF_tblSessionRecording_Status DEFAULT(N'PENDING_MERGE'),
        Format           NVARCHAR(8)   NULL,
        DurationSecs     INT           NULL,
        SizeBytes        BIGINT        NULL,
        SegmentCount     INT           NULL,
        ParticipantsJson NVARCHAR(MAX) NULL,
        FailureReason    NVARCHAR(512) NULL,
        AttemptCount     INT           NOT NULL CONSTRAINT DF_tblSessionRecording_AttemptCount DEFAULT(0),
        CreatedAt        DATETIME2     NOT NULL CONSTRAINT DF_tblSessionRecording_CreatedAt DEFAULT(SYSUTCDATETIME()),
        CompletedAt      DATETIME2     NULL,
        ExpiresAt        DATETIME2     NULL,
        IsDeleted        BIT           NOT NULL CONSTRAINT DF_tblSessionRecording_IsDeleted DEFAULT(0)
    );

    CREATE UNIQUE INDEX UX_tblSessionRecording_SessionId
        ON dbo.tblSessionRecording (SessionId) WHERE IsDeleted = 0;

    CREATE INDEX IX_tblSessionRecording_Status
        ON dbo.tblSessionRecording (Status) WHERE IsDeleted = 0;
END
GO
