using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Infrastructure.Data.Migrations
{
    /// <summary>
    /// 2026-06-11 INCIDENT data-fix migration.
    ///
    /// Strips raw 0x00 (null) bytes from every JSONB column on
    /// events.registrations and events.registration_additions. The
    /// root incident is one Registration row on event
    /// ad8903c4-e98e-49dd-b44e-d89f916c49dc whose JSONB has an
    /// embedded null byte at byte 149 -- legal in Postgres jsonb storage but
    /// illegal in JSON per RFC 8259 (and rejected by System.Text.Json during
    /// EF Core 8's IncludeJsonEntityReference shaper). ONE bad row breaks
    /// EVERY repository query that hydrates Event entities via Includes;
    /// hotfix-2 (df0dc31b GetAllAsync) + hotfix-3 (433618b2 HasQueryFilter)
    /// were defensive workarounds. This migration is the permanent fix.
    ///
    /// Targets ALL rows containing 0x00 bytes (not just the known bad event)
    /// -- defensive sweep, single chance to catch related corruption in
    /// related tables. Idempotent: skips clean rows via
    /// `WHERE position(E'\x00' in col::text) > 0`. Safe: regexp_replace
    /// strips the byte without altering surrounding string content.
    ///
    /// Down() is intentionally a no-op -- re-introducing null bytes is never
    /// desirable.
    ///
    /// NOTE: EF Core scaffolded this migration with reference_data created_at
    /// drift updates (seed values use NOW() which re-evaluates each scaffold)
    /// -- those were REMOVED per CLAUDE.md Section 5. The .Designer.cs
    /// companion snapshot intentionally retains the drift so subsequent
    /// migrations don't re-emit the same updates.
    ///
    /// After this migration runs successfully on staging, the HasQueryFilter
    /// in EventConfiguration.cs (added in hotfix-3 commit 433618b2) MUST be
    /// removed in a follow-up commit, and the matching FromSqlRaw WHERE
    /// clause in EventRepository.SearchAsync MUST be removed too.
    /// </summary>
    public partial class SanitizeNullBytesInRegistrationJsonb : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // events.registrations — five JSONB columns
            foreach (var column in new[]
            {
                "attendee_info",
                "attendees",
                "pending_seat_assignments",
                "contact",
                "head_count",
            })
            {
                migrationBuilder.Sql(
                    "UPDATE events.registrations " +
                    $"SET {column} = regexp_replace({column}::text, E'\\x00', '', 'g')::jsonb " +
                    $"WHERE {column} IS NOT NULL AND position(E'\\x00' in {column}::text) > 0;");
            }

            // events.registration_additions — two JSONB columns
            foreach (var column in new[]
            {
                "new_attendees",
                "head_count_delta",
            })
            {
                migrationBuilder.Sql(
                    "UPDATE events.registration_additions " +
                    $"SET {column} = regexp_replace({column}::text, E'\\x00', '', 'g')::jsonb " +
                    $"WHERE {column} IS NOT NULL AND position(E'\\x00' in {column}::text) > 0;");
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Intentionally empty: re-introducing 0x00 bytes is never desirable.
        }
    }
}
