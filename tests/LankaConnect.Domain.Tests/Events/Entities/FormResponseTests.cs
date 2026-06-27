using FluentAssertions;
using LankaConnect.Modules.Forms.Domain;
using LankaConnect.Modules.Forms.Domain.Entities;
using LankaConnect.Modules.Forms.Domain.Enums;
using LankaConnect.Modules.Forms.Domain.DomainEvents;
using LankaConnect.Modules.Forms.Domain.Repositories;
using LankaConnect.Domain.Events.Entities;
using LankaConnect.Domain.Events.DomainEvents;
using Xunit;

namespace LankaConnect.Domain.Tests.Events.Entities;

/// <summary>
/// TDD tests for FormResponse aggregate root.
/// Tests creation, answer management, editing capability, and validation.
/// </summary>
public class FormResponseTests
{
    private readonly Guid _formId = Guid.NewGuid();
    private readonly Guid _eventId = Guid.NewGuid();
    private readonly string _tokenHash = "abc123hashvalue";

    #region Creation Tests

    [Fact]
    public void Create_WithValidData_Should_Return_Success()
    {
        var result = FormResponse.Create(_formId, _eventId, _tokenHash, null, "test@example.com", "John Doe");

        result.IsSuccess.Should().BeTrue();
        result.Value.EventFormId.Should().Be(_formId);
        result.Value.EventId.Should().Be(_eventId);
        result.Value.AccessTokenHash.Should().Be(_tokenHash);
        result.Value.RespondentEmail.Should().Be("test@example.com");
        result.Value.RespondentName.Should().Be("John Doe");
        result.Value.RespondentUserId.Should().BeNull();
        result.Value.SubmittedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Create_Should_Raise_FormResponseSubmittedEvent()
    {
        var result = FormResponse.Create(_formId, _eventId, _tokenHash, null, "test@example.com");

        result.IsSuccess.Should().BeTrue();
        result.Value.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<FormResponseSubmittedEvent>();
        var domainEvent = (FormResponseSubmittedEvent)result.Value.DomainEvents.First();
        domainEvent.FormId.Should().Be(_formId);
        domainEvent.RespondentEmail.Should().Be("test@example.com");
    }

    [Fact]
    public void Create_WithAuthenticatedUser_Should_Set_UserId()
    {
        var userId = Guid.NewGuid();
        var result = FormResponse.Create(_formId, _eventId, _tokenHash, null, "test@example.com", "John", userId);

        result.IsSuccess.Should().BeTrue();
        result.Value.RespondentUserId.Should().Be(userId);
    }

    [Fact]
    public void Create_WithEmptyFormId_Should_Fail()
    {
        var result = FormResponse.Create(Guid.Empty, _eventId, _tokenHash, null);
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Event form ID cannot be empty");
    }

    [Fact]
    public void Create_WithEmptyEventId_Should_Fail()
    {
        var result = FormResponse.Create(_formId, Guid.Empty, _tokenHash, null);
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Event ID cannot be empty");
    }

    [Fact]
    public void Create_WithEmptyTokenHash_Should_Fail()
    {
        var result = FormResponse.Create(_formId, _eventId, "", null);
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Access token hash cannot be empty");
    }

    [Fact]
    public void Create_WithInvalidEmail_Should_Fail()
    {
        var result = FormResponse.Create(_formId, _eventId, _tokenHash, null, "notanemail");
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Invalid email format");
    }

    [Fact]
    public void Create_WithLongEmail_Should_Fail()
    {
        var longEmail = new string('a', 250) + "@b.com";
        var result = FormResponse.Create(_formId, _eventId, _tokenHash, null, longEmail);
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain($"exceed {FormResponse.MaxEmailLength}");
    }

    #endregion

    #region Answer Management Tests

    [Fact]
    public void AddAnswer_Should_Succeed()
    {
        var response = FormResponse.Create(_formId, _eventId, _tokenHash, null).Value;
        var questionId = Guid.NewGuid();

        var result = response.AddAnswer(questionId, "What is your name?", textValue: "John");

        result.IsSuccess.Should().BeTrue();
        response.Answers.Should().HaveCount(1);
        response.Answers[0].TextValue.Should().Be("John");
        response.Answers[0].QuestionTextSnapshot.Should().Be("What is your name?");
    }

    [Fact]
    public void AddAnswer_WithChoiceOptions_Should_Succeed()
    {
        var response = FormResponse.Create(_formId, _eventId, _tokenHash, null).Value;
        var questionId = Guid.NewGuid();
        var optionId = Guid.NewGuid();

        var result = response.AddAnswer(
            questionId, "Pick one",
            selectedOptionIds: new List<Guid> { optionId },
            selectedOptionTextSnapshots: new List<string> { "Option A" });

        result.IsSuccess.Should().BeTrue();
        response.Answers[0].SelectedOptionIds.Should().ContainSingle().Which.Should().Be(optionId);
        response.Answers[0].SelectedOptionTextSnapshots.Should().ContainSingle().Which.Should().Be("Option A");
    }

    [Fact]
    public void AddAnswer_WithBooleanValue_Should_Succeed()
    {
        var response = FormResponse.Create(_formId, _eventId, _tokenHash, null).Value;
        var questionId = Guid.NewGuid();

        var result = response.AddAnswer(questionId, "Do you need parking?", booleanValue: true);

        result.IsSuccess.Should().BeTrue();
        response.Answers[0].BooleanValue.Should().BeTrue();
    }

    [Fact]
    public void AddAnswer_DuplicateQuestion_Should_Fail()
    {
        var response = FormResponse.Create(_formId, _eventId, _tokenHash, null).Value;
        var questionId = Guid.NewGuid();
        response.AddAnswer(questionId, "Q1", textValue: "A1");

        var result = response.AddAnswer(questionId, "Q1", textValue: "A2");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("already exists");
    }

    [Fact]
    public void UpdateAnswer_Should_Succeed()
    {
        var response = FormResponse.Create(_formId, _eventId, _tokenHash, null).Value;
        var questionId = Guid.NewGuid();
        response.AddAnswer(questionId, "Q1", textValue: "Original");
        response.ClearDomainEvents();

        var result = response.UpdateAnswer(questionId, textValue: "Updated");

        result.IsSuccess.Should().BeTrue();
        response.GetAnswer(questionId)!.TextValue.Should().Be("Updated");
        // Phase 6A.114: FormResponseUpdatedEvent raising moved out of UpdateAnswer()
        // into RaiseUpdatedEventWithContext(form, @event) to eliminate duplicate queries.
        // See FormResponse.cs lines 185-186. Event raising is tested separately when
        // Form + Event fixtures are available.
        response.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void UpdateAnswer_NonExistentQuestion_Should_Fail()
    {
        var response = FormResponse.Create(_formId, _eventId, _tokenHash, null).Value;

        var result = response.UpdateAnswer(Guid.NewGuid(), textValue: "Test");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("No answer found");
    }

    #endregion

    #region CanEdit Tests

    [Fact]
    public void CanEdit_WithNoDeadline_Should_Return_True()
    {
        var response = FormResponse.Create(_formId, _eventId, _tokenHash, null).Value;

        response.CanEdit(null).Should().BeTrue();
    }

    [Fact]
    public void CanEdit_BeforeDeadline_Should_Return_True()
    {
        var response = FormResponse.Create(_formId, _eventId, _tokenHash, null).Value;
        var futureDeadline = DateTime.UtcNow.AddDays(7);

        response.CanEdit(futureDeadline).Should().BeTrue();
    }

    [Fact]
    public void CanEdit_AfterDeadline_Should_Return_False()
    {
        var response = FormResponse.Create(_formId, _eventId, _tokenHash, null).Value;
        var pastDeadline = DateTime.UtcNow.AddDays(-1);

        response.CanEdit(pastDeadline).Should().BeFalse();
    }

    #endregion

    #region RaiseDeletedEvent Tests (Phase 6A.106)

    [Fact]
    public void RaiseDeletedEvent_Should_Succeed()
    {
        // Arrange
        var response = FormResponse.Create(_formId, _eventId, _tokenHash, null, "test@example.com").Value;
        response.ClearDomainEvents();

        // Act
        var result = response.RaiseDeletedEvent();

        // Assert
        result.IsSuccess.Should().BeTrue();
        response.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<FormResponseDeletedEvent>();
    }

    [Fact]
    public void RaiseDeletedEvent_Should_Include_ResponseData()
    {
        // Arrange
        var email = "test@example.com";
        var response = FormResponse.Create(_formId, _eventId, _tokenHash, null, email).Value;
        response.ClearDomainEvents();

        // Act
        response.RaiseDeletedEvent();
        var domainEvent = (FormResponseDeletedEvent)response.DomainEvents.First();

        // Assert
        domainEvent.FormId.Should().Be(_formId);
        domainEvent.ResponseId.Should().Be(response.Id);
        domainEvent.RespondentEmail.Should().Be(email);
        domainEvent.OccurredAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void RaiseDeletedEvent_AnonymousResponse_Should_Include_NullEmail()
    {
        // Arrange
        var response = FormResponse.Create(_formId, _eventId, _tokenHash, null).Value;
        response.ClearDomainEvents();

        // Act
        response.RaiseDeletedEvent();
        var domainEvent = (FormResponseDeletedEvent)response.DomainEvents.First();

        // Assert
        domainEvent.RespondentEmail.Should().BeNull();
    }

    #endregion
}
