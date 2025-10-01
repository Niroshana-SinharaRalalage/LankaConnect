using LankaConnect.Application.CulturalIntelligence.Events.Queries.GetEventRecommendations;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.ValueObjects;
using LankaConnect.Domain.Events.ValueObjects.Recommendations;
using LankaConnect.Domain.Common;
using FluentAssertions;
using Xunit;

namespace LankaConnect.Application.Tests.CulturalIntelligence.Events.Queries.GetEventRecommendations;

/// <summary>
/// TDD RED Phase: Tests for EventRecommendation mapping that should fail initially
/// </summary>
public class EventRecommendationMappingTests
{
    [Fact]
    public void ConvertToDto_ShouldMapDomainEventRecommendationToApplicationDto()
    {
        // Arrange
        var eventTitle = EventTitle.Create("Test Event");
        var eventDescription = EventDescription.Create("Test Description");
        var startDate = DateTime.UtcNow.AddDays(1);
        var endDate = DateTime.UtcNow.AddDays(2);
        var organizerId = Guid.NewGuid();

        var domainEvent = Event.Create(
            eventTitle.Value,
            eventDescription.Value,
            startDate,
            endDate,
            organizerId,
            100
        );

        var recommendationScore = new RecommendationScore(0.85);
        var domainRecommendation = new EventRecommendation(
            domainEvent.Value,
            recommendationScore,
            "Test recommendation reason",
            0.95
        );

        // Act
        var dto = EventRecommendationMapper.ConvertToDto(domainRecommendation);

        // Assert
        dto.Should().NotBeNull();
        dto.EventId.Should().Be(domainEvent.Value.Id);
        dto.Title.Should().Be(eventTitle.Value.Value);
        dto.Description.Should().Be(eventDescription.Value.Value);
        dto.StartDate.Should().Be(startDate);
        dto.EndDate.Should().Be(endDate);
        dto.RecommendationScore.Should().Be(0.85);
        dto.RecommendationReasons.Should().Contain("Test recommendation reason");
    }

    [Fact]
    public void ConvertToDtoWithCulturalScore_ShouldMapCulturalScoreCorrectly()
    {
        // Arrange
        var culturalScore = new Domain.Events.ValueObjects.Recommendations.CulturalScore(0.75);
        var extendedCulturalScore = new ExtendedCulturalScore(
            overallScore: 0.8,
            appropriatenessScore: 0.75,
            diasporaFriendlinessScore: 0.85,
            conflictLevel: Domain.Events.ValueObjects.Recommendations.CulturalConflictLevel.Low,
            factors: new List<string> { "Buddhist calendar compatible", "Traditional Sri Lankan event" }
        );

        // Act
        var dto = EventRecommendationMapper.ConvertCulturalScoreToDto(extendedCulturalScore);

        // Assert
        dto.Should().NotBeNull();
        dto.OverallScore.Should().Be(0.8);
        dto.AppropriatenessScore.Should().Be(0.75);
        dto.DiasporaFriendlinessScore.Should().Be(0.85);
        dto.ConflictLevel.Should().Be("Low");
        dto.CulturalFactors.Should().HaveCount(2);
        dto.CulturalFactors.Should().Contain("Buddhist calendar compatible");
        dto.CulturalFactors.Should().Contain("Traditional Sri Lankan event");
    }

    [Fact]
    public void ConvertMultipleRecommendations_ShouldMapAllCorrectly()
    {
        // Arrange
        var recommendations = CreateTestDomainRecommendations(3);

        // Act
        var dtos = EventRecommendationMapper.ConvertToDtos(recommendations);

        // Assert
        dtos.Should().HaveCount(3);
        dtos.Should().AllSatisfy(dto => dto.Should().NotBeNull());
        dtos.Should().AllSatisfy(dto => dto.EventId.Should().NotBeEmpty());
        dtos.Should().AllSatisfy(dto => dto.RecommendationScore.Should().BeGreaterThan(0));
    }

    private static IEnumerable<EventRecommendation> CreateTestDomainRecommendations(int count)
    {
        var recommendations = new List<EventRecommendation>();

        for (int i = 0; i < count; i++)
        {
            var eventTitle = EventTitle.Create($"Test Event {i + 1}");
            var eventDescription = EventDescription.Create($"Test Description {i + 1}");
            var startDate = DateTime.UtcNow.AddDays(i + 1);
            var endDate = DateTime.UtcNow.AddDays(i + 2);
            var organizerId = Guid.NewGuid();

            var domainEvent = Event.Create(
                eventTitle.Value,
                eventDescription.Value,
                startDate,
                endDate,
                organizerId,
                100
            );

            var recommendationScore = new RecommendationScore(0.7 + (i * 0.1));
            var recommendation = new EventRecommendation(
                domainEvent.Value,
                recommendationScore,
                $"Test recommendation reason {i + 1}",
                0.9
            );

            recommendations.Add(recommendation);
        }

        return recommendations;
    }
}

/// <summary>
/// Extended cultural score with missing properties needed by the application layer
/// This class bridges the gap between domain CulturalScore and application CulturalScoreDto
/// </summary>
public class ExtendedCulturalScore
{
    public double OverallScore { get; }
    public double AppropriatenessScore { get; }
    public double DiasporaFriendlinessScore { get; }
    public Domain.Events.ValueObjects.Recommendations.CulturalConflictLevel ConflictLevel { get; }
    public List<string> Factors { get; }

    public ExtendedCulturalScore(
        double overallScore,
        double appropriatenessScore,
        double diasporaFriendlinessScore,
        Domain.Events.ValueObjects.Recommendations.CulturalConflictLevel conflictLevel,
        List<string> factors)
    {
        OverallScore = overallScore;
        AppropriatenessScore = appropriatenessScore;
        DiasporaFriendlinessScore = diasporaFriendlinessScore;
        ConflictLevel = conflictLevel;
        Factors = factors ?? new List<string>();
    }
}

/// <summary>
/// Cultural conflict level enum - should be added to domain once tests pass
/// </summary>
public enum CulturalConflictLevel
{
    None,
    Low,
    Medium,
    High,
    Critical
}