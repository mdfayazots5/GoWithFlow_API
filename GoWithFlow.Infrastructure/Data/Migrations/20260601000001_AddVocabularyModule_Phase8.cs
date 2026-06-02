using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GoWithFlow.Infrastructure.Data.Migrations
{
    public partial class AddVocabularyModule_Phase8 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // tblUserVocabulary — tracks FocusWords practiced per user per VocabularySprint session.
            // Raw SQL used so the EF model snapshot does not require an entity mapping.
            // Access via ADO.NET in VocabularyRepository.
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'tblUserVocabulary')
BEGIN
    CREATE TABLE dbo.tblUserVocabulary (
        UserVocabularyId    BIGINT          IDENTITY(1,1)   NOT NULL,
        UserId              BIGINT          NOT NULL,
        FocusWord           NVARCHAR(64)    NOT NULL,
        SourceSessionId     BIGINT          NOT NULL,
        DateIntroduced      DATETIME2       NOT NULL        DEFAULT GETDATE(),
        WasProducedCorrectly BIT            NOT NULL        DEFAULT 0,
        TimesEncountered    INT             NOT NULL        DEFAULT 1,
        Tag                 NVARCHAR(64)    NULL,
        Comments            NVARCHAR(256)   NULL,
        SortOrder           INT             NOT NULL        DEFAULT 0,
        IPAddress           NVARCHAR(64)    NOT NULL        DEFAULT '127.0.0.1',
        CreatedBy           NVARCHAR(128)   NOT NULL        DEFAULT 'System',
        DateCreated         DATETIME2       NOT NULL        DEFAULT GETDATE(),
        UpdatedBy           NVARCHAR(128)   NULL,
        LastUpdated         DATETIME2       NULL,
        DeletedBy           NVARCHAR(128)   NULL,
        DateDeleted         DATETIME2       NULL,
        IsDeleted           BIT             NOT NULL        DEFAULT 0,
        CONSTRAINT PK_tblUserVocabulary_UserVocabularyId
            PRIMARY KEY (UserVocabularyId),
        CONSTRAINT FK_tblUserVocabulary_UserId_tblUser_UserId
            FOREIGN KEY (UserId) REFERENCES dbo.tblUser(UserId) ON DELETE CASCADE,
        CONSTRAINT FK_tblUserVocabulary_SourceSessionId_tblSession_SessionId
            FOREIGN KEY (SourceSessionId) REFERENCES dbo.tblSession(SessionId),
        CONSTRAINT UK_tblUserVocabulary_UserId_FocusWord_SessionId
            UNIQUE (UserId, FocusWord, SourceSessionId)
    );

    CREATE INDEX IDX_tblUserVocabulary_UserId
        ON dbo.tblUserVocabulary (UserId);

    CREATE INDEX IDX_tblUserVocabulary_UserId_FocusWord
        ON dbo.tblUserVocabulary (UserId, FocusWord);
END
");

            // PostgreSQL equivalent — applied via Supabase migration script
            // See: Docs/PostgreSQLMigration/17_add_vocabulary_module.sql
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'tblUserVocabulary')
BEGIN
    DROP TABLE dbo.tblUserVocabulary;
END
");
        }
    }
}
