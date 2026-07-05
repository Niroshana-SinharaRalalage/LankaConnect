using System;
using System.Linq;
using LankaConnect.SPLIT_PER_ENTITY.Migrations.Resources;
using Xunit;

namespace LankaConnect.Infrastructure.Tests.Data.Migrations;

/// <summary>
/// Phase 7C.2 recovery — verifies the authoritative pre-damage email template bodies
/// shipped as embedded resources load correctly and pass content invariants. These
/// invariants encode the structural facts the broken 20260421213355 migration destroyed
/// (banner, greeting, commitment-details card) so any future source change that silently
/// drops them will fail CI instead of shipping silently.
/// </summary>
public class Phase7C2RecoveryTemplatesTests
{
    [Theory]
    [InlineData("template-signup-list-commitment-confirmation")]
    [InlineData("template-signup-list-commitment-update")]
    [InlineData("template-signup-list-commitment-cancellation")]
    [InlineData("template-volunteer-commitment-confirmation")]
    [InlineData("template-volunteer-commitment-cancellation")]
    public void LoadHtml_known_template_returns_nonempty_body(string name)
    {
        var html = Phase7C2RecoveryTemplates.LoadHtml(name);

        Assert.NotNull(html);
        Assert.NotEmpty(html);
    }

    [Fact]
    public void LoadHtml_unknown_template_throws()
    {
        Assert.Throws<ArgumentException>(() =>
            Phase7C2RecoveryTemplates.LoadHtml("template-does-not-exist"));
    }

    [Theory]
    [InlineData("template-signup-list-commitment-confirmation", 60_000, 120_000)]
    [InlineData("template-signup-list-commitment-update", 60_000, 120_000)]
    [InlineData("template-signup-list-commitment-cancellation", 55_000, 120_000)]
    [InlineData("template-volunteer-commitment-confirmation", 60_000, 120_000)]
    [InlineData("template-volunteer-commitment-cancellation", 55_000, 120_000)]
    public void Body_size_is_within_expected_range(string name, int min, int max)
    {
        var html = Phase7C2RecoveryTemplates.LoadHtml(name);
        Assert.InRange(html.Length, min, max);
    }

    [Theory]
    [InlineData("template-signup-list-commitment-confirmation")]
    [InlineData("template-signup-list-commitment-update")]
    [InlineData("template-signup-list-commitment-cancellation")]
    [InlineData("template-volunteer-commitment-confirmation")]
    [InlineData("template-volunteer-commitment-cancellation")]
    public void Body_has_structural_invariants(string name)
    {
        var html = Phase7C2RecoveryTemplates.LoadHtml(name);

        Assert.Contains("<!doctype html>", html, StringComparison.Ordinal);
        Assert.Contains("{{UserName}}", html, StringComparison.Ordinal);
        Assert.Single(Split(html, "<html"));
        Assert.Single(Split(html, "</html>"));

        int openBlocks = CountOccurrences(html, "{{#");
        int closeBlocks = CountOccurrences(html, "{{/");
        Assert.True(
            openBlocks == closeBlocks,
            $"Handlebars block mismatch in {name}: {{{{# = {openBlocks} vs {{{{/ = {closeBlocks}");
    }

    [Theory]
    [InlineData("template-signup-list-commitment-confirmation")]
    [InlineData("template-signup-list-commitment-update")]
    [InlineData("template-volunteer-commitment-confirmation")]
    public void Confirmation_and_update_bodies_have_location_card(string name)
    {
        var html = Phase7C2RecoveryTemplates.LoadHtml(name);

        int eventLocationCount = CountOccurrences(html, "{{EventLocation}}");
        Assert.True(
            eventLocationCount >= 1,
            $"{name} must contain {{{{EventLocation}}}} at least once (found {eventLocationCount})");

        Assert.Contains("{{EventTitle}}", html, StringComparison.Ordinal);
        Assert.Contains("Event Date", html, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("template-signup-list-commitment-cancellation")]
    [InlineData("template-volunteer-commitment-cancellation")]
    public void Cancellation_bodies_omit_location_card_by_design(string name)
    {
        var html = Phase7C2RecoveryTemplates.LoadHtml(name);

        Assert.DoesNotContain("{{EventLocation}}", html, StringComparison.Ordinal);
        Assert.DoesNotContain("Event Date", html, StringComparison.Ordinal);
    }

    [Fact]
    public void Update_body_contains_old_and_new_quantity_tokens()
    {
        var html = Phase7C2RecoveryTemplates.LoadHtml("template-signup-list-commitment-update");
        Assert.Contains("{{OldQuantity}}", html, StringComparison.Ordinal);
        Assert.Contains("{{NewQuantity}}", html, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("template-volunteer-commitment-confirmation")]
    [InlineData("template-volunteer-commitment-cancellation")]
    public void Volunteer_bodies_reuse_signup_handlebars_contract(string name)
    {
        var html = Phase7C2RecoveryTemplates.LoadHtml(name);

        Assert.Contains("{{SignupListUrl}}", html, StringComparison.Ordinal);
        Assert.Contains("{{#HasSignUpLists}}", html, StringComparison.Ordinal);
        Assert.Contains("{{/HasSignUpLists}}", html, StringComparison.Ordinal);
        Assert.Contains("{{#HasSignupForms}}", html, StringComparison.Ordinal);
        Assert.Contains("{{/HasSignupForms}}", html, StringComparison.Ordinal);
        Assert.Contains("{{SignupFormsUrl}}", html, StringComparison.Ordinal);

        Assert.DoesNotContain("{{VolunteerListUrl}}", html, StringComparison.Ordinal);
        Assert.DoesNotContain("{{HasVolunteerLists}}", html, StringComparison.Ordinal);
        Assert.DoesNotContain("{{VolunteerFormsUrl}}", html, StringComparison.Ordinal);
        Assert.DoesNotContain("{{HasVolunteerForms}}", html, StringComparison.Ordinal);

        Assert.Contains("Volunteer", html, StringComparison.Ordinal);
    }

    private static int CountOccurrences(string haystack, string needle)
    {
        if (string.IsNullOrEmpty(needle)) return 0;
        int count = 0, i = 0;
        while ((i = haystack.IndexOf(needle, i, StringComparison.Ordinal)) != -1)
        {
            count++;
            i += needle.Length;
        }
        return count;
    }

    private static string[] Split(string haystack, string delim) =>
        haystack.Split(new[] { delim }, StringSplitOptions.None).Skip(1).ToArray();
}
