using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class Phase6AX_Hotfix_EnsureEmailTemplateNameConsistency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // HOTFIX: Ensure email template name matches application code
            // Root Cause: Phase 6A.76 migration renamed template from 'member-email-verification'
            // to 'template-membership-email-verification', but migration was not applied in Azure staging.
            // This idempotent SQL ensures template name consistency across all environments.
            migrationBuilder.Sql(@"
                -- Update template name if it exists with old name
                UPDATE communications.email_templates
                SET name = 'template-membership-email-verification', updated_at = NOW()
                WHERE name = 'member-email-verification';

                -- Log the change for audit trail
                DO $$
                DECLARE
                    rows_affected INTEGER;
                BEGIN
                    GET DIAGNOSTICS rows_affected = ROW_COUNT;
                    IF rows_affected > 0 THEN
                        RAISE NOTICE 'Phase6AX Hotfix: Updated % row(s) - renamed member-email-verification to template-membership-email-verification', rows_affected;
                    ELSE
                        RAISE NOTICE 'Phase6AX Hotfix: Template already has correct name or does not exist';
                    END IF;
                END $$;
            ");

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("0b9effc0-322f-8026-85c6-747e381b41e6"),
                column: "created_at",
                value: new DateTime(2026, 1, 27, 5, 2, 52, 505, DateTimeKind.Utc).AddTicks(5971));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("2d87836d-9322-d4b1-b4ec-b5b73eca9ad9"),
                column: "created_at",
                value: new DateTime(2026, 1, 27, 5, 2, 52, 505, DateTimeKind.Utc).AddTicks(6138));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("31f73d61-6c12-1252-f5ab-10d9d47eba46"),
                column: "created_at",
                value: new DateTime(2026, 1, 27, 5, 2, 52, 505, DateTimeKind.Utc).AddTicks(5831));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4de1eacb-273a-ab85-e811-d60addb4ae30"),
                column: "created_at",
                value: new DateTime(2026, 1, 27, 5, 2, 52, 505, DateTimeKind.Utc).AddTicks(6054));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4e57a1be-7a76-833e-003f-b2e3182f29f0"),
                column: "created_at",
                value: new DateTime(2026, 1, 27, 5, 2, 52, 505, DateTimeKind.Utc).AddTicks(6096));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("6313b249-2620-3e97-c1bd-f1d50814156d"),
                column: "created_at",
                value: new DateTime(2026, 1, 27, 5, 2, 52, 505, DateTimeKind.Utc).AddTicks(6352));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("70ab7cff-d677-f4bd-b331-f02908ee3347"),
                column: "created_at",
                value: new DateTime(2026, 1, 27, 5, 2, 52, 505, DateTimeKind.Utc).AddTicks(6010));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("80cd50b4-7630-f5d0-1f9a-a7c480347dcf"),
                column: "created_at",
                value: new DateTime(2026, 1, 27, 5, 2, 52, 505, DateTimeKind.Utc).AddTicks(5922));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("9b07d22a-d0bf-ad27-01bf-0c8410d4b9e1"),
                column: "created_at",
                value: new DateTime(2026, 1, 27, 5, 2, 52, 505, DateTimeKind.Utc).AddTicks(6274));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("c5735376-4831-c12b-a01e-672efee6c8e3"),
                column: "created_at",
                value: new DateTime(2026, 1, 27, 5, 2, 52, 505, DateTimeKind.Utc).AddTicks(6232));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("cdaa97c0-e68f-2819-984e-63bb9dcf35a6"),
                column: "created_at",
                value: new DateTime(2026, 1, 27, 5, 2, 52, 505, DateTimeKind.Utc).AddTicks(6177));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("e1d5afac-09d6-ef55-a529-f5bf473ef103"),
                column: "created_at",
                value: new DateTime(2026, 1, 27, 5, 2, 52, 505, DateTimeKind.Utc).AddTicks(6313));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Rollback: Revert template name to old name
            migrationBuilder.Sql(@"
                UPDATE communications.email_templates
                SET name = 'member-email-verification', updated_at = NOW()
                WHERE name = 'template-membership-email-verification';
            ");

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("0b9effc0-322f-8026-85c6-747e381b41e6"),
                column: "created_at",
                value: new DateTime(2026, 1, 27, 3, 32, 11, 345, DateTimeKind.Utc).AddTicks(5965));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("2d87836d-9322-d4b1-b4ec-b5b73eca9ad9"),
                column: "created_at",
                value: new DateTime(2026, 1, 27, 3, 32, 11, 345, DateTimeKind.Utc).AddTicks(6242));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("31f73d61-6c12-1252-f5ab-10d9d47eba46"),
                column: "created_at",
                value: new DateTime(2026, 1, 27, 3, 32, 11, 345, DateTimeKind.Utc).AddTicks(2386));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4de1eacb-273a-ab85-e811-d60addb4ae30"),
                column: "created_at",
                value: new DateTime(2026, 1, 27, 3, 32, 11, 345, DateTimeKind.Utc).AddTicks(6147));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4e57a1be-7a76-833e-003f-b2e3182f29f0"),
                column: "created_at",
                value: new DateTime(2026, 1, 27, 3, 32, 11, 345, DateTimeKind.Utc).AddTicks(6180));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("6313b249-2620-3e97-c1bd-f1d50814156d"),
                column: "created_at",
                value: new DateTime(2026, 1, 27, 3, 32, 11, 345, DateTimeKind.Utc).AddTicks(6403));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("70ab7cff-d677-f4bd-b331-f02908ee3347"),
                column: "created_at",
                value: new DateTime(2026, 1, 27, 3, 32, 11, 345, DateTimeKind.Utc).AddTicks(6106));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("80cd50b4-7630-f5d0-1f9a-a7c480347dcf"),
                column: "created_at",
                value: new DateTime(2026, 1, 27, 3, 32, 11, 345, DateTimeKind.Utc).AddTicks(2459));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("9b07d22a-d0bf-ad27-01bf-0c8410d4b9e1"),
                column: "created_at",
                value: new DateTime(2026, 1, 27, 3, 32, 11, 345, DateTimeKind.Utc).AddTicks(6341));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("c5735376-4831-c12b-a01e-672efee6c8e3"),
                column: "created_at",
                value: new DateTime(2026, 1, 27, 3, 32, 11, 345, DateTimeKind.Utc).AddTicks(6310));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("cdaa97c0-e68f-2819-984e-63bb9dcf35a6"),
                column: "created_at",
                value: new DateTime(2026, 1, 27, 3, 32, 11, 345, DateTimeKind.Utc).AddTicks(6276));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("e1d5afac-09d6-ef55-a529-f5bf473ef103"),
                column: "created_at",
                value: new DateTime(2026, 1, 27, 3, 32, 11, 345, DateTimeKind.Utc).AddTicks(6371));
        }
    }
}
