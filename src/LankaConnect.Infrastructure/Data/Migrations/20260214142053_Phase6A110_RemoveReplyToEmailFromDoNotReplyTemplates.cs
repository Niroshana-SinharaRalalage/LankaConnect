using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class Phase6A110_RemoveReplyToEmailFromDoNotReplyTemplates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Phase 6A.110: Fix GitHub Issue #71
            // Remove "reply to this email" text from all email templates
            // since sender is DoNotReply@lankaconnect.app
            // Replace with appropriate support contact information

            migrationBuilder.Sql(@"
                -- Update text_template: Replace 'reply to this email' with support contact
                UPDATE communications.email_templates
                SET text_template = REPLACE(
                    REPLACE(
                        REPLACE(
                            REPLACE(
                                text_template,
                                'Questions? Reply to this email or visit our support page.',
                                'Questions? Contact us at support@lankaconnect.app or visit our support page.'
                            ),
                            'If you have any follow-up questions, please reply to this email.',
                            'If you have any questions, please contact us at support@lankaconnect.app.'
                        ),
                        'please reply to this email',
                        'please contact us at support@lankaconnect.app'
                    ),
                    'Reply to this email',
                    'Contact us at support@lankaconnect.app'
                ),
                updated_at = NOW()
                WHERE text_template LIKE '%reply to this email%'
                   OR text_template LIKE '%Reply to this email%';

                -- Update html_template: Replace 'reply to this email' with support contact
                UPDATE communications.email_templates
                SET html_template = REPLACE(
                    REPLACE(
                        REPLACE(
                            REPLACE(
                                html_template,
                                'Questions? Reply to this email or visit our support page.',
                                'Questions? Contact us at <a href=""mailto:support@lankaconnect.app"" style=""color: #FF7900;"">support@lankaconnect.app</a> or visit our support page.'
                            ),
                            'If you have any follow-up questions, please reply to this email.',
                            'If you have any questions, please contact us at <a href=""mailto:support@lankaconnect.app"" style=""color: #FF7900;"">support@lankaconnect.app</a>.'
                        ),
                        'please reply to this email',
                        'please contact us at <a href=""mailto:support@lankaconnect.app"" style=""color: #FF7900;"">support@lankaconnect.app</a>'
                    ),
                    'Reply to this email',
                    'Contact us at <a href=""mailto:support@lankaconnect.app"" style=""color: #FF7900;"">support@lankaconnect.app</a>'
                ),
                updated_at = NOW()
                WHERE html_template LIKE '%reply to this email%'
                   OR html_template LIKE '%Reply to this email%';
            ");

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("0b9effc0-322f-8026-85c6-747e381b41e6"),
                column: "created_at",
                value: new DateTime(2026, 2, 14, 14, 20, 49, 726, DateTimeKind.Utc).AddTicks(8078));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("2d87836d-9322-d4b1-b4ec-b5b73eca9ad9"),
                column: "created_at",
                value: new DateTime(2026, 2, 14, 14, 20, 49, 726, DateTimeKind.Utc).AddTicks(8160));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("31f73d61-6c12-1252-f5ab-10d9d47eba46"),
                column: "created_at",
                value: new DateTime(2026, 2, 14, 14, 20, 49, 726, DateTimeKind.Utc).AddTicks(7990));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4de1eacb-273a-ab85-e811-d60addb4ae30"),
                column: "created_at",
                value: new DateTime(2026, 2, 14, 14, 20, 49, 726, DateTimeKind.Utc).AddTicks(8121));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4e57a1be-7a76-833e-003f-b2e3182f29f0"),
                column: "created_at",
                value: new DateTime(2026, 2, 14, 14, 20, 49, 726, DateTimeKind.Utc).AddTicks(8140));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("6313b249-2620-3e97-c1bd-f1d50814156d"),
                column: "created_at",
                value: new DateTime(2026, 2, 14, 14, 20, 49, 726, DateTimeKind.Utc).AddTicks(8256));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("70ab7cff-d677-f4bd-b331-f02908ee3347"),
                column: "created_at",
                value: new DateTime(2026, 2, 14, 14, 20, 49, 726, DateTimeKind.Utc).AddTicks(8101));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("80cd50b4-7630-f5d0-1f9a-a7c480347dcf"),
                column: "created_at",
                value: new DateTime(2026, 2, 14, 14, 20, 49, 726, DateTimeKind.Utc).AddTicks(8039));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("9b07d22a-d0bf-ad27-01bf-0c8410d4b9e1"),
                column: "created_at",
                value: new DateTime(2026, 2, 14, 14, 20, 49, 726, DateTimeKind.Utc).AddTicks(8218));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("c5735376-4831-c12b-a01e-672efee6c8e3"),
                column: "created_at",
                value: new DateTime(2026, 2, 14, 14, 20, 49, 726, DateTimeKind.Utc).AddTicks(8199));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("cdaa97c0-e68f-2819-984e-63bb9dcf35a6"),
                column: "created_at",
                value: new DateTime(2026, 2, 14, 14, 20, 49, 726, DateTimeKind.Utc).AddTicks(8181));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("e1d5afac-09d6-ef55-a529-f5bf473ef103"),
                column: "created_at",
                value: new DateTime(2026, 2, 14, 14, 20, 49, 726, DateTimeKind.Utc).AddTicks(8237));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Phase 6A.110: Rollback - restore original "reply to this email" text
            // Note: This is provided for completeness but should not be used
            // since the original text was incorrect (DoNotReply sender)

            migrationBuilder.Sql(@"
                -- Rollback text_template changes
                UPDATE communications.email_templates
                SET text_template = REPLACE(
                    REPLACE(
                        REPLACE(
                            REPLACE(
                                text_template,
                                'Questions? Contact us at support@lankaconnect.app or visit our support page.',
                                'Questions? Reply to this email or visit our support page.'
                            ),
                            'If you have any questions, please contact us at support@lankaconnect.app.',
                            'If you have any follow-up questions, please reply to this email.'
                        ),
                        'please contact us at support@lankaconnect.app',
                        'please reply to this email'
                    ),
                    'Contact us at support@lankaconnect.app',
                    'Reply to this email'
                ),
                updated_at = NOW()
                WHERE text_template LIKE '%contact us at support@lankaconnect.app%'
                   OR text_template LIKE '%Contact us at support@lankaconnect.app%';

                -- Rollback html_template changes
                UPDATE communications.email_templates
                SET html_template = REPLACE(
                    REPLACE(
                        REPLACE(
                            REPLACE(
                                html_template,
                                'Questions? Contact us at <a href=""mailto:support@lankaconnect.app"" style=""color: #FF7900;"">support@lankaconnect.app</a> or visit our support page.',
                                'Questions? Reply to this email or visit our support page.'
                            ),
                            'If you have any questions, please contact us at <a href=""mailto:support@lankaconnect.app"" style=""color: #FF7900;"">support@lankaconnect.app</a>.',
                            'If you have any follow-up questions, please reply to this email.'
                        ),
                        'please contact us at <a href=""mailto:support@lankaconnect.app"" style=""color: #FF7900;"">support@lankaconnect.app</a>',
                        'please reply to this email'
                    ),
                    'Contact us at <a href=""mailto:support@lankaconnect.app"" style=""color: #FF7900;"">support@lankaconnect.app</a>',
                    'Reply to this email'
                ),
                updated_at = NOW()
                WHERE html_template LIKE '%contact us at support@lankaconnect.app%'
                   OR html_template LIKE '%Contact us at support@lankaconnect.app%';
            ");

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("0b9effc0-322f-8026-85c6-747e381b41e6"),
                column: "created_at",
                value: new DateTime(2026, 2, 14, 5, 8, 49, 15, DateTimeKind.Utc).AddTicks(2030));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("2d87836d-9322-d4b1-b4ec-b5b73eca9ad9"),
                column: "created_at",
                value: new DateTime(2026, 2, 14, 5, 8, 49, 15, DateTimeKind.Utc).AddTicks(2187));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("31f73d61-6c12-1252-f5ab-10d9d47eba46"),
                column: "created_at",
                value: new DateTime(2026, 2, 14, 5, 8, 49, 15, DateTimeKind.Utc).AddTicks(1939));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4de1eacb-273a-ab85-e811-d60addb4ae30"),
                column: "created_at",
                value: new DateTime(2026, 2, 14, 5, 8, 49, 15, DateTimeKind.Utc).AddTicks(2119));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4e57a1be-7a76-833e-003f-b2e3182f29f0"),
                column: "created_at",
                value: new DateTime(2026, 2, 14, 5, 8, 49, 15, DateTimeKind.Utc).AddTicks(2152));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("6313b249-2620-3e97-c1bd-f1d50814156d"),
                column: "created_at",
                value: new DateTime(2026, 2, 14, 5, 8, 49, 15, DateTimeKind.Utc).AddTicks(2308));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("70ab7cff-d677-f4bd-b331-f02908ee3347"),
                column: "created_at",
                value: new DateTime(2026, 2, 14, 5, 8, 49, 15, DateTimeKind.Utc).AddTicks(2087));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("80cd50b4-7630-f5d0-1f9a-a7c480347dcf"),
                column: "created_at",
                value: new DateTime(2026, 2, 14, 5, 8, 49, 15, DateTimeKind.Utc).AddTicks(1996));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("9b07d22a-d0bf-ad27-01bf-0c8410d4b9e1"),
                column: "created_at",
                value: new DateTime(2026, 2, 14, 5, 8, 49, 15, DateTimeKind.Utc).AddTicks(2266));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("c5735376-4831-c12b-a01e-672efee6c8e3"),
                column: "created_at",
                value: new DateTime(2026, 2, 14, 5, 8, 49, 15, DateTimeKind.Utc).AddTicks(2227));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("cdaa97c0-e68f-2819-984e-63bb9dcf35a6"),
                column: "created_at",
                value: new DateTime(2026, 2, 14, 5, 8, 49, 15, DateTimeKind.Utc).AddTicks(2207));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("e1d5afac-09d6-ef55-a529-f5bf473ef103"),
                column: "created_at",
                value: new DateTime(2026, 2, 14, 5, 8, 49, 15, DateTimeKind.Utc).AddTicks(2291));
        }
    }
}
