using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class Phase6A103_AddEventImageToEmailTemplates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Phase 6A.103: Add event image HTML block to 8 email templates
            // This SQL injects the {{#HasEventImage}} conditional block before the <!-- BODY CONTENT anchor
            // Pattern matches the working registration confirmation templates with onerror graceful fallback
            migrationBuilder.Sql(@"
                UPDATE communications.email_templates
                SET html_template = replace(
                    html_template,
                    '<!-- BODY CONTENT',
                    '<!-- EVENT IMAGE (conditional + graceful fallback) -->
                        {{#HasEventImage}}
                        <!--[if !mso]><!-->
                        <tr>
                            <td style=""font-size: 0; line-height: 0"">
                                <!--<![endif]-->
                                <img
                                    src=""{{EventImageUrl}}""
                                    alt=""{{EventTitle}}""
                                    width=""860""
                                    style=""width: 100%; max-height: 300px; object-fit: cover; display: block""
                                    onerror=""
                                        this.style.display = ''none'';
                                        this.parentElement.style.height = ''0'';
                                        this.parentElement.style.overflow = ''hidden'';
                                    ""
                                />
                                <!--[if !mso]><!-->
                            </td>
                        </tr>
                        <!--<![endif]-->
                        {{/HasEventImage}}

                        <!-- BODY CONTENT'),
                    updated_at = NOW()
                WHERE name IN (
                    'template-event-details-publication',
                    'template-new-event-publication',
                    'template-event-reminder',
                    'template-event-cancellation-notifications',
                    'template-event-approval',
                    'template-signup-list-commitment-cancellation',
                    'template-signup-list-commitment-confirmation',
                    'template-signup-list-commitment-update'
                )
                AND html_template NOT LIKE '%HasEventImage%';
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Phase 6A.103: Remove event image HTML block from 8 email templates
            migrationBuilder.Sql(@"
                UPDATE communications.email_templates
                SET html_template = replace(
                    html_template,
                    '<!-- EVENT IMAGE (conditional + graceful fallback) -->
                        {{#HasEventImage}}
                        <!--[if !mso]><!-->
                        <tr>
                            <td style=""font-size: 0; line-height: 0"">
                                <!--<![endif]-->
                                <img
                                    src=""{{EventImageUrl}}""
                                    alt=""{{EventTitle}}""
                                    width=""860""
                                    style=""width: 100%; max-height: 300px; object-fit: cover; display: block""
                                    onerror=""
                                        this.style.display = ''none'';
                                        this.parentElement.style.height = ''0'';
                                        this.parentElement.style.overflow = ''hidden'';
                                    ""
                                />
                                <!--[if !mso]><!-->
                            </td>
                        </tr>
                        <!--<![endif]-->
                        {{/HasEventImage}}

                        ',
                    ''),
                    updated_at = NOW()
                WHERE name IN (
                    'template-event-details-publication',
                    'template-new-event-publication',
                    'template-event-reminder',
                    'template-event-cancellation-notifications',
                    'template-event-approval',
                    'template-signup-list-commitment-cancellation',
                    'template-signup-list-commitment-confirmation',
                    'template-signup-list-commitment-update'
                )
                AND html_template LIKE '%HasEventImage%';
            ");
        }
    }
}
