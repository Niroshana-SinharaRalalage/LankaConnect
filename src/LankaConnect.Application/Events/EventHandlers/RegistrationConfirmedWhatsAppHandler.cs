using LankaConnect.Modules.Identity.Contracts; // W4.6.d.2.b: IUserRepository -> IIdentityQueries/IIdentityCommands
using LankaConnect.Application.Common;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Communications.Enums;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Users.DomainEvents; // W4.7.a: user-aggregate events moved here
using LankaConnect.Domain.Events.DomainEvents;
using LankaConnect.Domain.Users;
using LankaConnect.Shared.Email.Helpers;
using LankaConnect.Shared.WhatsApp.Contracts;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LankaConnect.Application.Events.EventHandlers;

/// <summary>
/// Phase 7A.3: Sends WhatsApp notification when a free event registration is confirmed.
/// Parallel to RegistrationConfirmedEventHandler (email). Email handler is UNTOUCHED.
/// Uses fire-and-forget with IServiceScopeFactory [FIX C6].
/// </summary>
public class RegistrationConfirmedWhatsAppHandler : INotificationHandler<DomainEventNotification<RegistrationConfirmedEvent>>
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RegistrationConfirmedWhatsAppHandler> _logger;

    public RegistrationConfirmedWhatsAppHandler(
        IServiceScopeFactory scopeFactory,
        ILogger<RegistrationConfirmedWhatsAppHandler> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public Task Handle(DomainEventNotification<RegistrationConfirmedEvent> notification, CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;
        var eventId = domainEvent.EventId;
        var attendeeId = domainEvent.AttendeeId;
        var quantity = domainEvent.Quantity;

        _logger.LogInformation(
            "[Phase 7A] WhatsApp RegistrationConfirmed START: EventId={EventId}, AttendeeId={AttendeeId}, Quantity={Quantity}",
            eventId, attendeeId, quantity);

        _ = Task.Run(async () =>
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var whatsAppService = scope.ServiceProvider.GetRequiredService<IWhatsAppService>();
                var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
                var eventRepository = scope.ServiceProvider.GetRequiredService<IEventRepository>();

                var user = await userRepository.GetByIdAsync(attendeeId, CancellationToken.None);
                if (user == null)
                {
                    _logger.LogWarning("[Phase 7A] WhatsApp RegistrationConfirmed: User not found - AttendeeId={AttendeeId}", attendeeId);
                    return;
                }

                var @event = await eventRepository.GetByIdAsync(eventId, CancellationToken.None);
                if (@event == null)
                {
                    _logger.LogWarning("[Phase 7A] WhatsApp RegistrationConfirmed: Event not found - EventId={EventId}", eventId);
                    return;
                }

                var location = @event.Location?.Address != null
                    ? $"{@event.Location.Address.Street}, {@event.Location.Address.City}".Trim(',', ' ')
                    : "Online Event";

                var parameters = new Dictionary<string, string>
                {
                    { WhatsAppTemplateContract.Common.UserName, $"{user.FirstName} {user.LastName}" },
                    { WhatsAppTemplateContract.Common.EventTitle, @event.Title.Value },
                    { WhatsAppTemplateContract.Common.EventDate, EmailDateTimeHelper.FormatEventDate(@event.StartDate, @event.TimeZoneId) },
                    { WhatsAppTemplateContract.Common.EventTime, EmailDateTimeHelper.FormatEventTime(@event.StartDate, @event.TimeZoneId) },
                    { WhatsAppTemplateContract.Common.EventLocation, location },
                    { WhatsAppTemplateContract.Registration.RegistrationQuantity, quantity.ToString() },
                    { WhatsAppTemplateContract.Common.EventUrl, $"https://lankaconnect.com/events/{eventId}" }
                };

                var result = await whatsAppService.SendTemplateMessageAsync(
                    attendeeId,
                    WhatsAppTemplateContract.TemplateNames.EventRegistrationConfirmed,
                    parameters,
                    WhatsAppNotificationType.EventRegistration,
                    eventId,
                    ct: CancellationToken.None);

                if (result.IsSuccess && !result.Value.WasSkipped)
                {
                    _logger.LogInformation("[Phase 7A] WhatsApp RegistrationConfirmed SENT: EventId={EventId}, AttendeeId={AttendeeId}", eventId, attendeeId);
                }
                else if (result.IsSuccess && result.Value.WasSkipped)
                {
                    _logger.LogInformation("[Phase 7A] WhatsApp RegistrationConfirmed SKIPPED: {Reason}", result.Value.SkipReason);
                }
                else
                {
                    _logger.LogWarning("[Phase 7A] WhatsApp RegistrationConfirmed FAILED: {Errors}", string.Join(", ", result.Errors));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Phase 7A] WhatsApp RegistrationConfirmed EXCEPTION: EventId={EventId}, AttendeeId={AttendeeId}", eventId, attendeeId);
            }
        }, CancellationToken.None);

        return Task.CompletedTask;
    }
}
