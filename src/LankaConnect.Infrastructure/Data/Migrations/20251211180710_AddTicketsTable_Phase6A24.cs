using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTicketsTable_Phase6A24 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "tickets",
                schema: "events",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RegistrationId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    TicketCode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    QrCodeData = table.Column<string>(type: "text", nullable: false),
                    PdfBlobUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    IsValid = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    ValidatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tickets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_tickets_events_EventId",
                        column: x => x.EventId,
                        principalSchema: "events",
                        principalTable: "events",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_tickets_registrations_RegistrationId",
                        column: x => x.RegistrationId,
                        principalSchema: "events",
                        principalTable: "registrations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_tickets_users_UserId",
                        column: x => x.UserId,
                        principalSchema: "identity",
                        principalTable: "users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_tickets_EventId",
                schema: "events",
                table: "tickets",
                column: "EventId");

            migrationBuilder.CreateIndex(
                name: "IX_tickets_RegistrationId",
                schema: "events",
                table: "tickets",
                column: "RegistrationId");

            migrationBuilder.CreateIndex(
                name: "IX_tickets_TicketCode",
                schema: "events",
                table: "tickets",
                column: "TicketCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_tickets_UserId",
                schema: "events",
                table: "tickets",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "tickets",
                schema: "events");
        }
    }
}
