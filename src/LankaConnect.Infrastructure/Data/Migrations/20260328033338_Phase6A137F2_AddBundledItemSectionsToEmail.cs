using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Infrastructure.Data.Migrations
{
    /// <summary>
    /// Phase 6A.137F-Fix B: Add add-on, collection, and sponsor breakdown sections
    /// to the paid registration confirmation email template.
    ///
    /// Inserts new Handlebars sections between the existing {{#if HasDonation}}...{{/if}}
    /// block and the Total row inside the {{#if HasFinancialBreakdown}} section:
    /// - {{#if HasAddOns}}: Renders add-on line items HTML + total
    /// - {{#if HasCollection}}: Renders collection amount
    /// - {{#if HasSponsor}}: Renders sponsorship amount
    ///
    /// These sections were planned in Phase F3.4 but never implemented.
    /// The C# code in PaymentCompletedEventHandler already populates these params.
    /// </summary>
    public partial class Phase6A137F2_AddBundledItemSectionsToEmail : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Insert add-on/collection/sponsor rows BETWEEN the donation {{/if}} and the Total row.
            // The 137C migration created the breakdown with this pattern:
            //   ...{{#if HasDonation}}<tr>...Donation...</tr>{{/if}}
            //   <tr>..Total..{{AmountPaid}}...</tr>
            // We insert new sections just before the Total row.
            // Match: end of donation {{/if}} followed by newline and the Total row <tr>
            migrationBuilder.Sql(@"
                UPDATE communications.email_templates
                SET html_template = REGEXP_REPLACE(
                    html_template,
                    '(\{\{/if\}\})\s*(<tr><td style=""padding: 4px 0; font-size: 14px; color: #6b7280; border-top: 1px solid #e5e7eb)',
                    E'{{/if}}\n{{#if HasAddOns}}<tr><td style=""padding: 4px 0; font-size: 14px; color: #6b7280;"">{{{AddOnBreakdownHtml}}}</td></tr>\n<tr><td style=""padding: 4px 0; font-size: 14px; color: #6b7280;"">Add-on Total: <strong style=""color: #111827;"">${{AddOnTotal}}</strong></td></tr>{{/if}}\n{{#if HasCollection}}<tr><td style=""padding: 4px 0; font-size: 14px; color: #6b7280;"">Collection: <strong style=""color: #059669;"">${{CollectionBreakdownAmount}}</strong></td></tr>{{/if}}\n{{#if HasSponsor}}<tr><td style=""padding: 4px 0; font-size: 14px; color: #6b7280;"">Sponsorship: <strong style=""color: #059669;"">${{SponsorBreakdownAmount}}</strong></td></tr>{{/if}}\n<tr><td style=""padding: 4px 0; font-size: 14px; color: #6b7280; border-top: 1px solid #e5e7eb',
                    'g'
                ),
                text_template = REGEXP_REPLACE(
                    text_template,
                    '(\{\{/if\}\})(Total:)',
                    E'{{/if}}{{#if HasAddOns}}Add-on Total: ${{AddOnTotal}}\n{{/if}}{{#if HasCollection}}Collection: ${{CollectionBreakdownAmount}}\n{{/if}}{{#if HasSponsor}}Sponsorship: ${{SponsorBreakdownAmount}}\n{{/if}}Total:',
                    'g'
                ),
                updated_at = NOW()
                WHERE name = 'template-paid-event-registration-confirmation-with-ticket';
            ");

            // Keep auto-generated reference data updates
            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("0b9effc0-322f-8026-85c6-747e381b41e6"),
                column: "created_at",
                value: new DateTime(2026, 3, 28, 3, 33, 36, 302, DateTimeKind.Utc).AddTicks(3051));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("2d87836d-9322-d4b1-b4ec-b5b73eca9ad9"),
                column: "created_at",
                value: new DateTime(2026, 3, 28, 3, 33, 36, 302, DateTimeKind.Utc).AddTicks(3148));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("31f73d61-6c12-1252-f5ab-10d9d47eba46"),
                column: "created_at",
                value: new DateTime(2026, 3, 28, 3, 33, 36, 302, DateTimeKind.Utc).AddTicks(2986));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4de1eacb-273a-ab85-e811-d60addb4ae30"),
                column: "created_at",
                value: new DateTime(2026, 3, 28, 3, 33, 36, 302, DateTimeKind.Utc).AddTicks(3093));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4e57a1be-7a76-833e-003f-b2e3182f29f0"),
                column: "created_at",
                value: new DateTime(2026, 3, 28, 3, 33, 36, 302, DateTimeKind.Utc).AddTicks(3114));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("6313b249-2620-3e97-c1bd-f1d50814156d"),
                column: "created_at",
                value: new DateTime(2026, 3, 28, 3, 33, 36, 302, DateTimeKind.Utc).AddTicks(3254));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("70ab7cff-d677-f4bd-b331-f02908ee3347"),
                column: "created_at",
                value: new DateTime(2026, 3, 28, 3, 33, 36, 302, DateTimeKind.Utc).AddTicks(3073));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("80cd50b4-7630-f5d0-1f9a-a7c480347dcf"),
                column: "created_at",
                value: new DateTime(2026, 3, 28, 3, 33, 36, 302, DateTimeKind.Utc).AddTicks(3026));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("9b07d22a-d0bf-ad27-01bf-0c8410d4b9e1"),
                column: "created_at",
                value: new DateTime(2026, 3, 28, 3, 33, 36, 302, DateTimeKind.Utc).AddTicks(3215));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("c5735376-4831-c12b-a01e-672efee6c8e3"),
                column: "created_at",
                value: new DateTime(2026, 3, 28, 3, 33, 36, 302, DateTimeKind.Utc).AddTicks(3196));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("cdaa97c0-e68f-2819-984e-63bb9dcf35a6"),
                column: "created_at",
                value: new DateTime(2026, 3, 28, 3, 33, 36, 302, DateTimeKind.Utc).AddTicks(3167));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("e1d5afac-09d6-ef55-a529-f5bf473ef103"),
                column: "created_at",
                value: new DateTime(2026, 3, 28, 3, 33, 36, 302, DateTimeKind.Utc).AddTicks(3233));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("0b9effc0-322f-8026-85c6-747e381b41e6"),
                column: "created_at",
                value: new DateTime(2026, 3, 25, 23, 23, 10, 264, DateTimeKind.Utc).AddTicks(8137));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("2d87836d-9322-d4b1-b4ec-b5b73eca9ad9"),
                column: "created_at",
                value: new DateTime(2026, 3, 25, 23, 23, 10, 264, DateTimeKind.Utc).AddTicks(8231));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("31f73d61-6c12-1252-f5ab-10d9d47eba46"),
                column: "created_at",
                value: new DateTime(2026, 3, 25, 23, 23, 10, 264, DateTimeKind.Utc).AddTicks(7944));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4de1eacb-273a-ab85-e811-d60addb4ae30"),
                column: "created_at",
                value: new DateTime(2026, 3, 25, 23, 23, 10, 264, DateTimeKind.Utc).AddTicks(8187));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4e57a1be-7a76-833e-003f-b2e3182f29f0"),
                column: "created_at",
                value: new DateTime(2026, 3, 25, 23, 23, 10, 264, DateTimeKind.Utc).AddTicks(8204));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("6313b249-2620-3e97-c1bd-f1d50814156d"),
                column: "created_at",
                value: new DateTime(2026, 3, 25, 23, 23, 10, 264, DateTimeKind.Utc).AddTicks(8316));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("70ab7cff-d677-f4bd-b331-f02908ee3347"),
                column: "created_at",
                value: new DateTime(2026, 3, 25, 23, 23, 10, 264, DateTimeKind.Utc).AddTicks(8169));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("80cd50b4-7630-f5d0-1f9a-a7c480347dcf"),
                column: "created_at",
                value: new DateTime(2026, 3, 25, 23, 23, 10, 264, DateTimeKind.Utc).AddTicks(7976));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("9b07d22a-d0bf-ad27-01bf-0c8410d4b9e1"),
                column: "created_at",
                value: new DateTime(2026, 3, 25, 23, 23, 10, 264, DateTimeKind.Utc).AddTicks(8285));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("c5735376-4831-c12b-a01e-672efee6c8e3"),
                column: "created_at",
                value: new DateTime(2026, 3, 25, 23, 23, 10, 264, DateTimeKind.Utc).AddTicks(8269));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("cdaa97c0-e68f-2819-984e-63bb9dcf35a6"),
                column: "created_at",
                value: new DateTime(2026, 3, 25, 23, 23, 10, 264, DateTimeKind.Utc).AddTicks(8248));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("e1d5afac-09d6-ef55-a529-f5bf473ef103"),
                column: "created_at",
                value: new DateTime(2026, 3, 25, 23, 23, 10, 264, DateTimeKind.Utc).AddTicks(8300));
        }
    }
}
