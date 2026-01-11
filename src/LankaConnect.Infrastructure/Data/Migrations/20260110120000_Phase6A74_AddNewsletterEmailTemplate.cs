using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class Phase6A74_AddNewsletterEmailTemplate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Phase 6A.74: Seed newsletter email template into communications.email_templates

            migrationBuilder.Sql(@"
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
                VALUES
                (
                    gen_random_uuid(),
                    'newsletter',
                    'Newsletter/News Alert template for broadcasting announcements to email groups and subscribers',
                    'LankaConnect Newsletter: {{NewsletterTitle}}',
                    'LankaConnect Newsletter

{{NewsletterTitle}}

{{NewsletterContent}}

Visit LankaConnect: {{DashboardUrl}}

You''re receiving this because you''re subscribed to LankaConnect newsletters.',
                    '<!DOCTYPE html>
<html>
<head>
  <meta charset=""UTF-8"">
  <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
  <title>{{NewsletterTitle}}</title>
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

      <div style=""color: #4B5563; line-height: 1.6; margin-bottom: 24px; white-space: pre-wrap;"">
        {{NewsletterContent}}
      </div>

      <!-- CTA -->
      <div style=""text-align: center; margin-top: 32px;"">
        <a href=""{{DashboardUrl}}"" style=""display: inline-block; color: #FF7900; text-decoration: none; font-weight: 600;"">
          Visit LankaConnect →
        </a>
      </div>
    </div>

    <!-- Footer -->
    <div style=""background-color: #F9FAFB; padding: 20px; text-align: center; border-top: 1px solid #E5E7EB;"">
      <p style=""color: #6B7280; margin: 0; font-size: 14px;"">
        You''re receiving this because you''re subscribed to LankaConnect newsletters.
      </p>
    </div>
  </div>
</body>
</html>',
                    'Newsletter',
                    'Transactional',
                    true,
                    NOW()
                );
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DELETE FROM communications.email_templates
                WHERE ""name"" = 'newsletter'
                AND ""type"" = 'Newsletter';
            ");
        }
    }
}
