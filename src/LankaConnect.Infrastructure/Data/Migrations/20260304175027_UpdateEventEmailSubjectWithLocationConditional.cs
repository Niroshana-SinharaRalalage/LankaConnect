using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Infrastructure.Data.Migrations
{
    /// <summary>
    /// Updates the event email subject template to conditionally include location.
    /// Previously: "New Event: {{EventTitle}} in {{EventCity}}, {{EventState}}"
    /// Now: "New Event: {{EventTitle}}{{#if HasLocation}} in {{EventCity}}, {{EventState}}{{/if}}"
    ///
    /// This prevents "TBA, TBA" from appearing in email subjects when events have no location set,
    /// which can trigger spam filters in Gmail and other providers.
    /// </summary>
    public partial class UpdateEventEmailSubjectWithLocationConditional : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Update template-new-event-publication subject template
            // Using direct SET (not REPLACE) per architectural guidelines to avoid silent failure
            migrationBuilder.Sql(@"
                UPDATE communications.email_templates
                SET subject_template = 'New Event: {{EventTitle}}{{#if HasLocation}} in {{EventCity}}, {{EventState}}{{/if}}',
                    updated_at = NOW()
                WHERE name = 'template-new-event-publication';
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Revert to original subject template
            migrationBuilder.Sql(@"
                UPDATE communications.email_templates
                SET subject_template = 'New Event: {{EventTitle}} in {{EventCity}}, {{EventState}}',
                    updated_at = NOW()
                WHERE name = 'template-new-event-publication';
            ");
        }
    }
}
