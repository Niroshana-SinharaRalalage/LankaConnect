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
using LankaConnect.Domain.Events.Entities;
using LankaConnect.Domain.Events.Repositories;
using LankaConnect.Shared.Email.Contracts;
using LankaConnect.Shared.Email.Helpers;
using LankaConnect.Shared.Email.Services;
using MediatR;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Modules.Forms.Application.EventHandlers;

/// <summary>
/// Sends confirmation email when a form response is submitted.
/// Phase 6A.107: Email notification with edit link for cross-browser access.
///
/// FAIL-SILENT PATTERN: Email failures are logged but don't throw exceptions
/// to prevent transaction rollback. Domain event has already been raised,
/// response is already persisted - email is supplementary communication.
///
/// Architect Review: Approved - mirrors signup commitment pattern with response summary limits.
/// </summary>
public class FormResponseSubmittedEmailHandler : INotificationHandler<DomainEventNotification<FormResponseSubmittedEvent>>
{
    private readonly IFormResponseRepository _formResponseRepository;
    private readonly IFormRepository _eventFormRepository;
    private readonly IEventRepository _eventRepository;
    private readonly ITypedEmailService _typedEmailService;
    private readonly IEmailUrlHelper _emailUrlHelper;
    private readonly ILogger<FormResponseSubmittedEmailHandler> _logger;

    public FormResponseSubmittedEmailHandler(
        IFormResponseRepository formResponseRepository,
        IFormRepository eventFormRepository,
        IEventRepository eventRepository,
        ITypedEmailService typedEmailService,
        IEmailUrlHelper emailUrlHelper,
        ILogger<FormResponseSubmittedEmailHandler> logger)
    {
        _formResponseRepository = formResponseRepository;
        _eventFormRepository = eventFormRepository;
        _eventRepository = eventRepository;
        _typedEmailService = typedEmailService;
        _emailUrlHelper = emailUrlHelper;
        _logger = logger;
    }

    public async Task Handle(DomainEventNotification<FormResponseSubmittedEvent> notification, CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;

        using (LogContext.PushProperty("Operation", "FormResponseSubmittedEmail"))
        using (LogContext.PushProperty("EntityType", "FormResponse"))
        using (LogContext.PushProperty("FormId", domainEvent.FormId))
        using (LogContext.PushProperty("ResponseId", domainEvent.ResponseId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "FormResponseSubmittedEmail START: FormId={FormId}, ResponseId={ResponseId}, HasEmail={HasEmail}, HasToken={HasToken}",
                domainEvent.FormId, domainEvent.ResponseId,
                !string.IsNullOrEmpty(domainEvent.RespondentEmail),
                !string.IsNullOrEmpty(domainEvent.AccessToken));

            try
            {
                // Skip if no email provided (anonymous user without email)
                if (string.IsNullOrWhiteSpace(domainEvent.RespondentEmail))
                {
                    stopwatch.Stop();
                    _logger.LogInformation(
                        "FormResponseSubmittedEmail SKIPPED: No respondent email provided, Duration={ElapsedMs}ms",
                        stopwatch.ElapsedMilliseconds);
                    return;
                }

                // Load response with answers
                var response = await _formResponseRepository.GetByIdWithAnswersAsync(domainEvent.ResponseId, cancellationToken);
                if (response == null)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "FormResponseSubmittedEmail FAILED: Response not found - ResponseId={ResponseId}, Duration={ElapsedMs}ms",
                        domainEvent.ResponseId, stopwatch.ElapsedMilliseconds);
                    return;
                }

                // Load form with questions (for response summary)
                var form = await _eventFormRepository.GetByIdWithQuestionsAsync(domainEvent.FormId, cancellationToken);
                if (form == null)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "FormResponseSubmittedEmail FAILED: Form not found - FormId={FormId}, Duration={ElapsedMs}ms",
                        domainEvent.FormId, stopwatch.ElapsedMilliseconds);
                    return;
                }

                // Load event (for event details in email)
                var eventEntity = await _eventRepository.GetByIdAsync(response.EventId, cancellationToken);
                if (eventEntity == null)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "FormResponseSubmittedEmail FAILED: Event not found - EventId={EventId}, Duration={ElapsedMs}ms",
                        response.EventId, stopwatch.ElapsedMilliseconds);
                    return;
                }

                // Build response summary with length limits (Architect fix: max 5 questions, 100 chars per answer)
                var responseSummary = BuildResponseSummary(response.Answers, form.Questions, maxQuestions: 5, maxAnswerLength: 100);

                // Build edit URL with access token (for anonymous users) or without (for logged-in users)
                var editUrl = BuildEditUrl(response.EventId, domainEvent.FormId, domainEvent.AccessToken);

                // Create email parameters
                var emailParams = FormResponseEmailParams.CreateConfirmation(
                    userName: response.RespondentName ?? "User",
                    userEmail: domainEvent.RespondentEmail,
                    eventId: eventEntity.Id,
                    eventTitle: eventEntity.Title.Value,
                    formTitle: form.Title,
                    responseSummary: responseSummary,
                    editFormUrl: editUrl,
                    eventStartDate: eventEntity.StartDate.GetValueOrDefault(), // Phase 8YA-2 TODO: param class should accept DateTime?
                    timeZoneId: eventEntity.TimeZoneId,
                    eventLocation: eventEntity.Location?.ToString() ?? string.Empty,
                    eventDetailsUrl: _emailUrlHelper.BuildEventDetailsUrl(eventEntity.Id),
                    submittedAt: response.SubmittedAt
                );

                // Add optional fields
                if (!string.IsNullOrWhiteSpace(form.Description))
                {
                    emailParams.WithFormDescription(form.Description);
                }

                if (form.ResponseDeadline.HasValue)
                {
                    emailParams.WithResponseDeadline(form.ResponseDeadline);
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

                // Send email (fail-silent: log errors but don't throw)
                var emailResult = await _typedEmailService.SendEmailAsync(emailParams, cancellationToken);

                stopwatch.Stop();

                if (emailResult.Success)
                {
                    _logger.LogInformation(
                        "FormResponseSubmittedEmail SUCCESS: Email sent to {RecipientEmail}, Duration={ElapsedMs}ms",
                        domainEvent.RespondentEmail, stopwatch.ElapsedMilliseconds);
                }
                else
                {
                    _logger.LogError(
                        "FormResponseSubmittedEmail FAILED: Email send failed - RecipientEmail={RecipientEmail}, Error={Error}, Duration={ElapsedMs}ms",
                        domainEvent.RespondentEmail, string.Join(", ", emailResult.Errors), stopwatch.ElapsedMilliseconds);
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                // FAIL-SILENT: Log error but don't throw (prevent transaction rollback)
                _logger.LogError(ex,
                    "FormResponseSubmittedEmail EXCEPTION: Unexpected error - FormId={FormId}, ResponseId={ResponseId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    domainEvent.FormId, domainEvent.ResponseId, stopwatch.ElapsedMilliseconds, ex.Message);
            }
        }
    }

    /// <summary>
    /// Builds a concise response summary from form answers.
    /// Architect fix: Limits to 5 questions, 100 chars per answer to prevent email bloat.
    /// </summary>
    private string BuildResponseSummary(
        IReadOnlyList<FormAnswer> answers,
        IReadOnlyList<FormQuestion> questions,
        int maxQuestions = 5,
        int maxAnswerLength = 100)
    {
        if (!answers.Any())
            return "No responses provided.";

        var questionMap = questions.ToDictionary(q => q.Id, q => q.QuestionText);
        var displayedAnswers = answers.Take(maxQuestions);

        var summaryParts = displayedAnswers.Select(answer =>
        {
            var questionText = questionMap.TryGetValue(answer.FormQuestionId, out var qText)
                ? qText
                : "Question";

            var answerText = answer.TextValue ??
                            string.Join(", ", answer.SelectedOptionTextSnapshots ?? new List<string>()) ??
                            answer.BooleanValue?.ToString() ?? "";

            // Truncate long answers
            if (answerText.Length > maxAnswerLength)
                answerText = $"{answerText.Substring(0, maxAnswerLength)}...";

            return $"{questionText}: {answerText}";
        });

        var summary = string.Join(" | ", summaryParts);

        var remainingCount = answers.Count - maxQuestions;
        if (remainingCount > 0)
            summary += $" | ... and {remainingCount} more response{(remainingCount > 1 ? "s" : "")}";

        return summary;
    }

    /// <summary>
    /// Builds edit URL with access token for anonymous users or without for logged-in users.
    /// Architect fix: Token passed via domain event (in-memory only, never persisted).
    /// </summary>
    private string BuildEditUrl(Guid eventId, Guid formId, string? accessToken)
    {
        var baseUrl = _emailUrlHelper.BuildEventDetailsUrl(eventId).Replace("/details", "");

        // For anonymous users: Include token in URL for cross-browser access
        // For logged-in users: No token needed (fetched by userId in backend)
        if (!string.IsNullOrEmpty(accessToken))
        {
            return $"{baseUrl}/events/{eventId}/forms/{formId}?token={accessToken}";
        }

        return $"{baseUrl}/events/{eventId}/forms/{formId}";
    }
}
