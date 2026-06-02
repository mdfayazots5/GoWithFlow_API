using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GoWithFlow.Infrastructure.Data.Migrations
{
    public partial class AddSessionInvitation_Phase16 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add ScheduledAt column to tblSession
            migrationBuilder.Sql(@"
IF NOT EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'tblSession' AND COLUMN_NAME = 'ScheduledAt'
)
BEGIN
    ALTER TABLE dbo.tblSession ADD ScheduledAt DATETIME2 NULL;
END
");

            // tblSessionInvitation — push-based role assignment and invitation tracking
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'tblSessionInvitation')
BEGIN
    CREATE TABLE dbo.tblSessionInvitation (
        InvitationId    BIGINT          IDENTITY(1,1)   NOT NULL,
        SessionId       BIGINT          NOT NULL,
        UserId          BIGINT          NOT NULL,
        SlotIndex       TINYINT         NOT NULL,
        SlotName        NVARCHAR(64)    NOT NULL,
        Status          NVARCHAR(16)    NOT NULL        DEFAULT 'PENDING',
        SentAt          DATETIME2       NOT NULL        DEFAULT GETDATE(),
        RespondedAt     DATETIME2       NULL,
        ExpiresAt       DATETIME2       NULL,
        SortOrder       INT             NOT NULL        DEFAULT 0,
        Tag             NVARCHAR(64)    NULL,
        Comments        NVARCHAR(256)   NULL,
        IPAddress       NVARCHAR(64)    NOT NULL        DEFAULT '127.0.0.1',
        CreatedBy       NVARCHAR(128)   NOT NULL        DEFAULT 'Admin',
        DateCreated     DATETIME2       NOT NULL        DEFAULT GETDATE(),
        UpdatedBy       NVARCHAR(128)   NULL,
        LastUpdated     DATETIME2       NULL,
        DeletedBy       NVARCHAR(128)   NULL,
        DateDeleted     DATETIME2       NULL,
        IsDeleted       BIT             NOT NULL        DEFAULT 0,
        CONSTRAINT PK_tblSessionInvitation_InvitationId PRIMARY KEY (InvitationId),
        CONSTRAINT FK_tblSessionInvitation_SessionId_tblSession_SessionId
            FOREIGN KEY (SessionId) REFERENCES dbo.tblSession(SessionId),
        CONSTRAINT FK_tblSessionInvitation_UserId_tblUser_UserId
            FOREIGN KEY (UserId) REFERENCES dbo.tblUser(UserId)
    );

    CREATE INDEX IDX_tblSessionInvitation_SessionId ON dbo.tblSessionInvitation (SessionId);
    CREATE INDEX IDX_tblSessionInvitation_UserId    ON dbo.tblSessionInvitation (UserId);
END
");

            // uspInsertSessionInvitation — idempotent: updates existing PENDING row or inserts new
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.objects WHERE type = 'P' AND name = 'uspInsertSessionInvitation')
    DROP PROCEDURE dbo.uspInsertSessionInvitation;
");
            migrationBuilder.Sql(@"
CREATE PROCEDURE dbo.uspInsertSessionInvitation
    @SessionId   BIGINT,
    @UserId      BIGINT,
    @SlotIndex   TINYINT,
    @SlotName    NVARCHAR(64),
    @ExpiresAt   DATETIME2,
    @CreatedBy   NVARCHAR(128),
    @IPAddress   NVARCHAR(64)
AS
BEGIN
    SET NOCOUNT ON;

    -- If a PENDING invitation already exists for this session+user, reset it
    IF EXISTS (
        SELECT 1 FROM dbo.tblSessionInvitation
        WHERE  SessionId = @SessionId AND UserId = @UserId AND Status = 'PENDING' AND IsDeleted = 0
    )
    BEGIN
        UPDATE dbo.tblSessionInvitation
        SET    SlotIndex   = @SlotIndex,
               SlotName    = @SlotName,
               ExpiresAt   = @ExpiresAt,
               SentAt      = GETDATE(),
               RespondedAt = NULL,
               UpdatedBy   = @CreatedBy,
               LastUpdated = GETDATE()
        WHERE  SessionId = @SessionId AND UserId = @UserId AND Status = 'PENDING' AND IsDeleted = 0;

        SELECT InvitationId FROM dbo.tblSessionInvitation
        WHERE  SessionId = @SessionId AND UserId = @UserId AND Status = 'PENDING' AND IsDeleted = 0;
    END
    ELSE
    BEGIN
        INSERT INTO dbo.tblSessionInvitation
            (SessionId, UserId, SlotIndex, SlotName, Status, SentAt, ExpiresAt, CreatedBy, IPAddress)
        VALUES
            (@SessionId, @UserId, @SlotIndex, @SlotName, 'PENDING', GETDATE(), @ExpiresAt, @CreatedBy, @IPAddress);

        SELECT SCOPE_IDENTITY() AS InvitationId;
    END
END
");

            // uspUpdateInvitationStatus — invitee responds (ACCEPTED / DECLINED)
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.objects WHERE type = 'P' AND name = 'uspUpdateInvitationStatus')
    DROP PROCEDURE dbo.uspUpdateInvitationStatus;
");
            migrationBuilder.Sql(@"
CREATE PROCEDURE dbo.uspUpdateInvitationStatus
    @InvitationId BIGINT,
    @UserId       BIGINT,
    @Status       NVARCHAR(16),
    @UpdatedBy    NVARCHAR(128),
    @IPAddress    NVARCHAR(64)
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.tblSessionInvitation
    SET    Status      = @Status,
           RespondedAt = GETDATE(),
           UpdatedBy   = @UpdatedBy,
           LastUpdated = GETDATE(),
           IPAddress   = @IPAddress
    WHERE  InvitationId = @InvitationId
      AND  UserId       = @UserId
      AND  IsDeleted    = 0;
END
");

            // uspCancelSessionInvitation — host rescinds a single invitation
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.objects WHERE type = 'P' AND name = 'uspCancelSessionInvitation')
    DROP PROCEDURE dbo.uspCancelSessionInvitation;
");
            migrationBuilder.Sql(@"
CREATE PROCEDURE dbo.uspCancelSessionInvitation
    @InvitationId BIGINT,
    @SessionId    BIGINT,
    @UpdatedBy    NVARCHAR(128)
AS
BEGIN
    SET NOCOUNT ON;
    UPDATE dbo.tblSessionInvitation
    SET    Status      = 'CANCELLED',
           UpdatedBy   = @UpdatedBy,
           LastUpdated = GETDATE()
    WHERE  InvitationId = @InvitationId
      AND  SessionId    = @SessionId
      AND  IsDeleted    = 0;
END
");

            // uspGetInvitationsBySessionId — host waiting room view
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.objects WHERE type = 'P' AND name = 'uspGetInvitationsBySessionId')
    DROP PROCEDURE dbo.uspGetInvitationsBySessionId;
");
            migrationBuilder.Sql(@"
CREATE PROCEDURE dbo.uspGetInvitationsBySessionId
    @SessionId BIGINT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
        inv.InvitationId,
        inv.SessionId,
        inv.UserId,
        inv.SlotIndex,
        inv.SlotName,
        inv.Status,
        inv.SentAt,
        inv.RespondedAt,
        inv.ExpiresAt,
        u.FullName,
        u.AvatarUrl
    FROM   dbo.tblSessionInvitation inv
    INNER  JOIN dbo.tblUser u ON u.UserId = inv.UserId AND u.IsDeleted = 0
    WHERE  inv.SessionId = @SessionId
      AND  inv.IsDeleted = 0
    ORDER  BY inv.SlotIndex, inv.InvitationId;
END
");

            // uspGetPendingInvitationsByUserId — user dashboard inbox
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.objects WHERE type = 'P' AND name = 'uspGetPendingInvitationsByUserId')
    DROP PROCEDURE dbo.uspGetPendingInvitationsByUserId;
");
            migrationBuilder.Sql(@"
CREATE PROCEDURE dbo.uspGetPendingInvitationsByUserId
    @UserId BIGINT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT
        inv.InvitationId,
        inv.SessionId,
        inv.SlotIndex,
        inv.SlotName,
        inv.Status,
        inv.SentAt,
        inv.ExpiresAt,
        ses.SessionName,
        ses.SessionMode,
        ses.SessionDuration,
        ses.ScheduledAt,
        host.FullName   AS HostName,
        host.AvatarUrl  AS HostAvatarUrl
    FROM   dbo.tblSessionInvitation inv
    INNER  JOIN dbo.tblSession ses  ON ses.SessionId   = inv.SessionId  AND ses.IsDeleted  = 0
    INNER  JOIN dbo.tblUser    host ON host.UserId      = ses.HostUserId AND host.IsDeleted = 0
    WHERE  inv.UserId    = @UserId
      AND  inv.IsDeleted = 0
      AND  inv.Status    = 'PENDING'
      AND  (inv.ExpiresAt IS NULL OR inv.ExpiresAt > GETDATE())
    ORDER  BY inv.SentAt DESC;
END
");

            // uspSearchUsersByName — host user-search for role assignment (by name, max 20 results)
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.objects WHERE type = 'P' AND name = 'uspSearchUsersByName')
    DROP PROCEDURE dbo.uspSearchUsersByName;
");
            migrationBuilder.Sql(@"
CREATE PROCEDURE dbo.uspSearchUsersByName
    @SearchTerm NVARCHAR(128),
    @ExcludeUserId BIGINT
AS
BEGIN
    SET NOCOUNT ON;
    SELECT TOP 20
        UserId,
        FullName,
        AvatarUrl
    FROM   dbo.tblUser
    WHERE  IsDeleted = 0
      AND  IsActive  = 1
      AND  UserId   <> @ExcludeUserId
      AND  FullName  LIKE '%' + @SearchTerm + '%'
    ORDER  BY FullName ASC;
END
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.objects WHERE type = 'P' AND name = 'uspSearchUsersByName')             DROP PROCEDURE dbo.uspSearchUsersByName;
IF EXISTS (SELECT 1 FROM sys.objects WHERE type = 'P' AND name = 'uspGetPendingInvitationsByUserId') DROP PROCEDURE dbo.uspGetPendingInvitationsByUserId;
IF EXISTS (SELECT 1 FROM sys.objects WHERE type = 'P' AND name = 'uspGetInvitationsBySessionId')    DROP PROCEDURE dbo.uspGetInvitationsBySessionId;
IF EXISTS (SELECT 1 FROM sys.objects WHERE type = 'P' AND name = 'uspCancelSessionInvitation')      DROP PROCEDURE dbo.uspCancelSessionInvitation;
IF EXISTS (SELECT 1 FROM sys.objects WHERE type = 'P' AND name = 'uspUpdateInvitationStatus')       DROP PROCEDURE dbo.uspUpdateInvitationStatus;
IF EXISTS (SELECT 1 FROM sys.objects WHERE type = 'P' AND name = 'uspInsertSessionInvitation')      DROP PROCEDURE dbo.uspInsertSessionInvitation;
");

            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'tblSessionInvitation')
    DROP TABLE dbo.tblSessionInvitation;
");

            migrationBuilder.Sql(@"
IF EXISTS (
    SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS
    WHERE TABLE_NAME = 'tblSession' AND COLUMN_NAME = 'ScheduledAt'
)
BEGIN
    ALTER TABLE dbo.tblSession DROP COLUMN ScheduledAt;
END
");
        }
    }
}
