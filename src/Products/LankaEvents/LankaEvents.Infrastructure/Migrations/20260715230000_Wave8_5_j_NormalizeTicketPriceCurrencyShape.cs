using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LankaConnect.Products.LankaEvents.Infrastructure.Migrations
{
    /// <summary>
    /// Wave 8.5.j (Phase A.5) — normalize Currency-containing JSONB values in the
    /// events table so EF Core 8's <c>OwnsOne(...).ToJson()</c> MaterializeJsonEntity
    /// reader can hydrate them cleanly. Per Consult #26 Q3 Option (i) data migration
    /// (2026-07-14 ruling deferred to Phase A.5, executed 2026-07-15).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Root cause: pre-Consult-#23 writes stored <c>currency</c> as a nested Currency
    /// value object (<c>{"code": "USD", "name": "US Dollar", "symbol": "$", "decimalDigits": 2}</c>).
    /// Post-Consult-#23 (commit <c>d3a6b1d3</c>, 2026-07-10) added <c>CurrencyValueConverter</c>
    /// so new writes emit <c>"currency": "USD"</c> (plain ISO 4217 string). The EF Core 8
    /// reader for <c>ToJson</c> owned entities expects the plain string form; hitting a
    /// legacy nested-object row throws <c>Cannot get token type 'Object' as string</c>
    /// during <c>Utf8JsonReader.GetString()</c> inside
    /// <c>JsonConvertedValueReaderWriter.FromJsonTyped</c>. Wave 9 smoke observed
    /// <c>'Number' as string</c> variant — likely amount-position drift; same family.
    /// </para>
    /// <para>
    /// Affected columns (7 ToJson blocks in EventConfiguration.cs):
    /// <list type="bullet">
    ///   <item><c>ticket_price</c> — root-level Money</item>
    ///   <item><c>pricing</c> — Pricing VO wrapping AdultPrice + ChildPrice + GroupTiers[].PricePerPerson</item>
    ///   <item><c>revenue_breakdown</c> — 6 Money fields</item>
    ///   <item><c>donation_config</c> — SuggestedAmounts List&lt;decimal&gt; (no Currency but included for defensive scan)</item>
    ///   <item><c>collection_config</c> — same shape</item>
    ///   <item><c>sponsor_config</c> — SponsorshipPackage Money fields</item>
    ///   <item><c>add_on_config</c> — AddOnDefinition Money fields</item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Migration strategy (PL/pgSQL recursive descent):</b> walk each JSONB tree, find
    /// any key named <c>currency</c> whose value is a JSON object with a <c>code</c> child,
    /// and replace with the string value of <c>code</c>. Also handles legacy PascalCase
    /// (<c>Currency</c> + <c>Code</c>) which some pre-Consult-#23 writes may have used.
    /// Idempotent: re-run is a no-op (already-string values are unchanged).
    /// </para>
    /// <para>
    /// <b>Rollback:</b> Down() is intentionally empty. Reverting to legacy nested-object
    /// form would BREAK the current reader path (post-Consult-#23 write format is the
    /// canonical shape). If prod cutover surfaces an issue, use PITR (7-day retention per
    /// docs/operations/migration-rollback.md).
    /// </para>
    /// <para>
    /// <b>Idempotent SQL for commit body</b> (per CLAUDE.md §5 rule 1): the SQL below
    /// mutates rows only when the source shape is an object; it is safe to re-run.
    /// </para>
    /// </remarks>
    public partial class Wave8_5_j_NormalizeTicketPriceCurrencyShape : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Recursive PL/pgSQL function that normalizes any Currency-object nested value
            // to its plain string form. Handles both {"code": "USD", ...} + {"Code": "USD", ...}.
            migrationBuilder.Sql(@"
                CREATE OR REPLACE FUNCTION events._wave85j_normalize_currency(j jsonb)
                RETURNS jsonb
                LANGUAGE plpgsql
                IMMUTABLE
                AS $func$
                DECLARE
                    result jsonb;
                    k text;
                    v jsonb;
                    code_val text;
                BEGIN
                    IF j IS NULL THEN
                        RETURN NULL;
                    END IF;

                    IF jsonb_typeof(j) = 'object' THEN
                        result := '{}'::jsonb;
                        FOR k, v IN SELECT * FROM jsonb_each(j) LOOP
                            -- Detect a Currency-object at this key (case-insensitive against
                            -- 'currency'/'Currency') where value is an object with a 'code'/'Code' child.
                            IF (lower(k) = 'currency')
                                AND jsonb_typeof(v) = 'object'
                                AND (v ? 'code' OR v ? 'Code')
                            THEN
                                code_val := COALESCE(v ->> 'code', v ->> 'Code');
                                IF code_val IS NOT NULL AND length(code_val) > 0 THEN
                                    result := result || jsonb_build_object(k, code_val);
                                ELSE
                                    -- Malformed row — preserve original (don't nuke data).
                                    result := result || jsonb_build_object(k, v);
                                END IF;
                            ELSE
                                -- Recurse into nested objects / arrays.
                                result := result || jsonb_build_object(k, events._wave85j_normalize_currency(v));
                            END IF;
                        END LOOP;
                        RETURN result;
                    ELSIF jsonb_typeof(j) = 'array' THEN
                        RETURN (SELECT jsonb_agg(events._wave85j_normalize_currency(elem))
                                FROM jsonb_array_elements(j) elem);
                    ELSE
                        -- Scalar (string / number / bool / null) — unchanged.
                        RETURN j;
                    END IF;
                END;
                $func$;
            ");

            // Apply the normalization to each of the 7 ToJson columns on events.events.
            // WHERE clause narrows the update to rows where the column contains a Currency
            // object shape so we don't touch rows already in canonical form (idempotent no-op
            // on repeat runs).
            migrationBuilder.Sql(@"
                UPDATE events.events
                SET ticket_price = events._wave85j_normalize_currency(ticket_price)
                WHERE ticket_price IS NOT NULL
                  AND ticket_price::text ~ '""[Cc]urrency""\s*:\s*\{';
            ");

            migrationBuilder.Sql(@"
                UPDATE events.events
                SET pricing = events._wave85j_normalize_currency(pricing)
                WHERE pricing IS NOT NULL
                  AND pricing::text ~ '""[Cc]urrency""\s*:\s*\{';
            ");

            migrationBuilder.Sql(@"
                UPDATE events.events
                SET revenue_breakdown = events._wave85j_normalize_currency(revenue_breakdown)
                WHERE revenue_breakdown IS NOT NULL
                  AND revenue_breakdown::text ~ '""[Cc]urrency""\s*:\s*\{';
            ");

            migrationBuilder.Sql(@"
                UPDATE events.events
                SET sponsor_config = events._wave85j_normalize_currency(sponsor_config)
                WHERE sponsor_config IS NOT NULL
                  AND sponsor_config::text ~ '""[Cc]urrency""\s*:\s*\{';
            ");

            migrationBuilder.Sql(@"
                UPDATE events.events
                SET add_on_config = events._wave85j_normalize_currency(add_on_config)
                WHERE add_on_config IS NOT NULL
                  AND add_on_config::text ~ '""[Cc]urrency""\s*:\s*\{';
            ");

            migrationBuilder.Sql(@"
                UPDATE events.events
                SET donation_config = events._wave85j_normalize_currency(donation_config)
                WHERE donation_config IS NOT NULL
                  AND donation_config::text ~ '""[Cc]urrency""\s*:\s*\{';
            ");

            migrationBuilder.Sql(@"
                UPDATE events.events
                SET collection_config = events._wave85j_normalize_currency(collection_config)
                WHERE collection_config IS NOT NULL
                  AND collection_config::text ~ '""[Cc]urrency""\s*:\s*\{';
            ");

            // Function can stay in place — cheap to keep, useful for future ad-hoc normalization.
            // Explicit no-drop; if desired, drop in a follow-up cleanup migration.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Intentional no-op. Reverting to legacy nested-object Currency form would break
            // the current EF Core 8 reader path (post-Consult-#23 writes emit plain ISO 4217
            // strings; the reader is now shape-locked to that form). If a prod deploy of this
            // migration causes issues, rely on Azure Postgres PITR (7-day retention) per
            // docs/operations/migration-rollback.md instead of Down().
            //
            // The helper function created in Up() persists — dropping it here would leave
            // any downstream ad-hoc normalization needs without the utility.
        }
    }
}
