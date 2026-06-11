using System.Reflection;
using LankaConnect.Modules.Forms.Contracts;

namespace LankaConnect.Modules.Forms.Contracts.Tests;

/// <summary>
/// Shape-pinning tests for <see cref="IFormQueries"/>, <see cref="IFormCommands"/>
/// + the Contracts DTOs. Wave 5.3a (2026-06-11). Purpose: catch silent ABI
/// drift -- if the cross-module surface changes accidentally, consumers in
/// LankaConnect.Application.Events.* (post Wave 5.3d) would break.
/// </summary>
public sealed class IFormQueriesShapeTests
{
    [Fact]
    public void IFormQueries_Has_FourQueryMethods()
    {
        var methods = typeof(IFormQueries).GetMethods(BindingFlags.Public | BindingFlags.Instance);

        methods.Select(m => m.Name).Should().BeEquivalentTo(new[]
        {
            "GetByOwnerAsync",
            "GetByIdAsync",
            "GetByIdWithQuestionsAsync",
            "GetResponseWithAnswersAsync",
        });
    }

    [Fact]
    public void IFormCommands_Has_SingleMutator()
    {
        var methods = typeof(IFormCommands).GetMethods(BindingFlags.Public | BindingFlags.Instance);

        methods.Select(m => m.Name).Should().BeEquivalentTo(new[]
        {
            "DeleteResponsesByEventAndUserAsync",
        });
    }

    [Fact]
    public void FormSummaryDto_Carries_OwnerDiscriminator()
    {
        var props = typeof(FormSummaryDto).GetProperties();

        props.Select(p => p.Name).Should().Contain(new[]
        {
            nameof(FormSummaryDto.Id),
            nameof(FormSummaryDto.OwnerEntityType),
            nameof(FormSummaryDto.OwnerEntityId),
            nameof(FormSummaryDto.Title),
            nameof(FormSummaryDto.Status),
            nameof(FormSummaryDto.CreatedAt),
        });
    }

    [Fact]
    public void FormDetailDto_Carries_Questions()
    {
        var props = typeof(FormDetailDto).GetProperties();

        props.Select(p => p.Name).Should().Contain(nameof(FormDetailDto.Questions));

        var questionsProp = typeof(FormDetailDto).GetProperty(nameof(FormDetailDto.Questions))!;
        questionsProp.PropertyType.Should().Be(typeof(IReadOnlyList<FormQuestionContractDto>));
    }

    [Fact]
    public void FormResponseDetailDto_Carries_AnswersAndEventId()
    {
        var props = typeof(FormResponseDetailDto).GetProperties();

        props.Select(p => p.Name).Should().Contain(new[]
        {
            nameof(FormResponseDetailDto.FormId),
            nameof(FormResponseDetailDto.EventId),
            nameof(FormResponseDetailDto.Answers),
        });
    }

    [Fact]
    public void FormStatusDto_HasFourValues_MatchingDomain()
    {
        var values = Enum.GetValues<FormStatusDto>();

        values.Should().BeEquivalentTo(new[]
        {
            FormStatusDto.Draft, FormStatusDto.Active, FormStatusDto.Closed, FormStatusDto.Archived,
        });

        ((int)FormStatusDto.Draft).Should().Be(0);
        ((int)FormStatusDto.Active).Should().Be(1);
        ((int)FormStatusDto.Closed).Should().Be(2);
        ((int)FormStatusDto.Archived).Should().Be(3);
    }

    [Fact]
    public void FormOwnerEntityTypeDto_Event_Is_1()
    {
        ((int)FormOwnerEntityTypeDto.Event).Should().Be(1);
    }
}
