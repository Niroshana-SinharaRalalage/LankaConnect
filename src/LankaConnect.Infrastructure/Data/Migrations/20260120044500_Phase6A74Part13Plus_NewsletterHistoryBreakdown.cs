using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Infrastructure.Data.Migrations
{
    /// <summary>
    /// Phase 6A.74 Part 13+: Add detailed recipient breakdown columns to newsletter_email_history
    /// Adds columns to track all 4 recipient sources separately plus success/failed counts
    /// </summary>
    public partial class Phase6A74Part13Plus_NewsletterHistoryBreakdown : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add new columns for detailed breakdown
            migrationBuilder.AddColumn<int>(
                name: "newsletter_email_group_count",
                schema: "communications",
                table: "newsletter_email_history",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "event_email_group_count",
                schema: "communications",
                table: "newsletter_email_history",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "subscriber_count",
                schema: "communications",
                table: "newsletter_email_history",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "event_registration_count",
                schema: "communications",
                table: "newsletter_email_history",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "successful_sends",
                schema: "communications",
                table: "newsletter_email_history",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "failed_sends",
                schema: "communications",
                table: "newsletter_email_history",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            // Backfill existing records using legacy columns
            migrationBuilder.Sql(@"
                UPDATE communications.newsletter_email_history
                SET
                    newsletter_email_group_count = COALESCE(email_group_recipient_count, 0),
                    subscriber_count = COALESCE(subscriber_recipient_count, 0),
                    successful_sends = total_recipient_count,
                    failed_sends = 0
                WHERE newsletter_email_group_count = 0
                  AND subscriber_count = 0
                  AND successful_sends = 0;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "newsletter_email_group_count",
                schema: "communications",
                table: "newsletter_email_history");

            migrationBuilder.DropColumn(
                name: "event_email_group_count",
                schema: "communications",
                table: "newsletter_email_history");

            migrationBuilder.DropColumn(
                name: "subscriber_count",
                schema: "communications",
                table: "newsletter_email_history");

            migrationBuilder.DropColumn(
                name: "event_registration_count",
                schema: "communications",
                table: "newsletter_email_history");

            migrationBuilder.DropColumn(
                name: "successful_sends",
                schema: "communications",
                table: "newsletter_email_history");

            migrationBuilder.DropColumn(
                name: "failed_sends",
                schema: "communications",
                table: "newsletter_email_history");
        }
    }
}
