using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class ConvertBadgeExpiryToDuration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Badges_ExpiresAt",
                schema: "badges",
                table: "badges");

            migrationBuilder.DropColumn(
                name: "ExpiresAt",
                schema: "badges",
                table: "badges");

            migrationBuilder.AddColumn<int>(
                name: "DurationDays",
                schema: "badges",
                table: "event_badges",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiresAt",
                schema: "badges",
                table: "event_badges",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DefaultDurationDays",
                schema: "badges",
                table: "badges",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_EventBadges_ExpiresAt",
                schema: "badges",
                table: "event_badges",
                column: "ExpiresAt",
                filter: "\"ExpiresAt\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Badges_DefaultDurationDays",
                schema: "badges",
                table: "badges",
                column: "DefaultDurationDays",
                filter: "\"DefaultDurationDays\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_EventBadges_ExpiresAt",
                schema: "badges",
                table: "event_badges");

            migrationBuilder.DropIndex(
                name: "IX_Badges_DefaultDurationDays",
                schema: "badges",
                table: "badges");

            migrationBuilder.DropColumn(
                name: "DurationDays",
                schema: "badges",
                table: "event_badges");

            migrationBuilder.DropColumn(
                name: "ExpiresAt",
                schema: "badges",
                table: "event_badges");

            migrationBuilder.DropColumn(
                name: "DefaultDurationDays",
                schema: "badges",
                table: "badges");

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
    }
}
