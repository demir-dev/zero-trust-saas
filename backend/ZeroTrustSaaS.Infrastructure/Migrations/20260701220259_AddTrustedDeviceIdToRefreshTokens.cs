using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ZeroTrustSaaS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTrustedDeviceIdToRefreshTokens : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "trusted_device_id",
                table: "refresh_tokens",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "trusted_device_id",
                table: "refresh_tokens");
        }
    }
}
