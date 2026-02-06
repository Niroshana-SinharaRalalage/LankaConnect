using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    /// <summary>
    /// Phase 6A.43: Migration to transform attendee data from age (int) to age_category (Adult/Child) and add gender field.
    ///
    /// This migration transforms existing JSONB attendee data:
    /// - age (int) is converted to age_category (string: "Adult" or "Child")
    /// - gender field is added as null for existing records
    /// - Age <= 18 becomes "Child", Age > 18 becomes "Adult"
    /// </summary>
    public partial class UpdateAttendeesAgeCategoryAndGender_Phase6A43 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Transform existing attendees JSONB data:
            // - Convert 'age' (int) to 'age_category' (string: "Adult" or "Child")
            // - Add 'gender' field as null
            // - Age <= 18 → "Child", Age > 18 → "Adult"
            // NOTE: This migration is idempotent and safe to run on empty databases
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    -- Only run if registrations table exists
                    IF EXISTS (SELECT FROM pg_tables WHERE schemaname = 'public' AND tablename = 'registrations') THEN
                        UPDATE registrations
                        SET attendees = (
                            SELECT jsonb_agg(
                                jsonb_build_object(
                                    'name', elem->>'name',
                                    'age_category', CASE
                                        WHEN (elem->>'age')::int <= 18 THEN 'Child'
                                        ELSE 'Adult'
                                    END,
                                    'gender', null::text
                                )
                            )
                            FROM jsonb_array_elements(attendees) elem
                        )
                        WHERE attendees IS NOT NULL
                          AND jsonb_array_length(attendees) > 0
                          AND attendees->0 ? 'age';
                    END IF;
                END $$;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Reverse transformation: Convert age_category back to age
            // Adult → 25 (default adult age), Child → 10 (default child age)
            // NOTE: This migration is idempotent and safe to run on empty databases
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    -- Only run if registrations table exists
                    IF EXISTS (SELECT FROM pg_tables WHERE schemaname = 'public' AND tablename = 'registrations') THEN
                        UPDATE registrations
                        SET attendees = (
                            SELECT jsonb_agg(
                                jsonb_build_object(
                                    'name', elem->>'name',
                                    'age', CASE
                                        WHEN elem->>'age_category' = 'Child' THEN 10
                                        ELSE 25
                                    END
                                )
                            )
                            FROM jsonb_array_elements(attendees) elem
                        )
                        WHERE attendees IS NOT NULL
                          AND jsonb_array_length(attendees) > 0
                          AND attendees->0 ? 'age_category';
                    END IF;
                END $$;
            ");
        }
    }
}
