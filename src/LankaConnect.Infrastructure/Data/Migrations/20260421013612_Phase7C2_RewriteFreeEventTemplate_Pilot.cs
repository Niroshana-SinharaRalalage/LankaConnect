using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.SPLIT_PER_ENTITY.Migrations
{
    /// <inheritdoc />
    /// <summary>
    /// Phase 7C.2 pilot: rewrite the single {{EventLocation}} placeholder inside
    /// template-free-event-registration-confirmation so it renders the new Phase 7C.1
    /// decomposed location layout — bolded venue name, full 5-part address, and the
    /// optional "Parking Lot" / "Secondary Venue" secondary block.
    ///
    /// Pilot scope: only this one template. The remaining 12 event-email templates are
    /// updated in a later migration once this pilot is visually verified in staging.
    ///
    /// Safety: {{EventLocation}} is a unique literal string (no whitespace variance),
    /// so REPLACE is safe here — the Phase 6A.117 lesson about REGEXP_REPLACE applies
    /// to multi-line HTML blocks, not single-token placeholders. Row count is asserted
    /// via GET DIAGNOSTICS to guard against silent 0-row updates.
    /// </summary>
    public partial class Phase7C2_RewriteFreeEventTemplate_Pilot : Migration
    {
        // The new block that replaces the single {{EventLocation}} placeholder.
        // Uses <span style="display:block"> because the outer <p> cannot contain block elements.
        // Handlebars: falls back to legacy {{EventLocation}} when no LocationName is set,
        // so pre-Phase-7C.1 events with unnamed venues still render correctly.
        private const string NewLocationBlock =
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

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Replace the single {{EventLocation}} placeholder. Row-count assertion
            // catches the Phase 6A.117 silent-failure case (template already migrated,
            // or template missing entirely).
            migrationBuilder.Sql($@"
                DO $migration$
                DECLARE affected INT;
                BEGIN
                    UPDATE communications.email_templates
                    SET html_template = REPLACE(
                        html_template,
                        '{{{{EventLocation}}}}',
                        '{NewLocationBlock.Replace("'", "''")}'
                    )
                    WHERE name = 'template-free-event-registration-confirmation'
                      AND html_template LIKE '%{{{{EventLocation}}}}%';

                    GET DIAGNOSTICS affected = ROW_COUNT;
                    IF affected = 0 THEN
                        RAISE EXCEPTION 'Phase 7C.2 pilot: template-free-event-registration-confirmation did not contain {{{{EventLocation}}}} placeholder - migration not applied';
                    END IF;
                END $migration$;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Revert: swap the new block back to {{EventLocation}}.
            migrationBuilder.Sql($@"
                DO $migration$
                DECLARE affected INT;
                BEGIN
                    UPDATE communications.email_templates
                    SET html_template = REPLACE(
                        html_template,
                        '{NewLocationBlock.Replace("'", "''")}',
                        '{{{{EventLocation}}}}'
                    )
                    WHERE name = 'template-free-event-registration-confirmation';

                    GET DIAGNOSTICS affected = ROW_COUNT;
                    IF affected = 0 THEN
                        RAISE EXCEPTION 'Phase 7C.2 pilot rollback: could not find new location block in template-free-event-registration-confirmation';
                    END IF;
                END $migration$;
            ");
        }
    }
}
