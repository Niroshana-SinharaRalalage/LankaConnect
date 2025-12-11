using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSignUpListAndSignUpCommitmentTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "event_passes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    total_quantity = table.Column<int>(type: "integer", nullable: false),
                    reserved_quantity = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    price_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    price_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_event_passes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_event_passes_events_event_id",
                        column: x => x.event_id,
                        principalSchema: "events",
                        principalTable: "events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "pass_purchases",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_pass_id = table.Column<Guid>(type: "uuid", nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "Pending"),
                    qr_code = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    confirmed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    cancelled_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    total_price_amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    total_price_currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pass_purchases", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "sign_up_lists",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    category = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    sign_up_type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    predefined_items = table.Column<List<string>>(type: "jsonb", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sign_up_lists", x => x.id);
                    table.ForeignKey(
                        name: "FK_sign_up_lists_events_event_id",
                        column: x => x.event_id,
                        principalSchema: "events",
                        principalTable: "events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sign_up_commitments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    item_description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    quantity = table.Column<int>(type: "integer", nullable: false),
                    committed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SignUpListId = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sign_up_commitments", x => x.id);
                    table.ForeignKey(
                        name: "FK_sign_up_commitments_sign_up_lists_SignUpListId",
                        column: x => x.SignUpListId,
                        principalTable: "sign_up_lists",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_event_passes_event_id",
                table: "event_passes",
                column: "event_id");

            migrationBuilder.CreateIndex(
                name: "ix_pass_purchases_event_id",
                table: "pass_purchases",
                column: "event_id");

            migrationBuilder.CreateIndex(
                name: "ix_pass_purchases_event_pass_id",
                table: "pass_purchases",
                column: "event_pass_id");

            migrationBuilder.CreateIndex(
                name: "ix_pass_purchases_event_user_status",
                table: "pass_purchases",
                columns: new[] { "event_id", "user_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_pass_purchases_qr_code",
                table: "pass_purchases",
                column: "qr_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_pass_purchases_user_id",
                table: "pass_purchases",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_sign_up_commitments_SignUpListId",
                table: "sign_up_commitments",
                column: "SignUpListId");

            migrationBuilder.CreateIndex(
                name: "ix_sign_up_commitments_user_id",
                table: "sign_up_commitments",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_sign_up_lists_category",
                table: "sign_up_lists",
                column: "category");

            migrationBuilder.CreateIndex(
                name: "ix_sign_up_lists_event_id",
                table: "sign_up_lists",
                column: "event_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "event_passes");

            migrationBuilder.DropTable(
                name: "pass_purchases");

            migrationBuilder.DropTable(
                name: "sign_up_commitments");

            migrationBuilder.DropTable(
                name: "sign_up_lists");
        }
    }
}
