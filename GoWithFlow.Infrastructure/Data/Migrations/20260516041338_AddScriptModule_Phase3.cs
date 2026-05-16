using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GoWithFlow.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddScriptModule_Phase3 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF TYPE_ID(N'dbo.UtteranceTVP') IS NULL
                BEGIN
                    CREATE TYPE dbo.UtteranceTVP AS TABLE
                    (
                        SequenceId INT NOT NULL,
                        SpeakerLabel NVARCHAR(64) NOT NULL,
                        EnglishText NVARCHAR(512) NOT NULL,
                        HintText NVARCHAR(512) NULL,
                        GrammarTag NVARCHAR(64) NULL,
                        ContextTag NVARCHAR(64) NULL,
                        FocusWord NVARCHAR(64) NULL,
                        PronunciationNote NVARCHAR(256) NULL
                    );
                END
                """);

            migrationBuilder.CreateTable(
                name: "tblScript",
                columns: table => new
                {
                    ScriptId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ScriptTitle = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    GrammarFocusTag = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ContextTag = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ComplexityLevel = table.Column<byte>(type: "tinyint", nullable: false),
                    TargetAgeGroup = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    HintLanguage = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    UploadedDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    UploadedByUserId = table.Column<long>(type: "bigint", nullable: false),
                    Version = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    UtteranceCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
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
                    table.PrimaryKey("PK_tblScript_ScriptId", x => x.ScriptId);
                    table.ForeignKey(
                        name: "FK_tblScript_UploadedByUserId_tblUser_UserId",
                        column: x => x.UploadedByUserId,
                        principalTable: "tblUser",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tblScriptVersion",
                columns: table => new
                {
                    ScriptVersionId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ScriptId = table.Column<long>(type: "bigint", nullable: false),
                    VersionNumber = table.Column<int>(type: "int", nullable: false),
                    VersionNotes = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    UploadedByUserId = table.Column<long>(type: "bigint", nullable: false),
                    UploadedDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
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
                    table.PrimaryKey("PK_tblScriptVersion_ScriptVersionId", x => x.ScriptVersionId);
                    table.ForeignKey(
                        name: "FK_tblScriptVersion_ScriptId_tblScript_ScriptId",
                        column: x => x.ScriptId,
                        principalTable: "tblScript",
                        principalColumn: "ScriptId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_tblScriptVersion_UploadedByUserId_tblUser_UserId",
                        column: x => x.UploadedByUserId,
                        principalTable: "tblUser",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tblUtterance",
                columns: table => new
                {
                    UtteranceId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ScriptId = table.Column<long>(type: "bigint", nullable: false),
                    SequenceId = table.Column<int>(type: "int", nullable: false),
                    SpeakerLabel = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    EnglishText = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    HintText = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    GrammarTag = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    ContextTag = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    FocusWord = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    PronunciationNote = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
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
                    table.PrimaryKey("PK_tblUtterance_UtteranceId", x => x.UtteranceId);
                    table.ForeignKey(
                        name: "FK_tblUtterance_ScriptId_tblScript_ScriptId",
                        column: x => x.ScriptId,
                        principalTable: "tblScript",
                        principalColumn: "ScriptId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IDX_tblScript_Category",
                table: "tblScript",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IDX_tblScript_GrammarFocusTag",
                table: "tblScript",
                column: "GrammarFocusTag");

            migrationBuilder.CreateIndex(
                name: "IDX_tblScript_IsActive",
                table: "tblScript",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_tblScript_UploadedByUserId",
                table: "tblScript",
                column: "UploadedByUserId");

            migrationBuilder.CreateIndex(
                name: "IDX_tblScriptVersion_ScriptId",
                table: "tblScriptVersion",
                column: "ScriptId");

            migrationBuilder.CreateIndex(
                name: "IX_tblScriptVersion_UploadedByUserId",
                table: "tblScriptVersion",
                column: "UploadedByUserId");

            migrationBuilder.CreateIndex(
                name: "IDX_tblUtterance_ScriptId",
                table: "tblUtterance",
                column: "ScriptId");

            migrationBuilder.CreateIndex(
                name: "UK_tblUtterance_ScriptId_SequenceId",
                table: "tblUtterance",
                columns: new[] { "ScriptId", "SequenceId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tblScriptVersion");

            migrationBuilder.DropTable(
                name: "tblUtterance");

            migrationBuilder.DropTable(
                name: "tblScript");

            migrationBuilder.Sql(
                """
                IF TYPE_ID(N'dbo.UtteranceTVP') IS NOT NULL
                BEGIN
                    DROP TYPE dbo.UtteranceTVP;
                END
                """);
        }
    }
}
