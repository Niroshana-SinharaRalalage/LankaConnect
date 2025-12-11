using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class SeedMetroAreasReferenceData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Phase 6A.9: Seed metro area reference data via EF Core migration
            // Fixes PUT /api/Users/{id}/preferred-metro-areas persistence issue
            // Root cause: Metro areas table was empty in staging DB, blocking validation
            migrationBuilder.Sql(@"
INSERT INTO events.metro_areas (id, name, state, center_latitude, center_longitude, radius_miles, is_state_level_area, is_active, created_at, updated_at)
VALUES
  ('01000000-0000-0000-0000-000000000001', 'All Alabama', 'AL', 32.8067, -86.7113, 200, true, true, CURRENT_TIMESTAMP, NULL),
  ('01111111-1111-1111-1111-111111111001', 'Birmingham', 'AL', 33.5186, -86.8104, 30, false, true, CURRENT_TIMESTAMP, NULL),
  ('01111111-1111-1111-1111-111111111002', 'Montgomery', 'AL', 32.3792, -86.3077, 25, false, true, CURRENT_TIMESTAMP, NULL),
  ('01111111-1111-1111-1111-111111111003', 'Mobile', 'AL', 30.6954, -88.0399, 25, false, true, CURRENT_TIMESTAMP, NULL),
  ('02000000-0000-0000-0000-000000000001', 'All Alaska', 'AK', 64.0685, -152.2782, 300, true, true, CURRENT_TIMESTAMP, NULL),
  ('02111111-1111-1111-1111-111111111001', 'Anchorage', 'AK', 61.2181, -149.9003, 30, false, true, CURRENT_TIMESTAMP, NULL),
  ('04000000-0000-0000-0000-000000000001', 'All Arizona', 'AZ', 33.7298, -111.4312, 200, true, true, CURRENT_TIMESTAMP, NULL),
  ('04111111-1111-1111-1111-111111111001', 'Phoenix', 'AZ', 33.4484, -112.0742, 35, false, true, CURRENT_TIMESTAMP, NULL),
  ('04111111-1111-1111-1111-111111111002', 'Tucson', 'AZ', 32.2226, -110.9747, 30, false, true, CURRENT_TIMESTAMP, NULL),
  ('04111111-1111-1111-1111-111111111003', 'Mesa', 'AZ', 33.4152, -111.8317, 25, false, true, CURRENT_TIMESTAMP, NULL),
  ('06000000-0000-0000-0000-000000000001', 'All California', 'CA', 36.1162, -119.6816, 250, true, true, CURRENT_TIMESTAMP, NULL),
  ('06111111-1111-1111-1111-111111111001', 'Los Angeles', 'CA', 34.0522, -118.2437, 40, false, true, CURRENT_TIMESTAMP, NULL),
  ('06111111-1111-1111-1111-111111111002', 'San Francisco Bay Area', 'CA', 37.7749, -122.4194, 40, false, true, CURRENT_TIMESTAMP, NULL),
  ('06111111-1111-1111-1111-111111111003', 'San Diego', 'CA', 32.7157, -117.1611, 35, false, true, CURRENT_TIMESTAMP, NULL),
  ('17000000-0000-0000-0000-000000000001', 'All Illinois', 'IL', 40.3495, -88.9861, 200, true, true, CURRENT_TIMESTAMP, NULL),
  ('17111111-1111-1111-1111-111111111001', 'Chicago', 'IL', 41.8781, -87.6298, 45, false, true, CURRENT_TIMESTAMP, NULL),
  ('36000000-0000-0000-0000-000000000001', 'All New York', 'NY', 42.1657, -74.9481, 250, true, true, CURRENT_TIMESTAMP, NULL),
  ('36111111-1111-1111-1111-111111111001', 'New York City', 'NY', 40.7128, -74.0060, 40, false, true, CURRENT_TIMESTAMP, NULL),
  ('48000000-0000-0000-0000-000000000001', 'All Texas', 'TX', 31.9686, -99.9018, 300, true, true, CURRENT_TIMESTAMP, NULL),
  ('48111111-1111-1111-1111-111111111001', 'Houston', 'TX', 29.7604, -95.3698, 40, false, true, CURRENT_TIMESTAMP, NULL),
  ('48111111-1111-1111-1111-111111111002', 'Dallas-Fort Worth', 'TX', 32.7767, -96.7970, 40, false, true, CURRENT_TIMESTAMP, NULL),
  ('48111111-1111-1111-1111-111111111003', 'Austin', 'TX', 30.2672, -97.7431, 30, false, true, CURRENT_TIMESTAMP, NULL)
ON CONFLICT (id) DO NOTHING;
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
