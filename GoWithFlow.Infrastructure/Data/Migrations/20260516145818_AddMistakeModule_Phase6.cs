using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GoWithFlow.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMistakeModule_Phase6 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tblMistake",
                columns: table => new
                {
                    MistakeId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    SessionId = table.Column<long>(type: "bigint", nullable: false),
                    UtteranceId = table.Column<long>(type: "bigint", nullable: false),
                    ScriptId = table.Column<long>(type: "bigint", nullable: false),
                    UtteranceText = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    SpokenText = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    MistakeType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    MistakeDetail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    GrammarTag = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    ContextTag = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    CorrectionText = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    PracticeCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    IsResolved = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    FirstOccurrence = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
                    LastAttempt = table.Column<DateTime>(type: "datetime2", nullable: true),
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
                    table.PrimaryKey("PK_tblMistake_MistakeId", x => x.MistakeId);
                    table.ForeignKey(
                        name: "FK_tblMistake_ScriptId_tblScript_ScriptId",
                        column: x => x.ScriptId,
                        principalTable: "tblScript",
                        principalColumn: "ScriptId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_tblMistake_SessionId_tblSession_SessionId",
                        column: x => x.SessionId,
                        principalTable: "tblSession",
                        principalColumn: "SessionId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_tblMistake_UserId_tblUser_UserId",
                        column: x => x.UserId,
                        principalTable: "tblUser",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_tblMistake_UtteranceId_tblUtterance_UtteranceId",
                        column: x => x.UtteranceId,
                        principalTable: "tblUtterance",
                        principalColumn: "UtteranceId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tblRepracticeSession",
                columns: table => new
                {
                    RepracticeSessionId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    SourceSessionId = table.Column<long>(type: "bigint", nullable: false),
                    TotalMistakes = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    CompletedRounds = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    ImprovementPercent = table.Column<decimal>(type: "decimal(5,2)", nullable: false, defaultValue: 0m),
                    Status = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false, defaultValue: "PENDING"),
                    GeneratedDate = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
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
                    table.PrimaryKey("PK_tblRepracticeSession_RepracticeSessionId", x => x.RepracticeSessionId);
                    table.ForeignKey(
                        name: "FK_tblRepracticeSession_SourceSessionId_tblSession_SessionId",
                        column: x => x.SourceSessionId,
                        principalTable: "tblSession",
                        principalColumn: "SessionId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_tblRepracticeSession_UserId_tblUser_UserId",
                        column: x => x.UserId,
                        principalTable: "tblUser",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tblRepracticeUtterance",
                columns: table => new
                {
                    RepracticeUtteranceId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RepracticeSessionId = table.Column<long>(type: "bigint", nullable: false),
                    MistakeId = table.Column<long>(type: "bigint", nullable: false),
                    OriginalUtteranceId = table.Column<long>(type: "bigint", nullable: false),
                    EnglishText = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    HintText = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    MistakeType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    MistakeDetail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    CorrectionNote = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    AttemptCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    BestScore = table.Column<decimal>(type: "decimal(5,2)", nullable: false, defaultValue: 0m),
                    LastScore = table.Column<decimal>(type: "decimal(5,2)", nullable: false, defaultValue: 0m),
                    IsResolved = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
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
                    table.PrimaryKey("PK_tblRepracticeUtterance_RepracticeUtteranceId", x => x.RepracticeUtteranceId);
                    table.ForeignKey(
                        name: "FK_tblRepracticeUtterance_MistakeId_tblMistake_MistakeId",
                        column: x => x.MistakeId,
                        principalTable: "tblMistake",
                        principalColumn: "MistakeId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_tblRepracticeUtterance_OriginalUtteranceId_tblUtterance_UtteranceId",
                        column: x => x.OriginalUtteranceId,
                        principalTable: "tblUtterance",
                        principalColumn: "UtteranceId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_tblRepracticeUtterance_RepracticeSessionId_tblRepracticeSession_RepracticeSessionId",
                        column: x => x.RepracticeSessionId,
                        principalTable: "tblRepracticeSession",
                        principalColumn: "RepracticeSessionId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IDX_tblMistake_GrammarTag",
                table: "tblMistake",
                column: "GrammarTag");

            migrationBuilder.CreateIndex(
                name: "IDX_tblMistake_SessionId",
                table: "tblMistake",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IDX_tblMistake_UserId",
                table: "tblMistake",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IDX_tblMistake_UserId_IsResolved",
                table: "tblMistake",
                columns: new[] { "UserId", "IsResolved" });

            migrationBuilder.CreateIndex(
                name: "IX_tblMistake_ScriptId",
                table: "tblMistake",
                column: "ScriptId");

            migrationBuilder.CreateIndex(
                name: "IX_tblMistake_UtteranceId",
                table: "tblMistake",
                column: "UtteranceId");

            migrationBuilder.CreateIndex(
                name: "IDX_tblRepracticeSession_UserId",
                table: "tblRepracticeSession",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_tblRepracticeSession_SourceSessionId",
                table: "tblRepracticeSession",
                column: "SourceSessionId");

            migrationBuilder.CreateIndex(
                name: "IDX_tblRepracticeUtterance_RepracticeSessionId",
                table: "tblRepracticeUtterance",
                column: "RepracticeSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_tblRepracticeUtterance_MistakeId",
                table: "tblRepracticeUtterance",
                column: "MistakeId");

            migrationBuilder.CreateIndex(
                name: "IX_tblRepracticeUtterance_OriginalUtteranceId",
                table: "tblRepracticeUtterance",
                column: "OriginalUtteranceId");

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tblRepracticeUtterance");

            migrationBuilder.DropTable(
                name: "tblMistake");

            migrationBuilder.DropTable(
                name: "tblRepracticeSession");
        }
    }
}
