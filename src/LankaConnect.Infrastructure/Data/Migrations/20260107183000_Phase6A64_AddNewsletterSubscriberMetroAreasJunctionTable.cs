using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class Phase6A64_AddNewsletterSubscriberMetroAreasJunctionTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Phase 6A.64: Create junction table for many-to-many relationship between NewsletterSubscriber and MetroArea
            // This fixes issue where UI allows selecting multiple metro areas (e.g., all 5 Ohio metro areas)
            // but database schema only stored a single metro_area_id value

            // Step 1: Create the junction table
            migrationBuilder.CreateTable(
                name: "newsletter_subscriber_metro_areas",
                schema: "communications",
                columns: table => new
                {
                    subscriber_id = table.Column<Guid>(type: "uuid", nullable: false),
                    metro_area_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_newsletter_subscriber_metro_areas", x => new { x.subscriber_id, x.metro_area_id });
                    table.ForeignKey(
                        name: "fk_newsletter_subscriber_metro_areas_newsletter_subscribers",
                        column: x => x.subscriber_id,
                        principalSchema: "communications",
                        principalTable: "newsletter_subscribers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_newsletter_subscriber_metro_areas_metro_areas",
                        column: x => x.metro_area_id,
                        principalSchema: "events",
                        principalTable: "metro_areas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Step 2: Create indexes for query performance
            migrationBuilder.CreateIndex(
                name: "ix_newsletter_subscriber_metro_areas_metro_area_id",
                schema: "communications",
                table: "newsletter_subscriber_metro_areas",
                column: "metro_area_id");

            migrationBuilder.CreateIndex(
                name: "ix_newsletter_subscriber_metro_areas_subscriber_id",
                schema: "communications",
                table: "newsletter_subscriber_metro_areas",
                column: "subscriber_id");

            // Step 3: Migrate existing data from newsletter_subscribers.metro_area_id to junction table
            // Only migrate records where metro_area_id is NOT NULL AND exists in metro_areas table (to avoid FK violation)
            migrationBuilder.Sql(@"
                INSERT INTO communications.newsletter_subscriber_metro_areas (subscriber_id, metro_area_id, created_at)
                SELECT ns.id, ns.metro_area_id, ns.created_at
                FROM communications.newsletter_subscribers ns
                INNER JOIN events.metro_areas ma ON ns.metro_area_id = ma.id
                WHERE ns.metro_area_id IS NOT NULL;
            ");

            // Step 4: Drop the old metro_area_id column (no longer needed with junction table)
            // Note: We keep receive_all_locations column as it serves a different purpose
            migrationBuilder.DropColumn(
                name: "metro_area_id",
                schema: "communications",
                table: "newsletter_subscribers");

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("0b9effc0-322f-8026-85c6-747e381b41e6"),
                column: "created_at",
                value: new DateTime(2026, 1, 7, 18, 29, 55, 894, DateTimeKind.Utc).AddTicks(9540));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("2d87836d-9322-d4b1-b4ec-b5b73eca9ad9"),
                column: "created_at",
                value: new DateTime(2026, 1, 7, 18, 29, 55, 894, DateTimeKind.Utc).AddTicks(9656));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("31f73d61-6c12-1252-f5ab-10d9d47eba46"),
                column: "created_at",
                value: new DateTime(2026, 1, 7, 18, 29, 55, 894, DateTimeKind.Utc).AddTicks(9429));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4de1eacb-273a-ab85-e811-d60addb4ae30"),
                column: "created_at",
                value: new DateTime(2026, 1, 7, 18, 29, 55, 894, DateTimeKind.Utc).AddTicks(9598));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4e57a1be-7a76-833e-003f-b2e3182f29f0"),
                column: "created_at",
                value: new DateTime(2026, 1, 7, 18, 29, 55, 894, DateTimeKind.Utc).AddTicks(9628));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("6313b249-2620-3e97-c1bd-f1d50814156d"),
                column: "created_at",
                value: new DateTime(2026, 1, 7, 18, 29, 55, 894, DateTimeKind.Utc).AddTicks(9825));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("70ab7cff-d677-f4bd-b331-f02908ee3347"),
                column: "created_at",
                value: new DateTime(2026, 1, 7, 18, 29, 55, 894, DateTimeKind.Utc).AddTicks(9569));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("80cd50b4-7630-f5d0-1f9a-a7c480347dcf"),
                column: "created_at",
                value: new DateTime(2026, 1, 7, 18, 29, 55, 894, DateTimeKind.Utc).AddTicks(9487));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("9b07d22a-d0bf-ad27-01bf-0c8410d4b9e1"),
                column: "created_at",
                value: new DateTime(2026, 1, 7, 18, 29, 55, 894, DateTimeKind.Utc).AddTicks(9769));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("c5735376-4831-c12b-a01e-672efee6c8e3"),
                column: "created_at",
                value: new DateTime(2026, 1, 7, 18, 29, 55, 894, DateTimeKind.Utc).AddTicks(9739));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("cdaa97c0-e68f-2819-984e-63bb9dcf35a6"),
                column: "created_at",
                value: new DateTime(2026, 1, 7, 18, 29, 55, 894, DateTimeKind.Utc).AddTicks(9705));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("e1d5afac-09d6-ef55-a529-f5bf473ef103"),
                column: "created_at",
                value: new DateTime(2026, 1, 7, 18, 29, 55, 894, DateTimeKind.Utc).AddTicks(9797));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Phase 6A.64 Rollback: Restore single metro_area_id column
            // WARNING: This will result in data loss if a subscriber has multiple metro areas
            // Only the first metro area will be restored

            // Step 1: Re-add the metro_area_id column
            migrationBuilder.AddColumn<Guid>(
                name: "metro_area_id",
                schema: "communications",
                table: "newsletter_subscribers",
                type: "uuid",
                nullable: true);

            // Step 2: Migrate data back (keeping only first metro area per subscriber)
            migrationBuilder.Sql(@"
                UPDATE communications.newsletter_subscribers ns
                SET metro_area_id = (
                    SELECT metro_area_id
                    FROM communications.newsletter_subscriber_metro_areas
                    WHERE subscriber_id = ns.id
                    ORDER BY created_at
                    LIMIT 1
                );
            ");

            // Step 3: Drop the junction table indexes
            migrationBuilder.DropIndex(
                name: "ix_newsletter_subscriber_metro_areas_metro_area_id",
                schema: "communications",
                table: "newsletter_subscriber_metro_areas");

            migrationBuilder.DropIndex(
                name: "ix_newsletter_subscriber_metro_areas_subscriber_id",
                schema: "communications",
                table: "newsletter_subscriber_metro_areas");

            // Step 4: Drop the junction table
            migrationBuilder.DropTable(
                name: "newsletter_subscriber_metro_areas",
                schema: "communications");

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("0b9effc0-322f-8026-85c6-747e381b41e6"),
                column: "created_at",
                value: new DateTime(2026, 1, 7, 4, 39, 29, 900, DateTimeKind.Utc).AddTicks(784));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("2d87836d-9322-d4b1-b4ec-b5b73eca9ad9"),
                column: "created_at",
                value: new DateTime(2026, 1, 7, 4, 39, 29, 900, DateTimeKind.Utc).AddTicks(861));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("31f73d61-6c12-1252-f5ab-10d9d47eba46"),
                column: "created_at",
                value: new DateTime(2026, 1, 7, 4, 39, 29, 900, DateTimeKind.Utc).AddTicks(727));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4de1eacb-273a-ab85-e811-d60addb4ae30"),
                column: "created_at",
                value: new DateTime(2026, 1, 7, 4, 39, 29, 900, DateTimeKind.Utc).AddTicks(830));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4e57a1be-7a76-833e-003f-b2e3182f29f0"),
                column: "created_at",
                value: new DateTime(2026, 1, 7, 4, 39, 29, 900, DateTimeKind.Utc).AddTicks(846));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("6313b249-2620-3e97-c1bd-f1d50814156d"),
                column: "created_at",
                value: new DateTime(2026, 1, 7, 4, 39, 29, 900, DateTimeKind.Utc).AddTicks(935));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("70ab7cff-d677-f4bd-b331-f02908ee3347"),
                column: "created_at",
                value: new DateTime(2026, 1, 7, 4, 39, 29, 900, DateTimeKind.Utc).AddTicks(813));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("80cd50b4-7630-f5d0-1f9a-a7c480347dcf"),
                column: "created_at",
                value: new DateTime(2026, 1, 7, 4, 39, 29, 900, DateTimeKind.Utc).AddTicks(764));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("9b07d22a-d0bf-ad27-01bf-0c8410d4b9e1"),
                column: "created_at",
                value: new DateTime(2026, 1, 7, 4, 39, 29, 900, DateTimeKind.Utc).AddTicks(907));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("c5735376-4831-c12b-a01e-672efee6c8e3"),
                column: "created_at",
                value: new DateTime(2026, 1, 7, 4, 39, 29, 900, DateTimeKind.Utc).AddTicks(892));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("cdaa97c0-e68f-2819-984e-63bb9dcf35a6"),
                column: "created_at",
                value: new DateTime(2026, 1, 7, 4, 39, 29, 900, DateTimeKind.Utc).AddTicks(876));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("e1d5afac-09d6-ef55-a529-f5bf473ef103"),
                column: "created_at",
                value: new DateTime(2026, 1, 7, 4, 39, 29, 900, DateTimeKind.Utc).AddTicks(921));
        }
    }
}
