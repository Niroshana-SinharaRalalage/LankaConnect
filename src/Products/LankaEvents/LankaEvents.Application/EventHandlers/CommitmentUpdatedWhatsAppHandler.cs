using LankaConnect.Modules.Identity.Contracts; // W4.6.d.2.b: IUserRepository -> IIdentityQueries/IIdentityCommands
using LankaConnect.Application.Common;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Communications.Enums;
using LankaConnect.Products.LankaEvents.Domain;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using LankaConnect.Products.LankaEvents.Domain.DomainEvents;
using LankaConnect.Shared.WhatsApp.Contracts;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
namespace LankaConnect.Products.LankaEvents.Application.EventHandlers;

/// <summary>
/// Phase 7A.3: Sends WhatsApp notification when a user updates their sign-up commitment.
/// Parallel to CommitmentUpdatedEventHandler (email). Email handler is UNTOUCHED.
/// Uses fire-and-forget with IServiceScopeFactory [FIX C6].
/// </summary>
public class CommitmentUpdatedWhatsAppHandler : INotificationHandler<DomainEventNotification<CommitmentUpdatedEvent>>
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<CommitmentUpdatedWhatsAppHandler> _logger;

    public CommitmentUpdatedWhatsAppHandler(
        IServiceScopeFactory scopeFactory,
        ILogger<CommitmentUpdatedWhatsAppHandler> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public Task Handle(DomainEventNotification<CommitmentUpdatedEvent> notification, CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;
        var signUpItemId = domainEvent.SignUpItemId;
        var userId = domainEvent.UserId;
        var itemDescription = domainEvent.ItemDescription;
        var newQuantity = domainEvent.NewPhysicalQuantity ?? domainEvent.NewSlotsClaimed ?? 0;

        _logger.LogInformation(
            "[Phase 7A] WhatsApp CommitmentUpdated START: UserId={UserId}, Item={Item}, NewQuantity={Quantity}",
            userId, itemDescription, newQuantity);

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
                    _logger.LogWarning("[Phase 7A] WhatsApp CommitmentUpdated: User not found - UserId={UserId}", userId);
                    return;
                }

                var @event = await eventRepository.GetEventBySignUpItemIdAsync(signUpItemId, CancellationToken.None);
                if (@event == null)
                {
                    _logger.LogWarning("[Phase 7A] WhatsApp CommitmentUpdated: Event not found - SignUpItemId={SignUpItemId}", signUpItemId);
                    return;
                }

                // Find the list name from the event's sign-up lists
                var listName = "Sign-Up List";
                if (@event.SignUpLists != null)
                {
                    foreach (var list in @event.SignUpLists)
                    {
                        if (list.Items?.Any(i => i.Id == signUpItemId) == true)
                        {
                            listName = list.Category;
                            break;
                        }
                    }
                }

                var parameters = new Dictionary<string, string>
                {
                    { WhatsAppTemplateContract.Common.UserName, user.FirstName },
                    { WhatsAppTemplateContract.Signup.ListName, listName },
                    { WhatsAppTemplateContract.Common.EventTitle, @event.Title.Value },
                    { WhatsAppTemplateContract.Signup.CommitmentItem, itemDescription },
                    { WhatsAppTemplateContract.Signup.CommitmentQuantity, newQuantity.ToString() },
                    { WhatsAppTemplateContract.Common.EventUrl, $"https://lankaconnect.com/events/{@event.Id}" }
                };

                var result = await whatsAppService.SendTemplateMessageAsync(
                    userId,
                    WhatsAppTemplateContract.TemplateNames.SignupCommitmentUpdated,
                    parameters,
                    WhatsAppNotificationType.SignupCommitment,
                    @event.Id,
                    ct: CancellationToken.None);

                if (result.IsSuccess && !result.Value.WasSkipped)
                {
                    _logger.LogInformation("[Phase 7A] WhatsApp CommitmentUpdated SENT: UserId={UserId}, EventId={EventId}", userId, @event.Id);
                }
                else if (result.IsSuccess && result.Value.WasSkipped)
                {
                    _logger.LogInformation("[Phase 7A] WhatsApp CommitmentUpdated SKIPPED: {Reason}", result.Value.SkipReason);
                }
                else
                {
                    _logger.LogWarning("[Phase 7A] WhatsApp CommitmentUpdated FAILED: {Errors}", string.Join(", ", result.Errors));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Phase 7A] WhatsApp CommitmentUpdated EXCEPTION: UserId={UserId}", userId);
            }
        }, CancellationToken.None);

        return Task.CompletedTask;
    }
}
