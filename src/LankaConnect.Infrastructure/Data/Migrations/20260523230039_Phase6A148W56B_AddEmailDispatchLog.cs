using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class Phase6A148W56B_AddEmailDispatchLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "email_dispatch_log",
                schema: "communications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    correlation_id = table.Column<Guid>(type: "uuid", nullable: false),
                    refund_request_id = table.Column<Guid>(type: "uuid", nullable: true),
                    entity_type = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    entity_id = table.Column<Guid>(type: "uuid", nullable: true),
                    template_name = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    recipient_email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    recipient_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    subject_rendered = table.Column<string>(type: "text", nullable: true),
                    payload_json = table.Column<string>(type: "jsonb", nullable: true),
                    suppressed = table.Column<bool>(type: "boolean", nullable: false),
                    suppression_reason = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    dispatched_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    provider_message_id = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    provider_status = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_email_dispatch_log", x => x.Id);
                });

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("0b9effc0-322f-8026-85c6-747e381b41e6"),
                column: "created_at",
                value: new DateTime(2026, 5, 23, 23, 0, 35, 274, DateTimeKind.Utc).AddTicks(5314));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("2d87836d-9322-d4b1-b4ec-b5b73eca9ad9"),
                column: "created_at",
                value: new DateTime(2026, 5, 23, 23, 0, 35, 274, DateTimeKind.Utc).AddTicks(5416));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("31f73d61-6c12-1252-f5ab-10d9d47eba46"),
                column: "created_at",
                value: new DateTime(2026, 5, 23, 23, 0, 35, 274, DateTimeKind.Utc).AddTicks(5240));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4de1eacb-273a-ab85-e811-d60addb4ae30"),
                column: "created_at",
                value: new DateTime(2026, 5, 23, 23, 0, 35, 274, DateTimeKind.Utc).AddTicks(5368));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4e57a1be-7a76-833e-003f-b2e3182f29f0"),
                column: "created_at",
                value: new DateTime(2026, 5, 23, 23, 0, 35, 274, DateTimeKind.Utc).AddTicks(5393));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("6313b249-2620-3e97-c1bd-f1d50814156d"),
                column: "created_at",
                value: new DateTime(2026, 5, 23, 23, 0, 35, 274, DateTimeKind.Utc).AddTicks(5592));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("70ab7cff-d677-f4bd-b331-f02908ee3347"),
                column: "created_at",
                value: new DateTime(2026, 5, 23, 23, 0, 35, 274, DateTimeKind.Utc).AddTicks(5340));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("80cd50b4-7630-f5d0-1f9a-a7c480347dcf"),
                column: "created_at",
                value: new DateTime(2026, 5, 23, 23, 0, 35, 274, DateTimeKind.Utc).AddTicks(5287));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("9b07d22a-d0bf-ad27-01bf-0c8410d4b9e1"),
                column: "created_at",
                value: new DateTime(2026, 5, 23, 23, 0, 35, 274, DateTimeKind.Utc).AddTicks(5504));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("c5735376-4831-c12b-a01e-672efee6c8e3"),
                column: "created_at",
                value: new DateTime(2026, 5, 23, 23, 0, 35, 274, DateTimeKind.Utc).AddTicks(5481));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("cdaa97c0-e68f-2819-984e-63bb9dcf35a6"),
                column: "created_at",
                value: new DateTime(2026, 5, 23, 23, 0, 35, 274, DateTimeKind.Utc).AddTicks(5441));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("e1d5afac-09d6-ef55-a529-f5bf473ef103"),
                column: "created_at",
                value: new DateTime(2026, 5, 23, 23, 0, 35, 274, DateTimeKind.Utc).AddTicks(5567));

            migrationBuilder.CreateIndex(
                name: "ix_email_dispatch_log_correlation",
                schema: "communications",
                table: "email_dispatch_log",
                column: "correlation_id");

            migrationBuilder.CreateIndex(
                name: "ix_email_dispatch_log_entity_dispatched",
                schema: "communications",
                table: "email_dispatch_log",
                columns: new[] { "entity_type", "entity_id", "dispatched_at" },
                filter: "entity_type IS NOT NULL AND entity_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_email_dispatch_log_recipient_dispatched",
                schema: "communications",
                table: "email_dispatch_log",
                columns: new[] { "recipient_email", "dispatched_at" });

            migrationBuilder.CreateIndex(
                name: "ix_email_dispatch_log_rr_dispatched",
                schema: "communications",
                table: "email_dispatch_log",
                columns: new[] { "refund_request_id", "dispatched_at" },
                filter: "refund_request_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "ix_email_dispatch_log_template_dispatched",
                schema: "communications",
                table: "email_dispatch_log",
                columns: new[] { "template_name", "dispatched_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "email_dispatch_log",
                schema: "communications");

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("0b9effc0-322f-8026-85c6-747e381b41e6"),
                column: "created_at",
                value: new DateTime(2026, 5, 22, 23, 30, 45, 402, DateTimeKind.Utc).AddTicks(1519));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("2d87836d-9322-d4b1-b4ec-b5b73eca9ad9"),
                column: "created_at",
                value: new DateTime(2026, 5, 22, 23, 30, 45, 402, DateTimeKind.Utc).AddTicks(1585));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("31f73d61-6c12-1252-f5ab-10d9d47eba46"),
                column: "created_at",
                value: new DateTime(2026, 5, 22, 23, 30, 45, 402, DateTimeKind.Utc).AddTicks(1477));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4de1eacb-273a-ab85-e811-d60addb4ae30"),
                column: "created_at",
                value: new DateTime(2026, 5, 22, 23, 30, 45, 402, DateTimeKind.Utc).AddTicks(1548));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4e57a1be-7a76-833e-003f-b2e3182f29f0"),
                column: "created_at",
                value: new DateTime(2026, 5, 22, 23, 30, 45, 402, DateTimeKind.Utc).AddTicks(1561));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("6313b249-2620-3e97-c1bd-f1d50814156d"),
                column: "created_at",
                value: new DateTime(2026, 5, 22, 23, 30, 45, 402, DateTimeKind.Utc).AddTicks(1656));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("70ab7cff-d677-f4bd-b331-f02908ee3347"),
                column: "created_at",
                value: new DateTime(2026, 5, 22, 23, 30, 45, 402, DateTimeKind.Utc).AddTicks(1534));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("80cd50b4-7630-f5d0-1f9a-a7c480347dcf"),
                column: "created_at",
                value: new DateTime(2026, 5, 22, 23, 30, 45, 402, DateTimeKind.Utc).AddTicks(1504));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("9b07d22a-d0bf-ad27-01bf-0c8410d4b9e1"),
                column: "created_at",
                value: new DateTime(2026, 5, 22, 23, 30, 45, 402, DateTimeKind.Utc).AddTicks(1631));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("c5735376-4831-c12b-a01e-672efee6c8e3"),
                column: "created_at",
                value: new DateTime(2026, 5, 22, 23, 30, 45, 402, DateTimeKind.Utc).AddTicks(1618));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("cdaa97c0-e68f-2819-984e-63bb9dcf35a6"),
                column: "created_at",
                value: new DateTime(2026, 5, 22, 23, 30, 45, 402, DateTimeKind.Utc).AddTicks(1600));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("e1d5afac-09d6-ef55-a529-f5bf473ef103"),
                column: "created_at",
                value: new DateTime(2026, 5, 22, 23, 30, 45, 402, DateTimeKind.Utc).AddTicks(1643));
        }
    }
}
