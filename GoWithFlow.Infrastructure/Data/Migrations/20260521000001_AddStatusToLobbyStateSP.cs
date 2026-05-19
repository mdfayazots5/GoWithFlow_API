using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GoWithFlow.Infrastructure.Data.Migrations
{
    public partial class AddStatusToLobbyStateSP : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // uspGetSessionBySessionId
            // Returns (1) session header including Status, (2) active members with slot names.
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
                        ses.Status,
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
            // Restore previous version without Status column
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
    }
}
