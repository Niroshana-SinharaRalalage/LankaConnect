using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class Phase6A63Fix4_CorrectColumnCase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Phase 6A.63 FIX 4: Use CORRECT lowercase column names
            migrationBuilder.Sql(@"DELETE FROM communications.email_templates WHERE name = 'event-cancelled-notification';");
            migrationBuilder.Sql(@"
INSERT INTO communications.email_templates (""id"", ""name"", ""description"", ""subject_template"", ""text_template"", ""html_template"", ""category"", ""is_active"", ""created_at"", ""updated_at"") VALUES (gen_random_uuid(), 'event-cancelled-notification', 'Event cancellation notification', 'Event Cancelled: {{EventTitle}} - LankaConnect', 'EVENT CANCELLED', '<!DOCTYPE html><html><body>Event Cancelled</body></html>', 'event', true, NOW(), NOW());
            ");
        }
        protected override void Down(MigrationBuilder migrationBuilder) { migrationBuilder.Sql(@"DELETE FROM communications.email_templates WHERE name = 'event-cancelled-notification';"); }
    }
}
