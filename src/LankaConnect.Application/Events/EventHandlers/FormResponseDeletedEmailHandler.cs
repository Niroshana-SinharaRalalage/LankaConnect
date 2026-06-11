using System.Diagnostics;
using LankaConnect.Modules.Forms.Domain;
using LankaConnect.Modules.Forms.Domain.Entities;
using LankaConnect.Modules.Forms.Domain.Enums;
using LankaConnect.Modules.Forms.Domain.DomainEvents;
using LankaConnect.Modules.Forms.Domain.Repositories;
using LankaConnect.Application.Common;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Interfaces;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.DomainEvents;
using LankaConnect.Domain.Events.Repositories;
using LankaConnect.Shared.Email.Contracts;
using LankaConnect.Shared.Email.Helpers;
using LankaConnect.Shared.Email.Services;
using MediatR;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Application.Events.EventHandlers;

/// <summary>
/// Sends cancellation notification email when a form response is deleted.
/// Phase 6A.107: Email notification confirming deletion/cancellation.
///
/// IMPORTANT: This handler runs AFTER the response is deleted from the database.
/// We cannot load the response or its answers. We use data from the domain event.
///
/// FAIL-SILENT PATTERN: Email failures are logged but don't throw exceptions.
///
/// Architect Review: Approved - mirrors signup commitment cancellation pattern.
/// </summary>
public class FormResponseDeletedEmailHandler : INotificationHandler<DomainEventNotification<FormResponseDeletedEvent>>
{
    private readonly IFormRepository _eventFormRepository;
    private readonly IEventRepository _eventRepository;
    private readonly ITypedEmailService _typedEmailService;
    private readonly IEmailUrlHelper _emailUrlHelper;
    private readonly ILogger<FormResponseDeletedEmailHandler> _logger;

    public FormResponseDeletedEmailHandler(
        IFormRepository eventFormRepository,
        IEventRepository eventRepository,
        ITypedEmailService typedEmailService,
        IEmailUrlHelper emailUrlHelper,
        ILogger<FormResponseDeletedEmailHandler> logger)
    {
        _eventFormRepository = eventFormRepository;
        _eventRepository = eventRepository;
        _typedEmailService = typedEmailService;
        _emailUrlHelper = emailUrlHelper;
        _logger = logger;
    }

    public async Task Handle(DomainEventNotification<FormResponseDeletedEvent> notification, CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;

        using (LogContext.PushProperty("Operation", "FormResponseDeletedEmail"))
        using (LogContext.PushProperty("EntityType", "FormResponse"))
        using (LogContext.PushProperty("FormId", domainEvent.FormId))
        using (LogContext.PushProperty("ResponseId", domainEvent.ResponseId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "FormResponseDeletedEmail START: FormId={FormId}, ResponseId={ResponseId}, HasEmail={HasEmail}",
                domainEvent.FormId, domainEvent.ResponseId, !string.IsNullOrEmpty(domainEvent.RespondentEmail));

            try
            {
                // Skip if no email provided
                if (string.IsNullOrWhiteSpace(domainEvent.RespondentEmail))
                {
                    stopwatch.Stop();
                    _logger.LogInformation(
                        "FormResponseDeletedEmail SKIPPED: No respondent email, Duration={ElapsedMs}ms",
                        stopwatch.ElapsedMilliseconds);
                    return;
                }

                // Load form (for form title and event FK)
                var form = await _eventFormRepository.GetByIdAsync(domainEvent.FormId, cancellationToken);
                if (form == null)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "FormResponseDeletedEmail FAILED: Form not found - FormId={FormId}, Duration={ElapsedMs}ms",
                        domainEvent.FormId, stopwatch.ElapsedMilliseconds);
                    return;
                }

                // Load event (for event details in email)
                var eventEntity = await _eventRepository.GetByIdAsync(form.EventId, cancellationToken);
                if (eventEntity == null)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "FormResponseDeletedEmail FAILED: Event not found - EventId={EventId}, Duration={ElapsedMs}ms",
                        form.EventId, stopwatch.ElapsedMilliseconds);
                    return;
                }

                // NOTE: Response is already deleted, so we can't access answers for summary
                // Using generic message instead
                var responseSummary = "Your response has been cancelled.";

                // Create email parameters (no edit link for cancellation)
                var emailParams = FormResponseEmailParams.CreateCancellation(
                    userName: "User",  // We don't have RespondentName in the event
                    userEmail: domainEvent.RespondentEmail,
                    eventId: eventEntity.Id,
                    eventTitle: eventEntity.Title.Value,
                    formTitle: form.Title,
                    responseSummary: responseSummary,
                    eventStartDate: eventEntity.StartDate.GetValueOrDefault(), // Phase 8YA-2 TODO: param class should accept DateTime?
                    timeZoneId: eventEntity.TimeZoneId,
                    eventLocation: eventEntity.Location?.ToString() ?? string.Empty,
                    eventDetailsUrl: _emailUrlHelper.BuildEventDetailsUrl(eventEntity.Id),
                    cancelledAt: domainEvent.OccurredAt
                );

                // Add optional fields
                if (!string.IsNullOrWhiteSpace(form.Description))
                {
                    emailParams.WithFormDescription(form.Description);
                }

                // Get primary image from Images collection
                var primaryImage = eventEntity.Images.FirstOrDefault(i => i.IsPrimary);
                var imageUrl = primaryImage?.ImageUrl ?? eventEntity.Images.FirstOrDefault()?.ImageUrl ?? "";
                if (!string.IsNullOrWhiteSpace(imageUrl))
                {
                    emailParams.WithEventImage(imageUrl);
                }

                emailParams.WithOrganizerContacts(
                    eventEntity.OrganizerContacts
                        .OrderBy(c => c.SortOrder)
                        .Select(c => new OrganizerContactInfo(c.ContactName, c.ContactEmail, c.ContactPhone, c.IsPrimary))
                        .ToList());

                // Phase 6A.129: Add signup lists & forms URLs for email action buttons
                if (eventEntity.SignUpLists?.Any() == true)
                {
                    var signupListsUrl = _emailUrlHelper.BuildSignupListsUrl(eventEntity.Id);
                    emailParams.WithSignupListsUrl(signupListsUrl);
                }
                // Always add signup forms URL — event has forms if we're sending a form response email
                var signupFormsUrl = _emailUrlHelper.BuildSignupFormsUrl(eventEntity.Id);
                emailParams.WithSignupFormsUrl(signupFormsUrl);

                // Send email (fail-silent)
                var emailResult = await _typedEmailService.SendEmailAsync(emailParams, cancellationToken);

                stopwatch.Stop();

                if (emailResult.Success)
                {
                    _logger.LogInformation(
                        "FormResponseDeletedEmail SUCCESS: Email sent to {RecipientEmail}, Duration={ElapsedMs}ms",
                        domainEvent.RespondentEmail, stopwatch.ElapsedMilliseconds);
                }
                else
                {
                    _logger.LogError(
                        "FormResponseDeletedEmail FAILED: Email send failed - RecipientEmail={RecipientEmail}, Error={Error}, Duration={ElapsedMs}ms",
                        domainEvent.RespondentEmail, string.Join(", ", emailResult.Errors), stopwatch.ElapsedMilliseconds);
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                // FAIL-SILENT: Log error but don't throw
                _logger.LogError(ex,
                    "FormResponseDeletedEmail EXCEPTION: Unexpected error - FormId={FormId}, ResponseId={ResponseId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    domainEvent.FormId, domainEvent.ResponseId, stopwatch.ElapsedMilliseconds, ex.Message);
            }
        }
    }
}
