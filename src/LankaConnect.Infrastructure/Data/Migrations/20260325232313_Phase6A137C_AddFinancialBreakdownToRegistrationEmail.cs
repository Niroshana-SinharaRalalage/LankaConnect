using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Infrastructure.Data.Migrations
{
    /// <summary>
    /// Phase 6A.137C: Add financial breakdown section to paid registration confirmation email template.
    ///
    /// When a registration includes a bundled donation, the email now shows:
    /// - Ticket Price: $140.00
    /// - Donation: $25.00
    /// - Total: $165.00
    ///
    /// Uses Handlebars {{#if HasFinancialBreakdown}} conditional so existing registrations
    /// without donations are unaffected.
    ///
    /// New placeholders:
    /// - {{HasFinancialBreakdown}} - Boolean flag
    /// - {{HasDonation}} - Boolean flag
    /// - {{TicketSubtotal}} - Ticket price portion
    /// - {{DonationAmount}} - Donation portion
    /// - {{BreakdownCurrency}} - Currency code
    /// </summary>
    public partial class Phase6A137C_AddFinancialBreakdownToRegistrationEmail : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add financial breakdown section to HTML template.
            // Strategy: Insert the breakdown section just BEFORE the closing </td></tr></table>
            // that wraps the payment details card (identified by the AmountPaid placeholder).
            // We use REGEXP_REPLACE to find the AmountPaid row and add breakdown rows after it.
            migrationBuilder.Sql(@"
                UPDATE communications.email_templates
                SET html_template = REGEXP_REPLACE(
                    html_template,
                    '(\{\{AmountPaid\}\})',
                    E'{{AmountPaid}}</td>\n</tr>\n{{#if HasFinancialBreakdown}}\n<tr>\n<td colspan=""2"" style=""padding: 12px 0 0 0;"">\n<table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""border-top: 1px dashed #d1d5db; margin-top: 8px; padding-top: 12px;"">\n<tr><td style=""font-size: 13px; font-weight: 600; color: #374151; text-transform: uppercase; letter-spacing: 0.5px; padding-bottom: 8px;"">Payment Breakdown</td></tr>\n<tr><td style=""padding: 4px 0; font-size: 14px; color: #6b7280;"">Ticket Price: <strong style=""color: #111827;"">{{BreakdownCurrency}} ${{TicketSubtotal}}</strong></td></tr>\n{{#if HasDonation}}<tr><td style=""padding: 4px 0; font-size: 14px; color: #6b7280;"">Donation: <strong style=""color: #059669;"">{{BreakdownCurrency}} ${{DonationAmount}}</strong></td></tr>{{/if}}\n<tr><td style=""padding: 4px 0; font-size: 14px; color: #6b7280; border-top: 1px solid #e5e7eb; margin-top: 4px; padding-top: 8px;"">Total: <strong style=""color: #111827; font-size: 16px;"">{{AmountPaid}}</strong></td></tr>\n</table>\n</td>\n</tr>\n{{/if}}\n<!-- end-breakdown',
                    'g'
                ),
                text_template = REGEXP_REPLACE(
                    text_template,
                    '(Amount Paid:\s*\{\{AmountPaid\}\})',
                    E'Amount Paid: {{AmountPaid}}\n{{#if HasFinancialBreakdown}}\nPAYMENT BREAKDOWN\n-----------------\nTicket Price: {{BreakdownCurrency}} ${{TicketSubtotal}}\n{{#if HasDonation}}Donation: {{BreakdownCurrency}} ${{DonationAmount}}\n{{/if}}Total: {{AmountPaid}}\n{{/if}}',
                    'g'
                ),
                updated_at = NOW()
                WHERE name = 'template-paid-event-registration-confirmation-with-ticket';
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove the financial breakdown section from HTML template
            migrationBuilder.Sql(@"
                UPDATE communications.email_templates
                SET html_template = REGEXP_REPLACE(
                    html_template,
                    '\{\{#if HasFinancialBreakdown\}\}.*?\{\{/if\}\}\s*<!-- end-breakdown',
                    '',
                    'gs'
                ),
                text_template = REGEXP_REPLACE(
                    text_template,
                    '\n\{\{#if HasFinancialBreakdown\}\}\nPAYMENT BREAKDOWN.*?\{\{/if\}\}',
                    '',
                    'gs'
                ),
                updated_at = NOW()
                WHERE name = 'template-paid-event-registration-confirmation-with-ticket';
            ");
        }
    }
}
