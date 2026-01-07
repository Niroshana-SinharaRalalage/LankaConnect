using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Communications.Commands.ConfirmNewsletterSubscription;
using LankaConnect.Domain.Communications.Entities;
using LankaConnect.Domain.Shared.ValueObjects;
using LankaConnect.Domain.Common;

namespace LankaConnect.Application.Tests.Communications.Commands;

/// <summary>
/// Tests for ConfirmNewsletterSubscriptionCommandHandler
/// </summary>
public class ConfirmNewsletterSubscriptionCommandHandlerTests
{
    private readonly Mock<INewsletterSubscriberRepository> _mockRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<ILogger<ConfirmNewsletterSubscriptionCommandHandler>> _mockLogger;
    private readonly ConfirmNewsletterSubscriptionCommandHandler _handler;

    public ConfirmNewsletterSubscriptionCommandHandlerTests()
    {
        _mockRepository = new Mock<INewsletterSubscriberRepository>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockLogger = new Mock<ILogger<ConfirmNewsletterSubscriptionCommandHandler>>();

        _handler = new ConfirmNewsletterSubscriptionCommandHandler(
            _mockRepository.Object,
            _mockUnitOfWork.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task Handle_ValidToken_ConfirmsSubscription()
    {
        // Arrange
        var subscriber = NewsletterSubscriber.Create(
            Email.Create("test@example.com").Value,
            Guid.NewGuid(),
            false).Value;

        var token = subscriber.ConfirmationToken!;
        var command = new ConfirmNewsletterSubscriptionCommand(token);

        _mockRepository
            .Setup(r => r.GetByConfirmationTokenAsync(token, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscriber);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Email.Should().Be("test@example.com");
        result.Value.ConfirmedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));

        subscriber.IsConfirmed.Should().BeTrue();
        subscriber.ConfirmedAt.Should().NotBeNull();

        _mockUnitOfWork.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_InvalidToken_ReturnsFailure()
    {
        // Arrange
        var command = new ConfirmNewsletterSubscriptionCommand("invalid-token");

        _mockRepository
            .Setup(r => r.GetByConfirmationTokenAsync("invalid-token", It.IsAny<CancellationToken>()))
            .ReturnsAsync((NewsletterSubscriber?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Invalid or expired");

        _mockUnitOfWork.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_EmptyToken_ReturnsFailure()
    {
        // Arrange
        var command = new ConfirmNewsletterSubscriptionCommand("");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("required");

        _mockRepository.Verify(r => r.GetByConfirmationTokenAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_AlreadyConfirmed_ReturnsFailure()
    {
        // Arrange
        var subscriber = NewsletterSubscriber.Create(
            Email.Create("test@example.com").Value,
            Guid.NewGuid(),
            false).Value;

        var token = subscriber.ConfirmationToken!;
        subscriber.Confirm(token); // Already confirmed

        var command = new ConfirmNewsletterSubscriptionCommand(token);

        _mockRepository
            .Setup(r => r.GetByConfirmationTokenAsync(token, It.IsAny<CancellationToken>()))
            .ReturnsAsync(subscriber);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("already confirmed");

        // Should not commit if already confirmed
        _mockUnitOfWork.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
