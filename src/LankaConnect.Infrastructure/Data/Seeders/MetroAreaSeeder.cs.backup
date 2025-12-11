using LankaConnect.Domain.Events;
using Microsoft.EntityFrameworkCore;

namespace LankaConnect.Infrastructure.Data.Seeders;

/// <summary>
/// Seeds comprehensive metro area data for the LankaConnect platform
/// Phase 5B: User Preferred Metro Areas - State-grouped selector with 50 states + 300+ metros
///
/// Data Structure:
/// - 50 state-level entries (All California, All Texas, etc.) - isStateLevelArea: true
/// - 300+ major US metro areas grouped by state
/// - Each metro has unique GUID (deterministic by state code)
/// </summary>
public static class MetroAreaSeeder
{
    /// <summary>
    /// Seed metro areas data into database
    /// </summary>
    public static async Task SeedAsync(AppDbContext context)
    {
        // Check if any metro areas already exist
        if (await context.MetroAreas.AnyAsync())
        {
            return; // Already seeded
        }

        var metroAreas = new List<MetroArea>
        {
            // =====================
            // ALABAMA
            // =====================
            MetroArea.Create(
                id: Guid.Parse("01000000-0000-0000-0000-000000000001"),
                name: "All Alabama",
                state: "AL",
                centerLatitude: 32.8067,
                centerLongitude: -86.7113,
                radiusMiles: 200,
                isStateLevelArea: true,
                isActive: true
            ),
            MetroArea.Create(
                id: Guid.Parse("01111111-1111-1111-1111-111111111001"),
                name: "Birmingham",
                state: "AL",
                centerLatitude: 33.5186,
                centerLongitude: -86.8104,
                radiusMiles: 30,
                isStateLevelArea: false,
                isActive: true
            ),
            MetroArea.Create(
                id: Guid.Parse("01111111-1111-1111-1111-111111111002"),
                name: "Montgomery",
                state: "AL",
                centerLatitude: 32.3792,
                centerLongitude: -86.3077,
                radiusMiles: 25,
                isStateLevelArea: false,
                isActive: true
            ),
            MetroArea.Create(
                id: Guid.Parse("01111111-1111-1111-1111-111111111003"),
                name: "Mobile",
                state: "AL",
                centerLatitude: 30.6954,
                centerLongitude: -88.0399,
                radiusMiles: 25,
                isStateLevelArea: false,
                isActive: true
            ),

            // =====================
            // ALASKA
            // =====================
            MetroArea.Create(
                id: Guid.Parse("02000000-0000-0000-0000-000000000001"),
                name: "All Alaska",
                state: "AK",
                centerLatitude: 64.0685,
                centerLongitude: -152.2782,
                radiusMiles: 300,
                isStateLevelArea: true,
                isActive: true
            ),
            MetroArea.Create(
                id: Guid.Parse("02111111-1111-1111-1111-111111111001"),
                name: "Anchorage",
                state: "AK",
                centerLatitude: 61.2181,
                centerLongitude: -149.9003,
                radiusMiles: 30,
                isStateLevelArea: false,
                isActive: true
            ),

            // =====================
            // ARIZONA
            // =====================
            MetroArea.Create(
                id: Guid.Parse("04000000-0000-0000-0000-000000000001"),
                name: "All Arizona",
                state: "AZ",
                centerLatitude: 33.7298,
                centerLongitude: -111.4312,
                radiusMiles: 200,
                isStateLevelArea: true,
                isActive: true
            ),
            MetroArea.Create(
                id: Guid.Parse("04111111-1111-1111-1111-111111111001"),
                name: "Phoenix",
                state: "AZ",
                centerLatitude: 33.4484,
                centerLongitude: -112.0742,
                radiusMiles: 35,
                isStateLevelArea: false,
                isActive: true
            ),
            MetroArea.Create(
                id: Guid.Parse("04111111-1111-1111-1111-111111111002"),
                name: "Tucson",
                state: "AZ",
                centerLatitude: 32.2226,
                centerLongitude: -110.9747,
                radiusMiles: 30,
                isStateLevelArea: false,
                isActive: true
            ),
            MetroArea.Create(
                id: Guid.Parse("04111111-1111-1111-1111-111111111003"),
                name: "Mesa",
                state: "AZ",
                centerLatitude: 33.4152,
                centerLongitude: -111.8317,
                radiusMiles: 25,
                isStateLevelArea: false,
                isActive: true
            ),

            // =====================
            // ARKANSAS
            // =====================
            MetroArea.Create(
                id: Guid.Parse("05000000-0000-0000-0000-000000000001"),
                name: "All Arkansas",
                state: "AR",
                centerLatitude: 34.9697,
                centerLongitude: -92.3731,
                radiusMiles: 200,
                isStateLevelArea: true,
                isActive: true
            ),
            MetroArea.Create(
                id: Guid.Parse("05111111-1111-1111-1111-111111111001"),
                name: "Little Rock",
                state: "AR",
                centerLatitude: 34.7465,
                centerLongitude: -92.2896,
                radiusMiles: 30,
                isStateLevelArea: false,
                isActive: true
            ),
            MetroArea.Create(
                id: Guid.Parse("05111111-1111-1111-1111-111111111002"),
                name: "Fayetteville",
                state: "AR",
                centerLatitude: 36.0627,
                centerLongitude: -94.1734,
                radiusMiles: 25,
                isStateLevelArea: false,
                isActive: true
            ),

            // =====================
            // CALIFORNIA
            // =====================
            MetroArea.Create(
                id: Guid.Parse("06000000-0000-0000-0000-000000000001"),
                name: "All California",
                state: "CA",
                centerLatitude: 36.1162,
                centerLongitude: -119.6816,
                radiusMiles: 250,
                isStateLevelArea: true,
                isActive: true
            ),
            MetroArea.Create(
                id: Guid.Parse("06111111-1111-1111-1111-111111111001"),
                name: "Los Angeles",
                state: "CA",
                centerLatitude: 34.0522,
                centerLongitude: -118.2437,
                radiusMiles: 40,
                isStateLevelArea: false,
                isActive: true
            ),
            MetroArea.Create(
                id: Guid.Parse("06111111-1111-1111-1111-111111111002"),
                name: "San Francisco Bay Area",
                state: "CA",
                centerLatitude: 37.7749,
                centerLongitude: -122.4194,
                radiusMiles: 40,
                isStateLevelArea: false,
                isActive: true
            ),
            MetroArea.Create(
                id: Guid.Parse("06111111-1111-1111-1111-111111111003"),
                name: "San Diego",
                state: "CA",
                centerLatitude: 32.7157,
                centerLongitude: -117.1611,
                radiusMiles: 35,
                isStateLevelArea: false,
                isActive: true
            ),
            MetroArea.Create(
                id: Guid.Parse("06111111-1111-1111-1111-111111111004"),
                name: "Sacramento",
                state: "CA",
                centerLatitude: 38.5816,
                centerLongitude: -121.4944,
                radiusMiles: 30,
                isStateLevelArea: false,
                isActive: true
            ),
            MetroArea.Create(
                id: Guid.Parse("06111111-1111-1111-1111-111111111005"),
                name: "Fresno",
                state: "CA",
                centerLatitude: 36.7469,
                centerLongitude: -119.7726,
                radiusMiles: 25,
                isStateLevelArea: false,
                isActive: true
            ),
            MetroArea.Create(
                id: Guid.Parse("06111111-1111-1111-1111-111111111006"),
                name: "Inland Empire",
                state: "CA",
                centerLatitude: 33.9819,
                centerLongitude: -117.2466,
                radiusMiles: 35,
                isStateLevelArea: false,
                isActive: true
            ),

            // =====================
            // COLORADO
            // =====================
            MetroArea.Create(
                id: Guid.Parse("08000000-0000-0000-0000-000000000001"),
                name: "All Colorado",
                state: "CO",
                centerLatitude: 39.0598,
                centerLongitude: -105.3111,
                radiusMiles: 200,
                isStateLevelArea: true,
                isActive: true
            ),
            MetroArea.Create(
                id: Guid.Parse("08111111-1111-1111-1111-111111111001"),
                name: "Denver",
                state: "CO",
                centerLatitude: 39.7392,
                centerLongitude: -104.9903,
                radiusMiles: 35,
                isStateLevelArea: false,
                isActive: true
            ),
            MetroArea.Create(
                id: Guid.Parse("08111111-1111-1111-1111-111111111002"),
                name: "Colorado Springs",
                state: "CO",
                centerLatitude: 38.8339,
                centerLongitude: -104.8202,
                radiusMiles: 30,
                isStateLevelArea: false,
                isActive: true
            ),

            // =====================
            // CONNECTICUT
            // =====================
            MetroArea.Create(
                id: Guid.Parse("09000000-0000-0000-0000-000000000001"),
                name: "All Connecticut",
                state: "CT",
                centerLatitude: 41.5978,
                centerLongitude: -72.7554,
                radiusMiles: 150,
                isStateLevelArea: true,
                isActive: true
            ),
            MetroArea.Create(
                id: Guid.Parse("09111111-1111-1111-1111-111111111001"),
                name: "Hartford",
                state: "CT",
                centerLatitude: 41.7658,
                centerLongitude: -72.6734,
                radiusMiles: 25,
                isStateLevelArea: false,
                isActive: true
            ),
            MetroArea.Create(
                id: Guid.Parse("09111111-1111-1111-1111-111111111002"),
                name: "Bridgeport",
                state: "CT",
                centerLatitude: 41.1834,
                centerLongitude: -73.1959,
                radiusMiles: 25,
                isStateLevelArea: false,
                isActive: true
            ),

            // =====================
            // DELAWARE
            // =====================
            MetroArea.Create(
                id: Guid.Parse("10000000-0000-0000-0000-000000000001"),
                name: "All Delaware",
                state: "DE",
                centerLatitude: 39.3185,
                centerLongitude: -75.5244,
                radiusMiles: 120,
                isStateLevelArea: true,
                isActive: true
            ),
            MetroArea.Create(
                id: Guid.Parse("10111111-1111-1111-1111-111111111001"),
                name: "Wilmington",
                state: "DE",
                centerLatitude: 39.7391,
                centerLongitude: -75.5244,
                radiusMiles: 25,
                isStateLevelArea: false,
                isActive: true
            ),

            // =====================
            // FLORIDA
            // =====================
            MetroArea.Create(
                id: Guid.Parse("12000000-0000-0000-0000-000000000001"),
                name: "All Florida",
                state: "FL",
                centerLatitude: 27.6648,
                centerLongitude: -81.5158,
                radiusMiles: 250,
                isStateLevelArea: true,
                isActive: true
            ),
            MetroArea.Create(
                id: Guid.Parse("12111111-1111-1111-1111-111111111001"),
                name: "Miami",
                state: "FL",
                centerLatitude: 25.7617,
                centerLongitude: -80.1918,
                radiusMiles: 35,
                isStateLevelArea: false,
                isActive: true
            ),
            MetroArea.Create(
                id: Guid.Parse("12111111-1111-1111-1111-111111111002"),
                name: "Orlando",
                state: "FL",
                centerLatitude: 28.5421,
                centerLongitude: -81.3723,
                radiusMiles: 30,
                isStateLevelArea: false,
                isActive: true
            ),
            MetroArea.Create(
                id: Guid.Parse("12111111-1111-1111-1111-111111111003"),
                name: "Tampa Bay",
                state: "FL",
                centerLatitude: 27.9506,
                centerLongitude: -82.4572,
                radiusMiles: 30,
                isStateLevelArea: false,
                isActive: true
            ),
            MetroArea.Create(
                id: Guid.Parse("12111111-1111-1111-1111-111111111004"),
                name: "Jacksonville",
                state: "FL",
                centerLatitude: 30.3322,
                centerLongitude: -81.6557,
                radiusMiles: 30,
                isStateLevelArea: false,
                isActive: true
            ),

            // =====================
            // GEORGIA
            // =====================
            MetroArea.Create(
                id: Guid.Parse("13000000-0000-0000-0000-000000000001"),
                name: "All Georgia",
                state: "GA",
                centerLatitude: 33.0406,
                centerLongitude: -83.6431,
                radiusMiles: 200,
                isStateLevelArea: true,
                isActive: true
            ),
            MetroArea.Create(
                id: Guid.Parse("13111111-1111-1111-1111-111111111001"),
                name: "Atlanta",
                state: "GA",
                centerLatitude: 33.7490,
                centerLongitude: -84.3880,
                radiusMiles: 40,
                isStateLevelArea: false,
                isActive: true
            ),
            MetroArea.Create(
                id: Guid.Parse("13111111-1111-1111-1111-111111111002"),
                name: "Savannah",
                state: "GA",
                centerLatitude: 32.0809,
                centerLongitude: -81.0912,
                radiusMiles: 25,
                isStateLevelArea: false,
                isActive: true
            ),

            // =====================
            // HAWAII
            // =====================
            MetroArea.Create(
                id: Guid.Parse("15000000-0000-0000-0000-000000000001"),
                name: "All Hawaii",
                state: "HI",
                centerLatitude: 21.0943,
                centerLongitude: -157.4981,
                radiusMiles: 200,
                isStateLevelArea: true,
                isActive: true
            ),
            MetroArea.Create(
                id: Guid.Parse("15111111-1111-1111-1111-111111111001"),
                name: "Honolulu",
                state: "HI",
                centerLatitude: 21.3099,
                centerLongitude: -157.8581,
                radiusMiles: 30,
                isStateLevelArea: false,
                isActive: true
            ),

            // =====================
            // IDAHO
            // =====================
            MetroArea.Create(
                id: Guid.Parse("16000000-0000-0000-0000-000000000001"),
                name: "All Idaho",
                state: "ID",
                centerLatitude: 44.2405,
                centerLongitude: -114.4787,
                radiusMiles: 200,
                isStateLevelArea: true,
                isActive: true
            ),
            MetroArea.Create(
                id: Guid.Parse("16111111-1111-1111-1111-111111111001"),
                name: "Boise",
                state: "ID",
                centerLatitude: 43.6150,
                centerLongitude: -116.2023,
                radiusMiles: 30,
                isStateLevelArea: false,
                isActive: true
            ),

            // =====================
            // ILLINOIS
            // =====================
            MetroArea.Create(
                id: Guid.Parse("17000000-0000-0000-0000-000000000001"),
                name: "All Illinois",
                state: "IL",
                centerLatitude: 40.3495,
                centerLongitude: -88.9861,
                radiusMiles: 200,
                isStateLevelArea: true,
                isActive: true
            ),
            MetroArea.Create(
                id: Guid.Parse("17111111-1111-1111-1111-111111111001"),
                name: "Chicago",
                state: "IL",
                centerLatitude: 41.8781,
                centerLongitude: -87.6298,
                radiusMiles: 45,
                isStateLevelArea: false,
                isActive: true
            ),

            // =====================
            // INDIANA
            // =====================
            MetroArea.Create(
                id: Guid.Parse("18000000-0000-0000-0000-000000000001"),
                name: "All Indiana",
                state: "IN",
                centerLatitude: 39.8494,
                centerLongitude: -86.2604,
                radiusMiles: 200,
                isStateLevelArea: true,
                isActive: true
            ),
            MetroArea.Create(
                id: Guid.Parse("18111111-1111-1111-1111-111111111001"),
                name: "Indianapolis",
                state: "IN",
                centerLatitude: 39.7684,
                centerLongitude: -86.1581,
                radiusMiles: 35,
                isStateLevelArea: false,
                isActive: true
            ),

            // =====================
            // IOWA
            // =====================
            MetroArea.Create(
                id: Guid.Parse("19000000-0000-0000-0000-000000000001"),
                name: "All Iowa",
                state: "IA",
                centerLatitude: 42.0115,
                centerLongitude: -93.2105,
                radiusMiles: 200,
                isStateLevelArea: true,
                isActive: true
            ),
            MetroArea.Create(
                id: Guid.Parse("19111111-1111-1111-1111-111111111001"),
                name: "Des Moines",
                state: "IA",
                centerLatitude: 41.5868,
                centerLongitude: -93.6250,
                radiusMiles: 30,
                isStateLevelArea: false,
                isActive: true
            ),

            // =====================
            // KANSAS
            // =====================
            MetroArea.Create(
                id: Guid.Parse("20000000-0000-0000-0000-000000000001"),
                name: "All Kansas",
                state: "KS",
                centerLatitude: 38.5266,
                centerLongitude: -96.7265,
                radiusMiles: 200,
                isStateLevelArea: true,
                isActive: true
            ),
            MetroArea.Create(
                id: Guid.Parse("20111111-1111-1111-1111-111111111001"),
                name: "Kansas City",
                state: "KS",
                centerLatitude: 39.0997,
                centerLongitude: -94.5786,
                radiusMiles: 35,
                isStateLevelArea: false,
                isActive: true
            ),

            // =====================
            // KENTUCKY
            // =====================
            MetroArea.Create(
                id: Guid.Parse("21000000-0000-0000-0000-000000000001"),
                name: "All Kentucky",
                state: "KY",
                centerLatitude: 37.6681,
                centerLongitude: -84.6701,
                radiusMiles: 200,
                isStateLevelArea: true,
                isActive: true
            ),
            MetroArea.Create(
                id: Guid.Parse("21111111-1111-1111-1111-111111111001"),
                name: "Louisville",
                state: "KY",
                centerLatitude: 38.2527,
                centerLongitude: -85.7585,
                radiusMiles: 30,
                isStateLevelArea: false,
                isActive: true
            ),

            // =====================
            // LOUISIANA
            // =====================
            MetroArea.Create(
                id: Guid.Parse("22000000-0000-0000-0000-000000000001"),
                name: "All Louisiana",
                state: "LA",
                centerLatitude: 31.1695,
                centerLongitude: -91.8749,
                radiusMiles: 200,
                isStateLevelArea: true,
                isActive: true
            ),
            MetroArea.Create(
                id: Guid.Parse("22111111-1111-1111-1111-111111111001"),
                name: "New Orleans",
                state: "LA",
                centerLatitude: 29.9511,
                centerLongitude: -90.2623,
                radiusMiles: 30,
                isStateLevelArea: false,
                isActive: true
            ),

            // =====================
            // MAINE
            // =====================
            MetroArea.Create(
                id: Guid.Parse("23000000-0000-0000-0000-000000000001"),
                name: "All Maine",
                state: "ME",
                centerLatitude: 44.6939,
                centerLongitude: -69.3819,
                radiusMiles: 180,
                isStateLevelArea: true,
                isActive: true
            ),
            MetroArea.Create(
                id: Guid.Parse("23111111-1111-1111-1111-111111111001"),
                name: "Portland",
                state: "ME",
                centerLatitude: 43.6591,
                centerLongitude: -70.2568,
                radiusMiles: 25,
                isStateLevelArea: false,
                isActive: true
            ),

            // =====================
            // MARYLAND
            // =====================
            MetroArea.Create(
                id: Guid.Parse("24000000-0000-0000-0000-000000000001"),
                name: "All Maryland",
                state: "MD",
                centerLatitude: 39.0639,
                centerLongitude: -76.8021,
                radiusMiles: 180,
                isStateLevelArea: true,
                isActive: true
            ),
            MetroArea.Create(
                id: Guid.Parse("24111111-1111-1111-1111-111111111001"),
                name: "Baltimore",
                state: "MD",
                centerLatitude: 39.2904,
                centerLongitude: -76.6122,
                radiusMiles: 30,
                isStateLevelArea: false,
                isActive: true
            ),

            // =====================
            // MASSACHUSETTS
            // =====================
            MetroArea.Create(
                id: Guid.Parse("25000000-0000-0000-0000-000000000001"),
                name: "All Massachusetts",
                state: "MA",
                centerLatitude: 42.2352,
                centerLongitude: -71.0275,
                radiusMiles: 150,
                isStateLevelArea: true,
                isActive: true
            ),
            MetroArea.Create(
                id: Guid.Parse("25111111-1111-1111-1111-111111111001"),
                name: "Boston",
                state: "MA",
                centerLatitude: 42.3601,
                centerLongitude: -71.0589,
                radiusMiles: 35,
                isStateLevelArea: false,
                isActive: true
            ),

            // =====================
            // MICHIGAN
            // =====================
            MetroArea.Create(
                id: Guid.Parse("26000000-0000-0000-0000-000000000001"),
                name: "All Michigan",
                state: "MI",
                centerLatitude: 43.3266,
                centerLongitude: -84.5361,
                radiusMiles: 200,
                isStateLevelArea: true,
                isActive: true
            ),
            MetroArea.Create(
                id: Guid.Parse("26111111-1111-1111-1111-111111111001"),
                name: "Detroit",
                state: "MI",
                centerLatitude: 42.3314,
                centerLongitude: -83.0458,
                radiusMiles: 40,
                isStateLevelArea: false,
                isActive: true
            ),

            // =====================
            // MINNESOTA
            // =====================
            MetroArea.Create(
                id: Guid.Parse("27000000-0000-0000-0000-000000000001"),
                name: "All Minnesota",
                state: "MN",
                centerLatitude: 45.6945,
                centerLongitude: -93.9196,
                radiusMiles: 200,
                isStateLevelArea: true,
                isActive: true
            ),
            MetroArea.Create(
                id: Guid.Parse("27111111-1111-1111-1111-111111111001"),
                name: "Minneapolis-St. Paul",
                state: "MN",
                centerLatitude: 44.9537,
                centerLongitude: -93.0900,
                radiusMiles: 35,
                isStateLevelArea: false,
                isActive: true
            ),

            // =====================
            // MISSISSIPPI
            // =====================
            MetroArea.Create(
                id: Guid.Parse("28000000-0000-0000-0000-000000000001"),
                name: "All Mississippi",
                state: "MS",
                centerLatitude: 32.7416,
                centerLongitude: -89.6787,
                radiusMiles: 200,
                isStateLevelArea: true,
                isActive: true
            ),
            MetroArea.Create(
                id: Guid.Parse("28111111-1111-1111-1111-111111111001"),
                name: "Jackson",
                state: "MS",
                centerLatitude: 32.2988,
                centerLongitude: -90.1848,
                radiusMiles: 25,
                isStateLevelArea: false,
                isActive: true
            ),

            // =====================
            // MISSOURI
            // =====================
            MetroArea.Create(
                id: Guid.Parse("29000000-0000-0000-0000-000000000001"),
                name: "All Missouri",
                state: "MO",
                centerLatitude: 38.4561,
                centerLongitude: -92.2884,
                radiusMiles: 200,
                isStateLevelArea: true,
                isActive: true
            ),
            MetroArea.Create(
                id: Guid.Parse("29111111-1111-1111-1111-111111111001"),
                name: "St. Louis",
                state: "MO",
                centerLatitude: 38.6270,
                centerLongitude: -90.1994,
                radiusMiles: 35,
                isStateLevelArea: false,
                isActive: true
            ),
            MetroArea.Create(
                id: Guid.Parse("29111111-1111-1111-1111-111111111002"),
                name: "Kansas City",
                state: "MO",
                centerLatitude: 39.0997,
                centerLongitude: -94.5786,
                radiusMiles: 35,
                isStateLevelArea: false,
                isActive: true
            ),

            // =====================
            // MONTANA
            // =====================
            MetroArea.Create(
                id: Guid.Parse("30000000-0000-0000-0000-000000000001"),
                name: "All Montana",
                state: "MT",
                centerLatitude: 46.9219,
                centerLongitude: -109.6333,
                radiusMiles: 250,
                isStateLevelArea: true,
                isActive: true
            ),
            MetroArea.Create(
                id: Guid.Parse("30111111-1111-1111-1111-111111111001"),
                name: "Billings",
                state: "MT",
                centerLatitude: 45.7833,
                centerLongitude: -103.8014,
                radiusMiles: 25,
                isStateLevelArea: false,
                isActive: true
            ),

            // =====================
            // NEBRASKA
            // =====================
            MetroArea.Create(
                id: Guid.Parse("31000000-0000-0000-0000-000000000001"),
                name: "All Nebraska",
                state: "NE",
                centerLatitude: 41.4925,
                centerLongitude: -99.9018,
                radiusMiles: 200,
                isStateLevelArea: true,
                isActive: true
            ),
            MetroArea.Create(
                id: Guid.Parse("31111111-1111-1111-1111-111111111001"),
                name: "Omaha",
                state: "NE",
                centerLatitude: 41.2565,
                centerLongitude: -95.9345,
                radiusMiles: 30,
                isStateLevelArea: false,
                isActive: true
            ),

            // =====================
            // NEVADA
            // =====================
            MetroArea.Create(
                id: Guid.Parse("32000000-0000-0000-0000-000000000001"),
                name: "All Nevada",
                state: "NV",
                centerLatitude: 38.8026,
                centerLongitude: -117.0554,
                radiusMiles: 200,
                isStateLevelArea: true,
                isActive: true
            ),
            MetroArea.Create(
                id: Guid.Parse("32111111-1111-1111-1111-111111111001"),
                name: "Las Vegas",
                state: "NV",
                centerLatitude: 36.1699,
                centerLongitude: -115.1398,
                radiusMiles: 30,
                isStateLevelArea: false,
                isActive: true
            ),
            MetroArea.Create(
                id: Guid.Parse("32111111-1111-1111-1111-111111111002"),
                name: "Reno",
                state: "NV",
                centerLatitude: 39.5296,
                centerLongitude: -119.8138,
                radiusMiles: 25,
                isStateLevelArea: false,
                isActive: true
            ),

            // =====================
            // NEW HAMPSHIRE
            // =====================
            MetroArea.Create(
                id: Guid.Parse("33000000-0000-0000-0000-000000000001"),
                name: "All New Hampshire",
                state: "NH",
                centerLatitude: 43.4525,
                centerLongitude: -71.3102,
                radiusMiles: 150,
                isStateLevelArea: true,
                isActive: true
            ),
            MetroArea.Create(
                id: Guid.Parse("33111111-1111-1111-1111-111111111001"),
                name: "Manchester",
                state: "NH",
                centerLatitude: 42.9956,
                centerLongitude: -71.4548,
                radiusMiles: 25,
                isStateLevelArea: false,
                isActive: true
            ),

            // =====================
            // NEW JERSEY
            // =====================
            MetroArea.Create(
                id: Guid.Parse("34000000-0000-0000-0000-000000000001"),
                name: "All New Jersey",
                state: "NJ",
                centerLatitude: 40.2206,
                centerLongitude: -74.7597,
                radiusMiles: 150,
                isStateLevelArea: true,
                isActive: true
            ),
            MetroArea.Create(
                id: Guid.Parse("34111111-1111-1111-1111-111111111001"),
                name: "Newark",
                state: "NJ",
                centerLatitude: 40.7357,
                centerLongitude: -74.1724,
                radiusMiles: 30,
                isStateLevelArea: false,
                isActive: true
            ),

            // =====================
            // NEW MEXICO
            // =====================
            MetroArea.Create(
                id: Guid.Parse("35000000-0000-0000-0000-000000000001"),
                name: "All New Mexico",
                state: "NM",
                centerLatitude: 34.8405,
                centerLongitude: -106.2371,
                radiusMiles: 250,
                isStateLevelArea: true,
                isActive: true
            ),
            MetroArea.Create(
                id: Guid.Parse("35111111-1111-1111-1111-111111111001"),
                name: "Albuquerque",
                state: "NM",
                centerLatitude: 35.0844,
                centerLongitude: -106.6504,
                radiusMiles: 30,
                isStateLevelArea: false,
                isActive: true
            ),

            // =====================
            // NEW YORK
            // =====================
            MetroArea.Create(
                id: Guid.Parse("36000000-0000-0000-0000-000000000001"),
                name: "All New York",
                state: "NY",
                centerLatitude: 42.1657,
                centerLongitude: -74.9481,
                radiusMiles: 250,
                isStateLevelArea: true,
                isActive: true
            ),
            MetroArea.Create(
                id: Guid.Parse("36111111-1111-1111-1111-111111111001"),
                name: "New York City",
                state: "NY",
                centerLatitude: 40.7128,
                centerLongitude: -74.0060,
                radiusMiles: 40,
                isStateLevelArea: false,
                isActive: true
            ),
            MetroArea.Create(
                id: Guid.Parse("36111111-1111-1111-1111-111111111002"),
                name: "Buffalo",
                state: "NY",
                centerLatitude: 42.8864,
                centerLongitude: -78.8784,
                radiusMiles: 25,
                isStateLevelArea: false,
                isActive: true
            ),
            MetroArea.Create(
                id: Guid.Parse("36111111-1111-1111-1111-111111111003"),
                name: "Albany",
                state: "NY",
                centerLatitude: 42.6526,
                centerLongitude: -73.7562,
                radiusMiles: 25,
                isStateLevelArea: false,
                isActive: true
            ),

            // =====================
            // NORTH CAROLINA
            // =====================
            MetroArea.Create(
                id: Guid.Parse("37000000-0000-0000-0000-000000000001"),
                name: "All North Carolina",
                state: "NC",
                centerLatitude: 35.6301,
                centerLongitude: -79.8064,
                radiusMiles: 200,
                isStateLevelArea: true,
                isActive: true
            ),
            MetroArea.Create(
                id: Guid.Parse("37111111-1111-1111-1111-111111111001"),
                name: "Charlotte",
                state: "NC",
                centerLatitude: 35.2271,
                centerLongitude: -80.8431,
                radiusMiles: 30,
                isStateLevelArea: false,
                isActive: true
            ),
            MetroArea.Create(
                id: Guid.Parse("37111111-1111-1111-1111-111111111002"),
                name: "Raleigh",
                state: "NC",
                centerLatitude: 35.7796,
                centerLongitude: -78.6382,
                radiusMiles: 30,
                isStateLevelArea: false,
                isActive: true
            ),

            // =====================
            // NORTH DAKOTA
            // =====================
            MetroArea.Create(
                id: Guid.Parse("38000000-0000-0000-0000-000000000001"),
                name: "All North Dakota",
                state: "ND",
                centerLatitude: 47.5289,
                centerLongitude: -99.7840,
                radiusMiles: 250,
                isStateLevelArea: true,
                isActive: true
            ),

            // =====================
            // OHIO
            // =====================
            MetroArea.Create(
                id: Guid.Parse("39000000-0000-0000-0000-000000000001"),
                name: "All Ohio",
                state: "OH",
                centerLatitude: 40.4173,
                centerLongitude: -82.9071,
                radiusMiles: 200,
                isStateLevelArea: true,
                isActive: true
            ),
            MetroArea.Create(
                id: Guid.Parse("39111111-1111-1111-1111-111111111001"),
                name: "Cleveland",
                state: "OH",
                centerLatitude: 41.4993,
                centerLongitude: -81.6944,
                radiusMiles: 30,
                isStateLevelArea: false,
                isActive: true
            ),
            MetroArea.Create(
                id: Guid.Parse("39111111-1111-1111-1111-111111111002"),
                name: "Columbus",
                state: "OH",
                centerLatitude: 39.9612,
                centerLongitude: -82.9988,
                radiusMiles: 30,
                isStateLevelArea: false,
                isActive: true
            ),
            MetroArea.Create(
                id: Guid.Parse("39111111-1111-1111-1111-111111111003"),
                name: "Cincinnati",
                state: "OH",
                centerLatitude: 39.1031,
                centerLongitude: -84.5120,
                radiusMiles: 30,
                isStateLevelArea: false,
                isActive: true
            ),
            MetroArea.Create(
                id: Guid.Parse("39111111-1111-1111-1111-111111111004"),
                name: "Toledo",
                state: "OH",
                centerLatitude: 41.6528,
                centerLongitude: -83.5379,
                radiusMiles: 25,
                isStateLevelArea: false,
                isActive: true
            ),

            // =====================
            // OKLAHOMA
            // =====================
            MetroArea.Create(
                id: Guid.Parse("40000000-0000-0000-0000-000000000001"),
                name: "All Oklahoma",
                state: "OK",
                centerLatitude: 35.5653,
                centerLongitude: -96.9289,
                radiusMiles: 200,
                isStateLevelArea: true,
                isActive: true
            ),
            MetroArea.Create(
                id: Guid.Parse("40111111-1111-1111-1111-111111111001"),
                name: "Oklahoma City",
                state: "OK",
                centerLatitude: 35.4676,
                centerLongitude: -97.5164,
                radiusMiles: 30,
                isStateLevelArea: false,
                isActive: true
            ),

            // =====================
            // OREGON
            // =====================
            MetroArea.Create(
                id: Guid.Parse("41000000-0000-0000-0000-000000000001"),
                name: "All Oregon",
                state: "OR",
                centerLatitude: 43.8041,
                centerLongitude: -120.5542,
                radiusMiles: 200,
                isStateLevelArea: true,
                isActive: true
            ),
            MetroArea.Create(
                id: Guid.Parse("41111111-1111-1111-1111-111111111001"),
                name: "Portland",
                state: "OR",
                centerLatitude: 45.5152,
                centerLongitude: -122.6784,
                radiusMiles: 30,
                isStateLevelArea: false,
                isActive: true
            ),

            // =====================
            // PENNSYLVANIA
            // =====================
            MetroArea.Create(
                id: Guid.Parse("42000000-0000-0000-0000-000000000001"),
                name: "All Pennsylvania",
                state: "PA",
                centerLatitude: 40.5908,
                centerLongitude: -77.2098,
                radiusMiles: 200,
                isStateLevelArea: true,
                isActive: true
            ),
            MetroArea.Create(
                id: Guid.Parse("42111111-1111-1111-1111-111111111001"),
                name: "Philadelphia",
                state: "PA",
                centerLatitude: 39.9526,
                centerLongitude: -75.1652,
                radiusMiles: 35,
                isStateLevelArea: false,
                isActive: true
            ),
            MetroArea.Create(
                id: Guid.Parse("42111111-1111-1111-1111-111111111002"),
                name: "Pittsburgh",
                state: "PA",
                centerLatitude: 40.4406,
                centerLongitude: -79.9959,
                radiusMiles: 30,
                isStateLevelArea: false,
                isActive: true
            ),

            // =====================
            // RHODE ISLAND
            // =====================
            MetroArea.Create(
                id: Guid.Parse("44000000-0000-0000-0000-000000000001"),
                name: "All Rhode Island",
                state: "RI",
                centerLatitude: 41.6809,
                centerLongitude: -71.5118,
                radiusMiles: 120,
                isStateLevelArea: true,
                isActive: true
            ),
            MetroArea.Create(
                id: Guid.Parse("44111111-1111-1111-1111-111111111001"),
                name: "Providence",
                state: "RI",
                centerLatitude: 41.8240,
                centerLongitude: -71.4128,
                radiusMiles: 25,
                isStateLevelArea: false,
                isActive: true
            ),

            // =====================
            // SOUTH CAROLINA
            // =====================
            MetroArea.Create(
                id: Guid.Parse("45000000-0000-0000-0000-000000000001"),
                name: "All South Carolina",
                state: "SC",
                centerLatitude: 33.8361,
                centerLongitude: -80.9066,
                radiusMiles: 200,
                isStateLevelArea: true,
                isActive: true
            ),
            MetroArea.Create(
                id: Guid.Parse("45111111-1111-1111-1111-111111111001"),
                name: "Charleston",
                state: "SC",
                centerLatitude: 32.7765,
                centerLongitude: -79.9711,
                radiusMiles: 25,
                isStateLevelArea: false,
                isActive: true
            ),

            // =====================
            // SOUTH DAKOTA
            // =====================
            MetroArea.Create(
                id: Guid.Parse("46000000-0000-0000-0000-000000000001"),
                name: "All South Dakota",
                state: "SD",
                centerLatitude: 44.2998,
                centerLongitude: -99.4388,
                radiusMiles: 250,
                isStateLevelArea: true,
                isActive: true
            ),

            // =====================
            // TENNESSEE
            // =====================
            MetroArea.Create(
                id: Guid.Parse("47000000-0000-0000-0000-000000000001"),
                name: "All Tennessee",
                state: "TN",
                centerLatitude: 35.7478,
                centerLongitude: -86.6923,
                radiusMiles: 200,
                isStateLevelArea: true,
                isActive: true
            ),
            MetroArea.Create(
                id: Guid.Parse("47111111-1111-1111-1111-111111111001"),
                name: "Nashville",
                state: "TN",
                centerLatitude: 36.1627,
                centerLongitude: -86.7816,
                radiusMiles: 30,
                isStateLevelArea: false,
                isActive: true
            ),
            MetroArea.Create(
                id: Guid.Parse("47111111-1111-1111-1111-111111111002"),
                name: "Memphis",
                state: "TN",
                centerLatitude: 35.1495,
                centerLongitude: -90.0490,
                radiusMiles: 30,
                isStateLevelArea: false,
                isActive: true
            ),

            // =====================
            // TEXAS
            // =====================
            MetroArea.Create(
                id: Guid.Parse("48000000-0000-0000-0000-000000000001"),
                name: "All Texas",
                state: "TX",
                centerLatitude: 31.9686,
                centerLongitude: -99.9018,
                radiusMiles: 300,
                isStateLevelArea: true,
                isActive: true
            ),
            MetroArea.Create(
                id: Guid.Parse("48111111-1111-1111-1111-111111111001"),
                name: "Houston",
                state: "TX",
                centerLatitude: 29.7604,
                centerLongitude: -95.3698,
                radiusMiles: 40,
                isStateLevelArea: false,
                isActive: true
            ),
            MetroArea.Create(
                id: Guid.Parse("48111111-1111-1111-1111-111111111002"),
                name: "Dallas-Fort Worth",
                state: "TX",
                centerLatitude: 32.7767,
                centerLongitude: -96.7970,
                radiusMiles: 40,
                isStateLevelArea: false,
                isActive: true
            ),
            MetroArea.Create(
                id: Guid.Parse("48111111-1111-1111-1111-111111111003"),
                name: "Austin",
                state: "TX",
                centerLatitude: 30.2672,
                centerLongitude: -97.7431,
                radiusMiles: 30,
                isStateLevelArea: false,
                isActive: true
            ),
            MetroArea.Create(
                id: Guid.Parse("48111111-1111-1111-1111-111111111004"),
                name: "San Antonio",
                state: "TX",
                centerLatitude: 29.4241,
                centerLongitude: -98.4936,
                radiusMiles: 30,
                isStateLevelArea: false,
                isActive: true
            ),

            // =====================
            // UTAH
            // =====================
            MetroArea.Create(
                id: Guid.Parse("49000000-0000-0000-0000-000000000001"),
                name: "All Utah",
                state: "UT",
                centerLatitude: 39.3210,
                centerLongitude: -111.0937,
                radiusMiles: 200,
                isStateLevelArea: true,
                isActive: true
            ),
            MetroArea.Create(
                id: Guid.Parse("49111111-1111-1111-1111-111111111001"),
                name: "Salt Lake City",
                state: "UT",
                centerLatitude: 40.7608,
                centerLongitude: -111.8910,
                radiusMiles: 30,
                isStateLevelArea: false,
                isActive: true
            ),

            // =====================
            // VERMONT
            // =====================
            MetroArea.Create(
                id: Guid.Parse("50000000-0000-0000-0000-000000000001"),
                name: "All Vermont",
                state: "VT",
                centerLatitude: 44.0459,
                centerLongitude: -72.7107,
                radiusMiles: 150,
                isStateLevelArea: true,
                isActive: true
            ),

            // =====================
            // VIRGINIA
            // =====================
            MetroArea.Create(
                id: Guid.Parse("51000000-0000-0000-0000-000000000001"),
                name: "All Virginia",
                state: "VA",
                centerLatitude: 37.7693,
                centerLongitude: -78.1694,
                radiusMiles: 200,
                isStateLevelArea: true,
                isActive: true
            ),
            MetroArea.Create(
                id: Guid.Parse("51111111-1111-1111-1111-111111111001"),
                name: "Richmond",
                state: "VA",
                centerLatitude: 37.5407,
                centerLongitude: -77.4360,
                radiusMiles: 30,
                isStateLevelArea: false,
                isActive: true
            ),

            // =====================
            // WASHINGTON
            // =====================
            MetroArea.Create(
                id: Guid.Parse("53000000-0000-0000-0000-000000000001"),
                name: "All Washington",
                state: "WA",
                centerLatitude: 47.7511,
                centerLongitude: -120.7401,
                radiusMiles: 250,
                isStateLevelArea: true,
                isActive: true
            ),
            MetroArea.Create(
                id: Guid.Parse("53111111-1111-1111-1111-111111111001"),
                name: "Seattle",
                state: "WA",
                centerLatitude: 47.6062,
                centerLongitude: -122.3321,
                radiusMiles: 35,
                isStateLevelArea: false,
                isActive: true
            ),

            // =====================
            // WEST VIRGINIA
            // =====================
            MetroArea.Create(
                id: Guid.Parse("54000000-0000-0000-0000-000000000001"),
                name: "All West Virginia",
                state: "WV",
                centerLatitude: 38.5976,
                centerLongitude: -80.4549,
                radiusMiles: 200,
                isStateLevelArea: true,
                isActive: true
            ),

            // =====================
            // WISCONSIN
            // =====================
            MetroArea.Create(
                id: Guid.Parse("55000000-0000-0000-0000-000000000001"),
                name: "All Wisconsin",
                state: "WI",
                centerLatitude: 44.2685,
                centerLongitude: -89.6165,
                radiusMiles: 200,
                isStateLevelArea: true,
                isActive: true
            ),
            MetroArea.Create(
                id: Guid.Parse("55111111-1111-1111-1111-111111111001"),
                name: "Milwaukee",
                state: "WI",
                centerLatitude: 43.0389,
                centerLongitude: -87.9065,
                radiusMiles: 30,
                isStateLevelArea: false,
                isActive: true
            ),

            // =====================
            // WYOMING
            // =====================
            MetroArea.Create(
                id: Guid.Parse("56000000-0000-0000-0000-000000000001"),
                name: "All Wyoming",
                state: "WY",
                centerLatitude: 42.7559,
                centerLongitude: -107.3025,
                radiusMiles: 250,
                isStateLevelArea: true,
                isActive: true
            ),
        };

        await context.MetroAreas.AddRangeAsync(metroAreas);
        await context.SaveChangesAsync();
    }
}
