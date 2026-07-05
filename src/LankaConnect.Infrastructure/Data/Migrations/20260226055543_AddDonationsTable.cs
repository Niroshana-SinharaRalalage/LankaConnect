using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.SPLIT_PER_ENTITY.Migrations
{
    /// <inheritdoc />
    public partial class AddDonationsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "donation_config",
                schema: "events",
                table: "events",
                type: "jsonb",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "donations",
                schema: "events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    registration_id = table.Column<Guid>(type: "uuid", nullable: true),
                    donor_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    donor_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    donor_email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    donor_phone = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    donor_notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    amount_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    stripe_checkout_session_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    stripe_payment_intent_id = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    checkout_expires_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    stripe_fee_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    stripe_fee_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                    platform_commission_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    platform_commission_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                    organizer_payout_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    organizer_payout_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                    payment_completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    failed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    abandoned_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    refunded_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_donations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_donations_events_event_id",
                        column: x => x.event_id,
                        principalSchema: "events",
                        principalTable: "events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("0b9effc0-322f-8026-85c6-747e381b41e6"),
                column: "created_at",
                value: new DateTime(2026, 2, 26, 5, 55, 35, 482, DateTimeKind.Utc).AddTicks(7443));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("2d87836d-9322-d4b1-b4ec-b5b73eca9ad9"),
                column: "created_at",
                value: new DateTime(2026, 2, 26, 5, 55, 35, 482, DateTimeKind.Utc).AddTicks(7594));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("31f73d61-6c12-1252-f5ab-10d9d47eba46"),
                column: "created_at",
                value: new DateTime(2026, 2, 26, 5, 55, 35, 482, DateTimeKind.Utc).AddTicks(7127));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4de1eacb-273a-ab85-e811-d60addb4ae30"),
                column: "created_at",
                value: new DateTime(2026, 2, 26, 5, 55, 35, 482, DateTimeKind.Utc).AddTicks(7525));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4e57a1be-7a76-833e-003f-b2e3182f29f0"),
                column: "created_at",
                value: new DateTime(2026, 2, 26, 5, 55, 35, 482, DateTimeKind.Utc).AddTicks(7560));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("6313b249-2620-3e97-c1bd-f1d50814156d"),
                column: "created_at",
                value: new DateTime(2026, 2, 26, 5, 55, 35, 482, DateTimeKind.Utc).AddTicks(7768));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("70ab7cff-d677-f4bd-b331-f02908ee3347"),
                column: "created_at",
                value: new DateTime(2026, 2, 26, 5, 55, 35, 482, DateTimeKind.Utc).AddTicks(7487));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("80cd50b4-7630-f5d0-1f9a-a7c480347dcf"),
                column: "created_at",
                value: new DateTime(2026, 2, 26, 5, 55, 35, 482, DateTimeKind.Utc).AddTicks(7214));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("9b07d22a-d0bf-ad27-01bf-0c8410d4b9e1"),
                column: "created_at",
                value: new DateTime(2026, 2, 26, 5, 55, 35, 482, DateTimeKind.Utc).AddTicks(7697));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("c5735376-4831-c12b-a01e-672efee6c8e3"),
                column: "created_at",
                value: new DateTime(2026, 2, 26, 5, 55, 35, 482, DateTimeKind.Utc).AddTicks(7664));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("cdaa97c0-e68f-2819-984e-63bb9dcf35a6"),
                column: "created_at",
                value: new DateTime(2026, 2, 26, 5, 55, 35, 482, DateTimeKind.Utc).AddTicks(7628));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("e1d5afac-09d6-ef55-a529-f5bf473ef103"),
                column: "created_at",
                value: new DateTime(2026, 2, 26, 5, 55, 35, 482, DateTimeKind.Utc).AddTicks(7731));

            migrationBuilder.CreateIndex(
                name: "ix_donations_checkout_session",
                schema: "events",
                table: "donations",
                column: "stripe_checkout_session_id");

            migrationBuilder.CreateIndex(
                name: "ix_donations_donor_user_id",
                schema: "events",
                table: "donations",
                column: "donor_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_donations_event_id",
                schema: "events",
                table: "donations",
                column: "event_id");

            migrationBuilder.CreateIndex(
                name: "ix_donations_payment_intent",
                schema: "events",
                table: "donations",
                column: "stripe_payment_intent_id");

            migrationBuilder.CreateIndex(
                name: "ix_donations_registration_id",
                schema: "events",
                table: "donations",
                column: "registration_id");

            migrationBuilder.CreateIndex(
                name: "ix_donations_status",
                schema: "events",
                table: "donations",
                column: "status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "donations",
                schema: "events");

            migrationBuilder.DropColumn(
                name: "donation_config",
                schema: "events",
                table: "events");

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("0b9effc0-322f-8026-85c6-747e381b41e6"),
                column: "created_at",
                value: new DateTime(2026, 2, 17, 13, 24, 11, 145, DateTimeKind.Utc).AddTicks(3735));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("2d87836d-9322-d4b1-b4ec-b5b73eca9ad9"),
                column: "created_at",
                value: new DateTime(2026, 2, 17, 13, 24, 11, 145, DateTimeKind.Utc).AddTicks(3797));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("31f73d61-6c12-1252-f5ab-10d9d47eba46"),
                column: "created_at",
                value: new DateTime(2026, 2, 17, 13, 24, 11, 145, DateTimeKind.Utc).AddTicks(3592));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4de1eacb-273a-ab85-e811-d60addb4ae30"),
                column: "created_at",
                value: new DateTime(2026, 2, 17, 13, 24, 11, 145, DateTimeKind.Utc).AddTicks(3767));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4e57a1be-7a76-833e-003f-b2e3182f29f0"),
                column: "created_at",
                value: new DateTime(2026, 2, 17, 13, 24, 11, 145, DateTimeKind.Utc).AddTicks(3782));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("6313b249-2620-3e97-c1bd-f1d50814156d"),
                column: "created_at",
                value: new DateTime(2026, 2, 17, 13, 24, 11, 145, DateTimeKind.Utc).AddTicks(3870));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("70ab7cff-d677-f4bd-b331-f02908ee3347"),
                column: "created_at",
                value: new DateTime(2026, 2, 17, 13, 24, 11, 145, DateTimeKind.Utc).AddTicks(3751));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("80cd50b4-7630-f5d0-1f9a-a7c480347dcf"),
                column: "created_at",
                value: new DateTime(2026, 2, 17, 13, 24, 11, 145, DateTimeKind.Utc).AddTicks(3718));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("9b07d22a-d0bf-ad27-01bf-0c8410d4b9e1"),
                column: "created_at",
                value: new DateTime(2026, 2, 17, 13, 24, 11, 145, DateTimeKind.Utc).AddTicks(3840));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("c5735376-4831-c12b-a01e-672efee6c8e3"),
                column: "created_at",
                value: new DateTime(2026, 2, 17, 13, 24, 11, 145, DateTimeKind.Utc).AddTicks(3827));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("cdaa97c0-e68f-2819-984e-63bb9dcf35a6"),
                column: "created_at",
                value: new DateTime(2026, 2, 17, 13, 24, 11, 145, DateTimeKind.Utc).AddTicks(3813));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("e1d5afac-09d6-ef55-a529-f5bf473ef103"),
                column: "created_at",
                value: new DateTime(2026, 2, 17, 13, 24, 11, 145, DateTimeKind.Utc).AddTicks(3855));
        }
    }
}
