using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.SPLIT_PER_ENTITY.Migrations
{
    /// <inheritdoc />
    /// <summary>
    /// Phase 6A.133 Email: Fix organizer card placement and syntax in ALL 4 templates:
    /// - template-newsletter-notification
    /// - template-event-reminder
    /// - template-signup-list-commitment-confirmation
    /// - template-signup-list-commitment-update
    ///
    /// Strategy:
    /// 1. Remove all existing organizer blocks (both {{#if}} and legacy {{#block}} syntax)
    /// 2. Template-specific structural cleanup (PICKUP card, empty row)
    /// 3. Insert organizer as a self-contained outer row with proper background before CLOSING
    /// </summary>
    public partial class Phase6A133Email_FixAllTemplateOrganizerCards : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ============================================================
            // Organizer card as a complete self-contained outer row.
            // Has its own <tr><td> with background: #fef9f5 to prevent
            // gradient bleed-through from the outer container.
            // ============================================================
            var orgRow = @"<tr><td align=""center"" style=""background: #fef9f5""><!--[if mso]><table role=""presentation"" width=""720"" align=""center"" cellspacing=""0"" cellpadding=""0"" border=""0""><tr><td><![endif]--><table role=""presentation"" width=""720"" cellspacing=""0"" cellpadding=""0"" border=""0"" class=""email-content"" style=""max-width: 720px; width: 100%""><tr><td class=""email-body-padding"" style=""padding: 0 40px 12px"">{{#if HasOrganizerContact}}<table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin: 0 0 16px""><tr><td style=""background: #fefaf7; border: 1px solid #f3e4d5; border-radius: 12px; overflow: hidden;""><table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0""><tr><td class=""email-card-header"" style=""padding: 14px 22px 10px; border-bottom: 1px solid #f3e4d5;""><p style=""font-family: &amp;quot;Segoe UI&amp;quot;, Arial, sans-serif; font-size: 12px; font-weight: 700; color: #9f1239; margin: 0;"">&#128222;&ensp;{{OrganizerContactHeader}}</p></td></tr></table><table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0""><tr><td class=""email-card-padding"" style=""padding: 16px 22px 18px"">{{{OrganizerContactsHtml}}}</td></tr></table></td></tr></table>{{/if}}</td></tr></table><!--[if mso]></td></tr></table><![endif]--></td></tr>";
            var sqlOrgRow = orgRow.Replace("'", "''");

            var templateNames = new[]
            {
                "template-newsletter-notification",
                "template-event-reminder",
                "template-signup-list-commitment-confirmation",
                "template-signup-list-commitment-update"
            };

            // -----------------------------------------------
            // Phase 1: Remove ALL organizer blocks from ALL 4 templates
            // Handles both new {{#if}} and legacy {{#block}} syntax
            // -----------------------------------------------
            foreach (var name in templateNames)
            {
                // 1a: Remove {{#if HasOrganizerContact}}...{{/if}}
                migrationBuilder.Sql($@"
                    UPDATE communications.email_templates
                    SET html_template = REGEXP_REPLACE(
                        html_template,
                        '\{{\{{#if HasOrganizerContact\}}\}}.*?\{{\{{/if\}}\}}',
                        '',
                        'gs'
                    ),
                    updated_at = NOW()
                    WHERE name = '{name}'
                      AND html_template LIKE '%#if HasOrganizerContact%';
                ");

                // 1b: Remove {{#HasOrganizerContact}}...{{/HasOrganizerContact}}
                migrationBuilder.Sql($@"
                    UPDATE communications.email_templates
                    SET html_template = REGEXP_REPLACE(
                        html_template,
                        '\{{\{{#HasOrganizerContact\}}\}}.*?\{{\{{/HasOrganizerContact\}}\}}',
                        '',
                        'gs'
                    ),
                    updated_at = NOW()
                    WHERE name = '{name}'
                      AND html_template LIKE '%HasOrganizerContact%';
                ");
            }

            // -----------------------------------------------
            // Phase 2a: Signup-confirmation — remove broken empty PICKUP/DELIVERY card
            // This card has opening tags (<table><tr><td>) but no content,
            // creating orphaned HTML elements that render as a colored bar.
            // -----------------------------------------------
            migrationBuilder.Sql(@"
                UPDATE communications.email_templates
                SET html_template = REGEXP_REPLACE(
                    html_template,
                    '<!-- PICKUP/DELIVERY CARD -->\s*<table[^>]*>\s*<tr>\s*<td[^>]*>',
                    '',
                    'gs'
                ),
                updated_at = NOW()
                WHERE name = 'template-signup-list-commitment-confirmation'
                  AND html_template LIKE '%PICKUP/DELIVERY CARD%';
            ");

            // -----------------------------------------------
            // Phase 2b: Signup-update — fix broken empty separate row
            // After organizer removal, there's an empty <tr><td class="email-body-padding">
            // row that was the wrapper for the old organizer. Remove it and
            // add proper closing structure for the content section.
            // -----------------------------------------------
            migrationBuilder.Sql(@"
                UPDATE communications.email_templates
                SET html_template = REGEXP_REPLACE(
                    html_template,
                    '</tr>\s*<!--\[if mso\]>.*?<!\[endif\]-->\s*<tr>\s*<td\s+class=""email-body-padding""[^>]*>\s*</td>\s*</tr>\s*</table>\s*<!-- CLOSING -->',
                    '</td></tr></table><!--[if mso]></td></tr></table><![endif]--></td></tr><!-- CLOSING -->',
                    'gs'
                ),
                updated_at = NOW()
                WHERE name = 'template-signup-list-commitment-update'
                  AND html_template LIKE '%<!-- CLOSING -->%'
                  AND html_template NOT LIKE '%OrganizerContactsHtml%';
            ");

            // -----------------------------------------------
            // Phase 3: Insert organizer row before <!-- CLOSING --> in ALL templates
            // The organizer is a self-contained <tr> with background: #fef9f5
            // that matches the closing section, preventing gradient bleed.
            // -----------------------------------------------
            foreach (var name in templateNames)
            {
                migrationBuilder.Sql($@"
                    UPDATE communications.email_templates
                    SET html_template = REPLACE(
                        html_template,
                        '<!-- CLOSING -->',
                        '{sqlOrgRow}<!-- CLOSING -->'
                    ),
                    updated_at = NOW()
                    WHERE name = '{name}'
                      AND html_template LIKE '%<!-- CLOSING -->%'
                      AND html_template NOT LIKE '%OrganizerContactsHtml%';
                ");
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Revert: remove the organizer row from all 4 templates
            var templateNames = new[]
            {
                "template-newsletter-notification",
                "template-event-reminder",
                "template-signup-list-commitment-confirmation",
                "template-signup-list-commitment-update"
            };

            foreach (var name in templateNames)
            {
                // Remove the self-contained organizer row
                migrationBuilder.Sql($@"
                    UPDATE communications.email_templates
                    SET html_template = REGEXP_REPLACE(
                        html_template,
                        '<tr><td align=""center"" style=""background: #fef9f5"">.*?OrganizerContactsHtml.*?</td></tr>\s*<!-- CLOSING -->',
                        '<!-- CLOSING -->',
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
