using FluentAssertions;
using LankaConnect.Modules.Communications.Contracts.Email.Helpers;

namespace LankaConnect.Shared.Tests.Email.Helpers;

/// <summary>
/// Phase 7C.2b: invariants on the canonical decomposed-location Handlebars block.
/// This is the SINGLE source of truth for the venue-name + address + secondary
/// location fragment that every event-email template uses in place of the flat
/// <c>{{EventLocation}}</c> token. Chunk 1/2/3 migrations all REPLACE
/// <c>{{EventLocation}}</c> with this exact string — so any drift here breaks
/// every downstream template in lockstep and is caught by these tests.
/// </summary>
public class EmailLocationBlockHtmlTests
{
    [Fact]
    public void DecomposedBlock_contains_all_required_placeholders()
    {
        var block = EmailLocationBlockHtml.DecomposedBlock;

        block.Should().Contain("{{#if HasLocationName}}");
        block.Should().Contain("{{LocationName}}");
        block.Should().Contain("{{LocationAddress}}");
        block.Should().Contain("{{#if HasSecondaryLocation}}");
        block.Should().Contain("{{SecondaryLocationLabel}}");
        block.Should().Contain("{{#if HasSecondaryLocationName}}");
        block.Should().Contain("{{SecondaryLocationName}}");
        block.Should().Contain("{{SecondaryLocationAddress}}");
    }

    [Fact]
    public void DecomposedBlock_does_not_contain_else_clause()
    {
        // The custom template engine in AzureEmailService.RenderTemplateContent does
        // NOT support Handlebars {{else}} — it would leak a literal "{{else}}" into
        // the rendered email body. Phase 7C.2 pilot shipped {{else}} and had to be
        // patched by 20260421150451_Phase7C2_FreeEventTemplate_FixElseClause. The
        // canonical block must never reintroduce it.
        var block = EmailLocationBlockHtml.DecomposedBlock;

        block.Should().NotContain("{{else}}");
        block.Should().NotContain("{{ else }}");
        block.Should().NotContain("{{/else}}");
    }

    [Fact]
    public void DecomposedBlock_does_not_contain_legacy_EventLocation_token()
    {
        // {{EventLocation}} is the flat-string fallback the new block REPLACES.
        // If the block itself contained {{EventLocation}}, the migration's idempotency
        // check would permanently succeed on the post-migration body (the token would
        // still be present) and the next rerun would recursively nest the block inside
        // itself.
        var block = EmailLocationBlockHtml.DecomposedBlock;

        block.Should().NotContain("{{EventLocation}}");
    }

    [Fact]
    public void DecomposedBlock_if_blocks_are_balanced()
    {
        var block = EmailLocationBlockHtml.DecomposedBlock;

        var openCount = CountOccurrences(block, "{{#if");
        var closeCount = CountOccurrences(block, "{{/if}}");

        openCount.Should().Be(3, "block has exactly 3 conditional branches: HasLocationName, HasSecondaryLocation, HasSecondaryLocationName");
        closeCount.Should().Be(openCount, "every {{#if}} must have a matching {{/if}} or the custom engine leaves literal tags in the rendered output");
    }

    [Fact]
    public void DecomposedBlock_uses_span_not_p_for_block_elements()
    {
        // Phase 7C.2 pilot lesson: the outer container of {{EventLocation}} in most
        // templates is a <p> element, which cannot legally contain other block-level
        // elements. Every line in the decomposed block uses <span style="display:block">
        // so the rendered HTML is valid regardless of the outer wrapper.
        var block = EmailLocationBlockHtml.DecomposedBlock;

        block.Should().NotContain("<p ");
        block.Should().NotContain("<div");
        block.Should().Contain("<span", "each line of the block must be a span-with-display-block to avoid inline-inside-block HTML errors");
    }

    [Fact]
    public void DecomposedBlock_matches_free_event_pilot_NewBlock_byte_for_byte()
    {
        // The pilot migration Phase7C2_FreeEventTemplate_FixElseClause already shipped
        // this block on template-free-event-registration-confirmation, which is the
        // ONLY event-email template currently rendering multi-venue correctly on
        // staging. To keep the other 15 templates visually consistent with it, the
        // canonical constant must equal the pilot's NewBlock byte-for-byte.
        //
        // We reproduce the exact NewBlock literal from
        // src/LankaConnect.Infrastructure/Data/Migrations/20260421150451_Phase7C2_FreeEventTemplate_FixElseClause.cs:52-63
        // inline here so this test is independent of the Infrastructure project (the
        // Shared tests project does not reference Infrastructure).
        var expectedFromPilot =
            "{{#if HasLocationName}}" +
                "<span style=\"display:block;font-weight:700;color:#111827;font-size:14px;\">{{LocationName}}</span>" +
            "{{/if}}" +
            "<span style=\"display:block;font-weight:500;color:#374151;font-size:13px;margin-top:2px;\">{{LocationAddress}}</span>" +
            "{{#if HasSecondaryLocation}}" +
                "<span style=\"display:block;margin-top:14px;font-size:10px;font-weight:600;text-transform:uppercase;letter-spacing:1.2px;color:#9ca3af;\">{{SecondaryLocationLabel}}</span>" +
                "{{#if HasSecondaryLocationName}}" +
                    "<span style=\"display:block;font-weight:700;color:#111827;font-size:14px;margin-top:2px;\">{{SecondaryLocationName}}</span>" +
                "{{/if}}" +
                "<span style=\"display:block;font-weight:500;color:#374151;font-size:13px;margin-top:2px;\">{{SecondaryLocationAddress}}</span>" +
            "{{/if}}";

        EmailLocationBlockHtml.DecomposedBlock.Should().Be(expectedFromPilot);
    }

    private static int CountOccurrences(string haystack, string needle)
    {
        if (string.IsNullOrEmpty(needle)) return 0;
        var count = 0;
        var i = 0;
        while ((i = haystack.IndexOf(needle, i, StringComparison.Ordinal)) != -1)
        {
            count++;
            i += needle.Length;
        }
        return count;
    }
}
