using LankaConnect.Modules.Forms.Contracts;
using LankaConnect.Application.Common;
using LankaConnect.Modules.Forms.Domain;
using LankaConnect.Modules.Forms.Domain.Entities;
using LankaConnect.Modules.Forms.Domain.Enums;
using LankaConnect.Modules.Forms.Domain.DomainEvents;
using LankaConnect.Modules.Forms.Domain.Repositories;
using LankaConnect.Application.Events.EventHandlers;
using LankaConnect.Application.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.DomainEvents;
using LankaConnect.Domain.Events.Entities;
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Domain.Events.Repositories;
using LankaConnect.Domain.Events.ValueObjects;
using LankaConnect.Domain.Shared.ValueObjects;
using LankaConnect.Domain.Users;
using LankaConnect.Shared.Email.Services;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LankaConnect.Application.Tests.Events.EventHandlers;

/// <summary>
/// Phase 7C.2b (Chunk 0): adds a single diagnostic <see cref="LogLevel.Information"/>
/// entry to <see cref="CommitmentCancelledEmailHandler"/> so that every cancellation
/// email send records which event was resolved and what location was projected into
/// the rendered email body. This lets us disambiguate Symptom 2 of the 2026-04-22
/// inbox report (wrong event's address apparently appearing in a cancellation email)
/// without needing another live inbox round-trip — the log is deterministic and
/// queryable in Azure container logs after the fact.
/// </summary>
public class CommitmentCancelledEmailHandlerDiagnosticLogTests
{
    private readonly Mock<IServiceScopeFactory> _scopeFactory;
    private readonly Mock<IUserRepository> _userRepository;
    private readonly Mock<IEventRepository> _eventRepository;
    private readonly Mock<IFormQueries> _eventFormRepository;
    private readonly Mock<IEmailUrlHelper> _emailUrlHelper;
    private readonly Mock<ILogger<CommitmentCancelledEmailHandler>> _logger;
    private readonly CommitmentCancelledEmailHandler _handler;

    public CommitmentCancelledEmailHandlerDiagnosticLogTests()
    {
        _scopeFactory = new Mock<IServiceScopeFactory>();
        _userRepository = new Mock<IUserRepository>();
        _eventRepository = new Mock<IEventRepository>();
        _eventFormRepository = new Mock<IFormQueries>();
        _emailUrlHelper = new Mock<IEmailUrlHelper>();
        _logger = new Mock<ILogger<CommitmentCancelledEmailHandler>>();

        _emailUrlHelper
            .Setup(x => x.BuildEventDetailsUrl(It.IsAny<Guid>()))
            .Returns((Guid id) => $"https://lankaconnect.com/events/{id}");

        // Minimal scope factory so the fire-and-forget Task.Run inside the handler
        // does not NRE on scope.ServiceProvider.GetRequiredService<ITypedEmailService>().
        var typedEmailService = new Mock<ITypedEmailService>();
        typedEmailService
            .Setup(x => x.SendEmailAsync(It.IsAny<Shared.Email.Contracts.IEmailParameters>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(TypedEmailSendResult.Ok("test-correlation-id", 100));
        var serviceProvider = new Mock<IServiceProvider>();
        serviceProvider
            .Setup(sp => sp.GetService(typeof(ITypedEmailService)))
            .Returns(typedEmailService.Object);
        var scope = new Mock<IServiceScope>();
        scope.Setup(s => s.ServiceProvider).Returns(serviceProvider.Object);
        _scopeFactory.Setup(f => f.CreateScope()).Returns(scope.Object);

        _eventFormRepository
            .Setup(x => x.GetByOwnerAsync(FormOwnerEntityTypeDto.Event, It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<FormSummaryDto>());

        _handler = new CommitmentCancelledEmailHandler(
            _scopeFactory.Object,
            _userRepository.Object,
            _eventRepository.Object,
            _eventFormRepository.Object,
            _emailUrlHelper.Object,
            _logger.Object);
    }

    [Fact]
    public async Task Handle_WhenEventAndUserResolve_EmitsDiagnosticLog_WithResolvedEventAndLocationFields()
    {
        // Arrange — a commitment cancellation for a user against a specific event.
        // The diagnostic log must record the resolved eventId, event title, the projected
        // decomposed-location fields (HasLocationName / LocationAddress / HasSecondaryLocation),
        // and the caller identity (userId / commitmentId / signUpListId) so an operator can
        // grep the log and confirm which event's data actually ended up in the rendered email.
        var eventId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var commitmentId = Guid.NewGuid();
        var signUpListId = Guid.NewGuid();
        var signUpItemId = Guid.NewGuid();

        var user = CreateTestUser(userId, "user@example.com", "Test", "User");
        var @event = CreateTestEvent(eventId, "Diagnostic Log Test Event");

        _userRepository
            .Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _eventRepository
            .Setup(x => x.GetEventBySignUpListIdAsync(signUpListId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(@event);

        var domainEvent = new CommitmentCancelledEvent(
            SignUpItemId: signUpItemId,
            CommitmentId: commitmentId,
            UserId: userId,
            SignUpListId: signUpListId,
            ItemDescription: "Rice Tray",
            CancelledPhysicalQuantity: 1,
            CancelledSlotsClaimed: null);
        var notification = new DomainEventNotification<CommitmentCancelledEvent>(domainEvent);

        // Act
        await _handler.Handle(notification, CancellationToken.None);

        // Assert — diagnostic log fires exactly once on the synchronous path (pre-fire-and-forget).
        // Verify the structured log message mentions the diagnostic marker AND the resolved eventId
        // (which is the single most important disambiguation field for Symptom 2).
        _logger.Verify(
            l => l.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, _) =>
                    state.ToString()!.Contains("CommitmentCancelled DIAGNOSTIC", StringComparison.Ordinal)
                    && state.ToString()!.Contains(eventId.ToString(), StringComparison.OrdinalIgnoreCase)),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce,
            "the diagnostic log must be emitted on the synchronous path so we can disambiguate which event the handler resolved for a given cancellation");
    }

    [Fact]
    public async Task Handle_WhenEventAndUserResolve_DiagnosticLog_IncludesLocationAndUserFields()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var signUpListId = Guid.NewGuid();

        var user = CreateTestUser(userId, "user@example.com", "Test", "User");
        var @event = CreateTestEvent(eventId, "Another Diagnostic Event");

        _userRepository.Setup(x => x.GetByIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _eventRepository.Setup(x => x.GetEventBySignUpListIdAsync(signUpListId, It.IsAny<CancellationToken>())).ReturnsAsync(@event);

        var domainEvent = new CommitmentCancelledEvent(
            SignUpItemId: Guid.NewGuid(),
            CommitmentId: Guid.NewGuid(),
            UserId: userId,
            SignUpListId: signUpListId,
            ItemDescription: "Plates",
            CancelledPhysicalQuantity: null,
            CancelledSlotsClaimed: 2);
        var notification = new DomainEventNotification<CommitmentCancelledEvent>(domainEvent);

        // Act
        await _handler.Handle(notification, CancellationToken.None);

        // Assert — structured log message template must include every diagnostic field.
        // We check for the LITERAL placeholder names in the message template so any future
        // refactor that drops a field (e.g. removes {HasSecondaryLocation}) breaks this test.
        _logger.Verify(
            l => l.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, _) =>
                    FormattedLogValuesContainsKeys(state,
                        "EventId",
                        "EventTitle",
                        "HasLocationName",
                        "LocationName",
                        "LocationAddress",
                        "HasSecondaryLocation",
                        "SecondaryLocationName",
                        "UserId",
                        "CommitmentId",
                        "SignUpListId")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce,
            "every field required for Symptom 2 disambiguation must be in the structured log's key set");
    }

    /// <summary>
    /// Microsoft.Extensions.Logging passes its state to logger.Log as a
    /// <c>FormattedLogValues</c> (an <see cref="IReadOnlyList{T}"/> of
    /// <see cref="KeyValuePair{TKey,TValue}"/>). This helper checks each required
    /// structured-log key is present in the state's key set.
    /// </summary>
    private static bool FormattedLogValuesContainsKeys(object state, params string[] requiredKeys)
    {
        if (state is not IReadOnlyList<KeyValuePair<string, object?>> kvps)
            return false;

        var keys = kvps.Select(k => k.Key).ToHashSet(StringComparer.Ordinal);
        return requiredKeys.All(keys.Contains);
    }

    private static Event CreateTestEvent(Guid eventId, string title)
    {
        var eventObj = Event.Create(
            EventTitle.Create(title).Value,
            EventDescription.Create("Diagnostic-log test event description").Value,
            DateTime.UtcNow.AddDays(7),
            DateTime.UtcNow.AddDays(7).AddHours(2),
            Guid.NewGuid(),
            100,
            null, // online event — keeps the test minimal; full location projection is tested elsewhere
            EventCategory.Cultural).Value;

        var idProperty = typeof(LegacyBaseEntity).GetProperty("Id");
        idProperty?.SetValue(eventObj, eventId);
        return eventObj;
    }

    private static User CreateTestUser(Guid userId, string email, string firstName, string lastName)
    {
        var userEmail = Email.Create(email).Value;
        var user = User.Create(userEmail, firstName, lastName).Value;
        var idProperty = typeof(LegacyBaseEntity).GetProperty("Id");
        idProperty?.SetValue(user, userId);
        return user;
    }
}
