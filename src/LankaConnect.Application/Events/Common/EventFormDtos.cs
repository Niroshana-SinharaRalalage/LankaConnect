using LankaConnect.Domain.Events.Enums;
using LankaConnect.Modules.Forms.Domain;
using LankaConnect.Modules.Forms.Domain.Entities;
using LankaConnect.Modules.Forms.Domain.Enums;
using LankaConnect.Modules.Forms.Domain.DomainEvents;
using LankaConnect.Modules.Forms.Domain.Repositories;

namespace LankaConnect.Application.Events.Common;

/// <summary>
/// Summary DTO for event forms (list view - no questions loaded).
/// </summary>
public record EventFormDto
{
    public Guid Id { get; init; }
    public Guid EventId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public FormStatus Status { get; init; }
    public bool AllowMultipleResponses { get; init; }
    public DateTime? ResponseDeadline { get; init; }
    public int? MaxResponses { get; init; }
    public bool HasResponses { get; init; }
    public int QuestionCount { get; init; }
    public int ResponseCount { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
    /// <summary>
    /// Phase 6A.146: when true, the public /responses/public endpoint will
    /// return PII-redacted responses for this form; the event detail page
    /// renders a PublicFormResponsesSection card.
    /// </summary>
    public bool AllowAttendeesToViewResponses { get; init; }
}

/// <summary>
/// Detail DTO for event forms (includes questions).
/// </summary>
public record EventFormDetailDto
{
    public Guid Id { get; init; }
    public Guid EventId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public FormStatus Status { get; init; }
    public bool AllowMultipleResponses { get; init; }
    public DateTime? ResponseDeadline { get; init; }
    public int? MaxResponses { get; init; }
    public bool HasResponses { get; init; }
    public int ResponseCount { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public IReadOnlyList<FormQuestionDto> Questions { get; init; } = Array.Empty<FormQuestionDto>();
    /// <summary>
    /// Phase 6A.146: organizer-controlled visibility flag. UI uses this to
    /// initialize the "Allow event visitors to see responses" toggle on the
    /// form create/edit page AND to gate the PublicFormResponsesSection on
    /// the event detail page.
    /// </summary>
    public bool AllowAttendeesToViewResponses { get; init; }
}

/// <summary>
/// DTO for form questions.
/// </summary>
public record FormQuestionDto
{
    public Guid Id { get; init; }
    public string QuestionText { get; init; } = string.Empty;
    public FormQuestionType QuestionType { get; init; }
    public bool IsRequired { get; init; }
    public int SortOrder { get; init; }
    public string? HelpText { get; init; }
    public IReadOnlyList<QuestionOptionDto> Options { get; init; } = Array.Empty<QuestionOptionDto>();
}

/// <summary>
/// DTO for question options.
/// </summary>
public record QuestionOptionDto
{
    public Guid Id { get; init; }
    public string Text { get; init; } = string.Empty;
    public int SortOrder { get; init; }
}

/// <summary>
/// DTO for form responses.
/// </summary>
public record FormResponseDto
{
    public Guid Id { get; init; }
    public Guid EventFormId { get; init; }
    public string? RespondentName { get; init; }
    public string? RespondentEmail { get; init; }
    public Guid? RespondentUserId { get; init; }
    public DateTime SubmittedAt { get; init; }
    public IReadOnlyList<FormAnswerDto> Answers { get; init; } = Array.Empty<FormAnswerDto>();
}

/// <summary>
/// DTO for individual form answers.
/// </summary>
public record FormAnswerDto
{
    public Guid Id { get; init; }
    public Guid FormQuestionId { get; init; }
    public string QuestionTextSnapshot { get; init; } = string.Empty;
    public string? TextValue { get; init; }
    public IReadOnlyList<Guid> SelectedOptionIds { get; init; } = Array.Empty<Guid>();
    public IReadOnlyList<string> SelectedOptionTextSnapshots { get; init; } = Array.Empty<string>();
    public bool? BooleanValue { get; init; }
}

/// <summary>
/// Paginated response DTO for organizer response viewer.
/// </summary>
public record FormResponsesPagedDto
{
    public IReadOnlyList<FormResponseDto> Responses { get; init; } = Array.Empty<FormResponseDto>();
    public int TotalCount { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
}

// ==================== Phase 6A.146 — Public DTOs (PII-redacted) ====================
//
// These DTOs are returned by the [AllowAnonymous] GET /forms/{formId}/responses/public
// endpoint and are seen by any event visitor when the organizer has toggled
// AllowAttendeesToViewResponses on. PII is redacted by CONSTRUCTION — the
// respondent name, email, and user id are not properties on these types at
// all, so they cannot be accidentally re-introduced by a future projection
// edit. The reflection-based tests in GetPublicFormResponsesQueryHandlerTests
// pin that contract.

/// <summary>
/// Public form-response DTO. Surfaces the respondent's name (when provided)
/// because in a sign-up / RSVP context attribution is normal — "Niro K is
/// bringing biriyani" is not a privacy violation. Email and user-id are
/// still hidden: those are the actual contact-method PII whose exposure
/// the toggle's privacy promise covers.
///
/// Compile-time guarantee: <c>RespondentEmail</c> and <c>RespondentUserId</c>
/// are physically absent from this record. Reflection-asserted in the test
/// fixture so any future re-addition fails at runtime.
/// </summary>
public record PublicFormResponseDto
{
    public Guid Id { get; init; }
    /// <summary>
    /// Respondent's self-supplied name, surfaced verbatim. Null when the
    /// respondent (typically anonymous) skipped the optional name field;
    /// the UI falls back to <see cref="RespondentLabel"/> in that case.
    /// </summary>
    public string? RespondentName { get; init; }
    /// <summary>
    /// Ordinal label assigned by the handler ("Respondent 1", "Respondent 2", …)
    /// based on <c>SubmittedAt</c> ascending. Always present so the UI has a
    /// stable fallback when <see cref="RespondentName"/> is null.
    /// </summary>
    public string RespondentLabel { get; init; } = string.Empty;
    /// <summary>
    /// Calendar date of submission. Time-of-day is intentionally dropped to
    /// prevent timing-correlation attacks against WhatsApp / email blasts.
    /// </summary>
    public DateOnly SubmittedOn { get; init; }
    public IReadOnlyList<PublicFormAnswerDto> Answers { get; init; } = Array.Empty<PublicFormAnswerDto>();
}

/// <summary>
/// Public answer DTO. Answer values are preserved verbatim because they're
/// the whole point of the public view — the organizer is responsible for
/// authoring questions that don't request PII when the toggle is on.
/// </summary>
public record PublicFormAnswerDto
{
    public Guid QuestionId { get; init; }
    public string QuestionTextSnapshot { get; init; } = string.Empty;
    public string? TextValue { get; init; }
    public IReadOnlyList<string> SelectedOptionTextSnapshots { get; init; } = Array.Empty<string>();
    public bool? BooleanValue { get; init; }
}

/// <summary>
/// Top-level payload for the public responses endpoint.
/// </summary>
public record PublicFormResponsesDto
{
    public Guid FormId { get; init; }
    public string FormTitle { get; init; } = string.Empty;
    public int TotalCount { get; init; }
    public IReadOnlyList<PublicFormResponseDto> Responses { get; init; } = Array.Empty<PublicFormResponseDto>();
}
