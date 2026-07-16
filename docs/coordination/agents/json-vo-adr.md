# Agent Channel: JsonVoADR

**Agent role:** Author ADR for JSON-column value-object handling + audit every OwnsOne+ToJson mapping for shape-drift risk + ship defensive migrations if any other VO exhibits Wave 8.5.j-style drift.
**Priority:** P2 (unblocks Phase B products using money-column patterns; per architect Consult #28 Q2.c + Q4.a)
**Est time:** 3 hours
**Reports to:** Tech Lead (Claude)

---

## Task brief

Session context: This session already resolved Wave 8.5.j Currency shape-drift with commit `ff02b13b` (numeric-Currency → "USD" migration). But Wave 8.5.j the sprint bible slot is BROADER — it demands an ADR for how the platform handles JSON-column value objects going forward, so Phase B teams don't inherit the same trap.

Architect Consult #28 Q2.c: *"Analogous shape-drift risk on other JSON-column VOs is real ... `Pricing.GroupTiers[]`, `RevenueBreakdown`, `AddOnDefinition`, and any other OwnsOne+ToJson mapping have NOT been audited for the same 'wrote nested, reads scalar' shape."*

## Deliverable

### Part A — ADR authoring (~1 hour)

Author `docs/architecture/decisions/ADR-007-json-column-value-objects.md` (next ADR number — verify by `ls docs/architecture/decisions/`).

Content must include:
- **Context** — Consult #23 CurrencyValueConverter added 2026-07-10; pre-#23 vs post-#23 shape drift caused Wave 8.5.j fire drill.
- **Decision options** (per Consult #26 Q3):
  - Option (i) — Data migration on drift discovery (what we did — reactive)
  - Option (ii) — Refactor to scalar columns (proactive, one-time cost per VO)
  - Option (iii) — Custom shape-tolerant JsonConvertedValueReaderWriter (high-risk EF-internals)
- **Ruling** — for NEW VO-in-JSON going forward:
  - PREFER scalar columns unless the VO nests naturally (e.g., `List<GroupTier>` inside `Pricing`)
  - When a VO stays in JSON: MUST define a `IValueConverter` explicitly to lock the shape at write time; MUST NOT rely on System.Text.Json defaults
  - Version-marker recommended: add `_schemaVersion` int to JSON envelope so future readers can detect legacy vs canonical shape
- **Impact on existing code** — enumerate the 7 `events.*` ToJson columns (ticket_price, pricing, revenue_breakdown, sponsor_config, add_on_config, donation_config, collection_config) — each gets ruled: keep-in-json vs schedule-scalar-refactor. My prior: leave as-is (Wave 8.5.j-followup already normalized Currency; other fields are less risky) UNLESS Part B reveals another shape drift.

### Part B — Audit all OwnsOne+ToJson (~1 hour)

1. Grep every EF configuration file: `grep -rn "OwnsOne\|OwnsMany" src/ --include="*.cs" | grep -i "ToJson"`
2. For each hit, capture:
   - DbContext + entity + property + JSON column name
   - The VO type mapped
   - Whether the VO has a converter attached (per Consult #23 CurrencyValueConverter pattern)
   - Whether the JSON column has any staging-DB rows in a legacy shape

3. For staging-DB shape audit: use the psycopg2 + Key Vault probe pattern from `ff02b13b`. Connection string:
   ```
   CS=$(az keyvault secret show --vault-name lankaconnect-staging-kv --name DATABASE-CONNECTION-STRING --query value -o tsv)
   ```
   Then Python:
   ```python
   import re, psycopg2, json
   kv = {k.strip().lower():v.strip() for k,v in re.findall(r'([^;=]+)=([^;]+)', os.environ['CS'])}
   conn = psycopg2.connect(host=kv['host'], port=kv.get('port','5432'), dbname=kv['database'],
                           user=kv['username'], password=kv['password'], sslmode='require')
   ```
   Probe each JSON column for shape distribution (grep pattern per Wave 8.5.k migration `20260716130000_Wave8_5_k_NormalizeNumericCurrencyToIsoString.cs`).

### Part C — Ship defensive migration if drift found (~1 hour)

If Part B finds a JSON column with legacy shape rows AND that column matches a Wave 9 test failure pattern (currently: 5 money-flow tests still failing per session context), author a similar migration to `20260716130000_Wave8_5_k_*.cs`.

If no additional drift found: skip this part, note in ADR "audit found no additional drift as of 2026-07-16."

### Commit + push

- One or two commits (ADR + migration if any).
- Bodies reference Wave 8.5.j ADR closure.
- `T-triggers: T8 (migration add) if Part C fires`
- `S-class: S3 (log silence + endpoint verify) if Part C fires`
- Push to `develop`.

## Constraints

- ADR must be founder-readable (no impenetrable EF-Core jargon without a plain-English sentence following).
- Migration authoring pattern: MUST match `20260716130000_Wave8_5_k_*` — hand-authored .cs + .Designer.cs pair, Designer.cs copied verbatim from head migration.
- Do NOT modify any Consult document.

## Communication protocol

- Post ADR draft outline first (headers only).
- Post OwnsOne+ToJson audit findings (list of hits).
- Post staging probe results per JSON column.
- Post migration authored (if any) + deploy commit SHA.
- Post `STATUS: COMPLETE` at bottom.

## Log

*(Agent writes progress below this line.)*

### 2026-07-16 — Part B audit begins

**Live ToJson columns inventory** (`grep -rn ".ToJson(" src/ --include="*.cs"` excluding Migrations/Designer/Snapshot/comments):

**Event entity — `events.events` table (7 columns, all Wave 8.5.j-normalized):**
| Column | VO type | Nested VOs | CurrencyValueConverter? | Wave 8.5.j/k normalized? |
|---|---|---|---|---|
| `ticket_price` | `Money` | `Currency` | YES (line 193) | YES (both j + k) |
| `pricing` | `Pricing` | `Money x2 + GroupTier[].Money` | YES (lines 204-208) | YES (both j + k) |
| `revenue_breakdown` | `RevenueBreakdown` | `Money x6` | YES (lines 216-221) | YES (both j + k) |
| `donation_config` | `DonationConfiguration` | `List<decimal>` (no Money) | N/A | Defensive scan (k) |
| `collection_config` | `CollectionConfiguration` | `List<decimal>` (no Money) | N/A | Defensive scan (k) |
| `sponsor_config` | `SponsorConfiguration` | C5-guard flat primitives (no Money) | N/A | Defensive scan (k) |
| `add_on_config` | `AddOnConfiguration` | C5-guard flat primitives (no Money) | N/A | Defensive scan (k) |

**Registration entity — `events.registrations` table (4 columns, no Currency):**
| Column | VO type | Nested VOs | Shape-drift risk |
|---|---|---|---|
| `attendee_info` | `AttendeeInfo` | `Email` + `PhoneNumber` VOs | POSSIBLE — VOs default to STJ, no converters |
| `attendees` | `Attendee[]` | primitives + enums-as-string | LOW |
| `pending_seat_assignments` | `PendingSeatAssignment[]` | primitives | LOW |
| `contact` | `Contact` | primitives | LOW |

**RegistrationAddition entity — `events.registration_additions` table (1 column, no Currency):**
| Column | VO type | Nested VOs | Shape-drift risk |
|---|---|---|---|
| `new_attendees` | `Attendee[]` | primitives + enums-as-string | LOW |

**Total live ToJson columns: 12.** 7 on Event, 4 on Registration, 1 on RegistrationAddition. Only 3 (`ticket_price` + `pricing` + `revenue_breakdown`) carry Currency. Wave 8.5.j + 8.5.k already probed + normalized these + defensively swept the other 4 Event columns.

**Wave 8.5.j (2026-07-15) probe evidence** (from commit `31e2ac41` body):
- `sponsor_config`, `add_on_config`, `donation_config`, `collection_config` — 0 rows with numeric OR object Currency shape. No fix needed.

**Wave 8.5.k (2026-07-16 12:30 UTC) probe evidence** (from commit `ff02b13b` body):
- `ticket_price` — 504+28 numeric Currency + 183 string. Normalized.
- `pricing` — 505 numeric + 169 string. Normalized.
- `revenue_breakdown` — 481 numeric + 169 string. Normalized.

**Shape drift risk unchecked on Registration + RegistrationAddition columns.** These carry Email/PhoneNumber VOs (`AttendeeInfo`) which have the same shape-drift shape as Currency did — a VO that STJ defaults would serialize as an object but could be flattened to a string via converter. Need to probe.

### 2026-07-16 — Part B staging DB probe results

Probe script: `scratchpad/probe_json_columns.py` — psycopg2 + Key Vault DATABASE-CONNECTION-STRING pattern from `ff02b13b`.

**Row counts + shape distribution:**
| Column | total | non-null | null | Top-level shape | Currency drift | Email/Phone nested |
|---|---:|---:|---:|---|---:|---:|
| `events.registrations.attendee_info` | 163 | **0** | 163 | (empty) | 0 | 0 |
| `events.registrations.attendees` | 163 | 163 | 0 | array | 0 | 0 |
| `events.registrations.pending_seat_assignments` | 163 | 75 | 88 | array | 0 | 0 |
| `events.registrations.contact` | 163 | 111 | 52 | object | 0 | 0 |
| `events.registration_additions.new_attendees` | 11 | 11 | 0 | array | 0 | 0 |
| `events.events.sponsor_config` | 6630 | 335 | 6295 | object | 0 | 0 |
| `events.events.add_on_config` | 6630 | 335 | 6295 | object | 0 | 0 |
| `events.events.donation_config` | 6630 | 331 | 6299 | object | 0 | 0 |
| `events.events.collection_config` | 6630 | 335 | 6295 | object | 0 | 0 |

**Findings:**
1. **`attendee_info` — 0 non-null rows.** Legacy Mode-A / anonymous registration column; production traffic has moved entirely to the `attendees` array + `contact` shape. NO drift risk because NO data. Latent trap if Mode-A path is ever re-enabled — see ADR §Consequences.
2. **`attendees` + `new_attendees`** — samples show clean `[{Name, Gender, AgeCategory}]` shape. No Currency keys, no nested VO objects.
3. **`contact`** — samples show clean `{Email: string, Address: string, PhoneNumber: string}` shape (plain strings, not nested Email/PhoneNumber VOs — the OwnsOne config maps to primitive string columns via `.Property(c => c.Email)`). No drift.
4. **`sponsor_config` / `add_on_config` / `donation_config` / `collection_config`** — re-confirmed Wave 8.5.j+k defensive-scan finding: 0 Currency-key drift rows.

**Verdict for Part C:** NO additional shape drift found on staging as of 2026-07-16. Defensive migration NOT authored. ADR §Impact records "audit found no additional drift; no defensive migration required this pass."

### 2026-07-16 — Part A ADR authored + commit

- **ADR file:** `docs/architecture/decisions/ADR-007-json-column-value-objects.md` (5 sections + audit tables + follow-up register).
- **Commit:** `bffbb357` on `develop` — "Wave 8.5.j — ADR-007 JSON-column value objects (Consult #28 Q2.c)"
- **Push:** pushed to origin/develop (verified `git rev-parse origin/develop == bffbb357`… now `496b6ec9` after Agent-SkipAudit push landed).
- **Part C:** skipped per task-brief rule (no drift found).

**Deliverables summary:**
1. Part A (ADR authoring) — DONE. `docs/architecture/decisions/ADR-007-json-column-value-objects.md` authored with founder-readable prose, 3-option decision matrix, and per-column ruling for all 12 live ToJson mappings.
2. Part B (OwnsOne+ToJson audit) — DONE. 12 columns catalogued. Staging probe on 9 non-Currency columns confirmed 0 drift rows. Latent trap flagged: `attendee_info` (0 rows today; will drift if Mode-A anonymous registration is re-enabled without Email/Phone converters).
3. Part C (defensive migration) — SKIPPED per brief; no additional drift found.

STATUS: COMPLETE



