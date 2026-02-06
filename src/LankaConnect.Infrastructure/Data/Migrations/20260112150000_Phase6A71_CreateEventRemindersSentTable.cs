using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Infrastructure.Data.Migrations
{
    /// <summary>
    /// Phase 6A.71: Create event_reminders_sent tracking table for idempotency
    /// Prevents duplicate reminder emails when job runs multiple times
    /// </summary>
    public partial class Phase6A71_CreateEventRemindersSentTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create event_reminders_sent tracking table
            migrationBuilder.CreateTable(
                name: "event_reminders_sent",
                schema: "events",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_id = table.Column<Guid>(type: "uuid", nullable: false),
                    registration_id = table.Column<Guid>(type: "uuid", nullable: false),
                    reminder_type = table.Column<string>(type: "varchar(50)", nullable: false),
                    sent_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    recipient_email = table.Column<string>(type: "varchar(255)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_event_reminders_sent", x => x.id);
                    table.ForeignKey(
                        name: "fk_event_reminders_sent_events_event_id",
                        column: x => x.event_id,
                        principalSchema: "events",
                        principalTable: "events",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_event_reminders_sent_registrations_registration_id",
                        column: x => x.registration_id,
                        principalSchema: "events",
                        principalTable: "registrations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Create indexes for efficient lookups
            migrationBuilder.CreateIndex(
                name: "ix_event_reminders_sent_event_id",
                schema: "events",
                table: "event_reminders_sent",
                column: "event_id");

            migrationBuilder.CreateIndex(
                name: "ix_event_reminders_sent_registration_id",
                schema: "events",
                table: "event_reminders_sent",
                column: "registration_id");

            // Composite unique index to prevent duplicate reminders
            migrationBuilder.CreateIndex(
                name: "ix_event_reminders_sent_event_registration_type",
                schema: "events",
                table: "event_reminders_sent",
                columns: new[] { "event_id", "registration_id", "reminder_type" },
                unique: true);

            // Index for sent_at for cleanup queries
            migrationBuilder.CreateIndex(
                name: "ix_event_reminders_sent_sent_at",
                schema: "events",
                table: "event_reminders_sent",
                column: "sent_at");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "event_reminders_sent",
                schema: "events");
        }
    }
}
