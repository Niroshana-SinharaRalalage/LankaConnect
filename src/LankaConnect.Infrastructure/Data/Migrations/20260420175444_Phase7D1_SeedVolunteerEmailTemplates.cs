using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.SPLIT_PER_ENTITY.Migrations
{
    /// <inheritdoc />
    /// <summary>
    /// Phase 7D.1 Phase C: Seed volunteer commitment email templates.
    /// Clones html/text/subject from existing signup-commitment templates and relabels
    /// visible "Sign-Up"/"Signed up"/etc wording to volunteer equivalents via targeted
    /// REGEXP_REPLACE (MEMORY 6A.117 — avoid literal REPLACE on multi-line strings).
    /// Handlebars placeholders ({{ListName}}, {{CommitmentItem}}, ...) are preserved
    /// so the existing SignupCommitmentEmailParams contract is reused.
    /// NO File.ReadAllText (MEMORY 6A.129b).
    /// </summary>
    public partial class Phase7D1_SeedVolunteerEmailTemplates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Phase 7D.1 Phase C: Seed volunteer commitment confirmation template
            // Cloned from template-signup-list-commitment-confirmation; wording relabeled.
            migrationBuilder.Sql(@"
                INSERT INTO communications.email_templates
                    (""Id"", name, description, subject_template, text_template, html_template,
                     type, category, is_active, tags, created_at)
                SELECT
                    gen_random_uuid(),
                    'template-volunteer-commitment-confirmation',
                    'Sent to a user when they sign up to volunteer for a role on an event (Phase 7D.1)',
                    REGEXP_REPLACE(
                        REGEXP_REPLACE(subject_template, 'Sign[- ]?[Uu]p', 'Volunteer', 'g'),
                        'Signed up', 'Volunteered', 'g'),
                    REGEXP_REPLACE(
                        REGEXP_REPLACE(
                            REGEXP_REPLACE(text_template, 'Sign[- ]?[Uu]p', 'Volunteer', 'g'),
                            'Signed up', 'Volunteered', 'g'),
                        'signed up', 'volunteered', 'g'),
                    REGEXP_REPLACE(
                        REGEXP_REPLACE(
                            REGEXP_REPLACE(html_template, 'Sign[- ]?[Uu]p', 'Volunteer', 'g'),
                            'Signed up', 'Volunteered', 'g'),
                        'signed up', 'volunteered', 'g'),
                    type, category, TRUE, tags, NOW()
                FROM communications.email_templates
                WHERE name = 'template-signup-list-commitment-confirmation'
                ON CONFLICT (name) DO NOTHING;
            ");

            // Phase 7D.1 Phase C: Seed volunteer commitment cancellation template
            migrationBuilder.Sql(@"
                INSERT INTO communications.email_templates
                    (""Id"", name, description, subject_template, text_template, html_template,
                     type, category, is_active, tags, created_at)
                SELECT
                    gen_random_uuid(),
                    'template-volunteer-commitment-cancellation',
                    'Sent to a user when their volunteer signup for a role is cancelled (Phase 7D.1)',
                    REGEXP_REPLACE(
                        REGEXP_REPLACE(subject_template, 'Sign[- ]?[Uu]p', 'Volunteer', 'g'),
                        'Signed up', 'Volunteered', 'g'),
                    REGEXP_REPLACE(
                        REGEXP_REPLACE(
                            REGEXP_REPLACE(text_template, 'Sign[- ]?[Uu]p', 'Volunteer', 'g'),
                            'Signed up', 'Volunteered', 'g'),
                        'signed up', 'volunteered', 'g'),
                    REGEXP_REPLACE(
                        REGEXP_REPLACE(
                            REGEXP_REPLACE(html_template, 'Sign[- ]?[Uu]p', 'Volunteer', 'g'),
                            'Signed up', 'Volunteered', 'g'),
                        'signed up', 'volunteered', 'g'),
                    type, category, TRUE, tags, NOW()
                FROM communications.email_templates
                WHERE name = 'template-signup-list-commitment-cancellation'
                ON CONFLICT (name) DO NOTHING;
            ");

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("0b9effc0-322f-8026-85c6-747e381b41e6"),
                column: "created_at",
                value: new DateTime(2026, 4, 20, 17, 54, 42, 619, DateTimeKind.Utc).AddTicks(5140));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("2d87836d-9322-d4b1-b4ec-b5b73eca9ad9"),
                column: "created_at",
                value: new DateTime(2026, 4, 20, 17, 54, 42, 619, DateTimeKind.Utc).AddTicks(5261));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("31f73d61-6c12-1252-f5ab-10d9d47eba46"),
                column: "created_at",
                value: new DateTime(2026, 4, 20, 17, 54, 42, 619, DateTimeKind.Utc).AddTicks(5094));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4de1eacb-273a-ab85-e811-d60addb4ae30"),
                column: "created_at",
                value: new DateTime(2026, 4, 20, 17, 54, 42, 619, DateTimeKind.Utc).AddTicks(5183));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4e57a1be-7a76-833e-003f-b2e3182f29f0"),
                column: "created_at",
                value: new DateTime(2026, 4, 20, 17, 54, 42, 619, DateTimeKind.Utc).AddTicks(5197));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("6313b249-2620-3e97-c1bd-f1d50814156d"),
                column: "created_at",
                value: new DateTime(2026, 4, 20, 17, 54, 42, 619, DateTimeKind.Utc).AddTicks(5333));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("70ab7cff-d677-f4bd-b331-f02908ee3347"),
                column: "created_at",
                value: new DateTime(2026, 4, 20, 17, 54, 42, 619, DateTimeKind.Utc).AddTicks(5156));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("80cd50b4-7630-f5d0-1f9a-a7c480347dcf"),
                column: "created_at",
                value: new DateTime(2026, 4, 20, 17, 54, 42, 619, DateTimeKind.Utc).AddTicks(5124));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("9b07d22a-d0bf-ad27-01bf-0c8410d4b9e1"),
                column: "created_at",
                value: new DateTime(2026, 4, 20, 17, 54, 42, 619, DateTimeKind.Utc).AddTicks(5308));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("c5735376-4831-c12b-a01e-672efee6c8e3"),
                column: "created_at",
                value: new DateTime(2026, 4, 20, 17, 54, 42, 619, DateTimeKind.Utc).AddTicks(5294));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("cdaa97c0-e68f-2819-984e-63bb9dcf35a6"),
                column: "created_at",
                value: new DateTime(2026, 4, 20, 17, 54, 42, 619, DateTimeKind.Utc).AddTicks(5275));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("e1d5afac-09d6-ef55-a529-f5bf473ef103"),
                column: "created_at",
                value: new DateTime(2026, 4, 20, 17, 54, 42, 619, DateTimeKind.Utc).AddTicks(5320));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DELETE FROM communications.email_templates
                WHERE name IN (
                    'template-volunteer-commitment-confirmation',
                    'template-volunteer-commitment-cancellation'
                );
            ");

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("0b9effc0-322f-8026-85c6-747e381b41e6"),
                column: "created_at",
                value: new DateTime(2026, 4, 20, 4, 1, 50, 884, DateTimeKind.Utc).AddTicks(8294));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("2d87836d-9322-d4b1-b4ec-b5b73eca9ad9"),
                column: "created_at",
                value: new DateTime(2026, 4, 20, 4, 1, 50, 884, DateTimeKind.Utc).AddTicks(8419));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("31f73d61-6c12-1252-f5ab-10d9d47eba46"),
                column: "created_at",
                value: new DateTime(2026, 4, 20, 4, 1, 50, 884, DateTimeKind.Utc).AddTicks(8204));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4de1eacb-273a-ab85-e811-d60addb4ae30"),
                column: "created_at",
                value: new DateTime(2026, 4, 20, 4, 1, 50, 884, DateTimeKind.Utc).AddTicks(8349));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4e57a1be-7a76-833e-003f-b2e3182f29f0"),
                column: "created_at",
                value: new DateTime(2026, 4, 20, 4, 1, 50, 884, DateTimeKind.Utc).AddTicks(8376));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("6313b249-2620-3e97-c1bd-f1d50814156d"),
                column: "created_at",
                value: new DateTime(2026, 4, 20, 4, 1, 50, 884, DateTimeKind.Utc).AddTicks(8555));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("70ab7cff-d677-f4bd-b331-f02908ee3347"),
                column: "created_at",
                value: new DateTime(2026, 4, 20, 4, 1, 50, 884, DateTimeKind.Utc).AddTicks(8319));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("80cd50b4-7630-f5d0-1f9a-a7c480347dcf"),
                column: "created_at",
                value: new DateTime(2026, 4, 20, 4, 1, 50, 884, DateTimeKind.Utc).AddTicks(8267));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("9b07d22a-d0bf-ad27-01bf-0c8410d4b9e1"),
                column: "created_at",
                value: new DateTime(2026, 4, 20, 4, 1, 50, 884, DateTimeKind.Utc).AddTicks(8504));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("c5735376-4831-c12b-a01e-672efee6c8e3"),
                column: "created_at",
                value: new DateTime(2026, 4, 20, 4, 1, 50, 884, DateTimeKind.Utc).AddTicks(8479));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("cdaa97c0-e68f-2819-984e-63bb9dcf35a6"),
                column: "created_at",
                value: new DateTime(2026, 4, 20, 4, 1, 50, 884, DateTimeKind.Utc).AddTicks(8443));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("e1d5afac-09d6-ef55-a529-f5bf473ef103"),
                column: "created_at",
                value: new DateTime(2026, 4, 20, 4, 1, 50, 884, DateTimeKind.Utc).AddTicks(8529));
        }
    }
}
