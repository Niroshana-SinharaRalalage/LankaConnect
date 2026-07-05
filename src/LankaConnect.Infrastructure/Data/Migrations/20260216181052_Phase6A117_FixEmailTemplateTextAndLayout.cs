using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.SPLIT_PER_ENTITY.Migrations
{
    /// <summary>
    /// Phase 6A.117: Fix Email Template Text and Layout Issues
    ///
    /// Problem 1 (Issue #10): "Feel free to reply to this email" text encourages replies to automated emails
    /// Problem 2 (Issues #11, #12): Empty PICKUP/DELIVERY card section creates layout spacing issues
    ///
    /// Solution 1: Remove "feel free to reply" text entirely from 3 templates
    /// Solution 2: Remove empty card sections from 2 signup list commitment templates
    ///
    /// Templates Updated:
    /// Issue #10:
    /// 1. template-event-registration-cancellation
    /// 2. template-event-reminder
    /// 3. template-signup-list-commitment-update
    ///
    /// Issues #11, #12:
    /// 4. template-signup-list-commitment-confirmation
    /// 5. template-signup-list-commitment-update
    ///
    /// This uses PostgreSQL REPLACE() and REGEXP_REPLACE() functions to update templates directly.
    /// </summary>
    public partial class Phase6A117_FixEmailTemplateTextAndLayout : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Fix Issue #10: Remove "feel free to reply" text entirely
            // Better UX: Don't encourage replies to automated emails
            // Organizer contact card already provides direct contact info
            migrationBuilder.Sql(@"
                UPDATE communications.email_templates
                SET
                    html_template = REPLACE(
                        html_template,
                        '<br />If you have questions, feel free to reply to this email.',
                        ''
                    ),
                    updated_at = NOW()
                WHERE name IN (
                    'template-event-registration-cancellation',
                    'template-event-reminder',
                    'template-signup-list-commitment-update'
                )
                AND html_template LIKE '%If you have questions, feel free to reply to this email.%';
            ");

            // Fix Issues #11 & #12: Remove empty PICKUP/DELIVERY card
            // Removes extra whitespace and layout inconsistency
            migrationBuilder.Sql(@"
                UPDATE communications.email_templates
                SET
                    html_template = REGEXP_REPLACE(
                        html_template,
                        '<!-- PICKUP/DELIVERY CARD -->[\s\S]*?</table>[\s\S]*?<!--\[if mso\]>[\s\S]*?<!\[endif\]-->[\s\S]*?</td>[\s\S]*?</tr>',
                        '',
                        'g'
                    ),
                    updated_at = NOW()
                WHERE name IN (
                    'template-signup-list-commitment-confirmation',
                    'template-signup-list-commitment-update'
                )
                AND html_template LIKE '%PICKUP/DELIVERY CARD%';
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Rollback not implemented - these are template text fixes that improve UX
            // Reverting would restore poor UX practices (encouraging email replies)
            // and layout issues (empty card sections)

            // If rollback is absolutely required, restore templates from Template_Correction folder
            // or use Phase6A113 migration as baseline
        }
    }
}
