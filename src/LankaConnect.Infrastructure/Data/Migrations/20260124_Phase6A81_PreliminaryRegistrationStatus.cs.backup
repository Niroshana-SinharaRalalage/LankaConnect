using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Infrastructure.Data.Migrations
{
    /// <summary>
    /// Phase 6A.81: Payment Bypass Bug Fix - Three-State Registration Lifecycle
    /// Adds Preliminary and Abandoned states to prevent payment bypass vulnerability
    ///
    /// Security Issue: Users could bypass payment by closing Stripe checkout tab,
    /// leaving unpaid registrations with Status='Confirmed' in the database forever.
    ///
    /// Solution: Three-State Lifecycle
    /// - Preliminary (0): Waiting for payment, doesn't consume capacity, doesn't block email
    /// - Confirmed (2): Payment completed or free event, consumes capacity, blocks email
    /// - Abandoned (8): Checkout expired, doesn't consume capacity, doesn't block email
    /// </summary>
    public partial class Phase6A81_PreliminaryRegistrationStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ========================================================================
            // CRITICAL: This migration MUST run in a transaction to avoid data loss
            // ========================================================================

            // Step 1: Add new columns for payment lifecycle tracking
            migrationBuilder.AddColumn<DateTime>(
                name: "checkout_session_expires_at",
                table: "registrations",
                type: "timestamp with time zone",
                nullable: true,
                comment: "Phase 6A.81: Timestamp when Stripe checkout session expires (24h from creation). Set only for Preliminary registrations.");

            migrationBuilder.AddColumn<DateTime>(
                name: "abandoned_at",
                table: "registrations",
                type: "timestamp with time zone",
                nullable: true,
                comment: "Phase 6A.81: Timestamp when registration was marked as Abandoned. Used for audit trail and soft delete after 30 days.");

            // Step 2: Update existing email uniqueness constraint
            // Current constraint blocks ALL registrations with same email
            // New constraint only blocks Confirmed/Waitlisted/CheckedIn/Attended (active registrations)
            // This allows Preliminary and Abandoned registrations to reuse the same email

            // First, check if old constraint exists and drop it
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    -- Drop old constraint if it exists (different names in different environments)
                    IF EXISTS (
                        SELECT 1 FROM pg_constraint
                        WHERE conname = 'IX_Registrations_EventId_ContactEmail'
                    ) THEN
                        DROP INDEX ""IX_Registrations_EventId_ContactEmail"";
                    END IF;

                    IF EXISTS (
                        SELECT 1 FROM pg_constraint
                        WHERE conname = 'ix_registrations_event_id_contact_email'
                    ) THEN
                        DROP INDEX ix_registrations_event_id_contact_email;
                    END IF;

                    IF EXISTS (
                        SELECT 1 FROM pg_constraint
                        WHERE conname = 'uq_registrations_event_contact_email'
                    ) THEN
                        ALTER TABLE registrations DROP CONSTRAINT uq_registrations_event_contact_email;
                    END IF;
                END $$;
            ");

            // Step 3: Create new partial unique index
            // This is a CRITICAL security constraint that prevents duplicate active registrations
            // but allows abandoned/preliminary registrations to retry with the same email
            migrationBuilder.Sql(@"
                -- Phase 6A.81: Email uniqueness only for ACTIVE registrations
                -- Status values:
                --   Preliminary = 0 (NEW - excluded)
                --   Pending = 1 (deprecated - excluded for backward compatibility)
                --   Confirmed = 2 (included)
                --   Waitlisted = 3 (included)
                --   CheckedIn = 4 (included)
                --   Attended = 5 (included)
                --   Cancelled = 6 (excluded)
                --   Refunded = 7 (excluded)
                --   Abandoned = 8 (NEW - excluded)

                CREATE UNIQUE INDEX ix_registrations_event_contact_email_active
                ON registrations (event_id, (contact->>'Email'))
                WHERE status IN (2, 3, 4, 5);
            ");

            // Step 4: Add index on CheckoutSessionExpiresAt for cleanup job performance
            migrationBuilder.Sql(@"
                -- Phase 6A.81: Index for efficient cleanup job queries
                -- Background job runs hourly to find expired preliminary registrations
                CREATE INDEX ix_registrations_preliminary_expired
                ON registrations (created_at, status)
                WHERE status = 0 AND checkout_session_expires_at IS NOT NULL;
            ");

            // Step 5: Add index on AbandonedAt for soft delete cleanup
            migrationBuilder.Sql(@"
                -- Phase 6A.81: Index for soft delete cleanup (30-day retention)
                CREATE INDEX ix_registrations_abandoned_for_cleanup
                ON registrations (abandoned_at)
                WHERE status = 8 AND abandoned_at IS NOT NULL;
            ");

            // Step 6: Update existing Pending registrations to Preliminary
            // This provides backward compatibility for any existing pending registrations
            migrationBuilder.Sql(@"
                -- Phase 6A.81: Migrate existing Pending (1) to Preliminary (0)
                -- Set checkout expiration to 24h from now for safety
                UPDATE registrations
                SET
                    status = 0,  -- Preliminary
                    checkout_session_expires_at = NOW() + INTERVAL '24 hours'
                WHERE status = 1  -- Pending
                  AND payment_status = 0;  -- PaymentStatus.Pending
            ");

            // Step 7: Log migration success for audit trail
            migrationBuilder.Sql(@"
                DO $$
                DECLARE
                    migrated_count INT;
                BEGIN
                    SELECT COUNT(*) INTO migrated_count
                    FROM registrations
                    WHERE status = 0;  -- Preliminary

                    RAISE NOTICE 'Phase 6A.81 Migration Complete: % Preliminary registrations', migrated_count;
                END $$;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // ========================================================================
            // ROLLBACK: Restore original state before Phase 6A.81 changes
            // WARNING: This will lose Preliminary/Abandoned state information
            // ========================================================================

            // Step 1: Revert Preliminary registrations to Pending for backward compatibility
            migrationBuilder.Sql(@"
                UPDATE registrations
                SET status = 1  -- Pending
                WHERE status = 0;  -- Preliminary
            ");

            // Step 2: Cancel Abandoned registrations (safest rollback approach)
            migrationBuilder.Sql(@"
                UPDATE registrations
                SET status = 6  -- Cancelled
                WHERE status = 8;  -- Abandoned
            ");

            // Step 3: Drop new indexes
            migrationBuilder.Sql(@"
                DROP INDEX IF EXISTS ix_registrations_event_contact_email_active;
                DROP INDEX IF EXISTS ix_registrations_preliminary_expired;
                DROP INDEX IF EXISTS ix_registrations_abandoned_for_cleanup;
            ");

            // Step 4: Restore old email uniqueness constraint (all statuses except Cancelled)
            migrationBuilder.Sql(@"
                CREATE UNIQUE INDEX ix_registrations_event_id_contact_email
                ON registrations (event_id, (contact->>'Email'))
                WHERE status NOT IN (6, 7);  -- Exclude Cancelled and Refunded
            ");

            // Step 5: Remove new columns
            migrationBuilder.DropColumn(
                name: "checkout_session_expires_at",
                table: "registrations");

            migrationBuilder.DropColumn(
                name: "abandoned_at",
                table: "registrations");
        }
    }
}
