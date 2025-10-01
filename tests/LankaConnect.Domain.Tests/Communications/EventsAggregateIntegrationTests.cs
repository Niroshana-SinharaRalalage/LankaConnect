using LankaConnect.Domain.Communications.Entities;
using LankaConnect.Domain.Communications.Services;
using LankaConnect.Domain.Communications.ValueObjects;
using LankaConnect.Domain.Communications.Enums;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.Services;
using LankaConnect.Domain.Events.ValueObjects;
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Domain.Events.DomainEvents;
using UserEmail = LankaConnect.Domain.Users.ValueObjects.Email;
using FluentAssertions;
using Moq;
using Xunit;

namespace LankaConnect.Domain.Tests.Communications;

/// <summary>
/// TDD RED Phase: Events Aggregate Integration Tests (15 Tests)
/// Testing cross-aggregate email communications with cultural awareness
/// Following London School TDD with mock-driven development for aggregate interactions
/// </summary>
public class EventsAggregateIntegrationTests
{
    private readonly UserEmail _validFromEmail = UserEmail.Create("events@lankaconnect.com").Value;
    private readonly UserEmail _validToEmail = UserEmail.Create("participant@example.com").Value;
    private readonly Mock<IEventRepository> _mockEventRepository;
    private readonly Mock<ICulturalCalendarService> _mockCulturalCalendar;
    private readonly Mock<IEmailTemplateService> _mockTemplateService;
    private readonly Mock<IEventNotificationService> _mockNotificationService;
    private readonly Mock<IEventRecommendationEngine> _mockRecommendationEngine;

    public EventsAggregateIntegrationTests()
    {
        _mockEventRepository = new Mock<IEventRepository>();
        _mockCulturalCalendar = new Mock<ICulturalCalendarService>();
        _mockTemplateService = new Mock<IEmailTemplateService>();
        _mockNotificationService = new Mock<IEventNotificationService>();
        _mockRecommendationEngine = new Mock<IEventRecommendationEngine>();
    }

    #region Event Registration Confirmation Tests

    [Fact]
    public void EventRegistrationConfirmation_WithCulturalContext_ShouldGenerateLocalizedEmail()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var culturalEvent = CreateCulturalEvent(eventId, "Vesak Day Celebration", DateTime.UtcNow.AddDays(30));
        
        var registrationEvent = new RegistrationConfirmedEvent(eventId, userId, 2, DateTime.UtcNow);
        
        _mockEventRepository
            .Setup(x => x.GetByIdAsync(eventId))
            .ReturnsAsync(culturalEvent);
            
        _mockTemplateService
            .Setup(x => x.GetEventRegistrationTemplate(EmailType.EventNotification, SriLankanLanguage.Sinhala))
            .Returns(new EventEmailTemplate(
                Subject: "සිදුවීම් ලියාපදිංචිය සනාථ කිරීම",
                Body: "ඔබගේ වේසක් දින සැමරුම සඳහා ලියාපදිංචිය සාර්ථකව සම්පූර්ණ කර ඇත",
                CulturalContext: CulturalBackground.SinhalaBuddhist,
                FestivalReferences: new[] { "Vesak blessings", "Triple gem salutation" }
            )); // Will fail - interface and class don't exist

        var eventEmailService = new EventCulturalEmailService(
            _mockEventRepository.Object,
            _mockTemplateService.Object,
            _mockCulturalCalendar.Object
        ); // Will fail - class doesn't exist

        // Act
        var result = eventEmailService.HandleRegistrationConfirmed(registrationEvent, _validToEmail);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Subject.Should().Contain("සිදුවීම්");
        result.Value.Body.Should().Contain("වේසක්");
        result.Value.CulturalAdaptations.Should().Contain("Triple gem salutation"); // Will fail - property doesn't exist
        result.Value.EventContext.Should().NotBeNull(); // Will fail - property doesn't exist
        
        // Verify repository interaction
        _mockEventRepository.Verify(x => x.GetByIdAsync(eventId), Times.Once);
        _mockTemplateService.Verify(x => x.GetEventRegistrationTemplate(It.IsAny<EmailType>(), It.IsAny<SriLankanLanguage>()), Times.Once);
    }

    [Fact]
    public void EventRegistrationConfirmation_MultiRecipient_ShouldPersonalizeForEachCulture()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var culturalEvent = CreateCulturalEvent(eventId, "Harmony Festival", DateTime.UtcNow.AddDays(15));
        
        var recipients = new[]
        {
            new EventRecipient(_validToEmail, CulturalBackground.SinhalaBuddhist, SriLankanLanguage.Sinhala),
            new EventRecipient(UserEmail.Create("tamil@example.com").Value, CulturalBackground.TamilHindu, SriLankanLanguage.Tamil),
            new EventRecipient(UserEmail.Create("muslim@example.com").Value, CulturalBackground.SriLankanMuslim, SriLankanLanguage.English)
        }; // Will fail - class doesn't exist

        var registrationEvent = new RegistrationConfirmedEvent(eventId, Guid.NewGuid(), 3, DateTime.UtcNow);
        
        _mockEventRepository
            .Setup(x => x.GetByIdAsync(eventId))
            .ReturnsAsync(culturalEvent);

        var eventEmailService = new EventCulturalEmailService(
            _mockEventRepository.Object,
            _mockTemplateService.Object,
            _mockCulturalCalendar.Object
        );

        // Act
        var result = eventEmailService.SendMultiCulturalRegistrationConfirmations(registrationEvent, recipients);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.GeneratedEmails.Should().HaveCount(3);
        result.Value.CulturalVariations.Should().HaveCount(3); // Will fail - return type doesn't exist
        result.Value.LanguageDistribution.Should().ContainKeys(
            SriLankanLanguage.Sinhala,
            SriLankanLanguage.Tamil,
            SriLankanLanguage.English
        ); // Will fail - property doesn't exist
        
        _mockEventRepository.Verify(x => x.GetByIdAsync(eventId), Times.Once);
    }

    #endregion

    #region Event Reminder Email Tests

    [Fact]
    public void EventReminderEmail_WithCulturalCalendarIntegration_ShouldOptimizeTimingForPoyaday()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var buddhistEvent = CreateCulturalEvent(eventId, "Dhamma Discussion", DateTime.UtcNow.AddDays(7));
        var poyadayDate = DateTime.UtcNow.AddDays(5);
        
        _mockCulturalCalendar
            .Setup(x => x.IsPoyaday(poyadayDate))
            .Returns(true);
            
        _mockCulturalCalendar
            .Setup(x => x.GetOptimalReminderTime(buddhistEvent.StartDate, CulturalCalendarType.Buddhist))
            .Returns(poyadayDate.AddDays(1).AddHours(6)); // Day after Poyaday, early morning

        var reminderService = new CulturalEventReminderService(
            _mockEventRepository.Object,
            _mockCulturalCalendar.Object,
            _mockTemplateService.Object
        ); // Will fail - class doesn't exist

        // Act
        var result = reminderService.ScheduleReminderWithCulturalOptimization(buddhistEvent, CulturalBackground.SinhalaBuddhist);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.OptimalReminderTime.Should().BeAfter(poyadayDate);
        result.Value.CulturalDelayReason.Should().Contain("Poyaday"); // Will fail - return type doesn't exist
        result.Value.ReligiousContext.Should().Be(ReligiousContext.Buddhist); // Will fail - property and enum don't exist
        
        _mockCulturalCalendar.Verify(x => x.IsPoyaday(It.IsAny<DateTime>()), Times.AtLeastOnce);
        _mockCulturalCalendar.Verify(x => x.GetOptimalReminderTime(It.IsAny<DateTime>(), CulturalCalendarType.Buddhist), Times.Once);
    }

    [Fact]
    public void EventReminderEmail_DuringRamadan_ShouldScheduleAfterIftar()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var islamicEvent = CreateCulturalEvent(eventId, "Iftar Community Gathering", DateTime.UtcNow.AddDays(10));
        var ramadanDate = DateTime.UtcNow.AddDays(7);
        
        _mockCulturalCalendar
            .Setup(x => x.IsRamadan(ramadanDate))
            .Returns(true);
            
        _mockCulturalCalendar
            .Setup(x => x.GetIftarTime(ramadanDate, GeographicRegion.SriLanka))
            .Returns(TimeSpan.FromHours(18.5)); // 6:30 PM

        var reminderService = new CulturalEventReminderService(
            _mockEventRepository.Object,
            _mockCulturalCalendar.Object,
            _mockTemplateService.Object
        );

        // Act
        var result = reminderService.ScheduleReminderWithCulturalOptimization(islamicEvent, CulturalBackground.SriLankanMuslim);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.OptimalReminderTime.TimeOfDay.Should().BeAfter(TimeSpan.FromHours(19)); // After Iftar
        result.Value.RamadanConsiderations.Should().Contain("After Iftar"); // Will fail - property doesn't exist
        result.Value.IslamicContext.Should().NotBeNull(); // Will fail - property doesn't exist
    }

    [Fact]
    public void EventReminderEmail_MultiDayEvent_ShouldSendCulturallyAwareReminders()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var multiDayEvent = CreateCulturalEvent(eventId, "Perahera Festival", DateTime.UtcNow.AddDays(30), DateTime.UtcNow.AddDays(33));
        
        _mockCulturalCalendar
            .Setup(x => x.GetPeraheraTradition(It.IsAny<DateTime>()))
            .Returns(new PeraheraTradition(
                ProcessionDays: 3,
                CulturalSignificance: "Buddhist traditional pageant",
                OptimalReminderSchedule: new[] { 7, 3, 1 } // Days before
            )); // Will fail - method and class don't exist

        var reminderService = new CulturalEventReminderService(
            _mockEventRepository.Object,
            _mockCulturalCalendar.Object,
            _mockTemplateService.Object
        );

        // Act
        var result = reminderService.CreatePeraheraReminderSeries(multiDayEvent, CulturalBackground.SinhalaBuddhist);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.ReminderEmails.Should().HaveCount(3); // Will fail - return type doesn't exist
        result.Value.CulturalNarrative.Should().Contain("Buddhist traditional pageant"); // Will fail - property doesn't exist
        result.Value.ProcessionContext.Should().NotBeNull(); // Will fail - property doesn't exist
    }

    #endregion

    #region Event Cancellation Tests

    [Fact]
    public void EventCancellationNotification_CulturalEvent_ShouldProvideRespectfulCancellation()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var culturalEvent = CreateCulturalEvent(eventId, "Tamil Cultural Night", DateTime.UtcNow.AddDays(20));
        var cancellationEvent = new EventCancelledEvent(eventId, "Venue unavailable", DateTime.UtcNow);
        
        _mockEventRepository
            .Setup(x => x.GetByIdAsync(eventId))
            .ReturnsAsync(culturalEvent);
            
        _mockTemplateService
            .Setup(x => x.GetCancellationTemplate(CulturalBackground.TamilHindu, SriLankanLanguage.Tamil))
            .Returns(new CancellationEmailTemplate(
                Subject: "கலாச்சார நிகழ்வு ரத்து",
                Body: "மன்னிப்புடன் தெரிவித்துக்கொள்கிறோம், தமிழ் கலாச்சார இரவு ரத்து செய்யப்பட்டுள்ளது",
                ApologyTone: CulturalApologyTone.Respectful,
                CulturalSensitivity: "Tamil community appropriate language"
            )); // Will fail - interface and class don't exist

        var cancellationService = new CulturalEventCancellationService(
            _mockEventRepository.Object,
            _mockTemplateService.Object,
            _mockCulturalCalendar.Object
        ); // Will fail - class doesn't exist

        // Act
        var result = cancellationService.HandleCulturalEventCancellation(cancellationEvent, CulturalBackground.TamilHindu);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Subject.Should().Contain("கலாச்சார");
        result.Value.ApologyLevel.Should().Be(CulturalApologyTone.Respectful); // Will fail - property and enum don't exist
        result.Value.CulturalSensitivityScore.Should().BeGreaterThan(8); // Will fail - property doesn't exist
        
        _mockEventRepository.Verify(x => x.GetByIdAsync(eventId), Times.Once);
        _mockTemplateService.Verify(x => x.GetCancellationTemplate(It.IsAny<CulturalBackground>(), It.IsAny<SriLankanLanguage>()), Times.Once);
    }

    [Fact]
    public void EventCancellationNotification_ReligiousEvent_ShouldIncludeAlternativeOpportunities()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var religiousEvent = CreateCulturalEvent(eventId, "Meditation Retreat", DateTime.UtcNow.AddDays(14));
        var cancellationEvent = new EventCancelledEvent(eventId, "Instructor illness", DateTime.UtcNow);
        
        _mockEventRepository
            .Setup(x => x.GetByIdAsync(eventId))
            .ReturnsAsync(religiousEvent);
            
        _mockRecommendationEngine
            .Setup(x => x.FindAlternativeReligiousEvents(religiousEvent, CulturalBackground.SinhalaBuddhist))
            .Returns(new AlternativeEventRecommendations(
                SimilarEvents: new[] { "Online Dhamma Talk", "Temple Visit", "Group Meditation" },
                TimeframeMatches: 3,
                CulturalRelevanceScore: 0.95
            )); // Will fail - interface and class don't exist

        var cancellationService = new CulturalEventCancellationService(
            _mockEventRepository.Object,
            _mockTemplateService.Object,
            _mockCulturalCalendar.Object
        );

        // Act
        var result = cancellationService.HandleReligiousEventCancellation(cancellationEvent, CulturalBackground.SinhalaBuddhist, _mockRecommendationEngine.Object);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.AlternativeEvents.Should().HaveCount(3); // Will fail - return type doesn't exist
        result.Value.RecommendationContext.Should().Be("Buddhist spiritual activities"); // Will fail - property doesn't exist
        result.Value.CulturalRelevanceScore.Should().Be(0.95); // Will fail - property doesn't exist
        
        _mockRecommendationEngine.Verify(x => x.FindAlternativeReligiousEvents(It.IsAny<Event>(), CulturalBackground.SinhalaBuddhist), Times.Once);
    }

    #endregion

    #region Event Recommendation Tests

    [Fact]
    public void EventRecommendationEmail_CulturalPreferences_ShouldPersonalizeRecommendations()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var userProfile = new CulturalUserProfile(
            CulturalBackground.SinhalaBuddhist,
            PreferredLanguage: SriLankanLanguage.Sinhala,
            InterestCategories: new[] { EventCategory.Cultural, EventCategory.Religious },
            GeographicPreference: GeographicRegion.WesternProvince
        ); // Will fail - class doesn't exist
        
        var recommendations = new EventRecommendation[]
        {
            new("Vesak Lantern Festival", DateTime.UtcNow.AddDays(10), CulturalRelevanceScore: 0.98, "Traditional Buddhist celebration"),
            new("Meditation Workshop", DateTime.UtcNow.AddDays(15), CulturalRelevanceScore: 0.85, "Mindfulness and inner peace"),
            new("Sinhala Poetry Reading", DateTime.UtcNow.AddDays(22), CulturalRelevanceScore: 0.78, "Literary cultural event")
        }; // Will fail - class doesn't exist
        
        _mockRecommendationEngine
            .Setup(x => x.GeneratePersonalizedRecommendations(userId, userProfile))
            .Returns(recommendations);
            
        _mockTemplateService
            .Setup(x => x.GetRecommendationTemplate(CulturalBackground.SinhalaBuddhist, SriLankanLanguage.Sinhala))
            .Returns(new RecommendationEmailTemplate(
                Subject: "ඔබට සුදුසු සිදුවීම්",
                IntroText: "ඔබගේ සංස්කෘතික සහ ආධ්යාත්මික අභිරුචිකමට අනුව",
                CulturalContext: "Buddhist mindful recommendations"
            )); // Will fail - interface and class don't exist

        var recommendationService = new CulturalEventRecommendationService(
            _mockRecommendationEngine.Object,
            _mockTemplateService.Object,
            _mockCulturalCalendar.Object
        ); // Will fail - class doesn't exist

        // Act
        var result = recommendationService.GeneratePersonalizedRecommendationEmail(userId, userProfile, _validToEmail);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Subject.Should().Contain("සිදුවීම්");
        result.Value.RecommendedEvents.Should().HaveCount(3); // Will fail - return type doesn't exist
        result.Value.CulturalRelevanceAverage.Should().BeGreaterThan(0.8); // Will fail - property doesn't exist
        result.Value.PersonalizationScore.Should().BeGreaterThan(0.9); // Will fail - property doesn't exist
        
        _mockRecommendationEngine.Verify(x => x.GeneratePersonalizedRecommendations(userId, userProfile), Times.Once);
        _mockTemplateService.Verify(x => x.GetRecommendationTemplate(CulturalBackground.SinhalaBuddhist, SriLankanLanguage.Sinhala), Times.Once);
    }

    [Fact]
    public void EventRecommendationEmail_DiasporaCommunity_ShouldIncludeVirtualOptions()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var diasporaProfile = new DiasporaCulturalProfile(
            OriginRegion: GeographicRegion.SriLanka,
            CurrentRegion: GeographicRegion.UnitedStates,
            CulturalBackground: CulturalBackground.TamilHindu,
            ConnectivityPreference: ConnectivityType.Virtual
        ); // Will fail - class doesn't exist
        
        var virtualRecommendations = new EventRecommendation[]
        {
            new("Virtual Tamil Language Class", DateTime.UtcNow.AddDays(7), CulturalRelevanceScore: 0.92, "Online cultural learning"),
            new("Global Tamil Heritage Webinar", DateTime.UtcNow.AddDays(14), CulturalRelevanceScore: 0.89, "International diaspora connection"),
            new("Virtual Bharatanatyam Performance", DateTime.UtcNow.AddDays(21), CulturalRelevanceScore: 0.87, "Traditional dance appreciation")
        };
        
        _mockRecommendationEngine
            .Setup(x => x.GenerateDiasporaRecommendations(userId, diasporaProfile))
            .Returns(virtualRecommendations); // Will fail - method doesn't exist

        var recommendationService = new CulturalEventRecommendationService(
            _mockRecommendationEngine.Object,
            _mockTemplateService.Object,
            _mockCulturalCalendar.Object
        );

        // Act
        var result = recommendationService.GenerateDiasporaRecommendationEmail(userId, diasporaProfile, _validToEmail);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.VirtualEvents.Should().HaveCount(3); // Will fail - return type doesn't exist
        result.Value.DiasporaContext.Should().NotBeNull(); // Will fail - property doesn't exist
        result.Value.CulturalConnectionScore.Should().BeGreaterThan(0.85); // Will fail - property doesn't exist
        
        _mockRecommendationEngine.Verify(x => x.GenerateDiasporaRecommendations(userId, diasporaProfile), Times.Once);
    }

    #endregion

    #region Event Waitlist and Registration Management Tests

    [Fact]
    public void WaitlistNotificationEmail_CulturalEvent_ShouldPrioritizeCulturalRelevance()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var culturalEvent = CreateCulturalEvent(eventId, "Traditional Dance Workshop", DateTime.UtcNow.AddDays(5));
        var waitlistUsers = new WaitlistUser[]
        {
            new(Guid.NewGuid(), CulturalBackground.TamilHindu, RegistrationPriority.High, "Dance experience"),
            new(Guid.NewGuid(), CulturalBackground.SinhalaBuddhist, RegistrationPriority.Medium, "Cultural interest"),
            new(Guid.NewGuid(), CulturalBackground.SriLankanChristian, RegistrationPriority.Low, "General interest")
        }; // Will fail - class doesn't exist
        
        var waitlistService = new CulturalWaitlistNotificationService(
            _mockEventRepository.Object,
            _mockTemplateService.Object,
            _mockCulturalCalendar.Object
        ); // Will fail - class doesn't exist

        // Act
        var result = waitlistService.NotifyWaitlistWithCulturalPriority(culturalEvent, waitlistUsers);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.NotifiedUsers.Should().HaveCount(3);
        result.Value.CulturalPriorityOrder.Should().BeEquivalentTo(new[] 
        { 
            CulturalBackground.TamilHindu,  // Highest cultural relevance for dance
            CulturalBackground.SinhalaBuddhist,
            CulturalBackground.SriLankanChristian
        }); // Will fail - return type doesn't exist
        result.Value.PriorityExplanations.Should().NotBeEmpty(); // Will fail - property doesn't exist
    }

    [Fact]
    public void PostEventFollowup_CulturalGathering_ShouldIncludeCulturalReflections()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var completedEvent = CreateCulturalEvent(eventId, "Poson Poya Celebration", DateTime.UtcNow.AddDays(-1)); // Yesterday
        completedEvent.Complete();
        
        var attendees = new EventAttendee[]
        {
            new(_validToEmail, CulturalBackground.SinhalaBuddhist, AttendanceStatus.Attended, "Inspiring ceremony"),
            new(UserEmail.Create("attendee2@example.com").Value, CulturalBackground.SinhalaBuddhist, AttendanceStatus.Attended, "Peaceful atmosphere")
        }; // Will fail - class doesn't exist
        
        _mockEventRepository
            .Setup(x => x.GetByIdAsync(eventId))
            .ReturnsAsync(completedEvent);
            
        _mockTemplateService
            .Setup(x => x.GetFollowupTemplate(EventCategory.Religious, CulturalBackground.SinhalaBuddhist))
            .Returns(new PostEventFollowupTemplate(
                Subject: "පෝසන් පෝය සැමරුම - ස්තූතිය",
                ThankYouMessage: "පෝසන් පෝය සැමරුමට සහභාගී වීම පිළිබඳ ස්තූතිය",
                CulturalReflections: "බුද්ධ ධර්මයේ ශ්‍රී ලංකාවට ආගමනය සිහිකරමු",
                NextSimilarEvents: "ඊළඟ ආධ්යාත්මික සිදුවීම්"
            )); // Will fail - interface and class don't exist

        var followupService = new CulturalEventFollowupService(
            _mockEventRepository.Object,
            _mockTemplateService.Object,
            _mockCulturalCalendar.Object
        ); // Will fail - class doesn't exist

        // Act
        var result = followupService.SendCulturalEventFollowup(completedEvent, attendees);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.FollowupEmails.Should().HaveCount(2);
        result.Value.CulturalContext.Should().Be(ReligiousContext.Buddhist); // Will fail - return type doesn't exist
        result.Value.ReflectionContent.Should().Contain("බුද්ධ ධර්මය"); // Will fail - property doesn't exist
        result.Value.FutureEventSuggestions.Should().NotBeEmpty(); // Will fail - property doesn't exist
        
        _mockEventRepository.Verify(x => x.GetByIdAsync(eventId), Times.Once);
        _mockTemplateService.Verify(x => x.GetFollowupTemplate(EventCategory.Religious, CulturalBackground.SinhalaBuddhist), Times.Once);
    }

    #endregion

    #region Helper Methods

    private Event CreateCulturalEvent(Guid eventId, string title, DateTime startDate, DateTime? endDate = null)
    {
        var eventTitle = EventTitle.Create(title).Value;
        var eventDescription = EventDescription.Create($"Cultural event: {title}").Value;
        var organizerId = Guid.NewGuid();
        
        var eventResult = Event.Create(
            eventTitle, 
            eventDescription, 
            startDate, 
            endDate ?? startDate.AddHours(3), 
            organizerId, 
            100
        );
        
        if (eventResult.IsSuccess)
        {
            var @event = eventResult.Value;
            
            // Use reflection to set the Id for testing purposes
            var idProperty = typeof(Event).BaseType?.GetProperty("Id");
            idProperty?.SetValue(@event, eventId);
            
            return @event;
        }
        
        throw new InvalidOperationException("Failed to create test event");
    }

    #endregion
}