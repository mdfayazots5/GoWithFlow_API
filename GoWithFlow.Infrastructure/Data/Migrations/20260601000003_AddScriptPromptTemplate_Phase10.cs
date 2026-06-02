using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GoWithFlow.Infrastructure.Data.Migrations
{
    /// <summary>
    /// Creates tblScriptPromptTemplate (SQL Server).
    /// PostgreSQL equivalent: Docs/PostgreSQLMigration/19_add_script_prompt_template.sql
    /// Seeding is done via the PostgreSQL migration script (includes all 6 category prompts
    /// from ExcelTemplateStandard.md). SQL Server seeding is skipped here — run via
    /// a separate data script if SQL Server is the active provider.
    /// </summary>
    public partial class AddScriptPromptTemplate_Phase10 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'tblScriptPromptTemplate')
BEGIN
    CREATE TABLE dbo.tblScriptPromptTemplate (
        PromptTemplateId    BIGINT          IDENTITY(1,1)   NOT NULL,
        Category            NVARCHAR(64)    NOT NULL,
        PromptText          NVARCHAR(MAX)   NOT NULL,
        SourceRef           NVARCHAR(256)   NOT NULL        DEFAULT 'ExcelTemplateStandard.md',
        Version             INT             NOT NULL        DEFAULT 1,
        IsActive            BIT             NOT NULL        DEFAULT 1,
        SortOrder           INT             NOT NULL        DEFAULT 0,
        IPAddress           NVARCHAR(64)    NOT NULL        DEFAULT '127.0.0.1',
        CreatedBy           NVARCHAR(128)   NOT NULL        DEFAULT 'System',
        DateCreated         DATETIME2       NOT NULL        DEFAULT GETDATE(),
        UpdatedBy           NVARCHAR(128)   NULL,
        LastUpdated         DATETIME2       NULL,
        DeletedBy           NVARCHAR(128)   NULL,
        DateDeleted         DATETIME2       NULL,
        IsDeleted           BIT             NOT NULL        DEFAULT 0,
        CONSTRAINT PK_tblScriptPromptTemplate_PromptTemplateId
            PRIMARY KEY (PromptTemplateId),
        CONSTRAINT UK_tblScriptPromptTemplate_Category
            UNIQUE (Category)
    );
    CREATE INDEX IDX_tblScriptPromptTemplate_Category ON dbo.tblScriptPromptTemplate (Category);
END
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'tblScriptPromptTemplate')
BEGIN
    DROP TABLE dbo.tblScriptPromptTemplate;
END
");
        }
    }
}
