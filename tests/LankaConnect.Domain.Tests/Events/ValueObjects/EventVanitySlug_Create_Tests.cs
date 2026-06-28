using FluentAssertions;
using LankaConnect.Products.LankaEvents.Domain.ValueObjects;
using Xunit;

namespace LankaConnect.Domain.Tests.Events.ValueObjects;

/// <summary>
/// Phase 6A.154 — pins the <see cref="EventVanitySlug.Create"/> contract.
///
/// The VO is the single source of truth for slug shape; every entry point
/// (commands, validators, FE form via slug-config endpoint) must produce the
/// exact same Accept/Reject set. These tests lock it down.
///
/// Coverage matrix (16 cases): 12 slug-shape tests + 4 reserved-words tests.
/// Mutator-on-Event tests live in <c>Event_SetVanitySlug_Tests</c>.
/// </summary>
public class EventVanitySlug_Create_Tests
{
    // ─────────────────────────────────────────────────────────────────────────
    //  Happy path — valid slugs
    // ─────────────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("cleveland-show")]
    [InlineData("annual-gala-2026")]
    [InlineData("xyz")] // 3-char minimum boundary
    [InlineData("aaa-bbb-ccc-ddd-eee")] // multiple hyphens (non-consecutive)
    [InlineData("event-with-digits-123")] // digits in middle/end allowed
    public void Create_WithValidSlug_Succeeds(string input)
    {
        var result = EventVanitySlug.Create(input);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Value.Should().Be(input);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Null/empty/whitespace — "no slug" (legacy backward-compatible default)
    // ─────────────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    public void Create_WithNullOrEmpty_ReturnsSuccessNull(string? input)
    {
        // Architect D7: default null (organizer opt-in). Null / empty /
        // whitespace all mean "no slug" — the organizer just didn't set one.
        // This is not a validation failure; it's the legacy backward-compatible
        // path. Aggregates that never set a slug stay null forever.
        var result = EventVanitySlug.Create(input);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeNull();
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Length violations
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Create_TooShort_Fails()
    {
        // 2 chars — below MinLength=3. Architect D2: 3-char floor blocks
        // `/a` and `/ab` style collisions with future short routes.
        var result = EventVanitySlug.Create("ab");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("at least 3");
    }

    [Fact]
    public void Create_TooLong_Fails()
    {
        // 81 chars — above MaxLength=80. Ceiling chosen for B-tree page +
        // URL ergonomics (no one shares an 80-char "short" URL).
        var input = new string('a', 81);

        var result = EventVanitySlug.Create(input);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("80");
    }

    [Fact]
    public void Create_AtMaxBoundary_Succeeds()
    {
        // Exactly 80 chars — inclusive upper bound.
        var input = new string('a', 80);

        var result = EventVanitySlug.Create(input);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Value.Should().Be(input);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Character / shape violations
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Create_MixedCase_Fails()
    {
        // D5: do NOT silently lowercase — surface error so organizer sees
        // the canonical form. Hiding this would breed `Show` vs `show`
        // confusion later when they wonder why their URL is different.
        var result = EventVanitySlug.Create("Cleveland-Show");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("lowercase");
    }

    [Fact]
    public void Create_LeadingHyphen_Fails()
    {
        var result = EventVanitySlug.Create("-cleveland");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("start with a letter");
    }

    [Fact]
    public void Create_TrailingHyphen_Fails()
    {
        // Architect D2: trailing hyphen rejected — visually ugly in shared
        // links and ambiguous with terminal punctuation in social media
        // copy-paste ("...check out lankaconnect.app/event-." breaks the URL).
        var result = EventVanitySlug.Create("cleveland-");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("cannot end with a hyphen");
    }

    [Fact]
    public void Create_ConsecutiveHyphens_Fails()
    {
        // Architect D2: double hyphens look like typos.
        var result = EventVanitySlug.Create("cleve--land");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("consecutive hyphens");
    }

    [Fact]
    public void Create_LeadingDigit_Fails()
    {
        // Architect D2: leading digit rejected to keep the slug namespace
        // distinct from any future numeric-ID route convention. A URL like
        // `/3-musketeers` is fine readability-wise but creates ambiguity
        // when ID-based routes ship.
        var result = EventVanitySlug.Create("3-musketeers");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("start with a letter");
    }

    [Fact]
    public void Create_Underscore_Fails()
    {
        // Architect D2: underscores confuse copy-paste (some clients
        // auto-link past the underscore, others don't) and look ambiguous
        // with hyphens. Pick one separator — hyphen wins.
        var result = EventVanitySlug.Create("club_event");

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("lowercase letters, digits, and hyphens");
    }

    [Fact]
    public void Create_Unicode_Fails()
    {
        // D2: ASCII-only. Sinhala/Tamil slugs require Punycode/IDN which is
        // a separate phase (deferred; documented in Master TODO risks R9).
        var result = EventVanitySlug.Create("café-night");

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Create_DotsOrSpaces_Fails()
    {
        var result = EventVanitySlug.Create("event.party");

        result.IsFailure.Should().BeTrue();
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Reserved words (D14) — single source of truth in the VO
    // ─────────────────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("events")]       // top-level route — direct shadow risk
    [InlineData("login")]        // auth route
    [InlineData("dashboard")]    // dashboard group
    [InlineData("lankaconnect")] // brand block
    [InlineData("admin")]        // future namespace (no admin UI today,
                                 //   but reserve before squatter gets it)
    [InlineData("api")]          // backend API route prefix
    // Note: `_next`, `.well-known`, `robots.txt` etc. are in ReservedSlugs but
    // they fail the regex (underscores / dots) before reaching the reserved
    // check. That's fine — they're rejected either way. Only test reserved
    // values that PASS the shape check to exercise the reserved-words branch.
    public void Create_ReservedSlug_Fails(string reserved)
    {
        var result = EventVanitySlug.Create(reserved);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("reserved");
        // Message must include the offending word so the organizer's form
        // error reads "'events' is reserved" rather than a generic "invalid".
        result.Error.Should().Contain($"'{reserved}'");
    }

    [Fact]
    public void ReservedSlugs_IncludesAllKnownTopLevelRoutes()
    {
        // Sanity-pin: every top-level directory listed in the Master TODO
        // appears in the const. If a new top-level route ships without
        // landing in this list, the build-time CI test (substream M)
        // catches it; this unit test catches the regression earlier still.
        var expectedTopLevel = new[]
        {
            "about", "blog", "business", "contact", "dashboard", "events",
            "forums", "guidelines", "help", "marketplace", "newsletter",
            "newsletters", "safety", "search", "templates",
            "login", "register", "verify-email",
            "notifications", "profile",
        };

        foreach (var route in expectedTopLevel)
        {
            EventVanitySlug.ReservedSlugs.Should().Contain(route,
                $"`{route}` is a real top-level route — must be in ReservedSlugs to prevent slug shadowing");
        }
    }
}
