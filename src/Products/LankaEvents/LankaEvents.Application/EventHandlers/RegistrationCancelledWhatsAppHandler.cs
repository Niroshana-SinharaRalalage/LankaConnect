using LankaConnect.Modules.Identity.Contracts; // W4.6.d.2.b: IUserRepository -> IIdentityQueries/IIdentityCommands
using LankaConnect.BuildingBlocks.Application.Common;
using LankaConnect.BuildingBlocks.Application.Common.Interfaces;
using LankaConnect.Modules.Communications.Domain.Enums;
using LankaConnect.Products.LankaEvents.Domain;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using LankaConnect.Products.LankaEvents.Domain.DomainEvents;
using LankaConnect.Modules.Communications.Contracts.Email.Helpers;
using LankaConnect.Modules.Communications.Contracts.WhatsApp.Contracts;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
namespace LankaConnect.Products.LankaEvents.Application.EventHandlers;

/// <summary>
/// Phase 7A.3: Sends WhatsApp notification when a registration is cancelled.
/// Parallel to RegistrationCancelledEventHandler (email). Email handler is UNTOUCHED.
/// Uses fire-and-forget with IServiceScopeFactory [FIX C6].
/// </summary>
public class RegistrationCancelledWhatsAppHandler : INotificationHandler<DomainEventNotification<RegistrationCancelledEvent>>
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RegistrationCancelledWhatsAppHandler> _logger;

    public RegistrationCancelledWhatsAppHandler(
        IServiceScopeFactory scopeFactory,
        ILogger<RegistrationCancelledWhatsAppHandler> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public Task Handle(DomainEventNotification<RegistrationCancelledEvent> notification, CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;
        var eventId = domainEvent.EventId;
        var attendeeId = domainEvent.AttendeeId;

        _logger.LogInformation(
            "[Phase 7A] WhatsApp RegistrationCancelled START: EventId={EventId}, AttendeeId={AttendeeId}",
            eventId, attendeeId);

        _ = Task.Run(async () =>
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var whatsAppService = scope.ServiceProvider.GetRequiredService<IWhatsAppService>();
                var identityQueries = scope.ServiceProvider.GetRequiredService<IIdentityQueries>();
                var eventRepository = scope.ServiceProvider.GetRequiredService<IEventRepository>();

                var user = await identityQueries.GetUserByIdAsync(attendeeId, CancellationToken.None);
                if (user == null)
                {
                    _logger.LogWarning("[Phase 7A] WhatsApp RegistrationCancelled: User not found - AttendeeId={AttendeeId}", attendeeId);
                    return;
                }

                var @event = await eventRepository.GetByIdAsync(eventId, CancellationToken.None);
                if (@event == null)
                {
                    _logger.LogWarning("[Phase 7A] WhatsApp RegistrationCancelled: Event not found - EventId={EventId}", eventId);
                    return;
                }

                var parameters = new Dictionary<string, string>
                {
                    { WhatsAppTemplateContract.Common.UserName, $"{user.FirstName} {user.LastName}" },
                    { WhatsAppTemplateContract.Common.EventTitle, @event.Title.Value },
                    { WhatsAppTemplateContract.Registration.CancellationReason, "Registration cancelled by attendee" },
                    { WhatsAppTemplateContract.Registration.CancellationDate, EmailDateTimeHelper.FormatEventDate(domainEvent.CancelledAt) },
                    { WhatsAppTemplateContract.Common.EventUrl, $"https://lankaconnect.com/events/{eventId}" }
                };

                var result = await whatsAppService.SendTemplateMessageAsync(
                    attendeeId,
                    WhatsAppTemplateContract.TemplateNames.RegistrationCancelled,
                    parameters,
                    WhatsAppNotificationType.EventRegistration,
                    eventId,
                    ct: CancellationToken.None);

                if (result.IsSuccess && !result.Value.WasSkipped)
                {
                    _logger.LogInformation("[Phase 7A] WhatsApp RegistrationCancelled SENT: EventId={EventId}, AttendeeId={AttendeeId}", eventId, attendeeId);
                }
                else if (result.IsSuccess && result.Value.WasSkipped)
                {
                    _logger.LogInformation("[Phase 7A] WhatsApp RegistrationCancelled SKIPPED: {Reason}", result.Value.SkipReason);
                }
                else
                {
                    _logger.LogWarning("[Phase 7A] WhatsApp RegistrationCancelled FAILED: {Errors}", string.Join(", ", result.Errors));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Phase 7A] WhatsApp RegistrationCancelled EXCEPTION: EventId={EventId}, AttendeeId={AttendeeId}", eventId, attendeeId);
            }
        }, CancellationToken.None);

        return Task.CompletedTask;
    }
}
