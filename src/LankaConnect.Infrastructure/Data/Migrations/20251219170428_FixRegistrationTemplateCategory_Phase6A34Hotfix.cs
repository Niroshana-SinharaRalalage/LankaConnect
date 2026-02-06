using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class FixRegistrationTemplateCategory_Phase6A34Hotfix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // HOTFIX: Phase 6A.34 seeded 'Event' category which is INVALID
            // EmailTemplateCategory only supports: Authentication, Business, Marketing, System, Notification
            // According to domain logic: EmailType.Transactional => System category
            // This caused "Cannot access value of a failed result" during EF Core deserialization

            migrationBuilder.Sql(@"
                UPDATE communications.email_templates
                SET ""category"" = 'System'
                WHERE ""name"" = 'registration-confirmation'
                  AND ""category"" = 'Event';
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Revert to invalid 'Event' category (not recommended, but for rollback completeness)
            migrationBuilder.Sql(@"
                UPDATE communications.email_templates
                SET ""category"" = 'Event'
                WHERE ""name"" = 'registration-confirmation'
                  AND ""category"" = 'System';
            ");
        }
    }
}
