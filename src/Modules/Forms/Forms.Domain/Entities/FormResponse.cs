using LankaConnect.BuildingBlocks.Domain;
using LankaConnect.Products.LankaEvents.Domain;
using LankaConnect.Modules.Forms.Domain.DomainEvents;

namespace LankaConnect.Modules.Forms.Domain.Entities;

/// <summary>
/// Aggregate root representing a respondent's submission to an event form.
/// Separate aggregate from Form to handle unbounded growth and concurrent submissions.
///
/// Architect decision: Independent aggregate because:
/// 1. Responses can grow to thousands (unbounded)
/// 2. Concurrent submissions should not contend on the same aggregate
/// 3. Read patterns need pagination (not full collection loading)
///
/// Anonymous respondents are identified by a cryptographic access token
/// (stored as SHA256 hash). This token is returned on submission and used for editing.
/// </summary>
public class FormResponse : LegacyBaseEntity
{
    public const int MaxEmailLength = 255;
    public const int MaxNameLength = 200;

    private readonly List<FormAnswer> _answers = new();

    /// <summary>
    /// FK to the form this response belongs to.
    /// </summary>
    public Guid EventFormId { get; private set; }

    /// <summary>
    /// Denormalized FK to the event (for query efficiency without joining through Form).
    /// </summary>
    public Guid EventId { get; private set; }

    /// <summary>
    /// SHA256 hash of the access token. Used for anonymous respondent identification.
    /// The plaintext token is returned to the respondent on submission and never stored.
    /// </summary>
    public string AccessTokenHash { get; private set; } = string.Empty;

    /// <summary>
    /// Email of the respondent (optional, used for identification and editing).
    /// </summary>
    public string? RespondentEmail { get; private set; }

    /// <summary>
    /// Name of the respondent (optional).
    /// </summary>
    public string? RespondentName { get; private set; }

    /// <summary>
    /// User ID if the respondent is a logged-in LankaConnect member.
    /// Null for anonymous respondents.
    /// </summary>
    public Guid? RespondentUserId { get; private set; }

    /// <summary>
    /// When the response was submitted.
    /// </summary>
    public DateTime SubmittedAt { get; private set; }

    /// <summary>
    /// All answers in this response.
    /// </summary>
    public IReadOnlyList<FormAnswer> Answers => _answers.AsReadOnly();

    // EF Core constructor
    private FormResponse()
    {
    }

    private FormResponse(
        Guid eventFormId,
        Guid eventId,
        string accessTokenHash,
        string? respondentEmail,
        string? respondentName,
        Guid? respondentUserId)
    {
        EventFormId = eventFormId;
        EventId = eventId;
        AccessTokenHash = accessTokenHash;
        RespondentEmail = respondentEmail;
        RespondentName = respondentName;
        RespondentUserId = respondentUserId;
        SubmittedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Creates a new FormResponse.
    /// Phase 6A.106 Update: Added accessToken parameter for email edit link generation.
    /// </summary>
    public static Result<FormResponse> Create(
        Guid eventFormId,
        Guid eventId,
        string accessTokenHash,
        string? accessToken,  // Plaintext token for email edit link (in-memory only)
        string? respondentEmail = null,
        string? respondentName = null,
        Guid? respondentUserId = null)
    {
        if (eventFormId == Guid.Empty)
            return Result<FormResponse>.Failure("Event form ID cannot be empty");

        if (eventId == Guid.Empty)
            return Result<FormResponse>.Failure("Event ID cannot be empty");

        if (string.IsNullOrWhiteSpace(accessTokenHash))
            return Result<FormResponse>.Failure("Access token hash cannot be empty");

        if (respondentEmail != null)
        {
            respondentEmail = respondentEmail.Trim();
            if (respondentEmail.Length > MaxEmailLength)
                return Result<FormResponse>.Failure($"Email cannot exceed {MaxEmailLength} characters");
            if (!respondentEmail.Contains('@'))
                return Result<FormResponse>.Failure("Invalid email format");
        }

        if (respondentName != null)
        {
            respondentName = respondentName.Trim();
            if (respondentName.Length > MaxNameLength)
                return Result<FormResponse>.Failure($"Name cannot exceed {MaxNameLength} characters");
            if (string.IsNullOrWhiteSpace(respondentName))
                respondentName = null;
        }

        var response = new FormResponse(
            eventFormId, eventId, accessTokenHash,
            respondentEmail, respondentName, respondentUserId);

        response.RaiseDomainEvent(new FormResponseSubmittedEvent(
            eventFormId, response.Id, respondentEmail, accessToken, DateTime.UtcNow));

        return Result<FormResponse>.Success(response);
    }

    /// <summary>
    /// Adds an answer to this response.
    /// </summary>
    public Result AddAnswer(
        Guid questionId,
        string questionTextSnapshot,
        string? textValue = null,
        List<Guid>? selectedOptionIds = null,
        List<string>? selectedOptionTextSnapshots = null,
        bool? booleanValue = null)
    {
        // Check for duplicate question answers
        if (_answers.Any(a => a.FormQuestionId == questionId))
            return Result.Failure($"An answer for question {questionId} already exists in this response");

        var answerResult = FormAnswer.Create(
            Id, questionId, questionTextSnapshot,
            textValue, selectedOptionIds, selectedOptionTextSnapshots, booleanValue);

        if (answerResult.IsFailure)
            return Result.Failure(answerResult.Error);

        _answers.Add(answerResult.Value);
        return Result.Success();
    }

    /// <summary>
    /// Updates an existing answer (for editing a response before deadline).
    /// </summary>
    public Result UpdateAnswer(
        Guid questionId,
        string? textValue = null,
        List<Guid>? selectedOptionIds = null,
        List<string>? selectedOptionTextSnapshots = null,
        bool? booleanValue = null)
    {
        var answer = _answers.FirstOrDefault(a => a.FormQuestionId == questionId);
        if (answer == null)
            return Result.Failure($"No answer found for question {questionId}");

        var updateResult = answer.Update(textValue, selectedOptionIds, selectedOptionTextSnapshots, booleanValue);
        if (updateResult.IsFailure)
            return updateResult;


        // Phase 6A.114: Event raising moved to RaiseUpdatedEventWithContext()
        // Command handler will call it once after all answer updates complete

        return Result.Success();
    }

    /// <summary>
    /// Gets the answer for a specific question.
    /// </summary>
    public FormAnswer? GetAnswer(Guid questionId) => _answers.FirstOrDefault(a => a.FormQuestionId == questionId);

    /// <summary>
    /// Checks if this response can still be edited (deadline not passed).
    /// </summary>
    public bool CanEdit(DateTime? responseDeadline)
    {
        if (!responseDeadline.HasValue)
            return true; // No deadline means always editable

        return DateTime.UtcNow <= responseDeadline.Value;
    }

    /// <summary>
    /// Raises updated event with full context (Form + Event entities).
    /// Phase 6A.114: Performance optimization - pass already-loaded data to email handler
    /// to eliminate duplicate queries. Reduces email processing time from 40s → 5-8s.
    ///
    /// MUST be called by command handler AFTER all answer updates are complete.
    /// Command handler loads Form + Event once, then passes here to avoid re-querying.
    /// </summary>
    /// <param name="form">Form already loaded by command handler (with questions)</param>
    /// <param name="event">Event already loaded by command handler</param>
    public Result RaiseUpdatedEventWithContext(Form form, Event @event)
    {
        if (form == null)
            return Result.Failure("Form is required to raise updated event");

        if (@event == null)
            return Result.Failure("Event is required to raise updated event");

        RaiseDomainEvent(new FormResponseUpdatedEvent(
            EventFormId, Id, DateTime.UtcNow, form, @event));

        return Result.Success();
    }

    /// <summary>
    /// Raises deletion domain event. Must be called BEFORE physical deletion
    /// so event handlers can access entity properties.
    /// Phase 6A.106: Enable email notifications before hard delete.
    /// Architect Review: Approved - renamed from MarkAsDeleted() for clarity (doesn't mark, just raises event).
    /// </summary>
    public Result RaiseDeletedEvent()
    {
        // Raise domain event BEFORE actual deletion (handler needs access to properties)
        RaiseDomainEvent(new FormResponseDeletedEvent(
            EventFormId, Id, RespondentEmail, DateTime.UtcNow));

        return Result.Success();
    }
}
