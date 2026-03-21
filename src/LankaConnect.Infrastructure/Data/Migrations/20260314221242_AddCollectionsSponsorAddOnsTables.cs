using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCollectionsSponsorAddOnsTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "add_on_config",
                schema: "events",
                table: "events",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "collection_config",
                schema: "events",
                table: "events",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "sponsor_config",
                schema: "events",
                table: "events",
                type: "jsonb",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "add_on_definitions",
                schema: "events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    price_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    price_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    quantity_limit = table.Column<int>(type: "integer", nullable: true),
                    quantity_sold = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_add_on_definitions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_add_on_definitions_events_event_id",
                        column: x => x.event_id,
                        principalSchema: "events",
                        principalTable: "events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "collections",
                schema: "events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    contributor_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    contributor_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    contributor_email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    contributor_phone = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    contributor_notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    amount_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    stripe_checkout_session_id = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
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
                    table.PrimaryKey("PK_collections", x => x.Id);
                    table.ForeignKey(
                        name: "FK_collections_events_event_id",
                        column: x => x.event_id,
                        principalSchema: "events",
                        principalTable: "events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "sponsors",
                schema: "events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    sponsor_type = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    sponsor_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    sponsor_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    sponsor_email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    sponsor_phone = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    sponsor_organization = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    sponsor_notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    amount_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    stripe_checkout_session_id = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
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
                    item_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    item_description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    estimated_value = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    recorded_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sponsors", x => x.Id);
                    table.ForeignKey(
                        name: "FK_sponsors_events_event_id",
                        column: x => x.event_id,
                        principalSchema: "events",
                        principalTable: "events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "add_on_purchases",
                schema: "events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    add_on_definition_id = table.Column<Guid>(type: "uuid", nullable: false),
                    registration_id = table.Column<Guid>(type: "uuid", nullable: true),
                    buyer_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    buyer_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    buyer_email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                    buyer_phone = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    unit_price_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    unit_price_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    total_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    total_amount_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    stripe_checkout_session_id = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
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
                    table.PrimaryKey("PK_add_on_purchases", x => x.Id);
                    table.ForeignKey(
                        name: "FK_add_on_purchases_add_on_definitions_add_on_definition_id",
                        column: x => x.add_on_definition_id,
                        principalSchema: "events",
                        principalTable: "add_on_definitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_add_on_purchases_events_event_id",
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
                value: new DateTime(2026, 3, 14, 22, 12, 39, 27, DateTimeKind.Utc).AddTicks(59));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("2d87836d-9322-d4b1-b4ec-b5b73eca9ad9"),
                column: "created_at",
                value: new DateTime(2026, 3, 14, 22, 12, 39, 27, DateTimeKind.Utc).AddTicks(152));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("31f73d61-6c12-1252-f5ab-10d9d47eba46"),
                column: "created_at",
                value: new DateTime(2026, 3, 14, 22, 12, 39, 27, DateTimeKind.Utc).AddTicks(10));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4de1eacb-273a-ab85-e811-d60addb4ae30"),
                column: "created_at",
                value: new DateTime(2026, 3, 14, 22, 12, 39, 27, DateTimeKind.Utc).AddTicks(106));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4e57a1be-7a76-833e-003f-b2e3182f29f0"),
                column: "created_at",
                value: new DateTime(2026, 3, 14, 22, 12, 39, 27, DateTimeKind.Utc).AddTicks(123));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("6313b249-2620-3e97-c1bd-f1d50814156d"),
                column: "created_at",
                value: new DateTime(2026, 3, 14, 22, 12, 39, 27, DateTimeKind.Utc).AddTicks(247));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("70ab7cff-d677-f4bd-b331-f02908ee3347"),
                column: "created_at",
                value: new DateTime(2026, 3, 14, 22, 12, 39, 27, DateTimeKind.Utc).AddTicks(77));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("80cd50b4-7630-f5d0-1f9a-a7c480347dcf"),
                column: "created_at",
                value: new DateTime(2026, 3, 14, 22, 12, 39, 27, DateTimeKind.Utc).AddTicks(40));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("9b07d22a-d0bf-ad27-01bf-0c8410d4b9e1"),
                column: "created_at",
                value: new DateTime(2026, 3, 14, 22, 12, 39, 27, DateTimeKind.Utc).AddTicks(215));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("c5735376-4831-c12b-a01e-672efee6c8e3"),
                column: "created_at",
                value: new DateTime(2026, 3, 14, 22, 12, 39, 27, DateTimeKind.Utc).AddTicks(197));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("cdaa97c0-e68f-2819-984e-63bb9dcf35a6"),
                column: "created_at",
                value: new DateTime(2026, 3, 14, 22, 12, 39, 27, DateTimeKind.Utc).AddTicks(170));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("e1d5afac-09d6-ef55-a529-f5bf473ef103"),
                column: "created_at",
                value: new DateTime(2026, 3, 14, 22, 12, 39, 27, DateTimeKind.Utc).AddTicks(231));

            migrationBuilder.CreateIndex(
                name: "ix_add_on_definitions_event_id",
                schema: "events",
                table: "add_on_definitions",
                column: "event_id");

            migrationBuilder.CreateIndex(
                name: "ix_add_on_definitions_is_active",
                schema: "events",
                table: "add_on_definitions",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ix_add_on_purchases_add_on_definition_id",
                schema: "events",
                table: "add_on_purchases",
                column: "add_on_definition_id");

            migrationBuilder.CreateIndex(
                name: "ix_add_on_purchases_buyer_user_id",
                schema: "events",
                table: "add_on_purchases",
                column: "buyer_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_add_on_purchases_checkout_session",
                schema: "events",
                table: "add_on_purchases",
                column: "stripe_checkout_session_id");

            migrationBuilder.CreateIndex(
                name: "ix_add_on_purchases_event_id",
                schema: "events",
                table: "add_on_purchases",
                column: "event_id");

            migrationBuilder.CreateIndex(
                name: "ix_add_on_purchases_payment_intent",
                schema: "events",
                table: "add_on_purchases",
                column: "stripe_payment_intent_id");

            migrationBuilder.CreateIndex(
                name: "ix_add_on_purchases_registration_id",
                schema: "events",
                table: "add_on_purchases",
                column: "registration_id");

            migrationBuilder.CreateIndex(
                name: "ix_add_on_purchases_status",
                schema: "events",
                table: "add_on_purchases",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_collections_checkout_session",
                schema: "events",
                table: "collections",
                column: "stripe_checkout_session_id");

            migrationBuilder.CreateIndex(
                name: "ix_collections_contributor_user_id",
                schema: "events",
                table: "collections",
                column: "contributor_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_collections_event_id",
                schema: "events",
                table: "collections",
                column: "event_id");

            migrationBuilder.CreateIndex(
                name: "ix_collections_payment_intent",
                schema: "events",
                table: "collections",
                column: "stripe_payment_intent_id");

            migrationBuilder.CreateIndex(
                name: "ix_collections_status",
                schema: "events",
                table: "collections",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ix_sponsors_checkout_session",
                schema: "events",
                table: "sponsors",
                column: "stripe_checkout_session_id");

            migrationBuilder.CreateIndex(
                name: "ix_sponsors_event_id",
                schema: "events",
                table: "sponsors",
                column: "event_id");

            migrationBuilder.CreateIndex(
                name: "ix_sponsors_payment_intent",
                schema: "events",
                table: "sponsors",
                column: "stripe_payment_intent_id");

            migrationBuilder.CreateIndex(
                name: "ix_sponsors_sponsor_type",
                schema: "events",
                table: "sponsors",
                column: "sponsor_type");

            migrationBuilder.CreateIndex(
                name: "ix_sponsors_sponsor_user_id",
                schema: "events",
                table: "sponsors",
                column: "sponsor_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_sponsors_status",
                schema: "events",
                table: "sponsors",
                column: "status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "add_on_purchases",
                schema: "events");

            migrationBuilder.DropTable(
                name: "collections",
                schema: "events");

            migrationBuilder.DropTable(
                name: "sponsors",
                schema: "events");

            migrationBuilder.DropTable(
                name: "add_on_definitions",
                schema: "events");

            migrationBuilder.DropColumn(
                name: "add_on_config",
                schema: "events",
                table: "events");

            migrationBuilder.DropColumn(
                name: "collection_config",
                schema: "events",
                table: "events");

            migrationBuilder.DropColumn(
                name: "sponsor_config",
                schema: "events",
                table: "events");

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("0b9effc0-322f-8026-85c6-747e381b41e6"),
                column: "created_at",
                value: new DateTime(2026, 3, 14, 2, 11, 15, 103, DateTimeKind.Utc).AddTicks(2907));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("2d87836d-9322-d4b1-b4ec-b5b73eca9ad9"),
                column: "created_at",
                value: new DateTime(2026, 3, 14, 2, 11, 15, 103, DateTimeKind.Utc).AddTicks(2971));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("31f73d61-6c12-1252-f5ab-10d9d47eba46"),
                column: "created_at",
                value: new DateTime(2026, 3, 14, 2, 11, 15, 103, DateTimeKind.Utc).AddTicks(2780));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4de1eacb-273a-ab85-e811-d60addb4ae30"),
                column: "created_at",
                value: new DateTime(2026, 3, 14, 2, 11, 15, 103, DateTimeKind.Utc).AddTicks(2935));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4e57a1be-7a76-833e-003f-b2e3182f29f0"),
                column: "created_at",
                value: new DateTime(2026, 3, 14, 2, 11, 15, 103, DateTimeKind.Utc).AddTicks(2947));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("6313b249-2620-3e97-c1bd-f1d50814156d"),
                column: "created_at",
                value: new DateTime(2026, 3, 14, 2, 11, 15, 103, DateTimeKind.Utc).AddTicks(3036));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("70ab7cff-d677-f4bd-b331-f02908ee3347"),
                column: "created_at",
                value: new DateTime(2026, 3, 14, 2, 11, 15, 103, DateTimeKind.Utc).AddTicks(2921));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("80cd50b4-7630-f5d0-1f9a-a7c480347dcf"),
                column: "created_at",
                value: new DateTime(2026, 3, 14, 2, 11, 15, 103, DateTimeKind.Utc).AddTicks(2890));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("9b07d22a-d0bf-ad27-01bf-0c8410d4b9e1"),
                column: "created_at",
                value: new DateTime(2026, 3, 14, 2, 11, 15, 103, DateTimeKind.Utc).AddTicks(3013));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("c5735376-4831-c12b-a01e-672efee6c8e3"),
                column: "created_at",
                value: new DateTime(2026, 3, 14, 2, 11, 15, 103, DateTimeKind.Utc).AddTicks(3001));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("cdaa97c0-e68f-2819-984e-63bb9dcf35a6"),
                column: "created_at",
                value: new DateTime(2026, 3, 14, 2, 11, 15, 103, DateTimeKind.Utc).AddTicks(2984));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("e1d5afac-09d6-ef55-a529-f5bf473ef103"),
                column: "created_at",
                value: new DateTime(2026, 3, 14, 2, 11, 15, 103, DateTimeKind.Utc).AddTicks(3025));
        }
    }
}
