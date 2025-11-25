using LankaConnect.Domain.Events;
using Microsoft.EntityFrameworkCore;

namespace LankaConnect.Infrastructure.Data.Seeders;

/// <summary>
/// Seeds comprehensive metro area data for the LankaConnect platform
/// Phase 5B: User Preferred Metro Areas - State-grouped selector
///
/// Data Structure:
/// - 74 major US metro areas grouped by state
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

            // =====================
            // OHIO
            // =====================
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
            MetroArea.Create(
                id: Guid.Parse("39111111-1111-1111-1111-111111111005"),
                name: "Akron",
                state: "OH",
                centerLatitude: 41.0823,
                centerLongitude: -81.5178,
                radiusMiles: 25,
                isStateLevelArea: false,
                isActive: true
            ),

            // =====================
            // OKLAHOMA
            // =====================
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

            // =====================
            // TENNESSEE
            // =====================
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

            // =====================
            // VIRGINIA
            // =====================
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

            // =====================
            // WISCONSIN
            // =====================
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
        };

        await context.MetroAreas.AddRangeAsync(metroAreas);
        await context.SaveChangesAsync();
    }
}
