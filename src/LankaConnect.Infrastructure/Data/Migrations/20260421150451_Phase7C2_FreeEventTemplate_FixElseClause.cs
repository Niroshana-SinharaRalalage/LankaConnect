using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.SPLIT_PER_ENTITY.Migrations
{
    /// <inheritdoc />
    /// <summary>
    /// Phase 7C.2 pilot follow-up: the template block shipped in
    /// Phase7C2_RewriteFreeEventTemplate_Pilot used a Handlebars-style
    /// <c>{{#if HasLocationName}}...{{else}}{{EventLocation}}{{/if}}</c> clause.
    /// The custom template engine in <c>AzureEmailService.RenderTemplateContent</c>
    /// does NOT understand <c>{{else}}</c> — it only strips the whole
    /// <c>{{#if}}...{{/if}}</c> block when falsy, or keeps the entire content
    /// verbatim (including literal <c>{{else}}</c>) when truthy. Result: the pilot
    /// email rendered both branches and leaked a literal <c>{{else}}</c> into the
    /// Location row.
    ///
    /// Fix: collapse to two independent blocks.
    ///   {{#if HasLocationName}}&lt;bold name&gt;{{/if}}
    ///   &lt;address&gt;{{LocationAddress}}&lt;/address&gt;       ← always rendered
    /// The Application-layer projection now falls back <c>LocationAddress</c> to
    /// "Online Event" when an event has no physical address, so the template has
    /// no need for a legacy-fallback branch.
    ///
    /// Safety: the old block is a unique literal inside the single pilot template,
    /// so REPLACE is safe (per Phase 6A.117 rule: REPLACE for single-token
    /// substitutions, REGEXP_REPLACE only for multi-line whitespace-sensitive
    /// patterns). Row-count assertion guards against silent 0-row updates.
    /// </summary>
    public partial class Phase7C2_FreeEventTemplate_FixElseClause : Migration
    {
        // Buggy block shipped in Phase7C2_RewriteFreeEventTemplate_Pilot.
        private const string OldBlock =
            "{{#if HasLocationName}}" +
                "<span style=\"display:block;font-weight:700;color:#111827;font-size:14px;\">{{LocationName}}</span>" +
                "<span style=\"display:block;font-weight:500;color:#374151;font-size:13px;margin-top:2px;\">{{LocationAddress}}</span>" +
            "{{else}}" +
                "{{EventLocation}}" +
            "{{/if}}" +
            "{{#if HasSecondaryLocation}}" +
                "<span style=\"display:block;margin-top:14px;font-size:10px;font-weight:600;text-transform:uppercase;letter-spacing:1.2px;color:#9ca3af;\">{{SecondaryLocationLabel}}</span>" +
                "{{#if HasSecondaryLocationName}}" +
                    "<span style=\"display:block;font-weight:700;color:#111827;font-size:14px;margin-top:2px;\">{{SecondaryLocationName}}</span>" +
                "{{/if}}" +
                "<span style=\"display:block;font-weight:500;color:#374151;font-size:13px;margin-top:2px;\">{{SecondaryLocationAddress}}</span>" +
            "{{/if}}";

        // Fixed block: no {{else}}. LocationAddress is guaranteed non-empty by the
        // Application-layer projection (falls back to "Online Event").
        private const string NewBlock =
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

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($@"
                DO $migration$
                DECLARE affected INT;
                BEGIN
                    UPDATE communications.email_templates
                    SET html_template = REPLACE(
                        html_template,
                        '{OldBlock.Replace("'", "''")}',
                        '{NewBlock.Replace("'", "''")}'
                    )
                    WHERE name = 'template-free-event-registration-confirmation'
                      AND html_template LIKE '%{{{{else}}}}{{{{EventLocation}}}}%';

                    GET DIAGNOSTICS affected = ROW_COUNT;
                    IF affected = 0 THEN
                        RAISE EXCEPTION 'Phase 7C.2 fix: template-free-event-registration-confirmation did not contain the buggy {{{{else}}}} block - migration not applied';
                    END IF;
                END $migration$;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($@"
                DO $migration$
                DECLARE affected INT;
                BEGIN
                    UPDATE communications.email_templates
                    SET html_template = REPLACE(
                        html_template,
                        '{NewBlock.Replace("'", "''")}',
                        '{OldBlock.Replace("'", "''")}'
                    )
                    WHERE name = 'template-free-event-registration-confirmation';

                    GET DIAGNOSTICS affected = ROW_COUNT;
                    IF affected = 0 THEN
                        RAISE EXCEPTION 'Phase 7C.2 fix rollback: could not find the fixed block in template-free-event-registration-confirmation';
                    END IF;
                END $migration$;
            ");
        }
    }
}
