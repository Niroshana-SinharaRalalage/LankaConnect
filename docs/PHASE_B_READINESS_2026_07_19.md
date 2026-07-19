# Phase B Readiness Memo — 2026-07-19

**Author:** Agent-FounderBriefing (Wave 4, Phase A Final Execution Sprint)
**Date:** 2026-07-19
**Reports to:** Tech Lead → Founder ratifies scope + sequencing
**Sibling docs:**
- `docs/architecture/PHASE_A_COMPREHENSIVE_REVIEW_2026_07_16.md` — D2 (evidence base)
- `docs/coordination/RISK_MATRIX_2026_07_19.md` — D5 (risk-view)
- `docs/coordination/WAVE_85_SEQUENCING_2026_07_19.md` — D6 (retrospective)
- `docs/architecture/EXTRACTABILITY_AUDIT_2026_07_18.md` — extractability grade board
- `docs/architecture/COMMON_COMPONENTS_INVENTORY_2026_07_16.md` — publishable surface + gap register
- `docs/architect-consults/2026-07-16-consult-28-phase-a-completion-review.md` — Consult #28 rulings
- `docs/sprint/PHASE_A_CLOSED.md` — Phase A close-out evidence

---

## Executive Summary

**Phase B status: GO-FOR-LANKATEMPLES-FIRST-SLICE at head `910dc7a9`.** Three of the four Consult #27 Q5 RED gates flipped GREEN this sprint (Wave 8.5.f 100 % closed at `dcd6c492`; Wave 8.5.j ADR-007 authored at `bffbb357`; Wave 8.5.a `LankaConnect.Application` csproj deleted at `2f0f257d`). The fourth gate (Wave 8.5.b) advanced from RED to YELLOW with 6 Part 5 relocation commits. GAP-6 (the umbrella extractability blocker per `EXTRACTABILITY_AUDIT_2026_07_18.md`) is CLOSED — Address/GeoCoordinate + Email/PhoneNumber VOs promoted from `LankaEvents.Domain.ValueObjects` to `SharedKernel.Geo` + `SharedKernel.Contact` at commits `839fec4a` + `d13e2b0b`, extended with Haversine + radius + composite ContactInfo VO + 30 unit tests + ArchTest rules at `ff5d4762` + `0eced7b5`. GAP-1 (LankaTemples cultural-calendar blocker) is unblocked per Tech Lead D-13 with GapClosure-CulturalCalendar dispatched in Wave 4.

**LankaTemples first-slice implementation is unblocked TODAY pending founder ratification.** Scaffold at `36d1fce2` is Consult #27 Q5 GREEN-checklist ≥ 7/8; GAP-1 clearance path is D-13 ratified; GAP-6 is closed; read-only queries do not require Wave 8.5.c ApiRename to land first. The other five Phase B products (LankaBusiness / LankaHomes / LankaMart / LankaSeyla / LankaNivasa) are YELLOW today — each named blocker is either a shared gap-closure (GAP-2/3/4/5) or a product-scope decision (Consult #12 Option D LankaBusiness re-surfacing).

**Recommended first-product sequencing:** LankaTemples read-only slice starts NOW (GAP-1 in flight). GAP-2 (Full-text Search) + GAP-5 (Taxonomy) in parallel next — unblock 5 of 6 remaining products. GAP-3 (Notifications-templating registry) follows once Communications module settles post Wave 8.5.b Part 5. GAP-4 (Sponsorship/promotion primitive) is Business+Mart "featured" gate; deferrable to product #3.

---

## §1 — Per-gate green/yellow/red status

Consult #27 Q5 canonical gate matrix, re-ratified against head `910dc7a9`:

| Gate | Phase-A-close (2026-07-15) | Consult #28 (2026-07-16) | **Today (2026-07-19)** | Closing commits / status |
|---|:---:|:---:|:---:|---|
| Multi-context UoW handlers (Wave 8.5.f + 8.5.h) | RED | YELLOW | **GREEN** | 8.5.f 100 % at `dcd6c492`; 8.5.h retired per D-01 at `2d296aca` + `6b4b4676` |
| JSON-column VO handlers (Wave 8.5.j + ADR) | RED | YELLOW | **GREEN** | ADR-007 authored `bffbb357`; full JSONB audit → zero additional drift |
| Copy-paste from LankaConnect.Infrastructure (Wave 8.5.b) | RED | RED | **YELLOW** | Part 5 relocation shipped (6 commits `73c4ebe5`/`275d6e42`/`9f53a243`/`3337701c`/`aa8babbd`/`320d8fb0`); residual = 8.5.c ApiRename queued |
| Cross-product read via legacy Application (Wave 8.5.a-refined) | RED | RED | **GREEN** | `LankaConnect.Application` csproj DELETED at `2f0f257d`; Dashboard cross-module Query pair folded into Host at `c2a6e3fc` |
| **Umbrella extractability blocker** (GAP-6 — ContactInfo/Geo promotion) | — (surfaced 2026-07-18 by EXTRACTABILITY_AUDIT) | — | **GREEN** | Core promotion `839fec4a` + `d13e2b0b`; extras `ff5d4762` + `0eced7b5` |
| **LankaTemples product blocker** (GAP-1 — CulturalCalendar promotion) | — | RED | **UNBLOCKED (in flight)** | D-13 Option A primitive-parameter refactor; GapClosure-CulturalCalendar dispatched Wave 4 |

**Three of four Consult #27 gates GREEN; the fourth is YELLOW pending 8.5.c ApiRename. GAP-6 umbrella closed. GAP-1 unblocked.**

---

## §2 — Per-product green/yellow/red status with named blockers

Six future products from `docs/SESSION_PRIMER.md` §1 (per Consult #28 out-of-scope constraint: no new products).

### 2.1 LankaTemples — GREEN for read-only first-slice; YELLOW for write path

**Domain concepts:** Temple registry, poya-driven puja recurring schedule, donation intake, event RSVP.

**Product-shape prior:** read-heavy (directory + calendar), donation write, event-listing write.

| Slice | Status | Blocker |
|---|:---:|---|
| Scaffold (Domain/Application/Contracts/Infrastructure/API csprojs + DbContext skeleton + ArchTest + 501 controllers) | **DONE** at `36d1fce2` | — |
| Read-only first slice (temple directory list + detail + poya-calendar read) | **GREEN today** | GAP-1 CulturalCalendar in flight (Tech Lead D-13 unblocks); founder ratification |
| Donation write slice | YELLOW | Payments module + Wave 8.5.b Part 5 tail (~2-3 days after ApiRename) |
| RSVP write slice | YELLOW | LankaEvents extraction pattern or cross-context read (Consult #7 Delta §5.3) — Phase B-implementation-time decision |

**Blockers named:** GAP-1 (in flight); founder ratification.

**Recommended start:** THIS WEEK once GapClosure-CulturalCalendar closes.

### 2.2 LankaBusiness — YELLOW; product decision pending

**Domain concepts:** Sri Lankan business directory + reviews + featured/promoted listings + geo-radius directory search + category browse + subscription-tier billing.

**Product-shape prior:** heavy on category browse + geo search + full-text search.

| Slice | Status | Blocker |
|---|:---:|---|
| Scaffold | pending | Consult #12 Option D reversal (product re-surfacing decision) — **founder decision** |
| Read-only directory listing | RED | GAP-2 Full-text search + GAP-5 Taxonomy required first |
| Business profile write | RED | Wave 8.5.k residual (Businesses removed 2026-07-16 per `c9df3599`); founder direction on re-adding |
| Subscription billing | YELLOW | Payments capability available; needs product-tier catalog |
| Featured listings | RED | GAP-4 Sponsorship/promotion primitive required |

**Blockers named:** Consult #12 Option D reversal (founder); GAP-2 / GAP-4 / GAP-5.

**Recommended start:** after GAP-2 + GAP-5 land; founder ratifies product-scope doc first.

### 2.3 LankaHomes — YELLOW; GAP-6 CLOSED but GAP-2/5 pending

**Domain concepts:** Housing/real-estate marketplace, listing aggregate, inquiry, open-house scheduling, radius search "homes near me", saved-search alerts.

**Product-shape prior:** heavy on geo-radius search + inquiry forms + scheduling.

| Slice | Status | Blocker |
|---|:---:|---|
| Scaffold | pending | Straightforward — mirror LankaTemples scaffold pattern |
| Read-only listing directory | YELLOW | GAP-2 Full-text search + GAP-5 Taxonomy required |
| Geo-radius "near me" | **GREEN today** | GAP-6 CLOSED at `839fec4a` + `ff5d4762` (Haversine + radius LINQ extensions in SharedKernel.Geo) |
| Inquiry form + open-house schedule | YELLOW | Forms capability available; Scheduling.Contracts is bare (needs formalization) |
| Match-alert notifications | YELLOW | GAP-3 Notifications-templating registry required |

**Blockers named:** GAP-2 (P0), GAP-5 (P0), GAP-3 (P1).

**Recommended start:** product #3 or #4 (after GAP-2/5 shipped).

### 2.4 LankaMart — YELLOW; needs GAP-2/4/5

**Domain concepts:** Goods marketplace, product listing, cart, order, seller profile, promoted products, category browse, product search.

**Product-shape prior:** search + cart/order + category browse — every gap fires.

| Slice | Status | Blocker |
|---|:---:|---|
| Scaffold | pending | Straightforward |
| Product listing directory | YELLOW | GAP-2 Full-text search + GAP-5 Taxonomy required |
| Cart + order + payments | YELLOW | Payments capability available; new saga pattern via integration events |
| Promoted products | RED | GAP-4 Sponsorship/promotion primitive required |

**Blockers named:** GAP-2 / GAP-4 / GAP-5.

**Recommended start:** product #3 or #4 (co-schedule with LankaHomes; share GAP-2 + GAP-5 work).

### 2.5 LankaSeyla — YELLOW; GAP-6 CLOSED

**Domain concepts:** Community-service registry, service-provider profile, offer/help intent, matching, reviews, flash-sale slot.

| Slice | Status | Blocker |
|---|:---:|---|
| Scaffold | pending | Straightforward |
| Service-provider directory | YELLOW | GAP-2 + GAP-5 |
| "Helpers near me" geo-radius | **GREEN today** | GAP-6 CLOSED |
| Matching + reviews | YELLOW | Scheduling.Contracts expansion; Reviews cap needs authoring |
| Flash-sale slots | YELLOW | Scheduling capability formalization |

**Blockers named:** GAP-2 / GAP-5; Scheduling.Contracts expansion.

**Recommended start:** product #5.

### 2.6 LankaNivasa — YELLOW; GAP-3 is soft-blocker

**Domain concepts:** Immigration/settlement resources, resource catalog, question forum, expert Q&A.

| Slice | Status | Blocker |
|---|:---:|---|
| Scaffold | pending | Straightforward |
| Resource catalog | YELLOW | GAP-2 Search + GAP-5 Taxonomy |
| Q&A forum | YELLOW | Forum aggregates disposition (Consult #21 pending on Communications-Forum families) |
| Resource-alert notifications | YELLOW | GAP-3 templating (soft-blocker, MVP can inline) |

**Blockers named:** GAP-2 / GAP-5; Consult #21 Forum disposition (soft); GAP-3 (soft).

**Recommended start:** product #6.

---

## §3 — Common-components inventory summary + gap-closure completion status

Per `docs/architecture/COMMON_COMPONENTS_INVENTORY_2026_07_16.md` and this sprint's Wave 3/4 closures:

### 3.1 SharedKernel publishable surface

| Csproj | Status | Purpose |
|---|:---:|---|
| SharedKernel.Contracts | ✅ published (marker) | Namespace anchor |
| SharedKernel.Cultural | ✅ published (rich) | Sri Lankan cultural vocabulary + ReferenceData lookups |
| SharedKernel.Geo | ✅ published (extended) | MetroAreaDto + **GeoCoordinate (Haversine + WithinRadiusKm) + Address + ContactInfo composite** (GAP-6) |
| SharedKernel.Identity | ✅ published | UserId + IUserContext |
| SharedKernel.Locale | ✅ published | Locale + Country |
| SharedKernel.Money | ✅ published | Money + Currency + CurrencyJsonConverter |
| SharedKernel.Time | 🟡 scaffold | Placeholder — needs Duration + TimeZoneCode when TZ-aware use-case surfaces |
| **SharedKernel.Contact** (NEW this sprint) | ✅ published | Email + PhoneNumber promoted from LankaEvents.Domain (GAP-6 core) |

### 3.2 BuildingBlocks publishable surface

All six csprojs published: Abstractions / Application / Contracts / Domain / Infrastructure / Web. Cleanup candidates named in `COMMON_COMPONENTS_INVENTORY_2026_07_16.md` §Appendix A (dead surface: ~90 CulturalIntelligence DTOs, `RootLegacy/Businesses`, `EnterpriseContractTier`/`SubscriptionTier`, `USStateHelper`). Non-blocking for Phase B.

### 3.3 Capability publishable surface (Module.Contracts)

| Capability | Contract-surface health | Ready for Phase B? |
|---|:---:|---|
| Identity | Rich (5 interfaces + 12 DTOs) | ✅ |
| Payments | Rich (~30 request/result DTOs + IStripePaymentService) | ✅ |
| Communications | Rich (18+ interfaces + 44 typed email params + WhatsApp templates) | ✅ (LegacyPromotions split complete post `2aed1ded`) |
| Media | Minimal (IImageService + 2 integration events) | ✅ |
| Forms | Rich (2 interfaces + 9 DTOs) | ✅ |
| Notifications | Minimal (INotificationDispatcher + 1 integration event) | ✅ |
| Scheduling | **Bare (2 interfaces + AssemblyMarker only)** | 🟡 needs expansion when LankaTemples first-slice consumes |
| CulturalIntelligence | **Bare (AssemblyMarker only; GAP-1 layer inversion)** | 🟡 D-13 refactor in flight |

### 3.4 Gap-closure completion status

| Gap | Owner | Priority | Status | Closing commits |
|---|---|:---:|:---:|---|
| GAP-1 CulturalCalendar promotion + real poya calendar | GapClosure-CulturalCalendar (Wave 4) | P0 for LankaTemples | **UNBLOCKED via D-13 Option A; in flight** | pending |
| GAP-2 Full-text search abstraction | (queued Phase B kickoff) | P0 | Queued | — |
| GAP-3 Notifications-templating registry formalization | (queued) | P1 | Queued | — |
| GAP-4 Sponsorship/promotion cross-product primitive | (queued) | P1 | Queued | — |
| GAP-5 Taxonomy / hierarchical categorization | (queued Phase B kickoff) | P0 | Queued | — |
| GAP-6 ContactInfo + Geo VO promotion | LayerInversion → GapClosure-Geo (Wave 3) | P0 | **CORE + EXTRAS CLOSED** | `839fec4a` (Address+GeoCoordinate → SharedKernel.Geo); `d13e2b0b` (Email+PhoneNumber → SharedKernel.Contact); `ff5d4762` (Haversine + radius + ContactInfo composite + 30 tests); `0eced7b5` (ArchTest rules + usage doc) |

---

## §4 — Recommended first-slice sequencing

**Ordering rationale:** highest-count-of-unblocked-products first; layer-inversion-un-inverters ahead of business-scope decisions; smallest-scope-that-unblocks-real-implementation prioritized over largest.

### Step 1 — LankaTemples read-only first-slice (THIS WEEK)

- **Why first:** GAP-1 unblocked via D-13 Option A; GAP-6 CLOSED; product scaffold at `36d1fce2` is Consult #27 Q5 GREEN-checklist ≥ 7/8; read-only queries do not depend on Wave 8.5.c ApiRename.
- **Scope:** temple directory list + detail; poya-day calendar read; basic search over temple names (no full-text needed for MVP).
- **Effort estimate:** 5-8 days first slice (1 developer or 3-4 agents in parallel).
- **Non-gates:** GAP-2 (Search) — deferred to Phase B step 3; MVP uses simple name-prefix match.
- **Founder decision:** ratify LankaTemples first-slice product-scope doc + first-week priority.

### Step 2 — GAP-2 (Search) + GAP-5 (Taxonomy) in parallel (2-3 weeks parallel)

- **Why together:** both P0, both universal to 5 of 6 remaining products, no cross-dependency.
- **GAP-2 scope:** `Capabilities/Search.Contracts` with `ISearchService` port + Postgres `tsvector` MVP impl; Meilisearch/Azure Cognitive Search swappable behind the port. 3-4 days.
- **GAP-5 scope:** `Capabilities/Taxonomy.Contracts` with `ICategoryTreeService` + hierarchical `CategoryNode` model + per-locale labels. 2-3 days.
- **After Step 2 lands:** LankaHomes / LankaMart / LankaSeyla / LankaNivasa unblocked for scaffold + read-only slice.

### Step 3 — GAP-3 (Notifications-templating registry) (1 week)

- **Why third:** universal but MVP-workaround exists (products can inline templates). Promote `EmailTemplateContract`/`ITypedEmailService` pattern into cross-product registry with per-product template scopes. 2 days.
- **Prerequisite:** Communications module settled post Wave 8.5.b Part 5 (already done at `aa8babbd`).

### Step 4 — GAP-4 (Sponsorship/promotion primitive) (~ 1 week)

- **Why fourth:** LankaEvents ships fine on current in-Product impl; only Business "featured" + Mart "promoted" need it before their featured slices. 3 days.
- **Scope:** `Capabilities/Sponsorship.Contracts` polymorphic `Sponsor<TSponsee>` with adapters for Event, Business, Product.

### Step 5+ — Remaining product-first-slices

Recommended landing order per shared-blocker overlap:

1. LankaTemples (Step 1 above)
2. LankaHomes (post GAP-2/5) — shares infrastructure pattern with LankaTemples (scheduling + inquiries + geo)
3. LankaMart (co-scheduled with LankaHomes)
4. LankaBusiness — requires Consult #12 Option D reversal decision (founder) + GAP-4
5. LankaSeyla — after Reviews capability authoring + Scheduling.Contracts expansion
6. LankaNivasa — last (soft-blockers only)

---

## §5 — Extraction-readiness (per `EXTRACTABILITY_AUDIT_2026_07_18.md`)

Founder objective: *"each module extractable with minimal effort."*

**Sprint delta:** GAP-6 (the umbrella extractability blocker) CLOSED. Per audit's headline: *"5 of 6 modules currently treat `LankaEvents.Domain` as a de-facto Shared Kernel. GAP-6 (SharedKernel.ContactInfo + Geo promotion) is the umbrella-fix."* This is now DONE.

Extraction-grade board revised per this sprint's closures:

| # | Module | Grade at Consult #28 | **Grade today** | Headline blocker today |
|---:|---|:---:|:---:|---|
| 1 | Notifications | GREEN | **GREEN** | None. Extraction-pilot candidate. |
| 2 | Payments | N/A | **N/A** (per Consult #7 Delta permanent) | Not extracted. |
| 3 | Media | RED (4-6d) | **YELLOW (2-4d)** | MED-INV-01 handler relocation (PhotoAlbums LankaEvents.Application → Media.Application) remains. GAP-6 unblocks Domain layer. |
| 4 | Forms | RED (4-6d) | **YELLOW (3-5d)** | FRM-APP-01 introduce `IEventQueries`/`IRegistrationQueries` in `LankaEvents.Contracts`. GAP-6 removed Domain-layer coupling. |
| 5 | Identity | RED (5-7d) | **YELLOW (3-4d)** | ID-APP-01 introduce `IMetroAreaQueries` in `LankaEvents.Contracts` (see also Wave 8.5.i landing at `7e98bf94`+`b6a576d3` which retired raw-SQL cross-module writes). GAP-6 removed User.Email/PhoneNumber cross-Product coupling. |
| 6 | Communications | RED (5-8d) | **YELLOW (3-5d)** | COMM-CT-01 rewrite `IRegistrationEmailService` with LankaEvents-agnostic DTOs. GAP-6 removed Domain-layer coupling. |
| 7 | LankaEvents | YELLOW (2-4d) | **YELLOW (2-4d)** | Wave 8.5.d/e LegacyPromotions split (2 of 3 shipped this sprint; `ba25bc4e` Media + `2aed1ded` Communications; LankaEvents.Contracts split likely 3/3 via `910dc7a9`). LE-CT-01 rewrite to reference Identity.Contracts. |

**Recommendation:** Notifications extraction pilot within 2 sprints — validates extraction runbook (Program.cs bootstrap, deploy workflow, container-app resource, Bicep) before any real product-scale extraction. ~1 day work.

---

## §6 — What founder needs to decide this week

1. **Ratify Phase A close-out at head `910dc7a9`** — Consult #26 pattern (8.5.c ApiRename + 8.5.e workflow tail + 8.5.l verification as Phase-A-close carryover, not blocking).
2. **Approve LankaTemples first-slice implementation start** — read-only queries only; GAP-1 clearance imminent; GAP-6 done; founder-scope-decision needed for first-week priority (which temple listings ship first).
3. **Ratify gap-closure sequencing** per §4 (GAP-2 + GAP-5 parallel next; GAP-3 third; GAP-4 fourth).
4. **LankaBusiness product-scope decision (Consult #12 Option D reversal)** — founder-authored product-scope doc required before LankaBusiness scaffold restart. Not urgent — deferrable to product #4.
5. **Schedule 30-min UI UAT walkthrough** — coordinate with 8.5.c ApiRename timing so playbook doesn't need re-authoring.
6. **Notifications extraction pilot decision** — ratify or defer; low-risk single-day of work to validate extraction runbook before larger extraction bets.

---

## §7 — Success criteria for Phase B first-slice

Same shape as Consult #27 Q4 canonical Phase-A DoD, adapted per-product:

**LankaTemples first-slice CLOSED when:**
- Solution builds against LankaConnect.API entry point (0 errors)
- Wave 9 smoke suite includes 5-10 `templates-*` endpoint tests, all green
- ArchTest zero CI-blocking failures for LankaTemples csprojs
- Zero staging migration drift on LankaTemplesDbContext
- Frontend `web` workspace builds against new LankaTemples API contract
- Operator UAT walkthrough of temple-directory browse flow signed off

Wave discipline: same Rule 5j.4 T-triggers + S-class per commit. Every LankaTemples handler adds a unit test + staging smoke same-commit.

---

## §8 — Related canonical documents

- `docs/architecture/PHASE_A_COMPREHENSIVE_REVIEW_2026_07_16.md` — D2 evidence base for every claim in this memo
- `docs/architecture/EXTRACTABILITY_AUDIT_2026_07_18.md` — per-module extraction feasibility grade + GAP-6 umbrella-fix rationale
- `docs/architecture/COMMON_COMPONENTS_INVENTORY_2026_07_16.md` — publishable surface + 6-gap register
- `docs/architecture/DBCONTEXT_OWNERSHIP_MATRIX.md` — canonical 7-DbContext ownership (replaces Consult #7 Delta §2.4's stale "5 DbContexts")
- `docs/architecture/decisions/ADR-007-json-column-value-objects.md` — JSON VO shape-locking pattern for Phase B products
- `docs/architect-consults/2026-07-16-consult-28-phase-a-completion-review.md` — Consult #28 rulings this memo re-ratifies
- `docs/coordination/RISK_MATRIX_2026_07_19.md` — D5 risk view
- `docs/coordination/WAVE_85_SEQUENCING_2026_07_19.md` — D6 sequencing retrospective
