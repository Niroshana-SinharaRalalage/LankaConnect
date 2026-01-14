using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class Phase6A61_Hotfix_AddUpdatedAtColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Phase 6A.61 Hotfix: Idempotent migration to create event_notification_history table
            // Root Cause: Original migration (20260113020500) was missing 'updated_at' column
            // This migration creates the table if it doesn't exist, or adds missing column if table exists

            migrationBuilder.Sql(@"
                -- Create table if it doesn't exist (handles case where migration history exists but table doesn't)
                CREATE TABLE IF NOT EXISTS communications.event_notification_history (
                    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
                    event_id UUID NOT NULL,
                    sent_by_user_id UUID NOT NULL,
                    sent_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                    recipient_count INT NOT NULL,
                    successful_sends INT NOT NULL DEFAULT 0,
                    failed_sends INT NOT NULL DEFAULT 0,
                    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                    updated_at TIMESTAMPTZ,

                    CONSTRAINT fk_event_notification_history_event_id
                        FOREIGN KEY (event_id)
                        REFERENCES events.events(""Id"")
                        ON DELETE CASCADE,

                    CONSTRAINT fk_event_notification_history_sent_by_user_id
                        FOREIGN KEY (sent_by_user_id)
                        REFERENCES identity.users(""Id"")
                        ON DELETE RESTRICT
                );

                -- Add updated_at column if table exists but column is missing
                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.columns
                        WHERE table_schema = 'communications'
                        AND table_name = 'event_notification_history'
                        AND column_name = 'updated_at'
                    ) THEN
                        ALTER TABLE communications.event_notification_history
                        ADD COLUMN updated_at TIMESTAMPTZ;
                    END IF;
                END $$;

                -- Create indexes if they don't exist
                CREATE INDEX IF NOT EXISTS ix_event_notification_history_event_id
                    ON communications.event_notification_history(event_id);

                CREATE INDEX IF NOT EXISTS ix_event_notification_history_sent_at_desc
                    ON communications.event_notification_history(sent_at DESC);

                CREATE INDEX IF NOT EXISTS ix_event_notification_history_sent_by_user_id
                    ON communications.event_notification_history(sent_by_user_id);
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Phase 6A.61 Hotfix Rollback: Drop the event_notification_history table
            // WARNING: This will delete all notification history data
            migrationBuilder.Sql(@"
                DROP TABLE IF EXISTS communications.event_notification_history;
            ");
        }
    }
}
