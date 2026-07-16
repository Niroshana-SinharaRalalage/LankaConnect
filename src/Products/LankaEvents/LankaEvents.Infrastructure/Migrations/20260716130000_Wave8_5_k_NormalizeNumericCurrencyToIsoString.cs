using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Products.LankaEvents.Infrastructure.Migrations
{
    /// <summary>
    /// Wave 8.5.k (Phase A.5) — normalize NUMERIC Currency values in the events
    /// table JSONB columns to their canonical ISO 4217 string form ("USD"). Follow-up
    /// to Wave 8.5.j after a staging probe (2026-07-16 12:30 UTC) discovered the
    /// actual bad-data shape is <c>{"Amount": 0, "Currency": 1}</c> — a JSON number
    /// — not the nested Currency-object form Wave 8.5.j targeted.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Root cause corrected:</b> pre-Consult-#25 (2026-07-14 CurrencyValueConverter
    /// landing commit d3a6b1d3) write paths serialized Currency as a raw integer
    /// (values observed: <c>0</c>, <c>1</c>). Post-Consult-#25 writes emit plain
    /// ISO 4217 strings ("USD"). EF Core 8's <c>OwnsOne(...).ToJson()</c>
    /// MaterializeJsonEntity reader is shape-locked to the string form and throws
    /// <c>Cannot get token type 'Number' as string</c> when it hits a numeric row.
    /// </para>
    /// <para>
    /// <b>Staging probe (2026-07-16 12:30 UTC):</b>
    /// <list type="bullet">
    ///   <item><c>events.ticket_price</c>: 504 rows Currency=1 + 28 rows Currency=0 + 183 rows Currency="USD"</item>
    ///   <item><c>events.pricing</c>: 505 numeric + 169 string</item>
    ///   <item><c>events.revenue_breakdown</c>: 481 numeric + 169 string</item>
    ///   <item><c>events.sponsor_config / add_on_config / donation_config / collection_config</c>: 0 numeric — no fix needed</item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Mapping decision:</b> 100% of the post-Consult-#25 string-normalized rows
    /// are "USD" (verified via staging probe sample). Both numeric values (0 and 1)
    /// map to "USD". LankaEvents is a global Sri Lankan-diaspora platform with USD
    /// as the platform-default currency (LKR usage would surface separately in
    /// per-event overrides that would have been written post-Consult-#25 as strings).
    /// If prod cutover finds a non-USD legacy row, PITR restore (7-day retention)
    /// per docs/operations/migration-rollback.md.
    /// </para>
    /// <para>
    /// <b>Migration strategy:</b> recursive PL/pgSQL function walks each JSONB tree,
    /// rewrites any <c>currency</c>/<c>Currency</c> key whose value is a JSON number
    /// (any numeric value) to the string <c>"USD"</c>. Idempotent — re-run is a no-op
    /// (already-string values are unchanged).
    /// </para>
    /// <para>
    /// <b>Idempotent SQL for commit body</b> (per CLAUDE.md §5 rule 1): the UPDATE
    /// statements narrow with a regex WHERE clause + JSON-typeof check, so re-runs
    /// touch zero rows.
    /// </para>
    /// </remarks>
    public partial class Wave8_5_k_NormalizeNumericCurrencyToIsoString : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Recursive PL/pgSQL function that normalizes any numeric Currency value
            // to its plain ISO 4217 string form ("USD").
            migrationBuilder.Sql(@"
                CREATE OR REPLACE FUNCTION events._wave85k_normalize_numeric_currency(j jsonb)
                RETURNS jsonb
                LANGUAGE plpgsql
                IMMUTABLE
                AS $func$
                DECLARE
                    result jsonb;
                    k text;
                    v jsonb;
                BEGIN
                    IF j IS NULL THEN
                        RETURN NULL;
                    END IF;

                    IF jsonb_typeof(j) = 'object' THEN
                        result := '{}'::jsonb;
                        FOR k, v IN SELECT * FROM jsonb_each(j) LOOP
                            IF (lower(k) = 'currency')
                                AND jsonb_typeof(v) = 'number'
                            THEN
                                -- Numeric Currency → canonical 'USD' per Wave 8.5.k mapping ruling.
                                result := result || jsonb_build_object(k, to_jsonb('USD'::text));
                            ELSE
                                -- Recurse into nested objects / arrays; leave scalars unchanged.
                                result := result || jsonb_build_object(k, events._wave85k_normalize_numeric_currency(v));
                            END IF;
                        END LOOP;
                        RETURN result;
                    ELSIF jsonb_typeof(j) = 'array' THEN
                        RETURN (SELECT jsonb_agg(events._wave85k_normalize_numeric_currency(elem))
                                FROM jsonb_array_elements(j) elem);
                    ELSE
                        RETURN j;
                    END IF;
                END;
                $func$;
            ");

            // Apply to the 3 columns that showed numeric-Currency rows in the staging probe.
            // WHERE clause narrows to rows containing a numeric Currency value so already-
            // normalized rows are untouched (idempotent no-op on re-run).
            migrationBuilder.Sql(@"
                UPDATE events.events
                SET ticket_price = events._wave85k_normalize_numeric_currency(ticket_price)
                WHERE ticket_price IS NOT NULL
                  AND ticket_price::text ~ '""[Cc]urrency""\s*:\s*[0-9]';
            ");

            migrationBuilder.Sql(@"
                UPDATE events.events
                SET pricing = events._wave85k_normalize_numeric_currency(pricing)
                WHERE pricing IS NOT NULL
                  AND pricing::text ~ '""[Cc]urrency""\s*:\s*[0-9]';
            ");

            migrationBuilder.Sql(@"
                UPDATE events.events
                SET revenue_breakdown = events._wave85k_normalize_numeric_currency(revenue_breakdown)
                WHERE revenue_breakdown IS NOT NULL
                  AND revenue_breakdown::text ~ '""[Cc]urrency""\s*:\s*[0-9]';
            ");

            // Defensive: also apply to the other 4 ToJson columns even though the probe
            // showed 0 numeric-Currency rows in each. This is a no-op today but ensures
            // any drift written to those columns pre-Consult-#25 gets caught. Cheap.
            migrationBuilder.Sql(@"
                UPDATE events.events
                SET sponsor_config = events._wave85k_normalize_numeric_currency(sponsor_config)
                WHERE sponsor_config IS NOT NULL
                  AND sponsor_config::text ~ '""[Cc]urrency""\s*:\s*[0-9]';
            ");

            migrationBuilder.Sql(@"
                UPDATE events.events
                SET add_on_config = events._wave85k_normalize_numeric_currency(add_on_config)
                WHERE add_on_config IS NOT NULL
                  AND add_on_config::text ~ '""[Cc]urrency""\s*:\s*[0-9]';
            ");

            migrationBuilder.Sql(@"
                UPDATE events.events
                SET donation_config = events._wave85k_normalize_numeric_currency(donation_config)
                WHERE donation_config IS NOT NULL
                  AND donation_config::text ~ '""[Cc]urrency""\s*:\s*[0-9]';
            ");

            migrationBuilder.Sql(@"
                UPDATE events.events
                SET collection_config = events._wave85k_normalize_numeric_currency(collection_config)
                WHERE collection_config IS NOT NULL
                  AND collection_config::text ~ '""[Cc]urrency""\s*:\s*[0-9]';
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Intentional no-op. Reverting canonical "USD" strings back to legacy numeric
            // form would BREAK the current EF Core 8 reader path. Rely on Azure Postgres
            // PITR (7-day retention) for rollback per docs/operations/migration-rollback.md.
            //
            // The helper function created in Up() persists — dropping it here would leave
            // any downstream ad-hoc normalization needs without the utility.
        }
    }
}
