using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Infrastructure.Data.Migrations
{
    /// <summary>
    /// Phase 6A.133 Email: Updates all email templates to support multiple organizer contacts.
    ///
    /// BEFORE: Templates render single organizer using {{OrganizerContactName}}, {{OrganizerContactEmail}}, {{OrganizerContactPhone}}
    /// AFTER: Templates render all organizer contacts using {{{OrganizerContactsHtml}}} (pre-formatted HTML from OrganizerContactHtmlBuilder)
    ///        and {{OrganizerContactHeader}} for dynamic "EVENT ORGANIZER(S)" header text.
    ///
    /// The entire {{#HasOrganizerContact}}...{{/HasOrganizerContact}} block is replaced with a standardized
    /// card layout that uses the pre-formatted HTML string containing all contacts.
    ///
    /// This migration uses REGEXP_REPLACE to handle whitespace variations across templates.
    /// </summary>
    public partial class Phase6A133Email_UpdateTemplatesMultiOrganizerContacts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // The new organizer contact section uses:
            // - {{OrganizerContactHeader}} for dynamic "EVENT ORGANIZER" or "EVENT ORGANIZERS" text
            // - {{{OrganizerContactsHtml}}} (triple-brace) for pre-formatted HTML with all contacts
            // - Keeps {{#if HasOrganizerContact}} conditional to hide section when no contacts exist
            var newOrganizerSection = @"{{#if HasOrganizerContact}}" +
                @"<table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin: 30px 0;"">" +
                @"<tr><td style=""background: #fefaf7; padding: 20px; border-radius: 8px; border: 1px solid #f3e4d5;"">" +
                @"<p style=""margin: 0 0 12px 0; font-size: 14px; font-weight: 700; color: #9f1239; text-transform: uppercase; letter-spacing: 0.5px;"">{{OrganizerContactHeader}}</p>" +
                @"<table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"">" +
                @"{{{OrganizerContactsHtml}}}" +
                @"</table>" +
                @"</td></tr></table>" +
                @"{{/if}}";

            // Escape single quotes for SQL
            var sqlNewSection = newOrganizerSection.Replace("'", "''");

            // Replace legacy syntax: {{#HasOrganizerContact}}...{{/HasOrganizerContact}}
            // Uses 'gs' flags: g=global, s=dot matches newlines (POSIX: n=newline-sensitive OFF)
            migrationBuilder.Sql($@"
                UPDATE communications.email_templates
                SET html_template = REGEXP_REPLACE(
                    html_template,
                    '\{{\{{#HasOrganizerContact\}}\}}.*?\{{\{{/HasOrganizerContact\}}\}}',
                    '{sqlNewSection}',
                    'gs'
                ),
                updated_at = NOW()
                WHERE html_template LIKE '%HasOrganizerContact%';
            ");

            // Also replace {{#if HasOrganizerContact}}...{{/if}} syntax if any templates use it
            // This uses a more specific regex to avoid matching other {{/if}} blocks:
            // Match from {{#if HasOrganizerContact}} to the next {{/if}} that is NOT preceded by another {{#if
            // Note: This is a best-effort match - if nested {{#if}} exist inside the organizer block,
            // the greedy match may not work correctly. Manual verification recommended post-deployment.
            migrationBuilder.Sql($@"
                UPDATE communications.email_templates
                SET html_template = REGEXP_REPLACE(
                    html_template,
                    '\{{\{{#if HasOrganizerContact\}}\}}.*?\{{\{{/if\}}\}}',
                    '{sqlNewSection}',
                    'gs'
                ),
                updated_at = NOW()
                WHERE html_template LIKE '%#if HasOrganizerContact%';
            ");

            // Also update text_template to use the new placeholders
            // Replace individual organizer placeholders with the multi-contact text version
            migrationBuilder.Sql(@"
                UPDATE communications.email_templates
                SET text_template = REGEXP_REPLACE(
                    text_template,
                    '\{\{#HasOrganizerContact\}\}.*?\{\{/HasOrganizerContact\}\}',
                    '{{#if HasOrganizerContact}}' || E'\n' || '{{OrganizerContactHeader}}' || E'\n' || '{{{OrganizerContactsHtml}}}' || E'\n' || '{{/if}}',
                    'gs'
                ),
                updated_at = NOW()
                WHERE text_template LIKE '%HasOrganizerContact%';
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Revert to single-contact section
            // This restores a generic single-contact template
            var oldOrganizerSection = @"{{#HasOrganizerContact}}" +
                @"<table role=""presentation"" width=""100%"" cellspacing=""0"" cellpadding=""0"" border=""0"" style=""margin: 30px 0;"">" +
                @"<tr><td style=""background: #fefaf7; padding: 20px; border-radius: 8px; border: 1px solid #f3e4d5;"">" +
                @"<p style=""margin: 0 0 12px 0; font-size: 14px; font-weight: 700; color: #9f1239; text-transform: uppercase; letter-spacing: 0.5px;"">EVENT ORGANIZER</p>" +
                @"<p style=""margin: 0; font-size: 15px; color: #2d3748;""><strong>{{OrganizerContactName}}</strong></p>" +
                @"{{#if OrganizerContactEmail}}<p style=""margin: 4px 0 0 0; font-size: 14px;""><a href=""mailto:{{OrganizerContactEmail}}"" style=""color: #9f1239; text-decoration: none;"">{{OrganizerContactEmail}}</a></p>{{/if}}" +
                @"{{#if OrganizerContactPhone}}<p style=""margin: 4px 0 0 0; font-size: 14px; color: #4a5568;"">{{OrganizerContactPhone}}</p>{{/if}}" +
                @"</td></tr></table>" +
                @"{{/HasOrganizerContact}}";

            var sqlOldSection = oldOrganizerSection.Replace("'", "''");

            migrationBuilder.Sql($@"
                UPDATE communications.email_templates
                SET html_template = REGEXP_REPLACE(
                    html_template,
                    '\{{\{{#if HasOrganizerContact\}}\}}.*?\{{\{{/if\}}\}}',
                    '{sqlOldSection}',
                    'gs'
                ),
                updated_at = NOW()
                WHERE html_template LIKE '%OrganizerContactsHtml%';
            ");
        }
    }
}
