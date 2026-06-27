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
using LankaConnect.Modules.Identity.Domain.DomainEvents;
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
/// Sends update notification email when a form response is modified.
/// Phase 6A.107: Email notification confirming changes with edit link.
///
/// FAIL-SILENT PATTERN: Email failures are logged but don't throw exceptions
/// to prevent transaction rollback.
///
/// Architect Review: Approved - mirrors signup commitment update pattern.
/// </summary>
public class FormResponseUpdatedEmailHandler : INotificationHandler<DomainEventNotification<FormResponseUpdatedEvent>>
{
    private readonly IFormResponseRepository _formResponseRepository;
    private readonly IFormRepository _eventFormRepository;
    private readonly IEventRepository _eventRepository;
    private readonly ITypedEmailService _typedEmailService;
    private readonly IEmailUrlHelper _emailUrlHelper;
    private readonly ILogger<FormResponseUpdatedEmailHandler> _logger;

    public FormResponseUpdatedEmailHandler(
        IFormResponseRepository formResponseRepository,
        IFormRepository eventFormRepository,
        IEventRepository eventRepository,
        ITypedEmailService typedEmailService,
        IEmailUrlHelper emailUrlHelper,
        ILogger<FormResponseUpdatedEmailHandler> logger)
    {
        _formResponseRepository = formResponseRepository;
        _eventFormRepository = eventFormRepository;
        _eventRepository = eventRepository;
        _typedEmailService = typedEmailService;
        _emailUrlHelper = emailUrlHelper;
        _logger = logger;
    }

    public async Task Handle(DomainEventNotification<FormResponseUpdatedEvent> notification, CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;

        using (LogContext.PushProperty("Operation", "FormResponseUpdatedEmail"))
        using (LogContext.PushProperty("EntityType", "FormResponse"))
        using (LogContext.PushProperty("FormId", domainEvent.FormId))
        using (LogContext.PushProperty("ResponseId", domainEvent.ResponseId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "FormResponseUpdatedEmail START: FormId={FormId}, ResponseId={ResponseId}",
                domainEvent.FormId, domainEvent.ResponseId);

            try
            {
                // Phase 6A.114: Use Form and Event from domain event (already loaded by command handler)
                // Performance optimization: Eliminates 2 duplicate database queries
                // Before: 3 queries (response, form, event) = 40s total
                // After: 1 query (response only) = 5-8s total (75-80% improvement!)
                var form = domainEvent.Form;
                var eventEntity = domainEvent.Event;

                _logger.LogInformation(
                    "FormResponseUpdatedEmail: Using pre-loaded entities from domain event - Form={FormTitle}, Event={EventTitle}",
                    form.Title, eventEntity.Title.Value);

                // Load response with answers (still needed for email content with latest data)
                var response = await _formResponseRepository.GetByIdWithAnswersAsync(domainEvent.ResponseId, cancellationToken);
                if (response == null)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "FormResponseUpdatedEmail FAILED: Response not found - ResponseId={ResponseId}, Duration={ElapsedMs}ms",
                        domainEvent.ResponseId, stopwatch.ElapsedMilliseconds);
                    return;
                }

                // Skip if no email provided
                if (string.IsNullOrWhiteSpace(response.RespondentEmail))
                {
                    stopwatch.Stop();
                    _logger.LogInformation(
                        "FormResponseUpdatedEmail SKIPPED: No respondent email, Duration={ElapsedMs}ms",
                        stopwatch.ElapsedMilliseconds);
                    return;
                }

                // Build response summary
                var responseSummary = BuildResponseSummary(response.Answers, form.Questions, maxQuestions: 5, maxAnswerLength: 100);

                // Phase 6A.116 Issue #8: Build edit URL with proper path (fixes 404 error)
                // Note: No token available for update emails (plaintext token is only returned on initial submission)
                // User must use token from original confirmation email or be logged in to edit
                var editUrl = _emailUrlHelper.BuildFormEditUrl(eventEntity.Id, domainEvent.FormId, accessToken: null);

                _logger.LogInformation(
                    "FormResponseUpdatedEmail: Generated edit URL: {EditUrl}, EventId: {EventId}, FormId: {FormId}",
                    editUrl, eventEntity.Id, domainEvent.FormId);

                // Create email parameters
                var emailParams = FormResponseEmailParams.CreateUpdate(
                    userName: response.RespondentName ?? "User",
                    userEmail: response.RespondentEmail,
                    eventId: eventEntity.Id,
                    eventTitle: eventEntity.Title.Value,
                    formTitle: form.Title,
                    responseSummary: responseSummary,
                    editFormUrl: editUrl,
                    eventStartDate: eventEntity.StartDate.GetValueOrDefault(), // Phase 8YA-2 TODO: param class should accept DateTime?
                    timeZoneId: eventEntity.TimeZoneId,
                    eventLocation: eventEntity.Location?.ToString() ?? string.Empty,
                    eventDetailsUrl: _emailUrlHelper.BuildEventDetailsUrl(eventEntity.Id),
                    updatedAt: domainEvent.OccurredAt
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

                // Phase 6A.116 Issue #4, #9: Add signup lists & forms URLs
                if (eventEntity.SignUpLists?.Any() == true)
                {
                    var signupListsUrl = _emailUrlHelper.BuildSignupListsUrl(eventEntity.Id);
                    emailParams.WithSignupListsUrl(signupListsUrl);

                    _logger.LogInformation(
                        "FormResponseUpdatedEmail: Added signup lists URL: {SignupListsUrl}",
                        signupListsUrl);
                }

                // Phase 6A.116 Issue #4: Always add signup forms URL since we're in a form response context
                // The event definitely has forms if we're sending a form response email
                var signupFormsUrl = _emailUrlHelper.BuildSignupFormsUrl(eventEntity.Id);
                emailParams.WithSignupFormsUrl(signupFormsUrl);

                _logger.LogInformation(
                    "FormResponseUpdatedEmail: Added signup forms URL: {SignupFormsUrl}",
                    signupFormsUrl);

                // Send email (fail-silent)
                var emailResult = await _typedEmailService.SendEmailAsync(emailParams, cancellationToken);

                stopwatch.Stop();

                if (emailResult.Success)
                {
                    _logger.LogInformation(
                        "FormResponseUpdatedEmail SUCCESS: Email sent to {RecipientEmail}, Duration={ElapsedMs}ms",
                        response.RespondentEmail, stopwatch.ElapsedMilliseconds);
                }
                else
                {
                    _logger.LogError(
                        "FormResponseUpdatedEmail FAILED: Email send failed - RecipientEmail={RecipientEmail}, Error={Error}, Duration={ElapsedMs}ms",
                        response.RespondentEmail, string.Join(", ", emailResult.Errors), stopwatch.ElapsedMilliseconds);
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                // FAIL-SILENT: Log error but don't throw
                _logger.LogError(ex,
                    "FormResponseUpdatedEmail EXCEPTION: Unexpected error - FormId={FormId}, ResponseId={ResponseId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    domainEvent.FormId, domainEvent.ResponseId, stopwatch.ElapsedMilliseconds, ex.Message);
            }
        }
    }

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

            if (answerText.Length > maxAnswerLength)
                answerText = $"{answerText.Substring(0, maxAnswerLength)}...";

            return $"<strong>{questionText}:</strong> {answerText}";
        });

        // Phase 6A.115 Issue #4: Use HTML line breaks instead of pipes for better email readability
        var summary = string.Join("<br/>", summaryParts);

        var remainingCount = answers.Count - maxQuestions;
        if (remainingCount > 0)
            summary += $"<br/><em>... and {remainingCount} more response{(remainingCount > 1 ? "s" : "")}</em>";

        return summary;
    }
}
