using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Infrastructure.Data.Migrations
{
    /// <summary>
    /// Phase 8 S8.1 — snapshot-only migration. Adds <c>SeatId</c> and
    /// <c>SeatLabel</c> properties to the <c>attendees</c> JSONB column on
    /// <c>events.registrations</c>. Because <c>attendees</c> is mapped via
    /// <c>OwnsMany().ToJson()</c> the column is schema-less Postgres JSONB,
    /// so adding new fields requires NO <c>ALTER TABLE</c>. Existing rows
    /// deserialise with null defaults — same pattern as the WhatsApp opt-in
    /// fields added in Phase 7A.6D (<c>RegistrationConfiguration.cs:152-153</c>).
    ///
    /// EF Core still needs the migration recorded so subsequent migrations
    /// diff against an up-to-date snapshot. The auto-generated body included
    /// drift updates on <c>reference_data.reference_values.created_at</c>
    /// timestamps — those are seed-data-drift noise unrelated to this change
    /// and have been intentionally cleared so the migration is a pure no-op
    /// on the database.
    ///
    /// See ADR-011 (Seating Wire-Up) for the broader Slice S8 plan.
    /// </summary>
    /// <inheritdoc />
    public partial class Phase8S81_AddSeatFieldsToAttendeeJsonb : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Intentionally empty: see class summary.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Intentionally empty: see class summary.
        }
    }
}
