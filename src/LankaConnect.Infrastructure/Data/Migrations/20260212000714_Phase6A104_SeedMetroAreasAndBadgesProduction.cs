using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class Phase6A104_SeedMetroAreasAndBadgesProduction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Phase 6A.104: Seed missing metro areas and system badges to production
            // This migration ensures both staging and production have identical reference data

            migrationBuilder.Sql(@"
                -- ========================================
                -- PART A: METRO AREAS (134 total)
                -- ========================================
                -- 50 state-level 'All [State]' areas + 84 city-level metros

                INSERT INTO events.metro_areas (id, name, state, center_latitude, center_longitude, radius_miles, is_state_level_area, is_active, created_at, updated_at)
                VALUES
                    -- =====================
                    -- STATE-LEVEL AREAS (50)
                    -- =====================
                    ('01000000-0000-0000-0000-000000000001', 'All Alabama', 'AL', 32.8067, -86.7113, 200, true, true, CURRENT_TIMESTAMP, NULL),
                    ('02000000-0000-0000-0000-000000000001', 'All Alaska', 'AK', 64.2008, -149.4937, 200, true, true, CURRENT_TIMESTAMP, NULL),
                    ('04000000-0000-0000-0000-000000000001', 'All Arizona', 'AZ', 34.0489, -111.0937, 200, true, true, CURRENT_TIMESTAMP, NULL),
                    ('05000000-0000-0000-0000-000000000001', 'All Arkansas', 'AR', 34.7465, -92.2896, 200, true, true, CURRENT_TIMESTAMP, NULL),
                    ('06000000-0000-0000-0000-000000000001', 'All California', 'CA', 36.7783, -119.4179, 200, true, true, CURRENT_TIMESTAMP, NULL),
                    ('08000000-0000-0000-0000-000000000001', 'All Colorado', 'CO', 39.5501, -105.7821, 200, true, true, CURRENT_TIMESTAMP, NULL),
                    ('09000000-0000-0000-0000-000000000001', 'All Connecticut', 'CT', 41.6032, -73.0877, 200, true, true, CURRENT_TIMESTAMP, NULL),
                    ('10000000-0000-0000-0000-000000000001', 'All Delaware', 'DE', 38.9108, -75.5277, 200, true, true, CURRENT_TIMESTAMP, NULL),
                    ('12000000-0000-0000-0000-000000000001', 'All Florida', 'FL', 27.6648, -81.5158, 200, true, true, CURRENT_TIMESTAMP, NULL),
                    ('13000000-0000-0000-0000-000000000001', 'All Georgia', 'GA', 32.1574, -82.9071, 200, true, true, CURRENT_TIMESTAMP, NULL),
                    ('15000000-0000-0000-0000-000000000001', 'All Hawaii', 'HI', 19.8968, -155.5828, 200, true, true, CURRENT_TIMESTAMP, NULL),
                    ('16000000-0000-0000-0000-000000000001', 'All Idaho', 'ID', 44.0682, -114.7420, 200, true, true, CURRENT_TIMESTAMP, NULL),
                    ('17000000-0000-0000-0000-000000000001', 'All Illinois', 'IL', 40.6331, -89.3985, 200, true, true, CURRENT_TIMESTAMP, NULL),
                    ('18000000-0000-0000-0000-000000000001', 'All Indiana', 'IN', 40.2672, -86.1349, 200, true, true, CURRENT_TIMESTAMP, NULL),
                    ('19000000-0000-0000-0000-000000000001', 'All Iowa', 'IA', 41.8780, -93.0977, 200, true, true, CURRENT_TIMESTAMP, NULL),
                    ('20000000-0000-0000-0000-000000000001', 'All Kansas', 'KS', 39.0119, -98.4842, 200, true, true, CURRENT_TIMESTAMP, NULL),
                    ('21000000-0000-0000-0000-000000000001', 'All Kentucky', 'KY', 37.8393, -84.2700, 200, true, true, CURRENT_TIMESTAMP, NULL),
                    ('22000000-0000-0000-0000-000000000001', 'All Louisiana', 'LA', 31.2448, -92.1450, 200, true, true, CURRENT_TIMESTAMP, NULL),
                    ('23000000-0000-0000-0000-000000000001', 'All Maine', 'ME', 45.2538, -69.4455, 200, true, true, CURRENT_TIMESTAMP, NULL),
                    ('24000000-0000-0000-0000-000000000001', 'All Maryland', 'MD', 39.0458, -76.6413, 200, true, true, CURRENT_TIMESTAMP, NULL),
                    ('25000000-0000-0000-0000-000000000001', 'All Massachusetts', 'MA', 42.4072, -71.3824, 200, true, true, CURRENT_TIMESTAMP, NULL),
                    ('26000000-0000-0000-0000-000000000001', 'All Michigan', 'MI', 44.3148, -85.6024, 200, true, true, CURRENT_TIMESTAMP, NULL),
                    ('27000000-0000-0000-0000-000000000001', 'All Minnesota', 'MN', 46.7296, -94.6859, 200, true, true, CURRENT_TIMESTAMP, NULL),
                    ('28000000-0000-0000-0000-000000000001', 'All Mississippi', 'MS', 32.3547, -89.3985, 200, true, true, CURRENT_TIMESTAMP, NULL),
                    ('29000000-0000-0000-0000-000000000001', 'All Missouri', 'MO', 37.9643, -91.8318, 200, true, true, CURRENT_TIMESTAMP, NULL),
                    ('30000000-0000-0000-0000-000000000001', 'All Montana', 'MT', 46.8797, -110.3626, 200, true, true, CURRENT_TIMESTAMP, NULL),
                    ('31000000-0000-0000-0000-000000000001', 'All Nebraska', 'NE', 41.4925, -99.9018, 200, true, true, CURRENT_TIMESTAMP, NULL),
                    ('32000000-0000-0000-0000-000000000001', 'All Nevada', 'NV', 38.8026, -116.4194, 200, true, true, CURRENT_TIMESTAMP, NULL),
                    ('33000000-0000-0000-0000-000000000001', 'All New Hampshire', 'NH', 43.1939, -71.5724, 200, true, true, CURRENT_TIMESTAMP, NULL),
                    ('34000000-0000-0000-0000-000000000001', 'All New Jersey', 'NJ', 40.0583, -74.4057, 200, true, true, CURRENT_TIMESTAMP, NULL),
                    ('35000000-0000-0000-0000-000000000001', 'All New Mexico', 'NM', 34.5199, -105.8701, 200, true, true, CURRENT_TIMESTAMP, NULL),
                    ('36000000-0000-0000-0000-000000000001', 'All New York', 'NY', 43.2994, -74.2179, 200, true, true, CURRENT_TIMESTAMP, NULL),
                    ('37000000-0000-0000-0000-000000000001', 'All North Carolina', 'NC', 35.7596, -79.0193, 200, true, true, CURRENT_TIMESTAMP, NULL),
                    ('38000000-0000-0000-0000-000000000001', 'All North Dakota', 'ND', 47.5515, -101.0020, 200, true, true, CURRENT_TIMESTAMP, NULL),
                    ('39000000-0000-0000-0000-000000000001', 'All Ohio', 'OH', 40.4173, -82.9071, 200, true, true, CURRENT_TIMESTAMP, NULL),
                    ('40000000-0000-0000-0000-000000000001', 'All Oklahoma', 'OK', 35.4676, -97.5164, 200, true, true, CURRENT_TIMESTAMP, NULL),
                    ('41000000-0000-0000-0000-000000000001', 'All Oregon', 'OR', 43.8041, -120.5542, 200, true, true, CURRENT_TIMESTAMP, NULL),
                    ('42000000-0000-0000-0000-000000000001', 'All Pennsylvania', 'PA', 41.2033, -77.1945, 200, true, true, CURRENT_TIMESTAMP, NULL),
                    ('44000000-0000-0000-0000-000000000001', 'All Rhode Island', 'RI', 41.5801, -71.4774, 200, true, true, CURRENT_TIMESTAMP, NULL),
                    ('45000000-0000-0000-0000-000000000001', 'All South Carolina', 'SC', 33.8361, -81.1637, 200, true, true, CURRENT_TIMESTAMP, NULL),
                    ('46000000-0000-0000-0000-000000000001', 'All South Dakota', 'SD', 43.9695, -99.9018, 200, true, true, CURRENT_TIMESTAMP, NULL),
                    ('47000000-0000-0000-0000-000000000001', 'All Tennessee', 'TN', 35.5175, -86.5804, 200, true, true, CURRENT_TIMESTAMP, NULL),
                    ('48000000-0000-0000-0000-000000000001', 'All Texas', 'TX', 31.9686, -99.9018, 200, true, true, CURRENT_TIMESTAMP, NULL),
                    ('49000000-0000-0000-0000-000000000001', 'All Utah', 'UT', 39.3210, -111.0937, 200, true, true, CURRENT_TIMESTAMP, NULL),
                    ('50000000-0000-0000-0000-000000000001', 'All Vermont', 'VT', 44.5588, -72.5778, 200, true, true, CURRENT_TIMESTAMP, NULL),
                    ('51000000-0000-0000-0000-000000000001', 'All Virginia', 'VA', 37.4316, -78.6569, 200, true, true, CURRENT_TIMESTAMP, NULL),
                    ('53000000-0000-0000-0000-000000000001', 'All Washington', 'WA', 47.7511, -120.7401, 200, true, true, CURRENT_TIMESTAMP, NULL),
                    ('54000000-0000-0000-0000-000000000001', 'All West Virginia', 'WV', 38.5976, -80.4549, 200, true, true, CURRENT_TIMESTAMP, NULL),
                    ('55000000-0000-0000-0000-000000000001', 'All Wisconsin', 'WI', 43.7844, -88.7879, 200, true, true, CURRENT_TIMESTAMP, NULL),
                    ('56000000-0000-0000-0000-000000000001', 'All Wyoming', 'WY', 43.0760, -107.2903, 200, true, true, CURRENT_TIMESTAMP, NULL),

                    -- =====================
                    -- CITY-LEVEL METROS (84)
                    -- =====================
                    -- ALABAMA
                    ('01111111-1111-1111-1111-111111111001', 'Birmingham', 'AL', 33.5186, -86.8104, 30, false, true, CURRENT_TIMESTAMP, NULL),
                    ('01111111-1111-1111-1111-111111111002', 'Montgomery', 'AL', 32.3792, -86.3077, 25, false, true, CURRENT_TIMESTAMP, NULL),
                    ('01111111-1111-1111-1111-111111111003', 'Mobile', 'AL', 30.6954, -88.0399, 25, false, true, CURRENT_TIMESTAMP, NULL),

                    -- ALASKA
                    ('02111111-1111-1111-1111-111111111001', 'Anchorage', 'AK', 61.2181, -149.9003, 30, false, true, CURRENT_TIMESTAMP, NULL),

                    -- ARIZONA
                    ('04111111-1111-1111-1111-111111111001', 'Phoenix', 'AZ', 33.4484, -112.0742, 35, false, true, CURRENT_TIMESTAMP, NULL),
                    ('04111111-1111-1111-1111-111111111002', 'Tucson', 'AZ', 32.2226, -110.9747, 30, false, true, CURRENT_TIMESTAMP, NULL),
                    ('04111111-1111-1111-1111-111111111003', 'Mesa', 'AZ', 33.4152, -111.8317, 25, false, true, CURRENT_TIMESTAMP, NULL),

                    -- ARKANSAS
                    ('05111111-1111-1111-1111-111111111001', 'Little Rock', 'AR', 34.7465, -92.2896, 30, false, true, CURRENT_TIMESTAMP, NULL),
                    ('05111111-1111-1111-1111-111111111002', 'Fayetteville', 'AR', 36.0627, -94.1734, 25, false, true, CURRENT_TIMESTAMP, NULL),

                    -- CALIFORNIA
                    ('06111111-1111-1111-1111-111111111001', 'Los Angeles', 'CA', 34.0522, -118.2437, 40, false, true, CURRENT_TIMESTAMP, NULL),
                    ('06111111-1111-1111-1111-111111111002', 'San Francisco Bay Area', 'CA', 37.7749, -122.4194, 40, false, true, CURRENT_TIMESTAMP, NULL),
                    ('06111111-1111-1111-1111-111111111003', 'San Diego', 'CA', 32.7157, -117.1611, 35, false, true, CURRENT_TIMESTAMP, NULL),
                    ('06111111-1111-1111-1111-111111111004', 'Sacramento', 'CA', 38.5816, -121.4944, 30, false, true, CURRENT_TIMESTAMP, NULL),
                    ('06111111-1111-1111-1111-111111111005', 'Fresno', 'CA', 36.7469, -119.7726, 25, false, true, CURRENT_TIMESTAMP, NULL),
                    ('06111111-1111-1111-1111-111111111006', 'Inland Empire', 'CA', 33.9819, -117.2466, 35, false, true, CURRENT_TIMESTAMP, NULL),

                    -- COLORADO
                    ('08111111-1111-1111-1111-111111111001', 'Denver', 'CO', 39.7392, -104.9903, 35, false, true, CURRENT_TIMESTAMP, NULL),
                    ('08111111-1111-1111-1111-111111111002', 'Colorado Springs', 'CO', 38.8339, -104.8202, 30, false, true, CURRENT_TIMESTAMP, NULL),

                    -- CONNECTICUT
                    ('09111111-1111-1111-1111-111111111001', 'Hartford', 'CT', 41.7658, -72.6734, 25, false, true, CURRENT_TIMESTAMP, NULL),
                    ('09111111-1111-1111-1111-111111111002', 'Bridgeport', 'CT', 41.1834, -73.1959, 25, false, true, CURRENT_TIMESTAMP, NULL),

                    -- DELAWARE
                    ('10111111-1111-1111-1111-111111111001', 'Wilmington', 'DE', 39.7391, -75.5244, 25, false, true, CURRENT_TIMESTAMP, NULL),

                    -- FLORIDA
                    ('12111111-1111-1111-1111-111111111001', 'Miami', 'FL', 25.7617, -80.1918, 35, false, true, CURRENT_TIMESTAMP, NULL),
                    ('12111111-1111-1111-1111-111111111002', 'Orlando', 'FL', 28.5421, -81.3723, 30, false, true, CURRENT_TIMESTAMP, NULL),
                    ('12111111-1111-1111-1111-111111111003', 'Tampa Bay', 'FL', 27.9506, -82.4572, 30, false, true, CURRENT_TIMESTAMP, NULL),
                    ('12111111-1111-1111-1111-111111111004', 'Jacksonville', 'FL', 30.3322, -81.6557, 30, false, true, CURRENT_TIMESTAMP, NULL),

                    -- GEORGIA
                    ('13111111-1111-1111-1111-111111111001', 'Atlanta', 'GA', 33.7490, -84.3880, 40, false, true, CURRENT_TIMESTAMP, NULL),
                    ('13111111-1111-1111-1111-111111111002', 'Savannah', 'GA', 32.0809, -81.0912, 25, false, true, CURRENT_TIMESTAMP, NULL),

                    -- HAWAII
                    ('15111111-1111-1111-1111-111111111001', 'Honolulu', 'HI', 21.3099, -157.8581, 30, false, true, CURRENT_TIMESTAMP, NULL),

                    -- IDAHO
                    ('16111111-1111-1111-1111-111111111001', 'Boise', 'ID', 43.6150, -116.2023, 30, false, true, CURRENT_TIMESTAMP, NULL),

                    -- ILLINOIS
                    ('17111111-1111-1111-1111-111111111001', 'Chicago', 'IL', 41.8781, -87.6298, 45, false, true, CURRENT_TIMESTAMP, NULL),

                    -- INDIANA
                    ('18111111-1111-1111-1111-111111111001', 'Indianapolis', 'IN', 39.7684, -86.1581, 35, false, true, CURRENT_TIMESTAMP, NULL),

                    -- IOWA
                    ('19111111-1111-1111-1111-111111111001', 'Des Moines', 'IA', 41.5868, -93.6250, 30, false, true, CURRENT_TIMESTAMP, NULL),

                    -- KANSAS
                    ('20111111-1111-1111-1111-111111111001', 'Kansas City', 'KS', 39.0997, -94.5786, 35, false, true, CURRENT_TIMESTAMP, NULL),

                    -- KENTUCKY
                    ('21111111-1111-1111-1111-111111111001', 'Louisville', 'KY', 38.2527, -85.7585, 30, false, true, CURRENT_TIMESTAMP, NULL),

                    -- LOUISIANA
                    ('22111111-1111-1111-1111-111111111001', 'New Orleans', 'LA', 29.9511, -90.2623, 30, false, true, CURRENT_TIMESTAMP, NULL),

                    -- MAINE
                    ('23111111-1111-1111-1111-111111111001', 'Portland', 'ME', 43.6591, -70.2568, 25, false, true, CURRENT_TIMESTAMP, NULL),

                    -- MARYLAND
                    ('24111111-1111-1111-1111-111111111001', 'Baltimore', 'MD', 39.2904, -76.6122, 30, false, true, CURRENT_TIMESTAMP, NULL),

                    -- MASSACHUSETTS
                    ('25111111-1111-1111-1111-111111111001', 'Boston', 'MA', 42.3601, -71.0589, 35, false, true, CURRENT_TIMESTAMP, NULL),

                    -- MICHIGAN
                    ('26111111-1111-1111-1111-111111111001', 'Detroit', 'MI', 42.3314, -83.0458, 40, false, true, CURRENT_TIMESTAMP, NULL),

                    -- MINNESOTA
                    ('27111111-1111-1111-1111-111111111001', 'Minneapolis-St. Paul', 'MN', 44.9537, -93.0900, 35, false, true, CURRENT_TIMESTAMP, NULL),

                    -- MISSISSIPPI
                    ('28111111-1111-1111-1111-111111111001', 'Jackson', 'MS', 32.2988, -90.1848, 25, false, true, CURRENT_TIMESTAMP, NULL),

                    -- MISSOURI
                    ('29111111-1111-1111-1111-111111111001', 'St. Louis', 'MO', 38.6270, -90.1994, 35, false, true, CURRENT_TIMESTAMP, NULL),
                    ('29111111-1111-1111-1111-111111111002', 'Kansas City', 'MO', 39.0997, -94.5786, 35, false, true, CURRENT_TIMESTAMP, NULL),

                    -- MONTANA
                    ('30111111-1111-1111-1111-111111111001', 'Billings', 'MT', 45.7833, -103.8014, 25, false, true, CURRENT_TIMESTAMP, NULL),

                    -- NEBRASKA
                    ('31111111-1111-1111-1111-111111111001', 'Omaha', 'NE', 41.2565, -95.9345, 30, false, true, CURRENT_TIMESTAMP, NULL),

                    -- NEVADA
                    ('32111111-1111-1111-1111-111111111001', 'Las Vegas', 'NV', 36.1699, -115.1398, 30, false, true, CURRENT_TIMESTAMP, NULL),
                    ('32111111-1111-1111-1111-111111111002', 'Reno', 'NV', 39.5296, -119.8138, 25, false, true, CURRENT_TIMESTAMP, NULL),

                    -- NEW HAMPSHIRE
                    ('33111111-1111-1111-1111-111111111001', 'Manchester', 'NH', 42.9956, -71.4548, 25, false, true, CURRENT_TIMESTAMP, NULL),

                    -- NEW JERSEY
                    ('34111111-1111-1111-1111-111111111001', 'Newark', 'NJ', 40.7357, -74.1724, 30, false, true, CURRENT_TIMESTAMP, NULL),

                    -- NEW MEXICO
                    ('35111111-1111-1111-1111-111111111001', 'Albuquerque', 'NM', 35.0844, -106.6504, 30, false, true, CURRENT_TIMESTAMP, NULL),

                    -- NEW YORK
                    ('36111111-1111-1111-1111-111111111001', 'New York City', 'NY', 40.7128, -74.0060, 40, false, true, CURRENT_TIMESTAMP, NULL),
                    ('36111111-1111-1111-1111-111111111002', 'Buffalo', 'NY', 42.8864, -78.8784, 25, false, true, CURRENT_TIMESTAMP, NULL),
                    ('36111111-1111-1111-1111-111111111003', 'Albany', 'NY', 42.6526, -73.7562, 25, false, true, CURRENT_TIMESTAMP, NULL),

                    -- NORTH CAROLINA
                    ('37111111-1111-1111-1111-111111111001', 'Charlotte', 'NC', 35.2271, -80.8431, 30, false, true, CURRENT_TIMESTAMP, NULL),
                    ('37111111-1111-1111-1111-111111111002', 'Raleigh', 'NC', 35.7796, -78.6382, 30, false, true, CURRENT_TIMESTAMP, NULL),

                    -- NORTH DAKOTA
                    ('38111111-1111-1111-1111-111111111001', 'Fargo', 'ND', 46.8772, -96.7898, 30, false, true, CURRENT_TIMESTAMP, NULL),
                    ('38111111-1111-1111-1111-111111111002', 'Bismarck', 'ND', 46.8083, -100.7837, 25, false, true, CURRENT_TIMESTAMP, NULL),

                    -- OHIO
                    ('39111111-1111-1111-1111-111111111001', 'Cleveland', 'OH', 41.4993, -81.6944, 30, false, true, CURRENT_TIMESTAMP, NULL),
                    ('39111111-1111-1111-1111-111111111002', 'Columbus', 'OH', 39.9612, -82.9988, 30, false, true, CURRENT_TIMESTAMP, NULL),
                    ('39111111-1111-1111-1111-111111111003', 'Cincinnati', 'OH', 39.1031, -84.5120, 30, false, true, CURRENT_TIMESTAMP, NULL),
                    ('39111111-1111-1111-1111-111111111004', 'Toledo', 'OH', 41.6528, -83.5379, 25, false, true, CURRENT_TIMESTAMP, NULL),
                    ('39111111-1111-1111-1111-111111111005', 'Akron', 'OH', 41.0823, -81.5178, 25, false, true, CURRENT_TIMESTAMP, NULL),

                    -- OKLAHOMA
                    ('40111111-1111-1111-1111-111111111001', 'Oklahoma City', 'OK', 35.4676, -97.5164, 30, false, true, CURRENT_TIMESTAMP, NULL),

                    -- OREGON
                    ('41111111-1111-1111-1111-111111111001', 'Portland', 'OR', 45.5152, -122.6784, 30, false, true, CURRENT_TIMESTAMP, NULL),

                    -- PENNSYLVANIA
                    ('42111111-1111-1111-1111-111111111001', 'Philadelphia', 'PA', 39.9526, -75.1652, 35, false, true, CURRENT_TIMESTAMP, NULL),
                    ('42111111-1111-1111-1111-111111111002', 'Pittsburgh', 'PA', 40.4406, -79.9959, 30, false, true, CURRENT_TIMESTAMP, NULL),

                    -- RHODE ISLAND
                    ('44111111-1111-1111-1111-111111111001', 'Providence', 'RI', 41.8240, -71.4128, 25, false, true, CURRENT_TIMESTAMP, NULL),

                    -- SOUTH CAROLINA
                    ('45111111-1111-1111-1111-111111111001', 'Charleston', 'SC', 32.7765, -79.9711, 25, false, true, CURRENT_TIMESTAMP, NULL),

                    -- SOUTH DAKOTA
                    ('46111111-1111-1111-1111-111111111001', 'Sioux Falls', 'SD', 43.5446, -96.7311, 30, false, true, CURRENT_TIMESTAMP, NULL),
                    ('46111111-1111-1111-1111-111111111002', 'Rapid City', 'SD', 44.0805, -103.2310, 25, false, true, CURRENT_TIMESTAMP, NULL),

                    -- TENNESSEE
                    ('47111111-1111-1111-1111-111111111001', 'Nashville', 'TN', 36.1627, -86.7816, 30, false, true, CURRENT_TIMESTAMP, NULL),
                    ('47111111-1111-1111-1111-111111111002', 'Memphis', 'TN', 35.1495, -90.0490, 30, false, true, CURRENT_TIMESTAMP, NULL),

                    -- TEXAS
                    ('48111111-1111-1111-1111-111111111001', 'Houston', 'TX', 29.7604, -95.3698, 40, false, true, CURRENT_TIMESTAMP, NULL),
                    ('48111111-1111-1111-1111-111111111002', 'Dallas-Fort Worth', 'TX', 32.7767, -96.7970, 40, false, true, CURRENT_TIMESTAMP, NULL),
                    ('48111111-1111-1111-1111-111111111003', 'Austin', 'TX', 30.2672, -97.7431, 30, false, true, CURRENT_TIMESTAMP, NULL),
                    ('48111111-1111-1111-1111-111111111004', 'San Antonio', 'TX', 29.4241, -98.4936, 30, false, true, CURRENT_TIMESTAMP, NULL),

                    -- UTAH
                    ('49111111-1111-1111-1111-111111111001', 'Salt Lake City', 'UT', 40.7608, -111.8910, 30, false, true, CURRENT_TIMESTAMP, NULL),

                    -- VERMONT
                    ('50111111-1111-1111-1111-111111111001', 'Burlington', 'VT', 44.4759, -73.2121, 25, false, true, CURRENT_TIMESTAMP, NULL),

                    -- VIRGINIA
                    ('51111111-1111-1111-1111-111111111001', 'Richmond', 'VA', 37.5407, -77.4360, 30, false, true, CURRENT_TIMESTAMP, NULL),

                    -- WASHINGTON
                    ('53111111-1111-1111-1111-111111111001', 'Seattle', 'WA', 47.6062, -122.3321, 35, false, true, CURRENT_TIMESTAMP, NULL),

                    -- WEST VIRGINIA
                    ('54111111-1111-1111-1111-111111111001', 'Charleston', 'WV', 38.3498, -81.6326, 25, false, true, CURRENT_TIMESTAMP, NULL),
                    ('54111111-1111-1111-1111-111111111002', 'Huntington', 'WV', 38.4192, -82.4452, 30, false, true, CURRENT_TIMESTAMP, NULL),

                    -- WISCONSIN
                    ('55111111-1111-1111-1111-111111111001', 'Milwaukee', 'WI', 43.0389, -87.9065, 30, false, true, CURRENT_TIMESTAMP, NULL),

                    -- WYOMING
                    ('56111111-1111-1111-1111-111111111001', 'Cheyenne', 'WY', 41.1400, -104.8202, 25, false, true, CURRENT_TIMESTAMP, NULL),
                    ('56111111-1111-1111-1111-111111111002', 'Casper', 'WY', 42.8501, -106.3252, 25, false, true, CURRENT_TIMESTAMP, NULL)
                ON CONFLICT (id) DO NOTHING;

                -- ========================================
                -- PART B: SYSTEM BADGES (11)
                -- ========================================

                INSERT INTO badges.badges (""Id"", ""Name"", ""ImageUrl"", ""BlobName"", ""Position"", ""DisplayOrder"", ""IsSystem"", ""IsActive"", ""CreatedAt"", ""UpdatedAt"", ""DefaultDurationDays"")
                VALUES
                    ('00000000-0000-0000-0000-000000000001', 'New Event', '', '', '1', 1, true, true, CURRENT_TIMESTAMP, NULL, NULL),
                    ('00000000-0000-0000-0000-000000000002', 'New', '', '', '1', 2, true, true, CURRENT_TIMESTAMP, NULL, NULL),
                    ('00000000-0000-0000-0000-000000000003', 'Canceled', '', '', '0', 3, true, true, CURRENT_TIMESTAMP, NULL, NULL),
                    ('00000000-0000-0000-0000-000000000004', 'New Year', '', '', '1', 4, true, true, CURRENT_TIMESTAMP, NULL, NULL),
                    ('00000000-0000-0000-0000-000000000005', 'Valentines', '', '', '1', 5, true, true, CURRENT_TIMESTAMP, NULL, NULL),
                    ('00000000-0000-0000-0000-000000000006', 'Christmas', '', '', '1', 6, true, true, CURRENT_TIMESTAMP, NULL, NULL),
                    ('00000000-0000-0000-0000-000000000007', 'Thanksgiving', '', '', '1', 7, true, true, CURRENT_TIMESTAMP, NULL, NULL),
                    ('00000000-0000-0000-0000-000000000008', 'Halloween', '', '', '1', 8, true, true, CURRENT_TIMESTAMP, NULL, NULL),
                    ('00000000-0000-0000-0000-000000000009', 'Easter', '', '', '1', 9, true, true, CURRENT_TIMESTAMP, NULL, NULL),
                    ('00000000-0000-0000-0000-000000000010', 'Sinhala Tamil New Year', '', '', '1', 10, true, true, CURRENT_TIMESTAMP, NULL, NULL),
                    ('00000000-0000-0000-0000-000000000011', 'Vesak', '', '', '1', 11, true, true, CURRENT_TIMESTAMP, NULL, NULL)
                ON CONFLICT (""Id"") DO NOTHING;
            ");

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("0b9effc0-322f-8026-85c6-747e381b41e6"),
                column: "created_at",
                value: new DateTime(2026, 2, 12, 0, 7, 11, 247, DateTimeKind.Utc).AddTicks(1140));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("2d87836d-9322-d4b1-b4ec-b5b73eca9ad9"),
                column: "created_at",
                value: new DateTime(2026, 2, 12, 0, 7, 11, 247, DateTimeKind.Utc).AddTicks(1256));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("31f73d61-6c12-1252-f5ab-10d9d47eba46"),
                column: "created_at",
                value: new DateTime(2026, 2, 12, 0, 7, 11, 247, DateTimeKind.Utc).AddTicks(1058));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4de1eacb-273a-ab85-e811-d60addb4ae30"),
                column: "created_at",
                value: new DateTime(2026, 2, 12, 0, 7, 11, 247, DateTimeKind.Utc).AddTicks(1190));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4e57a1be-7a76-833e-003f-b2e3182f29f0"),
                column: "created_at",
                value: new DateTime(2026, 2, 12, 0, 7, 11, 247, DateTimeKind.Utc).AddTicks(1216));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("6313b249-2620-3e97-c1bd-f1d50814156d"),
                column: "created_at",
                value: new DateTime(2026, 2, 12, 0, 7, 11, 247, DateTimeKind.Utc).AddTicks(1384));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("70ab7cff-d677-f4bd-b331-f02908ee3347"),
                column: "created_at",
                value: new DateTime(2026, 2, 12, 0, 7, 11, 247, DateTimeKind.Utc).AddTicks(1165));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("80cd50b4-7630-f5d0-1f9a-a7c480347dcf"),
                column: "created_at",
                value: new DateTime(2026, 2, 12, 0, 7, 11, 247, DateTimeKind.Utc).AddTicks(1114));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("9b07d22a-d0bf-ad27-01bf-0c8410d4b9e1"),
                column: "created_at",
                value: new DateTime(2026, 2, 12, 0, 7, 11, 247, DateTimeKind.Utc).AddTicks(1337));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("c5735376-4831-c12b-a01e-672efee6c8e3"),
                column: "created_at",
                value: new DateTime(2026, 2, 12, 0, 7, 11, 247, DateTimeKind.Utc).AddTicks(1313));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("cdaa97c0-e68f-2819-984e-63bb9dcf35a6"),
                column: "created_at",
                value: new DateTime(2026, 2, 12, 0, 7, 11, 247, DateTimeKind.Utc).AddTicks(1283));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("e1d5afac-09d6-ef55-a529-f5bf473ef103"),
                column: "created_at",
                value: new DateTime(2026, 2, 12, 0, 7, 11, 247, DateTimeKind.Utc).AddTicks(1360));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Phase 6A.104: Remove seeded metro areas and badges
            migrationBuilder.Sql(@"
                -- Delete state-level metro areas
                DELETE FROM events.metro_areas
                WHERE id IN (
                    '01000000-0000-0000-0000-000000000001', '02000000-0000-0000-0000-000000000001',
                    '04000000-0000-0000-0000-000000000001', '05000000-0000-0000-0000-000000000001',
                    '06000000-0000-0000-0000-000000000001', '08000000-0000-0000-0000-000000000001',
                    '09000000-0000-0000-0000-000000000001', '10000000-0000-0000-0000-000000000001',
                    '12000000-0000-0000-0000-000000000001', '13000000-0000-0000-0000-000000000001',
                    '15000000-0000-0000-0000-000000000001', '16000000-0000-0000-0000-000000000001',
                    '17000000-0000-0000-0000-000000000001', '18000000-0000-0000-0000-000000000001',
                    '19000000-0000-0000-0000-000000000001', '20000000-0000-0000-0000-000000000001',
                    '21000000-0000-0000-0000-000000000001', '22000000-0000-0000-0000-000000000001',
                    '23000000-0000-0000-0000-000000000001', '24000000-0000-0000-0000-000000000001',
                    '25000000-0000-0000-0000-000000000001', '26000000-0000-0000-0000-000000000001',
                    '27000000-0000-0000-0000-000000000001', '28000000-0000-0000-0000-000000000001',
                    '29000000-0000-0000-0000-000000000001', '30000000-0000-0000-0000-000000000001',
                    '31000000-0000-0000-0000-000000000001', '32000000-0000-0000-0000-000000000001',
                    '33000000-0000-0000-0000-000000000001', '34000000-0000-0000-0000-000000000001',
                    '35000000-0000-0000-0000-000000000001', '36000000-0000-0000-0000-000000000001',
                    '37000000-0000-0000-0000-000000000001', '38000000-0000-0000-0000-000000000001',
                    '39000000-0000-0000-0000-000000000001', '40000000-0000-0000-0000-000000000001',
                    '41000000-0000-0000-0000-000000000001', '42000000-0000-0000-0000-000000000001',
                    '44000000-0000-0000-0000-000000000001', '45000000-0000-0000-0000-000000000001',
                    '46000000-0000-0000-0000-000000000001', '47000000-0000-0000-0000-000000000001',
                    '48000000-0000-0000-0000-000000000001', '49000000-0000-0000-0000-000000000001',
                    '50000000-0000-0000-0000-000000000001', '51000000-0000-0000-0000-000000000001',
                    '53000000-0000-0000-0000-000000000001', '54000000-0000-0000-0000-000000000001',
                    '55000000-0000-0000-0000-000000000001', '56000000-0000-0000-0000-000000000001'
                );

                -- Delete city-level metro areas (using LIKE pattern for efficiency)
                DELETE FROM events.metro_areas
                WHERE id::text LIKE '%111111-1111-1111-1111-111111111%';

                -- Delete system badges
                DELETE FROM badges.badges
                WHERE ""Id"" IN (
                    '00000000-0000-0000-0000-000000000001', '00000000-0000-0000-0000-000000000002',
                    '00000000-0000-0000-0000-000000000003', '00000000-0000-0000-0000-000000000004',
                    '00000000-0000-0000-0000-000000000005', '00000000-0000-0000-0000-000000000006',
                    '00000000-0000-0000-0000-000000000007', '00000000-0000-0000-0000-000000000008',
                    '00000000-0000-0000-0000-000000000009', '00000000-0000-0000-0000-000000000010',
                    '00000000-0000-0000-0000-000000000011'
                );
            ");

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("0b9effc0-322f-8026-85c6-747e381b41e6"),
                column: "created_at",
                value: new DateTime(2026, 2, 11, 20, 8, 24, 135, DateTimeKind.Utc).AddTicks(4640));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("2d87836d-9322-d4b1-b4ec-b5b73eca9ad9"),
                column: "created_at",
                value: new DateTime(2026, 2, 11, 20, 8, 24, 135, DateTimeKind.Utc).AddTicks(4981));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("31f73d61-6c12-1252-f5ab-10d9d47eba46"),
                column: "created_at",
                value: new DateTime(2026, 2, 11, 20, 8, 24, 135, DateTimeKind.Utc).AddTicks(4554));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4de1eacb-273a-ab85-e811-d60addb4ae30"),
                column: "created_at",
                value: new DateTime(2026, 2, 11, 20, 8, 24, 135, DateTimeKind.Utc).AddTicks(4696));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("4e57a1be-7a76-833e-003f-b2e3182f29f0"),
                column: "created_at",
                value: new DateTime(2026, 2, 11, 20, 8, 24, 135, DateTimeKind.Utc).AddTicks(4908));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("6313b249-2620-3e97-c1bd-f1d50814156d"),
                column: "created_at",
                value: new DateTime(2026, 2, 11, 20, 8, 24, 135, DateTimeKind.Utc).AddTicks(5116));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("70ab7cff-d677-f4bd-b331-f02908ee3347"),
                column: "created_at",
                value: new DateTime(2026, 2, 11, 20, 8, 24, 135, DateTimeKind.Utc).AddTicks(4666));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("80cd50b4-7630-f5d0-1f9a-a7c480347dcf"),
                column: "created_at",
                value: new DateTime(2026, 2, 11, 20, 8, 24, 135, DateTimeKind.Utc).AddTicks(4610));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("9b07d22a-d0bf-ad27-01bf-0c8410d4b9e1"),
                column: "created_at",
                value: new DateTime(2026, 2, 11, 20, 8, 24, 135, DateTimeKind.Utc).AddTicks(5069));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("c5735376-4831-c12b-a01e-672efee6c8e3"),
                column: "created_at",
                value: new DateTime(2026, 2, 11, 20, 8, 24, 135, DateTimeKind.Utc).AddTicks(5044));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("cdaa97c0-e68f-2819-984e-63bb9dcf35a6"),
                column: "created_at",
                value: new DateTime(2026, 2, 11, 20, 8, 24, 135, DateTimeKind.Utc).AddTicks(5009));

            migrationBuilder.UpdateData(
                schema: "reference_data",
                table: "reference_values",
                keyColumn: "id",
                keyValue: new Guid("e1d5afac-09d6-ef55-a529-f5bf473ef103"),
                column: "created_at",
                value: new DateTime(2026, 2, 11, 20, 8, 24, 135, DateTimeKind.Utc).AddTicks(5092));
        }
    }
}
