using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GoWithFlow.Infrastructure.Data.Migrations
{
    // Fixes stale IsReady flag when a member leaves the lobby.
    // Previously uspUpdateSessionMemberLeft did not reset IsReady = 0, so a member
    // who had toggled ready before leaving would show as ready if they re-joined.
    // This also preserves the existing auto-abandon logic (host left / no members remain).
    public partial class FixMemberLeftSP_ResetIsReady : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                CREATE OR ALTER PROCEDURE dbo.uspUpdateSessionMemberLeft
                (
                    @SessionId  BIGINT,
                    @UserId     BIGINT,
                    @UpdatedBy  NVARCHAR(128),
                    @IPAddress  NVARCHAR(64)
                )
                AS
                BEGIN
                    SET NOCOUNT ON;

                    -- Capture host flag before deactivating the row
                    DECLARE @IsHost BIT = 0;

                    SELECT @IsHost = CAST(sem.IsHost AS BIT)
                    FROM dbo.tblSessionMember AS sem
                    WHERE sem.SessionId = @SessionId
                      AND sem.UserId    = @UserId
                      AND sem.IsActive  = 1
                      AND sem.IsDeleted = 0;

                    -- Deactivate member and clear ready flag
                    UPDATE dbo.tblSessionMember
                    SET IsActive    = 0,
                        IsReady     = 0,
                        LeftAt      = GETDATE(),
                        UpdatedBy   = @UpdatedBy,
                        LastUpdated = GETDATE(),
                        IPAddress   = @IPAddress
                    WHERE SessionId = @SessionId
                      AND UserId    = @UserId
                      AND IsActive  = 1
                      AND IsDeleted = 0;

                    -- Auto-abandon when the host leaves
                    IF @IsHost = 1
                    BEGIN
                        UPDATE dbo.tblSession
                        SET Status      = N'ABANDONED',
                            UpdatedBy   = @UpdatedBy,
                            LastUpdated = GETDATE()
                        WHERE SessionId = @SessionId
                          AND IsDeleted = 0;

                        RETURN;
                    END

                    -- Auto-abandon when no active members remain
                    IF NOT EXISTS
                    (
                        SELECT 1
                        FROM dbo.tblSessionMember
                        WHERE SessionId = @SessionId
                          AND IsActive  = 1
                          AND IsDeleted = 0
                    )
                    BEGIN
                        UPDATE dbo.tblSession
                        SET Status      = N'ABANDONED',
                            UpdatedBy   = @UpdatedBy,
                            LastUpdated = GETDATE()
                        WHERE SessionId = @SessionId
                          AND IsDeleted = 0;
                    END
                END
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Restore previous version without IsReady = 0 reset
            migrationBuilder.Sql(
                """
                CREATE OR ALTER PROCEDURE dbo.uspUpdateSessionMemberLeft
                (
                    @SessionId  BIGINT,
                    @UserId     BIGINT,
                    @UpdatedBy  NVARCHAR(128),
                    @IPAddress  NVARCHAR(64)
                )
                AS
                BEGIN
                    SET NOCOUNT ON;

                    DECLARE @IsHost BIT = 0;

                    SELECT @IsHost = CAST(sem.IsHost AS BIT)
                    FROM dbo.tblSessionMember AS sem
                    WHERE sem.SessionId = @SessionId
                      AND sem.UserId    = @UserId
                      AND sem.IsActive  = 1
                      AND sem.IsDeleted = 0;

                    UPDATE dbo.tblSessionMember
                    SET IsActive    = 0,
                        LeftAt      = GETDATE(),
                        UpdatedBy   = @UpdatedBy,
                        LastUpdated = GETDATE(),
                        IPAddress   = @IPAddress
                    WHERE SessionId = @SessionId
                      AND UserId    = @UserId
                      AND IsActive  = 1
                      AND IsDeleted = 0;

                    IF @IsHost = 1
                    BEGIN
                        UPDATE dbo.tblSession
                        SET Status      = N'ABANDONED',
                            UpdatedBy   = @UpdatedBy,
                            LastUpdated = GETDATE()
                        WHERE SessionId = @SessionId
                          AND IsDeleted = 0;

                        RETURN;
                    END

                    IF NOT EXISTS
                    (
                        SELECT 1
                        FROM dbo.tblSessionMember
                        WHERE SessionId = @SessionId
                          AND IsActive  = 1
                          AND IsDeleted = 0
                    )
                    BEGIN
                        UPDATE dbo.tblSession
                        SET Status      = N'ABANDONED',
                            UpdatedBy   = @UpdatedBy,
                            LastUpdated = GETDATE()
                        WHERE SessionId = @SessionId
                          AND IsDeleted = 0;
                    END
                END
                """);
        }
    }
}
