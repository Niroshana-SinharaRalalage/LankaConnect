using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Infrastructure.Data.Migrations
{
    /// <summary>
    /// Phase 6A.103: Add event image support to 5 email templates.
    ///
    /// Currently only 2 templates (free-event-registration-confirmation and
    /// paid-event-registration-confirmation-with-ticket) have the {{#HasEventImage}}
    /// conditional image block. This migration adds it to 5 more event-related templates.
    ///
    /// Templates updated:
    /// 1. template-event-details-publication (manual event notification)
    /// 2. template-new-event-publication (auto event published notification)
    /// 3. template-event-reminder (event reminder emails)
    /// 4. template-event-cancellation-notifications (event cancellation emails)
    /// 5. template-event-approval (event approved by admin)
    ///
    /// The image block is inserted between the HEADER and BODY CONTENT sections,
    /// matching the pattern used in the existing registration confirmation templates.
    ///
    /// Idempotent: Uses NOT LIKE '%HasEventImage%' guard to prevent double injection.
    /// </summary>
    public partial class Phase6A103_AddEventImageToEmailTemplates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Inject the {{#HasEventImage}} image block into 5 templates.
            // Uses PostgreSQL replace() to insert the image HTML before the <!-- BODY CONTENT marker.
            // The NOT LIKE guard ensures idempotency.
            migrationBuilder.Sql(@"
UPDATE communications.email_templates
SET html_template = replace(
    html_template,
    '<!-- BODY CONTENT',
    '<!-- EVENT IMAGE (conditional + graceful fallback) -->
                        {{#HasEventImage}}
                        <!--[if !mso]><!-->
                        <tr>
                            <td style=""font-size: 0; line-height: 0"">
                                <!--<![endif]-->
                                <img
                                    src=""{{EventImageUrl}}""
                                    alt=""{{EventTitle}}""
                                    width=""860""
                                    style=""width: 100%; max-height: 300px; object-fit: cover; display: block""
                                />
                                <!--[if !mso]><!-->
                            </td>
                        </tr>
                        <!--<![endif]-->
                        {{/HasEventImage}}

                        <!-- BODY CONTENT'),
    updated_at = NOW()
WHERE name IN (
    'template-event-details-publication',
    'template-new-event-publication',
    'template-event-reminder',
    'template-event-cancellation-notifications',
    'template-event-approval'
)
AND html_template NOT LIKE '%HasEventImage%';
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove the image block by replacing the full image HTML + BODY CONTENT marker
            // back to just the BODY CONTENT marker.
            migrationBuilder.Sql(@"
UPDATE communications.email_templates
SET html_template = replace(
    html_template,
    '<!-- EVENT IMAGE (conditional + graceful fallback) -->
                        {{#HasEventImage}}
                        <!--[if !mso]><!-->
                        <tr>
                            <td style=""font-size: 0; line-height: 0"">
                                <!--<![endif]-->
                                <img
                                    src=""{{EventImageUrl}}""
                                    alt=""{{EventTitle}}""
                                    width=""860""
                                    style=""width: 100%; max-height: 300px; object-fit: cover; display: block""
                                />
                                <!--[if !mso]><!-->
                            </td>
                        </tr>
                        <!--<![endif]-->
                        {{/HasEventImage}}

                        <!-- BODY CONTENT',
    '<!-- BODY CONTENT'),
    updated_at = NOW()
WHERE name IN (
    'template-event-details-publication',
    'template-new-event-publication',
    'template-event-reminder',
    'template-event-cancellation-notifications',
    'template-event-approval'
)
AND html_template LIKE '%HasEventImage%';
");
        }
    }
}
