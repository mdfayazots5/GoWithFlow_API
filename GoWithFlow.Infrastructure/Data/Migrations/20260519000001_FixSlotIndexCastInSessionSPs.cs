using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GoWithFlow.Infrastructure.Data.Migrations
{
    // Fixes InvalidCastException: ROW_NUMBER() returns BIGINT in SQL Server,
    // but the repository reads SlotIndex with GetByte() (expects TINYINT).
    // Adding CAST(sps.SlotIndex AS TINYINT) in the SELECT of both SPs resolves this.
    public partial class FixSlotIndexCastInSessionSPs : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                CREATE OR ALTER PROCEDURE dbo.uspGetSessionByJoinCode
                (
                    @JoinCode NVARCHAR(8)
                )
                AS
                BEGIN
                    SET NOCOUNT ON;

                    DECLARE @SessionId  BIGINT  = 0;
                    DECLARE @ScriptId   BIGINT  = 0;
                    DECLARE @MaxMembers TINYINT = 0;

                    BEGIN TRY
                        SELECT TOP (1)
                            @SessionId  = ses.SessionId,
                            @ScriptId   = ses.ScriptId,
                            @MaxMembers = ses.MaxMembers
                        FROM dbo.tblSession AS ses
                        WHERE ses.JoinCode   = @JoinCode
                          AND ses.IsDeleted  = 0
                          AND ses.Status IN (N'LOBBY', N'ACTIVE')
                          AND (ses.RoomExpiresAt IS NULL OR ses.RoomExpiresAt > GETDATE());

                        IF @SessionId = 0
                            RETURN;

                        SELECT
                            ses.SessionId,
                            ses.SessionName,
                            ses.SessionMode,
                            scr.ScriptTitle,
                            scr.GrammarFocusTag              AS ScriptGrammarTag,
                            ses.SessionDuration              AS Duration,
                            ses.MaxMembers,
                            COUNT(sem.SessionMemberId)       AS CurrentMemberCount,
                            ses.Status
                        FROM dbo.tblSession AS ses
                        INNER JOIN dbo.tblScript AS scr
                            ON scr.ScriptId  = ses.ScriptId
                           AND scr.IsDeleted = 0
                        LEFT JOIN dbo.tblSessionMember AS sem
                            ON sem.SessionId = ses.SessionId
                           AND sem.IsDeleted = 0
                           AND sem.IsActive  = 1
                        WHERE ses.SessionId = @SessionId
                        GROUP BY
                            ses.SessionId, ses.SessionName, ses.SessionMode,
                            scr.ScriptTitle, scr.GrammarFocusTag,
                            ses.SessionDuration, ses.MaxMembers, ses.Status;

                        ;WITH SpeakerSlot AS
                        (
                            SELECT TOP (@MaxMembers)
                                ROW_NUMBER() OVER (ORDER BY src.MinSequenceId ASC, src.SpeakerLabel ASC) AS SlotIndex,
                                src.SpeakerLabel AS SlotName
                            FROM
                            (
                                SELECT
                                    ut.SpeakerLabel,
                                    MIN(ut.SequenceId) AS MinSequenceId
                                FROM dbo.tblUtterance AS ut
                                WHERE ut.ScriptId  = @ScriptId
                                  AND ut.IsDeleted = 0
                                GROUP BY ut.SpeakerLabel
                            ) AS src
                            ORDER BY src.MinSequenceId ASC, src.SpeakerLabel ASC
                        )
                        SELECT
                            CAST(sps.SlotIndex AS TINYINT)                                          AS SlotIndex,
                            sps.SlotName,
                            CAST(CASE WHEN sem.SessionMemberId IS NULL THEN 0 ELSE 1 END AS BIT)   AS IsOccupied,
                            usr.FullName                                                            AS UserFullName,
                            CAST(ISNULL(sem.IsReady, 0) AS BIT)                                    AS IsReady
                        FROM SpeakerSlot AS sps
                        LEFT JOIN dbo.tblSessionMember AS sem
                            ON sem.SessionId  = @SessionId
                           AND sem.SlotIndex  = sps.SlotIndex
                           AND sem.IsDeleted  = 0
                           AND sem.IsActive   = 1
                        LEFT JOIN dbo.tblUser AS usr
                            ON usr.UserId    = sem.UserId
                           AND usr.IsDeleted = 0
                        ORDER BY sps.SlotIndex ASC;
                    END TRY
                    BEGIN CATCH
                        THROW;
                    END CATCH
                END
                """);

            migrationBuilder.Sql(
                """
                CREATE OR ALTER PROCEDURE dbo.uspGetAvailableSlotsBySessionId
                (
                    @SessionId BIGINT
                )
                AS
                BEGIN
                    SET NOCOUNT ON;

                    DECLARE @ScriptId   BIGINT  = 0;
                    DECLARE @MaxMembers TINYINT = 0;

                    BEGIN TRY
                        SELECT
                            @ScriptId   = ses.ScriptId,
                            @MaxMembers = ses.MaxMembers
                        FROM dbo.tblSession AS ses
                        WHERE ses.SessionId = @SessionId
                          AND ses.IsDeleted = 0;

                        ;WITH SpeakerSlot AS
                        (
                            SELECT TOP (@MaxMembers)
                                ROW_NUMBER() OVER (ORDER BY src.MinSequenceId ASC, src.SpeakerLabel ASC) AS SlotIndex,
                                src.SpeakerLabel AS SlotName
                            FROM
                            (
                                SELECT
                                    ut.SpeakerLabel,
                                    MIN(ut.SequenceId) AS MinSequenceId
                                FROM dbo.tblUtterance AS ut
                                WHERE ut.ScriptId  = @ScriptId
                                  AND ut.IsDeleted = 0
                                GROUP BY ut.SpeakerLabel
                            ) AS src
                            ORDER BY src.MinSequenceId ASC, src.SpeakerLabel ASC
                        )
                        SELECT
                            CAST(sps.SlotIndex AS TINYINT)                                          AS SlotIndex,
                            sps.SlotName,
                            CAST(CASE WHEN sem.SessionMemberId IS NULL THEN 0 ELSE 1 END AS BIT)   AS IsOccupied,
                            usr.FullName                                                            AS UserFullName,
                            CAST(ISNULL(sem.IsReady, 0) AS BIT)                                    AS IsReady
                        FROM SpeakerSlot AS sps
                        LEFT JOIN dbo.tblSessionMember AS sem
                            ON sem.SessionId  = @SessionId
                           AND sem.SlotIndex  = sps.SlotIndex
                           AND sem.IsDeleted  = 0
                           AND sem.IsActive   = 1
                        LEFT JOIN dbo.tblUser AS usr
                            ON usr.UserId    = sem.UserId
                           AND usr.IsDeleted = 0
                        ORDER BY sps.SlotIndex ASC;
                    END TRY
                    BEGIN CATCH
                        THROW;
                    END CATCH
                END
                """);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Restores SlotIndex without the TINYINT cast (reverts to BIGINT from ROW_NUMBER)
            migrationBuilder.Sql(
                """
                CREATE OR ALTER PROCEDURE dbo.uspGetSessionByJoinCode
                (
                    @JoinCode NVARCHAR(8)
                )
                AS
                BEGIN
                    SET NOCOUNT ON;

                    DECLARE @SessionId  BIGINT  = 0;
                    DECLARE @ScriptId   BIGINT  = 0;
                    DECLARE @MaxMembers TINYINT = 0;

                    BEGIN TRY
                        SELECT TOP (1)
                            @SessionId  = ses.SessionId,
                            @ScriptId   = ses.ScriptId,
                            @MaxMembers = ses.MaxMembers
                        FROM dbo.tblSession AS ses
                        WHERE ses.JoinCode   = @JoinCode
                          AND ses.IsDeleted  = 0
                          AND ses.Status IN (N'LOBBY', N'ACTIVE')
                          AND (ses.RoomExpiresAt IS NULL OR ses.RoomExpiresAt > GETDATE());

                        IF @SessionId = 0
                            RETURN;

                        SELECT
                            ses.SessionId, ses.SessionName, ses.SessionMode,
                            scr.ScriptTitle, scr.GrammarFocusTag AS ScriptGrammarTag,
                            ses.SessionDuration AS Duration, ses.MaxMembers,
                            COUNT(sem.SessionMemberId) AS CurrentMemberCount, ses.Status
                        FROM dbo.tblSession AS ses
                        INNER JOIN dbo.tblScript AS scr ON scr.ScriptId = ses.ScriptId AND scr.IsDeleted = 0
                        LEFT JOIN dbo.tblSessionMember AS sem ON sem.SessionId = ses.SessionId AND sem.IsDeleted = 0 AND sem.IsActive = 1
                        WHERE ses.SessionId = @SessionId
                        GROUP BY ses.SessionId, ses.SessionName, ses.SessionMode, scr.ScriptTitle, scr.GrammarFocusTag, ses.SessionDuration, ses.MaxMembers, ses.Status;

                        ;WITH SpeakerSlot AS (
                            SELECT TOP (@MaxMembers)
                                ROW_NUMBER() OVER (ORDER BY src.MinSequenceId ASC, src.SpeakerLabel ASC) AS SlotIndex,
                                src.SpeakerLabel AS SlotName
                            FROM (SELECT ut.SpeakerLabel, MIN(ut.SequenceId) AS MinSequenceId FROM dbo.tblUtterance AS ut WHERE ut.ScriptId = @ScriptId AND ut.IsDeleted = 0 GROUP BY ut.SpeakerLabel) AS src
                            ORDER BY src.MinSequenceId ASC, src.SpeakerLabel ASC
                        )
                        SELECT sps.SlotIndex, sps.SlotName,
                            CAST(CASE WHEN sem.SessionMemberId IS NULL THEN 0 ELSE 1 END AS BIT) AS IsOccupied,
                            usr.FullName AS UserFullName, CAST(ISNULL(sem.IsReady, 0) AS BIT) AS IsReady
                        FROM SpeakerSlot AS sps
                        LEFT JOIN dbo.tblSessionMember AS sem ON sem.SessionId = @SessionId AND sem.SlotIndex = sps.SlotIndex AND sem.IsDeleted = 0 AND sem.IsActive = 1
                        LEFT JOIN dbo.tblUser AS usr ON usr.UserId = sem.UserId AND usr.IsDeleted = 0
                        ORDER BY sps.SlotIndex ASC;
                    END TRY
                    BEGIN CATCH THROW; END CATCH
                END
                """);

            migrationBuilder.Sql(
                """
                CREATE OR ALTER PROCEDURE dbo.uspGetAvailableSlotsBySessionId
                (
                    @SessionId BIGINT
                )
                AS
                BEGIN
                    SET NOCOUNT ON;

                    DECLARE @ScriptId BIGINT = 0; DECLARE @MaxMembers TINYINT = 0;
                    BEGIN TRY
                        SELECT @ScriptId = ses.ScriptId, @MaxMembers = ses.MaxMembers FROM dbo.tblSession AS ses WHERE ses.SessionId = @SessionId AND ses.IsDeleted = 0;

                        ;WITH SpeakerSlot AS (
                            SELECT TOP (@MaxMembers)
                                ROW_NUMBER() OVER (ORDER BY src.MinSequenceId ASC, src.SpeakerLabel ASC) AS SlotIndex,
                                src.SpeakerLabel AS SlotName
                            FROM (SELECT ut.SpeakerLabel, MIN(ut.SequenceId) AS MinSequenceId FROM dbo.tblUtterance AS ut WHERE ut.ScriptId = @ScriptId AND ut.IsDeleted = 0 GROUP BY ut.SpeakerLabel) AS src
                            ORDER BY src.MinSequenceId ASC, src.SpeakerLabel ASC
                        )
                        SELECT sps.SlotIndex, sps.SlotName,
                            CAST(CASE WHEN sem.SessionMemberId IS NULL THEN 0 ELSE 1 END AS BIT) AS IsOccupied,
                            usr.FullName AS UserFullName, CAST(ISNULL(sem.IsReady, 0) AS BIT) AS IsReady
                        FROM SpeakerSlot AS sps
                        LEFT JOIN dbo.tblSessionMember AS sem ON sem.SessionId = @SessionId AND sem.SlotIndex = sps.SlotIndex AND sem.IsDeleted = 0 AND sem.IsActive = 1
                        LEFT JOIN dbo.tblUser AS usr ON usr.UserId = sem.UserId AND usr.IsDeleted = 0
                        ORDER BY sps.SlotIndex ASC;
                    END TRY
                    BEGIN CATCH THROW; END CATCH
                END
                """);
        }
    }
}
