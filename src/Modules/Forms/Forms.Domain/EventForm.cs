using LankaConnect.Domain.Common;
using LankaConnect.Modules.Forms.Domain.Entities;
using LankaConnect.Modules.Forms.Domain.Enums;
using LankaConnect.Modules.Forms.Domain.DomainEvents;
using LankaConnect.Domain.Events.ValueObjects;

namespace LankaConnect.Modules.Forms.Domain;

/// <summary>
/// Aggregate root for custom event forms (Google Forms-like surveys).
/// Independent aggregate with EventId FK reference (not a child of Event aggregate).
/// Architect decision: Separate aggregate to avoid bloating Event entity (2059 lines, 10 collections).
///
/// Lifecycle: Draft -> Active -> Closed -> Archived
/// Only Active forms accept responses.
/// </summary>
public class EventForm : LegacyBaseEntity
{
    public const int MaxTitleLength = 200;
    public const int MaxDescriptionLength = 2000;

    private readonly List<FormQuestion> _questions = new();

    /// <summary>
    /// FK to the Event this form belongs to. Not a navigation property.
    /// </summary>
    public Guid EventId { get; private set; }

    public string Title { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public EventFormStatus Status { get; private set; }
    public bool AllowMultipleResponses { get; private set; }
    public DateTime? ResponseDeadline { get; private set; }
    public int? MaxResponses { get; private set; }

    /// <summary>
    /// Denormalized flag indicating at least one response has been submitted.
    /// Used to enforce business rules (e.g., cannot delete questions when responses exist).
    /// Updated by the command handler when first response is submitted.
    /// </summary>
    public bool HasResponses { get; private set; }

    /// <summary>
    /// Phase 6A.146 — when true, anonymous event visitors can view a PII-redacted
    /// list of all submitted responses on the public event detail page. Personal
    /// fields (respondent name, email, user id) are ALWAYS stripped at the
    /// projection layer; question/answer values are preserved verbatim. Default
    /// false (existing rows in DB get false via column default). The public
    /// endpoint additionally gates on Status ∈ {Active, Closed} so Draft and
    /// Archived forms never leak responses even when the flag is true.
    /// </summary>
    public bool AllowAttendeesToViewResponses { get; private set; }

    /// <summary>
    /// Questions belonging to this form, ordered by SortOrder.
    /// </summary>
    public IReadOnlyList<FormQuestion> Questions => _questions.AsReadOnly();

    // EF Core constructor
    private EventForm()
    {
    }

    private EventForm(
        Guid eventId,
        string title,
        string? description,
        bool allowMultipleResponses,
        DateTime? responseDeadline,
        int? maxResponses,
        bool allowAttendeesToViewResponses)
    {
        EventId = eventId;
        Title = title;
        Description = description;
        Status = EventFormStatus.Draft;
        AllowMultipleResponses = allowMultipleResponses;
        ResponseDeadline = responseDeadline;
        MaxResponses = maxResponses;
        HasResponses = false;
        AllowAttendeesToViewResponses = allowAttendeesToViewResponses;
    }

    /// <summary>
    /// Creates a new EventForm in Draft status.
    /// Phase 6A.146: <paramref name="allowAttendeesToViewResponses"/> is appended at the
    /// END of the signature as an optional parameter so positional callers (including
    /// the integration test suite) continue compiling unchanged.
    /// </summary>
    public static Result<EventForm> Create(
        Guid eventId,
        string title,
        string? description = null,
        bool allowMultipleResponses = false,
        DateTime? responseDeadline = null,
        int? maxResponses = null,
        bool allowAttendeesToViewResponses = false)
    {
        if (eventId == Guid.Empty)
            return Result<EventForm>.Failure("Event ID cannot be empty");

        if (string.IsNullOrWhiteSpace(title))
            return Result<EventForm>.Failure("Form title cannot be empty");

        title = title.Trim();

        if (title.Length > MaxTitleLength)
            return Result<EventForm>.Failure($"Form title cannot exceed {MaxTitleLength} characters");

        if (description != null)
        {
            description = description.Trim();
            if (description.Length > MaxDescriptionLength)
                return Result<EventForm>.Failure($"Form description cannot exceed {MaxDescriptionLength} characters");
            if (string.IsNullOrWhiteSpace(description))
                description = null;
        }

        if (maxResponses.HasValue && maxResponses.Value < 1)
            return Result<EventForm>.Failure("Maximum responses must be at least 1");

        if (responseDeadline.HasValue && responseDeadline.Value.Kind != DateTimeKind.Utc)
            responseDeadline = DateTime.SpecifyKind(responseDeadline.Value, DateTimeKind.Utc);

        var form = new EventForm(eventId, title, description, allowMultipleResponses, responseDeadline, maxResponses, allowAttendeesToViewResponses);

        form.RaiseDomainEvent(new EventFormCreatedEvent(eventId, form.Id, title, DateTime.UtcNow));

        return Result<EventForm>.Success(form);
    }

    #region Question Management

    /// <summary>
    /// Adds a question to this form.
    /// </summary>
    public Result<FormQuestion> AddQuestion(
        string questionText,
        FormQuestionType questionType,
        bool isRequired,
        int sortOrder,
        IEnumerable<QuestionOption>? options = null,
        string? helpText = null)
    {
        var questionResult = FormQuestion.Create(
            Id, questionText, questionType, isRequired, sortOrder, options, helpText);

        if (questionResult.IsFailure)
            return questionResult;

        _questions.Add(questionResult.Value);

        return questionResult;
    }

    /// <summary>
    /// Updates an existing question. Allowed even when responses exist for safe edits
    /// (text changes, help text, reorder). Type changes are blocked when responses exist.
    /// </summary>
    public Result UpdateQuestion(
        Guid questionId,
        string questionText,
        FormQuestionType questionType,
        bool isRequired,
        int sortOrder,
        IEnumerable<QuestionOption>? options = null,
        string? helpText = null)
    {
        var question = _questions.FirstOrDefault(q => q.Id == questionId);
        if (question == null)
            return Result.Failure($"Question with ID {questionId} not found");

        // Block type changes when responses exist
        if (HasResponses && question.QuestionType != questionType)
            return Result.Failure("Cannot change question type after responses have been submitted");

        return question.UpdateDetails(questionText, questionType, isRequired, sortOrder, options, helpText);
    }

    /// <summary>
    /// Removes a question from the form. Blocked when responses exist.
    /// </summary>
    public Result RemoveQuestion(Guid questionId)
    {
        if (HasResponses)
            return Result.Failure("Cannot delete questions after responses have been submitted");

        var question = _questions.FirstOrDefault(q => q.Id == questionId);
        if (question == null)
            return Result.Failure($"Question with ID {questionId} not found");

        _questions.Remove(question);

        return Result.Success();
    }

    /// <summary>
    /// Reorders questions by setting SortOrder based on the provided ordered list of question IDs.
    /// </summary>
    public Result ReorderQuestions(IList<Guid> questionIdsInOrder)
    {
        if (questionIdsInOrder == null || questionIdsInOrder.Count == 0)
            return Result.Failure("Question IDs list cannot be empty");

        if (questionIdsInOrder.Count != _questions.Count)
            return Result.Failure("All question IDs must be provided for reordering");

        var questionMap = _questions.ToDictionary(q => q.Id);
        foreach (var id in questionIdsInOrder)
        {
            if (!questionMap.ContainsKey(id))
                return Result.Failure($"Question with ID {id} not found in this form");
        }

        for (int i = 0; i < questionIdsInOrder.Count; i++)
        {
            questionMap[questionIdsInOrder[i]].UpdateSortOrder(i);
        }

        return Result.Success();
    }

    /// <summary>
    /// Gets a specific question by ID.
    /// </summary>
    public FormQuestion? GetQuestion(Guid questionId) => _questions.FirstOrDefault(q => q.Id == questionId);

    /// <summary>
    /// Gets the total number of questions.
    /// </summary>
    public int GetQuestionCount() => _questions.Count;

    #endregion

    #region Form Details Management

    /// <summary>
    /// Updates form metadata (title, description, settings).
    /// Phase 6A.146: <paramref name="allowAttendeesToViewResponses"/> is appended at
    /// the END of the signature as a NULLABLE optional parameter — null means "leave
    /// the visibility flag unchanged", preserving exact behavior for all existing
    /// positional callers (architect's correction C1). Per architect's correction C2,
    /// there is no status guard here: the flag can be toggled in Draft, Active, or
    /// Closed states — the public endpoint enforces the Active/Closed gate separately.
    /// </summary>
    public Result UpdateDetails(
        string title,
        string? description,
        bool allowMultipleResponses,
        DateTime? responseDeadline,
        int? maxResponses,
        bool? allowAttendeesToViewResponses = null)
    {
        if (string.IsNullOrWhiteSpace(title))
            return Result.Failure("Form title cannot be empty");

        title = title.Trim();

        if (title.Length > MaxTitleLength)
            return Result.Failure($"Form title cannot exceed {MaxTitleLength} characters");

        if (description != null)
        {
            description = description.Trim();
            if (description.Length > MaxDescriptionLength)
                return Result.Failure($"Form description cannot exceed {MaxDescriptionLength} characters");
            if (string.IsNullOrWhiteSpace(description))
                description = null;
        }

        if (maxResponses.HasValue && maxResponses.Value < 1)
            return Result.Failure("Maximum responses must be at least 1");

        if (responseDeadline.HasValue && responseDeadline.Value.Kind != DateTimeKind.Utc)
            responseDeadline = DateTime.SpecifyKind(responseDeadline.Value, DateTimeKind.Utc);

        Title = title;
        Description = description;
        AllowMultipleResponses = allowMultipleResponses;
        ResponseDeadline = responseDeadline;
        MaxResponses = maxResponses;
        if (allowAttendeesToViewResponses.HasValue)
            AllowAttendeesToViewResponses = allowAttendeesToViewResponses.Value;

        return Result.Success();
    }

    #endregion

    #region Status Lifecycle

    /// <summary>
    /// Transitions from Draft to Active. Must have at least one question.
    /// </summary>
    public Result Publish()
    {
        if (Status != EventFormStatus.Draft)
            return Result.Failure("Only Draft forms can be published");

        if (_questions.Count == 0)
            return Result.Failure("Cannot publish a form with no questions");

        Status = EventFormStatus.Active;

        RaiseDomainEvent(new EventFormPublishedEvent(EventId, Id, DateTime.UtcNow));

        return Result.Success();
    }

    /// <summary>
    /// Transitions from Active to Closed. No more responses accepted.
    /// </summary>
    public Result Close()
    {
        if (Status != EventFormStatus.Active)
            return Result.Failure("Only Active forms can be closed");

        Status = EventFormStatus.Closed;

        RaiseDomainEvent(new EventFormClosedEvent(EventId, Id, DateTime.UtcNow));

        return Result.Success();
    }

    /// <summary>
    /// Transitions from Closed back to Active.
    /// </summary>
    public Result Reopen()
    {
        if (Status != EventFormStatus.Closed)
            return Result.Failure("Only Closed forms can be reopened");

        Status = EventFormStatus.Active;

        return Result.Success();
    }

    /// <summary>
    /// Archives the form (hidden from lists).
    /// </summary>
    public Result Archive()
    {
        if (Status == EventFormStatus.Archived)
            return Result.Failure("Form is already archived");

        Status = EventFormStatus.Archived;

        return Result.Success();
    }

    /// <summary>
    /// Checks if the form is currently accepting responses.
    /// Must be Active and deadline not yet passed.
    /// MaxResponses check is done at the application layer via a COUNT query.
    /// </summary>
    public bool IsAcceptingResponses()
    {
        if (Status != EventFormStatus.Active)
            return false;

        if (ResponseDeadline.HasValue && DateTime.UtcNow > ResponseDeadline.Value)
            return false;

        return true;
    }

    /// <summary>
    /// Marks that at least one response has been submitted.
    /// Called by the command handler after first response is saved.
    /// </summary>
    public void MarkHasResponses()
    {
        if (!HasResponses)
        {
            HasResponses = true;
        }
    }

    #endregion
}
