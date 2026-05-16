using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GoWithFlow.Infrastructure.Data.Migrations
{
    public partial class AddUserModule_Phase7 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tblUserBadge",
                columns: table => new
                {
                    UserBadgeId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    BadgeCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    BadgeName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    EarnedDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
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
                    table.PrimaryKey("PK_tblUserBadge_UserBadgeId", x => x.UserBadgeId);
                    table.ForeignKey(
                        name: "FK_tblUserBadge_UserId_tblUser_UserId",
                        column: x => x.UserId,
                        principalTable: "tblUser",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tblUserStreak",
                columns: table => new
                {
                    UserStreakId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    StreakDate = table.Column<DateTime>(type: "date", nullable: false),
                    SessionCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    PracticeMinutes = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
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
                    table.PrimaryKey("PK_tblUserStreak_UserStreakId", x => x.UserStreakId);
                    table.ForeignKey(
                        name: "FK_tblUserStreak_UserId_tblUser_UserId",
                        column: x => x.UserId,
                        principalTable: "tblUser",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IDX_tblUserBadge_UserId",
                table: "tblUserBadge",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "UK_tblUserBadge_UserId_BadgeCode",
                table: "tblUserBadge",
                columns: new[] { "UserId", "BadgeCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IDX_tblUserStreak_UserId",
                table: "tblUserStreak",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "UK_tblUserStreak_UserId_StreakDate",
                table: "tblUserStreak",
                columns: new[] { "UserId", "StreakDate" },
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tblUserBadge");

            migrationBuilder.DropTable(
                name: "tblUserStreak");
        }
    }
}
