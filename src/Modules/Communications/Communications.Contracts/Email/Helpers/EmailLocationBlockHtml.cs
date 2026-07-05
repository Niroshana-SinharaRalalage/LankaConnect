namespace LankaConnect.Shared.Email.Helpers;

/// <summary>
/// Phase 7C.2b: single canonical source for the Handlebars HTML fragment that
/// renders an event's primary and optional secondary location in email bodies.
///
/// <para>
/// Every event-email template in the system used to contain the flat
/// <c>{{EventLocation}}</c> token, which produced a single-line "Street, City"
/// string with no venue-name bolding and no multi-venue support. Phase 7C.2
/// introduced a decomposed projection (<see cref="LocationEmailProjection"/>)
/// plus a shared dictionary writer (<see cref="LocationEmailDictionaryWriter"/>)
/// to emit 8 discrete Handlebars keys; this class owns the HTML fragment that
/// consumes those keys.
/// </para>
/// <para>
/// Chunk 1 / 2 / 3 migrations each run
/// <c>REPLACE(html_template, '{{EventLocation}}', <see cref="DecomposedBlock"/>)</c>
/// against their batch of templates. Keeping the fragment in exactly one place
/// prevents the per-template drift that caused the 10-template gap Phase 7C.2
/// closed out with this chunk.
/// </para>
/// <para>
/// Contract — every line of the fragment uses <c>&lt;span style="display:block"&gt;</c>
/// rather than block-level elements (<c>&lt;p&gt;</c>, <c>&lt;div&gt;</c>), so the
/// fragment can be dropped inside a <c>&lt;p&gt;</c> wrapper without producing
/// invalid inline-inside-block HTML. The custom template engine in
/// <c>AzureEmailService.RenderTemplateContent</c> does not support Handlebars
/// <c>{{else}}</c> (it would render the else literal as text) — the decomposed
/// shape therefore uses two sibling <c>{{#if}}</c> blocks instead. The
/// Application-layer projection guarantees <c>LocationAddress</c> is non-empty
/// by falling back to <c>"Online Event"</c>, so no else branch is needed.
/// </para>
/// </summary>
public static class EmailLocationBlockHtml
{
    /// <summary>
    /// Phase 7C.2b: the canonical decomposed-location Handlebars block.
    ///
    /// <para>Byte-identical to the final <c>NewBlock</c> shipped in
    /// <c>20260421150451_Phase7C2_FreeEventTemplate_FixElseClause.cs:52-63</c> (the
    /// only template currently rendering multi-venue correctly on staging). Changing
    /// this constant changes the rendering of every event-location email in the
    /// system — treat as load-bearing.</para>
    /// </summary>
    public const string DecomposedBlock =
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
}
