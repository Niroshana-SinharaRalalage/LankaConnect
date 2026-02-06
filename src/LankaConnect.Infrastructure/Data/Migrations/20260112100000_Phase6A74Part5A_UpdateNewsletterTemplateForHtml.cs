using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    /// Phase 6A.74 Part 5A & 5C: Update Newsletter Email Template for HTML Content and Event Links
    /// Changes:
    /// 1. Remove white-space: pre-wrap to allow HTML rendering
    /// 2. Add event details section (conditional)
    /// 3. Add event links (details + sign-up lists)
    /// 4. Support both plain text and HTML content
    public partial class Phase6A74Part5A_UpdateNewsletterTemplateForHtml : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Update newsletter email template to support HTML content and event links
            migrationBuilder.Sql(@"
                UPDATE communications.email_templates
                SET html_template = '<!DOCTYPE html>
<html>
<head>
  <meta charset=""UTF-8"">
  <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
  <title>{{NewsletterTitle}}</title>
  <style>
    /* Base styles for rich text content */
    .newsletter-content h1 {
      font-size: 2em;
      font-weight: 700;
      margin: 0.67em 0;
      color: #8B1538;
    }
    .newsletter-content h2 {
      font-size: 1.5em;
      font-weight: 600;
      margin: 0.83em 0;
      color: #8B1538;
    }
    .newsletter-content h3 {
      font-size: 1.17em;
      font-weight: 600;
      margin: 1em 0;
      color: #8B1538;
    }
    .newsletter-content ul,
    .newsletter-content ol {
      padding-left: 1.5em;
      margin: 1em 0;
    }
    .newsletter-content img {
      max-width: 100%;
      height: auto;
      border-radius: 8px;
      margin: 1em 0;
    }
    .newsletter-content a {
      color: #FF7900;
      text-decoration: underline;
    }
    .newsletter-content a:hover {
      color: #E66D00;
    }
    .newsletter-content p {
      margin: 1em 0;
    }
  </style>
</head>
<body style=""margin: 0; padding: 0; font-family: -apple-system, BlinkMacSystemFont, ''Segoe UI'', sans-serif;"">
  <div style=""max-width: 600px; margin: 0 auto; background-color: #ffffff;"">
    <!-- Header with Sri Lankan gradient -->
    <div style=""background: linear-gradient(135deg, #FF7900 0%, #8B1538 100%); padding: 40px 20px; text-align: center;"">
      <h1 style=""color: #ffffff; margin: 0; font-size: 28px; font-weight: bold;"">
        LankaConnect Newsletter
      </h1>
    </div>

    <!-- Content -->
    <div style=""padding: 40px 20px;"">
      <h2 style=""color: #1F2937; margin-top: 0; margin-bottom: 16px; font-size: 24px;"">
        {{NewsletterTitle}}
      </h2>

      <!-- Newsletter Content (HTML or Plain Text) -->
      <div class=""newsletter-content"" style=""color: #4B5563; line-height: 1.6; margin-bottom: 24px;"">
        {{{NewsletterContent}}}
      </div>

      <!-- Event Details Section (Conditional - Part 5C) -->
      {{#if EventId}}
      <div style=""background-color: #F3F4F6; border-radius: 8px; padding: 20px; margin: 24px 0; border-left: 4px solid #FF7900;"">
        <h3 style=""color: #1F2937; margin-top: 0; margin-bottom: 12px; font-size: 18px;"">
          📅 Related Event
        </h3>
        <p style=""color: #4B5563; margin: 0 0 8px 0; font-weight: 600; font-size: 16px;"">
          {{EventTitle}}
        </p>
        {{#if EventLocation}}
        <p style=""color: #6B7280; margin: 0 0 4px 0; font-size: 14px;"">
          📍 {{EventLocation}}
        </p>
        {{/if}}
        <p style=""color: #6B7280; margin: 0 0 16px 0; font-size: 14px;"">
          🕒 {{EventDate}}
        </p>

        <!-- Event Action Buttons -->
        <div style=""margin-top: 16px;"">
          <a href=""{{EventDetailsUrl}}"" style=""display: inline-block; background: linear-gradient(135deg, #FF7900 0%, #E66D00 100%); color: #ffffff; text-decoration: none; padding: 12px 24px; border-radius: 6px; font-weight: 600; margin-right: 8px; margin-bottom: 8px;"">
            View Event Details
          </a>
          {{#if HasSignUpLists}}
          <a href=""{{SignUpListsUrl}}"" style=""display: inline-block; background: linear-gradient(135deg, #8B1538 0%, #6B1028 100%); color: #ffffff; text-decoration: none; padding: 12px 24px; border-radius: 6px; font-weight: 600; margin-bottom: 8px;"">
            View Sign-up Lists
          </a>
          {{/if}}
        </div>
      </div>
      {{/if}}

      <!-- CTA -->
      <div style=""text-align: center; margin-top: 32px;"">
        <a href=""{{DashboardUrl}}"" style=""display: inline-block; color: #FF7900; text-decoration: none; font-weight: 600; font-size: 16px;"">
          Visit LankaConnect →
        </a>
      </div>
    </div>

    <!-- Footer -->
    <div style=""background-color: #F9FAFB; padding: 20px; text-align: center; border-top: 1px solid #E5E7EB;"">
      <p style=""color: #6B7280; margin: 0; font-size: 14px;"">
        You''re receiving this because you''re subscribed to LankaConnect newsletters.
      </p>
      {{#if UnsubscribeUrl}}
      <p style=""color: #9CA3AF; margin: 8px 0 0 0; font-size: 12px;"">
        <a href=""{{UnsubscribeUrl}}"" style=""color: #9CA3AF; text-decoration: underline;"">Unsubscribe</a>
      </p>
      {{/if}}
    </div>
  </div>
</body>
</html>',
                    text_template = 'LankaConnect Newsletter

{{NewsletterTitle}}

{{NewsletterContent}}

{{#if EventId}}
Related Event:
{{EventTitle}}
{{#if EventLocation}}
Location: {{EventLocation}}
{{/if}}
Date: {{EventDate}}

View Event Details: {{EventDetailsUrl}}
{{#if HasSignUpLists}}
View Sign-up Lists: {{SignUpListsUrl}}
{{/if}}
{{/if}}

Visit LankaConnect: {{DashboardUrl}}

You''re receiving this because you''re subscribed to LankaConnect newsletters.
{{#if UnsubscribeUrl}}
Unsubscribe: {{UnsubscribeUrl}}
{{/if}}',
                    updated_at = NOW()
                WHERE name = 'newsletter' AND type = 'Newsletter';
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Revert to original template
            migrationBuilder.Sql(@"
                UPDATE communications.email_templates
                SET html_template = '<!DOCTYPE html>
<html>
<head>
  <meta charset=""UTF-8"">
  <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
  <title>{{NewsletterTitle}}</title>
</head>
<body style=""margin: 0; padding: 0; font-family: -apple-system, BlinkMacSystemFont, ''Segoe UI'', sans-serif;"">
  <div style=""max-width: 600px; margin: 0 auto; background-color: #ffffff;"">
    <div style=""background: linear-gradient(135deg, #FF7900 0%, #8B1538 100%); padding: 40px 20px; text-align: center;"">
      <h1 style=""color: #ffffff; margin: 0; font-size: 28px; font-weight: bold;"">
        LankaConnect Newsletter
      </h1>
    </div>
    <div style=""padding: 40px 20px;"">
      <h2 style=""color: #1F2937; margin-top: 0; margin-bottom: 16px; font-size: 24px;"">
        {{NewsletterTitle}}
      </h2>
      <div style=""color: #4B5563; line-height: 1.6; margin-bottom: 24px; white-space: pre-wrap;"">
        {{NewsletterContent}}
      </div>
      <div style=""text-align: center; margin-top: 32px;"">
        <a href=""{{DashboardUrl}}"" style=""display: inline-block; color: #FF7900; text-decoration: none; font-weight: 600;"">
          Visit LankaConnect →
        </a>
      </div>
    </div>
    <div style=""background-color: #F9FAFB; padding: 20px; text-align: center; border-top: 1px solid #E5E7EB;"">
      <p style=""color: #6B7280; margin: 0; font-size: 14px;"">
        You''re receiving this because you''re subscribed to LankaConnect newsletters.
      </p>
    </div>
  </div>
</body>
</html>',
                    text_template = 'LankaConnect Newsletter

{{NewsletterTitle}}

{{NewsletterContent}}

Visit LankaConnect: {{DashboardUrl}}

You''re receiving this because you''re subscribed to LankaConnect newsletters.',
                    updated_at = NOW()
                WHERE name = 'newsletter' AND type = 'Newsletter';
            ");
        }
    }
}
