using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Infrastructure.Data.Migrations
{
    /// <summary>
    /// Phase 6A.74 Part 14: Add IsAnnouncementOnly column to newsletters table
    ///
    /// This migration adds support for two newsletter types:
    /// 1. Published Newsletters (IsAnnouncementOnly = false):
    ///    - Must be published before sending emails
    ///    - Visible on public /newsletters page when Active
    ///    - Status flow: Draft → Active → Inactive
    ///
    /// 2. Announcement-Only Newsletters (IsAnnouncementOnly = true):
    ///    - Auto-activates on creation (skips Draft state)
    ///    - NOT visible on public /newsletters page
    ///    - Can send emails immediately after creation
    ///    - Status flow: (auto)Active → Inactive
    ///
    /// Both types can send unlimited emails while Active (removed old "Sent" lock behavior).
    /// </summary>
    public partial class Phase6A74Part14_AddNewsletterIsAnnouncementOnly : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add is_announcement_only column with default value false
            // All existing newsletters are "published" type (IsAnnouncementOnly = false)
            migrationBuilder.AddColumn<bool>(
                name: "is_announcement_only",
                schema: "communications",
                table: "newsletters",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            // Index for filtering announcement-only newsletters
            migrationBuilder.CreateIndex(
                name: "ix_newsletters_is_announcement_only",
                schema: "communications",
                table: "newsletters",
                column: "is_announcement_only");

            // Composite index for public newsletter queries
            // Optimizes queries that filter by status AND is_announcement_only
            migrationBuilder.CreateIndex(
                name: "ix_newsletters_status_is_announcement_only",
                schema: "communications",
                table: "newsletters",
                columns: new[] { "status", "is_announcement_only" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop composite index first
            migrationBuilder.DropIndex(
                name: "ix_newsletters_status_is_announcement_only",
                schema: "communications",
                table: "newsletters");

            // Drop simple index
            migrationBuilder.DropIndex(
                name: "ix_newsletters_is_announcement_only",
                schema: "communications",
                table: "newsletters");

            // Drop column
            migrationBuilder.DropColumn(
                name: "is_announcement_only",
                schema: "communications",
                table: "newsletters");
        }
    }
}
