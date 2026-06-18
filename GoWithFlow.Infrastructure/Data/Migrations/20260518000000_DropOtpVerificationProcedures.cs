using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GoWithFlow.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class DropOtpVerificationProcedures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drift cleanup: the backing table tblOtpVerification was removed by
            // 20260517000000_RemoveOtpVerification, but these stored procedures were left
            // orphaned in the SQL Server catalog (no live endpoint, no entity, no repository
            // references them). Drop them so the OTP contract is fully gone.
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS dbo.uspInsertOtpVerification;");
            migrationBuilder.Sql("DROP PROCEDURE IF EXISTS dbo.uspVerifyOtp;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Intentional no-op: the SQL Server procedure bodies are not retained in the repo
            // and the backing table no longer exists, so these orphaned procedures cannot be
            // meaningfully recreated. The OTP feature is decommissioned.
        }
    }
}
