using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZeroTrustSaaS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RemovePlanAddMfaRecoveryCodes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Plan",
                table: "tenants");

            migrationBuilder.AddColumn<string>(
                name: "mfa_recovery_code_hashes",
                table: "users",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "mfa_recovery_code_hashes",
                table: "users");

            migrationBuilder.AddColumn<int>(
                name: "Plan",
                table: "tenants",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
