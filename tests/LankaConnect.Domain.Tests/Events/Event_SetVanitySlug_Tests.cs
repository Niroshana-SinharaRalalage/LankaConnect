using FluentAssertions;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Events.Enums;
using LankaConnect.Domain.Events.ValueObjects;
using Xunit;

namespace LankaConnect.Domain.Tests.Events;

/// <summary>
/// Phase 6A.154 — pins the <c>Event.SetVanitySlug</c> aggregate mutator
/// contract.
///
/// The VO (<see cref="EventVanitySlug"/>) owns shape validation — those
/// tests live in <c>EventVanitySlug_Create_Tests</c>. This file owns the
/// aggregate-level invariants:
///
/// - D6 status lockout (Cancelled / Completed / Archived / Active)
/// - D3 alias bookkeeping (old slug → SlugAliases when changed/cleared)
/// - D16 clearing emits an alias too
/// </summary>
public class Event_SetVanitySlug_Tests
{
    private static EventTitle Title() =>
        EventTitle.Create("Phase 6A.154 slug-mutator test event").Value;

    private static EventDescription Description() =>
        EventDescription.Create("Phase 6A.154 coverage").Value;

    /// <summary>
    /// Helper: build a Draft event 30 days out (default editable status).
    /// </summary>
    private static Event NewDraftEvent()
    {
        var start = DateTime.UtcNow.AddDays(30);
        return Event.Create(
            Title(), Description(),
            startDate: start, endDate: start.AddHours(3),
            organizerId: Guid.NewGuid(),
            capacity: 100).Value;
    }

    /// <summary>
    /// Helper: force the event into a given status via reflection — the
    /// existing test files use this pattern (Status has no public setter
    /// outside of specific transition methods).
    /// </summary>
    private static void ForceStatus(Event @event, EventStatus status)
    {
        var field = typeof(Event).GetField("<Status>k__BackingField",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        field!.SetValue(@event, status);
    }

    private static EventVanitySlug Slug(string s) =>
        EventVanitySlug.Create(s).Value!;

    // ─────────────────────────────────────────────────────────────────────────
    //  Happy path — set / clear / replace
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void SetVanitySlug_OnDraftEvent_FromNull_Succeeds()
    {
        var @event = NewDraftEvent();
        var slug = Slug("cleveland-show");

        var result = @event.SetVanitySlug(slug);

        result.IsSuccess.Should().BeTrue();
        @event.VanitySlug.Should().Be(slug);
        @event.SlugAliases.Should().BeEmpty(
            "no prior slug means no alias to retire");
    }

    [Fact]
    public void SetVanitySlug_ClearFromNull_IsNoOp()
    {
        // Saving the form without ever setting a slug must not create an
        // empty/null alias row. Same-value short-circuit must hold.
        var @event = NewDraftEvent();

        var result = @event.SetVanitySlug(null);

        result.IsSuccess.Should().BeTrue();
        @event.VanitySlug.Should().BeNull();
        @event.SlugAliases.Should().BeEmpty();
    }

    [Fact]
    public void SetVanitySlug_SameValue_IsNoOp()
    {
        // Organizer saves the form without changing the slug — must NOT
        // append an alias. Equality is VO value-equality.
        var @event = NewDraftEvent();
        @event.SetVanitySlug(Slug("cleveland-show"));

        var result = @event.SetVanitySlug(Slug("cleveland-show"));

        result.IsSuccess.Should().BeTrue();
        @event.SlugAliases.Should().BeEmpty(
            "same-value re-set must not appear in alias history");
    }

    [Fact]
    public void SetVanitySlug_ReplaceWithDifferent_EmitsAlias()
    {
        // The headline alias-history test. Architect D3 — when slug changes,
        // the old value becomes a permanent 301 source.
        var @event = NewDraftEvent();
        @event.SetVanitySlug(Slug("cleveland-show"));

        var result = @event.SetVanitySlug(Slug("cleveland-festival"));

        result.IsSuccess.Should().BeTrue();
        @event.VanitySlug!.Value.Should().Be("cleveland-festival");
        @event.SlugAliases.Should().ContainSingle();
        @event.SlugAliases[0].Alias.Should().Be("cleveland-show",
            "old slug must be retired as alias when replaced");
        @event.SlugAliases[0].EventId.Should().Be(@event.Id);
    }

    [Fact]
    public void SetVanitySlug_ClearFromExisting_EmitsAlias()
    {
        // Architect D16: clearing also emits an alias. The organizer might
        // remove the vanity URL but their share-links shouldn't 404 — they
        // 301 to /events/{id} via the alias lookup path.
        var @event = NewDraftEvent();
        @event.SetVanitySlug(Slug("cleveland-show"));

        var result = @event.SetVanitySlug(null);

        result.IsSuccess.Should().BeTrue();
        @event.VanitySlug.Should().BeNull();
        @event.SlugAliases.Should().ContainSingle();
        @event.SlugAliases[0].Alias.Should().Be("cleveland-show");
    }

    [Fact]
    public void SetVanitySlug_MultipleChanges_AccumulateAliases()
    {
        // Organizer rebrands twice — both old values preserved. Aliases are
        // append-only by design (architect D3).
        var @event = NewDraftEvent();
        @event.SetVanitySlug(Slug("first-name"));
        @event.SetVanitySlug(Slug("second-name"));
        @event.SetVanitySlug(Slug("third-name"));

        @event.VanitySlug!.Value.Should().Be("third-name");
        @event.SlugAliases.Should().HaveCount(2);
        @event.SlugAliases.Select(a => a.Alias).Should().BeEquivalentTo(
            new[] { "first-name", "second-name" },
            "every retired slug must be preserved");
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  D6 — status lockout
    // ─────────────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(EventStatus.Cancelled)]
    [InlineData(EventStatus.Completed)]
    [InlineData(EventStatus.Archived)]
    [InlineData(EventStatus.Active)]
    public void SetVanitySlug_OnLockedStatus_Fails(EventStatus lockedStatus)
    {
        var @event = NewDraftEvent();
        ForceStatus(@event, lockedStatus);

        var result = @event.SetVanitySlug(Slug("cleveland-show"));

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("cannot be changed");
        result.Error.Should().Contain(lockedStatus.ToString());
    }

    [Theory]
    [InlineData(EventStatus.Draft)]
    [InlineData(EventStatus.Planning)]
    [InlineData(EventStatus.Published)]
    public void SetVanitySlug_OnEditableStatus_Succeeds(EventStatus editableStatus)
    {
        // Architect D6 + user-confirmed: editable in Draft/Planning/Published.
        var @event = NewDraftEvent();
        ForceStatus(@event, editableStatus);

        var result = @event.SetVanitySlug(Slug("cleveland-show"));

        result.IsSuccess.Should().BeTrue();
        @event.VanitySlug!.Value.Should().Be("cleveland-show");
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  D16 — alias rows carry the event id (cascade-delete plumbing)
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void SetVanitySlug_RetiredAlias_HasCorrectTimestamps()
    {
        var @event = NewDraftEvent();
        @event.SetVanitySlug(Slug("old-slug"));

        var beforeRetire = DateTime.UtcNow.AddSeconds(-1);
        @event.SetVanitySlug(Slug("new-slug"));
        var afterRetire = DateTime.UtcNow.AddSeconds(1);

        var alias = @event.SlugAliases.Single();
        // RetiredAt is bounded to the call window — proves we used
        // DateTime.UtcNow (not a stale field) and didn't drop the timestamp.
        alias.RetiredAt.Should().BeAfter(beforeRetire).And.BeBefore(afterRetire);
        // ActivatedAt should fall before retirement (it's UpdatedAt or
        // CreatedAt of the parent event, set during the first SetVanitySlug).
        alias.ActivatedAt.Should().BeBefore(alias.RetiredAt);
    }
}
