# ADR-005: Money DTO Migration Strategy (Frontend ↔ Backend)

| | |
|---|---|
| **Status** | Accepted (2026-04-26) |
| **Date** | 2026-04-26 |
| **Decision Owner** | Niroshana (Founder/Architect) |
| **Reviewers** | system-architect agent |
| **Supersedes** | — |
| **Related** | ADR-001 (i18n scope), ADR-003 (Stripe currency), ADR-004 (feature flags) |

## Context

Phase A introduces a `Money` value object replacing every `decimal` price/amount across the codebase. Backend entities, DTOs, EF Core mappings, and Stripe abstractions all change shape from `decimal price` → `Money { decimal amount, string currencyCode }`.

This is a **breaking API change**. Existing frontend components consume `decimal price` directly. A naive deploy would break every page that renders a price.

Solo-founder constraints rule out atomic deploys (frontend and backend deployed in same instant). The migration must support an interim period where backend serves both shapes and frontend migrates field-by-field.

## Options Considered

### Option A: Dual-field DTOs with expand/contract pattern (RECOMMENDED)

API responses return BOTH the legacy `price: decimal` AND the new `priceMoney: { amount, currencyCode }` fields simultaneously during the migration window. Frontend components migrate from `price` to `priceMoney` field-by-field. After all consumers migrated (verified by grep + lint rule), backend PR drops the legacy field.

```json
// Transition response shape (Weeks 9–14):
{
  "id": "evt-123",
  "name": "Wedding",
  "ticketPrice": 50.00,                    // legacy — deprecated
  "ticketPriceMoney": { "amount": 50.00, "currencyCode": "USD" }   // new
}

// Final response shape (Week 15+):
{
  "id": "evt-123",
  "name": "Wedding",
  "ticketPriceMoney": { "amount": 50.00, "currencyCode": "USD" }
}
```

### Option B: Translation layer in API client

Backend ships only the new `Money` shape. Frontend `api-client-*` packages internally transform `Money` back to a `decimal` for unmigrated components.

### Option C: Big-bang atomic deploy

Backend + frontend deploy together with the new shape. No transition period.

### Option D: API versioning (`/v1` returns old, `/v2` returns new)

URL or header-based API versioning. Clients opt into the new version explicitly.

## Decision

**Adopt Option A (Dual-field DTOs with expand/contract pattern).**

### Naming Convention

For every existing `decimal` money field, the new dual fields use this rule:

- Existing field: `price` (kept temporarily, deprecated)
- New field: `priceMoney` (the existing name + `Money` suffix)

This keeps the legacy field's name unchanged (no mass frontend rename until cleanup), and the new field is unambiguously typed.

### Migration Process

| Phase | Backend | Frontend |
|---|---|---|
| **Week 9** (Money refactor lands) | EF schema migrated (Money value converter); API DTOs return BOTH legacy `price` and new `priceMoney` fields | No change yet |
| **Week 12** (Frontend migration) | No change | Components migrate one-by-one to consume `priceMoney`. Each component = 1 PR with screenshot evidence. ESLint rule `no-decimal-money` warns on legacy `price` reads in NEW code |
| **Week 14** (Cleanup gate) | Grep verification: no frontend file references legacy `price` field. ArchTest verifies. | n/a |
| **Week 14+** (Cleanup PR) | Backend PR drops legacy `price` field. OpenAPI regenerates. | api-client-* packages regenerate without legacy field |
| **Week 15** (Verification) | Production deployed; ESLint rule upgraded from warn → error | n/a |

### ESLint Rules (Frontend)

- `no-decimal-money`: warn on use of legacy `price` field after W5 (Money refactor lands per revised sequence); upgrade to error after W14 (cleanup PR)
- `prefer-money-formatter`: warn when raw `priceMoney.amount` is rendered without going through `formatMoney(money, locale)`

### Backend Discipline

- Every DTO containing money MUST have BOTH fields during the window
- Every controller action returning money MUST be covered by a contract test asserting both fields present
- **Contract test asserts EXACT JSON property names** (per MEMORY.md Phase 6A.124 — System.Text.Json + interface property pitfalls):
  ```csharp
  // Required pattern in Money DTO contract tests:
  var json = await response.Content.ReadAsStringAsync();
  var doc = JsonDocument.Parse(json);
  Assert.True(doc.RootElement.TryGetProperty("price", out _),       // legacy field present
              "Legacy 'price' field missing from response");
  Assert.True(doc.RootElement.TryGetProperty("priceMoney", out var moneyEl),
              "New 'priceMoney' field missing from response");
  Assert.True(moneyEl.TryGetProperty("amount", out _));
  Assert.True(moneyEl.TryGetProperty("currencyCode", out _));
  ```
- PR template includes "Money DTO migration" checklist for any DTO change

### External Caller Risk Posture (architect review)

Original ADR claimed verification of zero external callers via API gateway logs. **This is unverifiable for LankaConnect** (Container Apps direct ingress; no API gateway with field-level request logs).

**Honest position**:
- LankaConnect has no documented external API consumers (no public mobile app, no documented partner integrations)
- 2-week deprecation period documented in `CHANGELOG.md` and `docs/api-deprecations.md` before legacy field removal
- If unknown consumers exist, they break at cleanup PR; recovery is reverting the cleanup PR (1-day fix)
- This is acceptable risk for an internal-only API in Phase A

## Consequences

### Positive

- No big-bang deploy required (impossible for solo founder)
- Frontend migration is incremental (one component, one PR)
- Each migrated component independently testable + reviewable
- Easy rollback per component (revert PR; legacy field still served)
- Industry-standard expand/contract pattern (Martin Fowler)
- ESLint enforcement prevents accidental legacy reads in new code

### Negative / Trade-offs

- API responses ~10–15% larger during transition window (~3 weeks)
- Discipline required to land cleanup PR — flag in `Refactor.*` registry per ADR-004
- Risk that someone consumes legacy field after Week 14 — mitigated by ESLint error + ArchTest
- Frontend OpenAPI client must regenerate twice (Week 9 add new field; Week 14 drop legacy)

### Risks

- **Risk: cleanup PR delayed indefinitely; both fields live forever.** Mitigation: `Refactor.MoneyDto.LegacyField` flag in registry with sunset Week 15; CI fails if alive past sunset.
- **Risk: 3rd-party API consumer (if any) breaks at cleanup.** Mitigation: 2-week deprecation notice; only Phase A consumer is internal frontend; verify zero external callers via API gateway logs.
- **Risk: Money formatter inconsistencies (e.g., `Rs 1,500` vs `LKR 1,500`).** Mitigation: single `formatMoney()` function in `@lankaconnect/formatters`; ESLint rule prevents direct `.toFixed()` on amounts.

## Rejected Alternatives

- **Option B (translation layer)**: Hides truth from frontend devs. Debugging "why does this look like a decimal but actually has currency lost?" is painful. Adds api-client complexity.
- **Option C (big bang)**: Solo founder cannot reliably deploy frontend + backend atomically. Single point of failure.
- **Option D (API versioning)**: Heavyweight ceremony for transient internal change. URL versioning fragments cache headers, complicates routing. Header versioning loses observability. Both are overkill for an internal-only API.

## Implementation Checklist

### Backend (Week 9)

- [ ] EF Core Money value converter
- [ ] Per-module migration: rename existing `_price`/`_amount` columns; backfill rows as USD
- [ ] DTO update: add `*Money` field alongside legacy field
- [ ] Contract tests assert both fields present in every monetary response
- [ ] `Refactor.MoneyDto.LegacyField` flag added with sunset Week 15

### Frontend (Week 11) — REVISED per architect (was W12)

- [ ] **W11.0 (NEW): Component migration audit** — grep ALL `.price` JSX usages in `web/`; produce explicit migration list with file paths, count, and PR cohort plan. Don't assume ≤5-PR cohort size; cohort by component family (cards, lists, modals, forms).
- [ ] Generate api-client-events from updated OpenAPI (both fields visible)
- [ ] `formatMoney()` utility in `@lankaconnect/formatters` package
- [ ] ESLint rules `no-decimal-money` (warn), `prefer-money-formatter` (warn)
- [ ] Per-cohort migration PRs (size determined by audit, not arbitrary 5)

### Cleanup (Week 14)

- [ ] Grep verification: zero frontend files reference legacy field
- [ ] Backend PR drops legacy field
- [ ] OpenAPI regenerates
- [ ] api-client packages regenerate
- [ ] Flag removed from registry
- [ ] ESLint rules upgraded to `error`

## References

- Architect review: 2026-04-26 (Question 5 — DTO migration strategy)
- Martin Fowler, "Parallel Change" (expand/contract pattern)
- ADR-004 for flag-driven cutover process
