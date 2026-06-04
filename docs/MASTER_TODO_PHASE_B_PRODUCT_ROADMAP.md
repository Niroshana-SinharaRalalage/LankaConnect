# Master TODO — Phase B: Product Module Roadmap

| | |
|---|---|
| **Plan Version** | v1 (created 2026-06-04 alongside Phase A v5 wave plan) |
| **Phase B Trigger** | Phase A Wave 8 final task complete (3-week stabilization soak passed, zero P0/P1 outstanding) |
| **Phase B Estimated Start** | ~Late October 2026 (depending on Phase A nominal pace) — early December 2026 (risk-adjusted) |
| **Phase B Goal** | Build new product modules on top of the stable Phase A foundation, following the Capability/Product pattern from ENTERPRISE_ARCHITECTURE_BLUEPRINT.md |
| **Authoritative Architecture** | [ENTERPRISE_ARCHITECTURE_BLUEPRINT.md](architecture/ENTERPRISE_ARCHITECTURE_BLUEPRINT.md) |
| **Related** | [MASTER_TODO_PHASE_A_MODULAR_MONOLITH.md](MASTER_TODO_PHASE_A_MODULAR_MONOLITH.md) (Phase A wave plan) |

---

## Why this document exists

Phase A (modular monolith refactor) finishes with `Products/LankaEvents` as the proof point. Phase B builds the next 6 products against the same Capability foundation. Without a roadmap, the temptation will be to start the most exciting product first (LankaSeyla e-commerce) rather than the simplest proof-of-architecture product (LankaTemples). This document prevents that mistake.

---

## Phase A → Phase B Handoff

### Last TODO of Phase A (refactor end)

**Phase A Wave 8 final**: 3-week production stabilization soak completed; zero P0/P1 outstanding; LankaEvents end-to-end smoke test (list → detail → register → pay → photos → forms → cancel/refund) matches pre-refactor baselines; `MODULE_OVERVIEW.md` written for Phase 2 onboarding.

### First TODO of Phase B (this document)

**Phase B.W0.1**: Create `src/Products/LankaTemples/` skeleton per the Capability/Product template documented in `ENTERPRISE_ARCHITECTURE_BLUEPRINT.md` §1 + the `CAPABILITY_CONTRACT.md` template (Phase A Wave 5 deliverable).

---

## Future Product Inventory (7 products)

| Product | Description | Phase B order | Estimate | Capabilities consumed |
|---|---|---|---|---|
| **LankaTemples** | Sri Lankan diaspora temple/community events; poya days, religious observance schedules | **1st (recommended)** | 4-6 weeks | Scheduling, Communications, Identity, CulturalIntelligence (heavy), Notifications |
| **LankaBusiness** | Business directory; member businesses, profiles, search | 2nd | 4-6 weeks | Identity, Media, Forms (business profile), Notifications, Communications |
| **LankaHomes** | Real estate listings (rental + sale) for diaspora returning home | 3rd | 6-8 weeks | Identity, Media (gallery), Forms (inquiry), Communications, Payments (premium listings) |
| **LankaMart** | Commerce — physical goods (Sri Lankan products to diaspora) | 4th | 8-10 weeks | Identity, Media, Payments (Stripe), Forms (shipping), Notifications, Communications |
| **LankaSeyla** | Commerce — clothing (saree, kurtha, designer pieces) | 5th | 6-8 weeks | Same as LankaMart but visual-heavy (Media-first UI patterns) |
| **LankaNivasa** | Hospitality — diaspora-friendly accommodations in Sri Lanka | 6th | 8-10 weeks | Scheduling (booking windows), Payments, Media, Identity, Communications |
| **LankaEvents v2** | Re-platform LankaEvents on the cleaned Capability foundation | (already done in Phase A Wave 5) | n/a | All capabilities |

**Total Phase B**: ~40-50 weeks (10-12 months) for all 6 new products at solo-founder pace. Realistic with parallel contractor support: ~6-8 months.

---

## Why LankaTemples First (architect-recommended)

The first new product proves the Capability/Product pattern. The wrong first choice (LankaSeyla, LankaMart) risks discovering architecture gaps under pressure of e-commerce complexity (Stripe, tax, fulfillment, inventory).

LankaTemples is the **lowest-risk proof point**:

- **No payment complexity** — temples post free events; no tickets, no money flow
- **Heavy Scheduling capability use** — validates that Scheduling extraction from Events (Phase A Wave 4) was correctly generalized
- **Heavy CulturalIntelligence capability use** — validates ICulturalCalendarService + poya/festival queries work cleanly
- **Identity + Notifications + Communications** — validates standard cross-capability patterns
- **Real diaspora audience** — actual customer value delivered, not a toy product

If LankaTemples ships in 4-6 weeks WITHOUT architectural rework, the foundation is proven and Products 2-6 follow the same template at 4-10 weeks each.

If LankaTemples surfaces architecture gaps, address them while only ONE new product is built — far cheaper than discovering them mid-LankaMart.

---

## Per-Product Phase B Template

Each new product follows the same shape:

### Phase B.{Product}.W0 — Architecture audit (1 week)

- Read ENTERPRISE_ARCHITECTURE_BLUEPRINT.md + Phase A Wave 5 LankaEvents carve-out as reference
- Identify which capabilities the product needs (consult capability matrix above)
- Identify any NEW capability surface needed (e.g., LankaMart might need a new `Capabilities/Inventory`)
- Write `Products/{Product}/CAPABILITY_CONTRACT.md` per template
- Document expected aggregates, integration events published/consumed, schema migrations order

### Phase B.{Product}.W1 — Product skeleton (1 week)

- Create `src/Products/{Product}/` with 5 csprojs: `{Product}.Domain`, `{Product}.Application`, `{Product}.Infrastructure`, `{Product}.Api`, `{Product}.Contracts`
- Per-product DbContext with own schema (`temples.*`, `homes.*`, `mart.*`, etc.)
- Outbox + IdempotencyKey + DeadLetter tables in product schema (ArchTest enforces)
- Wire to capability Contracts assemblies
- ArchTest rules pass: Product references only BuildingBlocks + SharedKernel + Capabilities.*.Contracts

### Phase B.{Product}.W2+ — Aggregate-by-aggregate feature work

- Each aggregate gets: domain types + repository + handlers + integration tests + API endpoints + frontend feature package + e2e smoke
- 1-2 aggregates per week typical
- Feature flag per aggregate during ramp (per ADR-004)
- Per-aggregate staging soak before production canary

### Phase B.{Product}.W{N} — Production launch

- 7-day staging soak with API regression
- Production canary (1% → 10% → 50% → 100% per ADR-004)
- 2-week stabilization

---

## NEW capabilities likely surfaced during Phase B

Per architect analysis, these capabilities will likely be needed but are NOT in the Phase A capability set:

| Capability | Triggered by | Phase B introduction |
|---|---|---|
| `Capabilities/Inventory` | LankaMart, LankaSeyla (stock counts, reservations) | Build during LankaMart W2 |
| `Capabilities/Search` | LankaBusiness (directory search), LankaHomes (filter search) | Build during LankaBusiness W2; reuse for LankaHomes |
| `Capabilities/Reviews` | LankaMart, LankaSeyla, LankaNivasa | Build during LankaMart W3 |
| `Capabilities/Geolocation` | LankaHomes (map view), LankaNivasa (location search) | Build during LankaHomes W2 |
| `Capabilities/Booking` | LankaNivasa (accommodation booking) | Build during LankaNivasa W2 |

Each NEW capability follows the Capability/Product topology rules — `Capabilities/{Name}/{Domain,Application,Infrastructure,Api,Contracts}` with the same ArchTest enforcement.

---

## Phase B Timeline (high-level)

| Month | Work | Cumulative |
|---|---|---|
| 1 (Phase B start) | LankaTemples W0-W1 | LankaTemples skeleton + first aggregate |
| 2 | LankaTemples W2-W4 | LankaTemples to production canary |
| 3 | LankaTemples stabilization + LankaBusiness W0-W1 | LankaTemples shipped; LankaBusiness skeleton |
| 4-5 | LankaBusiness build | LankaBusiness to production |
| 6-7 | LankaHomes build | LankaHomes to production |
| 8-9 | LankaMart build (Inventory + Reviews capabilities introduced) | LankaMart to production |
| 10-11 | LankaSeyla build (reuses LankaMart capabilities) | LankaSeyla to production |
| 12-14 | LankaNivasa build (Booking + Geolocation capabilities introduced) | LankaNivasa to production |

Solo-founder pace; parallel contractor support compresses by 30-40%.

---

## What this document is NOT

- Per-product detailed task lists (those land in `MASTER_TODO_PHASE_B_{PRODUCT}.md` per product when that product's Phase B.W0 starts)
- Capability extension specs (those land in their own ADRs per new capability)
- Frontend per-product specifics (Wave 7 Turborepo enables per-product app shells; details per-product)
- Phase B end-of-life criteria (Phase B doesn't have a single "done" — each product ships and operates independently)

---

## Open Questions for Founder (resolve before Phase B start)

1. **Phase B sequencing**: confirm LankaTemples first, or override (e.g., if business case justifies LankaMart first despite higher risk)
2. **Capability extension trigger**: which NEW capabilities (Inventory, Search, Reviews, Geolocation, Booking) are built JUST-IN-TIME for first consumer vs upfront?
3. **Frontend strategy**: each product gets its own `apps/{product}-web/` Next.js app OR consolidated under one app with product-aware routing? (Recommend: per-product apps for true isolation; share via packages)
4. **Hosting strategy**: each product hosts on its own Container App OR all products share `Host.AllInOne`? (Recommend: AllInOne until per-product traffic justifies split)
5. **Branding/marketing infrastructure**: is each product its own brand (separate marketing site, separate identity) or sub-brands of LankaConnect? (Affects URL strategy, SEO, authentication flow)

These do not block Phase A. Resolve during Phase A Wave 7-8 (final stabilization) when Phase B start is imminent.

---

## Status Update Log

- 2026-06-04: Document created alongside Phase A v5 wave plan. Phase B does not start until Phase A Wave 8 final task complete.
