using FluentAssertions;
using LankaConnect.Domain.Events.Entities;
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Domain.Events.ValueObjects;
using LankaConnect.Domain.Events.DomainEvents;
using Xunit;

namespace LankaConnect.Domain.Tests.Events.Entities;

/// <summary>
/// TDD tests for EventForm aggregate root.
/// Tests creation, validation, question management, and lifecycle transitions.
/// </summary>
public class EventFormTests
{
    private readonly Guid _eventId = Guid.NewGuid();

    #region Creation Tests

    [Fact]
    public void Create_WithValidData_Should_Return_Success()
    {
        var result = EventForm.Create(_eventId, "Registration Survey");

        result.IsSuccess.Should().BeTrue();
        result.Value.EventId.Should().Be(_eventId);
        result.Value.Title.Should().Be("Registration Survey");
        result.Value.Status.Should().Be(EventFormStatus.Draft);
        result.Value.HasResponses.Should().BeFalse();
        result.Value.AllowMultipleResponses.Should().BeFalse();
        result.Value.ResponseDeadline.Should().BeNull();
        result.Value.MaxResponses.Should().BeNull();
    }

    [Fact]
    public void Create_Should_Raise_EventFormCreatedEvent()
    {
        var result = EventForm.Create(_eventId, "Test Form");

        result.IsSuccess.Should().BeTrue();
        result.Value.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<EventFormCreatedEvent>();
        var domainEvent = (EventFormCreatedEvent)result.Value.DomainEvents.First();
        domainEvent.EventId.Should().Be(_eventId);
        domainEvent.FormId.Should().Be(result.Value.Id);
        domainEvent.Title.Should().Be("Test Form");
    }

    [Fact]
    public void Create_WithAllOptions_Should_Set_All_Properties()
    {
        var deadline = DateTime.UtcNow.AddDays(7);
        var result = EventForm.Create(
            _eventId, "Full Survey", "Please fill out this survey",
            allowMultipleResponses: true, responseDeadline: deadline, maxResponses: 100);

        result.IsSuccess.Should().BeTrue();
        result.Value.Description.Should().Be("Please fill out this survey");
        result.Value.AllowMultipleResponses.Should().BeTrue();
        result.Value.ResponseDeadline.Should().Be(deadline);
        result.Value.MaxResponses.Should().Be(100);
    }

    [Fact]
    public void Create_WithEmptyEventId_Should_Return_Failure()
    {
        var result = EventForm.Create(Guid.Empty, "Test");
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Event ID cannot be empty");
    }

    [Fact]
    public void Create_WithEmptyTitle_Should_Return_Failure()
    {
        var result = EventForm.Create(_eventId, "");
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Form title cannot be empty");
    }

    [Fact]
    public void Create_WithTitleExceedingMaxLength_Should_Return_Failure()
    {
        var longTitle = new string('A', EventForm.MaxTitleLength + 1);
        var result = EventForm.Create(_eventId, longTitle);
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain($"exceed {EventForm.MaxTitleLength}");
    }

    [Fact]
    public void Create_WithDescriptionExceedingMaxLength_Should_Return_Failure()
    {
        var longDesc = new string('A', EventForm.MaxDescriptionLength + 1);
        var result = EventForm.Create(_eventId, "Test", longDesc);
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain($"exceed {EventForm.MaxDescriptionLength}");
    }

    [Fact]
    public void Create_WithMaxResponsesLessThan1_Should_Return_Failure()
    {
        var result = EventForm.Create(_eventId, "Test", maxResponses: 0);
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Maximum responses must be at least 1");
    }

    [Fact]
    public void Create_Should_Trim_Title_And_Description()
    {
        var result = EventForm.Create(_eventId, "  Survey  ", "  Description  ");
        result.IsSuccess.Should().BeTrue();
        result.Value.Title.Should().Be("Survey");
        result.Value.Description.Should().Be("Description");
    }

    #endregion

    #region Question Management Tests

    [Fact]
    public void AddQuestion_WithValidData_Should_Return_Success()
    {
        var form = EventForm.Create(_eventId, "Test Form").Value;

        var result = form.AddQuestion("What is your name?", FormQuestionType.ShortText, true, 0);

        result.IsSuccess.Should().BeTrue();
        form.GetQuestionCount().Should().Be(1);
        result.Value.QuestionText.Should().Be("What is your name?");
        result.Value.QuestionType.Should().Be(FormQuestionType.ShortText);
        result.Value.IsRequired.Should().BeTrue();
    }

    [Fact]
    public void AddQuestion_ChoiceType_WithOptions_Should_Succeed()
    {
        var form = EventForm.Create(_eventId, "Test Form").Value;
        var options = new[]
        {
            QuestionOption.Create("Option A", 0).Value,
            QuestionOption.Create("Option B", 1).Value,
        };

        var result = form.AddQuestion("Pick one", FormQuestionType.SingleChoice, false, 0, options);

        result.IsSuccess.Should().BeTrue();
        result.Value.Options.Should().HaveCount(2);
        result.Value.Options[0].Text.Should().Be("Option A");
    }

    [Fact]
    public void AddQuestion_ChoiceType_WithoutOptions_Should_Fail()
    {
        var form = EventForm.Create(_eventId, "Test Form").Value;

        var result = form.AddQuestion("Pick one", FormQuestionType.SingleChoice, false, 0);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("at least 2 options");
    }

    [Fact]
    public void AddQuestion_TextType_WithOptions_Should_Fail()
    {
        var form = EventForm.Create(_eventId, "Test Form").Value;
        var options = new[]
        {
            QuestionOption.Create("Option A", 0).Value,
            QuestionOption.Create("Option B", 1).Value,
        };

        var result = form.AddQuestion("Name?", FormQuestionType.ShortText, false, 0, options);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not allowed for ShortText");
    }

    [Fact]
    public void RemoveQuestion_WhenNoResponses_Should_Succeed()
    {
        var form = EventForm.Create(_eventId, "Test Form").Value;
        var question = form.AddQuestion("Name?", FormQuestionType.ShortText, false, 0).Value;

        var result = form.RemoveQuestion(question.Id);

        result.IsSuccess.Should().BeTrue();
        form.GetQuestionCount().Should().Be(0);
    }

    [Fact]
    public void RemoveQuestion_WhenHasResponses_Should_Fail()
    {
        var form = EventForm.Create(_eventId, "Test Form").Value;
        var question = form.AddQuestion("Name?", FormQuestionType.ShortText, false, 0).Value;
        form.MarkHasResponses();

        var result = form.RemoveQuestion(question.Id);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Cannot delete questions after responses");
    }

    [Fact]
    public void UpdateQuestion_TypeChange_WhenHasResponses_Should_Fail()
    {
        var form = EventForm.Create(_eventId, "Test Form").Value;
        var question = form.AddQuestion("Name?", FormQuestionType.ShortText, false, 0).Value;
        form.MarkHasResponses();

        var result = form.UpdateQuestion(question.Id, "Name?", FormQuestionType.LongText, false, 0);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Cannot change question type after responses");
    }

    [Fact]
    public void UpdateQuestion_TextChange_WhenHasResponses_Should_Succeed()
    {
        var form = EventForm.Create(_eventId, "Test Form").Value;
        var question = form.AddQuestion("Nmae?", FormQuestionType.ShortText, false, 0).Value;
        form.MarkHasResponses();

        var result = form.UpdateQuestion(question.Id, "Name?", FormQuestionType.ShortText, true, 0);

        result.IsSuccess.Should().BeTrue();
        form.GetQuestion(question.Id)!.QuestionText.Should().Be("Name?");
    }

    [Fact]
    public void ReorderQuestions_WithValidIds_Should_Succeed()
    {
        var form = EventForm.Create(_eventId, "Test Form").Value;
        var q1 = form.AddQuestion("Q1", FormQuestionType.ShortText, false, 0).Value;
        var q2 = form.AddQuestion("Q2", FormQuestionType.ShortText, false, 1).Value;
        var q3 = form.AddQuestion("Q3", FormQuestionType.ShortText, false, 2).Value;

        var result = form.ReorderQuestions(new List<Guid> { q3.Id, q1.Id, q2.Id });

        result.IsSuccess.Should().BeTrue();
        form.GetQuestion(q3.Id)!.SortOrder.Should().Be(0);
        form.GetQuestion(q1.Id)!.SortOrder.Should().Be(1);
        form.GetQuestion(q2.Id)!.SortOrder.Should().Be(2);
    }

    [Fact]
    public void ReorderQuestions_WithMissingIds_Should_Fail()
    {
        var form = EventForm.Create(_eventId, "Test Form").Value;
        form.AddQuestion("Q1", FormQuestionType.ShortText, false, 0);
        form.AddQuestion("Q2", FormQuestionType.ShortText, false, 1);

        var result = form.ReorderQuestions(new List<Guid> { Guid.NewGuid() });

        result.IsFailure.Should().BeTrue();
    }

    #endregion

    #region Lifecycle Tests

    [Fact]
    public void Publish_FromDraft_WithQuestions_Should_Succeed()
    {
        var form = EventForm.Create(_eventId, "Test Form").Value;
        form.AddQuestion("Q1", FormQuestionType.ShortText, false, 0);
        form.ClearDomainEvents();

        var result = form.Publish();

        result.IsSuccess.Should().BeTrue();
        form.Status.Should().Be(EventFormStatus.Active);
        form.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<EventFormPublishedEvent>();
    }

    [Fact]
    public void Publish_FromDraft_WithNoQuestions_Should_Fail()
    {
        var form = EventForm.Create(_eventId, "Test Form").Value;

        var result = form.Publish();

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("no questions");
    }

    [Fact]
    public void Publish_FromActive_Should_Fail()
    {
        var form = CreateActiveForm();

        var result = form.Publish();

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Only Draft forms");
    }

    [Fact]
    public void Close_FromActive_Should_Succeed()
    {
        var form = CreateActiveForm();
        form.ClearDomainEvents();

        var result = form.Close();

        result.IsSuccess.Should().BeTrue();
        form.Status.Should().Be(EventFormStatus.Closed);
        form.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<EventFormClosedEvent>();
    }

    [Fact]
    public void Close_FromDraft_Should_Fail()
    {
        var form = EventForm.Create(_eventId, "Test Form").Value;

        var result = form.Close();

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Only Active forms");
    }

    [Fact]
    public void Reopen_FromClosed_Should_Succeed()
    {
        var form = CreateActiveForm();
        form.Close();

        var result = form.Reopen();

        result.IsSuccess.Should().BeTrue();
        form.Status.Should().Be(EventFormStatus.Active);
    }

    [Fact]
    public void Reopen_FromActive_Should_Fail()
    {
        var form = CreateActiveForm();

        var result = form.Reopen();

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Only Closed forms");
    }

    [Fact]
    public void IsAcceptingResponses_WhenActive_And_NoDeadline_Should_Return_True()
    {
        var form = CreateActiveForm();

        form.IsAcceptingResponses().Should().BeTrue();
    }

    [Fact]
    public void IsAcceptingResponses_WhenDraft_Should_Return_False()
    {
        var form = EventForm.Create(_eventId, "Test Form").Value;

        form.IsAcceptingResponses().Should().BeFalse();
    }

    [Fact]
    public void IsAcceptingResponses_WhenDeadlinePassed_Should_Return_False()
    {
        var pastDeadline = DateTime.UtcNow.AddDays(-1);
        var form = EventForm.Create(_eventId, "Test", responseDeadline: pastDeadline).Value;
        form.AddQuestion("Q1", FormQuestionType.ShortText, false, 0);
        form.Publish();

        form.IsAcceptingResponses().Should().BeFalse();
    }

    [Fact]
    public void MarkHasResponses_Should_Set_Flag_To_True()
    {
        var form = EventForm.Create(_eventId, "Test Form").Value;

        form.MarkHasResponses();

        form.HasResponses.Should().BeTrue();
    }

    [Fact]
    public void MarkHasResponses_CalledTwice_Should_Not_Fail()
    {
        var form = EventForm.Create(_eventId, "Test Form").Value;
        form.MarkHasResponses();
        form.MarkHasResponses();

        form.HasResponses.Should().BeTrue();
    }

    #endregion

    #region UpdateDetails Tests

    [Fact]
    public void UpdateDetails_WithValidData_Should_Succeed()
    {
        var form = EventForm.Create(_eventId, "Old Title").Value;
        var deadline = DateTime.UtcNow.AddDays(14);

        var result = form.UpdateDetails("New Title", "New Description", true, deadline, 50);

        result.IsSuccess.Should().BeTrue();
        form.Title.Should().Be("New Title");
        form.Description.Should().Be("New Description");
        form.AllowMultipleResponses.Should().BeTrue();
        form.ResponseDeadline.Should().Be(deadline);
        form.MaxResponses.Should().Be(50);
    }

    [Fact]
    public void UpdateDetails_WithEmptyTitle_Should_Fail()
    {
        var form = EventForm.Create(_eventId, "Title").Value;

        var result = form.UpdateDetails("", null, false, null, null);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Form title cannot be empty");
    }

    #endregion

    #region Phase 6A.146 — Response Visibility Toggle

    [Fact]
    public void Create_Should_Default_AllowAttendeesToViewResponses_To_False()
    {
        var result = EventForm.Create(_eventId, "Test Form");

        result.IsSuccess.Should().BeTrue();
        result.Value.AllowAttendeesToViewResponses.Should().BeFalse();
    }

    [Fact]
    public void Create_WithExplicitVisibilityTrue_Should_Set_Property()
    {
        var result = EventForm.Create(
            _eventId, "Public Survey", description: null,
            allowMultipleResponses: false, responseDeadline: null, maxResponses: null,
            allowAttendeesToViewResponses: true);

        result.IsSuccess.Should().BeTrue();
        result.Value.AllowAttendeesToViewResponses.Should().BeTrue();
    }

    [Fact]
    public void UpdateDetails_WithVisibilityTrue_Should_Update_Property()
    {
        var form = EventForm.Create(_eventId, "Survey").Value;
        form.AllowAttendeesToViewResponses.Should().BeFalse(); // pre-condition

        var result = form.UpdateDetails(
            "Survey", null, false, null, null,
            allowAttendeesToViewResponses: true);

        result.IsSuccess.Should().BeTrue();
        form.AllowAttendeesToViewResponses.Should().BeTrue();
    }

    [Fact]
    public void UpdateDetails_FromTrueToFalse_Should_Update_Property()
    {
        // Phase 6A.146 — organizer must be able to retract the public toggle.
        var form = EventForm.Create(
            _eventId, "Survey", description: null,
            allowMultipleResponses: false, responseDeadline: null, maxResponses: null,
            allowAttendeesToViewResponses: true).Value;

        var result = form.UpdateDetails("Survey", null, false, null, null,
            allowAttendeesToViewResponses: false);

        result.IsSuccess.Should().BeTrue();
        form.AllowAttendeesToViewResponses.Should().BeFalse();
    }

    [Fact]
    public void UpdateDetails_PositionalCall_DoesNotChangeVisibility_BackwardCompatible()
    {
        // Architect's correction C1: the new parameter is OPTIONAL at the END so
        // every existing caller (UpdateEventFormCommandHandler, integration tests,
        // ~30+ positional callers) keeps compiling and keeps its prior semantics.
        var form = EventForm.Create(
            _eventId, "Survey", description: null,
            allowMultipleResponses: false, responseDeadline: null, maxResponses: null,
            allowAttendeesToViewResponses: true).Value;

        // Legacy call without the new parameter — visibility must NOT be flipped.
        var result = form.UpdateDetails("Updated", null, false, null, null);

        result.IsSuccess.Should().BeTrue();
        form.AllowAttendeesToViewResponses.Should().BeTrue();
    }

    [Fact]
    public void UpdateDetails_CanToggle_OnAnyNonArchivedStatus()
    {
        // Architect's correction C2: NO status guard on the toggle itself. The
        // public endpoint gates visibility on Active/Closed separately. This lets
        // an organizer configure the toggle before publishing the form.
        var form = EventForm.Create(_eventId, "Survey").Value;
        form.Status.Should().Be(EventFormStatus.Draft);

        // Toggle on Draft
        form.UpdateDetails("Survey", null, false, null, null, allowAttendeesToViewResponses: true)
            .IsSuccess.Should().BeTrue();
        form.AllowAttendeesToViewResponses.Should().BeTrue();
    }

    #endregion

    #region Helper Methods

    private EventForm CreateActiveForm()
    {
        var form = EventForm.Create(_eventId, "Test Form").Value;
        form.AddQuestion("Q1", FormQuestionType.ShortText, false, 0);
        form.Publish();
        return form;
    }

    #endregion
}
