using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.SPLIT_PER_ENTITY.Migrations
{
    /// <inheritdoc />
    /// <summary>
    /// Phase 8X.11 — Backfills existing ExternalPaid events from
    /// <c>registration_mode = 5 (NoRegistration)</c> to
    /// <c>registration_mode = 6 (External)</c>.
    ///
    /// Why: Phase 8X.4b set ExternalPaid events to RegistrationMode.NoRegistration as a
    /// "closest existing semantics" workaround. Phase 8X.11 introduced a dedicated
    /// RegistrationMode.External value (smallint 6) so the picker shows a semantically
    /// correct option. Existing ExternalPaid events on staging (and any future prod
    /// migration target) need their stale registration_mode column corrected.
    ///
    /// Forward-only by design — Down() is a no-op since reverting would be a semantic
    /// downgrade. Includes a RAISE EXCEPTION post-assertion (Phase 6A.122 lesson — silent
    /// UPDATE-success prevention) so the migration fails loudly if any ExternalPaid row
    /// is left at a non-External registration_mode value.
    ///
    /// Production note: at the time this migration was authored, the product owner
    /// confirmed prod has zero ExternalPaid events. The UPDATE matches 0 rows on prod
    /// and the assertion passes trivially; the migration is harmless on prod and
    /// necessary on staging.
    /// </summary>
    public partial class Phase8X11_BackfillExternalRegistrationMode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ─────────────────────────────────────────────────────────────────
            //  Backfill: ExternalPaid events with stale NoRegistration mode → External
            // ─────────────────────────────────────────────────────────────────

            migrationBuilder.Sql(@"
                UPDATE events.events
                   SET registration_mode = 6
                 WHERE payment_mode = 2
                   AND registration_mode = 5;
            ");

            // ─────────────────────────────────────────────────────────────────
            //  Post-backfill assertion (Phase 6A.122 lesson — never trust silent UPDATE)
            // ─────────────────────────────────────────────────────────────────
            //
            //  After Phase 8X.11, every row with payment_mode = 2 (ExternalPaid) MUST
            //  have registration_mode = 6 (External). If any row is left at any other
            //  value, the migration fails loudly here.

            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1
                          FROM events.events
                         WHERE payment_mode = 2
                           AND registration_mode <> 6
                    ) THEN
                        RAISE EXCEPTION 'Phase 8X.11 backfill failed: ExternalPaid events with non-External registration_mode still exist';
                    END IF;
                END $$;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Forward-only: reverting would be a semantic downgrade (External → NoRegistration
            // for ExternalPaid events). The previous mode was a workaround; we don't roll back
            // to it. If true rollback is ever needed, restore from a pre-deploy DB snapshot.
        }
    }
}
