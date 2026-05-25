using FluentAssertions;
using LankaConnect.Application.Events.Common;
using LankaConnect.Application.Events.Queries.GetPublicFormResponses;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events.Entities;
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Domain.Events.Repositories;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LankaConnect.Application.Tests.Events.Queries;

/// <summary>
/// Phase 6A.146 — TDD tests for GetPublicFormResponsesQueryHandler.
///
/// The public read path that an event detail page calls when an organizer has
/// flipped AllowAttendeesToViewResponses to true. Defense-in-depth contract:
///   - Form not found OR belongs to a different event → 404
///   - AllowAttendeesToViewResponses=false → 404 (NOT 403 — don't leak toggle state)
///   - Status not in {Active, Closed} → 404 (Draft/Archived stay private)
///   - Valid call → PII-redacted DTOs ordered by SubmittedAt ASC with
///     ordinal "Respondent N" labels and DateOnly SubmittedOn
///
/// These tests pin both the security gates and the projection contract.
/// PublicFormResponseDto MUST NOT include RespondentName / RespondentEmail /
/// RespondentUserId properties at all — compile-time guarantee (assertion via
/// reflection in test 14).
/// </summary>
public class GetPublicFormResponsesQueryHandlerTests
{
    private readonly Mock<IEventFormRepository> _formRepository = new();
    private readonly Mock<IFormResponseRepository> _responseRepository = new();
    private readonly Mock<ILogger<GetPublicFormResponsesQueryHandler>> _logger = new();

    private readonly Guid _eventId = Guid.NewGuid();
    private readonly Guid _formId = Guid.NewGuid();

    private GetPublicFormResponsesQueryHandler CreateHandler() =>
        new(_formRepository.Object, _responseRepository.Object, _logger.Object);

    private static EventForm BuildForm(
        Guid eventId,
        bool allowAttendeesToViewResponses,
        EventFormStatus status = EventFormStatus.Active)
    {
        var form = EventForm.Create(
            eventId, "Survey", description: null,
            allowMultipleResponses: false, responseDeadline: null, maxResponses: null,
            allowAttendeesToViewResponses: allowAttendeesToViewResponses).Value;

        // Force status via the lifecycle methods. Create yields Draft.
        if (status == EventFormStatus.Active || status == EventFormStatus.Closed
            || status == EventFormStatus.Archived)
        {
            form.AddQuestion("Q1?", FormQuestionType.ShortText, false, 0);
            form.Publish();  // Draft → Active
            if (status == EventFormStatus.Closed) form.Close();
            if (status == EventFormStatus.Archived)
            {
                form.Close();
                form.Archive();
            }
        }

        return form;
    }

    private static FormResponse BuildResponse(
        Guid formId, Guid eventId, DateTime submittedAt,
        string? respondentEmail = "user@example.com",
        string? respondentName = "Niro K",
        Guid? respondentUserId = null,
        string? answer = "my answer",
        Guid? questionId = null)
    {
        var resp = FormResponse.Create(
            formId, eventId,
            accessTokenHash: "hash-" + Guid.NewGuid(),
            accessToken: "plain-" + Guid.NewGuid(),
            respondentEmail: respondentEmail,
            respondentName: respondentName,
            respondentUserId: respondentUserId).Value;

        if (answer is not null)
            resp.AddAnswer(
                questionId ?? Guid.NewGuid(),
                questionTextSnapshot: "Q1?",
                textValue: answer);

        // Backfill SubmittedAt via reflection — entity sets DateTime.UtcNow at ctor time
        // and there's no public setter; we need deterministic ordering for ordinal tests.
        typeof(FormResponse).GetProperty(nameof(FormResponse.SubmittedAt))!
            .SetValue(resp, submittedAt);

        return resp;
    }

    [Fact]
    public async Task Handle_FormNotFound_Returns_NotFound()
    {
        _formRepository
            .Setup(r => r.GetByIdAsync(_formId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((EventForm?)null);

        var result = await CreateHandler().Handle(
            new GetPublicFormResponsesQuery(_eventId, _formId), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.ErrorKind.Should().Be(ErrorKind.NotFound);
    }

    [Fact]
    public async Task Handle_FormBelongsToDifferentEvent_Returns_NotFound()
    {
        // Form exists but its EventId is a totally different event.
        var otherEventId = Guid.NewGuid();
        _formRepository
            .Setup(r => r.GetByIdAsync(_formId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildForm(otherEventId, allowAttendeesToViewResponses: true));

        var result = await CreateHandler().Handle(
            new GetPublicFormResponsesQuery(_eventId, _formId), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.ErrorKind.Should().Be(ErrorKind.NotFound);
    }

    [Fact]
    public async Task Handle_VisibilityFlagOff_Returns_NotFound_NotForbidden()
    {
        // Defense-in-depth: must be 404 not 403, so we don't leak the toggle's existence.
        _formRepository
            .Setup(r => r.GetByIdAsync(_formId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildForm(_eventId, allowAttendeesToViewResponses: false));

        var result = await CreateHandler().Handle(
            new GetPublicFormResponsesQuery(_eventId, _formId), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.ErrorKind.Should().Be(ErrorKind.NotFound);
        result.ErrorKind.Should().NotBe(ErrorKind.Forbidden);
    }

    [Fact]
    public async Task Handle_FormStatusDraft_Returns_NotFound()
    {
        _formRepository
            .Setup(r => r.GetByIdAsync(_formId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildForm(_eventId, allowAttendeesToViewResponses: true, EventFormStatus.Draft));

        var result = await CreateHandler().Handle(
            new GetPublicFormResponsesQuery(_eventId, _formId), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.ErrorKind.Should().Be(ErrorKind.NotFound);
    }

    [Fact]
    public async Task Handle_FormStatusArchived_Returns_NotFound()
    {
        _formRepository
            .Setup(r => r.GetByIdAsync(_formId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildForm(_eventId, allowAttendeesToViewResponses: true, EventFormStatus.Archived));

        var result = await CreateHandler().Handle(
            new GetPublicFormResponsesQuery(_eventId, _formId), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.ErrorKind.Should().Be(ErrorKind.NotFound);
    }

    [Fact]
    public async Task Handle_StatusActive_VisibilityOn_ReturnsResponses()
    {
        _formRepository
            .Setup(r => r.GetByIdAsync(_formId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildForm(_eventId, allowAttendeesToViewResponses: true, EventFormStatus.Active));

        _responseRepository
            .Setup(r => r.GetPaginatedAsync(_formId, 1, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(((IReadOnlyList<FormResponse>)new List<FormResponse>
            {
                BuildResponse(_formId, _eventId, new DateTime(2026, 5, 10, 9, 30, 0, DateTimeKind.Utc))
            }, 1));

        var result = await CreateHandler().Handle(
            new GetPublicFormResponsesQuery(_eventId, _formId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCount.Should().Be(1);
        result.Value.Responses.Should().HaveCount(1);
    }

    [Fact]
    public async Task Handle_StatusClosed_VisibilityOn_ReturnsResponses()
    {
        // Architect locked decision: Closed forms still publish (historical record).
        _formRepository
            .Setup(r => r.GetByIdAsync(_formId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildForm(_eventId, allowAttendeesToViewResponses: true, EventFormStatus.Closed));

        _responseRepository
            .Setup(r => r.GetPaginatedAsync(_formId, 1, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(((IReadOnlyList<FormResponse>)new List<FormResponse>
            {
                BuildResponse(_formId, _eventId, new DateTime(2026, 5, 10, 9, 30, 0, DateTimeKind.Utc))
            }, 1));

        var result = await CreateHandler().Handle(
            new GetPublicFormResponsesQuery(_eventId, _formId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCount.Should().Be(1);
    }

    [Fact]
    public void PublicFormResponseDto_Surfaces_RespondentName()
    {
        // 2026-05-15 product correction: respondent NAME is shown publicly when
        // provided. Names in a sign-up context (e.g., "Niro K bringing biriyani")
        // are normal attribution, not personal contact info. Email + userId are
        // still hidden — those are the actual contact-method PII.
        typeof(PublicFormResponseDto).GetProperty("RespondentName").Should().NotBeNull();
    }

    [Fact]
    public void PublicFormResponseDto_DoesNotExpose_RespondentEmail()
    {
        // Email is the contact-method PII we hide. Compile-time guarantee.
        typeof(PublicFormResponseDto).GetProperty("RespondentEmail").Should().BeNull();
    }

    [Fact]
    public void PublicFormResponseDto_DoesNotExpose_RespondentUserId()
    {
        // UserId would let anyone correlate a response back to a member profile
        // page. Kept hidden so the surfaced name doesn't act as a profile link.
        typeof(PublicFormResponseDto).GetProperty("RespondentUserId").Should().BeNull();
    }

    [Fact]
    public async Task Handle_VisibilityOn_SurfacesRespondentName_WhenProvided()
    {
        _formRepository
            .Setup(r => r.GetByIdAsync(_formId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildForm(_eventId, allowAttendeesToViewResponses: true, EventFormStatus.Active));

        _responseRepository
            .Setup(r => r.GetPaginatedAsync(_formId, 1, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(((IReadOnlyList<FormResponse>)new List<FormResponse>
            {
                BuildResponse(_formId, _eventId, new DateTime(2026, 5, 10, 9, 0, 0, DateTimeKind.Utc),
                    respondentName: "Niro K"),
            }, 1));

        var result = await CreateHandler().Handle(
            new GetPublicFormResponsesQuery(_eventId, _formId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Responses[0].RespondentName.Should().Be("Niro K");
        // Ordinal label remains so the UI can fall back when name is null.
        result.Value.Responses[0].RespondentLabel.Should().Be("Respondent 1");
    }

    [Fact]
    public async Task Handle_VisibilityOn_RespondentName_IsNull_WhenNotProvided()
    {
        // Anonymous respondent who skipped the optional name field — UI is
        // expected to fall back to the ordinal label.
        _formRepository
            .Setup(r => r.GetByIdAsync(_formId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildForm(_eventId, allowAttendeesToViewResponses: true, EventFormStatus.Active));

        _responseRepository
            .Setup(r => r.GetPaginatedAsync(_formId, 1, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(((IReadOnlyList<FormResponse>)new List<FormResponse>
            {
                BuildResponse(_formId, _eventId, new DateTime(2026, 5, 10, 9, 0, 0, DateTimeKind.Utc),
                    respondentName: null),
            }, 1));

        var result = await CreateHandler().Handle(
            new GetPublicFormResponsesQuery(_eventId, _formId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Responses[0].RespondentName.Should().BeNull();
        result.Value.Responses[0].RespondentLabel.Should().Be("Respondent 1");
    }

    [Fact]
    public async Task Handle_AssignsOrdinalLabels_BySubmittedAtAsc()
    {
        _formRepository
            .Setup(r => r.GetByIdAsync(_formId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildForm(_eventId, allowAttendeesToViewResponses: true, EventFormStatus.Active));

        // Build responses in reverse-time order — handler must re-sort ASC before labeling.
        _responseRepository
            .Setup(r => r.GetPaginatedAsync(_formId, 1, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(((IReadOnlyList<FormResponse>)new List<FormResponse>
            {
                BuildResponse(_formId, _eventId, new DateTime(2026, 5, 12, 9, 0, 0, DateTimeKind.Utc), answer: "third"),
                BuildResponse(_formId, _eventId, new DateTime(2026, 5, 10, 9, 0, 0, DateTimeKind.Utc), answer: "first"),
                BuildResponse(_formId, _eventId, new DateTime(2026, 5, 11, 9, 0, 0, DateTimeKind.Utc), answer: "second"),
            }, 3));

        var result = await CreateHandler().Handle(
            new GetPublicFormResponsesQuery(_eventId, _formId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Responses.Should().HaveCount(3);
        result.Value.Responses[0].RespondentLabel.Should().Be("Respondent 1");
        result.Value.Responses[1].RespondentLabel.Should().Be("Respondent 2");
        result.Value.Responses[2].RespondentLabel.Should().Be("Respondent 3");
        // Verify the ordering is by SubmittedAt ASC (the "first" entry's answer
        // must surface in the row labelled Respondent 1).
        result.Value.Responses[0].Answers[0].TextValue.Should().Be("first");
        result.Value.Responses[1].Answers[0].TextValue.Should().Be("second");
        result.Value.Responses[2].Answers[0].TextValue.Should().Be("third");
    }

    [Fact]
    public async Task Handle_PreservesAnswerValues_TextAndQuestionSnapshot()
    {
        var questionId = Guid.NewGuid();
        _formRepository
            .Setup(r => r.GetByIdAsync(_formId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildForm(_eventId, allowAttendeesToViewResponses: true, EventFormStatus.Active));

        _responseRepository
            .Setup(r => r.GetPaginatedAsync(_formId, 1, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(((IReadOnlyList<FormResponse>)new List<FormResponse>
            {
                BuildResponse(_formId, _eventId, new DateTime(2026, 5, 10, 9, 0, 0, DateTimeKind.Utc),
                    answer: "Bringing biriyani", questionId: questionId)
            }, 1));

        var result = await CreateHandler().Handle(
            new GetPublicFormResponsesQuery(_eventId, _formId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var answer = result.Value.Responses[0].Answers[0];
        answer.QuestionId.Should().Be(questionId);
        answer.QuestionTextSnapshot.Should().Be("Q1?");
        answer.TextValue.Should().Be("Bringing biriyani");
    }

    [Fact]
    public async Task Handle_SubmittedOn_IsDateOnly_NoTimeOfDay()
    {
        // Architect-locked decision: prevent timing-correlation attacks by truncating
        // SubmittedAt to a calendar date in the public DTO.
        var submittedAt = new DateTime(2026, 5, 10, 14, 32, 17, DateTimeKind.Utc);

        _formRepository
            .Setup(r => r.GetByIdAsync(_formId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildForm(_eventId, allowAttendeesToViewResponses: true, EventFormStatus.Active));

        _responseRepository
            .Setup(r => r.GetPaginatedAsync(_formId, 1, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(((IReadOnlyList<FormResponse>)new List<FormResponse>
            {
                BuildResponse(_formId, _eventId, submittedAt)
            }, 1));

        var result = await CreateHandler().Handle(
            new GetPublicFormResponsesQuery(_eventId, _formId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Responses[0].SubmittedOn.Should().Be(new DateOnly(2026, 5, 10));
    }

    [Fact]
    public async Task Handle_ReturnsFormTitle_OnSuccess()
    {
        // Capture the form's actual Id so the assertion is against the entity's
        // canonical identifier rather than our mock-key Guid (which need not match
        // the entity's Id-on-Create since EventForm.Create generates a fresh GUID).
        var form = BuildForm(_eventId, allowAttendeesToViewResponses: true, EventFormStatus.Active);
        _formRepository
            .Setup(r => r.GetByIdAsync(_formId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(form);

        _responseRepository
            .Setup(r => r.GetPaginatedAsync(_formId, 1, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(((IReadOnlyList<FormResponse>)new List<FormResponse>(), 0));

        var result = await CreateHandler().Handle(
            new GetPublicFormResponsesQuery(_eventId, _formId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        // Title is the form's persisted title — UI uses this for the section card header.
        result.Value.FormTitle.Should().Be("Survey");
        result.Value.FormId.Should().Be(form.Id);
    }
}
