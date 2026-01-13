using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class Phase6A61_AddEventNotificationHistoryTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Phase 6A.61: Create event_notification_history table to track manual email sends

            migrationBuilder.Sql(@"
                CREATE TABLE communications.event_notification_history (
                    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
                    event_id UUID NOT NULL,
                    sent_by_user_id UUID NOT NULL,
                    sent_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
                    recipient_count INT NOT NULL,
                    successful_sends INT NOT NULL DEFAULT 0,
                    failed_sends INT NOT NULL DEFAULT 0,
                    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),

                    CONSTRAINT fk_event_notification_history_event_id
                        FOREIGN KEY (event_id)
                        REFERENCES events.events(""Id"")
                        ON DELETE CASCADE,

                    CONSTRAINT fk_event_notification_history_sent_by_user_id
                        FOREIGN KEY (sent_by_user_id)
                        REFERENCES users.users(""Id"")
                        ON DELETE RESTRICT
                );

                -- Index for querying history by event
                CREATE INDEX ix_event_notification_history_event_id
                    ON communications.event_notification_history(event_id);

                -- Index for ordering by send time (most recent first)
                CREATE INDEX ix_event_notification_history_sent_at_desc
                    ON communications.event_notification_history(sent_at DESC);
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                DROP TABLE IF EXISTS communications.event_notification_history;
            ");
        }
    }
}
