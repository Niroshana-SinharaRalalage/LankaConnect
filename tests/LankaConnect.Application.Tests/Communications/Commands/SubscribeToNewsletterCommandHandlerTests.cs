using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Communications.Commands.SubscribeToNewsletter;
using LankaConnect.Domain.Communications.Entities;
using LankaConnect.Domain.Shared.ValueObjects;
using LankaConnect.Domain.Common;

namespace LankaConnect.Application.Tests.Communications.Commands;

/// <summary>
/// Tests for SubscribeToNewsletterCommandHandler
/// </summary>
public class SubscribeToNewsletterCommandHandlerTests
{
    private readonly Mock<INewsletterSubscriberRepository> _mockRepository;
    private readonly Mock<IUnitOfWork> _mockUnitOfWork;
    private readonly Mock<IEmailService> _mockEmailService;
    private readonly Mock<ILogger<SubscribeToNewsletterCommandHandler>> _mockLogger;
    private readonly SubscribeToNewsletterCommandHandler _handler;

    public SubscribeToNewsletterCommandHandlerTests()
    {
        _mockRepository = new Mock<INewsletterSubscriberRepository>();
        _mockUnitOfWork = new Mock<IUnitOfWork>();
        _mockEmailService = new Mock<IEmailService>();
        _mockLogger = new Mock<ILogger<SubscribeToNewsletterCommandHandler>>();

        _handler = new SubscribeToNewsletterCommandHandler(
            _mockRepository.Object,
            _mockUnitOfWork.Object,
            _mockEmailService.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task Handle_ValidEmail_CreatesSubscriberAndSendsConfirmation()
    {
        // Arrange
        var email = "test@example.com";
        var metroAreaIds = new List<Guid> { Guid.NewGuid() };
        var command = new SubscribeToNewsletterCommand(email, metroAreaIds);

        _mockRepository
            .Setup(r => r.GetByEmailAsync(email, It.IsAny<CancellationToken>()))
            .ReturnsAsync((NewsletterSubscriber?)null);

        _mockEmailService
            .Setup(s => s.SendTemplatedEmailAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Dictionary<string, object>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Email.Should().Be(email);
        result.Value.MetroAreaIds.Should().NotBeNull();
        result.Value.MetroAreaIds.Should().HaveCount(1);
        result.Value.IsActive.Should().BeTrue();
        result.Value.IsConfirmed.Should().BeFalse();

        _mockRepository.Verify(r => r.AddAsync(It.IsAny<NewsletterSubscriber>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockEmailService.Verify(s => s.SendTemplatedEmailAsync(
            "newsletter-confirmation",
            email,
            It.IsAny<Dictionary<string, object>>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_InvalidEmail_ReturnsFailure()
    {
        // Arrange
        var invalidEmail = "not-an-email";
        var command = new SubscribeToNewsletterCommand(invalidEmail, null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("email");

        _mockRepository.Verify(r => r.AddAsync(It.IsAny<NewsletterSubscriber>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ExistingActiveSubscriber_ReturnsAlreadySubscribed()
    {
        // Arrange
        var email = "existing@example.com";
        var metroAreaIds = new List<Guid> { Guid.NewGuid() };
        var command = new SubscribeToNewsletterCommand(email, metroAreaIds);

        var existingSubscriber = NewsletterSubscriber.Create(
            Email.Create(email).Value,
            metroAreaIds.FirstOrDefault(),
            false).Value;

        _mockRepository
            .Setup(r => r.GetByEmailAsync(email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingSubscriber);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("already subscribed");

        _mockRepository.Verify(r => r.AddAsync(It.IsAny<NewsletterSubscriber>(), It.IsAny<CancellationToken>()), Times.Never);
        _mockUnitOfWork.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ExistingInactiveSubscriber_CreatesNewSubscriptionAndSendsConfirmation()
    {
        // Arrange
        var email = "inactive@example.com";
        var metroAreaIds = new List<Guid> { Guid.NewGuid() };
        var command = new SubscribeToNewsletterCommand(email, metroAreaIds);

        var existingSubscriber = NewsletterSubscriber.Create(
            Email.Create(email).Value,
            metroAreaIds.FirstOrDefault(),
            false).Value;
        existingSubscriber.Unsubscribe(existingSubscriber.UnsubscribeToken!); // Make inactive

        _mockRepository
            .Setup(r => r.GetByEmailAsync(email, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingSubscriber);

        _mockEmailService
            .Setup(s => s.SendTemplatedEmailAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Dictionary<string, object>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.IsActive.Should().BeTrue();
        result.Value.MetroAreaIds.Should().NotBeNull();
        result.Value.MetroAreaIds.Should().HaveCount(1);

        // Should remove old and add new
        _mockRepository.Verify(r => r.Remove(It.IsAny<NewsletterSubscriber>()), Times.Once);
        _mockRepository.Verify(r => r.AddAsync(It.IsAny<NewsletterSubscriber>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockEmailService.Verify(s => s.SendTemplatedEmailAsync(
            "newsletter-confirmation",
            email,
            It.IsAny<Dictionary<string, object>>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ReceiveAllLocations_CreatesSubscriberWithNoMetroArea()
    {
        // Arrange
        var email = "all@example.com";
        var command = new SubscribeToNewsletterCommand(email, null, ReceiveAllLocations: true);

        _mockRepository
            .Setup(r => r.GetByEmailAsync(email, It.IsAny<CancellationToken>()))
            .ReturnsAsync((NewsletterSubscriber?)null);

        _mockEmailService
            .Setup(s => s.SendTemplatedEmailAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Dictionary<string, object>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Success());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.MetroAreaId.Should().BeNull();
        result.Value.ReceiveAllLocations.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_EmailServiceFails_ReturnsFailure()
    {
        // Arrange
        var email = "test@example.com";
        var metroAreaIds = new List<Guid> { Guid.NewGuid() };
        var command = new SubscribeToNewsletterCommand(email, metroAreaIds);

        _mockRepository
            .Setup(r => r.GetByEmailAsync(email, It.IsAny<CancellationToken>()))
            .ReturnsAsync((NewsletterSubscriber?)null);

        _mockEmailService
            .Setup(s => s.SendTemplatedEmailAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<Dictionary<string, object>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result.Failure("Email service error"));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("confirmation email");

        // Subscriber should still be created even if email fails
        _mockRepository.Verify(r => r.AddAsync(It.IsAny<NewsletterSubscriber>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockUnitOfWork.Verify(u => u.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
