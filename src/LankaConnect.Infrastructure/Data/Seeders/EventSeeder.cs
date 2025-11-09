using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Domain.Events.ValueObjects;
using LankaConnect.Domain.Business.ValueObjects;
using LankaConnect.Domain.Shared.ValueObjects;
using LankaConnect.Domain.Shared.Enums;

namespace LankaConnect.Infrastructure.Data.Seeders;

/// <summary>
/// Seeds the Events table with diverse Sri Lankan community events across Ohio metro areas
/// Following TDD principles with realistic event data
/// </summary>
public static class EventSeeder
{
    private static readonly Guid TestOrganizerId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public static List<Event> GetSeedEvents()
    {
        var events = new List<Event>();
        var now = DateTime.UtcNow;

        // 1. Sinhala & Tamil New Year Celebration - Cleveland
        var newYearEvent = CreateEvent(
            "Sinhala & Tamil New Year Grand Celebration 2025",
            "Join us for the biggest Sinhala and Tamil New Year celebration in Cleveland! Traditional games (කීල), cultural performances, authentic Sri Lankan cuisine including Kiribath and Kavum. Special performances by visiting artists from Sri Lanka. Fun activities for the whole family!",
            now.AddDays(45),
            now.AddDays(45).AddHours(6),
            "Cleveland Public Auditorium",
            "500 E Lakeside Ave",
            "Cleveland",
            "44114",
            41.5051m,
            -81.6934m,
            EventCategory.Cultural,
            500,
            null, // Free event
            EventStatus.Published
        );
        if (newYearEvent != null) events.Add(newYearEvent);

        // 2. Vesak Day Celebration - Columbus
        var vesakEvent = CreateEvent(
            "Vesak Full Moon Poya Day Celebration",
            "Celebrate Vesak, the most sacred day for Buddhists, commemorating the birth, enlightenment, and passing of Lord Buddha. Includes Bodhi Pooja, Pirith chanting, meditation sessions, and traditional alms giving (දාන්). Beautiful lantern displays and cultural programs. All are welcome.",
            now.AddDays(60),
            now.AddDays(60).AddHours(8),
            "Ohio Buddhist Vihara",
            "4536 Indianola Ave",
            "Columbus",
            "43214",
            40.0428m,
            -83.0103m,
            EventCategory.Religious,
            200,
            null, // Free event
            EventStatus.Published
        );
        if (vesakEvent != null) events.Add(vesakEvent);

        // 3. Cricket Match - Cincinnati
        var cricketEvent = CreateEvent(
            "Ohio Premier League Cricket Tournament 2025",
            "Annual Sri Lankan cricket tournament featuring teams from across Ohio. Watch exciting T20 format matches, enjoy authentic Sri Lankan street food, and cheer for your favorite team! Player registrations open. Special prizes for Man of the Match and winning team.",
            now.AddDays(30),
            now.AddDays(31),
            "Embshoff Woods & Nature Preserve",
            "10616 Loveland Madeira Rd",
            "Loveland",
            "45140",
            39.2645m,
            -84.2633m,
            EventCategory.Community,
            300,
            CreateMoney(15, Currency.USD),
            EventStatus.Published
        );
        if (cricketEvent != null) events.Add(cricketEvent);

        // 4. Food Festival - Akron
        var foodFestivalEvent = CreateEvent(
            "Taste of Sri Lanka Food Festival",
            "Experience the rich flavors of Sri Lankan cuisine! Over 20 food vendors offering authentic dishes: Kottu Roti, String Hoppers, Lamprais, Deviled dishes, and traditional sweets. Live cooking demonstrations, spice market, and traditional coffee (කොප්පි කඩේ). Family-friendly event.",
            now.AddDays(21),
            now.AddDays(21).AddHours(5),
            "Akron Civic Commons",
            "140 E Market St",
            "Akron",
            "44308",
            41.0823m,
            -81.5178m,
            EventCategory.Cultural,
            450,
            CreateMoney(10, Currency.USD),
            EventStatus.Published
        );
        if (foodFestivalEvent != null) events.Add(foodFestivalEvent);

        // 5. Independence Day Celebration - Cleveland
        var independenceEvent = CreateEvent(
            "Sri Lankan Independence Day Celebration",
            "Celebrate 77 years of Sri Lankan independence with flag hoisting ceremony, national anthem, patriotic songs, and cultural dance performances. Traditional Sri Lankan breakfast (කිරිබත්, ලුණු මිරිස්, කට්ලට්) will be served. Wear national colors!",
            now.AddDays(90),
            now.AddDays(90).AddHours(4),
            "Sri Lanka Community Center",
            "6935 Pearl Rd",
            "Middleburg Heights",
            "44130",
            41.3670m,
            -81.8129m,
            EventCategory.Cultural,
            250,
            null, // Free event
            EventStatus.Published
        );
        if (independenceEvent != null) events.Add(independenceEvent);

        // 6. Poson Festival - Dublin
        var posonEvent = CreateEvent(
            "Poson Full Moon Poya Celebration",
            "Commemorate the arrival of Buddhism to Sri Lanka. Religious observances, Dhamma discussions, meditation sessions, and children's programs teaching Buddhist values. Vegetarian meals (දාන්) provided. Guest speaker: Venerable monk from Sri Lanka.",
            now.AddDays(75),
            now.AddDays(75).AddHours(6),
            "Dublin Community Recreation Center",
            "5600 Post Rd",
            "Dublin",
            "43016",
            40.0992m,
            -83.1141m,
            EventCategory.Religious,
            180,
            null, // Free event
            EventStatus.Published
        );
        if (posonEvent != null) events.Add(posonEvent);

        // 7. Youth Cultural Workshop - Aurora
        var youthWorkshopEvent = CreateEvent(
            "Sri Lankan Youth Cultural Workshop Series",
            "Interactive workshops for youth aged 10-18 to learn about Sri Lankan heritage. Sessions on Kandyan dancing, traditional drumming, Sinhala language basics, and history. Expert instructors from Sri Lanka. Certificate of completion provided.",
            now.AddDays(14),
            now.AddDays(16),
            "Aurora Community Center",
            "1 Library St",
            "Aurora",
            "44202",
            41.3175m,
            -81.3451m,
            EventCategory.Educational,
            80,
            CreateMoney(35, Currency.USD),
            EventStatus.Published
        );
        if (youthWorkshopEvent != null) events.Add(youthWorkshopEvent);

        // 8. Charity Fundraiser - Cleveland
        var charityEvent = CreateEvent(
            "Charity Dinner for Sri Lanka Flood Relief",
            "Black-tie charity dinner to raise funds for flood victims in Sri Lanka. Three-course authentic Sri Lankan dinner, live auction, cultural performances, and guest speakers. All proceeds go directly to relief efforts. Donation receipts provided.",
            now.AddDays(40),
            now.AddDays(40).AddHours(4),
            "The Ritz-Carlton Cleveland",
            "1515 W 3rd St",
            "Cleveland",
            "44113",
            41.4868m,
            -81.7009m,
            EventCategory.Charity,
            120,
            CreateMoney(75, Currency.USD),
            EventStatus.Published
        );
        if (charityEvent != null) events.Add(charityEvent);

        // 9. Business Networking - Westlake
        var businessEvent = CreateEvent(
            "Sri Lankan Professionals Network Mixer",
            "Monthly networking event for Sri Lankan professionals in Greater Cleveland. Connect with fellow professionals, discuss business opportunities, share experiences, and build your network. Light refreshments and beverages provided. Dress business casual.",
            now.AddDays(10),
            now.AddDays(10).AddHours(3),
            "Westlake Recreation Center",
            "28955 Hilliard Blvd",
            "Westlake",
            "44145",
            41.4556m,
            -81.9179m,
            EventCategory.Business,
            60,
            CreateMoney(20, Currency.USD),
            EventStatus.Published
        );
        if (businessEvent != null) events.Add(businessEvent);

        // 10. Esala Perahera Cultural Show - Columbus
        var peraheraEvent = CreateEvent(
            "Esala Perahera Cultural Night",
            "Experience the grandeur of Sri Lanka's Esala Perahera! Traditional Kandyan dance performances, drumming ensembles, fire dancers, and elaborate costumes. A spectacular cultural showcase bringing the pageantry of Kandy to Ohio.",
            now.AddDays(55),
            now.AddDays(55).AddHours(3),
            "Southern Theatre",
            "21 E Main St",
            "Columbus",
            "43215",
            39.9576m,
            -82.9988m,
            EventCategory.Cultural,
            400,
            CreateMoney(25, Currency.USD),
            EventStatus.Published
        );
        if (peraheraEvent != null) events.Add(peraheraEvent);

        // 11. Sinhala Language Classes - Cincinnati
        var languageEvent = CreateEvent(
            "Beginner Sinhala Language Course - 8 Week Program",
            "Learn to speak, read, and write Sinhala! Eight-week comprehensive course covering basics: alphabet (ඇල්ෆාබෙට්), common phrases, grammar fundamentals, and conversational skills. Workbooks included. Perfect for adults and teens.",
            now.AddDays(20),
            now.AddDays(76).AddHours(2), // 8 weeks from start
            "Cincinnati Public Library",
            "800 Vine St",
            "Cincinnati",
            "45202",
            39.1090m,
            -84.5124m,
            EventCategory.Educational,
            30,
            CreateMoney(120, Currency.USD),
            EventStatus.Published
        );
        if (languageEvent != null) events.Add(languageEvent);

        // 12. Community Picnic - Cleveland
        var picnicEvent = CreateEvent(
            "Annual Sri Lankan Summer Picnic",
            "Family-friendly picnic with outdoor games, cricket, volleyball, and traditional Sri Lankan games for kids. Potluck style - bring your favorite Sri Lankan dish to share! Soft drinks and desserts provided. Great opportunity to meet other Lankan families.",
            now.AddDays(35),
            now.AddDays(35).AddHours(5),
            "Edgewater Park",
            "6500 Cleveland Memorial Shoreway",
            "Cleveland",
            "44102",
            41.4841m,
            -81.7548m,
            EventCategory.Social,
            200,
            null, // Free event
            EventStatus.Published
        );
        if (picnicEvent != null) events.Add(picnicEvent);

        // 13. Medical Seminar - Cleveland
        var medicalEvent = CreateEvent(
            "Health & Wellness Seminar for Sri Lankan Community",
            "Free health screening and educational seminar by Sri Lankan-American healthcare professionals. Topics: diabetes prevention, heart health, mental health awareness. Free BP and glucose checks. Q&A session. Refreshments provided.",
            now.AddDays(25),
            now.AddDays(25).AddHours(3),
            "Cleveland Clinic Community Center",
            "9500 Euclid Ave",
            "Cleveland",
            "44195",
            41.5042m,
            -81.6211m,
            EventCategory.Educational,
            100,
            null, // Free event
            EventStatus.Published
        );
        if (medicalEvent != null) events.Add(medicalEvent);

        // 14. Cooking Class - Columbus
        var cookingEvent = CreateEvent(
            "Learn to Cook Authentic Sri Lankan Curry",
            "Hands-on cooking class with Chef Sunil from Colombo. Learn to make: chicken curry, dhal curry, coconut sambol, and pol roti. All ingredients and recipes provided. Take home the dishes you prepare! Limited spots available.",
            now.AddDays(18),
            now.AddDays(18).AddHours(4),
            "Columbus Cooking School",
            "1230 W 3rd Ave",
            "Columbus",
            "43212",
            39.9939m,
            -83.0303m,
            EventCategory.Cultural,
            25,
            CreateMoney(55, Currency.USD),
            EventStatus.Published
        );
        if (cookingEvent != null) events.Add(cookingEvent);

        // 15. Movie Night - Akron
        var movieEvent = CreateEvent(
            "Sri Lankan Cinema Night - Classic Film Screening",
            "Screening of classic Sinhala film 'Rekava' (1956) with English subtitles, followed by discussion. Explore the golden age of Sri Lankan cinema. Popcorn and traditional snacks (කැවුම්) provided. Film enthusiasts welcome!",
            now.AddDays(28),
            now.AddDays(28).AddHours(3),
            "Akron Public Library Main Branch",
            "60 S High St",
            "Akron",
            "44326",
            41.0847m,
            -81.5166m,
            EventCategory.Entertainment,
            75,
            null, // Free event
            EventStatus.Published
        );
        if (movieEvent != null) events.Add(movieEvent);

        // 16. Children's Cultural Program - Dublin
        var kidsEvent = CreateEvent(
            "Sri Lankan Children's Cultural Day",
            "Fun-filled day for kids aged 5-12! Activities: traditional dance lessons, drum circle, mask painting, folk tale storytelling (ජාතක කථා), and Sri Lankan games. Lunch included (rice and curry kids' portions). Parent participation encouraged.",
            now.AddDays(42),
            now.AddDays(42).AddHours(5),
            "Dublin Recreation Center",
            "5600 Post Rd",
            "Dublin",
            "43016",
            40.0992m,
            -83.1141m,
            EventCategory.Cultural,
            100,
            CreateMoney(15, Currency.USD),
            EventStatus.Published
        );
        if (kidsEvent != null) events.Add(kidsEvent);

        // 17. Music Concert - Cleveland
        var concertEvent = CreateEvent(
            "Live Baila Music Night with DJ Sohan",
            "Dance the night away to authentic Baila music! DJ Sohan from Colombo will be spinning classic and modern Baila hits. Live performances by local Sri Lankan artists. Bar available (21+ for alcohol). Traditional and Western dress welcome.",
            now.AddDays(50),
            now.AddDays(50).AddHours(5),
            "The Agora Theatre",
            "5000 Euclid Ave",
            "Cleveland",
            "44103",
            41.4845m,
            -81.6366m,
            EventCategory.Entertainment,
            350,
            CreateMoney(30, Currency.USD),
            EventStatus.Published
        );
        if (concertEvent != null) events.Add(concertEvent);

        // 18. Yoga & Meditation - Westlake
        var yogaEvent = CreateEvent(
            "Sri Lankan Buddhist Meditation & Yoga Retreat",
            "Day-long retreat combining traditional Buddhist meditation (භාවනා) with yoga practice. Guided sessions by experienced instructors. Learn Vipassana and Metta meditation techniques. Vegetarian lunch provided. Bring yoga mat and cushion.",
            now.AddDays(32),
            now.AddDays(32).AddHours(7),
            "Westlake Community Center",
            "28955 Hilliard Blvd",
            "Westlake",
            "44145",
            41.4556m,
            -81.9179m,
            EventCategory.Religious,
            50,
            CreateMoney(25, Currency.USD),
            EventStatus.Published
        );
        if (yogaEvent != null) events.Add(yogaEvent);

        // 19. Art Exhibition - Columbus
        var artEvent = CreateEvent(
            "Contemporary Sri Lankan Art Exhibition",
            "Featuring works by Sri Lankan-American artists from Ohio and visiting artists from Colombo. Paintings, sculptures, and photography inspired by Sri Lankan landscapes, culture, and diaspora experiences. Meet the artists at opening reception.",
            now.AddDays(38),
            now.AddDays(45), // Week-long exhibition
            "Columbus Museum of Art",
            "480 E Broad St",
            "Columbus",
            "43215",
            39.9620m,
            -82.9919m,
            EventCategory.Cultural,
            150,
            CreateMoney(12, Currency.USD),
            EventStatus.Published
        );
        if (artEvent != null) events.Add(artEvent);

        // 20. Career Workshop - Cincinnati
        var careerEvent = CreateEvent(
            "Career Development Workshop for Sri Lankan Youth",
            "Professional development workshop for college students and recent graduates. Topics: resume building, interview skills, networking strategies, and navigating corporate America. Panel of successful Sri Lankan professionals. Mentorship opportunities.",
            now.AddDays(22),
            now.AddDays(22).AddHours(4),
            "University of Cincinnati",
            "2600 Clifton Ave",
            "Cincinnati",
            "45221",
            39.1329m,
            -84.5150m,
            EventCategory.Educational,
            80,
            null, // Free event
            EventStatus.Published
        );
        if (careerEvent != null) events.Add(careerEvent);

        // 21. Traditional Wedding Showcase - Cleveland
        var weddingEvent = CreateEvent(
            "Sri Lankan Wedding Traditions Showcase",
            "For couples planning traditional Sri Lankan weddings! Learn about Poruwa ceremony, traditional attire (සරම් and ඔසරිය), customs, and rituals. Vendor showcase: decorators, caterers, photographers, and traditional musicians. Couples get discount vouchers.",
            now.AddDays(15),
            now.AddDays(15).AddHours(4),
            "Cleveland Convention Center",
            "300 Lakeside Ave E",
            "Cleveland",
            "44114",
            41.5051m,
            -81.6890m,
            EventCategory.Social,
            120,
            CreateMoney(25, Currency.USD),
            EventStatus.Published
        );
        if (weddingEvent != null) events.Add(weddingEvent);

        // 22. Tech Meetup - Columbus
        var techEvent = CreateEvent(
            "Sri Lankan Tech Professionals Meetup",
            "Monthly meetup for Lankan software engineers, IT professionals, and tech entrepreneurs. Tech talks, project showcases, job opportunities discussion, and networking. This month's topic: AI/ML in modern applications. Pizza and drinks provided.",
            now.AddDays(12),
            now.AddDays(12).AddHours(3),
            "Ohio State University Campus",
            "281 W Lane Ave",
            "Columbus",
            "43210",
            40.0067m,
            -83.0302m,
            EventCategory.Business,
            70,
            null, // Free event
            EventStatus.Published
        );
        if (techEvent != null) events.Add(techEvent);

        // 23. Volleyball Tournament - Aurora
        var volleyballEvent = CreateEvent(
            "Sri Lankan Volleyball Championship 2025",
            "Annual volleyball tournament for Lankan community. Men's and women's divisions. Register your team (6 players minimum). Exciting matches, trophies for winners, and traditional Lankan refreshments. Great sporting event for players and spectators!",
            now.AddDays(48),
            now.AddDays(48).AddHours(8),
            "Aurora High School Gymnasium",
            "102 E Pioneer Trail",
            "Aurora",
            "44202",
            41.3175m,
            -81.3451m,
            EventCategory.Community,
            200,
            CreateMoney(20, Currency.USD),
            EventStatus.Published
        );
        if (volleyballEvent != null) events.Add(volleyballEvent);

        // 24. Kathina Ceremony - Columbus
        var kathinaEvent = CreateEvent(
            "Annual Kathina Ceremony & Dana",
            "Traditional Kathina ceremony offering robes to Buddhist monks. Merit-making ceremony (පින්), Pirith chanting, and Dhamma sermon by chief monk. Grand alms giving (දාන්) with authentic Sri Lankan meals. All are invited to participate and gain merit.",
            now.AddDays(65),
            now.AddDays(65).AddHours(6),
            "Ohio Buddhist Temple",
            "4536 Indianola Ave",
            "Columbus",
            "43214",
            40.0428m,
            -83.0103m,
            EventCategory.Religious,
            250,
            null, // Free event (dana)
            EventStatus.Published
        );
        if (kathinaEvent != null) events.Add(kathinaEvent);

        // 25. Past Event - Drama Performance (Completed)
        var dramaEvent = CreateEvent(
            "Sinhala Drama Performance: 'Maname'",
            "Award-winning Sinhala drama 'Maname' by talented local Sri Lankan theater group. Powerful story about family, identity, and Sri Lankan culture in diaspora. Professional production with authentic costumes and stage design. Two acts with intermission.",
            now.AddDays(-15), // Past event
            now.AddDays(-15).AddHours(3),
            "Beck Center for the Arts",
            "17801 Detroit Ave",
            "Lakewood",
            "44107",
            41.4822m,
            -81.7976m,
            EventCategory.Entertainment,
            180,
            CreateMoney(18, Currency.USD),
            EventStatus.Completed
        );
        if (dramaEvent != null) events.Add(dramaEvent);

        return events;
    }

    private static Event? CreateEvent(
        string title,
        string description,
        DateTime startDate,
        DateTime endDate,
        string venueName,
        string street,
        string city,
        string zipCode,
        decimal latitude,
        decimal longitude,
        EventCategory category,
        int capacity,
        Money? ticketPrice,
        EventStatus status)
    {
        // Create value objects
        var eventTitle = EventTitle.Create(title);
        if (eventTitle.IsFailure)
        {
            Console.WriteLine($"Failed to create title: {eventTitle.Errors}");
            return null;
        }

        var eventDescription = EventDescription.Create(description);
        if (eventDescription.IsFailure)
        {
            Console.WriteLine($"Failed to create description: {eventDescription.Errors}");
            return null;
        }

        // Create address with venue name prepended
        var fullStreet = $"{venueName}, {street}";
        var address = Address.Create(fullStreet, city, "Ohio", zipCode, "USA");
        if (address.IsFailure)
        {
            Console.WriteLine($"Failed to create address: {address.Errors}");
            return null;
        }

        // Create coordinates
        var coordinates = GeoCoordinate.Create(latitude, longitude);
        if (coordinates.IsFailure)
        {
            Console.WriteLine($"Failed to create coordinates: {coordinates.Errors}");
            return null;
        }

        // Create location
        var location = EventLocation.Create(address.Value, coordinates.Value);
        if (location.IsFailure)
        {
            Console.WriteLine($"Failed to create location: {location.Errors}");
            return null;
        }

        // For past events, use a date in the past that passes validation
        // Event.Create requires startDate > DateTime.UtcNow, so we'll create it as future and manually set dates
        var futureStartDate = startDate < DateTime.UtcNow ? DateTime.UtcNow.AddDays(1) : startDate;
        var futureEndDate = endDate < DateTime.UtcNow ? DateTime.UtcNow.AddDays(2) : endDate;

        // Create event
        var eventResult = Event.Create(
            eventTitle.Value,
            eventDescription.Value,
            futureStartDate,
            futureEndDate,
            TestOrganizerId,
            capacity,
            location.Value,
            category,
            ticketPrice
        );

        if (eventResult.IsFailure)
        {
            Console.WriteLine($"Failed to create event: {eventResult.Errors}");
            return null;
        }

        var newEvent = eventResult.Value;

        // For past events, we need to use reflection or create a different approach
        // Since the Event entity has validation preventing past dates, we'll keep all events as future
        // and rely on the Status to indicate completion

        // Set the desired status
        if (status == EventStatus.Published)
        {
            newEvent.Publish();
        }
        else if (status == EventStatus.Completed)
        {
            // For completed events, publish first
            newEvent.Publish();
            // Then manually set dates using reflection to bypass validation
            typeof(Event).GetProperty("StartDate")?.SetValue(newEvent, startDate);
            typeof(Event).GetProperty("EndDate")?.SetValue(newEvent, endDate);
            // Set as completed
            newEvent.Complete();
        }

        return newEvent;
    }

    private static Money? CreateMoney(decimal amount, Currency currency)
    {
        var moneyResult = Money.Create(amount, currency);
        return moneyResult.IsSuccess ? moneyResult.Value : null;
    }
}
