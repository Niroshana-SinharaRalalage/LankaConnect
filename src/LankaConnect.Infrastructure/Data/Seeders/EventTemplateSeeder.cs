using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.Enums;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace LankaConnect.Infrastructure.Data.Seeders;

/// <summary>
/// Phase 6A.8: Event Template System
/// Seeds pre-designed event templates to help organizers quickly create events
/// Includes 12 templates across 8 categories with Sri Lankan cultural focus
/// </summary>
public static class EventTemplateSeeder
{
    /// <summary>
    /// Seed event templates data into database
    /// </summary>
    public static async Task SeedAsync(AppDbContext context)
    {
        // Check if any templates already exist
        if (await context.EventTemplates.AnyAsync())
        {
            return; // Already seeded
        }

        var templates = new List<EventTemplate>
        {
            // RELIGIOUS TEMPLATES
            CreateTemplate(
                id: Guid.Parse("00000001-0000-0000-0000-000000000001"),
                name: "Vesak Day Celebration",
                description: "Traditional Vesak celebration with lanterns, Dansalas, and cultural activities",
                category: EventCategory.Religious,
                displayOrder: 1,
                templateData: new
                {
                    title = "Vesak Day Celebration 2024",
                    description = "Join us for a traditional Vesak celebration featuring beautiful lanterns, free Dansala service, Bodhi Pooja, and cultural performances. Experience the spirit of giving and compassion during this sacred Buddhist festival.",
                    capacity = 150,
                    durationHours = 6,
                    suggestedStartTime = "10:00 AM",
                    ticketPrice = 0
                },
                thumbnailSvg: GetSvgIcon("lotus", "#FF9933")
            ),

            CreateTemplate(
                id: Guid.Parse("00000001-0000-0000-0000-000000000002"),
                name: "Poson Poya Program",
                description: "Buddhist meditation and Dhamma discussion program for Poson Poya",
                category: EventCategory.Religious,
                displayOrder: 2,
                templateData: new
                {
                    title = "Poson Poya Meditation Retreat",
                    description = "Join our Poson Poya meditation retreat featuring guided meditation sessions, Dhamma discussions, and teachings from visiting monks. Cultivate mindfulness and deepen your spiritual practice.",
                    capacity = 80,
                    durationHours = 4,
                    suggestedStartTime = "8:00 AM",
                    ticketPrice = 0
                },
                thumbnailSvg: GetSvgIcon("meditation", "#8B4513")
            ),

            // CULTURAL TEMPLATES
            CreateTemplate(
                id: Guid.Parse("00000002-0000-0000-0000-000000000001"),
                name: "Traditional Dance Performance",
                description: "Showcase of Sri Lankan traditional dance forms including Kandyan, Low Country, and Sabaragamuwa",
                category: EventCategory.Cultural,
                displayOrder: 3,
                templateData: new
                {
                    title = "Sri Lankan Traditional Dance Showcase",
                    description = "Experience the vibrant traditional dance forms of Sri Lanka including Kandyan dance, Low Country dance, and Sabaragamuwa dance. Performed by talented local artists with authentic costumes and live drumming.",
                    capacity = 200,
                    durationHours = 3,
                    suggestedStartTime = "6:00 PM",
                    ticketPrice = 15
                },
                thumbnailSvg: GetSvgIcon("dancers", "#DC143C")
            ),

            CreateTemplate(
                id: Guid.Parse("00000002-0000-0000-0000-000000000002"),
                name: "Sinhala & Tamil New Year Festival",
                description: "Traditional New Year celebration with games, rituals, and festive food",
                category: EventCategory.Cultural,
                displayOrder: 4,
                templateData: new
                {
                    title = "Sinhala & Tamil New Year Celebration",
                    description = "Celebrate Aluth Avurudu and Puthandu with traditional games (pillow fight, pot breaking, tug-of-war), ritual activities (lighting the hearth, eating milk rice), and authentic Sri Lankan cuisine. Fun for the whole family!",
                    capacity = 250,
                    durationHours = 8,
                    suggestedStartTime = "10:00 AM",
                    ticketPrice = 10
                },
                thumbnailSvg: GetSvgIcon("celebration", "#FFD700")
            ),

            // COMMUNITY TEMPLATES
            CreateTemplate(
                id: Guid.Parse("00000003-0000-0000-0000-000000000001"),
                name: "Cricket Match Viewing Party",
                description: "Community cricket viewing with food, drinks, and friendly banter",
                category: EventCategory.Community,
                displayOrder: 5,
                templateData: new
                {
                    title = "Sri Lanka Cricket Match Viewing Party",
                    description = "Join fellow cricket fans to watch Sri Lanka play! Enjoy the match on a big screen with Sri Lankan snacks, soft drinks, and lively conversation. Wear your team colors and bring your enthusiasm!",
                    capacity = 100,
                    durationHours = 5,
                    suggestedStartTime = "2:00 PM",
                    ticketPrice = 5
                },
                thumbnailSvg: GetSvgIcon("cricket", "#003DA5")
            ),

            CreateTemplate(
                id: Guid.Parse("00000003-0000-0000-0000-000000000002"),
                name: "Community Picnic & Sports Day",
                description: "Family-friendly outdoor picnic with traditional games and sports",
                category: EventCategory.Community,
                displayOrder: 6,
                templateData: new
                {
                    title = "Sri Lankan Community Picnic & Sports Day",
                    description = "Bring your family for a fun-filled day of picnic, traditional games, cricket matches, and outdoor activities. Pack your favorite Sri Lankan food to share, and enjoy quality time with the community in a beautiful park setting.",
                    capacity = 150,
                    durationHours = 6,
                    suggestedStartTime = "11:00 AM",
                    ticketPrice = 0
                },
                thumbnailSvg: GetSvgIcon("picnic", "#228B22")
            ),

            // EDUCATIONAL TEMPLATES
            CreateTemplate(
                id: Guid.Parse("00000004-0000-0000-0000-000000000001"),
                name: "Sinhala Language Class",
                description: "Learn Sinhala language basics - reading, writing, and conversation",
                category: EventCategory.Educational,
                displayOrder: 7,
                templateData: new
                {
                    title = "Beginner Sinhala Language Workshop",
                    description = "Learn Sinhala alphabet, basic vocabulary, common phrases, and conversational skills. Perfect for beginners and second-generation Sri Lankan Americans looking to connect with their heritage. Materials provided.",
                    capacity = 30,
                    durationHours = 2,
                    suggestedStartTime = "10:00 AM",
                    ticketPrice = 20
                },
                thumbnailSvg: GetSvgIcon("book", "#4B0082")
            ),

            CreateTemplate(
                id: Guid.Parse("00000004-0000-0000-0000-000000000002"),
                name: "Sri Lankan Cooking Workshop",
                description: "Hands-on cooking class featuring traditional Sri Lankan recipes",
                category: EventCategory.Educational,
                displayOrder: 8,
                templateData: new
                {
                    title = "Traditional Sri Lankan Cooking Class",
                    description = "Learn to cook authentic Sri Lankan dishes including rice & curry, hoppers, kottu roti, and traditional sweets. Hands-on instruction, recipe booklet included, and enjoy the meal you prepare together!",
                    capacity = 25,
                    durationHours = 4,
                    suggestedStartTime = "2:00 PM",
                    ticketPrice = 35
                },
                thumbnailSvg: GetSvgIcon("chef", "#FF6347")
            ),

            // SOCIAL TEMPLATES
            CreateTemplate(
                id: Guid.Parse("00000005-0000-0000-0000-000000000001"),
                name: "Young Professionals Networking Mixer",
                description: "Casual networking event for Sri Lankan young professionals",
                category: EventCategory.Social,
                displayOrder: 9,
                templateData: new
                {
                    title = "Sri Lankan Young Professionals Mixer",
                    description = "Network with other Sri Lankan young professionals in a casual setting. Share experiences, build connections, discuss career opportunities, and enjoy appetizers and drinks. Business casual attire.",
                    capacity = 75,
                    durationHours = 3,
                    suggestedStartTime = "6:30 PM",
                    ticketPrice = 15
                },
                thumbnailSvg: GetSvgIcon("networking", "#20B2AA")
            ),

            // BUSINESS TEMPLATES
            CreateTemplate(
                id: Guid.Parse("00000006-0000-0000-0000-000000000001"),
                name: "Entrepreneurship Seminar",
                description: "Business development seminar for Sri Lankan entrepreneurs",
                category: EventCategory.Business,
                displayOrder: 10,
                templateData: new
                {
                    title = "Sri Lankan American Entrepreneurs Forum",
                    description = "Learn from successful Sri Lankan American entrepreneurs about starting and growing businesses in the US. Topics include funding, market entry strategies, networking, and overcoming cultural challenges.",
                    capacity = 100,
                    durationHours = 4,
                    suggestedStartTime = "9:00 AM",
                    ticketPrice = 25
                },
                thumbnailSvg: GetSvgIcon("business", "#2E8B57")
            ),

            // CHARITY TEMPLATES
            CreateTemplate(
                id: Guid.Parse("00000007-0000-0000-0000-000000000001"),
                name: "Fundraiser Dinner & Auction",
                description: "Charity fundraising dinner with silent auction and entertainment",
                category: EventCategory.Charity,
                displayOrder: 11,
                templateData: new
                {
                    title = "Sri Lankan Charity Fundraiser Gala",
                    description = "Join us for an elegant dinner to support [Cause Name]. Features authentic Sri Lankan cuisine, silent auction, live music, and inspiring stories. All proceeds go directly to supporting communities in need.",
                    capacity = 150,
                    durationHours = 4,
                    suggestedStartTime = "6:00 PM",
                    ticketPrice = 75
                },
                thumbnailSvg: GetSvgIcon("heart", "#E91E63")
            ),

            // ENTERTAINMENT TEMPLATES
            CreateTemplate(
                id: Guid.Parse("00000008-0000-0000-0000-000000000001"),
                name: "Sri Lankan Music Concert",
                description: "Live music performance featuring popular Sri Lankan artists",
                category: EventCategory.Entertainment,
                displayOrder: 12,
                templateData: new
                {
                    title = "Live Sri Lankan Music Night",
                    description = "Enjoy an evening of live music featuring popular Sinhala and Tamil songs, both classic and contemporary. Talented local and visiting artists will perform your favorite hits. Dancing encouraged!",
                    capacity = 300,
                    durationHours = 4,
                    suggestedStartTime = "7:00 PM",
                    ticketPrice = 30
                },
                thumbnailSvg: GetSvgIcon("music", "#9C27B0")
            )
        };

        await context.EventTemplates.AddRangeAsync(templates);
        await context.SaveChangesAsync();
    }

    private static EventTemplate CreateTemplate(
        Guid id,
        string name,
        string description,
        EventCategory category,
        int displayOrder,
        object templateData,
        string thumbnailSvg)
    {
        var templateDataJson = JsonSerializer.Serialize(templateData);
        var result = EventTemplate.Create(name, description, category, thumbnailSvg, templateDataJson, displayOrder);

        if (result.IsFailure)
            throw new InvalidOperationException($"Failed to create template: {result.Errors.FirstOrDefault()}");

        // Set the specific ID for seeding
        var template = result.Value;
        var idProperty = typeof(EventTemplate).BaseType!.GetProperty("Id");
        idProperty!.SetValue(template, id);

        return template;
    }

    /// <summary>
    /// Generates simple SVG icons for template thumbnails
    /// Each icon is 200x200px with rounded corners and centered symbol
    /// </summary>
    private static string GetSvgIcon(string iconType, string color)
    {
        var background = $"<rect width=\"200\" height=\"200\" rx=\"12\" fill=\"{color}\" opacity=\"0.15\"/>";
        var symbol = iconType switch
        {
            "lotus" => $"<circle cx=\"100\" cy=\"100\" r=\"30\" fill=\"{color}\"/><circle cx=\"85\" cy=\"90\" r=\"15\" fill=\"{color}\" opacity=\"0.7\"/><circle cx=\"115\" cy=\"90\" r=\"15\" fill=\"{color}\" opacity=\"0.7\"/>",
            "meditation" => $"<circle cx=\"100\" cy=\"80\" r=\"20\" fill=\"{color}\"/><path d=\"M 80,100 Q 100,120 120,100\" stroke=\"{color}\" stroke-width=\"4\" fill=\"none\"/>",
            "dancers" => $"<circle cx=\"90\" cy=\"70\" r=\"12\" fill=\"{color}\"/><circle cx=\"110\" cy=\"70\" r=\"12\" fill=\"{color}\"/><path d=\"M 90,82 L 85,120 M 90,82 L 95,120 M 110,82 L 105,120 M 110,82 L 115,120\" stroke=\"{color}\" stroke-width=\"3\"/>",
            "celebration" => $"<path d=\"M 100,50 L 110,80 L 140,85 L 115,105 L 120,135 L 100,120 L 80,135 L 85,105 L 60,85 L 90,80 Z\" fill=\"{color}\"/>",
            "cricket" => $"<rect x=\"95\" y=\"60\" width=\"10\" height=\"80\" fill=\"{color}\"/><circle cx=\"100\" cy=\"50\" r=\"12\" fill=\"{color}\"/>",
            "picnic" => $"<path d=\"M 70,90 L 100,60 L 130,90 Z\" fill=\"{color}\"/><rect x=\"70\" y=\"90\" width=\"60\" height=\"40\" fill=\"{color}\" opacity=\"0.7\"/>",
            "book" => $"<rect x=\"70\" y=\"60\" width=\"60\" height=\"80\" rx=\"4\" fill=\"{color}\"/><line x1=\"100\" y1=\"60\" x2=\"100\" y2=\"140\" stroke=\"white\" stroke-width=\"2\"/>",
            "chef" => $"<circle cx=\"100\" cy=\"80\" r=\"25\" fill=\"{color}\"/><rect x=\"85\" y=\"105\" width=\"30\" height=\"35\" fill=\"{color}\"/>",
            "networking" => $"<circle cx=\"70\" cy=\"80\" r=\"15\" fill=\"{color}\"/><circle cx=\"130\" cy=\"80\" r=\"15\" fill=\"{color}\"/><circle cx=\"100\" cy=\"120\" r=\"15\" fill=\"{color}\"/><line x1=\"70\" y1=\"80\" x2=\"130\" y2=\"80\" stroke=\"{color}\" stroke-width=\"3\"/><line x1=\"70\" y1=\"80\" x2=\"100\" y2=\"120\" stroke=\"{color}\" stroke-width=\"3\"/><line x1=\"130\" y1=\"80\" x2=\"100\" y2=\"120\" stroke=\"{color}\" stroke-width=\"3\"/>",
            "business" => $"<rect x=\"70\" y=\"60\" width=\"60\" height=\"70\" fill=\"{color}\"/><rect x=\"80\" y=\"70\" width=\"15\" height=\"15\" fill=\"white\"/><rect x=\"105\" y=\"70\" width=\"15\" height=\"15\" fill=\"white\"/><rect x=\"80\" y=\"95\" width=\"15\" height=\"15\" fill=\"white\"/><rect x=\"105\" y=\"95\" width=\"15\" height=\"15\" fill=\"white\"/>",
            "heart" => $"<path d=\"M 100,130 C 80,110 60,90 60,75 C 60,55 75,50 85,50 C 92,50 97,53 100,58 C 103,53 108,50 115,50 C 125,50 140,55 140,75 C 140,90 120,110 100,130 Z\" fill=\"{color}\"/>",
            "music" => $"<circle cx=\"85\" cy=\"110\" r=\"15\" fill=\"{color}\"/><circle cx=\"115\" cy=\"100\" r=\"15\" fill=\"{color}\"/><rect x=\"100\" y=\"60\" width=\"4\" height=\"50\" fill=\"{color}\"/><rect x=\"130\" y=\"50\" width=\"4\" height=\"50\" fill=\"{color}\"/>",
            _ => $"<circle cx=\"100\" cy=\"100\" r=\"40\" fill=\"{color}\"/>"
        };

        return $"<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"200\" height=\"200\" viewBox=\"0 0 200 200\">{background}{symbol}</svg>";
    }
}
