# ADR-001: i18n Scope for Phase A

| | |
|---|---|
| **Status** | Accepted (2026-04-26) |
| **Date** | 2026-04-26 |
| **Decision Owner** | Niroshana (Founder/Architect) |
| **Reviewers** | system-architect agent |
| **Supersedes** | — |
| **Related** | ADR-003 (Stripe multi-currency), ADR-005 (Money DTO migration) |

## Context

LankaConnect is a USA-launched platform that must expand to Sri Lanka, India, UK, and other markets. Multi-currency and internationalization (i18n) are foundational concerns: every monetary field, every translatable string, and every locale-aware output (date/time/number formatting, email/WhatsApp templates) must be designed with this in mind.

The architectural risk is asymmetric:

- **Foundational primitives** (e.g., `Money` value object replacing `decimal`, `Locale`/`Country` on User, currency-aware Stripe abstraction) must be in place from Phase A. Retrofitting them later means touching every entity, DTO, repository, handler, and frontend component that handles money or text — an estimated 8–12 weeks of pure refactor work after the fact.
- **Content layer** (translated UI strings, multi-language email templates, `LocalizedString` JSONB conversion of existing event names/descriptions, translation management workflow) can be added incrementally per country launch.

The architecture review explicitly flagged that bundling the full i18n stack into Phase A inflates the calendar by ~4–5 weeks (not the 2 weeks originally estimated) and introduces risk concurrent with the modular refactor itself.

## Options Considered

### Option A: Foundation only (RECOMMENDED)

Phase A delivers:

- `Money` value object replacing every `decimal` price/amount field across the codebase
- `Currency` value object with ISO 4217 registry (USD, LKR, INR, GBP, EUR, AUD, CAD)
- `Locale` and `Country` value objects on `User`
- `next-intl` wired with `[locale]` route segment and middleware (English-only baseline)
- Email/WhatsApp template engine extended to support per-locale lookup with `en-US` fallback (no actual non-English templates added)
- Stripe abstraction takes a `Currency` parameter; ready for multi-currency settlement

Phase A.5 (later, as needed before 2nd country launch) delivers:

- `LocalizedString` JSONB conversion of existing translatable fields (Event name, description, etc.)
- Actual translated strings for new locales
- Per-locale email/WhatsApp template content

### Option B: Foundation + Content in Phase A

All of A plus full `LocalizedString` migration of existing data, translated baseline content for `si-LK`/`ta-LK`, RTL support scaffolding.

Adds 4–5 weeks. Translations rot before launch (no real users to validate them). RTL is unused without Arabic/Hebrew launches.

### Option C: Defer i18n entirely

USA-only Phase A. Add multi-currency + i18n at country-2 launch.

`Money` retrofit is the dealbreaker: touches every transaction-related entity, every API DTO, every payment integration. Estimated 8–12 weeks of pure rework, plus risk of silent bugs in money handling.

## Decision

**Adopt Option A (Foundation only).** Phase A includes Money + Locale-on-User + next-intl scaffolding + per-locale template lookup capability. Phase A.5 delivers content migration and translations when scheduled (≈2 weeks before 2nd country launch).

## Consequences

### Positive

- Phase A duration: ~19 weeks (vs ~24 if full i18n bundled)
- Money value object discipline starts day one of post-Phase-A work; no future retrofit
- `Locale` and `Country` on User are populated from registration day one (defaulting to `en-US` and `US`)
- Adding country N becomes a content + config exercise (~2–3 weeks per country, not 3–4 months)
- Stripe multi-currency capability ready when Commerce ships
- Email/WhatsApp template engine doesn't need re-architecture for per-locale variants

### Negative / Trade-offs

- `LocalizedString` not applied to Event name/description in Phase A — those stay as plain `string` columns until Phase A.5
- No translated baseline content for non-English locales until country-2 launch is scheduled
- A separate Phase A.5 mini-project required before 2nd country goes live (~10–12 days work)

### Risks

- Risk: contributors forget Money discipline and add new `decimal` price fields → mitigated by ArchTest rule banning `decimal` in money-named properties + ESLint rule on frontend.
- Risk: locale fallback bugs in template engine → mitigated by mandatory `en-US` fallback test in Communications module.

### ArchTest Rule Specification (per architect review)

The "ban `decimal` in money-named properties" rule needs concrete regex to prevent endless argument:

**Forbidden pattern**: any property of type `decimal`, `decimal?`, `double`, or `float` whose name matches:

```regex
(?i)(price|amount|fee|cost|total|subtotal|tax|discount|refund|tip|donation|charge)([A-Z_]|$)
```

**Examples that fail the rule**:
- `decimal Price` ❌
- `decimal TotalAmount` ❌
- `decimal? RefundAmount` ❌
- `decimal TipAmount` ❌

**Examples that pass**:
- `Money Price` ✅
- `Money TotalAmount` ✅
- `int PriceCount` ✅ (count, not money)
- `string PriceTier` ✅ (label, not money)

ArchTest implementation: `tests/architecture/MoneyArchitectureTests.cs` reflects all assemblies, asserts no property matches the forbidden pattern.

### Phase A.5 Trigger Definition (per architect review)

Phase A.5 (LocalizedString content migration + per-locale templates) is scheduled when **any one** of these is true:

1. A 2nd country launch is on the roadmap with date < 8 weeks out
2. Marketing requests translated UI for SEO experiment
3. A partner integration requires non-English templates (rare)

The owner is the founder; estimated effort 10–12 days. No auto-trigger; explicit decision.

### Communications W4 Constraint (per architect review)

This ADR requires the per-locale template lookup engine to be designed into the Communications module W4 work. This is a **constraint**, not an emergent property:

- Template entity carries `locale` column (nullable; NULL = default/fallback)
- Lookup logic: `WHERE template_name = X AND (locale = userLocale OR locale IS NULL) ORDER BY locale DESC LIMIT 1`
- Falls back to `en-US` baseline when no locale-specific entry exists
- Phase A ships only `en-US` baselines; Phase A.5 adds other locales

## Rollback

If Phase A.5 becomes critical before scheduled, no rollback needed; this ADR can be revised to incorporate content migration. Money/Locale foundation remains valid.

## References

- Architect review: 2026-04-26 (Question 7 — i18n realism flagged 4–5 weeks underestimated)
- MEMORY.md: pattern of EF migration silent failures — favor smaller, focused migrations
