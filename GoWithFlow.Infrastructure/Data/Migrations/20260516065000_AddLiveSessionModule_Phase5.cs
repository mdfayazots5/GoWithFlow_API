using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GoWithFlow.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddLiveSessionModule_Phase5 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tblListenerFeedback",
                columns: table => new
                {
                    ListenerFeedbackId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SessionId = table.Column<long>(type: "bigint", nullable: false),
                    TurnIndex = table.Column<int>(type: "int", nullable: false),
                    FromUserId = table.Column<long>(type: "bigint", nullable: false),
                    TargetUserId = table.Column<long>(type: "bigint", nullable: false),
                    FeedbackTag = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    FeedbackAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
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
                    table.PrimaryKey("PK_tblListenerFeedback_ListenerFeedbackId", x => x.ListenerFeedbackId);
                    table.ForeignKey(
                        name: "FK_tblListenerFeedback_FromUserId_tblUser_UserId",
                        column: x => x.FromUserId,
                        principalTable: "tblUser",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tblListenerFeedback_SessionId_tblSession_SessionId",
                        column: x => x.SessionId,
                        principalTable: "tblSession",
                        principalColumn: "SessionId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_tblListenerFeedback_TargetUserId_tblUser_UserId",
                        column: x => x.TargetUserId,
                        principalTable: "tblUser",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "tblTurnState",
                columns: table => new
                {
                    TurnStateId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SessionId = table.Column<long>(type: "bigint", nullable: false),
                    TurnIndex = table.Column<int>(type: "int", nullable: false),
                    TotalTurns = table.Column<int>(type: "int", nullable: false),
                    ActiveMemberId = table.Column<long>(type: "bigint", nullable: false),
                    ActiveSlotIndex = table.Column<byte>(type: "tinyint", nullable: false),
                    UtteranceId = table.Column<long>(type: "bigint", nullable: false),
                    ReReadAllowed = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    ReReadCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    MaxReReads = table.Column<int>(type: "int", nullable: false, defaultValue: 2),
                    TurnStatus = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false, defaultValue: "ACTIVE"),
                    TurnStartedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TurnCompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
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
                    table.PrimaryKey("PK_tblTurnState_TurnStateId", x => x.TurnStateId);
                    table.ForeignKey(
                        name: "FK_tblTurnState_ActiveMemberId_tblUser_UserId",
                        column: x => x.ActiveMemberId,
                        principalTable: "tblUser",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_tblTurnState_SessionId_tblSession_SessionId",
                        column: x => x.SessionId,
                        principalTable: "tblSession",
                        principalColumn: "SessionId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_tblTurnState_UtteranceId_tblUtterance_UtteranceId",
                        column: x => x.UtteranceId,
                        principalTable: "tblUtterance",
                        principalColumn: "UtteranceId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tblVoiceAnalysis",
                columns: table => new
                {
                    VoiceAnalysisId = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SessionId = table.Column<long>(type: "bigint", nullable: false),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    TurnIndex = table.Column<int>(type: "int", nullable: false),
                    UtteranceId = table.Column<long>(type: "bigint", nullable: false),
                    TranscribedText = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    ExpectedText = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    FluencyScore = table.Column<decimal>(type: "decimal(5,2)", nullable: false, defaultValue: 0m),
                    ConfidenceScore = table.Column<decimal>(type: "decimal(5,2)", nullable: false, defaultValue: 0m),
                    SpeakingSpeedWpm = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    PauseCount = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    HesitationWords = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    RepeatedWords = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    GrammarErrorsJson = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    PronunciationJson = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    OverallScore = table.Column<decimal>(type: "decimal(5,2)", nullable: false, defaultValue: 0m),
                    RecordedAt = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValueSql: "GETDATE()"),
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
                    table.PrimaryKey("PK_tblVoiceAnalysis_VoiceAnalysisId", x => x.VoiceAnalysisId);
                    table.ForeignKey(
                        name: "FK_tblVoiceAnalysis_SessionId_tblSession_SessionId",
                        column: x => x.SessionId,
                        principalTable: "tblSession",
                        principalColumn: "SessionId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_tblVoiceAnalysis_UserId_tblUser_UserId",
                        column: x => x.UserId,
                        principalTable: "tblUser",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_tblVoiceAnalysis_UtteranceId_tblUtterance_UtteranceId",
                        column: x => x.UtteranceId,
                        principalTable: "tblUtterance",
                        principalColumn: "UtteranceId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IDX_tblListenerFeedback_SessionId_TurnIndex",
                table: "tblListenerFeedback",
                columns: new[] { "SessionId", "TurnIndex" });

            migrationBuilder.CreateIndex(
                name: "IX_tblListenerFeedback_FromUserId",
                table: "tblListenerFeedback",
                column: "FromUserId");

            migrationBuilder.CreateIndex(
                name: "IX_tblListenerFeedback_TargetUserId",
                table: "tblListenerFeedback",
                column: "TargetUserId");

            migrationBuilder.CreateIndex(
                name: "IDX_tblTurnState_SessionId",
                table: "tblTurnState",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_tblTurnState_ActiveMemberId",
                table: "tblTurnState",
                column: "ActiveMemberId");

            migrationBuilder.CreateIndex(
                name: "IDX_tblTurnState_SessionId_TurnIndex",
                table: "tblTurnState",
                columns: new[] { "SessionId", "TurnIndex" });

            migrationBuilder.CreateIndex(
                name: "IX_tblTurnState_UtteranceId",
                table: "tblTurnState",
                column: "UtteranceId");

            migrationBuilder.CreateIndex(
                name: "IDX_tblVoiceAnalysis_SessionId",
                table: "tblVoiceAnalysis",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_tblVoiceAnalysis_UtteranceId",
                table: "tblVoiceAnalysis",
                column: "UtteranceId");

            migrationBuilder.CreateIndex(
                name: "IDX_tblVoiceAnalysis_UserId",
                table: "tblVoiceAnalysis",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tblListenerFeedback");

            migrationBuilder.DropTable(
                name: "tblTurnState");

            migrationBuilder.DropTable(
                name: "tblVoiceAnalysis");
        }
    }
}
