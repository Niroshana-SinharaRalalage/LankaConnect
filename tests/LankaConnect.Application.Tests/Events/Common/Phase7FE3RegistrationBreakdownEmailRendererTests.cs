using FluentAssertions;
using LankaConnect.Application.Events.Common;
using LankaConnect.Domain.Events.Enums;
using Xunit;

namespace LankaConnect.Application.Tests.Events.Common;

/// <summary>
/// Phase 7F-E.3 — RegistrationBreakdownEmailRenderer produces a self-contained inline-
/// styled HTML fragment that injects into the existing email-template
/// <c>&lt;!-- attendee-block-7e --&gt;</c> anchor. Every surface (4 templates) consumes
/// the same rendered output via one new token.
///
/// Architect rule: "N/A" is rendered explicitly when <c>BreakdownPair.Captured == false</c>;
/// no silent omission. Renderer-template coupling is intentional (single coupled point;
/// keeps logic out of templates per memory <c>feedback_regex_on_email_html.md</c>).
/// </summary>
public class Phase7FE3RegistrationBreakdownEmailRendererTests
{
    private static BreakdownPair Cap(int l, int r, string ll, string rl) =>
        new(Captured: true, Left: l, Right: r, LeftLabel: ll, RightLabel: rl);
    private static BreakdownPair NotCap(string ll, string rl) =>
        new(Captured: false, Left: 0, Right: 0, LeftLabel: ll, RightLabel: rl);

    [Fact]
    public void Renders_NonTiered_ModeA_WithBothCaptured_ProducesSingleBlock()
    {
        var bd = new RegistrationBreakdown(
            Rows: new[] { new RegistrationBreakdownRow(
                TierName: null, Count: 3,
                Age: Cap(2, 1, "Adult", "Child"),
                Gender: Cap(2, 1, "Male", "Female")) },
            TotalAttendees: 3,
            Mode: RegistrationMode.DetailedAttendees,
            IsTiered: false);

        var html = RegistrationBreakdownEmailRenderer.Render(bd, leadAttendeeName: null);

        html.Should().Contain("Total attendees");
        html.Should().Contain(">3<", because: "row count appears in the rendered block");
        html.Should().Contain("Adult/Child");
        html.Should().Contain("2/1");
        html.Should().Contain("Male/Female");
        html.Should().NotContain("N/A", because: "both axes captured for Mode A");
        html.Should().NotContain("Tier:", because: "non-tiered registration has no tier label");
    }

    [Fact]
    public void Renders_B1_NonTiered_BothAxesNotCaptured_ShowsNAForBoth()
    {
        var bd = new RegistrationBreakdown(
            Rows: new[] { new RegistrationBreakdownRow(
                TierName: null, Count: 4,
                Age: NotCap("Adult", "Child"),
                Gender: NotCap("Male", "Female")) },
            TotalAttendees: 4,
            Mode: RegistrationMode.HeadCountOnly,
            IsTiered: false);

        var html = RegistrationBreakdownEmailRenderer.Render(bd, leadAttendeeName: "Niroshana");

        html.Should().Contain("Niroshana", because: "lead attendee surfaces as a header");
        html.Should().Contain("Adult/Child");
        html.Should().Contain("Male/Female");
        // Both N/A — count = 2 occurrences
        var naCount = System.Text.RegularExpressions.Regex.Matches(html, @"N/A").Count;
        naCount.Should().Be(2);
    }

    [Fact]
    public void Renders_B2_Tiered_PerTierRow_WithTierName_AndAgeCaptured_GenderNA()
    {
        var bd = new RegistrationBreakdown(
            Rows: new[]
            {
                new RegistrationBreakdownRow("VIP", 3, Cap(2, 1, "Adult", "Child"), NotCap("Male", "Female")),
                new RegistrationBreakdownRow("General", 2, Cap(2, 0, "Adult", "Child"), NotCap("Male", "Female")),
            },
            TotalAttendees: 5,
            Mode: RegistrationMode.HeadCountByAge,
            IsTiered: true);

        var html = RegistrationBreakdownEmailRenderer.Render(bd, leadAttendeeName: "Lead");

        // Two tier rows — assert per-tier values present
        html.Should().Contain("VIP");
        html.Should().Contain("General");
        html.Should().Contain("2/1", because: "VIP age");
        html.Should().Contain("2/0", because: "General age");
        // Each row has a Male/Female cell rendered as N/A → 2 N/A
        var naCount = System.Text.RegularExpressions.Regex.Matches(html, @"N/A").Count;
        naCount.Should().Be(2);
    }

    [Fact]
    public void Renders_B3_GenderCaptured_AgeNA()
    {
        var bd = new RegistrationBreakdown(
            Rows: new[] { new RegistrationBreakdownRow(
                TierName: null, Count: 3,
                Age: NotCap("Adult", "Child"),
                Gender: Cap(2, 1, "Male", "Female")) },
            TotalAttendees: 3,
            Mode: RegistrationMode.HeadCountByGender,
            IsTiered: false);

        var html = RegistrationBreakdownEmailRenderer.Render(bd, leadAttendeeName: null);

        html.Should().Contain("N/A", because: "age N/A");
        html.Should().Contain("2/1", because: "gender captured");
    }

    [Fact]
    public void Renders_EmptyBreakdown_ReturnsEmptyString_DefensiveDefault()
    {
        var bd = new RegistrationBreakdown(
            Rows: Array.Empty<RegistrationBreakdownRow>(),
            TotalAttendees: 0,
            Mode: RegistrationMode.DetailedAttendees,
            IsTiered: false);

        var html = RegistrationBreakdownEmailRenderer.Render(bd, leadAttendeeName: null);

        html.Should().Be(string.Empty,
            "renderer returns empty when no rows so the surrounding template renders nothing");
    }

    [Fact]
    public void Renders_HtmlIsInlineStyled_OutlookCompatible()
    {
        // Architect rule: match the existing Phase 7F-A inline-styled aesthetic for
        // Outlook compatibility. Verify the output uses inline styles + table layout.
        var bd = new RegistrationBreakdown(
            Rows: new[] { new RegistrationBreakdownRow(
                TierName: "VIP", Count: 1,
                Age: Cap(1, 0, "Adult", "Child"),
                Gender: Cap(1, 0, "Male", "Female")) },
            TotalAttendees: 1,
            Mode: RegistrationMode.DetailedAttendees,
            IsTiered: true);

        var html = RegistrationBreakdownEmailRenderer.Render(bd, leadAttendeeName: null);

        // Inline style attributes (no external <link>/<style>)
        html.Should().Contain("style=\"");
        html.Should().Contain("<table", because: "Outlook-compatible layout uses tables");
        html.Should().NotContain("<link");
        html.Should().NotContain("class=\"", because: "no class hooks — inline-only for email");
    }

    [Fact]
    public void Renders_HtmlEscapesInjectedContent()
    {
        // Lead name with a quote / angle bracket should not produce broken HTML.
        var bd = new RegistrationBreakdown(
            Rows: new[] { new RegistrationBreakdownRow(
                TierName: null, Count: 1,
                Age: NotCap("Adult", "Child"),
                Gender: NotCap("Male", "Female")) },
            TotalAttendees: 1,
            Mode: RegistrationMode.HeadCountOnly,
            IsTiered: false);

        var html = RegistrationBreakdownEmailRenderer.Render(bd, leadAttendeeName: "Bob \"O'Brien\" <script>");

        html.Should().NotContain("<script>", because: "tag must be HTML-encoded");
        html.Should().Contain("&lt;script&gt;");
    }
}
