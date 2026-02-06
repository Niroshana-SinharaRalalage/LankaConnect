using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBadgeExpiryDate_Phase6A27 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiresAt",
                schema: "badges",
                table: "badges",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Badges_ExpiresAt",
                schema: "badges",
                table: "badges",
                column: "ExpiresAt",
                filter: "\"ExpiresAt\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Badges_ExpiresAt",
                schema: "badges",
                table: "badges");

            migrationBuilder.DropColumn(
                name: "ExpiresAt",
                schema: "badges",
                table: "badges");
        }
    }
}
