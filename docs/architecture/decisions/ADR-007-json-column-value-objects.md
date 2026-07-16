# ADR-007: JSON-Column Value Objects — Shape Locking and Drift Prevention

| | |
|---|---|
| **Status** | Accepted (2026-07-16) |
| **Date** | 2026-07-16 |
| **Decision Owner** | Niroshana (Founder / Architect) |
| **Reviewers** | system-architect persona (Consult #23 + Consult #26 Q3 + Consult #28 Q2.c) |
| **Related** | ADR-002 (5-Layer Topology), ADR-005 (Outbox-Everything), Consult #23 (2026-07-10 Currency VO shape), Consult #26 Q3 (2026-07-14 Wave 8.5.j data-migration route), Consult #28 Q2.c (2026-07-16 ADR requested), Wave 8.5.j migration `20260715230000_Wave8_5_j_*`, Wave 8.5.k migration `20260716130000_Wave8_5_k_*` |

## Decision

**Every value object persisted inside a JSONB column MUST have its serialized shape locked at write time by an explicit converter, and Phase B products SHOULD prefer scalar columns over `OwnsOne(...).ToJson()` unless the value object nests naturally (e.g., collections inside a container VO).**

Plain-English restatement: value objects living inside JSONB break in a specific, expensive way when a serialization change lands mid-flight — old rows contain one shape, new rows contain another, and the reader can only read one. This ADR names the pattern that prevents it: pin the write shape at the type level (`[JsonConverter]` + a matching EF `IValueConverter`), and default to scalar columns when in doubt.

## Context

On 2026-07-10 (Consult #23) the platform's `Currency` value object was flipped from an "emit-full-object" JSON shape (`{"code":"USD","name":"US Dollar","symbol":"$","decimalDigits":2}`) to an "emit-ISO-4217-string" shape (`"USD"`) so that API responses matched the Wave 9 smoke contract and the frontend consumer. The flip landed as a `CurrencyJsonConverter` on the type + a matching `CurrencyValueConverter` on the seven `OwnsOne(...).ToJson()` columns in `EventConfiguration.cs`.

The flip broke reads on any row written before 2026-07-10 — because EF Core 8's `MaterializeJsonEntity` reader for owned-JSON entities is shape-locked at model-build time and throws `Cannot get token type 'Object' as string` (Wave 8.5.j scenario) or `Cannot get token type 'Number' as string` (Wave 8.5.k scenario) when it hits a row of the wrong shape.

Wave 8.5.j (`20260715230000_*`) and Wave 8.5.k (`20260716130000_*`) resolved the concrete Currency-shape drift with two recursive PL/pgSQL migrations that walked the JSONB tree and rewrote every legacy Currency value to its ISO-4217 string form. The data is healed.

**What the ADR must prevent is a repeat of the same class of trap on any future JSON-column VO — for LankaEvents (~13 live ToJson columns) OR any of the six upcoming Phase B products (LankaTemples, LankaBusiness, LankaHomes, LankaMart, LankaSeyla, LankaNivasa) that will hit the exact same money / location / contact-info primitives.** Consult #28 Q2.c explicitly requested this ADR before any Phase B product first-slice implementation.

Pre-Consult-#23 vs post-Consult-#23 drift is the shape of every future incident on this axis: an old shape hardened onto disk, a converter added mid-flight, a Consult / Wave / migration to unblock reads. The cost of the Wave 8.5.j+k incident was one architect consult (#26) + two migrations + a session-day of investigation. Repeating on `Pricing.GroupTiers[]`, `RevenueBreakdown`, `AddOnDefinition`, or a Phase B `Location` / `Amenity[]` / `ServiceTime[]` VO would spend that budget again per incident. This ADR is the forcing function.

## Alternatives considered

### Option (i) — React on drift discovery (data-migration-per-incident)

**What it is:** ship the write-shape change; wait for reads to break in production or Wave 9 smoke; author a PL/pgSQL sweep migration each time. This is exactly what Wave 8.5.j + Wave 8.5.k did.

**Pros:**
- Zero upfront work — no ADR overhead, no type-annotation ceremony.
- Wave 8.5.j+k proved this route is technically feasible and idempotent.
- Existing PL/pgSQL utility functions (`_wave85j_normalize_currency`, `_wave85k_normalize_numeric_currency`) stay resident on the DB — cheap to reuse for future drift.

**Cons:**
- Reactive. Each incident consumes ~1 architect consult + 1 migration + 1 session-day of investigation.
- Time-of-drift is not time-of-fix — a shape change landed 2026-07-10 (Consult #23) surfaced 2026-07-14 (Wave 8.5.j misfire) and only fully resolved 2026-07-16 (Wave 8.5.k). A six-day tail of Wave 9 red on a "5-money-flow" cluster that founder briefing had to acknowledge.
- Un-scalable across seven products. Every future OwnsOne-ToJson-VO pattern-copy inherits the trap.
- Does nothing to prevent a Phase B team from committing the same shape-drift primitive tomorrow.

### Option (ii) — Refactor VO-in-JSON to scalar columns (proactive, one-time cost per VO)

**What it is:** for every OwnsOne(VO)+ToJson mapping, refactor the VO into scalar columns on the containing entity (e.g., `ticket_price` → `ticket_price_amount decimal + ticket_price_currency varchar(3)`). This is exactly the shape `Registration.TotalPrice` already uses (`total_price_amount` + `total_price_currency` columns via multi-column `OwnsOne` without `.ToJson()`).

**Pros:**
- Eliminates the shape-drift class entirely. Scalar columns cannot drift — their type is enforced by Postgres.
- Postgres tooling reads scalar columns natively (SQL joins, WHERE filters, index selectivity).
- Aligns with the "flat primitives" pattern C5 Guard already enforces on `SponsorConfiguration` / `AddOnConfiguration` / `DonationConfiguration` / `CollectionConfiguration`.

**Cons:**
- One-time schema migration + application-layer refactor per VO. Wave 5.1a already did this for `EventPass.Price` + `PassPurchase.TotalPrice` (see migration `20260627204031_Wave5_1a_alpha_2_ScalarPriceMoneyOnEventPassPassPurchase`). Non-trivial: `ticket_price` is 6630 rows on staging, `pricing` is nested three levels deep.
- Nested-inside-container VOs (e.g., `Pricing.GroupTiers[]` — a list) cannot become scalar columns without introducing a child table. Introducing a child table changes the aggregate persistence model and forces a repository / query refactor.
- Retro-fitting the existing 12 live ToJson columns is ~1-2 sprint-days of work.

### Option (iii) — Custom shape-tolerant `JsonConvertedValueReaderWriter` (high-risk EF-internals)

**What it is:** subclass EF Core 8's `JsonConvertedValueReaderWriter<TValue, TProvider>` (or the parent `JsonValueReaderWriter<T>`) with a reader that accepts multiple legacy shapes — the pattern `CurrencyJsonConverter.Read` uses today for API-layer JSON, extended into the EF Core hydration path.

**Pros:**
- No data migration required — the reader tolerates any legacy shape it knows about.
- Handles the case where legacy data cannot be safely rewritten (e.g., a VO whose legacy shape carried information the new shape drops).

**Cons:**
- Depends on EF Core 8's `JsonValueReaderWriter<T>` API surface, which is documented-but-internal and may change in EF Core 9+. Every EF upgrade becomes a re-verification risk.
- Wave 8.5.j originally attempted a form of this — the CurrencyJsonConverter API-layer reader already tolerates the legacy object shape — and it did not help EF Core 8's `MaterializeJsonEntity` internal reader path (different code path).
- Encoded legacy shapes are a permanent debt — the reader must keep supporting them forever, or someone must remember to remove the legacy branch and re-migrate the data later.
- No "compile-time" guarantee that a fresh Phase B VO follows the pattern; discoverability is poor.

## Final reasoning

Option (i) alone loses. It works, but it re-runs the incident-response cost every time a shape changes, and Phase B multiplies the number of surfaces by ~7x. Option (iii) alone loses. It couples us to an internal EF Core 8 API and encodes legacy shapes as permanent debt.

The ruling is a **hybrid with strong preference for Option (ii)**:

1. **For NEW value objects the platform introduces (Phase B products, new Capability primitives):** DEFAULT to scalar columns unless the VO nests naturally as a collection or wraps other VOs (e.g., `Pricing.GroupTiers[]` — a `List<GroupTier>` cannot become a scalar). This is the shape Wave 5.1a already committed to for `EventPass.Price` — proven pattern.

2. **For value objects that MUST live inside JSONB** (natural nesting, aggregate boundary, migration cost too high for legacy VOs): the type MUST carry an explicit `[JsonConverter(typeof(XxxJsonConverter))]` attribute at write time AND EF configurations MUST attach a matching `IValueConverter<T, string>` (the `CurrencyValueConverter` pattern from `BuildingBlocks.Infrastructure.ValueConverters`) on every `OwnsOne<T>` binding that touches the VO. Both are load-bearing: `[JsonConverter]` locks the API-layer shape; `IValueConverter` locks the EF-writer shape (they run through different serializers).

3. **For existing legacy JSONB columns hitting shape drift** (Wave 8.5.j-shape incidents): Option (i)'s PL/pgSQL data-migration remains the tactical route. This is not new debt — it is the cost of unlocking a pattern that was committed before this ADR.

**Version-marker recommendation (optional):** for VOs where a nested-collection shape may evolve (`Pricing.GroupTiers[]`), the containing JSONB envelope MAY carry a `_schemaVersion` integer at its root. Readers can inspect the marker before consuming — allowing a future writer to bump the marker without breaking the reader path. This is not mandatory for Phase A; it is a Phase B-onward tool.

Trade-offs accepted:
- The seven existing LankaEvents ToJson columns stay in JSONB as-is. Wave 8.5.j+k already healed the Currency drift; the four non-Currency configs (`sponsor_config`, `add_on_config`, `donation_config`, `collection_config`) are proven-clean by the 2026-07-16 staging probe (0 drift rows on 335 non-null rows each). Refactoring them to scalar today is un-necessary churn.
- Phase B teams accept a slight rigidity in exchange for a strong compile-time signal: "if you see `OwnsOne<T>.ToJson()`, T MUST have `[JsonConverter]` + a `CurrencyValueConverter`-shape `IValueConverter`. If it doesn't, don't ship."
- Post-ship enforcement is architect review + Rule 5j config-relocation audit; a Roslyn analyzer that detects the pattern is nice-to-have, not day-one gating.

## Impact on existing code

**Audit performed 2026-07-16 (Wave 8.5.j agent-JsonVoADR)** — grep of `.ToJson(` across `src/` excluding migrations/designers/snapshot/comments produced 13 live column mappings across three EF configurations:

| DbContext | Config | Column | VO type | Ruling |
|---|---|---|---|---|
| LankaEventsDbContext | EventConfiguration | `ticket_price` | `Money` | KEEP-IN-JSON. Has `CurrencyValueConverter`. Wave 8.5.j+k healed. |
| LankaEventsDbContext | EventConfiguration | `pricing` | `Pricing`→`Money`×2 + `GroupTier[].Money` | KEEP-IN-JSON. Nested collection; scalar refactor requires child table. Has converter. Healed. |
| LankaEventsDbContext | EventConfiguration | `revenue_breakdown` | `RevenueBreakdown`→`Money`×6 | KEEP-IN-JSON. 6-field aggregate; scalar refactor = 12 columns. Has converter. Healed. |
| LankaEventsDbContext | EventConfiguration | `donation_config` | `DonationConfiguration` (flat primitives + `List<decimal>`) | KEEP-IN-JSON. No VO nesting. 0 drift on staging. |
| LankaEventsDbContext | EventConfiguration | `collection_config` | `CollectionConfiguration` (flat primitives + `List<decimal>`) | KEEP-IN-JSON. No VO nesting. 0 drift on staging. |
| LankaEventsDbContext | EventConfiguration | `sponsor_config` | `SponsorConfiguration` (flat primitives) | KEEP-IN-JSON. No VO nesting. 0 drift on staging. |
| LankaEventsDbContext | EventConfiguration | `add_on_config` | `AddOnConfiguration` (flat primitives) | KEEP-IN-JSON. No VO nesting. 0 drift on staging. |
| LankaEventsDbContext | RegistrationConfiguration | `attendee_info` | `AttendeeInfo`→`Email`+`PhoneNumber` VOs | KEEP-IN-JSON, FLAG LATENT. 0 non-null rows on staging (legacy Mode-A/anonymous path). If Mode-A is re-enabled without adding `EmailValueConverter` + `PhoneNumberValueConverter`, this WILL drift — file a follow-up ADR before enabling the path. |
| LankaEventsDbContext | RegistrationConfiguration | `attendees` | `Attendee[]` (flat primitives + string-enums) | KEEP-IN-JSON. No VO nesting. Clean shape confirmed. |
| LankaEventsDbContext | RegistrationConfiguration | `pending_seat_assignments` | `PendingSeatAssignment[]` (flat primitives) | KEEP-IN-JSON. No VO nesting. Clean shape confirmed. |
| LankaEventsDbContext | RegistrationConfiguration | `contact` | `Contact` (flat primitives — email/phone stored as `string`, not VO) | KEEP-IN-JSON. Not a VO-in-JSON case; flat primitives already. |
| LankaEventsDbContext | RegistrationAdditionConfiguration | `new_attendees` | `Attendee[]` (flat primitives + string-enums) | KEEP-IN-JSON. No VO nesting. Clean shape confirmed. |

**Total live ToJson columns: 12** (task brief said 7 for Event alone; the fuller count adds 5 across Registration + RegistrationAddition).

**Staging probe result (2026-07-16, `scratchpad/probe_json_columns.py`):** Zero shape-drift rows found on any non-Currency column. Zero legacy Email/PhoneNumber nested-VO objects. Zero unexpected numeric values in string-position fields. Audit found no additional drift as of 2026-07-16; no defensive migration authored this pass.

**Phase B application checklist** — every product that introduces a new VO-in-JSON MUST:
1. Add `[JsonConverter(typeof(XxxJsonConverter))]` to the VO type declaration.
2. Add matching `IValueConverter<T, string>` in `BuildingBlocks.Infrastructure.ValueConverters/`.
3. Attach the value converter to every `OwnsOne<T>` binding that persists the VO to JSONB — pattern-copy `EventConfiguration.cs` lines 189-222.
4. Add a unit test that round-trips the VO through both `JsonSerializer.Serialize` AND EF Core's write path, asserting shape equality.
5. Prefer scalar columns per §Decision unless the VO nests naturally.

## Consequences

### Positive

- Phase B products have a written pattern to follow. "Where do I put my Money-shaped VO?" is answered without a live consult.
- Existing seven Event ToJson columns are formally ratified as KEEP-IN-JSON. No churn for churn's sake.
- The Wave 8.5.j+k data-migration route is preserved as the fallback for un-anticipated legacy drift.
- The `attendee_info` latent-drift trap is documented — the next agent re-enabling Mode-A will read this ADR and know to author the converter first.

### Negative / Trade-offs

- Phase B teams accept a small ceremony tax (`[JsonConverter]` + `IValueConverter` + round-trip test) for every new VO-in-JSON. Roughly ~30 minutes of authoring + review per VO.
- No compile-time enforcement in Phase A. A Phase B team could still commit an OwnsOne+ToJson without the converter pair; only architect review + code review will catch it. See §Follow-up work — Roslyn analyzer.
- The version-marker recommendation is optional and non-uniform; if Phase B teams adopt it inconsistently, envelope-inspection code will need per-column knowledge.

### Risks

- **R1 — Roslyn analyzer never lands.** Enforcement stays manual → future drift is inevitable. Mitigation: track under the "Rule 5j.4" test-debt umbrella; author when three violations accumulate (per `[[feedback-roslyn-analyzer-recurrence-trigger]]`).
- **R2 — Phase B PR reviewer misses the pattern on first-slice merges.** Mitigation: Founder briefing pack (D3 readiness) must include this ADR by number as a Phase B kickoff pre-read; Phase B product-owner agents get the ADR in their first prompt.
- **R3 — Someone re-enables Mode-A anonymous registration without adding Email/Phone converters.** Mitigation: `attendee_info` column-comment in a follow-up commit (out of scope this ADR); latent-drift entry in this ADR's §Impact table.
- **R4 — A Phase B product introduces a NEW OwnsOne+ToJson shape that this ADR did not anticipate.** Mitigation: quarterly audit of `.ToJson(` grep across `src/` — cheap; landed as Wave 9-follow-up ArchTest rule in the next audit sweep.

## Follow-up work

1. **Founder-briefing inclusion (D3 readiness pack):** name this ADR as a Phase B kickoff mandatory read. Agent-FounderBriefing sees this.
2. **Roslyn analyzer (deferred until 3+ violations accumulate):** detect `OwnsOne<T>.ToJson()` where `T` lacks either `[JsonConverter]` OR a value-converter binding on the OwnsOne. Emit compile-time warning. Priority: LOW today, MEDIUM once Phase B ships a first violation.
3. **`attendee_info` latent drift documentation:** add a column-comment migration in a future Wave 8.5 slot explaining the trap; author converters IF Mode-A re-enables.
4. **Quarterly `.ToJson(` audit:** cheap 15-minute grep + probe; catches new drift before it accumulates.

## Status Update Log

- **2026-07-16 (author):** Ratified by Tech Lead (Claude) per Consult #28 Q2.c mandate. Staging probe confirmed zero additional drift beyond Wave 8.5.j+k healed columns; no defensive migration authored this pass. Ready for founder review + Phase B kickoff read.
