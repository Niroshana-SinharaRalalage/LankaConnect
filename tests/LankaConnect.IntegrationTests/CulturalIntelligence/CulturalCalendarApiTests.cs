using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using LankaConnect.Application.CulturalIntelligence.Calendar.Queries.GetPoyadayCalculations;
using LankaConnect.Application.CulturalIntelligence.Calendar.Queries.GetFestivalDates;
using LankaConnect.Application.CulturalIntelligence.Calendar.Queries.ValidateScheduling;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace LankaConnect.IntegrationTests.CulturalIntelligence;

/// <summary>
/// Integration tests for Cultural Calendar Intelligence APIs
/// Tests Buddhist/Hindu calendar integration and cultural scheduling validation
/// </summary>
public class CulturalCalendarApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public CulturalCalendarApiTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = _factory.CreateClient();
        
        // Add authentication headers for API access
        _client.DefaultRequestHeaders.Add("X-API-Key", "test-api-key");
        _client.DefaultRequestHeaders.Add("X-API-Version", "1.0");
    }

    #region Poyaday Calculation API Tests

    [Theory]
    [InlineData(2024, 1, 25)] // Duruthu Full Moon Poyaday
    [InlineData(2024, 5, 23)] // Vesak Full Moon Poyaday  
    [InlineData(2024, 12, 13)] // Unduvap Full Moon Poyaday
    public async Task GetPoyadayCalculations_WithValidYear_ShouldReturnAccurateLunarDates(int year, int expectedMonth, int expectedDay)
    {
        // Arrange
        var request = new
        {
            Year = year,
            CalculationType = "FullMoonPoyadays",
            TimeZone = "Asia/Colombo"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/cultural-calendar/poya-days", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var calculations = JsonSerializer.Deserialize<PoyadayCalculationsResponse>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        calculations.Should().NotBeNull();
        calculations.PoyadayDates.Should().HaveCount(12); // 12 months of Full Moon Poyadays
        
        // Verify specific Poyaday calculation
        var expectedPoyaday = calculations.PoyadayDates
            .FirstOrDefault(p => p.Date.Month == expectedMonth && p.Date.Day == expectedDay);
        expectedPoyaday.Should().NotBeNull();
        expectedPoyaday.PoyadayType.Should().NotBeEmpty();
        expectedPoyaday.BuddhistSignificance.Should().NotBeEmpty();
        expectedPoyaday.IsFullMoon.Should().BeTrue();
    }

    [Fact]
    public async Task GetFestivalDates_WithHinduCalendar_ShouldReturnAccurateFestivalSchedule()
    {
        // Arrange
        var request = new
        {
            Year = 2024,
            CalendarType = "Hindu",
            Region = "SriLanka",
            IncludeRegionalVariations = true
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/cultural-calendar/festivals", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var festivals = JsonSerializer.Deserialize<FestivalDatesResponse>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        festivals.Should().NotBeNull();
        festivals.FestivalSchedule.Should().NotBeEmpty();
        
        // Verify major Hindu festivals
        var diwali = festivals.FestivalSchedule.FirstOrDefault(f => f.Name.Contains("Diwali"));
        diwali.Should().NotBeNull();
        diwali.CulturalSignificance.Should().NotBeEmpty();
        diwali.RegionalObservances.Should().NotBeEmpty();
        
        var thaipusam = festivals.FestivalSchedule.FirstOrDefault(f => f.Name.Contains("Thaipusam"));
        thaipusam.Should().NotBeNull();
        thaipusam.IsSignificantForTamilCommunity.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateEventScheduling_WithCulturalConflicts_ShouldReturnConflictAnalysis()
    {
        // Arrange
        var request = new
        {
            ProposedEvents = new[]
            {
                new
                {
                    EventId = Guid.NewGuid(),
                    Title = "Business Conference",
                    StartDate = new DateTime(2024, 5, 23), // Vesak Poyaday
                    EndDate = new DateTime(2024, 5, 23),
                    EventType = "Business",
                    TargetAudience = new[] { "Sri Lankan Buddhist", "Hindu", "General" }
                },
                new
                {
                    EventId = Guid.NewGuid(),
                    Title = "Cultural Night",
                    StartDate = new DateTime(2024, 4, 13), // Sinhala New Year
                    EndDate = new DateTime(2024, 4, 14),
                    EventType = "Cultural",
                    TargetAudience = new[] { "Sri Lankan Buddhist", "Sri Lankan Tamil" }
                }
            },
            ValidationCriteria = new
            {
                CheckBuddhistCalendar = true,
                CheckHinduCalendar = true,
                CheckCulturalConflicts = true,
                RequireAppropriatenessScoring = true
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/cultural-calendar/validate-scheduling", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var validation = JsonSerializer.Deserialize<SchedulingValidationResponse>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        validation.Should().NotBeNull();
        validation.ValidationResults.Should().HaveCount(2);
        
        // Verify Vesak conflict detection
        var vesakEvent = validation.ValidationResults.First(v => v.EventDate.Month == 5 && v.EventDate.Day == 23);
        vesakEvent.CulturalConflicts.Should().NotBeEmpty();
        vesakEvent.ConflictLevel.Should().Be("High");
        vesakEvent.Recommendations.Should().Contain("Reschedule to avoid Vesak Poyaday");
        
        // Verify Sinhala New Year appropriateness
        var newYearEvent = validation.ValidationResults.First(v => v.EventDate.Month == 4);
        newYearEvent.AppropriatenessScore.Should().BeGreaterThan(0.8); // Cultural events are appropriate during New Year
        newYearEvent.CulturalEnhancementSuggestions.Should().NotBeEmpty();
    }

    #endregion

    #region Cultural Timing Optimization Tests

    [Fact]
    public async Task OptimizeCulturalTiming_WithMultipleConstraints_ShouldReturnOptimalSchedule()
    {
        // Arrange
        var request = new
        {
            SchedulingConstraints = new
            {
                AvoidPoyadays = true,
                AvoidMajorFestivals = true,
                PreferWeekends = true,
                TimeZone = "America/New_York", // Diaspora timezone
                TargetAudiences = new[] { "Sri Lankan Buddhist", "Sri Lankan Tamil" }
            },
            SchedulingWindow = new
            {
                StartDate = DateTime.UtcNow,
                EndDate = DateTime.UtcNow.AddMonths(3)
            },
            EventRequirements = new
            {
                Duration = "2 hours",
                RequiresCulturalSensitivity = true,
                ExpectedAttendance = 100
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/cultural-calendar/optimize-timing", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var optimization = JsonSerializer.Deserialize<CulturalTimingOptimizationResponse>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        optimization.Should().NotBeNull();
        optimization.OptimalTimeSlots.Should().NotBeEmpty();
        optimization.CulturalConsiderations.Should().NotBeEmpty();
        
        // Verify cultural constraints are respected
        foreach (var timeSlot in optimization.OptimalTimeSlots)
        {
            timeSlot.ConflictsWithPoyaday.Should().BeFalse();
            timeSlot.ConflictsWithMajorFestival.Should().BeFalse();
            timeSlot.CulturalAppropriatenessScore.Should().BeGreaterThan(0.7);
        }
    }

    [Fact]
    public async Task GetSignificantDates_WithDiasporaContext_ShouldIncludeLocalAndTraditionalDates()
    {
        // Arrange
        var request = new
        {
            Year = 2024,
            DiasporaLocation = "Toronto, Canada",
            IncludeLocalHolidays = true,
            IncludeSriLankanTraditional = true,
            CommunityPreferences = new
            {
                BuddhistObservances = true,
                HinduObservances = true,
                ChristianObservances = false,
                SecularHolidays = true
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/cultural-calendar/significant-dates", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var dates = JsonSerializer.Deserialize<SignificantDatesResponse>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        dates.Should().NotBeNull();
        dates.TraditionalSriLankanDates.Should().NotBeEmpty();
        dates.LocalHolidays.Should().NotBeEmpty();
        dates.DiasporaSpecificDates.Should().NotBeEmpty();
        
        // Verify both Canadian and Sri Lankan significance
        dates.LocalHolidays.Should().Contain(d => d.Name.Contains("Canada Day"));
        dates.TraditionalSriLankanDates.Should().Contain(d => d.Name.Contains("Independence Day"));
        dates.DiasporaSpecificDates.Should().Contain(d => d.Description.Contains("diaspora community"));
    }

    #endregion

    #region Advanced Calendar Features Tests

    [Fact]
    public async Task CalculateLunarPhases_WithBuddhistCalendar_ShouldReturnPreciseCalculations()
    {
        // Arrange
        var request = new
        {
            CalculationPeriod = new
            {
                StartDate = new DateTime(2024, 1, 1),
                EndDate = new DateTime(2024, 12, 31)
            },
            CalendarSystem = "Buddhist",
            Precision = "HighAccuracy",
            IncludeMinorPhases = true
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/cultural-calendar/lunar-phases", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        
        var content = await response.Content.ReadAsStringAsync();
        var phases = JsonSerializer.Deserialize<LunarPhasesResponse>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        phases.Should().NotBeNull();
        phases.FullMoonPhases.Should().HaveCount(12); // Monthly full moons
        phases.NewMoonPhases.Should().HaveCount(12); // Monthly new moons
        phases.QuarterPhases.Should().NotBeEmpty();
        
        // Verify accuracy of major Poyadays
        var vesakFullMoon = phases.FullMoonPhases.FirstOrDefault(p => p.Date.Month == 5);
        vesakFullMoon.Should().NotBeNull();
        vesakFullMoon.BuddhistSignificance.Should().Contain("Vesak");
        vesakFullMoon.AccuracyLevel.Should().Be("HighPrecision");
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task GetPoyadayCalculations_WithInvalidYear_ShouldReturnBadRequest()
    {
        // Arrange
        var request = new
        {
            Year = 1800, // Too old for accurate calculations
            CalculationType = "FullMoonPoyadays"
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/cultural-calendar/poya-days", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Year must be between 1900 and 2100");
    }

    [Fact]
    public async Task ValidateEventScheduling_WithoutTargetAudience_ShouldReturnBadRequest()
    {
        // Arrange
        var request = new
        {
            ProposedEvents = new[]
            {
                new
                {
                    EventId = Guid.NewGuid(),
                    Title = "Event Without Audience",
                    StartDate = DateTime.UtcNow.AddDays(1),
                    EndDate = DateTime.UtcNow.AddDays(1)
                    // Missing TargetAudience
                }
            }
        };

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/cultural-calendar/validate-scheduling", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("TargetAudience is required for cultural validation");
    }

    #endregion
}

#region Response DTOs

public class PoyadayCalculationsResponse
{
    public List<PoyadayDto> PoyadayDates { get; set; } = new();
    public int Year { get; set; }
    public string CalculationMethod { get; set; } = string.Empty;
    public double AccuracyEstimate { get; set; }
}

public class PoyadayDto
{
    public DateTime Date { get; set; }
    public string PoyadayType { get; set; } = string.Empty;
    public string BuddhistSignificance { get; set; } = string.Empty;
    public bool IsFullMoon { get; set; }
    public string SinhalaName { get; set; } = string.Empty;
}

public class FestivalDatesResponse
{
    public List<FestivalDto> FestivalSchedule { get; set; } = new();
    public string CalendarType { get; set; } = string.Empty;
    public int Year { get; set; }
}

public class FestivalDto
{
    public string Name { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string CulturalSignificance { get; set; } = string.Empty;
    public List<string> RegionalObservances { get; set; } = new();
    public bool IsSignificantForTamilCommunity { get; set; }
}

public class SchedulingValidationResponse
{
    public List<ValidationResultDto> ValidationResults { get; set; } = new();
    public string OverallRecommendation { get; set; } = string.Empty;
}

public class ValidationResultDto
{
    public Guid EventId { get; set; }
    public DateTime EventDate { get; set; }
    public List<string> CulturalConflicts { get; set; } = new();
    public string ConflictLevel { get; set; } = string.Empty;
    public List<string> Recommendations { get; set; } = new();
    public double AppropriatenessScore { get; set; }
    public List<string> CulturalEnhancementSuggestions { get; set; } = new();
}

public class CulturalTimingOptimizationResponse
{
    public List<OptimalTimeSlotDto> OptimalTimeSlots { get; set; } = new();
    public List<string> CulturalConsiderations { get; set; } = new();
}

public class OptimalTimeSlotDto
{
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public double CulturalAppropriatenessScore { get; set; }
    public bool ConflictsWithPoyaday { get; set; }
    public bool ConflictsWithMajorFestival { get; set; }
    public string Reasoning { get; set; } = string.Empty;
}

public class SignificantDatesResponse
{
    public List<SignificantDateDto> TraditionalSriLankanDates { get; set; } = new();
    public List<SignificantDateDto> LocalHolidays { get; set; } = new();
    public List<SignificantDateDto> DiasporaSpecificDates { get; set; } = new();
}

public class SignificantDateDto
{
    public DateTime Date { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
}

public class LunarPhasesResponse
{
    public List<LunarPhaseDto> FullMoonPhases { get; set; } = new();
    public List<LunarPhaseDto> NewMoonPhases { get; set; } = new();
    public List<LunarPhaseDto> QuarterPhases { get; set; } = new();
}

public class LunarPhaseDto
{
    public DateTime Date { get; set; }
    public string PhaseType { get; set; } = string.Empty;
    public string BuddhistSignificance { get; set; } = string.Empty;
    public string AccuracyLevel { get; set; } = string.Empty;
}

#endregion