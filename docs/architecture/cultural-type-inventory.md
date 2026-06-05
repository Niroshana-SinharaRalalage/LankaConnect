# Cultural Type Inventory — Wave 2 (W2A)

**Status**: Inventory complete 2026-06-04. Drives W2C (enum moves), W2D (VO moves), W2E (service interface moves), W2F (dedupe).
**Authoritative architecture**: [ENTERPRISE_ARCHITECTURE_BLUEPRINT.md](./ENTERPRISE_ARCHITECTURE_BLUEPRINT.md) §2.D2 + Wave 2.
**ADR**: [ADR-008-cultural-shared-kernel-phase-a.md](./ADR-008-cultural-shared-kernel-phase-a.md).

---

## Scope decision

**MOVES to `SharedKernel.Cultural`**: cultural PRIMITIVES (value objects, enums, service interfaces) consumed by 2+ modules.

**STAYS in legacy `LankaConnect.Domain` for now** (extracted to `Capabilities/CulturalIntelligence` in Wave 4, NOT Wave 2):
- `Billing/CulturalIntelligenceBilling.cs`, `Billing/CulturalIntelligenceTier.cs` — Billing-specific feature code
- `Common/Database/Cultural*.cs` — persistence/EF mapping helpers
- `Common/Monitoring/Cultural*.cs` — telemetry/observability for the feature
- `CulturalIntelligence/` subfolder — full feature impl (orchestration, state, routing)
- `Enterprise/ValueObjects/Cultural*.cs` — Enterprise-tier feature flags
- `Infrastructure/Failover/Cultural*.cs`, `Infrastructure/Scaling/Cultural*.cs` — operational HA code
- `Shared/Cultural*.cs` — these are POCO types specific to Backup/Sync/Priority feature flows
- `Communications/Services/Cultural*.cs`, `Communications/Services/Diaspora*.cs`, `Communications/Services/MultiCultural*.cs` — service IMPLEMENTATIONS (interfaces move; impls stay until Wave 4)
- `Events/Services/CulturalCalendar.cs` — service impl
- `Users/ValueObjects/CulturalInterest.cs` — user-specific VO (might move to Capabilities/Identity in Wave 4)

Rule of thumb: if it ENDS in `Service`, `Orchestrator`, `Validator`, `Engine`, `Scaling`, `Failover`, `Tier`, `Billing`, `Metrics`, `Insights`, `Audit` — it's BEHAVIOR/PRODUCT code, not a primitive. Stays in legacy until Wave 4 lifts to `Capabilities/CulturalIntelligence`.

---

## A. Canonical types that MOVE in Wave 2 (27 types)

### A.1 Enums (9 — W2C) — architect-revised 2026-06-04

| # | Type | Current path | Target |
|---|---|---|---|
| 1 | `SriLankanLanguage` | `Communications/Enums/SriLankanLanguage.cs` | `SharedKernel.Cultural` (3-value narrow scope: Sinhala/Tamil/English for SL operations) |
| 2 | `SouthAsianLanguage` | `Common/Enums/SouthAsianLanguage.cs` (19 values + extensions class) | `SharedKernel.Cultural` (broad scope for Phase 6 cultural routing; **architect ruling Q1**: NOT a duplicate of SriLankanLanguage — distinct narrow/broad pair). Absorbs 4 regional variants (SriLankanTamil, IndianTamil, PakistaniUrdu, IndianUrdu) from the routing-models duplicate per architect Q2. |
| 3 | `GeographicRegion` | `Communications/Enums/GeographicRegion.cs` (CANONICAL — closed regional taxonomy enum) | `SharedKernel.Cultural` |
| 4 | `CulturalDataType` | `Common/Enums/CulturalDataType.cs` | `SharedKernel.Cultural` |
| 5 | `CulturalEventType` | `Common/Enums/CulturalEventType.cs` | `SharedKernel.Cultural` |
| 6 | `DiasporaEngagementType` | `Common/Enums/DiasporaEngagementType.cs` | `SharedKernel.Cultural` |
| 7 | `CulturalBackground` | `Communications/Enums/CulturalBackground.cs` (CANONICAL) | `SharedKernel.Cultural` |
| 8 | `ReligiousContext` | `Communications/Enums/ReligiousContext.cs` | `SharedKernel.Cultural` |
| 9 | `CulturalPriority` | **architect Q5 ruling**: treat as non-existent unless surfaced during W2D execution | (deferred / N/A) |

### A.2 Value Objects (16 — W2D)

All currently in `Communications/ValueObjects/`. Move to `SharedKernel.Cultural`.

| # | Type | File |
|---|---|---|
| 1 | `CulturalContext` | CulturalContext.cs |
| 2 | `CulturalEvent` | CulturalEvent.cs (CANONICAL — the class, NOT the enum dupes elsewhere) |
| 3 | `CulturalAppropriateness` | CulturalAppropriateness.cs |
| 4 | `CulturalConflict` | CulturalConflict.cs (CANONICAL — Communications class, NOT Events record OR CulturalUserProfile nested record) |
| 5 | `CulturalProfile` | CulturalProfile.cs |
| 6 | `CulturalCalendarSync` | CulturalCalendarSync.cs |
| 7 | `CulturalTimingPreference` | CulturalTimingPreference.cs |
| 8 | `CrossCulturalEvent` | CrossCulturalEvent.cs |
| 9 | `DiasporaCommunityProfile` | DiasporaCommunityProfile.cs |
| 10 | `DiasporaRelevance` | DiasporaRelevance.cs |
| 11 | `MultilingualContent` | MultilingualContent.cs |
| 12 | `MultilingualDescription` | MultilingualDescription.cs |
| 13 | `RecipientCulturalProfile` | RecipientCulturalProfile.cs |
| 14 | `MultiCulturalCommunity` | MultiCulturalCommunity.cs |
| 15 | `GoogleCalendarCulturalEvent` | GoogleCalendarCulturalEvent.cs |
| 16 | `TempleScheduleIntegration` | TempleScheduleIntegration.cs |

Note: `MultiCulturalSupporting` (blueprint listed) and `CulturalEmailContext` (blueprint did NOT list but exists in Communications/ValueObjects) — TBD during W2D (search source for actual file presence).

### A.3 Service interfaces (2 — W2E)

| # | Type | Current path | Target | Notes |
|---|---|---|---|---|
| 1 | `ICulturalCalendarService` | `Communications/Services/ICulturalCalendarService.cs` | `SharedKernel.Cultural` | Interface only; impl stays in `Communications/Services/` until Wave 4 extracts to `Capabilities/CulturalIntelligence` |
| 2 | `ICulturalAppropriatenessChecker` | (search) | `SharedKernel.Cultural` | Same pattern as above |

---

## B. Duplicates that need resolution (W2F)

These are real bugs from Phase 6-7 copy-paste. Each duplicate has a CANONICAL version (per A above) and JUNK versions to delete.

### B.1 `GeographicRegion` — 4 definitions (architect ruling Q3: 3 enums consolidate; 1 class is a distinct concept that RENAMES)

| Path | Disposition |
|---|---|
| `Communications/Enums/GeographicRegion.cs` | **KEEP** (canonical enum, moves to SharedKernel.Cultural) |
| `Common/Enums/GeographicRegion.cs` | DELETE after migrating callers |
| `Events/Enums/GeographicRegion.cs` | DELETE after migrating callers |
| `Billing/BillingSupportingTypes.cs` (line 168, `class GeographicRegion : ValueObject`) | **RENAME** to `BillingRegion` — architect Q3 ruling: this is an OPEN-TEXT tax-jurisdiction VO (Name/Country/Continent fields), NOT the closed regional enum. Distinct concept that happens to share a name. Update Billing callers. |

### B.2 `CulturalConflict` — 3 definitions (KEEP Communications class, DELETE 2)

| Path | Disposition |
|---|---|
| `Communications/ValueObjects/CulturalConflict.cs` (class) | **KEEP** (canonical, moves to SharedKernel.Cultural) |
| `Events/ValueObjects/CulturalConflict.cs` (record) | DELETE — was a parallel record-based variant; migrate Events callers to use the Communications class |
| `Common/Users/CulturalUserProfile.cs` line 127 (`record CulturalConflict(...)` nested) | DELETE — nested record; migrate inline callers |

### B.3 `CulturalEvent` — 4 definitions (KEEP Communications class, DELETE 3)

| Path | Disposition |
|---|---|
| `Communications/ValueObjects/CulturalEvent.cs` (class) | **KEEP** (canonical, moves to SharedKernel.Cultural) |
| `Shared/CulturalTypes.cs` line 15 (`enum CulturalEvent`) | DELETE — this is a *different* concept (event kind) that masquerades as the same name; rename to `CulturalEventKind` and keep, OR delete + use `CulturalEventType` enum |
| `Common/Database/MultiLanguageRoutingModels.cs` line 131 (`enum CulturalEvent`) | DELETE — duplicate of Shared's enum |
| `Infrastructure/Scaling/CulturalIntelligencePredictiveScaling.cs` line 11 (`class CulturalEvent : ValueObject`) | DELETE — copy-paste duplicate; migrate caller to use canonical Communications class |

### B.4 `CulturalContext` — 2 definitions (KEEP Communications, DELETE 1)

| Path | Disposition |
|---|---|
| `Communications/ValueObjects/CulturalContext.cs` | **KEEP** (canonical) |
| `Common/Database/AdditionalMissingModels.cs` line 260 (`class CulturalContext`) | DELETE — POCO duplicate; migrate to canonical |

### B.5 `CulturalBackground` — 3 definitions (KEEP Communications, DELETE 2)

| Path | Disposition |
|---|---|
| `Communications/Enums/CulturalBackground.cs` | **KEEP** (canonical) |
| `Shared/CulturalTypes.cs` line 32 (enum) | DELETE — duplicate |
| `Common/Database/MultiLanguageRoutingModels.cs` line 100 (enum) | DELETE — duplicate |

### B.6 `SouthAsianLanguage` vs `SriLankanLanguage` — architect Q1 ruling: NOT duplicates

These are a narrow/broad PAIR representing distinct concerns. Both move to SharedKernel.Cultural.

| Path | Disposition |
|---|---|
| `Communications/Enums/SriLankanLanguage.cs` | **KEEP** (canonical narrow 3-value: Sinhala/Tamil/English; moves to SharedKernel.Cultural) |
| `Common/Enums/SouthAsianLanguage.cs` (19 values + SouthAsianLanguageExtensions static class) | **KEEP** (canonical broad scope for Phase 6 diaspora routing; moves to SharedKernel.Cultural with the extensions class) |
| `Common/Database/MultiLanguageRoutingModels.cs` line 19 (`enum SouthAsianLanguage`, 20 values with regional variants SriLankanTamil/IndianTamil/PakistaniUrdu/IndianUrdu + Arabic/Persian) | **DELETE** — duplicate of Common/Enums version. Per architect Q2 ruling: the regional variants (SriLankanTamil, IndianTamil, PakistaniUrdu, IndianUrdu) are additively promoted into the canonical 19-value enum (becomes 23 values). Arabic/Persian also promoted if used. |

The blueprint's instruction to "rename SouthAsianLanguage → SriLankanLanguage" was WRONG. Architect reviewed and confirmed they are distinct types.

---

## C. Reference site count

Per architect's grep (in blueprint §2.D2): **410 cross-module references** including migrations; **54 production code references** outside Communications.

Wave 2 namespace updates affect these 410 sites. Mechanical sed batches, verified per-batch.

---

## D. Execution order — architect-revised 2026-06-04 (MVP scope, ~15-20 sessions)

**Founder mandate** (per [[feedback-enterprise-framing-over-pragmatic]]) is "no compounding debt", but architect Q4 ruling: blueprint's 10-day estimate was wrong; honest scope is 30-50 hours plus dedupe risk. **Cut Wave 2 to ONLY the primitives W4.1.2 Communications-retry needs to compile cleanly.** Defer the rest to a Wave 2.5 backlog (executed during Wave 4 Communications module surgery itself, when callers are being touched anyway).

| Phase | Sub-wave | Action | Risk |
|---|---|---|---|
| 1 | W2A (this doc) | Inventory locked + architect-revised | DONE 2026-06-04 |
| 2 | W2B | SharedKernel.Cultural skeleton (already done W1D) | DONE |
| 3 | W2C.1 | Move `SriLankanLanguage` (3-value narrow). ~9 callers. **START HERE** | LOW |
| 4 | W2C.2 | Move `SouthAsianLanguage` + extensions class. Absorb 4 regional variants from MultiLanguageRoutingModels dupe. 31 callers. Delete the routing-models dup. | MEDIUM |
| 5 | W2C.3 | Move `ReligiousContext` (no dupes) | LOW |
| 6 | W2C.4 | Move `CulturalBackground` (canonical Communications); delete Shared + Common/Database dupes | MEDIUM |
| 7 | W2C.5 | Move `GeographicRegion` enum (Communications canonical); delete Common + Events enum dupes; RENAME `Billing/BillingSupportingTypes.cs` GeographicRegion class to `BillingRegion` per architect Q3 | HIGH (4 caller sets) |
| 8 | W2D.1 | Move the W4.1.2-blocking VOs ONLY: `CulturalContext` + `CulturalEvent` + `CulturalConflict` (Communications class versions) and dedupe their variants. These are the high-traffic VOs Communications-retry needs. | HIGH (most callers) |
| 9 | W2E | Move `ICulturalCalendarService` + `ICulturalAppropriatenessChecker` interfaces (impls stay until Wave 4) | LOW |
| 10 | W2G | Re-run W4.1.2 dry-run; verify `LankaConnect.Domain.Communications/*` has ZERO cross-module deps (excluding SharedKernel) | GATE |

**DEFERRED to Wave 2.5** (executed during Wave 4 Communications surgery as opportunistic cleanup; NOT bulk-moved during Wave 2):

- `CulturalAppropriateness`, `CulturalProfile`, `CulturalCalendarSync`, `CulturalTimingPreference`, `CrossCulturalEvent`, `DiasporaCommunityProfile`, `DiasporaRelevance`, `MultilingualContent`, `MultilingualDescription`, `RecipientCulturalProfile`, `MultiCulturalCommunity`, `GoogleCalendarCulturalEvent`, `TempleScheduleIntegration`
- `CulturalDataType`, `CulturalEventType`, `DiasporaEngagementType` enums (rarely-used outside Comm internals)

These don't block W4.1.2 — they extract naturally when Communications module's other files move in Wave 4. Adding them to Wave 2 would add 4-6 weeks for zero blocker reduction.

Each phase = 1 commit. Verify build + tests green after every commit.

---

## E. Out of scope for Wave 2

These types LOOK cultural but stay in their current location:

| File | Why it stays |
|---|---|
| `Billing/CulturalIntelligenceTier.cs` | Billing/payments-tier feature flag — moves to `Capabilities/Payments` or stays as a domain enum |
| `Billing/CulturalIntelligenceBilling.cs` | Billing handler — moves to `Capabilities/Payments` in Wave 4 |
| `Common/Database/Cultural*Models.cs` | EF mapping/POCO models — refactored when DbContext moves to per-Capability (Wave 4) |
| `Common/Monitoring/CulturalIntelligence*.cs` | Telemetry — moves to `Capabilities/CulturalIntelligence` in Wave 4 |
| `Common/Users/CulturalUserProfile.cs` | User-aggregate-attached — moves to `Capabilities/Identity` in Wave 4 |
| `CulturalIntelligence/*` subfolder | Feature implementation — moves to `Capabilities/CulturalIntelligence` in Wave 4 |
| `Enterprise/ValueObjects/CulturalDataAudit.cs` etc. | Enterprise-tier feature — moves to `Capabilities/Payments` (enterprise billing) in Wave 4 |
| `Events/CulturalInterestsUpdatedEvent.cs` | Domain event in Events context — moves to `Products/LankaEvents` in Wave 5 |
| `Events/Services/CulturalCalendar.cs` | Service impl — moves to `Capabilities/CulturalIntelligence` in Wave 4 |
| `Events/ValueObjects/CulturalDate.cs`, `CulturalPeriod.cs` | Events-specific helpers — stay until Wave 5 or move to Capabilities/Scheduling |
| `Infrastructure/Failover/Cultural*.cs`, `Infrastructure/Scaling/Cultural*.cs` | Operational HA code — stays/moves with the rest of Infrastructure in Wave 4 |
| `Communications/Services/Cultural*.cs`, `Diaspora*.cs`, `MultiCultural*.cs` | Service impls — interfaces move (W2E), impls stay until Wave 4 |
| `Users/ValueObjects/CulturalInterest.cs` | User-aggregate VO — moves to Capabilities/Identity in Wave 4 |

---

## F. Risk register

| Risk | Mitigation |
|---|---|
| Wave 2 cascade — moving a type breaks 50+ callers | Per-type commit; build verify between each |
| Duplicate-rename creates compile errors in unmigrated callers | Move CANONICAL first; delete duplicates LAST (W2F) |
| Cultural type movement breaks staging | Wave 2 is doc-side only — no behavior change; existing services keep working until Wave 4 |
| Architect's 17-VO estimate is off | Reality is 16 VOs in Communications/ValueObjects (plus duplicates elsewhere); CulturalEmailContext + MultiCulturalSupporting TBD during W2D |
| Bulk-sed introduces typos | Each batch limited to one type's callers; build verify catches |

---

## G. Gate to exit Wave 2

Run the playbook's diagnostic command against `LankaConnect.Domain.Communications/*`:

```bash
grep -rln "using LankaConnect.Domain.Communications" --include=*.cs src/Modules/Communications/ \
  | grep -v "/Communications.Domain/"  # exclude self-refs
```

Expected: ZERO output. Cultural types live in SharedKernel.Cultural; Communications module can now extract cleanly in Wave 4.
