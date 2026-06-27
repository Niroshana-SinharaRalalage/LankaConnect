using LankaConnect.Application.Common;
using LankaConnect.Modules.Forms.Domain;
using LankaConnect.Modules.Forms.Domain.Entities;
using LankaConnect.Modules.Forms.Domain.Enums;
using LankaConnect.Modules.Forms.Domain.DomainEvents;
using LankaConnect.Modules.Forms.Domain.Repositories;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Communications.Enums;
using LankaConnect.Domain.Events;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using LankaConnect.Domain.Events.DomainEvents;
using LankaConnect.Domain.Events.Repositories;
using LankaConnect.Shared.WhatsApp.Contracts;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LankaConnect.Modules.Forms.Application.EventHandlers;

/// <summary>
/// Phase 7B.3: Sends WhatsApp form response confirmation when a user submits an event form.
/// Parallel to FormResponseSubmittedEventHandler (email). Email handler is UNTOUCHED.
/// Uses fire-and-forget with IServiceScopeFactory.
/// Note: FormResponseSubmittedEvent has no UserId — must look up from FormResponse entity.
/// </summary>
public class FormResponseWhatsAppHandler : INotificationHandler<DomainEventNotification<FormResponseSubmittedEvent>>
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<FormResponseWhatsAppHandler> _logger;

    public FormResponseWhatsAppHandler(
        IServiceScopeFactory scopeFactory,
        ILogger<FormResponseWhatsAppHandler> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public Task Handle(DomainEventNotification<FormResponseSubmittedEvent> notification, CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;
        var formId = domainEvent.FormId;
        var responseId = domainEvent.ResponseId;

        _logger.LogInformation(
            "[Phase 7B.3] WhatsApp FormResponse START: FormId={FormId}, ResponseId={ResponseId}",
            formId, responseId);

        _ = Task.Run(async () =>
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var whatsAppService = scope.ServiceProvider.GetRequiredService<IWhatsAppService>();
                var formResponseRepository = scope.ServiceProvider.GetRequiredService<IFormResponseRepository>();
                var eventFormRepository = scope.ServiceProvider.GetRequiredService<IFormRepository>();

                var formResponse = await formResponseRepository.GetByIdAsync(responseId, CancellationToken.None);
                if (formResponse == null)
                {
                    _logger.LogWarning("[Phase 7B.3] WhatsApp FormResponse: Response not found - ResponseId={ResponseId}", responseId);
                    return;
                }

                if (!formResponse.RespondentUserId.HasValue)
                {
                    _logger.LogInformation(
                        "[Phase 7B.3] WhatsApp FormResponse SKIPPED: Anonymous respondent - ResponseId={ResponseId}",
                        responseId);
                    return;
                }

                var form = await eventFormRepository.GetByIdAsync(formId, CancellationToken.None);
                if (form == null)
                {
                    _logger.LogWarning("[Phase 7B.3] WhatsApp FormResponse: Form not found - FormId={FormId}", formId);
                    return;
                }

                var parameters = new Dictionary<string, string>
                {
                    { WhatsAppTemplateContract.Common.UserName, formResponse.RespondentName ?? "Respondent" },
                    { WhatsAppTemplateContract.Form.FormTitle, form.Title },
                    { WhatsAppTemplateContract.Common.EventUrl, $"https://lankaconnect.com/events/{formResponse.EventId}" }
                };

                var result = await whatsAppService.SendTemplateMessageAsync(
                    formResponse.RespondentUserId.Value,
                    WhatsAppTemplateContract.TemplateNames.FormResponseConfirmed,
                    parameters,
                    WhatsAppNotificationType.FormResponse,
                    formResponse.EventId,
                    ct: CancellationToken.None);

                if (result.IsSuccess && !result.Value.WasSkipped)
                {
                    _logger.LogInformation("[Phase 7B.3] WhatsApp FormResponse SENT: FormId={FormId}, UserId={UserId}", formId, formResponse.RespondentUserId.Value);
                }
                else if (result.IsSuccess && result.Value.WasSkipped)
                {
                    _logger.LogInformation("[Phase 7B.3] WhatsApp FormResponse SKIPPED: {Reason}", result.Value.SkipReason);
                }
                else
                {
                    _logger.LogWarning("[Phase 7B.3] WhatsApp FormResponse FAILED: {Errors}", string.Join(", ", result.Errors));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Phase 7B.3] WhatsApp FormResponse EXCEPTION: FormId={FormId}, ResponseId={ResponseId}", formId, responseId);
            }
        }, CancellationToken.None);

        return Task.CompletedTask;
    }
}
