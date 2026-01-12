using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class Phase6A74Part3C_AddNewsletterTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "newsletters",
                schema: "communications",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: false),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_id = table.Column<Guid>(type: "uuid", nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Draft"),
                    published_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    sent_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    include_newsletter_subscribers = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    target_all_locations = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    version = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_newsletters", x => x.id);
                    table.ForeignKey(
                        name: "FK_newsletters_events_event_id",
                        column: x => x.event_id,
                        principalSchema: "events",
                        principalTable: "events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_newsletters_users_created_by_user_id",
                        column: x => x.created_by_user_id,
                        principalSchema: "identity",
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "newsletter_email_groups",
                schema: "communications",
                columns: table => new
                {
                    newsletter_id = table.Column<Guid>(type: "uuid", nullable: false),
                    email_group_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_newsletter_email_groups", x => new { x.newsletter_id, x.email_group_id });
                    table.ForeignKey(
                        name: "FK_newsletter_email_groups_email_groups_email_group_id",
                        column: x => x.email_group_id,
                        principalSchema: "communications",
                        principalTable: "email_groups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_newsletter_email_groups_newsletters_newsletter_id",
                        column: x => x.newsletter_id,
                        principalSchema: "communications",
                        principalTable: "newsletters",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "newsletter_metro_areas",
                schema: "communications",
                columns: table => new
                {
                    newsletter_id = table.Column<Guid>(type: "uuid", nullable: false),
                    metro_area_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_newsletter_metro_areas", x => new { x.newsletter_id, x.metro_area_id });
                    table.ForeignKey(
                        name: "FK_newsletter_metro_areas_metro_areas_metro_area_id",
                        column: x => x.metro_area_id,
                        principalSchema: "events",
                        principalTable: "metro_areas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_newsletter_metro_areas_newsletters_newsletter_id",
                        column: x => x.newsletter_id,
                        principalSchema: "communications",
                        principalTable: "newsletters",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("0b9effc0-322f-8026-85c6-747e381b41e6"),
                column: "created_at",
                value: new DateTime(2026, 1, 12, 4, 0, 32, 946, DateTimeKind.Utc).AddTicks(2488));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("2d87836d-9322-d4b1-b4ec-b5b73eca9ad9"),
                column: "created_at",
                value: new DateTime(2026, 1, 12, 4, 0, 32, 946, DateTimeKind.Utc).AddTicks(2581));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("31f73d61-6c12-1252-f5ab-10d9d47eba46"),
                column: "created_at",
                value: new DateTime(2026, 1, 12, 4, 0, 32, 946, DateTimeKind.Utc).AddTicks(2422));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4de1eacb-273a-ab85-e811-d60addb4ae30"),
                column: "created_at",
                value: new DateTime(2026, 1, 12, 4, 0, 32, 946, DateTimeKind.Utc).AddTicks(2540));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4e57a1be-7a76-833e-003f-b2e3182f29f0"),
                column: "created_at",
                value: new DateTime(2026, 1, 12, 4, 0, 32, 946, DateTimeKind.Utc).AddTicks(2563));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("6313b249-2620-3e97-c1bd-f1d50814156d"),
                column: "created_at",
                value: new DateTime(2026, 1, 12, 4, 0, 32, 946, DateTimeKind.Utc).AddTicks(2800));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("70ab7cff-d677-f4bd-b331-f02908ee3347"),
                column: "created_at",
                value: new DateTime(2026, 1, 12, 4, 0, 32, 946, DateTimeKind.Utc).AddTicks(2517));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("80cd50b4-7630-f5d0-1f9a-a7c480347dcf"),
                column: "created_at",
                value: new DateTime(2026, 1, 12, 4, 0, 32, 946, DateTimeKind.Utc).AddTicks(2462));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("9b07d22a-d0bf-ad27-01bf-0c8410d4b9e1"),
                column: "created_at",
                value: new DateTime(2026, 1, 12, 4, 0, 32, 946, DateTimeKind.Utc).AddTicks(2731));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("c5735376-4831-c12b-a01e-672efee6c8e3"),
                column: "created_at",
                value: new DateTime(2026, 1, 12, 4, 0, 32, 946, DateTimeKind.Utc).AddTicks(2711));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("cdaa97c0-e68f-2819-984e-63bb9dcf35a6"),
                column: "created_at",
                value: new DateTime(2026, 1, 12, 4, 0, 32, 946, DateTimeKind.Utc).AddTicks(2600));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("e1d5afac-09d6-ef55-a529-f5bf473ef103"),
                column: "created_at",
                value: new DateTime(2026, 1, 12, 4, 0, 32, 946, DateTimeKind.Utc).AddTicks(2775));

            migrationBuilder.CreateIndex(
                name: "IX_newsletter_email_groups_email_group_id",
                schema: "communications",
                table: "newsletter_email_groups",
                column: "email_group_id");

            migrationBuilder.CreateIndex(
                name: "IX_newsletter_email_groups_newsletter_id",
                schema: "communications",
                table: "newsletter_email_groups",
                column: "newsletter_id");

            migrationBuilder.CreateIndex(
                name: "IX_newsletter_metro_areas_metro_area_id",
                schema: "communications",
                table: "newsletter_metro_areas",
                column: "metro_area_id");

            migrationBuilder.CreateIndex(
                name: "IX_newsletter_metro_areas_newsletter_id",
                schema: "communications",
                table: "newsletter_metro_areas",
                column: "newsletter_id");

            migrationBuilder.CreateIndex(
                name: "ix_newsletters_created_by_user_id",
                schema: "communications",
                table: "newsletters",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_newsletters_event_id",
                schema: "communications",
                table: "newsletters",
                column: "event_id");

            migrationBuilder.CreateIndex(
                name: "ix_newsletters_expires_at",
                schema: "communications",
                table: "newsletters",
                column: "expires_at");

            migrationBuilder.CreateIndex(
                name: "ix_newsletters_status",
                schema: "communications",
                table: "newsletters",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_newsletters_status_published_at",
                schema: "communications",
                table: "newsletters",
                columns: new[] { "status", "published_at" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "newsletter_email_groups",
                schema: "communications");

            migrationBuilder.DropTable(
                name: "newsletter_metro_areas",
                schema: "communications");

            migrationBuilder.DropTable(
                name: "newsletters",
                schema: "communications");

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("0b9effc0-322f-8026-85c6-747e381b41e6"),
                column: "created_at",
                value: new DateTime(2026, 1, 12, 1, 46, 4, 171, DateTimeKind.Utc).AddTicks(8169));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("2d87836d-9322-d4b1-b4ec-b5b73eca9ad9"),
                column: "created_at",
                value: new DateTime(2026, 1, 12, 1, 46, 4, 171, DateTimeKind.Utc).AddTicks(8260));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("31f73d61-6c12-1252-f5ab-10d9d47eba46"),
                column: "created_at",
                value: new DateTime(2026, 1, 12, 1, 46, 4, 171, DateTimeKind.Utc).AddTicks(8099));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4de1eacb-273a-ab85-e811-d60addb4ae30"),
                column: "created_at",
                value: new DateTime(2026, 1, 12, 1, 46, 4, 171, DateTimeKind.Utc).AddTicks(8214));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4e57a1be-7a76-833e-003f-b2e3182f29f0"),
                column: "created_at",
                value: new DateTime(2026, 1, 12, 1, 46, 4, 171, DateTimeKind.Utc).AddTicks(8237));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("6313b249-2620-3e97-c1bd-f1d50814156d"),
                column: "created_at",
                value: new DateTime(2026, 1, 12, 1, 46, 4, 171, DateTimeKind.Utc).AddTicks(8471));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("70ab7cff-d677-f4bd-b331-f02908ee3347"),
                column: "created_at",
                value: new DateTime(2026, 1, 12, 1, 46, 4, 171, DateTimeKind.Utc).AddTicks(8191));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("80cd50b4-7630-f5d0-1f9a-a7c480347dcf"),
                column: "created_at",
                value: new DateTime(2026, 1, 12, 1, 46, 4, 171, DateTimeKind.Utc).AddTicks(8144));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("9b07d22a-d0bf-ad27-01bf-0c8410d4b9e1"),
                column: "created_at",
                value: new DateTime(2026, 1, 12, 1, 46, 4, 171, DateTimeKind.Utc).AddTicks(8321));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("c5735376-4831-c12b-a01e-672efee6c8e3"),
                column: "created_at",
                value: new DateTime(2026, 1, 12, 1, 46, 4, 171, DateTimeKind.Utc).AddTicks(8301));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("cdaa97c0-e68f-2819-984e-63bb9dcf35a6"),
                column: "created_at",
                value: new DateTime(2026, 1, 12, 1, 46, 4, 171, DateTimeKind.Utc).AddTicks(8280));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("e1d5afac-09d6-ef55-a529-f5bf473ef103"),
                column: "created_at",
                value: new DateTime(2026, 1, 12, 1, 46, 4, 171, DateTimeKind.Utc).AddTicks(8340));
        }
    }
}
