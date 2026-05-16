using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GoWithFlow.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAdminModule_Phase2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tblAdminNote",
                columns: table => new
                {
                    AdminNoteId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AdminUserId = table.Column<long>(type: "bigint", nullable: false),
                    TargetUserId = table.Column<long>(type: "bigint", nullable: false),
                    NoteText = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    NoteDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
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
                    table.PrimaryKey("PK_tblAdminNote_AdminNoteId", x => x.AdminNoteId);
                    table.ForeignKey(
                        name: "FK_tblAdminNote_AdminUserId_tblUser_UserId",
                        column: x => x.AdminUserId,
                        principalTable: "tblUser",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tblAdminNote_TargetUserId_tblUser_UserId",
                        column: x => x.TargetUserId,
                        principalTable: "tblUser",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tblDashboardMetric",
                columns: table => new
                {
                    DashboardMetricId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MetricDate = table.Column<DateTime>(type: "date", nullable: false),
                    TotalUsers = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    ActiveSessionsToday = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    TotalScriptsUploaded = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    TotalMistakesRecorded = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
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
                    table.PrimaryKey("PK_tblDashboardMetric_DashboardMetricId", x => x.DashboardMetricId);
                });

            migrationBuilder.CreateIndex(
                name: "IDX_tblAdminNote_TargetUserId",
                table: "tblAdminNote",
                column: "TargetUserId");

            migrationBuilder.CreateIndex(
                name: "IX_tblAdminNote_AdminUserId",
                table: "tblAdminNote",
                column: "AdminUserId");

            migrationBuilder.CreateIndex(
                name: "UK_tblDashboardMetric_MetricDate",
                table: "tblDashboardMetric",
                column: "MetricDate",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tblAdminNote");

            migrationBuilder.DropTable(
                name: "tblDashboardMetric");
        }
    }
}
