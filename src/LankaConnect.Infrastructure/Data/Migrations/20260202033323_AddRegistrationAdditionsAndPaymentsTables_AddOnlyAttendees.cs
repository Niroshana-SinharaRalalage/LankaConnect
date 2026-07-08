using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddRegistrationAdditionsAndPaymentsTables_AddOnlyAttendees : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "registration_additions",
                schema: "events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    registration_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    previous_total_price_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    previous_total_price_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    new_total_price_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    new_total_price_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    additional_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    additional_amount_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    stripe_checkout_session_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    stripe_payment_intent_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    checkout_expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    payment_completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    merged_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    failed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    abandoned_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    new_attendees = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_registration_additions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_registration_additions_events_event_id",
                        column: x => x.event_id,
                        principalSchema: "events",
                        principalTable: "events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_registration_additions_registrations_registration_id",
                        column: x => x.registration_id,
                        principalSchema: "events",
                        principalTable: "registrations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "registration_payments",
                schema: "events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    registration_id = table.Column<Guid>(type: "uuid", nullable: false),
                    stripe_payment_intent_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    amount_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    registration_addition_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_registration_payments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_registration_payments_registration_additions_registration_a~",
                        column: x => x.registration_addition_id,
                        principalSchema: "events",
                        principalTable: "registration_additions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_registration_payments_registrations_registration_id",
                        column: x => x.registration_id,
                        principalSchema: "events",
                        principalTable: "registrations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("0b9effc0-322f-8026-85c6-747e381b41e6"),
                column: "created_at",
                value: new DateTime(2026, 2, 2, 3, 33, 19, 513, DateTimeKind.Utc).AddTicks(9242));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("2d87836d-9322-d4b1-b4ec-b5b73eca9ad9"),
                column: "created_at",
                value: new DateTime(2026, 2, 2, 3, 33, 19, 513, DateTimeKind.Utc).AddTicks(9328));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("31f73d61-6c12-1252-f5ab-10d9d47eba46"),
                column: "created_at",
                value: new DateTime(2026, 2, 2, 3, 33, 19, 513, DateTimeKind.Utc).AddTicks(9156));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4de1eacb-273a-ab85-e811-d60addb4ae30"),
                column: "created_at",
                value: new DateTime(2026, 2, 2, 3, 33, 19, 513, DateTimeKind.Utc).AddTicks(9285));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4e57a1be-7a76-833e-003f-b2e3182f29f0"),
                column: "created_at",
                value: new DateTime(2026, 2, 2, 3, 33, 19, 513, DateTimeKind.Utc).AddTicks(9309));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("6313b249-2620-3e97-c1bd-f1d50814156d"),
                column: "created_at",
                value: new DateTime(2026, 2, 2, 3, 33, 19, 513, DateTimeKind.Utc).AddTicks(9552));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("70ab7cff-d677-f4bd-b331-f02908ee3347"),
                column: "created_at",
                value: new DateTime(2026, 2, 2, 3, 33, 19, 513, DateTimeKind.Utc).AddTicks(9264));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("80cd50b4-7630-f5d0-1f9a-a7c480347dcf"),
                column: "created_at",
                value: new DateTime(2026, 2, 2, 3, 33, 19, 513, DateTimeKind.Utc).AddTicks(9201));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("9b07d22a-d0bf-ad27-01bf-0c8410d4b9e1"),
                column: "created_at",
                value: new DateTime(2026, 2, 2, 3, 33, 19, 513, DateTimeKind.Utc).AddTicks(9515));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("c5735376-4831-c12b-a01e-672efee6c8e3"),
                column: "created_at",
                value: new DateTime(2026, 2, 2, 3, 33, 19, 513, DateTimeKind.Utc).AddTicks(9489));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("cdaa97c0-e68f-2819-984e-63bb9dcf35a6"),
                column: "created_at",
                value: new DateTime(2026, 2, 2, 3, 33, 19, 513, DateTimeKind.Utc).AddTicks(9347));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("e1d5afac-09d6-ef55-a529-f5bf473ef103"),
                column: "created_at",
                value: new DateTime(2026, 2, 2, 3, 33, 19, 513, DateTimeKind.Utc).AddTicks(9533));

            migrationBuilder.CreateIndex(
                name: "ix_registration_additions_checkout_session",
                schema: "events",
                table: "registration_additions",
                column: "stripe_checkout_session_id");

            migrationBuilder.CreateIndex(
                name: "ix_registration_additions_event_id",
                schema: "events",
                table: "registration_additions",
                column: "event_id");

            migrationBuilder.CreateIndex(
                name: "ix_registration_additions_registration_id",
                schema: "events",
                table: "registration_additions",
                column: "registration_id");

            migrationBuilder.CreateIndex(
                name: "ix_registration_additions_status",
                schema: "events",
                table: "registration_additions",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "uq_registration_additions_one_pending_per_registration",
                schema: "events",
                table: "registration_additions",
                columns: new[] { "registration_id", "status" },
                unique: true,
                filter: "status = 'Pending'");

            migrationBuilder.CreateIndex(
                name: "ix_registration_payments_addition_id",
                schema: "events",
                table: "registration_payments",
                column: "registration_addition_id");

            migrationBuilder.CreateIndex(
                name: "ix_registration_payments_registration_id",
                schema: "events",
                table: "registration_payments",
                column: "registration_id");

            migrationBuilder.CreateIndex(
                name: "ix_registration_payments_registration_type",
                schema: "events",
                table: "registration_payments",
                columns: new[] { "registration_id", "type" });

            migrationBuilder.CreateIndex(
                name: "uq_registration_payments_payment_intent",
                schema: "events",
                table: "registration_payments",
                column: "stripe_payment_intent_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "registration_payments",
                schema: "events");

            migrationBuilder.DropTable(
                name: "registration_additions",
                schema: "events");

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("0b9effc0-322f-8026-85c6-747e381b41e6"),
                column: "created_at",
                value: new DateTime(2026, 2, 1, 20, 26, 48, 52, DateTimeKind.Utc).AddTicks(7152));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("2d87836d-9322-d4b1-b4ec-b5b73eca9ad9"),
                column: "created_at",
                value: new DateTime(2026, 2, 1, 20, 26, 48, 52, DateTimeKind.Utc).AddTicks(7439));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("31f73d61-6c12-1252-f5ab-10d9d47eba46"),
                column: "created_at",
                value: new DateTime(2026, 2, 1, 20, 26, 48, 52, DateTimeKind.Utc).AddTicks(7042));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4de1eacb-273a-ab85-e811-d60addb4ae30"),
                column: "created_at",
                value: new DateTime(2026, 2, 1, 20, 26, 48, 52, DateTimeKind.Utc).AddTicks(7207));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4e57a1be-7a76-833e-003f-b2e3182f29f0"),
                column: "created_at",
                value: new DateTime(2026, 2, 1, 20, 26, 48, 52, DateTimeKind.Utc).AddTicks(7231));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("6313b249-2620-3e97-c1bd-f1d50814156d"),
                column: "created_at",
                value: new DateTime(2026, 2, 1, 20, 26, 48, 52, DateTimeKind.Utc).AddTicks(7568));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("70ab7cff-d677-f4bd-b331-f02908ee3347"),
                column: "created_at",
                value: new DateTime(2026, 2, 1, 20, 26, 48, 52, DateTimeKind.Utc).AddTicks(7177));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("80cd50b4-7630-f5d0-1f9a-a7c480347dcf"),
                column: "created_at",
                value: new DateTime(2026, 2, 1, 20, 26, 48, 52, DateTimeKind.Utc).AddTicks(7119));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("9b07d22a-d0bf-ad27-01bf-0c8410d4b9e1"),
                column: "created_at",
                value: new DateTime(2026, 2, 1, 20, 26, 48, 52, DateTimeKind.Utc).AddTicks(7522));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("c5735376-4831-c12b-a01e-672efee6c8e3"),
                column: "created_at",
                value: new DateTime(2026, 2, 1, 20, 26, 48, 52, DateTimeKind.Utc).AddTicks(7499));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("cdaa97c0-e68f-2819-984e-63bb9dcf35a6"),
                column: "created_at",
                value: new DateTime(2026, 2, 1, 20, 26, 48, 52, DateTimeKind.Utc).AddTicks(7475));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("e1d5afac-09d6-ef55-a529-f5bf473ef103"),
                column: "created_at",
                value: new DateTime(2026, 2, 1, 20, 26, 48, 52, DateTimeKind.Utc).AddTicks(7545));
        }
    }
}
