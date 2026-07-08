using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    /// <summary>
    /// Phase 6A.133 Email: Replace simplified single-table organizer card with proper
    /// nested-table card design matching the established pattern in registration-confirmation
    /// and other templates. Fixes both template-newsletter-notification and template-event-reminder.
    /// </summary>
    public partial class Phase6A133Email_FixOrganizerCardDesign : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ============================================================
            // Phase 6A.133 Email: Fix organizer card design in 2 templates
            // Replace simplified single-table card with proper nested-table
            // card matching established design pattern.
            // ============================================================

            // Proper organizer card HTML matching the established design:
            // - Outer table with margin: 0 0 16px
            // - Inner td with background, border, border-radius: 12px, overflow: hidden
            // - Separate header table with border-bottom
            // - Separate content table with padding for {{{OrganizerContactsHtml}}}
            var properOrganizerCard = @"{{#if HasOrganizerContact}}<table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin: 0 0 16px""><tr><td style=""background: #fefaf7; border: 1px solid #f3e4d5; border-radius: 12px; overflow: hidden;""><table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0""><tr><td class=""email-card-header"" style=""padding: 14px 22px 10px; border-bottom: 1px solid #f3e4d5;""><p style=""font-family: &amp;quot;Segoe UI&amp;quot;, Arial, sans-serif; font-size: 12px; font-weight: 700; color: #9f1239; margin: 0;"">&#128222;&ensp;{{OrganizerContactHeader}}</p></td></tr></table><table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0""><tr><td class=""email-card-padding"" style=""padding: 16px 22px 18px"">{{{OrganizerContactsHtml}}}</td></tr></table></td></tr></table>{{/if}}";
            var sqlProperCard = properOrganizerCard.Replace("'", "''");

            // -------------------------------------------------------
            // 1. template-newsletter-notification:
            //    Replace the simplified organizer block (inserted by
            //    previous migration 20260310015954) with proper card.
            //    The old block uses: margin: 20px 0 0; border-radius: 8px;
            // -------------------------------------------------------

            // Step 1a: Remove the simplified organizer block
            migrationBuilder.Sql(@"
                UPDATE communications.email_templates
                SET html_template = REGEXP_REPLACE(
                    html_template,
                    '\{\{#if HasOrganizerContact\}\}.*?\{\{/if\}\}\s*<!-- CLOSING -->',
                    '<!-- CLOSING -->',
                    'gs'
                ),
                updated_at = NOW()
                WHERE name = 'template-newsletter-notification'
                  AND html_template LIKE '%HasOrganizerContact%';
            ");

            // Step 1b: Insert proper card before <!-- CLOSING -->
            migrationBuilder.Sql($@"
                UPDATE communications.email_templates
                SET html_template = REPLACE(
                    html_template,
                    '<!-- CLOSING -->',
                    '{sqlProperCard}<!-- CLOSING -->'
                ),
                updated_at = NOW()
                WHERE name = 'template-newsletter-notification'
                  AND html_template LIKE '%CLOSING%'
                  AND html_template NOT LIKE '%OrganizerContactsHtml%';
            ");

            // -------------------------------------------------------
            // 2. template-event-reminder:
            //    Same fix — replace simplified block with proper card.
            // -------------------------------------------------------

            // Step 2a: Remove the simplified organizer block
            migrationBuilder.Sql(@"
                UPDATE communications.email_templates
                SET html_template = REGEXP_REPLACE(
                    html_template,
                    '\{\{#if HasOrganizerContact\}\}.*?\{\{/if\}\}\s*<!-- CLOSING -->',
                    '<!-- CLOSING -->',
                    'gs'
                ),
                updated_at = NOW()
                WHERE name = 'template-event-reminder'
                  AND html_template LIKE '%HasOrganizerContact%';
            ");

            // Step 2b: Insert proper card before <!-- CLOSING -->
            migrationBuilder.Sql($@"
                UPDATE communications.email_templates
                SET html_template = REPLACE(
                    html_template,
                    '<!-- CLOSING -->',
                    '{sqlProperCard}<!-- CLOSING -->'
                ),
                updated_at = NOW()
                WHERE name = 'template-event-reminder'
                  AND html_template LIKE '%CLOSING%'
                  AND html_template NOT LIKE '%OrganizerContactsHtml%';
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Revert: remove proper card, re-insert simplified version
            var simplifiedBlock = @"{{#if HasOrganizerContact}}<table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin: 20px 0 0;""><tr><td style=""background: #fefaf7; padding: 20px; border-radius: 8px; border: 1px solid #f3e4d5;""><p style=""margin: 0 0 12px 0; font-size: 14px; font-weight: 700; color: #9f1239; text-transform: uppercase; letter-spacing: 0.5px;"">{{OrganizerContactHeader}}</p>{{{OrganizerContactsHtml}}}</td></tr></table>{{/if}}";
            var sqlSimplified = simplifiedBlock.Replace("'", "''");

            // Revert newsletter template
            migrationBuilder.Sql(@"
                UPDATE communications.email_templates
                SET html_template = REGEXP_REPLACE(
                    html_template,
                    '\{\{#if HasOrganizerContact\}\}.*?\{\{/if\}\}\s*<!-- CLOSING -->',
                    '<!-- CLOSING -->',
                    'gs'
                ),
                updated_at = NOW()
                WHERE name = 'template-newsletter-notification'
                  AND html_template LIKE '%HasOrganizerContact%';
            ");
            migrationBuilder.Sql($@"
                UPDATE communications.email_templates
                SET html_template = REPLACE(
                    html_template,
                    '<!-- CLOSING -->',
                    '{sqlSimplified}<!-- CLOSING -->'
                ),
                updated_at = NOW()
                WHERE name = 'template-newsletter-notification'
                  AND html_template LIKE '%CLOSING%'
                  AND html_template NOT LIKE '%OrganizerContactsHtml%';
            ");

            // Revert event reminder template
            migrationBuilder.Sql(@"
                UPDATE communications.email_templates
                SET html_template = REGEXP_REPLACE(
                    html_template,
                    '\{\{#if HasOrganizerContact\}\}.*?\{\{/if\}\}\s*<!-- CLOSING -->',
                    '<!-- CLOSING -->',
                    'gs'
                ),
                updated_at = NOW()
                WHERE name = 'template-event-reminder'
                  AND html_template LIKE '%HasOrganizerContact%';
            ");
            migrationBuilder.Sql($@"
                UPDATE communications.email_templates
                SET html_template = REPLACE(
                    html_template,
                    '<!-- CLOSING -->',
                    '{sqlSimplified}<!-- CLOSING -->'
                ),
                updated_at = NOW()
                WHERE name = 'template-event-reminder'
                  AND html_template LIKE '%CLOSING%'
                  AND html_template NOT LIKE '%OrganizerContactsHtml%';
            ");
        }
    }
}
