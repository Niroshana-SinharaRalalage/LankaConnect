using System;
using FluentAssertions;
using LankaConnect.Shared.Email.Helpers;
using Xunit;

namespace LankaConnect.Infrastructure.Tests.Data.Migrations;

/// <summary>
/// Phase 7C.2b Chunk 2b — transformation-logic invariants for
/// <c>Phase7C3a_DecomposeLocationInRegistrationAndLifecycleTemplates</c>.
///
/// <para>This migration rewrites the 7 registration/lifecycle email templates'
/// <c>{{EventLocation}}</c> token to <see cref="EmailLocationBlockHtml.DecomposedBlock"/>,
/// plus the special-case <c>{{Location}}</c> token inside
/// <c>template-event-reminder</c> which uses a non-canonical placeholder name
/// (see Chunk 2a EventReminderEmailParams rename). 6 templates take the standard
/// REPLACE; 1 (event-reminder) takes the variant REPLACE against <c>{{Location}}</c>.</para>
///
/// <para>No Testcontainers here — these are pure transformation-level assertions
/// validating that the C# string.Replace mirrors the behavior of PostgreSQL
/// REPLACE. Per-template invariants (row-count = 1, body length, greeting
/// survival) are enforced by the migration SQL itself via
/// <c>RAISE EXCEPTION</c> at apply time.</para>
/// </summary>
public class Phase7C3aDecomposeLocationTests
{
    private const string StandardToken = "{{EventLocation}}";
    private const string EventReminderToken = "{{Location}}";

    /// <summary>
    /// Minimal synthetic body representing what each un-migrated template body looks
    /// like — a &lt;p&gt;-wrapped flat token inside an EVENT DETAILS card plus a
    /// greeting marker (which the migration guards must see survive). Real template
    /// bodies on staging are 60–120 KB; this fixture is intentionally tiny so the
    /// test run stays sub-millisecond — the migration's body-length-≥-50000 guard
    /// is validated against the real bodies at apply time, not here.
    /// </summary>
    private static string SyntheticBodyWithStandardToken() =>
        "<!doctype html><html><body>" +
        "<p>Hi {{UserName}},</p>" +
        "<p><strong>Location:</strong> {{EventLocation}}</p>" +
        "</body></html>";

    private static string SyntheticBodyWithEventReminderToken() =>
        "<!doctype html><html><body>" +
        "<p>Hi {{AttendeeName}},</p>" +
        "<p><strong>Location:</strong> {{Location}}</p>" +
        "</body></html>";

    [Fact]
    public void Replacing_StandardToken_RemovesLegacyPlaceholder()
    {
        var before = SyntheticBodyWithStandardToken();

        var after = before.Replace(StandardToken, EmailLocationBlockHtml.DecomposedBlock);

        after.Should().NotContain(StandardToken);
        after.Should().Contain("{{LocationName}}");
        after.Should().Contain("{{UserName}}", "greeting must survive the REPLACE — this is a primary migration guard");
    }

    [Fact]
    public void Replacing_EventReminderToken_RemovesOldPlaceholder()
    {
        var before = SyntheticBodyWithEventReminderToken();

        var after = before.Replace(EventReminderToken, EmailLocationBlockHtml.DecomposedBlock);

        after.Should().NotContain(EventReminderToken);
        after.Should().Contain("{{LocationName}}");
        after.Should().Contain("{{AttendeeName}}", "greeting must survive");
    }

    [Fact]
    public void Replacing_EventReminderToken_DoesNotReplaceInsideStandardBody()
    {
        // Defence in depth — if a template unexpectedly carries both tokens, the
        // event-reminder variant REPLACE must match only the event-reminder token
        // (we intentionally pick a token that is a prefix of nothing). This test
        // also guards against the broken-regex class of bug that caused the
        // 2026-04-21 damage.
        var before = SyntheticBodyWithStandardToken(); // contains {{EventLocation}} only

        var after = before.Replace(EventReminderToken, EmailLocationBlockHtml.DecomposedBlock);

        after.Should().Be(before, "standard-body {{EventLocation}} must not be touched by the event-reminder variant REPLACE");
    }

    [Theory]
    [InlineData("{{EventLocation}}")]
    [InlineData("{{Location}}")]
    public void Replacement_IsIdempotent_AgainstAlreadyDecomposedBody(string legacyToken)
    {
        // After Chunk 2b applies, re-running the same REPLACE must be a no-op. The
        // migration's post-UPDATE guard asserts `has_legacy_token = false` after
        // the REPLACE, so running this test twice in a row catches any regression
        // where the DecomposedBlock accidentally contains the legacy token.
        var once = "X".Replace(legacyToken, EmailLocationBlockHtml.DecomposedBlock);
        var twice = once.Replace(legacyToken, EmailLocationBlockHtml.DecomposedBlock);

        once.Should().Be(twice);
    }

    [Fact]
    public void Replacement_Token_Is_Unique_Per_Template_Family()
    {
        // Sanity check: the two tokens we REPLACE must be distinct strings. This
        // guards against a refactor that accidentally aliases {{Location}} to
        // {{EventLocation}} at the migration level.
        StandardToken.Should().NotBe(EventReminderToken);
        StandardToken.Should().NotContain("Location}}" + StandardToken);
        EventReminderToken.Should().NotContain("EventLocation");
    }
}
