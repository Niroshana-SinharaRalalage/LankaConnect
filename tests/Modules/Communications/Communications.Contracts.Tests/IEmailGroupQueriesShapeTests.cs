using System.Reflection;
using LankaConnect.Modules.Communications.Contracts;

namespace LankaConnect.Modules.Communications.Contracts.Tests;

/// <summary>
/// Shape-pinning tests for <see cref="IEmailGroupQueries"/>,
/// <see cref="ICommunicationsCommands"/> + the Contracts DTOs.
/// Wave 5.4.a (2026-06-13). Purpose: catch silent ABI drift -- if the
/// cross-module surface changes accidentally, the Event aggregate
/// + post-5.4.d.1 LankaConnect.Application consumers would break.
/// </summary>
public sealed class IEmailGroupQueriesShapeTests
{
    [Fact]
    public void IEmailGroupQueries_Has_FourQueryMethods()
    {
        var methods = typeof(IEmailGroupQueries).GetMethods(BindingFlags.Public | BindingFlags.Instance);

        methods.Select(m => m.Name).Should().BeEquivalentTo(new[]
        {
            "GetByIdsAsync",
            "GetByIdAsync",
            "GetByIdWithEmailsAsync",
            "GetByOwnerAsync",
        });
    }

    [Fact]
    public void IEmailGroupQueries_DoesNot_Have_GetByEventAsync()
    {
        // Architect ruling 2026-06-13. Event aggregate owns its _emailGroupIds;
        // cross-module fetches go GetByIdsAsync(event.EmailGroupIds) per the
        // Phase 6A.32 N+1 batch-fetch pattern. A GetByEventAsync would
        // re-introduce the cross-module coupling Wave 5.4 is cutting.
        var methods = typeof(IEmailGroupQueries).GetMethods(BindingFlags.Public | BindingFlags.Instance);
        methods.Select(m => m.Name).Should().NotContain("GetByEventAsync");
    }

    [Fact]
    public void ICommunicationsCommands_Is_EmptyMarker_AtWave54a()
    {
        // Concrete methods are added in Wave 5.4.d.1 after the
        // LankaConnect.Application audit identifies which cross-module mutator
        // paths need to go through Contracts. If the 5.4.d.1 audit finds zero,
        // this interface is removed in 5.4.d.3.
        var methods = typeof(ICommunicationsCommands).GetMethods(BindingFlags.Public | BindingFlags.Instance);
        methods.Should().BeEmpty();
    }

    [Fact]
    public void EmailGroupSummaryDto_Carries_CoreFields()
    {
        var props = typeof(EmailGroupSummaryDto).GetProperties();

        props.Select(p => p.Name).Should().BeEquivalentTo(new[]
        {
            nameof(EmailGroupSummaryDto.Id),
            nameof(EmailGroupSummaryDto.Name),
            nameof(EmailGroupSummaryDto.Description),
            nameof(EmailGroupSummaryDto.OwnerId),
            nameof(EmailGroupSummaryDto.EmailCount),
            nameof(EmailGroupSummaryDto.IsActive),
            nameof(EmailGroupSummaryDto.CreatedAt),
            nameof(EmailGroupSummaryDto.UpdatedAt),
        });
    }

    [Fact]
    public void EmailGroupDetailDto_Carries_Emails_AsReadOnlyList()
    {
        var emailsProp = typeof(EmailGroupDetailDto).GetProperty(nameof(EmailGroupDetailDto.Emails));
        emailsProp.Should().NotBeNull();
        emailsProp!.PropertyType.Should().Be(typeof(IReadOnlyList<string>));
    }
}
