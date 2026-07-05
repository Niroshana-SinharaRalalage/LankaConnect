using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.SPLIT_PER_ENTITY.Migrations
{
    /// <summary>
    /// Phase 6A.141 — adds the <c>TicketScanLogs</c> audit table for the paid-event
    /// ticket check-in feature. One row per scan attempt (accepted / rejected / unmarked).
    ///
    /// Hand-cleaned post-scaffold to remove unrelated <c>reference_data.reference_values</c>
    /// seed-timestamp drift that EF Core scaffolding adds whenever a snapshot is rebuilt
    /// (the project's seeders use <c>DateTime.UtcNow</c> so successive scaffolds always
    /// produce noisy idempotent UpdateData calls). The seed-drift normalization will get
    /// folded into a later phase's migration when one happens to be added.
    /// </summary>
    public partial class Phase6A141_AddTicketScanLog : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TicketScanLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ticket_id = table.Column<Guid>(type: "uuid", nullable: true),
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    ticket_code = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    scanner_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    scanner_name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    scan_result = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    rejection_reason = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    entry_method = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    verified_with_previous_key = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    client_ip = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    user_agent = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TicketScanLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TicketScanLogs_events_event_id",
                        column: x => x.event_id,
                        principalSchema: "events",
                        principalTable: "events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TicketScanLogs_tickets_ticket_id",
                        column: x => x.ticket_id,
                        principalSchema: "events",
                        principalTable: "tickets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_TicketScanLogs_users_scanner_user_id",
                        column: x => x.scanner_user_id,
                        principalSchema: "identity",
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TicketScanLogs_EventId_CreatedAt",
                table: "TicketScanLogs",
                columns: new[] { "event_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_TicketScanLogs_ScannerUserId_CreatedAt",
                table: "TicketScanLogs",
                columns: new[] { "scanner_user_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_TicketScanLogs_TicketId",
                table: "TicketScanLogs",
                column: "ticket_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TicketScanLogs");
        }
    }
}
