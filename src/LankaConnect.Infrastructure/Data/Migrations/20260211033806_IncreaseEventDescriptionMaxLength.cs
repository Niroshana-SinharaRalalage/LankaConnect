using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class IncreaseEventDescriptionMaxLength : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // PostgreSQL cannot ALTER COLUMN TYPE when the column is used by a generated column.
            // The search_vector column depends on description, so we must drop and recreate it.
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    -- Drop the generated search_vector column if it exists
                    IF EXISTS (
                        SELECT 1 FROM information_schema.columns
                        WHERE table_schema = 'events' AND table_name = 'events' AND column_name = 'search_vector'
                    ) THEN
                        ALTER TABLE events.events DROP COLUMN search_vector;
                    END IF;

                    -- Now safely alter the description column
                    ALTER TABLE events.events ALTER COLUMN description TYPE character varying(10000);

                    -- Recreate the search_vector generated column
                    ALTER TABLE events.events
                        ADD COLUMN search_vector tsvector
                        GENERATED ALWAYS AS (
                            setweight(to_tsvector('english', coalesce(title, '')), 'A') ||
                            setweight(to_tsvector('english', coalesce(description, '')), 'B')
                        ) STORED;

                    -- Recreate the GIN index for full-text search
                    CREATE INDEX IF NOT EXISTS ix_events_search_vector ON events.events USING gin(search_vector);
                END $$;
            ");

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("0b9effc0-322f-8026-85c6-747e381b41e6"),
                column: "created_at",
                value: new DateTime(2026, 2, 11, 3, 38, 1, 646, DateTimeKind.Utc).AddTicks(6644));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("2d87836d-9322-d4b1-b4ec-b5b73eca9ad9"),
                column: "created_at",
                value: new DateTime(2026, 2, 11, 3, 38, 1, 646, DateTimeKind.Utc).AddTicks(6873));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("31f73d61-6c12-1252-f5ab-10d9d47eba46"),
                column: "created_at",
                value: new DateTime(2026, 2, 11, 3, 38, 1, 646, DateTimeKind.Utc).AddTicks(6557));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4de1eacb-273a-ab85-e811-d60addb4ae30"),
                column: "created_at",
                value: new DateTime(2026, 2, 11, 3, 38, 1, 646, DateTimeKind.Utc).AddTicks(6806));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4e57a1be-7a76-833e-003f-b2e3182f29f0"),
                column: "created_at",
                value: new DateTime(2026, 2, 11, 3, 38, 1, 646, DateTimeKind.Utc).AddTicks(6831));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("6313b249-2620-3e97-c1bd-f1d50814156d"),
                column: "created_at",
                value: new DateTime(2026, 2, 11, 3, 38, 1, 646, DateTimeKind.Utc).AddTicks(7003));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("70ab7cff-d677-f4bd-b331-f02908ee3347"),
                column: "created_at",
                value: new DateTime(2026, 2, 11, 3, 38, 1, 646, DateTimeKind.Utc).AddTicks(6778));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("80cd50b4-7630-f5d0-1f9a-a7c480347dcf"),
                column: "created_at",
                value: new DateTime(2026, 2, 11, 3, 38, 1, 646, DateTimeKind.Utc).AddTicks(6612));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("9b07d22a-d0bf-ad27-01bf-0c8410d4b9e1"),
                column: "created_at",
                value: new DateTime(2026, 2, 11, 3, 38, 1, 646, DateTimeKind.Utc).AddTicks(6957));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("c5735376-4831-c12b-a01e-672efee6c8e3"),
                column: "created_at",
                value: new DateTime(2026, 2, 11, 3, 38, 1, 646, DateTimeKind.Utc).AddTicks(6932));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("cdaa97c0-e68f-2819-984e-63bb9dcf35a6"),
                column: "created_at",
                value: new DateTime(2026, 2, 11, 3, 38, 1, 646, DateTimeKind.Utc).AddTicks(6899));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("e1d5afac-09d6-ef55-a529-f5bf473ef103"),
                column: "created_at",
                value: new DateTime(2026, 2, 11, 3, 38, 1, 646, DateTimeKind.Utc).AddTicks(6981));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Reverse: drop search_vector, alter column back, recreate search_vector
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1 FROM information_schema.columns
                        WHERE table_schema = 'events' AND table_name = 'events' AND column_name = 'search_vector'
                    ) THEN
                        ALTER TABLE events.events DROP COLUMN search_vector;
                    END IF;

                    ALTER TABLE events.events ALTER COLUMN description TYPE character varying(2000);

                    ALTER TABLE events.events
                        ADD COLUMN search_vector tsvector
                        GENERATED ALWAYS AS (
                            setweight(to_tsvector('english', coalesce(title, '')), 'A') ||
                            setweight(to_tsvector('english', coalesce(description, '')), 'B')
                        ) STORED;

                    CREATE INDEX IF NOT EXISTS ix_events_search_vector ON events.events USING gin(search_vector);
                END $$;
            ");

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("0b9effc0-322f-8026-85c6-747e381b41e6"),
                column: "created_at",
                value: new DateTime(2026, 2, 10, 23, 13, 0, 976, DateTimeKind.Utc).AddTicks(2978));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("2d87836d-9322-d4b1-b4ec-b5b73eca9ad9"),
                column: "created_at",
                value: new DateTime(2026, 2, 10, 23, 13, 0, 976, DateTimeKind.Utc).AddTicks(3040));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("31f73d61-6c12-1252-f5ab-10d9d47eba46"),
                column: "created_at",
                value: new DateTime(2026, 2, 10, 23, 13, 0, 976, DateTimeKind.Utc).AddTicks(2869));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4de1eacb-273a-ab85-e811-d60addb4ae30"),
                column: "created_at",
                value: new DateTime(2026, 2, 10, 23, 13, 0, 976, DateTimeKind.Utc).AddTicks(3011));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4e57a1be-7a76-833e-003f-b2e3182f29f0"),
                column: "created_at",
                value: new DateTime(2026, 2, 10, 23, 13, 0, 976, DateTimeKind.Utc).AddTicks(3026));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("6313b249-2620-3e97-c1bd-f1d50814156d"),
                column: "created_at",
                value: new DateTime(2026, 2, 10, 23, 13, 0, 976, DateTimeKind.Utc).AddTicks(3123));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("70ab7cff-d677-f4bd-b331-f02908ee3347"),
                column: "created_at",
                value: new DateTime(2026, 2, 10, 23, 13, 0, 976, DateTimeKind.Utc).AddTicks(2995));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("80cd50b4-7630-f5d0-1f9a-a7c480347dcf"),
                column: "created_at",
                value: new DateTime(2026, 2, 10, 23, 13, 0, 976, DateTimeKind.Utc).AddTicks(2907));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("9b07d22a-d0bf-ad27-01bf-0c8410d4b9e1"),
                column: "created_at",
                value: new DateTime(2026, 2, 10, 23, 13, 0, 976, DateTimeKind.Utc).AddTicks(3087));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("c5735376-4831-c12b-a01e-672efee6c8e3"),
                column: "created_at",
                value: new DateTime(2026, 2, 10, 23, 13, 0, 976, DateTimeKind.Utc).AddTicks(3073));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("cdaa97c0-e68f-2819-984e-63bb9dcf35a6"),
                column: "created_at",
                value: new DateTime(2026, 2, 10, 23, 13, 0, 976, DateTimeKind.Utc).AddTicks(3056));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("e1d5afac-09d6-ef55-a529-f5bf473ef103"),
                column: "created_at",
                value: new DateTime(2026, 2, 10, 23, 13, 0, 976, DateTimeKind.Utc).AddTicks(3107));
        }
    }
}
