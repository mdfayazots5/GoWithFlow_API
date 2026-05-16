using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GoWithFlow.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSessionModule_Phase4 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tblSession",
                columns: table => new
                {
                    SessionId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SessionName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    JoinCode = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: false),
                    SessionMode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    MaxMembers = table.Column<byte>(type: "tinyint", nullable: false, defaultValue: (byte)4),
                    SessionDuration = table.Column<int>(type: "int", nullable: false),
                    HostUserId = table.Column<long>(type: "bigint", nullable: false),
                    ScriptId = table.Column<long>(type: "bigint", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false, defaultValue: "LOBBY"),
                    RoomExpiryMinutes = table.Column<int>(type: "int", nullable: false),
                    RoomExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    StartedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EndedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ActualDurationSec = table.Column<int>(type: "int", nullable: true),
                    Tag = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    Comments = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    IPAddress = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false, defaultValue: "127.0.0.1"),
                    CreatedBy = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false, defaultValue: "Admin"),
                    DateCreated = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    UpdatedBy = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    LastUpdated = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    DateDeleted = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tblSession_SessionId", x => x.SessionId);
                    table.ForeignKey(
                        name: "FK_tblSession_HostUserId_tblUser_UserId",
                        column: x => x.HostUserId,
                        principalTable: "tblUser",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_tblSession_ScriptId_tblScript_ScriptId",
                        column: x => x.ScriptId,
                        principalTable: "tblScript",
                        principalColumn: "ScriptId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tblSessionMember",
                columns: table => new
                {
                    SessionMemberId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SessionId = table.Column<long>(type: "bigint", nullable: false),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    SlotIndex = table.Column<byte>(type: "tinyint", nullable: false),
                    SlotName = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    IsReady = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    IsHost = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    JoinedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LeftAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    Tag = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    Comments = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    IPAddress = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false, defaultValue: "127.0.0.1"),
                    CreatedBy = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false, defaultValue: "Admin"),
                    DateCreated = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    UpdatedBy = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    LastUpdated = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DeletedBy = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    DateDeleted = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tblSessionMember_SessionMemberId", x => x.SessionMemberId);
                    table.ForeignKey(
                        name: "FK_tblSessionMember_SessionId_tblSession_SessionId",
                        column: x => x.SessionId,
                        principalTable: "tblSession",
                        principalColumn: "SessionId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_tblSessionMember_UserId_tblUser_UserId",
                        column: x => x.UserId,
                        principalTable: "tblUser",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IDX_tblSession_HostUserId",
                table: "tblSession",
                column: "HostUserId");

            migrationBuilder.CreateIndex(
                name: "IDX_tblSession_Status",
                table: "tblSession",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_tblSession_ScriptId",
                table: "tblSession",
                column: "ScriptId");

            migrationBuilder.CreateIndex(
                name: "UK_tblSession_JoinCode",
                table: "tblSession",
                column: "JoinCode",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IDX_tblSessionMember_SessionId",
                table: "tblSessionMember",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IDX_tblSessionMember_UserId",
                table: "tblSessionMember",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "UK_tblSessionMember_SessionId_SlotIndex",
                table: "tblSessionMember",
                columns: new[] { "SessionId", "SlotIndex" },
                unique: true,
                filter: "[IsDeleted] = 0 AND [IsActive] = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tblSessionMember");

            migrationBuilder.DropTable(
                name: "tblSession");
        }
    }
}
