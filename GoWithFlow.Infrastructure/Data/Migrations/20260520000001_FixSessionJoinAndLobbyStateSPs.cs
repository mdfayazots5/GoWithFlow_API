using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GoWithFlow.Infrastructure.Data.Migrations
{
    public partial class FixSessionJoinAndLobbyStateSPs : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // uspValidateJoinCode
            // Validates a join code for the JoinSession flow.
            // Returns @IsValid=1 only if the session exists, is in LOBBY status,
            // has not expired, and has at least one open slot.
            migrationBuilder.Sql(
                """
                CREATE OR ALTER PROCEDURE dbo.uspValidateJoinCode
                (
                    @JoinCode           NVARCHAR(8),
                    @IsValid            BIT           OUTPUT,
                    @SessionId          BIGINT        OUTPUT,
                    @SessionName        NVARCHAR(128) OUTPUT,
                    @Status             NVARCHAR(16)  OUTPUT,
                    @CurrentMemberCount INT           OUTPUT
                )
                AS
                BEGIN
                    SET NOCOUNT ON;

                    SET @IsValid            = 0;
                    SET @SessionId          = 0;
                    SET @SessionName        = N'';
                    SET @Status             = N'';
                    SET @CurrentMemberCount = 0;

                    DECLARE @MaxMembers TINYINT = 0;
                    DECLARE @TempId     BIGINT  = 0;

                    SELECT TOP (1)
                        @TempId      = ses.SessionId,
                        @SessionName = ses.SessionName,
                        @Status      = ses.Status,
                        @MaxMembers  = ses.MaxMembers
                    FROM dbo.tblSession AS ses
                    WHERE ses.JoinCode   = @JoinCode
                      AND ses.IsDeleted  = 0
                      AND ses.Status     = N'LOBBY'
                      AND (ses.RoomExpiresAt IS NULL OR ses.RoomExpiresAt > GETDATE());

                    IF @TempId = 0
                        RETURN;

                    SELECT @CurrentMemberCount = COUNT(sem.SessionMemberId)
                    FROM dbo.tblSessionMember AS sem
                    WHERE sem.SessionId = @TempId
                      AND sem.IsDeleted = 0
                      AND sem.IsActive  = 1;

                    IF @CurrentMemberCount < @MaxMembers
                    BEGIN
                        SET @IsValid   = 1;
                        SET @SessionId = @TempId;
                    END
                END
                """);

            // uspGetSessionBySessionId
            // Returns (1) session header, (2) active members with slot names.
            migrationBuilder.Sql(
                """
                CREATE OR ALTER PROCEDURE dbo.uspGetSessionBySessionId
                (
                    @SessionId BIGINT
                )
                AS
                BEGIN
                    SET NOCOUNT ON;

                    SELECT
                        ses.SessionId,
                        ses.SessionName,
                        ses.JoinCode,
                        ses.SessionMode,
                        scr.ScriptTitle,
                        ses.MaxMembers,
                        ses.SessionDuration
                    FROM dbo.tblSession AS ses
                    INNER JOIN dbo.tblScript AS scr
                        ON scr.ScriptId  = ses.ScriptId
                       AND scr.IsDeleted = 0
                    WHERE ses.SessionId = @SessionId
                      AND ses.IsDeleted = 0;

                    SELECT
                        usr.UserId,
                        usr.FullName,
                        usr.AvatarUrl,
                        sem.SlotIndex,
                        sem.SlotName,
                        CAST(sem.IsReady AS BIT) AS IsReady,
                        CAST(sem.IsHost  AS BIT) AS IsHost
                    FROM dbo.tblSessionMember AS sem
                    INNER JOIN dbo.tblUser AS usr
                        ON usr.UserId    = sem.UserId
                       AND usr.IsDeleted = 0
                    WHERE sem.SessionId = @SessionId
                      AND sem.IsDeleted = 0
                      AND sem.IsActive  = 1
                    ORDER BY sem.SlotIndex ASC;
                END
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS dbo.uspValidateJoinCode;");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS dbo.uspGetSessionBySessionId;");
        }
    }
}
