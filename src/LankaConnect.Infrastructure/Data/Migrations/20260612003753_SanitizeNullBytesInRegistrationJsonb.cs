using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Infrastructure.Data.Migrations
{
    /// <summary>
    /// 2026-06-11/12 INCIDENT data-fix migration (REVISED v2).
    ///
    /// V1 attempt (commit fa1932f4) FAILED on staging deploy with
    /// Postgres SQLSTATE 22021 "invalid byte sequence for encoding UTF8: 0x00"
    /// during the `column::text` cast. Postgres validates UTF8 on jsonb->text
    /// cast BEFORE regexp_replace ever runs, so any approach that needs the
    /// text representation cannot survive the bad byte. Approach changed:
    /// PL/pgSQL exception handling.
    ///
    /// V2 approach: loop over each Registration row, attempt to cast each JSONB
    /// column to text inside a BEGIN...EXCEPTION...END savepoint. If the cast
    /// raises character_not_in_repertoire (SQLSTATE 22021), replace the column
    /// with an empty JSONB value of the appropriate shape:
    ///   - attendee_info, contact, head_count, head_count_delta : '{}'  (object)
    ///   - attendees, pending_seat_assignments, new_attendees     : '[]'  (array)
    ///
    /// Trade-off: this is DATA-LOSSY for the affected row's affected column,
    /// but the data was already unreadable to .NET (System.Text.Json rejects
    /// 0x00 bytes per RFC 8259). The registration record itself is preserved
    /// so head-counts / totals stay consistent; only the attendee personal
    /// details / contact info on the corrupted column are wiped.
    ///
    /// Idempotent: rows whose columns cast cleanly are untouched.
    /// </summary>
    public partial class SanitizeNullBytesInRegistrationJsonb : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DO $$
DECLARE
    reg_id uuid;
    sanitized_count int := 0;
    affected_cols text;
BEGIN
    -- events.registrations -- five JSONB columns
    FOR reg_id IN SELECT id FROM events.registrations LOOP
        affected_cols := '';

        BEGIN
            PERFORM 1 FROM events.registrations
            WHERE id = reg_id AND attendee_info::text IS NOT NULL;
        EXCEPTION WHEN character_not_in_repertoire THEN
            UPDATE events.registrations SET attendee_info = '{}'::jsonb WHERE id = reg_id;
            affected_cols := affected_cols || 'attendee_info ';
        END;

        BEGIN
            PERFORM 1 FROM events.registrations
            WHERE id = reg_id AND attendees::text IS NOT NULL;
        EXCEPTION WHEN character_not_in_repertoire THEN
            UPDATE events.registrations SET attendees = '[]'::jsonb WHERE id = reg_id;
            affected_cols := affected_cols || 'attendees ';
        END;

        BEGIN
            PERFORM 1 FROM events.registrations
            WHERE id = reg_id AND pending_seat_assignments::text IS NOT NULL;
        EXCEPTION WHEN character_not_in_repertoire THEN
            UPDATE events.registrations SET pending_seat_assignments = '[]'::jsonb WHERE id = reg_id;
            affected_cols := affected_cols || 'pending_seat_assignments ';
        END;

        BEGIN
            PERFORM 1 FROM events.registrations
            WHERE id = reg_id AND contact::text IS NOT NULL;
        EXCEPTION WHEN character_not_in_repertoire THEN
            UPDATE events.registrations SET contact = '{}'::jsonb WHERE id = reg_id;
            affected_cols := affected_cols || 'contact ';
        END;

        BEGIN
            PERFORM 1 FROM events.registrations
            WHERE id = reg_id AND head_count::text IS NOT NULL;
        EXCEPTION WHEN character_not_in_repertoire THEN
            UPDATE events.registrations SET head_count = '{}'::jsonb WHERE id = reg_id;
            affected_cols := affected_cols || 'head_count ';
        END;

        IF affected_cols <> '' THEN
            RAISE NOTICE 'Sanitized events.registrations id=% columns: %', reg_id, affected_cols;
            sanitized_count := sanitized_count + 1;
        END IF;
    END LOOP;

    -- events.registration_additions -- two JSONB columns
    FOR reg_id IN SELECT id FROM events.registration_additions LOOP
        affected_cols := '';

        BEGIN
            PERFORM 1 FROM events.registration_additions
            WHERE id = reg_id AND new_attendees::text IS NOT NULL;
        EXCEPTION WHEN character_not_in_repertoire THEN
            UPDATE events.registration_additions SET new_attendees = '[]'::jsonb WHERE id = reg_id;
            affected_cols := affected_cols || 'new_attendees ';
        END;

        BEGIN
            PERFORM 1 FROM events.registration_additions
            WHERE id = reg_id AND head_count_delta::text IS NOT NULL;
        EXCEPTION WHEN character_not_in_repertoire THEN
            UPDATE events.registration_additions SET head_count_delta = '{}'::jsonb WHERE id = reg_id;
            affected_cols := affected_cols || 'head_count_delta ';
        END;

        IF affected_cols <> '' THEN
            RAISE NOTICE 'Sanitized events.registration_additions id=% columns: %', reg_id, affected_cols;
            sanitized_count := sanitized_count + 1;
        END IF;
    END LOOP;

    RAISE NOTICE 'Total rows sanitized: %', sanitized_count;
END $$;
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Intentionally empty: re-introducing 0x00 bytes is never desirable
            // and the original corrupt data is unrecoverable from the empty {} / [] values.
        }
    }
}
