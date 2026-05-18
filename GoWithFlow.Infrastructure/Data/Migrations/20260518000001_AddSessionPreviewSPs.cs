using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GoWithFlow.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSessionPreviewSPs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // uspGetSessionByJoinCode
            // Returns: (1) session header, (2) all slots with occupancy
            migrationBuilder.Sql(
                """
                CREATE OR ALTER PROCEDURE dbo.uspGetSessionByJoinCode
                    @JoinCode NVARCHAR(8)
                AS
                BEGIN
                    SET NOCOUNT ON;

                    DECLARE @SessionId  BIGINT;
                    DECLARE @ScriptId   BIGINT;
                    DECLARE @MaxMembers TINYINT;

                    SELECT
                        @SessionId  = s.SessionId,
                        @ScriptId   = s.ScriptId,
                        @MaxMembers = s.MaxMembers
                    FROM dbo.tblSession s
                    WHERE s.JoinCode  = @JoinCode
                      AND s.IsDeleted = 0
                      AND s.Status    = 'LOBBY';

                    IF @SessionId IS NULL
                        RETURN;

                    -- Result Set 1: session header
                    SELECT
                        s.SessionId,
                        s.SessionName,
                        s.SessionMode,
                        sc.ScriptTitle,
                        sc.GrammarFocusTag AS ScriptGrammarTag,
                        s.SessionDuration  AS Duration,
                        s.MaxMembers,
                        (
                            SELECT COUNT(*)
                            FROM dbo.tblSessionMember sm2
                            WHERE sm2.SessionId = s.SessionId
                              AND sm2.IsActive  = 1
                              AND sm2.IsDeleted = 0
                        ) AS CurrentMemberCount,
                        s.Status
                    FROM dbo.tblSession s
                    JOIN dbo.tblScript sc ON sc.ScriptId = s.ScriptId
                    WHERE s.SessionId = @SessionId;

                    -- Result Set 2: all slots 1..MaxMembers with occupancy
                    ;WITH SpeakerRank AS (
                        SELECT
                            LTRIM(RTRIM(u.SpeakerLabel))                      AS SlotName,
                            ROW_NUMBER() OVER (ORDER BY MIN(u.SequenceId))    AS SlotIndex
                        FROM dbo.tblUtterance u
                        WHERE u.ScriptId  = @ScriptId
                          AND u.IsDeleted = 0
                          AND LTRIM(RTRIM(u.SpeakerLabel)) <> ''
                        GROUP BY LTRIM(RTRIM(u.SpeakerLabel))
                    ),
                    SlotNumbers AS (
                        SELECT n
                        FROM (VALUES(1),(2),(3),(4),(5),(6),(7),(8)) AS v(n)
                        WHERE n <= @MaxMembers
                    )
                    SELECT
                        sn.n                                                            AS SlotIndex,
                        COALESCE(sr.SlotName, 'Slot ' + CAST(sn.n AS NVARCHAR(2)))     AS SlotName,
                        CASE WHEN sm.SessionMemberId IS NOT NULL
                             THEN CAST(1 AS BIT)
                             ELSE CAST(0 AS BIT) END                                    AS IsOccupied,
                        u.FullName                                                      AS UserFullName,
                        COALESCE(sm.IsReady, CAST(0 AS BIT))                            AS IsReady
                    FROM SlotNumbers sn
                    LEFT JOIN SpeakerRank sr
                           ON sr.SlotIndex = sn.n
                    LEFT JOIN dbo.tblSessionMember sm
                           ON sm.SessionId = @SessionId
                          AND sm.SlotIndex = sn.n
                          AND sm.IsActive  = 1
                          AND sm.IsDeleted = 0
                    LEFT JOIN dbo.tblUser u
                           ON u.UserId    = sm.UserId
                          AND u.IsDeleted = 0
                    ORDER BY sn.n;
                END
                """);

            // uspGetAvailableSlotsBySessionId
            // Called during JoinSession to find which slots are still free
            migrationBuilder.Sql(
                """
                CREATE OR ALTER PROCEDURE dbo.uspGetAvailableSlotsBySessionId
                    @SessionId BIGINT
                AS
                BEGIN
                    SET NOCOUNT ON;

                    DECLARE @ScriptId   BIGINT;
                    DECLARE @MaxMembers TINYINT;

                    SELECT
                        @ScriptId   = ScriptId,
                        @MaxMembers = MaxMembers
                    FROM dbo.tblSession
                    WHERE SessionId = @SessionId
                      AND IsDeleted = 0;

                    IF @ScriptId IS NULL
                        RETURN;

                    ;WITH SpeakerRank AS (
                        SELECT
                            LTRIM(RTRIM(u.SpeakerLabel))                      AS SlotName,
                            ROW_NUMBER() OVER (ORDER BY MIN(u.SequenceId))    AS SlotIndex
                        FROM dbo.tblUtterance u
                        WHERE u.ScriptId  = @ScriptId
                          AND u.IsDeleted = 0
                          AND LTRIM(RTRIM(u.SpeakerLabel)) <> ''
                        GROUP BY LTRIM(RTRIM(u.SpeakerLabel))
                    ),
                    SlotNumbers AS (
                        SELECT n
                        FROM (VALUES(1),(2),(3),(4),(5),(6),(7),(8)) AS v(n)
                        WHERE n <= @MaxMembers
                    )
                    SELECT
                        sn.n                                                            AS SlotIndex,
                        COALESCE(sr.SlotName, 'Slot ' + CAST(sn.n AS NVARCHAR(2)))     AS SlotName,
                        CASE WHEN sm.SessionMemberId IS NOT NULL
                             THEN CAST(1 AS BIT)
                             ELSE CAST(0 AS BIT) END                                    AS IsOccupied,
                        u.FullName                                                      AS UserFullName,
                        COALESCE(sm.IsReady, CAST(0 AS BIT))                            AS IsReady
                    FROM SlotNumbers sn
                    LEFT JOIN SpeakerRank sr
                           ON sr.SlotIndex = sn.n
                    LEFT JOIN dbo.tblSessionMember sm
                           ON sm.SessionId = @SessionId
                          AND sm.SlotIndex = sn.n
                          AND sm.IsActive  = 1
                          AND sm.IsDeleted = 0
                    LEFT JOIN dbo.tblUser u
                           ON u.UserId    = sm.UserId
                          AND u.IsDeleted = 0
                    ORDER BY sn.n;
                END
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS dbo.uspGetSessionByJoinCode;");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS dbo.uspGetAvailableSlotsBySessionId;");
        }
    }
}
