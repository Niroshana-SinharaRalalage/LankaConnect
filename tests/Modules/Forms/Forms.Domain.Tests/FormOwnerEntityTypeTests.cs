using LankaConnect.Modules.Forms.Domain;
using LankaConnect.Modules.Forms.Domain.Enums;

namespace LankaConnect.Modules.Forms.Domain.Tests;

/// <summary>
/// Wave 5.2b (2026-06-11): unit tests for the polymorphic owner-entity
/// discriminator introduced on the <see cref="Form"/> aggregate.
/// </summary>
/// <remarks>
/// Per CLAUDE.md §13.1 triggers T1 (new public properties on aggregate) +
/// T3 (Create() factory signature change). Same-commit tests REQUIRED.
///
/// 5.2c will land the EF migration that adds owner_entity_id +
/// owner_entity_type columns and backfills from event_id. These tests
/// guard the CLR-side invariants that survive that schema change.
/// </remarks>
public sealed class FormOwnerEntityTypeTests
{
    [Fact]
    public void Create_WithDefaultOwnerType_SetsOwnerToEvent()
    {
        var eventId = Guid.NewGuid();

        var result = Form.Create(eventId, "Survey");

        result.IsSuccess.Should().BeTrue();
        result.Value.OwnerEntityType.Should().Be(FormOwnerEntityType.Event,
            because: "Wave 5.2b default value for Form.Create() must be Event so legacy positional callers continue to behave identically.");
    }

    [Fact]
    public void Create_WithExplicitEventOwner_SetsOwnerEntityTypeToEvent()
    {
        var eventId = Guid.NewGuid();

        var result = Form.Create(eventId, "Survey",
            ownerEntityType: FormOwnerEntityType.Event);

        result.IsSuccess.Should().BeTrue();
        result.Value.OwnerEntityType.Should().Be(FormOwnerEntityType.Event);
    }

    [Fact]
    public void Create_OwnerEntityId_MirrorsEventId_WhenOwnerIsEvent()
    {
        var eventId = Guid.NewGuid();

        var result = Form.Create(eventId, "Survey");

        result.IsSuccess.Should().BeTrue();
        result.Value.OwnerEntityId.Should().Be(eventId,
            because: "For Event-owned forms during the 5.2b transitional wave, OwnerEntityId must equal EventId so 5.2c's UPDATE forms.event_forms SET owner_entity_id = event_id backfill matches the CLR-side model.");
        result.Value.EventId.Should().Be(eventId,
            because: "EventId is kept unchanged during 5.2b; it becomes a computed shim only in 5.2d.");
    }

    [Fact]
    public void Create_OwnerEntityId_AndEventId_StayInSync_AcrossMultipleForms()
    {
        var eventId1 = Guid.NewGuid();
        var eventId2 = Guid.NewGuid();

        var form1 = Form.Create(eventId1, "Survey 1").Value;
        var form2 = Form.Create(eventId2, "Survey 2").Value;

        form1.OwnerEntityId.Should().Be(form1.EventId);
        form2.OwnerEntityId.Should().Be(form2.EventId);
        form1.OwnerEntityId.Should().NotBe(form2.OwnerEntityId,
            because: "Different events produce different owner identifiers.");
    }
}
