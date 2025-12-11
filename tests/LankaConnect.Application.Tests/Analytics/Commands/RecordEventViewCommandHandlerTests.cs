using FluentAssertions;
using Moq;
using Xunit;
using LankaConnect.Application.Analytics.Commands.RecordEventView;
using LankaConnect.Domain.Analytics;
using LankaConnect.Domain.Common;

namespace LankaConnect.Application.Tests.Analytics.Commands;

/// <summary>
/// TDD RED Phase: RecordEventViewCommandHandler tests
/// Tests for analytics view recording functionality
/// </summary>
public class RecordEventViewCommandHandlerTests
{
    private readonly Mock<IEventAnalyticsRepository> _analyticsRepository;
    private readonly Mock<IEventViewRecordRepository> _viewRecordRepository;
    private readonly Mock<IUnitOfWork> _unitOfWork;
    private readonly RecordEventViewCommandHandler _handler;

    public RecordEventViewCommandHandlerTests()
    {
        _analyticsRepository = new Mock<IEventAnalyticsRepository>();
        _viewRecordRepository = new Mock<IEventViewRecordRepository>();
        _unitOfWork = new Mock<IUnitOfWork>();
        _handler = new RecordEventViewCommandHandler(
            _analyticsRepository.Object,
            _viewRecordRepository.Object,
            _unitOfWork.Object
        );
    }

    #region Happy Path Tests

    [Fact]
    public async Task Handle_WithValidAuthenticatedView_ShouldCreateAnalyticsAndRecordView()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var command = new RecordEventViewCommand(eventId, userId, "192.168.1.1", "Mozilla/5.0");

        _analyticsRepository.Setup(x => x.GetByEventIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((EventAnalytics?)null); // No existing analytics

        _analyticsRepository.Setup(x => x.ShouldCountViewAsync(
                eventId, userId, "192.168.1.1", It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true); // Should count this view

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        _analyticsRepository.Verify(x => x.AddAsync(
            It.Is<EventAnalytics>(a => a.EventId == eventId && a.TotalViews == 1),
            It.IsAny<CancellationToken>()), Times.Once);

        _viewRecordRepository.Verify(x => x.AddAsync(
            It.Is<EventViewRecord>(v =>
                v.EventId == eventId &&
                v.UserId == userId &&
                v.IpAddress == "192.168.1.1"),
            It.IsAny<CancellationToken>()), Times.Once);

        _unitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithValidAnonymousView_ShouldRecordViewWithoutUserId()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var command = new RecordEventViewCommand(eventId, null, "192.168.1.1", "Mozilla/5.0");

        _analyticsRepository.Setup(x => x.GetByEventIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((EventAnalytics?)null);

        _analyticsRepository.Setup(x => x.ShouldCountViewAsync(
                eventId, null, "192.168.1.1", It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        _viewRecordRepository.Verify(x => x.AddAsync(
            It.Is<EventViewRecord>(v =>
                v.EventId == eventId &&
                v.UserId == null &&
                v.IpAddress == "192.168.1.1"),
            It.IsAny<CancellationToken>()), Times.Once);

        _unitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithExistingAnalytics_ShouldIncrementViewCount()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var command = new RecordEventViewCommand(eventId, null, "192.168.1.1");

        var existingAnalytics = EventAnalytics.Create(eventId);
        existingAnalytics.RecordView(null, "192.168.1.100"); // Existing view

        _analyticsRepository.Setup(x => x.GetByEventIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingAnalytics);

        _analyticsRepository.Setup(x => x.ShouldCountViewAsync(
                eventId, null, "192.168.1.1", It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        existingAnalytics.TotalViews.Should().Be(2); // Was 1, now 2

        _analyticsRepository.Verify(x => x.Update(existingAnalytics), Times.Once);
        _unitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Deduplication Tests

    [Fact]
    public async Task Handle_WithDuplicateViewInWindow_ShouldNotCountView()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var command = new RecordEventViewCommand(eventId, userId, "192.168.1.1");

        var existingAnalytics = EventAnalytics.Create(eventId);
        existingAnalytics.RecordView(userId, "192.168.1.1"); // Existing view

        _analyticsRepository.Setup(x => x.GetByEventIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingAnalytics);

        _analyticsRepository.Setup(x => x.ShouldCountViewAsync(
                eventId, userId, "192.168.1.1", It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false); // Duplicate within window

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        existingAnalytics.TotalViews.Should().Be(1); // Should NOT increment

        // View record should still be created for tracking
        _viewRecordRepository.Verify(x => x.AddAsync(
            It.IsAny<EventViewRecord>(),
            It.IsAny<CancellationToken>()), Times.Once);

        _analyticsRepository.Verify(x => x.Update(It.IsAny<EventAnalytics>()), Times.Never);
        _unitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Validation Tests

    [Fact]
    public async Task Handle_WithEmptyEventId_ShouldReturnFailure()
    {
        // Arrange
        var command = new RecordEventViewCommand(Guid.Empty, null, "192.168.1.1");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Event ID");

        _analyticsRepository.Verify(x => x.AddAsync(It.IsAny<EventAnalytics>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithNullIpAddress_ShouldReturnFailure()
    {
        // Arrange
        var command = new RecordEventViewCommand(Guid.NewGuid(), null, null!);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("IP address");

        _analyticsRepository.Verify(x => x.AddAsync(It.IsAny<EventAnalytics>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithEmptyIpAddress_ShouldReturnFailure()
    {
        // Arrange
        var command = new RecordEventViewCommand(Guid.NewGuid(), null, "");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("IP address");

        _analyticsRepository.Verify(x => x.AddAsync(It.IsAny<EventAnalytics>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWork.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region Domain Event Tests

    [Fact]
    public async Task Handle_WhenRecordingView_ShouldRaiseDomainEvent()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var command = new RecordEventViewCommand(eventId, userId, "192.168.1.1");

        var analytics = EventAnalytics.Create(eventId);

        _analyticsRepository.Setup(x => x.GetByEventIdAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((EventAnalytics?)null);

        _analyticsRepository.Setup(x => x.ShouldCountViewAsync(
                eventId, userId, "192.168.1.1", It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        // Verify analytics was created and has domain event
        _analyticsRepository.Verify(x => x.AddAsync(
            It.Is<EventAnalytics>(a => a.DomainEvents.Any()),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion
}
