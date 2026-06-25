using LankaConnect.Application.Common;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Communications.Enums;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Users.DomainEvents; // W4.7.a: user-aggregate events moved here
using LankaConnect.Domain.Events.DomainEvents;
using LankaConnect.Shared.Email.Helpers;
using LankaConnect.Shared.WhatsApp.Contracts;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LankaConnect.Application.Events.EventHandlers;

/// <summary>
/// Phase 7A.3: Sends WhatsApp notification for anonymous event registrations.
/// Parallel to AnonymousRegistrationConfirmedEventHandler (email). Email handler is UNTOUCHED.
/// Uses SendTemplateMessageToPhoneAsync since anonymous users have no user account.
/// NOTE: Anonymous users don't have WhatsApp preferences, so this handler can only send
/// if we have their phone number from the registration form. For Phase 7A.3, this logs
/// that phone-based sending would occur if phone data is available.
/// </summary>
public class AnonymousRegistrationWhatsAppHandler : INotificationHandler<DomainEventNotification<AnonymousRegistrationConfirmedEvent>>
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AnonymousRegistrationWhatsAppHandler> _logger;

    public AnonymousRegistrationWhatsAppHandler(
        IServiceScopeFactory scopeFactory,
        ILogger<AnonymousRegistrationWhatsAppHandler> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public Task Handle(DomainEventNotification<AnonymousRegistrationConfirmedEvent> notification, CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;
        var eventId = domainEvent.EventId;
        var attendeeEmail = domainEvent.AttendeeEmail;
        var quantity = domainEvent.Quantity;

        _logger.LogInformation(
            "[Phase 7A] WhatsApp AnonymousRegistration START: EventId={EventId}, Email={Email}, Quantity={Quantity}",
            eventId, attendeeEmail, quantity);

        _ = Task.Run(async () =>
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var eventRepository = scope.ServiceProvider.GetRequiredService<IEventRepository>();
                var registrationRepository = scope.ServiceProvider.GetRequiredService<IRegistrationRepository>();

                var @event = await eventRepository.GetByIdAsync(eventId, CancellationToken.None);
                if (@event == null)
                {
                    _logger.LogWarning("[Phase 7A] WhatsApp AnonymousRegistration: Event not found - EventId={EventId}", eventId);
                    return;
                }

                // Phase 7A.6D: Get registration and check WhatsApp opt-in
                var registration = await registrationRepository.GetAnonymousByEventAndEmailAsync(
                    eventId, attendeeEmail, CancellationToken.None);

                // Check if user opted in to WhatsApp notifications
                var whatsAppOptedIn = registration?.Contact?.WhatsAppOptedIn ?? false;
                if (!whatsAppOptedIn)
                {
                    _logger.LogInformation(
                        "[Phase 7A] WhatsApp AnonymousRegistration SKIPPED: Not opted in - EventId={EventId}, Email={Email}",
                        eventId, attendeeEmail);
                    return;
                }

                // Use WhatsApp-specific phone number (not general contact phone)
                var phoneNumber = registration?.Contact?.WhatsAppPhoneNumber;
                if (string.IsNullOrWhiteSpace(phoneNumber))
                {
                    _logger.LogInformation(
                        "[Phase 7A] WhatsApp AnonymousRegistration SKIPPED: No WhatsApp phone number - EventId={EventId}, Email={Email}",
                        eventId, attendeeEmail);
                    return;
                }

                var whatsAppService = scope.ServiceProvider.GetRequiredService<IWhatsAppService>();

                var location = @event.Location?.Address != null
                    ? $"{@event.Location.Address.Street}, {@event.Location.Address.City}".Trim(',', ' ')
                    : "Online Event";

                var attendeeName = registration?.Attendees.FirstOrDefault()?.Name ?? "Guest";

                var parameters = new Dictionary<string, string>
                {
                    { WhatsAppTemplateContract.Common.UserName, attendeeName },
                    { WhatsAppTemplateContract.Common.EventTitle, @event.Title.Value },
                    { WhatsAppTemplateContract.Common.EventDate, EmailDateTimeHelper.FormatEventDate(@event.StartDate, @event.TimeZoneId) },
                    { WhatsAppTemplateContract.Common.EventTime, EmailDateTimeHelper.FormatEventTime(@event.StartDate, @event.TimeZoneId) },
                    { WhatsAppTemplateContract.Common.EventLocation, location },
                    { WhatsAppTemplateContract.Registration.RegistrationQuantity, quantity.ToString() },
                    { WhatsAppTemplateContract.Common.EventUrl, $"https://lankaconnect.com/events/{eventId}" }
                };

                var result = await whatsAppService.SendTemplateMessageToPhoneAsync(
                    phoneNumber,
                    WhatsAppTemplateContract.TemplateNames.EventRegistrationConfirmed,
                    parameters,
                    eventId,
                    CancellationToken.None);

                if (result.IsSuccess && !result.Value.WasSkipped)
                {
                    _logger.LogInformation("[Phase 7A] WhatsApp AnonymousRegistration SENT: EventId={EventId}", eventId);
                }
                else if (result.IsSuccess && result.Value.WasSkipped)
                {
                    _logger.LogInformation("[Phase 7A] WhatsApp AnonymousRegistration SKIPPED: {Reason}", result.Value.SkipReason);
                }
                else
                {
                    _logger.LogWarning("[Phase 7A] WhatsApp AnonymousRegistration FAILED: {Errors}", string.Join(", ", result.Errors));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Phase 7A] WhatsApp AnonymousRegistration EXCEPTION: EventId={EventId}", eventId);
            }
        }, CancellationToken.None);

        return Task.CompletedTask;
    }
}
