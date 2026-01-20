using FluentAssertions;
using Moq;
using LankaConnect.Application.Events.Queries.SearchEvents;
using LankaConnect.Application.Events.Common;
using LankaConnect.Application.Common.Models;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Domain.Events.ValueObjects;
using LankaConnect.Domain.Shared.ValueObjects;
using LankaConnect.Domain.Business.ValueObjects;
using AutoMapper;
using Microsoft.Extensions.Logging;

namespace LankaConnect.Application.Tests.Events.Queries;

public class SearchEventsQueryHandlerTests
{
    private readonly Mock<IEventRepository> _mockRepository;
    private readonly Mock<IMapper> _mockMapper;
    private readonly Mock<ILogger<SearchEventsQueryHandler>> _mockLogger;
    private readonly SearchEventsQueryHandler _handler;

    public SearchEventsQueryHandlerTests()
    {
        _mockRepository = new Mock<IEventRepository>();
        _mockMapper = new Mock<IMapper>();
        _mockLogger = new Mock<ILogger<SearchEventsQueryHandler>>();
        _handler = new SearchEventsQueryHandler(_mockRepository.Object, _mockMapper.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task Handle_ValidSearchTerm_ReturnsMatchingEvents()
    {
        // Arrange
        var query = new SearchEventsQuery("cricket tournament", Page: 1, PageSize: 20);
        var events = CreateSampleEvents();
        var totalCount = 5;

        _mockRepository
            .Setup(x => x.SearchAsync(
                query.SearchTerm,
                It.IsAny<int>(),
                It.IsAny<int>(),
                query.Category,
                query.IsFreeOnly,
                query.StartDateFrom,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((events, totalCount));

        var dtos = events.Select(e => new EventSearchResultDto
        {
            Id = e.Id,
            Title = e.Title.Value,
            SearchRelevance = 0.95m
        }).ToList();

        _mockMapper
            .Setup(x => x.Map<IEnumerable<EventSearchResultDto>>(events))
            .Returns(dtos);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Items.Should().HaveCount(5);
        result.Value.TotalCount.Should().Be(totalCount);
        result.Value.Page.Should().Be(1);
        result.Value.PageSize.Should().Be(20);
        result.Value.TotalPages.Should().Be(1);
    }

    [Fact]
    public async Task Handle_NoResults_ReturnsEmptyPagedResult()
    {
        // Arrange
        var query = new SearchEventsQuery("nonexistent event", Page: 1, PageSize: 20);

        _mockRepository
            .Setup(x => x.SearchAsync(
                query.SearchTerm,
                It.IsAny<int>(),
                It.IsAny<int>(),
                query.Category,
                query.IsFreeOnly,
                query.StartDateFrom,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Array.Empty<Event>(), 0));

        _mockMapper
            .Setup(x => x.Map<IEnumerable<EventSearchResultDto>>(It.IsAny<IEnumerable<Event>>()))
            .Returns(Array.Empty<EventSearchResultDto>());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().BeEmpty();
        result.Value.TotalCount.Should().Be(0);
        result.Value.TotalPages.Should().Be(0);
    }

    [Fact]
    public async Task Handle_WithCategoryFilter_PassesFilterToRepository()
    {
        // Arrange
        var query = new SearchEventsQuery(
            "festival",
            Page: 1,
            PageSize: 20,
            Category: EventCategory.Cultural);

        _mockRepository
            .Setup(x => x.SearchAsync(
                query.SearchTerm,
                It.IsAny<int>(),
                It.IsAny<int>(),
                EventCategory.Cultural,
                query.IsFreeOnly,
                query.StartDateFrom,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Array.Empty<Event>(), 0));

        _mockMapper
            .Setup(x => x.Map<IEnumerable<EventSearchResultDto>>(It.IsAny<IEnumerable<Event>>()))
            .Returns(Array.Empty<EventSearchResultDto>());

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        _mockRepository.Verify(
            x => x.SearchAsync(
                query.SearchTerm,
                It.IsAny<int>(),
                It.IsAny<int>(),
                EventCategory.Cultural,
                query.IsFreeOnly,
                query.StartDateFrom,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithIsFreeOnlyFilter_PassesFilterToRepository()
    {
        // Arrange
        var query = new SearchEventsQuery(
            "workshop",
            Page: 1,
            PageSize: 20,
            IsFreeOnly: true);

        _mockRepository
            .Setup(x => x.SearchAsync(
                query.SearchTerm,
                It.IsAny<int>(),
                It.IsAny<int>(),
                query.Category,
                true,
                query.StartDateFrom,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Array.Empty<Event>(), 0));

        _mockMapper
            .Setup(x => x.Map<IEnumerable<EventSearchResultDto>>(It.IsAny<IEnumerable<Event>>()))
            .Returns(Array.Empty<EventSearchResultDto>());

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        _mockRepository.Verify(
            x => x.SearchAsync(
                query.SearchTerm,
                It.IsAny<int>(),
                It.IsAny<int>(),
                query.Category,
                true,
                query.StartDateFrom,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithStartDateFromFilter_PassesFilterToRepository()
    {
        // Arrange
        var startDate = DateTime.UtcNow.AddDays(7);
        var query = new SearchEventsQuery(
            "concert",
            Page: 1,
            PageSize: 20,
            StartDateFrom: startDate);

        _mockRepository
            .Setup(x => x.SearchAsync(
                query.SearchTerm,
                It.IsAny<int>(),
                It.IsAny<int>(),
                query.Category,
                query.IsFreeOnly,
                startDate,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Array.Empty<Event>(), 0));

        _mockMapper
            .Setup(x => x.Map<IEnumerable<EventSearchResultDto>>(It.IsAny<IEnumerable<Event>>()))
            .Returns(Array.Empty<EventSearchResultDto>());

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        _mockRepository.Verify(
            x => x.SearchAsync(
                query.SearchTerm,
                It.IsAny<int>(),
                It.IsAny<int>(),
                query.Category,
                query.IsFreeOnly,
                startDate,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_Page2_CalculatesCorrectOffsetAndLimit()
    {
        // Arrange
        var query = new SearchEventsQuery("event", Page: 2, PageSize: 10);

        _mockRepository
            .Setup(x => x.SearchAsync(
                query.SearchTerm,
                10,  // limit = pageSize
                10,  // offset = (page - 1) * pageSize = (2 - 1) * 10 = 10
                query.Category,
                query.IsFreeOnly,
                query.StartDateFrom,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Array.Empty<Event>(), 0));

        _mockMapper
            .Setup(x => x.Map<IEnumerable<EventSearchResultDto>>(It.IsAny<IEnumerable<Event>>()))
            .Returns(Array.Empty<EventSearchResultDto>());

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        _mockRepository.Verify(
            x => x.SearchAsync(
                query.SearchTerm,
                10,  // limit
                10,  // offset
                query.Category,
                query.IsFreeOnly,
                query.StartDateFrom,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_MultipleFilters_PassesAllFiltersToRepository()
    {
        // Arrange
        var startDate = DateTime.UtcNow.AddDays(30);
        var query = new SearchEventsQuery(
            "networking",
            Page: 1,
            PageSize: 20,
            Category: EventCategory.Business,
            IsFreeOnly: false,
            StartDateFrom: startDate);

        _mockRepository
            .Setup(x => x.SearchAsync(
                query.SearchTerm,
                It.IsAny<int>(),
                It.IsAny<int>(),
                EventCategory.Business,
                false,
                startDate,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Array.Empty<Event>(), 0));

        _mockMapper
            .Setup(x => x.Map<IEnumerable<EventSearchResultDto>>(It.IsAny<IEnumerable<Event>>()))
            .Returns(Array.Empty<EventSearchResultDto>());

        // Act
        await _handler.Handle(query, CancellationToken.None);

        // Assert
        _mockRepository.Verify(
            x => x.SearchAsync(
                query.SearchTerm,
                It.IsAny<int>(),
                It.IsAny<int>(),
                EventCategory.Business,
                false,
                startDate,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_TotalCountExceedsPageSize_CalculatesCorrectTotalPages()
    {
        // Arrange
        var query = new SearchEventsQuery("event", Page: 1, PageSize: 10);
        var events = CreateSampleEvents().Take(10).ToList();
        var totalCount = 47;  // Should result in 5 pages (47 / 10 = 4.7 → ceil = 5)

        _mockRepository
            .Setup(x => x.SearchAsync(
                query.SearchTerm,
                It.IsAny<int>(),
                It.IsAny<int>(),
                query.Category,
                query.IsFreeOnly,
                query.StartDateFrom,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((events, totalCount));

        var dtos = events.Select(e => new EventSearchResultDto { Id = e.Id }).ToList();
        _mockMapper
            .Setup(x => x.Map<IEnumerable<EventSearchResultDto>>(events))
            .Returns(dtos);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.TotalPages.Should().Be(5);
        result.Value.HasNextPage.Should().BeTrue();
        result.Value.HasPreviousPage.Should().BeFalse();
    }

    private List<Event> CreateSampleEvents()
    {
        var events = new List<Event>();
        var organizerId = Guid.NewGuid();

        for (int i = 1; i <= 5; i++)
        {
            var address = Address.Create(
                $"{i * 100} Main St",
                "New York",
                "NY",
                "10001",
                "USA").Value;
            var coordinates = GeoCoordinate.Create(40.7128m + i, -74.0060m + i).Value;
            var location = EventLocation.Create(address, coordinates).Value;

            var title = EventTitle.Create($"Test Event {i}").Value;
            var description = EventDescription.Create($"Description for event {i}").Value;
            var ticketPrice = i % 2 == 0 ? null : Money.Create(50, LankaConnect.Domain.Shared.Enums.Currency.USD).Value;

            var @event = Event.Create(
                title,
                description,
                DateTime.UtcNow.AddDays(i * 7),
                DateTime.UtcNow.AddDays(i * 7 + 1),
                organizerId,
                100, // capacity
                location,
                EventCategory.Cultural,
                ticketPrice);

            events.Add(@event.Value);
        }

        return events;
    }
}
