using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GoWithFlow.Infrastructure.Data.Migrations
{
    public partial class AddLearningGoal_Phase9 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // tblUserGoal — stores active learning goal per user.
            // Raw SQL so the EF model snapshot does not require an entity mapping.
            // Access via ADO.NET in UserRepository.
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'tblUserGoal')
BEGIN
    CREATE TABLE dbo.tblUserGoal (
        UserGoalId      BIGINT          IDENTITY(1,1)   NOT NULL,
        UserId          BIGINT          NOT NULL,
        GoalType        NVARCHAR(64)    NOT NULL,
        TimelineWeeks   INT             NOT NULL,
        StartDate       DATETIME2       NOT NULL        DEFAULT GETDATE(),
        TargetDate      DATETIME2       NOT NULL,
        DetectedLevel   NVARCHAR(32)    NOT NULL        DEFAULT 'Intermediate',
        StartingScore   DECIMAL(5,2)    NOT NULL        DEFAULT 0,
        IsActive        BIT             NOT NULL        DEFAULT 1,
        SortOrder       INT             NOT NULL        DEFAULT 0,
        IPAddress       NVARCHAR(64)    NOT NULL        DEFAULT '127.0.0.1',
        CreatedBy       NVARCHAR(128)   NOT NULL        DEFAULT 'System',
        DateCreated     DATETIME2       NOT NULL        DEFAULT GETDATE(),
        UpdatedBy       NVARCHAR(128)   NULL,
        LastUpdated     DATETIME2       NULL,
        DeletedBy       NVARCHAR(128)   NULL,
        DateDeleted     DATETIME2       NULL,
        IsDeleted       BIT             NOT NULL        DEFAULT 0,
        CONSTRAINT PK_tblUserGoal_UserGoalId
            PRIMARY KEY (UserGoalId),
        CONSTRAINT FK_tblUserGoal_UserId_tblUser_UserId
            FOREIGN KEY (UserId) REFERENCES dbo.tblUser(UserId) ON DELETE CASCADE
    );

    CREATE INDEX IDX_tblUserGoal_UserId          ON dbo.tblUserGoal (UserId);
    CREATE INDEX IDX_tblUserGoal_UserId_IsActive  ON dbo.tblUserGoal (UserId, IsActive);
END
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'tblUserGoal')
BEGIN
    DROP TABLE dbo.tblUserGoal;
END
");
        }
    }
}
