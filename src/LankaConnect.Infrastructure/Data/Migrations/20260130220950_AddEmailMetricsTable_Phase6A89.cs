using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailMetricsTable_Phase6A89 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "email_metrics",
                schema: "communications",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    metric_date = table.Column<DateOnly>(type: "date", nullable: false),
                    template_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    total_sent = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    successful = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    failed = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    avg_duration_ms = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    total_duration_ms = table.Column<long>(type: "bigint", nullable: false, defaultValue: 0L),
                    validation_failures = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    template_not_found_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_email_metrics", x => x.id);
                });

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("0b9effc0-322f-8026-85c6-747e381b41e6"),
                column: "created_at",
                value: new DateTime(2026, 1, 30, 22, 9, 48, 450, DateTimeKind.Utc).AddTicks(7376));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("2d87836d-9322-d4b1-b4ec-b5b73eca9ad9"),
                column: "created_at",
                value: new DateTime(2026, 1, 30, 22, 9, 48, 450, DateTimeKind.Utc).AddTicks(7516));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("31f73d61-6c12-1252-f5ab-10d9d47eba46"),
                column: "created_at",
                value: new DateTime(2026, 1, 30, 22, 9, 48, 450, DateTimeKind.Utc).AddTicks(7327));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4de1eacb-273a-ab85-e811-d60addb4ae30"),
                column: "created_at",
                value: new DateTime(2026, 1, 30, 22, 9, 48, 450, DateTimeKind.Utc).AddTicks(7405));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4e57a1be-7a76-833e-003f-b2e3182f29f0"),
                column: "created_at",
                value: new DateTime(2026, 1, 30, 22, 9, 48, 450, DateTimeKind.Utc).AddTicks(7419));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("6313b249-2620-3e97-c1bd-f1d50814156d"),
                column: "created_at",
                value: new DateTime(2026, 1, 30, 22, 9, 48, 450, DateTimeKind.Utc).AddTicks(7585));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("70ab7cff-d677-f4bd-b331-f02908ee3347"),
                column: "created_at",
                value: new DateTime(2026, 1, 30, 22, 9, 48, 450, DateTimeKind.Utc).AddTicks(7391));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("80cd50b4-7630-f5d0-1f9a-a7c480347dcf"),
                column: "created_at",
                value: new DateTime(2026, 1, 30, 22, 9, 48, 450, DateTimeKind.Utc).AddTicks(7358));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("9b07d22a-d0bf-ad27-01bf-0c8410d4b9e1"),
                column: "created_at",
                value: new DateTime(2026, 1, 30, 22, 9, 48, 450, DateTimeKind.Utc).AddTicks(7559));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("c5735376-4831-c12b-a01e-672efee6c8e3"),
                column: "created_at",
                value: new DateTime(2026, 1, 30, 22, 9, 48, 450, DateTimeKind.Utc).AddTicks(7546));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("cdaa97c0-e68f-2819-984e-63bb9dcf35a6"),
                column: "created_at",
                value: new DateTime(2026, 1, 30, 22, 9, 48, 450, DateTimeKind.Utc).AddTicks(7532));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("e1d5afac-09d6-ef55-a529-f5bf473ef103"),
                column: "created_at",
                value: new DateTime(2026, 1, 30, 22, 9, 48, 450, DateTimeKind.Utc).AddTicks(7572));

            migrationBuilder.CreateIndex(
                name: "IX_EmailMetrics_CreatedAt",
                schema: "communications",
                table: "email_metrics",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "IX_EmailMetrics_Date",
                schema: "communications",
                table: "email_metrics",
                column: "metric_date");

            migrationBuilder.CreateIndex(
                name: "IX_EmailMetrics_Date_Template",
                schema: "communications",
                table: "email_metrics",
                columns: new[] { "metric_date", "template_name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmailMetrics_Template",
                schema: "communications",
                table: "email_metrics",
                column: "template_name");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "email_metrics",
                schema: "communications");

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("0b9effc0-322f-8026-85c6-747e381b41e6"),
                column: "created_at",
                value: new DateTime(2026, 1, 28, 5, 39, 3, 598, DateTimeKind.Utc).AddTicks(2824));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("2d87836d-9322-d4b1-b4ec-b5b73eca9ad9"),
                column: "created_at",
                value: new DateTime(2026, 1, 28, 5, 39, 3, 598, DateTimeKind.Utc).AddTicks(2974));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("31f73d61-6c12-1252-f5ab-10d9d47eba46"),
                column: "created_at",
                value: new DateTime(2026, 1, 28, 5, 39, 3, 598, DateTimeKind.Utc).AddTicks(2679));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4de1eacb-273a-ab85-e811-d60addb4ae30"),
                column: "created_at",
                value: new DateTime(2026, 1, 28, 5, 39, 3, 598, DateTimeKind.Utc).AddTicks(2900));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4e57a1be-7a76-833e-003f-b2e3182f29f0"),
                column: "created_at",
                value: new DateTime(2026, 1, 28, 5, 39, 3, 598, DateTimeKind.Utc).AddTicks(2938));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("6313b249-2620-3e97-c1bd-f1d50814156d"),
                column: "created_at",
                value: new DateTime(2026, 1, 28, 5, 39, 3, 598, DateTimeKind.Utc).AddTicks(3155));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("70ab7cff-d677-f4bd-b331-f02908ee3347"),
                column: "created_at",
                value: new DateTime(2026, 1, 28, 5, 39, 3, 598, DateTimeKind.Utc).AddTicks(2861));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("80cd50b4-7630-f5d0-1f9a-a7c480347dcf"),
                column: "created_at",
                value: new DateTime(2026, 1, 28, 5, 39, 3, 598, DateTimeKind.Utc).AddTicks(2780));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("9b07d22a-d0bf-ad27-01bf-0c8410d4b9e1"),
                column: "created_at",
                value: new DateTime(2026, 1, 28, 5, 39, 3, 598, DateTimeKind.Utc).AddTicks(3082));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("c5735376-4831-c12b-a01e-672efee6c8e3"),
                column: "created_at",
                value: new DateTime(2026, 1, 28, 5, 39, 3, 598, DateTimeKind.Utc).AddTicks(3047));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("cdaa97c0-e68f-2819-984e-63bb9dcf35a6"),
                column: "created_at",
                value: new DateTime(2026, 1, 28, 5, 39, 3, 598, DateTimeKind.Utc).AddTicks(3011));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("e1d5afac-09d6-ef55-a529-f5bf473ef103"),
                column: "created_at",
                value: new DateTime(2026, 1, 28, 5, 39, 3, 598, DateTimeKind.Utc).AddTicks(3119));
        }
    }
}
