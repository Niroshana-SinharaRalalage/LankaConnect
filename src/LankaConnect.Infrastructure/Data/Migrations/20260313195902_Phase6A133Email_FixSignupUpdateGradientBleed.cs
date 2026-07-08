using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    /// <summary>
    /// Phase 6A.133 Email Fix: signup-update template gradient bleed
    ///
    /// Problem: The Event Details section sits in a separate outer-table row
    /// whose td has no background, so the gradient shows through.
    /// Also: the previous migration inserted the organizer card as a
    /// self-contained outer row, which visually separates it from content.
    ///
    /// Fix:
    /// 1. Remove the self-contained organizer outer row (from previous migration)
    /// 2. Add background: #fef9f5 to the Event Details section td
    /// 3. Insert organizer card INSIDE the Event Details content cell
    ///    (before the cell closes), so it stays within the white content area
    /// </summary>
    public partial class Phase6A133Email_FixSignupUpdateGradientBleed : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ============================================================
            // Step 1: Remove the self-contained organizer outer row
            // that was inserted by the previous migration (before <!-- CLOSING -->).
            // This row has: <tr><td align="center" style="background: #fef9f5">...OrganizerContactsHtml...
            // ============================================================
            migrationBuilder.Sql(@"
                UPDATE communications.email_templates
                SET html_template = REGEXP_REPLACE(
                    html_template,
                    '<tr><td align=""center"" style=""background: #fef9f5"">.*?OrganizerContactsHtml.*?</td></tr>\s*(?=<!-- CLOSING -->)',
                    '',
                    'gs'
                ),
                updated_at = NOW()
                WHERE name = 'template-signup-list-commitment-update'
                  AND html_template LIKE '%OrganizerContactsHtml%';
            ");

            // ============================================================
            // Step 2: Add background to the Event Details section td.
            // The Event Details section starts with a <tr> whose <td> has
            // class=""email-body-padding"" but no background style.
            // We find: <td class=""email-body-padding"" style=""padding: 32px 40px 12px"">
            // followed by <!-- EVENT DETAILS CARD -->
            // and add background: #fef9f5 to the style.
            // ============================================================
            migrationBuilder.Sql(@"
                UPDATE communications.email_templates
                SET html_template = REGEXP_REPLACE(
                    html_template,
                    '<td\s+class=""email-body-padding""\s+style=""padding:\s*32px\s+40px\s+12px"">\s*(?=\s*<!--\s*EVENT DETAILS CARD\s*-->)',
                    '<td class=""email-body-padding"" style=""background: #fef9f5; padding: 32px 40px 12px"">',
                    'gs'
                ),
                updated_at = NOW()
                WHERE name = 'template-signup-list-commitment-update'
                  AND html_template LIKE '%EVENT DETAILS CARD%';
            ");

            // ============================================================
            // Step 3: Insert organizer card INSIDE the Event Details content
            // cell, right before the closing </td></tr></table> sequence
            // that precedes <!-- CLOSING -->.
            //
            // We look for the MSO closing comment + </td></tr> before CLOSING:
            //   <!--[if mso]>...</td></tr></table><![endif]--></td></tr><!-- CLOSING -->
            // and insert the organizer card before </td></tr><!-- CLOSING -->
            // ============================================================
            var orgCard = @"{{#if HasOrganizerContact}}<table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin: 16px 0 0""><tr><td style=""background: #fefaf7; border: 1px solid #f3e4d5; border-radius: 12px; overflow: hidden;""><table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0""><tr><td class=""email-card-header"" style=""padding: 14px 22px 10px; border-bottom: 1px solid #f3e4d5;""><p style=""font-family: &amp;quot;Segoe UI&amp;quot;, Arial, sans-serif; font-size: 12px; font-weight: 700; color: #9f1239; margin: 0;"">&#128222;&ensp;{{OrganizerContactHeader}}</p></td></tr></table><table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0""><tr><td class=""email-card-padding"" style=""padding: 16px 22px 18px"">{{{OrganizerContactsHtml}}}</td></tr></table></td></tr></table>{{/if}}";
            var sqlOrgCard = orgCard.Replace("'", "''");

            migrationBuilder.Sql($@"
                UPDATE communications.email_templates
                SET html_template = REGEXP_REPLACE(
                    html_template,
                    '(</td>\s*</tr>\s*</table>\s*<!--\[if mso\]>.*?<!\[endif\]-->\s*</td>\s*</tr>)\s*(?=\s*<!-- CLOSING -->)',
                    '{sqlOrgCard}\1',
                    'gs'
                ),
                updated_at = NOW()
                WHERE name = 'template-signup-list-commitment-update'
                  AND html_template NOT LIKE '%OrganizerContactsHtml%';
            ");

            // ============================================================
            // Also fix signup-confirmation if it has same gradient issue.
            // Add background to the Event Details td.
            // ============================================================
            migrationBuilder.Sql(@"
                UPDATE communications.email_templates
                SET html_template = REGEXP_REPLACE(
                    html_template,
                    '<td\s+class=""email-body-padding""\s+style=""padding:\s*32px\s+40px\s+12px"">\s*(?=\s*<!--\s*EVENT DETAILS CARD\s*-->)',
                    '<td class=""email-body-padding"" style=""background: #fef9f5; padding: 32px 40px 12px"">',
                    'gs'
                ),
                updated_at = NOW()
                WHERE name = 'template-signup-list-commitment-confirmation'
                  AND html_template LIKE '%EVENT DETAILS CARD%';
            ");

            // Remove self-contained organizer outer row from signup-confirmation too
            migrationBuilder.Sql(@"
                UPDATE communications.email_templates
                SET html_template = REGEXP_REPLACE(
                    html_template,
                    '<tr><td align=""center"" style=""background: #fef9f5"">.*?OrganizerContactsHtml.*?</td></tr>\s*(?=<!-- CLOSING -->)',
                    '',
                    'gs'
                ),
                updated_at = NOW()
                WHERE name = 'template-signup-list-commitment-confirmation'
                  AND html_template LIKE '%OrganizerContactsHtml%';
            ");

            // Insert organizer inside signup-confirmation content area
            migrationBuilder.Sql($@"
                UPDATE communications.email_templates
                SET html_template = REGEXP_REPLACE(
                    html_template,
                    '(</td>\s*</tr>\s*</table>\s*<!--\[if mso\]>.*?<!\[endif\]-->\s*</td>\s*</tr>)\s*(?=\s*<!-- CLOSING -->)',
                    '{sqlOrgCard}\1',
                    'gs'
                ),
                updated_at = NOW()
                WHERE name = 'template-signup-list-commitment-confirmation'
                  AND html_template NOT LIKE '%OrganizerContactsHtml%';
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Revert: remove organizer card from inside content area
            var templateNames = new[]
            {
                "template-signup-list-commitment-update",
                "template-signup-list-commitment-confirmation"
            };

            foreach (var name in templateNames)
            {
                // Remove the inline organizer card
                migrationBuilder.Sql($@"
                    UPDATE communications.email_templates
                    SET html_template = REGEXP_REPLACE(
                        html_template,
                        '\{{\{{#if HasOrganizerContact\}}\}}.*?OrganizerContactsHtml.*?\{{\{{/if\}}\}}',
                        '',
                        'gs'
                    ),
                    updated_at = NOW()
                    WHERE name = '{name}'
                      AND html_template LIKE '%OrganizerContactsHtml%';
                ");

                // Remove background from Event Details td
                migrationBuilder.Sql($@"
                    UPDATE communications.email_templates
                    SET html_template = REGEXP_REPLACE(
                        html_template,
                        'background: #fef9f5; padding: 32px 40px 12px',
                        'padding: 32px 40px 12px',
                        'g'
                    ),
                    updated_at = NOW()
                    WHERE name = '{name}';
                ");
            }
        }
    }
}
