using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class MoveStateTaxRatesToReferenceDataSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Phase 6A.X: Move state_tax_rates from public schema to reference_data schema
            // Step 1: Check if table exists in public schema, if so, move it
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    -- Check if table exists in public schema
                    IF EXISTS (
                        SELECT FROM information_schema.tables
                        WHERE table_schema = 'public'
                        AND table_name = 'state_tax_rates'
                    ) THEN
                        -- Create reference_data schema if it doesn't exist
                        CREATE SCHEMA IF NOT EXISTS reference_data;

                        -- Move table to reference_data schema
                        ALTER TABLE public.state_tax_rates SET SCHEMA reference_data;

                        RAISE NOTICE 'Moved state_tax_rates from public to reference_data schema';
                    ELSE
                        -- Table doesn't exist in public, create it in reference_data
                        CREATE SCHEMA IF NOT EXISTS reference_data;

                        CREATE TABLE reference_data.state_tax_rates (
                            id uuid NOT NULL,
                            state_code character varying(2) NOT NULL,
                            state_name character varying(100) NOT NULL,
                            tax_rate numeric(5,4) NOT NULL,
                            effective_date timestamp with time zone NOT NULL,
                            is_active boolean NOT NULL DEFAULT true,
                            data_source character varying(200),
                            created_at timestamp with time zone NOT NULL DEFAULT NOW(),
                            updated_at timestamp with time zone,
                            CONSTRAINT ""PK_state_tax_rates"" PRIMARY KEY (id)
                        );

                        -- Create indexes
                        CREATE INDEX ix_state_tax_rates_state_code ON reference_data.state_tax_rates (state_code);
                        CREATE INDEX ix_state_tax_rates_state_code_is_active ON reference_data.state_tax_rates (state_code, is_active);
                        CREATE INDEX ix_state_tax_rates_effective_date ON reference_data.state_tax_rates (effective_date);
                        CREATE UNIQUE INDEX uq_state_tax_rates_state_code_effective_date ON reference_data.state_tax_rates (state_code, effective_date);

                        -- Insert seed data (50 US states + DC)
                        INSERT INTO reference_data.state_tax_rates (id, state_code, state_name, tax_rate, effective_date, is_active, data_source, created_at)
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

                        RAISE NOTICE 'Created state_tax_rates table in reference_data schema with seed data';
                    END IF;
                END $$;
            ");

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("0b9effc0-322f-8026-85c6-747e381b41e6"),
                column: "created_at",
                value: new DateTime(2026, 1, 17, 15, 1, 25, 274, DateTimeKind.Utc).AddTicks(8675));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("2d87836d-9322-d4b1-b4ec-b5b73eca9ad9"),
                column: "created_at",
                value: new DateTime(2026, 1, 17, 15, 1, 25, 274, DateTimeKind.Utc).AddTicks(8781));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("31f73d61-6c12-1252-f5ab-10d9d47eba46"),
                column: "created_at",
                value: new DateTime(2026, 1, 17, 15, 1, 25, 274, DateTimeKind.Utc).AddTicks(8606));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4de1eacb-273a-ab85-e811-d60addb4ae30"),
                column: "created_at",
                value: new DateTime(2026, 1, 17, 15, 1, 25, 274, DateTimeKind.Utc).AddTicks(8723));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4e57a1be-7a76-833e-003f-b2e3182f29f0"),
                column: "created_at",
                value: new DateTime(2026, 1, 17, 15, 1, 25, 274, DateTimeKind.Utc).AddTicks(8745));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("6313b249-2620-3e97-c1bd-f1d50814156d"),
                column: "created_at",
                value: new DateTime(2026, 1, 17, 15, 1, 25, 274, DateTimeKind.Utc).AddTicks(9001));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("70ab7cff-d677-f4bd-b331-f02908ee3347"),
                column: "created_at",
                value: new DateTime(2026, 1, 17, 15, 1, 25, 274, DateTimeKind.Utc).AddTicks(8699));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("80cd50b4-7630-f5d0-1f9a-a7c480347dcf"),
                column: "created_at",
                value: new DateTime(2026, 1, 17, 15, 1, 25, 274, DateTimeKind.Utc).AddTicks(8649));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("9b07d22a-d0bf-ad27-01bf-0c8410d4b9e1"),
                column: "created_at",
                value: new DateTime(2026, 1, 17, 15, 1, 25, 274, DateTimeKind.Utc).AddTicks(8959));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("c5735376-4831-c12b-a01e-672efee6c8e3"),
                column: "created_at",
                value: new DateTime(2026, 1, 17, 15, 1, 25, 274, DateTimeKind.Utc).AddTicks(8934));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("cdaa97c0-e68f-2819-984e-63bb9dcf35a6"),
                column: "created_at",
                value: new DateTime(2026, 1, 17, 15, 1, 25, 274, DateTimeKind.Utc).AddTicks(8902));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("e1d5afac-09d6-ef55-a529-f5bf473ef103"),
                column: "created_at",
                value: new DateTime(2026, 1, 17, 15, 1, 25, 274, DateTimeKind.Utc).AddTicks(8980));

            // Indexes are already created in the SQL block above
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Phase 6A.X: Move state_tax_rates back to public schema
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT FROM information_schema.tables
                        WHERE table_schema = 'reference_data'
                        AND table_name = 'state_tax_rates'
                    ) THEN
                        ALTER TABLE reference_data.state_tax_rates SET SCHEMA public;
                        RAISE NOTICE 'Moved state_tax_rates back to public schema';
                    END IF;
                END $$;
            ");

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("0b9effc0-322f-8026-85c6-747e381b41e6"),
                column: "created_at",
                value: new DateTime(2026, 1, 16, 16, 3, 21, 765, DateTimeKind.Utc).AddTicks(1068));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("2d87836d-9322-d4b1-b4ec-b5b73eca9ad9"),
                column: "created_at",
                value: new DateTime(2026, 1, 16, 16, 3, 21, 765, DateTimeKind.Utc).AddTicks(1161));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("31f73d61-6c12-1252-f5ab-10d9d47eba46"),
                column: "created_at",
                value: new DateTime(2026, 1, 16, 16, 3, 21, 765, DateTimeKind.Utc).AddTicks(1002));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4de1eacb-273a-ab85-e811-d60addb4ae30"),
                column: "created_at",
                value: new DateTime(2026, 1, 16, 16, 3, 21, 765, DateTimeKind.Utc).AddTicks(1114));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4e57a1be-7a76-833e-003f-b2e3182f29f0"),
                column: "created_at",
                value: new DateTime(2026, 1, 16, 16, 3, 21, 765, DateTimeKind.Utc).AddTicks(1140));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("6313b249-2620-3e97-c1bd-f1d50814156d"),
                column: "created_at",
                value: new DateTime(2026, 1, 16, 16, 3, 21, 765, DateTimeKind.Utc).AddTicks(1262));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("70ab7cff-d677-f4bd-b331-f02908ee3347"),
                column: "created_at",
                value: new DateTime(2026, 1, 16, 16, 3, 21, 765, DateTimeKind.Utc).AddTicks(1091));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("80cd50b4-7630-f5d0-1f9a-a7c480347dcf"),
                column: "created_at",
                value: new DateTime(2026, 1, 16, 16, 3, 21, 765, DateTimeKind.Utc).AddTicks(1035));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("9b07d22a-d0bf-ad27-01bf-0c8410d4b9e1"),
                column: "created_at",
                value: new DateTime(2026, 1, 16, 16, 3, 21, 765, DateTimeKind.Utc).AddTicks(1222));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("c5735376-4831-c12b-a01e-672efee6c8e3"),
                column: "created_at",
                value: new DateTime(2026, 1, 16, 16, 3, 21, 765, DateTimeKind.Utc).AddTicks(1202));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("cdaa97c0-e68f-2819-984e-63bb9dcf35a6"),
                column: "created_at",
                value: new DateTime(2026, 1, 16, 16, 3, 21, 765, DateTimeKind.Utc).AddTicks(1182));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("e1d5afac-09d6-ef55-a529-f5bf473ef103"),
                column: "created_at",
                value: new DateTime(2026, 1, 16, 16, 3, 21, 765, DateTimeKind.Utc).AddTicks(1242));
        }
    }
}
