using LankaConnect.Modules.Identity.Contracts; // W4.6.d.2.b: IUserRepository -> IIdentityQueries/IIdentityCommands
using LankaConnect.Application.Common;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Communications.Enums;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Users.DomainEvents; // W4.7.a: user-aggregate events moved here
using LankaConnect.Domain.Events.DomainEvents;
using LankaConnect.Shared.WhatsApp.Contracts;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LankaConnect.Application.Events.EventHandlers;

/// <summary>
/// Phase 7A.3: Sends WhatsApp notification when a user cancels their sign-up commitment.
/// Parallel to CommitmentCancelledEmailHandler (email). Email handler is UNTOUCHED.
/// Uses fire-and-forget with IServiceScopeFactory [FIX C6].
/// NOTE: Uses data from domain event directly (entities may be deleted by the time this handler runs).
/// </summary>
public class CommitmentCancelledWhatsAppHandler : INotificationHandler<DomainEventNotification<CommitmentCancelledEvent>>
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<CommitmentCancelledWhatsAppHandler> _logger;

    public CommitmentCancelledWhatsAppHandler(
        IServiceScopeFactory scopeFactory,
        ILogger<CommitmentCancelledWhatsAppHandler> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public Task Handle(DomainEventNotification<CommitmentCancelledEvent> notification, CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;
        var userId = domainEvent.UserId;
        var signUpListId = domainEvent.SignUpListId;
        var itemDescription = domainEvent.ItemDescription;

        _logger.LogInformation(
            "[Phase 7A] WhatsApp CommitmentCancelled START: UserId={UserId}, Item={Item}, CommitmentId={CommitmentId}",
            userId, itemDescription, domainEvent.CommitmentId);

        _ = Task.Run(async () =>
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var whatsAppService = scope.ServiceProvider.GetRequiredService<IWhatsAppService>();
                var identityQueries = scope.ServiceProvider.GetRequiredService<IIdentityQueries>();
                var eventRepository = scope.ServiceProvider.GetRequiredService<IEventRepository>();

                var user = await identityQueries.GetUserByIdAsync(userId, CancellationToken.None);
                if (user == null)
                {
                    _logger.LogWarning("[Phase 7A] WhatsApp CommitmentCancelled: User not found - UserId={UserId}", userId);
                    return;
                }

                var @event = await eventRepository.GetEventBySignUpListIdAsync(signUpListId, CancellationToken.None);
                if (@event == null)
                {
                    _logger.LogWarning("[Phase 7A] WhatsApp CommitmentCancelled: Event not found - SignUpListId={SignUpListId}", signUpListId);
                    return;
                }

                var signUpList = @event.SignUpLists?.FirstOrDefault(l => l.Id == signUpListId);
                var listName = signUpList?.Category ?? "Sign-Up List";

                var parameters = new Dictionary<string, string>
                {
                    { WhatsAppTemplateContract.Common.UserName, user.FirstName },
                    { WhatsAppTemplateContract.Signup.ListName, listName },
                    { WhatsAppTemplateContract.Common.EventTitle, @event.Title.Value },
                    { WhatsAppTemplateContract.Common.EventUrl, $"https://lankaconnect.com/events/{@event.Id}" }
                };

                var result = await whatsAppService.SendTemplateMessageAsync(
                    userId,
                    WhatsAppTemplateContract.TemplateNames.SignupCommitmentCancelled,
                    parameters,
                    WhatsAppNotificationType.SignupCommitment,
                    @event.Id,
                    ct: CancellationToken.None);

                if (result.IsSuccess && !result.Value.WasSkipped)
                {
                    _logger.LogInformation("[Phase 7A] WhatsApp CommitmentCancelled SENT: UserId={UserId}, EventId={EventId}", userId, @event.Id);
                }
                else if (result.IsSuccess && result.Value.WasSkipped)
                {
                    _logger.LogInformation("[Phase 7A] WhatsApp CommitmentCancelled SKIPPED: {Reason}", result.Value.SkipReason);
                }
                else
                {
                    _logger.LogWarning("[Phase 7A] WhatsApp CommitmentCancelled FAILED: {Errors}", string.Join(", ", result.Errors));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Phase 7A] WhatsApp CommitmentCancelled EXCEPTION: UserId={UserId}", userId);
            }
        }, CancellationToken.None);

        return Task.CompletedTask;
    }
}
