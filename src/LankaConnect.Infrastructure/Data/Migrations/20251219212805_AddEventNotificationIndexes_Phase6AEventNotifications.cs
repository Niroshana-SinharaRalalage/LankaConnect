using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddEventNotificationIndexes_Phase6AEventNotifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Index 1: Optimize metro area subscriber queries (3-level location matching)
            // Used by: GetConfirmedSubscribersByMetroAreaAsync
            migrationBuilder.Sql(@"
                CREATE INDEX IF NOT EXISTS idx_newsletter_subscribers_metro_area_active
                ON newsletter_subscribers(metro_area_id)
                WHERE is_active = true AND is_confirmed = true;
            ");

            // Index 2: Optimize all-locations subscriber queries (3-level location matching)
            // Used by: GetConfirmedSubscribersForAllLocationsAsync
            migrationBuilder.Sql(@"
                CREATE INDEX IF NOT EXISTS idx_newsletter_subscribers_all_locations
                ON newsletter_subscribers(receive_all_locations)
                WHERE is_active = true AND is_confirmed = true AND receive_all_locations = true;
            ");

            // Index 3: Optimize state-level metro area lookups (3-level location matching)
            // Used by: GetConfirmedSubscribersByStateAsync (joins with metro_areas)
            migrationBuilder.Sql(@"
                CREATE INDEX IF NOT EXISTS idx_metro_areas_state_level
                ON metro_areas(state, is_state_level_area)
                WHERE is_state_level_area = true;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop indexes in reverse order
            migrationBuilder.Sql("DROP INDEX IF EXISTS idx_metro_areas_state_level;");
            migrationBuilder.Sql("DROP INDEX IF EXISTS idx_newsletter_subscribers_all_locations;");
            migrationBuilder.Sql("DROP INDEX IF EXISTS idx_newsletter_subscribers_metro_area_active;");
        }
    }
}
