using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    /// <summary>
    /// Phase 6A.75: Update Newsletter Email Template with properly styled buttons for event links.
    /// This fixes the issue where event details and sign-up list links were not appearing.
    /// Uses table-based button styling for maximum email client compatibility.
    /// </summary>
    public partial class Phase6A75_UpdateNewsletterTemplateWithStyledButtons : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Update newsletter email template with improved button styling
            migrationBuilder.Sql(@"
                UPDATE communications.email_templates
                SET html_template = '<!DOCTYPE html>
<html lang=""en"">
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
<body style=""margin: 0; padding: 0; font-family: Arial, Helvetica, sans-serif; background-color: #f4f4f4;"">
  <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""background-color: #f4f4f4;"">
    <tr>
      <td align=""center"" style=""padding: 40px 20px;"">
        <table role=""presentation"" width=""600"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""max-width: 600px; background: #ffffff; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 6px rgba(0,0,0,0.1);"">

          <!-- Header with Sri Lankan gradient -->
          <tr>
            <td style=""background: linear-gradient(135deg, #8B1538 0%, #FF6600 50%, #2d5016 100%); padding: 30px 20px; text-align: center;"">
              <h1 style=""color: #ffffff; margin: 0; font-size: 28px; font-weight: bold;"">
                LankaConnect Newsletter
              </h1>
            </td>
          </tr>

          <!-- Content -->
          <tr>
            <td style=""padding: 40px 30px; background: #ffffff;"">
              <h2 style=""color: #1F2937; margin-top: 0; margin-bottom: 16px; font-size: 24px;"">
                {{NewsletterTitle}}
              </h2>

              <!-- Newsletter Content (HTML or Plain Text) -->
              <div class=""newsletter-content"" style=""color: #4B5563; line-height: 1.6; margin-bottom: 24px;"">
                {{{NewsletterContent}}}
              </div>

              <!-- Event Details Section (Conditional) -->
              {{#if EventId}}
              <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin: 25px 0; background: #fff8f5; border-left: 4px solid #FF6600; border-radius: 0 8px 8px 0;"">
                <tr>
                  <td style=""padding: 20px;"">
                    <h3 style=""margin: 0 0 15px 0; color: #8B1538; font-size: 18px;"">
                      📅 Related Event
                    </h3>
                    <p style=""margin: 0 0 8px 0; color: #1F2937; font-weight: 600; font-size: 16px;"">
                      {{EventTitle}}
                    </p>
                    {{#if EventLocation}}
                    <p style=""margin: 0 0 4px 0; color: #6B7280; font-size: 14px;"">
                      📍 {{EventLocation}}
                    </p>
                    {{/if}}
                    <p style=""margin: 0 0 20px 0; color: #6B7280; font-size: 14px;"">
                      🕒 {{EventDate}}
                    </p>

                    <!-- Event Action Buttons -->
                    <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"">
                      <tr>
                        <td align=""center"" style=""padding-bottom: 10px;"">
                          <a href=""{{EventDetailsUrl}}"" style=""display: inline-block; padding: 14px 32px; background: linear-gradient(to right, #ea580c 0%, #9f1239 50%, #166534 100%); color: #ffffff; text-decoration: none; font-weight: bold; border-radius: 6px; font-size: 16px; box-shadow: 0 2px 4px rgba(0,0,0,0.2);"">
                            View Event Details
                          </a>
                        </td>
                      </tr>
                      {{#if HasSignUpLists}}
                      <tr>
                        <td align=""center"">
                          <a href=""{{SignUpListsUrl}}"" style=""display: inline-block; padding: 12px 28px; background: linear-gradient(to right, #8B1538 0%, #6B1028 100%); color: #ffffff; text-decoration: none; font-weight: bold; border-radius: 6px; font-size: 14px; box-shadow: 0 2px 4px rgba(0,0,0,0.2);"">
                            View Sign-up Lists
                          </a>
                        </td>
                      </tr>
                      {{/if}}
                    </table>
                  </td>
                </tr>
              </table>
              {{/if}}

              <!-- CTA -->
              <table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin-top: 32px;"">
                <tr>
                  <td align=""center"">
                    <a href=""{{DashboardUrl}}"" style=""display: inline-block; color: #FF7900; text-decoration: none; font-weight: 600; font-size: 16px;"">
                      Visit LankaConnect →
                    </a>
                  </td>
                </tr>
              </table>
            </td>
          </tr>

          <!-- Footer -->
          <tr>
            <td style=""background: linear-gradient(135deg, #8B1538 0%, #FF6600 50%, #2d5016 100%); padding: 30px 20px; text-align: center;"">
              <p style=""color: white; font-size: 20px; font-weight: bold; margin: 0 0 5px 0;"">LankaConnect</p>
              <p style=""color: rgba(255,255,255,0.9); font-size: 14px; margin: 0 0 10px 0;"">Sri Lankan Community Hub</p>
              <p style=""color: rgba(255,255,255,0.8); font-size: 12px; margin: 0;"">You''''re receiving this because you''''re subscribed to LankaConnect newsletters.</p>
              {{#if UnsubscribeUrl}}
              <p style=""color: rgba(255,255,255,0.7); margin: 8px 0 0 0; font-size: 12px;"">
                <a href=""{{UnsubscribeUrl}}"" style=""color: rgba(255,255,255,0.7); text-decoration: underline;"">Unsubscribe</a>
              </p>
              {{/if}}
            </td>
          </tr>

        </table>
      </td>
    </tr>
  </table>
</body>
</html>',
                    text_template = 'LankaConnect Newsletter

{{NewsletterTitle}}

{{NewsletterContent}}

{{#if EventId}}
----------------------------------------
📅 Related Event
----------------------------------------
{{EventTitle}}
{{#if EventLocation}}
📍 Location: {{EventLocation}}
{{/if}}
🕒 Date: {{EventDate}}

View Event Details: {{EventDetailsUrl}}
{{#if HasSignUpLists}}
View Sign-up Lists: {{SignUpListsUrl}}
{{/if}}
----------------------------------------
{{/if}}

Visit LankaConnect: {{DashboardUrl}}

You''''re receiving this because you''''re subscribed to LankaConnect newsletters.
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
            // Revert to previous template (from Phase 6A.74 Part 5A)
            migrationBuilder.Sql(@"
                UPDATE communications.email_templates
                SET html_template = '<!DOCTYPE html>
<html>
<head>
  <meta charset=""UTF-8"">
  <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
  <title>{{NewsletterTitle}}</title>
  <style>
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
<body style=""margin: 0; padding: 0; font-family: -apple-system, BlinkMacSystemFont, ''''Segoe UI'''', sans-serif;"">
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
      <div class=""newsletter-content"" style=""color: #4B5563; line-height: 1.6; margin-bottom: 24px;"">
        {{{NewsletterContent}}}
      </div>
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
      <div style=""text-align: center; margin-top: 32px;"">
        <a href=""{{DashboardUrl}}"" style=""display: inline-block; color: #FF7900; text-decoration: none; font-weight: 600; font-size: 16px;"">
          Visit LankaConnect →
        </a>
      </div>
    </div>
    <div style=""background-color: #F9FAFB; padding: 20px; text-align: center; border-top: 1px solid #E5E7EB;"">
      <p style=""color: #6B7280; margin: 0; font-size: 14px;"">
        You''''re receiving this because you''''re subscribed to LankaConnect newsletters.
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

You''''re receiving this because you''''re subscribed to LankaConnect newsletters.
{{#if UnsubscribeUrl}}
Unsubscribe: {{UnsubscribeUrl}}
{{/if}}',
                    updated_at = NOW()
                WHERE name = 'newsletter' AND type = 'Newsletter';
            ");
        }
    }
}
