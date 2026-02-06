using FluentAssertions;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Communications.Entities;
using LankaConnect.Domain.Communications.DomainEvents;
using LankaConnect.Domain.Communications.ValueObjects;
using LankaConnect.Domain.Shared.ValueObjects;
using Xunit;

namespace LankaConnect.Application.Tests.Domain.Communications.Entities;

public class NewsletterSubscriberTests
{
    [Fact]
    public void Create_WithValidEmailAndMetroArea_ShouldCreateSubscriber()
    {
        // Arrange
        var emailResult = Email.Create("test@example.com");
        var metroAreaId = Guid.NewGuid();

        // Act
        var result = NewsletterSubscriber.Create(
            emailResult.Value,
            metroAreaId,
            receiveAllLocations: false
        );

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Email.Value.Should().Be("test@example.com");
        result.Value.MetroAreaId.Should().Be(metroAreaId);
        result.Value.ReceiveAllLocations.Should().BeFalse();
        result.Value.IsActive.Should().BeTrue();
        result.Value.IsConfirmed.Should().BeFalse();
        result.Value.UnsubscribeToken.Should().NotBeNullOrEmpty();
        result.Value.ConfirmationToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Create_WithReceiveAllLocations_ShouldHaveNullMetroAreaId()
    {
        // Arrange
        var emailResult = Email.Create("test@example.com");

        // Act
        var result = NewsletterSubscriber.Create(
            emailResult.Value,
            metroAreaId: null,
            receiveAllLocations: true
        );

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.MetroAreaId.Should().BeNull();
        result.Value.ReceiveAllLocations.Should().BeTrue();
    }

    [Fact]
    public void Create_WithoutMetroAreaAndNotReceiveAll_ShouldFail()
    {
        // Arrange
        var emailResult = Email.Create("test@example.com");

        // Act
        var result = NewsletterSubscriber.Create(
            emailResult.Value,
            metroAreaId: null,
            receiveAllLocations: false
        );

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("metro area");
    }

    [Fact]
    public void Confirm_WithValidToken_ShouldConfirmSubscription()
    {
        // Arrange
        var emailResult = Email.Create("test@example.com");
        var subscriber = NewsletterSubscriber.Create(
            emailResult.Value,
            Guid.NewGuid(),
            false
        ).Value;
        var confirmationToken = subscriber.ConfirmationToken;

        // Act
        var result = subscriber.Confirm(confirmationToken!);

        // Assert
        result.IsSuccess.Should().BeTrue();
        subscriber.IsConfirmed.Should().BeTrue();
        subscriber.ConfirmedAt.Should().NotBeNull();
        subscriber.ConfirmationToken.Should().BeNull();
    }

    [Fact]
    public void Confirm_WithInvalidToken_ShouldFail()
    {
        // Arrange
        var emailResult = Email.Create("test@example.com");
        var subscriber = NewsletterSubscriber.Create(
            emailResult.Value,
            Guid.NewGuid(),
            false
        ).Value;

        // Act
        var result = subscriber.Confirm("invalid-token");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Invalid confirmation token");
        subscriber.IsConfirmed.Should().BeFalse();
    }

    [Fact]
    public void Confirm_WhenAlreadyConfirmed_ShouldFail()
    {
        // Arrange
        var emailResult = Email.Create("test@example.com");
        var subscriber = NewsletterSubscriber.Create(
            emailResult.Value,
            Guid.NewGuid(),
            false
        ).Value;
        var confirmationToken = subscriber.ConfirmationToken;
        subscriber.Confirm(confirmationToken!);

        // Act
        var result = subscriber.Confirm(confirmationToken!);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("already confirmed");
    }

    [Fact]
    public void Unsubscribe_WithValidToken_ShouldDeactivateSubscription()
    {
        // Arrange
        var emailResult = Email.Create("test@example.com");
        var subscriber = NewsletterSubscriber.Create(
            emailResult.Value,
            Guid.NewGuid(),
            false
        ).Value;
        var unsubscribeToken = subscriber.UnsubscribeToken;

        // Act
        var result = subscriber.Unsubscribe(unsubscribeToken!);

        // Assert
        result.IsSuccess.Should().BeTrue();
        subscriber.IsActive.Should().BeFalse();
        subscriber.UnsubscribedAt.Should().NotBeNull();
    }

    [Fact]
    public void Unsubscribe_WithInvalidToken_ShouldFail()
    {
        // Arrange
        var emailResult = Email.Create("test@example.com");
        var subscriber = NewsletterSubscriber.Create(
            emailResult.Value,
            Guid.NewGuid(),
            false
        ).Value;

        // Act
        var result = subscriber.Unsubscribe("invalid-token");

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Invalid unsubscribe token");
        subscriber.IsActive.Should().BeTrue();
    }

    [Fact]
    public void ResendConfirmation_ShouldGenerateNewToken()
    {
        // Arrange
        var emailResult = Email.Create("test@example.com");
        var subscriber = NewsletterSubscriber.Create(
            emailResult.Value,
            Guid.NewGuid(),
            false
        ).Value;
        var oldToken = subscriber.ConfirmationToken;

        // Act
        var result = subscriber.ResendConfirmation();

        // Assert
        result.IsSuccess.Should().BeTrue();
        subscriber.ConfirmationToken.Should().NotBeNullOrEmpty();
        subscriber.ConfirmationToken.Should().NotBe(oldToken);
        subscriber.ConfirmationSentAt.Should().NotBeNull();
    }

    [Fact]
    public void ResendConfirmation_WhenAlreadyConfirmed_ShouldFail()
    {
        // Arrange
        var emailResult = Email.Create("test@example.com");
        var subscriber = NewsletterSubscriber.Create(
            emailResult.Value,
            Guid.NewGuid(),
            false
        ).Value;
        subscriber.Confirm(subscriber.ConfirmationToken!);

        // Act
        var result = subscriber.ResendConfirmation();

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("already confirmed");
    }

    [Fact]
    public void Create_ShouldRaiseNewsletterSubscriptionCreatedEvent()
    {
        // Arrange
        var emailResult = Email.Create("test@example.com");
        var metroAreaId = Guid.NewGuid();

        // Act
        var result = NewsletterSubscriber.Create(
            emailResult.Value,
            metroAreaId,
            false
        );

        // Assert
        var subscriber = result.Value;
        subscriber.DomainEvents.Should().ContainSingle();
        subscriber.DomainEvents.First().Should().BeOfType<NewsletterSubscriptionCreatedEvent>();
    }

    [Fact]
    public void Confirm_ShouldRaiseNewsletterSubscriptionConfirmedEvent()
    {
        // Arrange
        var emailResult = Email.Create("test@example.com");
        var subscriber = NewsletterSubscriber.Create(
            emailResult.Value,
            Guid.NewGuid(),
            false
        ).Value;
        subscriber.ClearDomainEvents();

        // Act
        subscriber.Confirm(subscriber.ConfirmationToken!);

        // Assert
        subscriber.DomainEvents.Should().ContainSingle();
        subscriber.DomainEvents.First().Should().BeOfType<NewsletterSubscriptionConfirmedEvent>();
    }

    [Fact]
    public void Unsubscribe_ShouldRaiseNewsletterSubscriptionCancelledEvent()
    {
        // Arrange
        var emailResult = Email.Create("test@example.com");
        var subscriber = NewsletterSubscriber.Create(
            emailResult.Value,
            Guid.NewGuid(),
            false
        ).Value;
        subscriber.ClearDomainEvents();

        // Act
        subscriber.Unsubscribe(subscriber.UnsubscribeToken!);

        // Assert
        subscriber.DomainEvents.Should().ContainSingle();
        subscriber.DomainEvents.First().Should().BeOfType<NewsletterSubscriptionCancelledEvent>();
    }
}
