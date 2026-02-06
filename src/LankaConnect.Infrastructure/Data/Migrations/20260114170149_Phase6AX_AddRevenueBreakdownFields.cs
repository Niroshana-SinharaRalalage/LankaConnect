using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class Phase6AX_AddRevenueBreakdownFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "organizer_payout_amount",
                schema: "events",
                table: "registrations",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "organizer_payout_currency",
                schema: "events",
                table: "registrations",
                type: "character varying(3)",
                maxLength: 3,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "platform_commission_amount",
                schema: "events",
                table: "registrations",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "platform_commission_currency",
                schema: "events",
                table: "registrations",
                type: "character varying(3)",
                maxLength: 3,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "sales_tax_amount",
                schema: "events",
                table: "registrations",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "sales_tax_currency",
                schema: "events",
                table: "registrations",
                type: "character varying(3)",
                maxLength: 3,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "sales_tax_rate",
                schema: "events",
                table: "registrations",
                type: "numeric(5,4)",
                precision: 5,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "stripe_fee_amount",
                schema: "events",
                table: "registrations",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "stripe_fee_currency",
                schema: "events",
                table: "registrations",
                type: "character varying(3)",
                maxLength: 3,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "revenue_breakdown",
                schema: "events",
                table: "events",
                type: "jsonb",
                nullable: true);

            // Phase 6A.X: Create state_tax_rates table for US state sales tax rates
            migrationBuilder.CreateTable(
                name: "state_tax_rates",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    state_code = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    state_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    tax_rate = table.Column<decimal>(type: "numeric(5,4)", precision: 5, scale: 4, nullable: false),
                    effective_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    data_source = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_state_tax_rates", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_state_tax_rates_state_code",
                table: "state_tax_rates",
                column: "state_code");

            migrationBuilder.CreateIndex(
                name: "ix_state_tax_rates_state_code_is_active",
                table: "state_tax_rates",
                columns: new[] { "state_code", "is_active" });

            migrationBuilder.CreateIndex(
                name: "ix_state_tax_rates_effective_date",
                table: "state_tax_rates",
                column: "effective_date");

            migrationBuilder.CreateIndex(
                name: "uq_state_tax_rates_state_code_effective_date",
                table: "state_tax_rates",
                columns: new[] { "state_code", "effective_date" },
                unique: true);

            // Phase 6A.X: Seed US state sales tax rates (2025 data from Tax Foundation)
            // Source: https://taxfoundation.org/data/all/state/sales-tax-rates/
            // Using raw SQL for seed data to avoid EF Core entity mapping issues
            migrationBuilder.Sql(@"
INSERT INTO state_tax_rates (id, state_code, state_name, tax_rate, effective_date, is_active, data_source, created_at)
VALUES
    (gen_random_uuid(), 'AL', 'Alabama', 0.04, '2025-01-01 00:00:00+00', true, 'Tax Foundation 2025', NOW()),
    (gen_random_uuid(), 'AK', 'Alaska', 0.00, '2025-01-01 00:00:00+00', true, 'Tax Foundation 2025', NOW()),
    (gen_random_uuid(), 'AZ', 'Arizona', 0.056, '2025-01-01 00:00:00+00', true, 'Tax Foundation 2025', NOW()),
    (gen_random_uuid(), 'AR', 'Arkansas', 0.065, '2025-01-01 00:00:00+00', true, 'Tax Foundation 2025', NOW()),
    (gen_random_uuid(), 'CA', 'California', 0.0725, '2025-01-01 00:00:00+00', true, 'Tax Foundation 2025', NOW()),
    (gen_random_uuid(), 'CO', 'Colorado', 0.029, '2025-01-01 00:00:00+00', true, 'Tax Foundation 2025', NOW()),
    (gen_random_uuid(), 'CT', 'Connecticut', 0.0635, '2025-01-01 00:00:00+00', true, 'Tax Foundation 2025', NOW()),
    (gen_random_uuid(), 'DE', 'Delaware', 0.00, '2025-01-01 00:00:00+00', true, 'Tax Foundation 2025', NOW()),
    (gen_random_uuid(), 'FL', 'Florida', 0.06, '2025-01-01 00:00:00+00', true, 'Tax Foundation 2025', NOW()),
    (gen_random_uuid(), 'GA', 'Georgia', 0.04, '2025-01-01 00:00:00+00', true, 'Tax Foundation 2025', NOW()),
    (gen_random_uuid(), 'HI', 'Hawaii', 0.04, '2025-01-01 00:00:00+00', true, 'Tax Foundation 2025', NOW()),
    (gen_random_uuid(), 'ID', 'Idaho', 0.06, '2025-01-01 00:00:00+00', true, 'Tax Foundation 2025', NOW()),
    (gen_random_uuid(), 'IL', 'Illinois', 0.0625, '2025-01-01 00:00:00+00', true, 'Tax Foundation 2025', NOW()),
    (gen_random_uuid(), 'IN', 'Indiana', 0.07, '2025-01-01 00:00:00+00', true, 'Tax Foundation 2025', NOW()),
    (gen_random_uuid(), 'IA', 'Iowa', 0.06, '2025-01-01 00:00:00+00', true, 'Tax Foundation 2025', NOW()),
    (gen_random_uuid(), 'KS', 'Kansas', 0.065, '2025-01-01 00:00:00+00', true, 'Tax Foundation 2025', NOW()),
    (gen_random_uuid(), 'KY', 'Kentucky', 0.06, '2025-01-01 00:00:00+00', true, 'Tax Foundation 2025', NOW()),
    (gen_random_uuid(), 'LA', 'Louisiana', 0.0445, '2025-01-01 00:00:00+00', true, 'Tax Foundation 2025', NOW()),
    (gen_random_uuid(), 'ME', 'Maine', 0.055, '2025-01-01 00:00:00+00', true, 'Tax Foundation 2025', NOW()),
    (gen_random_uuid(), 'MD', 'Maryland', 0.06, '2025-01-01 00:00:00+00', true, 'Tax Foundation 2025', NOW()),
    (gen_random_uuid(), 'MA', 'Massachusetts', 0.0625, '2025-01-01 00:00:00+00', true, 'Tax Foundation 2025', NOW()),
    (gen_random_uuid(), 'MI', 'Michigan', 0.06, '2025-01-01 00:00:00+00', true, 'Tax Foundation 2025', NOW()),
    (gen_random_uuid(), 'MN', 'Minnesota', 0.0688, '2025-01-01 00:00:00+00', true, 'Tax Foundation 2025', NOW()),
    (gen_random_uuid(), 'MS', 'Mississippi', 0.07, '2025-01-01 00:00:00+00', true, 'Tax Foundation 2025', NOW()),
    (gen_random_uuid(), 'MO', 'Missouri', 0.0423, '2025-01-01 00:00:00+00', true, 'Tax Foundation 2025', NOW()),
    (gen_random_uuid(), 'MT', 'Montana', 0.00, '2025-01-01 00:00:00+00', true, 'Tax Foundation 2025', NOW()),
    (gen_random_uuid(), 'NE', 'Nebraska', 0.055, '2025-01-01 00:00:00+00', true, 'Tax Foundation 2025', NOW()),
    (gen_random_uuid(), 'NV', 'Nevada', 0.0685, '2025-01-01 00:00:00+00', true, 'Tax Foundation 2025', NOW()),
    (gen_random_uuid(), 'NH', 'New Hampshire', 0.00, '2025-01-01 00:00:00+00', true, 'Tax Foundation 2025', NOW()),
    (gen_random_uuid(), 'NJ', 'New Jersey', 0.0663, '2025-01-01 00:00:00+00', true, 'Tax Foundation 2025', NOW()),
    (gen_random_uuid(), 'NM', 'New Mexico', 0.0512, '2025-01-01 00:00:00+00', true, 'Tax Foundation 2025', NOW()),
    (gen_random_uuid(), 'NY', 'New York', 0.04, '2025-01-01 00:00:00+00', true, 'Tax Foundation 2025', NOW()),
    (gen_random_uuid(), 'NC', 'North Carolina', 0.0475, '2025-01-01 00:00:00+00', true, 'Tax Foundation 2025', NOW()),
    (gen_random_uuid(), 'ND', 'North Dakota', 0.05, '2025-01-01 00:00:00+00', true, 'Tax Foundation 2025', NOW()),
    (gen_random_uuid(), 'OH', 'Ohio', 0.0575, '2025-01-01 00:00:00+00', true, 'Tax Foundation 2025', NOW()),
    (gen_random_uuid(), 'OK', 'Oklahoma', 0.045, '2025-01-01 00:00:00+00', true, 'Tax Foundation 2025', NOW()),
    (gen_random_uuid(), 'OR', 'Oregon', 0.00, '2025-01-01 00:00:00+00', true, 'Tax Foundation 2025', NOW()),
    (gen_random_uuid(), 'PA', 'Pennsylvania', 0.06, '2025-01-01 00:00:00+00', true, 'Tax Foundation 2025', NOW()),
    (gen_random_uuid(), 'RI', 'Rhode Island', 0.07, '2025-01-01 00:00:00+00', true, 'Tax Foundation 2025', NOW()),
    (gen_random_uuid(), 'SC', 'South Carolina', 0.06, '2025-01-01 00:00:00+00', true, 'Tax Foundation 2025', NOW()),
    (gen_random_uuid(), 'SD', 'South Dakota', 0.045, '2025-01-01 00:00:00+00', true, 'Tax Foundation 2025', NOW()),
    (gen_random_uuid(), 'TN', 'Tennessee', 0.07, '2025-01-01 00:00:00+00', true, 'Tax Foundation 2025', NOW()),
    (gen_random_uuid(), 'TX', 'Texas', 0.0625, '2025-01-01 00:00:00+00', true, 'Tax Foundation 2025', NOW()),
    (gen_random_uuid(), 'UT', 'Utah', 0.0595, '2025-01-01 00:00:00+00', true, 'Tax Foundation 2025', NOW()),
    (gen_random_uuid(), 'VT', 'Vermont', 0.06, '2025-01-01 00:00:00+00', true, 'Tax Foundation 2025', NOW()),
    (gen_random_uuid(), 'VA', 'Virginia', 0.053, '2025-01-01 00:00:00+00', true, 'Tax Foundation 2025', NOW()),
    (gen_random_uuid(), 'WA', 'Washington', 0.065, '2025-01-01 00:00:00+00', true, 'Tax Foundation 2025', NOW()),
    (gen_random_uuid(), 'WV', 'West Virginia', 0.06, '2025-01-01 00:00:00+00', true, 'Tax Foundation 2025', NOW()),
    (gen_random_uuid(), 'WI', 'Wisconsin', 0.05, '2025-01-01 00:00:00+00', true, 'Tax Foundation 2025', NOW()),
    (gen_random_uuid(), 'WY', 'Wyoming', 0.04, '2025-01-01 00:00:00+00', true, 'Tax Foundation 2025', NOW()),
    (gen_random_uuid(), 'DC', 'District of Columbia', 0.06, '2025-01-01 00:00:00+00', true, 'Tax Foundation 2025', NOW())
ON CONFLICT DO NOTHING;
");

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("0b9effc0-322f-8026-85c6-747e381b41e6"),
                column: "created_at",
                value: new DateTime(2026, 1, 14, 17, 1, 46, 641, DateTimeKind.Utc).AddTicks(1962));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("2d87836d-9322-d4b1-b4ec-b5b73eca9ad9"),
                column: "created_at",
                value: new DateTime(2026, 1, 14, 17, 1, 46, 641, DateTimeKind.Utc).AddTicks(2037));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("31f73d61-6c12-1252-f5ab-10d9d47eba46"),
                column: "created_at",
                value: new DateTime(2026, 1, 14, 17, 1, 46, 641, DateTimeKind.Utc).AddTicks(1898));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4de1eacb-273a-ab85-e811-d60addb4ae30"),
                column: "created_at",
                value: new DateTime(2026, 1, 14, 17, 1, 46, 641, DateTimeKind.Utc).AddTicks(2001));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4e57a1be-7a76-833e-003f-b2e3182f29f0"),
                column: "created_at",
                value: new DateTime(2026, 1, 14, 17, 1, 46, 641, DateTimeKind.Utc).AddTicks(2019));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("6313b249-2620-3e97-c1bd-f1d50814156d"),
                column: "created_at",
                value: new DateTime(2026, 1, 14, 17, 1, 46, 641, DateTimeKind.Utc).AddTicks(2122));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("70ab7cff-d677-f4bd-b331-f02908ee3347"),
                column: "created_at",
                value: new DateTime(2026, 1, 14, 17, 1, 46, 641, DateTimeKind.Utc).AddTicks(1980));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("80cd50b4-7630-f5d0-1f9a-a7c480347dcf"),
                column: "created_at",
                value: new DateTime(2026, 1, 14, 17, 1, 46, 641, DateTimeKind.Utc).AddTicks(1942));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("9b07d22a-d0bf-ad27-01bf-0c8410d4b9e1"),
                column: "created_at",
                value: new DateTime(2026, 1, 14, 17, 1, 46, 641, DateTimeKind.Utc).AddTicks(2088));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("c5735376-4831-c12b-a01e-672efee6c8e3"),
                column: "created_at",
                value: new DateTime(2026, 1, 14, 17, 1, 46, 641, DateTimeKind.Utc).AddTicks(2070));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("cdaa97c0-e68f-2819-984e-63bb9dcf35a6"),
                column: "created_at",
                value: new DateTime(2026, 1, 14, 17, 1, 46, 641, DateTimeKind.Utc).AddTicks(2054));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("e1d5afac-09d6-ef55-a529-f5bf473ef103"),
                column: "created_at",
                value: new DateTime(2026, 1, 14, 17, 1, 46, 641, DateTimeKind.Utc).AddTicks(2104));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "organizer_payout_amount",
                schema: "events",
                table: "registrations");

            migrationBuilder.DropColumn(
                name: "organizer_payout_currency",
                schema: "events",
                table: "registrations");

            migrationBuilder.DropColumn(
                name: "platform_commission_amount",
                schema: "events",
                table: "registrations");

            migrationBuilder.DropColumn(
                name: "platform_commission_currency",
                schema: "events",
                table: "registrations");

            migrationBuilder.DropColumn(
                name: "sales_tax_amount",
                schema: "events",
                table: "registrations");

            migrationBuilder.DropColumn(
                name: "sales_tax_currency",
                schema: "events",
                table: "registrations");

            migrationBuilder.DropColumn(
                name: "sales_tax_rate",
                schema: "events",
                table: "registrations");

            migrationBuilder.DropColumn(
                name: "stripe_fee_amount",
                schema: "events",
                table: "registrations");

            migrationBuilder.DropColumn(
                name: "stripe_fee_currency",
                schema: "events",
                table: "registrations");

            migrationBuilder.DropColumn(
                name: "revenue_breakdown",
                schema: "events",
                table: "events");

            // Phase 6A.X: Drop state_tax_rates table
            migrationBuilder.DropTable(
                name: "state_tax_rates");

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("0b9effc0-322f-8026-85c6-747e381b41e6"),
                column: "created_at",
                value: new DateTime(2026, 1, 14, 15, 15, 32, 192, DateTimeKind.Utc).AddTicks(3061));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("2d87836d-9322-d4b1-b4ec-b5b73eca9ad9"),
                column: "created_at",
                value: new DateTime(2026, 1, 14, 15, 15, 32, 192, DateTimeKind.Utc).AddTicks(3298));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("31f73d61-6c12-1252-f5ab-10d9d47eba46"),
                column: "created_at",
                value: new DateTime(2026, 1, 14, 15, 15, 32, 192, DateTimeKind.Utc).AddTicks(2935));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4de1eacb-273a-ab85-e811-d60addb4ae30"),
                column: "created_at",
                value: new DateTime(2026, 1, 14, 15, 15, 32, 192, DateTimeKind.Utc).AddTicks(3124));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4e57a1be-7a76-833e-003f-b2e3182f29f0"),
                column: "created_at",
                value: new DateTime(2026, 1, 14, 15, 15, 32, 192, DateTimeKind.Utc).AddTicks(3259));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("6313b249-2620-3e97-c1bd-f1d50814156d"),
                column: "created_at",
                value: new DateTime(2026, 1, 14, 15, 15, 32, 192, DateTimeKind.Utc).AddTicks(3446));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("70ab7cff-d677-f4bd-b331-f02908ee3347"),
                column: "created_at",
                value: new DateTime(2026, 1, 14, 15, 15, 32, 192, DateTimeKind.Utc).AddTicks(3091));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("80cd50b4-7630-f5d0-1f9a-a7c480347dcf"),
                column: "created_at",
                value: new DateTime(2026, 1, 14, 15, 15, 32, 192, DateTimeKind.Utc).AddTicks(3008));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("9b07d22a-d0bf-ad27-01bf-0c8410d4b9e1"),
                column: "created_at",
                value: new DateTime(2026, 1, 14, 15, 15, 32, 192, DateTimeKind.Utc).AddTicks(3388));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("c5735376-4831-c12b-a01e-672efee6c8e3"),
                column: "created_at",
                value: new DateTime(2026, 1, 14, 15, 15, 32, 192, DateTimeKind.Utc).AddTicks(3359));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("cdaa97c0-e68f-2819-984e-63bb9dcf35a6"),
                column: "created_at",
                value: new DateTime(2026, 1, 14, 15, 15, 32, 192, DateTimeKind.Utc).AddTicks(3329));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("e1d5afac-09d6-ef55-a529-f5bf473ef103"),
                column: "created_at",
                value: new DateTime(2026, 1, 14, 15, 15, 32, 192, DateTimeKind.Utc).AddTicks(3417));
        }
    }
}
