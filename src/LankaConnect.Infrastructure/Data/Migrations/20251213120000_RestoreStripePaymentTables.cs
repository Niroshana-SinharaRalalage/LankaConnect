using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class RestoreStripePaymentTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Phase 6A.4 Fix: Restore Stripe payment tables that were accidentally dropped
            // by migration 20251129201535_AddSignUpItemCategorySupport

            // Step 1: Ensure payments schema exists
            migrationBuilder.Sql(@"
                CREATE SCHEMA IF NOT EXISTS payments;
            ");

            // Step 2: Create stripe_customers table if it doesn't exist
            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS payments.stripe_customers (
                    ""Id"" uuid NOT NULL,
                    user_id uuid NOT NULL,
                    stripe_customer_id character varying(255) NOT NULL,
                    email character varying(255) NOT NULL,
                    name character varying(255) NOT NULL,
                    stripe_created_at timestamp with time zone NOT NULL,
                    created_at timestamp with time zone NOT NULL DEFAULT NOW(),
                    updated_at timestamp with time zone,
                    CONSTRAINT ""PK_stripe_customers"" PRIMARY KEY (""Id"")
                );
            ");

            // Step 3: Create stripe_webhook_events table if it doesn't exist
            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS payments.stripe_webhook_events (
                    ""Id"" uuid NOT NULL,
                    event_id character varying(255) NOT NULL,
                    event_type character varying(100) NOT NULL,
                    processed boolean NOT NULL DEFAULT false,
                    processed_at timestamp with time zone,
                    error_message character varying(2000),
                    attempt_count integer NOT NULL DEFAULT 0,
                    created_at timestamp with time zone NOT NULL DEFAULT NOW(),
                    updated_at timestamp with time zone,
                    CONSTRAINT ""PK_stripe_webhook_events"" PRIMARY KEY (""Id"")
                );
            ");

            // Step 4: Create indexes for stripe_customers (idempotent)
            migrationBuilder.Sql(@"
                CREATE UNIQUE INDEX IF NOT EXISTS ix_stripe_customers_stripe_customer_id
                ON payments.stripe_customers (stripe_customer_id);
            ");

            migrationBuilder.Sql(@"
                CREATE UNIQUE INDEX IF NOT EXISTS ix_stripe_customers_user_id
                ON payments.stripe_customers (user_id);
            ");

            // Step 5: Create indexes for stripe_webhook_events (idempotent)
            migrationBuilder.Sql(@"
                CREATE UNIQUE INDEX IF NOT EXISTS ix_stripe_webhook_events_event_id
                ON payments.stripe_webhook_events (event_id);
            ");

            migrationBuilder.Sql(@"
                CREATE INDEX IF NOT EXISTS ix_stripe_webhook_events_event_type
                ON payments.stripe_webhook_events (event_type);
            ");

            migrationBuilder.Sql(@"
                CREATE INDEX IF NOT EXISTS ix_stripe_webhook_events_processed
                ON payments.stripe_webhook_events (processed);
            ");

            migrationBuilder.Sql(@"
                CREATE INDEX IF NOT EXISTS ix_stripe_webhook_events_processed_created_at
                ON payments.stripe_webhook_events (processed, created_at);
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Note: We don't drop the tables in Down() since they're needed by the application
            // and this migration is a fix for accidentally dropped tables.
            // If you truly need to remove them, do so manually.
        }
    }
}
