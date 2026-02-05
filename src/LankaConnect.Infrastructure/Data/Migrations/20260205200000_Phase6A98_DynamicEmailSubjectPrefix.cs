using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Infrastructure.Data.Migrations;

/// <summary>
/// Phase 6A.98: Update event-details-publication template to use dynamic subject prefix.
///
/// Changes:
/// - Updates subject from "New Event: {{EventTitle}}" to "{{SubjectPrefix}} {{EventTitle}}"
/// - Backend provides SubjectPrefix as "New Event:" or "Upcoming Event:" based on PublishedAt date
/// - Events published within 7 days get "New Event:" prefix
/// - Events published more than 7 days ago get "Upcoming Event:" prefix
/// </summary>
public partial class Phase6A98_DynamicEmailSubjectPrefix : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Update the template-event-details-publication subject to use dynamic SubjectPrefix
        migrationBuilder.Sql(@"
            UPDATE communications.email_templates
            SET subject_template = '{{SubjectPrefix}} {{EventTitle}} in {{EventCity}}, {{EventState}}'
            WHERE name = 'template-event-details-publication';
        ");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Revert to the original hardcoded "New Event:" prefix
        migrationBuilder.Sql(@"
            UPDATE communications.email_templates
            SET subject_template = 'New Event: {{EventTitle}} in {{EventCity}}, {{EventState}}'
            WHERE name = 'template-event-details-publication';
        ");
    }
}
