using LankaConnect.Domain.Events.Enums;

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
    public EventFormStatus Status { get; init; }
    public bool AllowMultipleResponses { get; init; }
    public DateTime? ResponseDeadline { get; init; }
    public int? MaxResponses { get; init; }
    public bool HasResponses { get; init; }
    public int QuestionCount { get; init; }
    public int ResponseCount { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
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
    public EventFormStatus Status { get; init; }
    public bool AllowMultipleResponses { get; init; }
    public DateTime? ResponseDeadline { get; init; }
    public int? MaxResponses { get; init; }
    public bool HasResponses { get; init; }
    public int ResponseCount { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
    public IReadOnlyList<FormQuestionDto> Questions { get; init; } = Array.Empty<FormQuestionDto>();
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
