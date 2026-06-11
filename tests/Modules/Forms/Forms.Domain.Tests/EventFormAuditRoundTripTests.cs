using LankaConnect.Modules.Forms.Domain;
using LankaConnect.Modules.Forms.Domain.Enums;

namespace LankaConnect.Modules.Forms.Domain.Tests;

/// <summary>
/// Wave4.9.1.7 (2026-06-08): per-mutator round-trip audit-field tests for
/// the Form aggregate. Same invariant as PhotoAlbum (Wave4.9.1.6):
/// every state-mutating method MUST set <c>UpdatedAt = DateTime.UtcNow</c>.
///
/// Pattern: CREATE -> assert CreatedAt fresh, UpdatedAt null -> MUTATE ->
/// re-assert UpdatedAt &gt; CreatedAt.
/// </summary>
/// <remarks>
/// Per CLAUDE.md §13.1 trigger T2 (mutator touching IAuditable).
/// Forms.Domain.Tests project was missing entirely before this commit -
/// added to LankaConnect.sln in the same commit.
/// </remarks>
public sealed class EventFormAuditRoundTripTests
{
    private static Form NewForm()
    {
        var result = Form.Create(
            eventId: Guid.NewGuid(),
            title: "Wave4.9.1.7 Round-Trip Form",
            description: "round-trip seed");
        result.IsSuccess.Should().BeTrue();
        return result.Value;
    }

    [Fact]
    public void Create_Sets_CreatedAt_And_Leaves_UpdatedAt_Null()
    {
        var before = DateTime.UtcNow;
        var form = NewForm();

        form.CreatedAt.Should().BeOnOrAfter(before.AddSeconds(-1));
        form.CreatedAt.Should().BeOnOrBefore(DateTime.UtcNow.AddSeconds(1));
        form.UpdatedAt.Should().BeNull();
    }

    [Fact]
    public void UpdateDetails_Advances_UpdatedAt()
    {
        var form = NewForm();
        var createdAt = form.CreatedAt;
        Thread.Sleep(20);

        var result = form.UpdateDetails(
            title: "Updated", description: "after", allowMultipleResponses: true,
            responseDeadline: null, maxResponses: 50);

        result.IsSuccess.Should().BeTrue();
        form.UpdatedAt.Should().NotBeNull();
        form.UpdatedAt!.Value.Should().BeAfter(createdAt);
    }

    [Fact]
    public void AddQuestion_Advances_UpdatedAt()
    {
        var form = NewForm();
        var createdAt = form.CreatedAt;
        Thread.Sleep(20);

        var result = form.AddQuestion(
            questionText: "Your name?", questionType: FormQuestionType.ShortText,
            isRequired: true, sortOrder: 0);

        result.IsSuccess.Should().BeTrue();
        form.UpdatedAt.Should().NotBeNull();
        form.UpdatedAt!.Value.Should().BeAfter(createdAt);
    }

    [Fact]
    public void UpdateQuestion_Advances_UpdatedAt()
    {
        var form = NewForm();
        var addResult = form.AddQuestion("Q1", FormQuestionType.ShortText, true, 0);
        addResult.IsSuccess.Should().BeTrue();
        var addUpdatedAt = form.UpdatedAt;
        Thread.Sleep(20);

        var result = form.UpdateQuestion(addResult.Value.Id,
            "Q1 renamed", FormQuestionType.ShortText, isRequired: false, sortOrder: 0);

        result.IsSuccess.Should().BeTrue();
        form.UpdatedAt.Should().NotBeNull();
        form.UpdatedAt!.Value.Should().BeAfter(addUpdatedAt!.Value);
    }

    [Fact]
    public void RemoveQuestion_Advances_UpdatedAt()
    {
        var form = NewForm();
        var addResult = form.AddQuestion("Q1", FormQuestionType.ShortText, true, 0);
        addResult.IsSuccess.Should().BeTrue();
        var addUpdatedAt = form.UpdatedAt;
        Thread.Sleep(20);

        var result = form.RemoveQuestion(addResult.Value.Id);

        result.IsSuccess.Should().BeTrue();
        form.GetQuestionCount().Should().Be(0);
        form.UpdatedAt.Should().NotBeNull();
        form.UpdatedAt!.Value.Should().BeAfter(addUpdatedAt!.Value);
    }

    [Fact]
    public void ReorderQuestions_Advances_UpdatedAt()
    {
        var form = NewForm();
        var q1 = form.AddQuestion("Q1", FormQuestionType.ShortText, true, 0).Value;
        var q2 = form.AddQuestion("Q2", FormQuestionType.ShortText, true, 1).Value;
        var lastUpdatedAt = form.UpdatedAt;
        Thread.Sleep(20);

        var result = form.ReorderQuestions(new List<Guid> { q2.Id, q1.Id });

        result.IsSuccess.Should().BeTrue();
        form.UpdatedAt.Should().NotBeNull();
        form.UpdatedAt!.Value.Should().BeAfter(lastUpdatedAt!.Value);
    }

    [Fact]
    public void Publish_Advances_UpdatedAt()
    {
        var form = NewForm();
        form.AddQuestion("Q1", FormQuestionType.ShortText, true, 0).IsSuccess.Should().BeTrue();
        var lastUpdatedAt = form.UpdatedAt;
        Thread.Sleep(20);

        var result = form.Publish();

        result.IsSuccess.Should().BeTrue();
        form.Status.Should().Be(FormStatus.Active);
        form.UpdatedAt.Should().NotBeNull();
        form.UpdatedAt!.Value.Should().BeAfter(lastUpdatedAt!.Value);
    }

    [Fact]
    public void Close_Advances_UpdatedAt()
    {
        var form = NewForm();
        form.AddQuestion("Q1", FormQuestionType.ShortText, true, 0).IsSuccess.Should().BeTrue();
        form.Publish().IsSuccess.Should().BeTrue();
        var lastUpdatedAt = form.UpdatedAt;
        Thread.Sleep(20);

        var result = form.Close();

        result.IsSuccess.Should().BeTrue();
        form.Status.Should().Be(FormStatus.Closed);
        form.UpdatedAt.Should().NotBeNull();
        form.UpdatedAt!.Value.Should().BeAfter(lastUpdatedAt!.Value);
    }

    [Fact]
    public void Reopen_Advances_UpdatedAt()
    {
        var form = NewForm();
        form.AddQuestion("Q1", FormQuestionType.ShortText, true, 0).IsSuccess.Should().BeTrue();
        form.Publish().IsSuccess.Should().BeTrue();
        form.Close().IsSuccess.Should().BeTrue();
        var lastUpdatedAt = form.UpdatedAt;
        Thread.Sleep(20);

        var result = form.Reopen();

        result.IsSuccess.Should().BeTrue();
        form.Status.Should().Be(FormStatus.Active);
        form.UpdatedAt.Should().NotBeNull();
        form.UpdatedAt!.Value.Should().BeAfter(lastUpdatedAt!.Value);
    }

    [Fact]
    public void Archive_Advances_UpdatedAt()
    {
        var form = NewForm();
        var createdAt = form.CreatedAt;
        Thread.Sleep(20);

        var result = form.Archive();

        result.IsSuccess.Should().BeTrue();
        form.Status.Should().Be(FormStatus.Archived);
        form.UpdatedAt.Should().NotBeNull();
        form.UpdatedAt!.Value.Should().BeAfter(createdAt);
    }

    [Fact]
    public void MarkHasResponses_Advances_UpdatedAt_On_First_Call()
    {
        var form = NewForm();
        var createdAt = form.CreatedAt;
        Thread.Sleep(20);

        form.MarkHasResponses();

        form.HasResponses.Should().BeTrue();
        form.UpdatedAt.Should().NotBeNull();
        form.UpdatedAt!.Value.Should().BeAfter(createdAt);
    }
}
