using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GoWithFlow.Infrastructure.Data.Migrations
{
    public partial class AddAudioArchive_Phase15 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // tblAudioArchive — stores per-turn voice clips for opted-in users
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'tblAudioArchive')
BEGIN
    CREATE TABLE dbo.tblAudioArchive (
        ArchiveId       BIGINT          IDENTITY(1,1)   NOT NULL,
        SessionId       BIGINT          NOT NULL,
        UserId          BIGINT          NOT NULL,
        TurnIndex       INT             NOT NULL,
        StorageKey      NVARCHAR(512)   NOT NULL,
        DurationSecs    INT             NOT NULL        DEFAULT 0,
        ExpiresAt       DATETIME2       NOT NULL,
        SortOrder       INT             NOT NULL        DEFAULT 0,
        IPAddress       NVARCHAR(64)    NOT NULL        DEFAULT '127.0.0.1',
        CreatedBy       NVARCHAR(128)   NOT NULL        DEFAULT 'System',
        DateCreated     DATETIME2       NOT NULL        DEFAULT GETDATE(),
        UpdatedBy       NVARCHAR(128)   NULL,
        LastUpdated     DATETIME2       NULL,
        DeletedBy       NVARCHAR(128)   NULL,
        DateDeleted     DATETIME2       NULL,
        IsDeleted       BIT             NOT NULL        DEFAULT 0,
        CONSTRAINT PK_tblAudioArchive_ArchiveId PRIMARY KEY (ArchiveId),
        CONSTRAINT FK_tblAudioArchive_SessionId_tblSession_SessionId
            FOREIGN KEY (SessionId) REFERENCES dbo.tblSession(SessionId),
        CONSTRAINT FK_tblAudioArchive_UserId_tblUser_UserId
            FOREIGN KEY (UserId) REFERENCES dbo.tblUser(UserId)
    );

    CREATE INDEX IDX_tblAudioArchive_SessionId_UserId  ON dbo.tblAudioArchive (SessionId, UserId);
    CREATE INDEX IDX_tblAudioArchive_UserId             ON dbo.tblAudioArchive (UserId);
    CREATE INDEX IDX_tblAudioArchive_ExpiresAt           ON dbo.tblAudioArchive (ExpiresAt);
END
");

            // uspInsertAudioArchive
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.objects WHERE type = 'P' AND name = 'uspInsertAudioArchive')
    DROP PROCEDURE dbo.uspInsertAudioArchive;
");
            migrationBuilder.Sql(@"
CREATE PROCEDURE dbo.uspInsertAudioArchive
    @SessionId    BIGINT,
    @UserId       BIGINT,
    @TurnIndex    INT,
    @StorageKey   NVARCHAR(512),
    @DurationSecs INT,
    @ExpiresAt    DATETIME2,
    @CreatedBy    NVARCHAR(128),
    @IPAddress    NVARCHAR(64)
AS
BEGIN
    SET NOCOUNT ON;
    INSERT INTO dbo.tblAudioArchive (SessionId, UserId, TurnIndex, StorageKey, DurationSecs, ExpiresAt, CreatedBy, IPAddress)
    VALUES (@SessionId, @UserId, @TurnIndex, @StorageKey, @DurationSecs, @ExpiresAt, @CreatedBy, @IPAddress);
    SELECT SCOPE_IDENTITY() AS ArchiveId;
END
");

            // uspGetAudioArchiveBySessionAndUser
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.objects WHERE type = 'P' AND name = 'uspGetAudioArchiveBySessionAndUser')
    DROP PROCEDURE dbo.uspGetAudioArchiveBySessionAndUser;
");
            migrationBuilder.Sql(@"
CREATE PROCEDURE dbo.uspGetAudioArchiveBySessionAndUser
    @SessionId BIGINT,
    @UserId    BIGINT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT ArchiveId, SessionId, UserId, TurnIndex, StorageKey, DurationSecs, ExpiresAt, DateCreated
    FROM   dbo.tblAudioArchive
    WHERE  SessionId = @SessionId
      AND  UserId    = @UserId
      AND  IsDeleted = 0
      AND  ExpiresAt >= GETDATE()
    ORDER  BY TurnIndex ASC;
END
");

            // uspDeleteAudioArchive — soft delete
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.objects WHERE type = 'P' AND name = 'uspDeleteAudioArchive')
    DROP PROCEDURE dbo.uspDeleteAudioArchive;
");
            migrationBuilder.Sql(@"
CREATE PROCEDURE dbo.uspDeleteAudioArchive
    @ArchiveId BIGINT,
    @UserId    BIGINT,
    @DeletedBy NVARCHAR(128)
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.tblAudioArchive
    SET    IsDeleted  = 1,
           DeletedBy  = @DeletedBy,
           DateDeleted = GETDATE()
    WHERE  ArchiveId = @ArchiveId
      AND  UserId    = @UserId
      AND  IsDeleted = 0;
END
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.objects WHERE type = 'P' AND name = 'uspDeleteAudioArchive')         DROP PROCEDURE dbo.uspDeleteAudioArchive;
IF EXISTS (SELECT 1 FROM sys.objects WHERE type = 'P' AND name = 'uspGetAudioArchiveBySessionAndUser') DROP PROCEDURE dbo.uspGetAudioArchiveBySessionAndUser;
IF EXISTS (SELECT 1 FROM sys.objects WHERE type = 'P' AND name = 'uspInsertAudioArchive')          DROP PROCEDURE dbo.uspInsertAudioArchive;
");

            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'tblAudioArchive')
    DROP TABLE dbo.tblAudioArchive;
");
        }
    }
}
