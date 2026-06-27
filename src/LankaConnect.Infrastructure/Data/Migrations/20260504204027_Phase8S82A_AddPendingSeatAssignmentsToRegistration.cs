using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Infrastructure.Data.Migrations
{
    /// <summary>
    /// Phase 8 S8.2.A — adds two nullable columns to <c>events.registrations</c>:
    /// <list type="bullet">
    ///   <item><c>pending_seat_assignments</c> (jsonb) — owned-collection
    ///   stash backing <see cref="LankaConnect.Products.LankaEvents.Domain.Registration.PendingSeatAssignments"/>.</item>
    ///   <item><c>pending_seat_session_id</c> (varchar(100)) — backing
    ///   <see cref="LankaConnect.Products.LankaEvents.Domain.Registration.PendingSeatSessionId"/>.</item>
    /// </list>
    ///
    /// Both columns are nullable so existing rows are unaffected — the stash
    /// is meaningful only for in-flight Preliminary registrations during the
    /// RSVP-to-Stripe-checkout window.
    ///
    /// The auto-generated migration body included seed-data drift updates on
    /// <c>reference_data.reference_values.created_at</c>; those have been
    /// intentionally cleaned out so the migration is precisely the schema
    /// change described above.
    ///
    /// See ADR-011 (Seating Wire-Up) for the broader Slice S8 plan.
    /// </summary>
    /// <inheritdoc />
    public partial class Phase8S82A_AddPendingSeatAssignmentsToRegistration : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "pending_seat_assignments",
                schema: "events",
                table: "registrations",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "pending_seat_session_id",
                schema: "events",
                table: "registrations",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "pending_seat_assignments",
                schema: "events",
                table: "registrations");

            migrationBuilder.DropColumn(
                name: "pending_seat_session_id",
                schema: "events",
                table: "registrations");
        }
    }
}
