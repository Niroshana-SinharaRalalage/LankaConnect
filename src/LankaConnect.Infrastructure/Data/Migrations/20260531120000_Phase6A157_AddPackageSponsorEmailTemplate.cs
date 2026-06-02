using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Infrastructure.Data.Migrations
{
    /// <summary>
    /// Phase 6A.157 — seed the package-sponsor confirmation email template row
    /// into <c>communications.email_templates</c>. Mirrors
    /// <c>Phase6A137B_AddReceiptEmailTemplates</c> pattern exactly: pure SQL
    /// INSERT inside an idempotent NOT-EXISTS guard so re-running the
    /// migration on an already-seeded database is a no-op.
    ///
    /// The template's voice is package-specific (per architect lock — different
    /// content than the existing generic sponsor confirmation), with Handlebars
    /// placeholders for: SponsorName, PackageNameSnapshot, PackageTierSnapshot
    /// + HasTier conditional, AmountPaid, Currency, PaymentDate,
    /// IncludedTicketCount + HasIncludedTickets conditional, PerksHtml +
    /// HasPerks conditional, EventTitle, EventDetailsUrl, SupportEmail, Year.
    ///
    /// Per user pivot 2026-05-31, the included-tickets text says the organizer
    /// will arrange admission off-platform — system does NOT issue tickets.
    /// </summary>
    /// <inheritdoc />
    public partial class Phase6A157_AddPackageSponsorEmailTemplate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // HTML template — uses Handlebars syntax ({{Var}}, {{#if HasX}}…{{/if}}).
            // Embedded as a single-quoted SQL literal — single quotes inside
            // are escaped as ''.
            var htmlTemplate = @"
<!DOCTYPE html>
<html>
<head>
  <meta charset=""utf-8"">
  <title>Sponsorship Confirmation - {{EventTitle}}</title>
</head>
<body style=""font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 24px; color: #1f2937; background: #f9fafb;"">
  <div style=""background: #ffffff; border-radius: 8px; padding: 32px; box-shadow: 0 1px 3px rgba(0,0,0,0.05);"">
    <h2 style=""color: #b45309; margin-top: 0;"">Welcome, {{PackageNameSnapshot}} sponsor!</h2>
    <p style=""color: #374151; line-height: 1.5;"">
      Hi {{SponsorName}},
    </p>
    <p style=""color: #374151; line-height: 1.5;"">
      Thank you for sponsoring <strong>{{EventTitle}}</strong>. Your support as a
      {{#if HasTier}}<strong>{{PackageTierSnapshot}}</strong>-tier {{/if}}sponsor
      makes this event possible.
    </p>

    <div style=""background: #fef3c7; border-left: 4px solid #f59e0b; padding: 16px; margin: 20px 0; border-radius: 4px;"">
      <h3 style=""margin: 0 0 12px 0; color: #92400e;"">Your Sponsorship</h3>
      <table style=""width: 100%; border-collapse: collapse;"">
        <tr>
          <td style=""padding: 4px 0; color: #6b7280;"">Package:</td>
          <td style=""padding: 4px 0; color: #1f2937; font-weight: 600;"">{{PackageNameSnapshot}}{{#if HasTier}} ({{PackageTierSnapshot}}){{/if}}</td>
        </tr>
        <tr>
          <td style=""padding: 4px 0; color: #6b7280;"">Amount Paid:</td>
          <td style=""padding: 4px 0; color: #1f2937; font-weight: 600;"">{{Currency}} {{AmountPaid}}</td>
        </tr>
        <tr>
          <td style=""padding: 4px 0; color: #6b7280;"">Date:</td>
          <td style=""padding: 4px 0; color: #1f2937;"">{{PaymentDate}}</td>
        </tr>
      </table>
    </div>

    {{#if HasPerks}}
    <div style=""margin: 20px 0;"">
      <h3 style=""color: #1f2937; margin-bottom: 8px;"">What's included</h3>
      {{{PerksHtml}}}
    </div>
    {{/if}}

    {{#if HasIncludedTickets}}
    <div style=""background: #f3f4f6; border: 1px solid #d1d5db; padding: 16px; margin: 20px 0; border-radius: 4px;"">
      <p style=""margin: 0; color: #4b5563;"">
        <strong>About your included tickets:</strong>
        Your package includes <strong>{{IncludedTicketCount}}</strong> adult ticket(s).
        The event organizer will coordinate your admission directly &mdash; please watch
        for a follow-up message from them. The platform does not issue tickets for
        sponsorship packages.
      </p>
    </div>
    {{/if}}

    <p style=""color: #374151; line-height: 1.5;"">
      <a href=""{{EventDetailsUrl}}"" style=""display: inline-block; background: #b45309; color: #ffffff; padding: 10px 20px; text-decoration: none; border-radius: 4px; margin-top: 8px;"">View Event Details</a>
    </p>

    <hr style=""border: none; border-top: 1px solid #e5e7eb; margin: 24px 0;"">
    <p style=""color: #6b7280; font-size: 13px; line-height: 1.5;"">
      Payment reference: {{PaymentIntentId}}<br>
      Questions? Email <a href=""mailto:{{SupportEmail}}"" style=""color: #b45309;"">{{SupportEmail}}</a>
    </p>
    <p style=""color: #9ca3af; font-size: 12px;"">
      &copy; {{Year}} LankaConnect. All rights reserved.
    </p>
  </div>
</body>
</html>";

            var escapedHtml = htmlTemplate.Replace("'", "''");

            migrationBuilder.Sql($@"
                INSERT INTO communications.email_templates
                (
                    ""Id"",
                    ""name"",
                    ""description"",
                    ""subject_template"",
                    ""text_template"",
                    ""html_template"",
                    ""type"",
                    ""category"",
                    ""is_active"",
                    ""created_at""
                )
                SELECT
                    gen_random_uuid(),
                    'template-package-sponsor-confirmation',
                    'Phase 6A.157: Packaged-sponsorship confirmation email. Forked from template-sponsor-confirmation — package-specific voice + perks list + informational included-tickets paragraph per user pivot 2026-05-31.',
                    'Sponsorship Confirmation - {{{{EventTitle}}}}',
                    'Hi {{{{SponsorName}}}},

Welcome as a {{{{PackageNameSnapshot}}}} sponsor of {{{{EventTitle}}}}!

YOUR SPONSORSHIP
----------------
Package: {{{{PackageNameSnapshot}}}}
Amount Paid: {{{{Currency}}}} {{{{AmountPaid}}}}
Date: {{{{PaymentDate}}}}

{{{{#if HasIncludedTickets}}}}
About your included tickets:
Your package includes {{{{IncludedTicketCount}}}} adult ticket(s). The event organizer will coordinate your admission directly — please watch for a follow-up message from them. The platform does not issue tickets for sponsorship packages.

{{{{/if}}}}View event details: {{{{EventDetailsUrl}}}}

Payment reference: {{{{PaymentIntentId}}}}
Questions? Email {{{{SupportEmail}}}}

© {{{{Year}}}} LankaConnect. All rights reserved.',
                    '{escapedHtml}',
                    'SponsorConfirmation',
                    'Notification',
                    true,
                    NOW()
                WHERE NOT EXISTS (
                    SELECT 1 FROM communications.email_templates
                    WHERE name = 'template-package-sponsor-confirmation'
                );
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DELETE FROM communications.email_templates
                WHERE name = 'template-package-sponsor-confirmation';
            ");
        }
    }
}
