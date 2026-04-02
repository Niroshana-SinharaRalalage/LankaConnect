using LankaConnect.Application.Common;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Communications.Enums;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.DomainEvents;
using LankaConnect.Domain.Users;
using LankaConnect.Shared.WhatsApp.Contracts;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LankaConnect.Application.Events.EventHandlers;

/// <summary>
/// Phase 7A.3: Sends WhatsApp notification when a user commits to a sign-up item.
/// Parallel to UserCommittedToSignUpEventHandler (email). Email handler is UNTOUCHED.
/// Uses fire-and-forget with IServiceScopeFactory [FIX C6].
/// </summary>
public class UserCommittedToSignUpWhatsAppHandler : INotificationHandler<DomainEventNotification<UserCommittedToSignUpEvent>>
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<UserCommittedToSignUpWhatsAppHandler> _logger;

    public UserCommittedToSignUpWhatsAppHandler(
        IServiceScopeFactory scopeFactory,
        ILogger<UserCommittedToSignUpWhatsAppHandler> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public Task Handle(DomainEventNotification<UserCommittedToSignUpEvent> notification, CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;
        var signUpListId = domainEvent.SignUpListId;
        var userId = domainEvent.UserId;
        var itemDescription = domainEvent.ItemDescription;
        var quantity = domainEvent.PhysicalQuantity ?? domainEvent.SlotsClaimed ?? 0;

        _logger.LogInformation(
            "[Phase 7A] WhatsApp UserCommittedToSignUp START: UserId={UserId}, Item={Item}, Quantity={Quantity}",
            userId, itemDescription, quantity);

        _ = Task.Run(async () =>
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var whatsAppService = scope.ServiceProvider.GetRequiredService<IWhatsAppService>();
                var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
                var eventRepository = scope.ServiceProvider.GetRequiredService<IEventRepository>();

                var user = await userRepository.GetByIdAsync(userId, CancellationToken.None);
                if (user == null)
                {
                    _logger.LogWarning("[Phase 7A] WhatsApp UserCommittedToSignUp: User not found - UserId={UserId}", userId);
                    return;
                }

                var @event = await eventRepository.GetEventBySignUpListIdAsync(signUpListId, CancellationToken.None);
                if (@event == null)
                {
                    _logger.LogWarning("[Phase 7A] WhatsApp UserCommittedToSignUp: Event not found - SignUpListId={SignUpListId}", signUpListId);
                    return;
                }

                var signUpList = @event.SignUpLists?.FirstOrDefault(l => l.Id == signUpListId);
                var listName = signUpList?.Category ?? "Sign-Up List";

                var parameters = new Dictionary<string, string>
                {
                    { WhatsAppTemplateContract.Common.UserName, user.FirstName },
                    { WhatsAppTemplateContract.Signup.ListName, listName },
                    { WhatsAppTemplateContract.Common.EventTitle, @event.Title.Value },
                    { WhatsAppTemplateContract.Signup.CommitmentItem, itemDescription },
                    { WhatsAppTemplateContract.Signup.CommitmentQuantity, quantity.ToString() },
                    { WhatsAppTemplateContract.Common.EventUrl, $"https://lankaconnect.com/events/{@event.Id}" }
                };

                var result = await whatsAppService.SendTemplateMessageAsync(
                    userId,
                    WhatsAppTemplateContract.TemplateNames.SignupCommitmentConfirmed,
                    parameters,
                    WhatsAppNotificationType.SignupCommitment,
                    @event.Id,
                    ct: CancellationToken.None);

                if (result.IsSuccess && !result.Value.WasSkipped)
                {
                    _logger.LogInformation("[Phase 7A] WhatsApp UserCommittedToSignUp SENT: UserId={UserId}, EventId={EventId}", userId, @event.Id);
                }
                else if (result.IsSuccess && result.Value.WasSkipped)
                {
                    _logger.LogInformation("[Phase 7A] WhatsApp UserCommittedToSignUp SKIPPED: {Reason}", result.Value.SkipReason);
                }
                else
                {
                    _logger.LogWarning("[Phase 7A] WhatsApp UserCommittedToSignUp FAILED: {Errors}", string.Join(", ", result.Errors));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Phase 7A] WhatsApp UserCommittedToSignUp EXCEPTION: UserId={UserId}", userId);
            }
        }, CancellationToken.None);

        return Task.CompletedTask;
    }
}
