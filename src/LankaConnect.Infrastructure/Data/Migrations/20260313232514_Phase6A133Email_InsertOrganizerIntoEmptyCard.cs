using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.SPLIT_PER_ENTITY.Migrations
{
    /// <inheritdoc />
    /// <summary>
    /// Phase 6A.133: Insert organizer card into existing empty ORGANIZER CONTACT CARD td.
    /// The previous migration removed the organizer content but the td placeholder remains.
    /// Also adds background to the organizer td to prevent gradient bleed.
    /// </summary>
    public partial class Phase6A133Email_InsertOrganizerIntoEmptyCard : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // The organizer card HTML to insert
            var orgCard = @"{{#if HasOrganizerContact}}<table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin: 0 0 16px""><tr><td style=""background: #fefaf7; border: 1px solid #f3e4d5; border-radius: 12px; overflow: hidden;""><table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0""><tr><td class=""email-card-header"" style=""padding: 14px 22px 10px; border-bottom: 1px solid #f3e4d5;""><p style=""font-family: &amp;quot;Segoe UI&amp;quot;, Arial, sans-serif; font-size: 12px; font-weight: 700; color: #9f1239; margin: 0;"">&#128222;&ensp;{{OrganizerContactHeader}}</p></td></tr></table><table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0""><tr><td class=""email-card-padding"" style=""padding: 16px 22px 18px"">{{{OrganizerContactsHtml}}}</td></tr></table></td></tr></table>{{/if}}";
            var sqlOrgCard = orgCard.Replace("'", "''");

            // ============================================================
            // signup-update: Insert organizer into the empty ORGANIZER CONTACT CARD td
            // Current DB has: <!-- ORGANIZER CONTACT CARD -->\n\s*\n\s*</td>
            // We replace with: <!-- ORGANIZER CONTACT CARD -->\n{orgCard}\n</td>
            // Also add background to the organizer td.
            // ============================================================
            migrationBuilder.Sql($@"
                UPDATE communications.email_templates
                SET html_template = REPLACE(
                    html_template,
                    '<!-- ORGANIZER CONTACT CARD -->

											</td>',
                    '<!-- ORGANIZER CONTACT CARD -->
{sqlOrgCard}
											</td>'
                ),
                updated_at = NOW()
                WHERE name = 'template-signup-list-commitment-update'
                  AND html_template LIKE '%ORGANIZER CONTACT CARD%'
                  AND html_template NOT LIKE '%OrganizerContactsHtml%';
            ");

            // Add background to the organizer td (prevent gradient bleed)
            migrationBuilder.Sql(@"
                UPDATE communications.email_templates
                SET html_template = REPLACE(
                    html_template,
                    '<td class=""email-body-padding"" style=""padding: 32px 40px 12px"">
						<!-- ORGANIZER CONTACT CARD -->',
                    '<td class=""email-body-padding"" style=""background: #fef9f5; padding: 32px 40px 12px"">
						<!-- ORGANIZER CONTACT CARD -->'
                ),
                updated_at = NOW()
                WHERE name = 'template-signup-list-commitment-update'
                  AND html_template LIKE '%ORGANIZER CONTACT CARD%';
            ");

            // ============================================================
            // signup-confirmation: Same fix if it has the empty organizer td
            // ============================================================
            migrationBuilder.Sql($@"
                UPDATE communications.email_templates
                SET html_template = REPLACE(
                    html_template,
                    '<!-- ORGANIZER CONTACT CARD -->

											</td>',
                    '<!-- ORGANIZER CONTACT CARD -->
{sqlOrgCard}
											</td>'
                ),
                updated_at = NOW()
                WHERE name = 'template-signup-list-commitment-confirmation'
                  AND html_template LIKE '%ORGANIZER CONTACT CARD%'
                  AND html_template NOT LIKE '%OrganizerContactsHtml%';
            ");

            migrationBuilder.Sql(@"
                UPDATE communications.email_templates
                SET html_template = REPLACE(
                    html_template,
                    '<td class=""email-body-padding"" style=""padding: 32px 40px 12px"">
						<!-- ORGANIZER CONTACT CARD -->',
                    '<td class=""email-body-padding"" style=""background: #fef9f5; padding: 32px 40px 12px"">
						<!-- ORGANIZER CONTACT CARD -->'
                ),
                updated_at = NOW()
                WHERE name = 'template-signup-list-commitment-confirmation'
                  AND html_template LIKE '%ORGANIZER CONTACT CARD%';
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Revert: remove organizer card content, leaving empty td
            var templateNames = new[]
            {
                "template-signup-list-commitment-update",
                "template-signup-list-commitment-confirmation"
            };

            foreach (var name in templateNames)
            {
                migrationBuilder.Sql($@"
                    UPDATE communications.email_templates
                    SET html_template = REGEXP_REPLACE(
                        html_template,
                        '<!-- ORGANIZER CONTACT CARD -->\s*\{{\{{#if HasOrganizerContact\}}\}}.*?\{{\{{/if\}}\}}\s*</td>',
                        '<!-- ORGANIZER CONTACT CARD -->

											</td>',
                        'gs'
                    ),
                    updated_at = NOW()
                    WHERE name = '{name}'
                      AND html_template LIKE '%OrganizerContactsHtml%';
                ");
            }
        }
    }
}
