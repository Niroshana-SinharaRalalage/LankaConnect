using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.SPLIT_PER_ENTITY.Migrations
{
    /// <inheritdoc />
    public partial class Phase7FB_AddRegistrationModeConversionAuditTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "registration_mode_conversions",
                schema: "events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    organiser_id = table.Column<Guid>(type: "uuid", nullable: false),
                    from_mode = table.Column<short>(type: "smallint", nullable: false),
                    to_mode = table.Column<short>(type: "smallint", nullable: false),
                    started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    total_count = table.Column<int>(type: "integer", nullable: false),
                    migrated_count = table.Column<int>(type: "integer", nullable: false),
                    skipped_count = table.Column<int>(type: "integer", nullable: false),
                    failed_count = table.Column<int>(type: "integer", nullable: false),
                    event_row_version_snapshot = table.Column<byte[]>(type: "bytea", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_registration_mode_conversions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "registration_mode_conversion_rows",
                schema: "events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    aggregate_conversion_id = table.Column<Guid>(type: "uuid", nullable: false),
                    registration_id = table.Column<Guid>(type: "uuid", nullable: false),
                    conversion_outcome = table.Column<short>(type: "smallint", nullable: false),
                    outcome_reason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    registration_row_version_snapshot = table.Column<byte[]>(type: "bytea", nullable: true),
                    before_shape = table.Column<string>(type: "jsonb", nullable: true),
                    after_shape = table.Column<string>(type: "jsonb", nullable: true),
                    converted_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_registration_mode_conversion_rows", x => x.Id);
                    table.ForeignKey(
                        name: "FK_registration_mode_conversion_rows_registration_mode_convers~",
                        column: x => x.aggregate_conversion_id,
                        principalSchema: "events",
                        principalTable: "registration_mode_conversions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("0b9effc0-322f-8026-85c6-747e381b41e6"),
                column: "created_at",
                value: new DateTime(2026, 5, 1, 3, 2, 35, 192, DateTimeKind.Utc).AddTicks(8768));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("2d87836d-9322-d4b1-b4ec-b5b73eca9ad9"),
                column: "created_at",
                value: new DateTime(2026, 5, 1, 3, 2, 35, 192, DateTimeKind.Utc).AddTicks(8866));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("31f73d61-6c12-1252-f5ab-10d9d47eba46"),
                column: "created_at",
                value: new DateTime(2026, 5, 1, 3, 2, 35, 192, DateTimeKind.Utc).AddTicks(8700));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4de1eacb-273a-ab85-e811-d60addb4ae30"),
                column: "created_at",
                value: new DateTime(2026, 5, 1, 3, 2, 35, 192, DateTimeKind.Utc).AddTicks(8811));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4e57a1be-7a76-833e-003f-b2e3182f29f0"),
                column: "created_at",
                value: new DateTime(2026, 5, 1, 3, 2, 35, 192, DateTimeKind.Utc).AddTicks(8831));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("6313b249-2620-3e97-c1bd-f1d50814156d"),
                column: "created_at",
                value: new DateTime(2026, 5, 1, 3, 2, 35, 192, DateTimeKind.Utc).AddTicks(9069));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("70ab7cff-d677-f4bd-b331-f02908ee3347"),
                column: "created_at",
                value: new DateTime(2026, 5, 1, 3, 2, 35, 192, DateTimeKind.Utc).AddTicks(8789));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("80cd50b4-7630-f5d0-1f9a-a7c480347dcf"),
                column: "created_at",
                value: new DateTime(2026, 5, 1, 3, 2, 35, 192, DateTimeKind.Utc).AddTicks(8740));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("9b07d22a-d0bf-ad27-01bf-0c8410d4b9e1"),
                column: "created_at",
                value: new DateTime(2026, 5, 1, 3, 2, 35, 192, DateTimeKind.Utc).AddTicks(8943));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("c5735376-4831-c12b-a01e-672efee6c8e3"),
                column: "created_at",
                value: new DateTime(2026, 5, 1, 3, 2, 35, 192, DateTimeKind.Utc).AddTicks(8920));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("cdaa97c0-e68f-2819-984e-63bb9dcf35a6"),
                column: "created_at",
                value: new DateTime(2026, 5, 1, 3, 2, 35, 192, DateTimeKind.Utc).AddTicks(8888));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("e1d5afac-09d6-ef55-a529-f5bf473ef103"),
                column: "created_at",
                value: new DateTime(2026, 5, 1, 3, 2, 35, 192, DateTimeKind.Utc).AddTicks(8962));

            migrationBuilder.CreateIndex(
                name: "ix_registration_mode_conversion_rows_aggregate_id",
                schema: "events",
                table: "registration_mode_conversion_rows",
                column: "aggregate_conversion_id");

            migrationBuilder.CreateIndex(
                name: "ix_registration_mode_conversion_rows_registration_id",
                schema: "events",
                table: "registration_mode_conversion_rows",
                column: "registration_id");

            migrationBuilder.CreateIndex(
                name: "ix_registration_mode_conversions_event_id",
                schema: "events",
                table: "registration_mode_conversions",
                column: "event_id");

            migrationBuilder.CreateIndex(
                name: "ix_registration_mode_conversions_organiser_id",
                schema: "events",
                table: "registration_mode_conversions",
                column: "organiser_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "registration_mode_conversion_rows",
                schema: "events");

            migrationBuilder.DropTable(
                name: "registration_mode_conversions",
                schema: "events");

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("0b9effc0-322f-8026-85c6-747e381b41e6"),
                column: "created_at",
                value: new DateTime(2026, 4, 30, 3, 10, 56, 940, DateTimeKind.Utc).AddTicks(1405));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("2d87836d-9322-d4b1-b4ec-b5b73eca9ad9"),
                column: "created_at",
                value: new DateTime(2026, 4, 30, 3, 10, 56, 940, DateTimeKind.Utc).AddTicks(1502));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("31f73d61-6c12-1252-f5ab-10d9d47eba46"),
                column: "created_at",
                value: new DateTime(2026, 4, 30, 3, 10, 56, 940, DateTimeKind.Utc).AddTicks(1346));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4de1eacb-273a-ab85-e811-d60addb4ae30"),
                column: "created_at",
                value: new DateTime(2026, 4, 30, 3, 10, 56, 940, DateTimeKind.Utc).AddTicks(1437));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4e57a1be-7a76-833e-003f-b2e3182f29f0"),
                column: "created_at",
                value: new DateTime(2026, 4, 30, 3, 10, 56, 940, DateTimeKind.Utc).AddTicks(1454));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("6313b249-2620-3e97-c1bd-f1d50814156d"),
                column: "created_at",
                value: new DateTime(2026, 4, 30, 3, 10, 56, 940, DateTimeKind.Utc).AddTicks(1586));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("70ab7cff-d677-f4bd-b331-f02908ee3347"),
                column: "created_at",
                value: new DateTime(2026, 4, 30, 3, 10, 56, 940, DateTimeKind.Utc).AddTicks(1421));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("80cd50b4-7630-f5d0-1f9a-a7c480347dcf"),
                column: "created_at",
                value: new DateTime(2026, 4, 30, 3, 10, 56, 940, DateTimeKind.Utc).AddTicks(1387));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("9b07d22a-d0bf-ad27-01bf-0c8410d4b9e1"),
                column: "created_at",
                value: new DateTime(2026, 4, 30, 3, 10, 56, 940, DateTimeKind.Utc).AddTicks(1556));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("c5735376-4831-c12b-a01e-672efee6c8e3"),
                column: "created_at",
                value: new DateTime(2026, 4, 30, 3, 10, 56, 940, DateTimeKind.Utc).AddTicks(1541));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("cdaa97c0-e68f-2819-984e-63bb9dcf35a6"),
                column: "created_at",
                value: new DateTime(2026, 4, 30, 3, 10, 56, 940, DateTimeKind.Utc).AddTicks(1522));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("e1d5afac-09d6-ef55-a529-f5bf473ef103"),
                column: "created_at",
                value: new DateTime(2026, 4, 30, 3, 10, 56, 940, DateTimeKind.Utc).AddTicks(1571));
        }
    }
}
