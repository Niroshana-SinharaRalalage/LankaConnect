# ADR-008: Cultural Types Live in SharedKernel.Cultural

| | |
|---|---|
| **Status** | Accepted (2026-06-04) |
| **Date** | 2026-06-04 |
| **Decision Owner** | Niroshana (Founder/Architect) |
| **Reviewers** | system-architect agent |
| **Supersedes** | Cultural types in `LankaConnect.Domain.Communications.ValueObjects/` (their original W3.9-era placement) |
| **Related** | [ENTERPRISE_ARCHITECTURE_BLUEPRINT.md](./ENTERPRISE_ARCHITECTURE_BLUEPRINT.md) §2.D2; ADR-006 (5-layer topology); W4.1.2 BLOCKED root cause |

## Context

The W4.1.2 Communications module extraction failed twice (2026-06-04) with cross-module type-coupling cycles. Root cause: 54 cultural types (`CulturalContext`, `CulturalEvent`, `CulturalAppropriateness`, `CulturalConflict`, plus 17+ enums and supporting VOs) live in `LankaConnect.Domain.Communications.*` but have 410 references from OUTSIDE Communications — including `LankaConnect.Domain.Event.cs`, `Common/Monitoring/`, `Common/Security/`, `Shared/BackupTypes.cs`, `Billing/CulturalIntelligenceBilling.cs`, and more.

Moving the cultural types into a new `Modules.Communications.Domain` assembly creates a cycle: legacy `LankaConnect.Domain` references the moved types, but `Modules.Communications.Domain` references legacy `BaseEntity` and `Result<T>`. Neither direction can be removed without further untangling.

The pattern repeats for every future capability extraction: Communications (now), Media (likely), Events (definitely — cultural touches Event aggregates), Identity (cultural touches user profile).

Cultural is a **cross-cutting domain concern**, not a single module's concern.

## Decision

**Cultural types are promoted to a new `SharedKernel.Cultural` project** (Layer 2 per ADR-006). All capabilities and products that need cultural awareness reference `SharedKernel.Cultural`.

### Scope of `SharedKernel.Cultural`

**Value Objects** (move from `LankaConnect.Domain.Communications.ValueObjects/`):

CulturalContext, CulturalEvent, CulturalAppropriateness, CulturalConflict, CulturalProfile, CulturalCalendarSync, CulturalTimingPreference, CrossCulturalEvent, DiasporaCommunityProfile, DiasporaRelevance, MultilingualContent, MultilingualDescription, RecipientCulturalProfile, MultiCulturalCommunity, MultiCulturalSupporting, GoogleCalendarCulturalEvent, TempleScheduleIntegration.

**Enums** (move from `LankaConnect.Domain.Common.Enums/` and `Domain/Shared/Enums/`):

SriLankanLanguage (renamed from SouthAsianLanguage), GeographicRegion, CulturalDataType, CulturalEventType, DiasporaEngagementType, CulturalBackground, ReligiousContext, CulturalPriority.

**Service interfaces** (interface only — implementations in NEW `Capabilities/CulturalIntelligence`):

`ICulturalCalendarService`, `ICulturalAppropriatenessChecker`.

### What does NOT live in SharedKernel.Cultural

- Implementations of `ICulturalCalendarService` (require external API queries — Google Calendar, religious calendar feeds — that's behavior, not a primitive)
- `CulturalIntelligenceBackupStatus` (service-internal to CulturalIntelligence capability)
- `SynchronizationPriority` (service-internal)

These live in `Capabilities/CulturalIntelligence`.

### Namespace

`LankaConnect.SharedKernel.Cultural` (NOT `LankaConnect.BuildingBlocks.Cultural`). Cultural is LankaConnect-business-specific (it knows about SriLankanLanguage); BuildingBlocks must remain framework-agnostic and reusable for any future Anthropic project.

## ArchTest Rule Specification

```csharp
[Fact]
public void SharedKernel_Cultural_DoesNotReferenceCapabilitiesOrProducts() {
    var result = Types.InAssembly(typeof(SharedKernelCulturalMarker).Assembly)
        .Should()
        .NotHaveDependencyOnAny("LankaConnect.Capabilities", "LankaConnect.Products", "LankaConnect.Domain")
        .GetResult();
    Assert.True(result.IsSuccessful);
}

[Fact]
public void Cultural_Types_LiveInSharedKernel_NotElsewhere() {
    var prohibitedAssemblies = new[] { "LankaConnect.Domain", "LankaConnect.Application" };
    foreach (var asm in prohibitedAssemblies) {
        var hits = Types.InAssembly(Assembly.Load(asm))
            .That()
            .HaveNameMatching("Cultural.*")
            .GetTypes();
        Assert.Empty(hits);  // post-Wave-2: zero cultural types in legacy
    }
}
```

## Migration Path (Wave 2)

Per blueprint §3 Wave 2 (~2 weeks):

1. **W2A** — Inventory: produce `docs/architecture/cultural-type-inventory.md` (54 types, 410 references)
2. **W2B** — Skeleton: create `src/SharedKernel/SharedKernel.Cultural/` csproj + namespace + AssemblyMarker
3. **W2C** — Move Enums (8 enums, ~120 caller updates)
4. **W2D** — Move ValueObjects (17 VOs, ~290 caller updates via sed)
5. **W2E** — Move Service interfaces (2 interfaces; implementations stay in legacy `LankaConnect.Infrastructure` for now, extracted to `Capabilities/CulturalIntelligence` in Wave 4)
6. **W2F** — Delete duplicate types: `ReportFormat` (enum + class dupe), dead `PerformanceObjective`, `Domain/Shared/Currency.cs` enum
7. **W2G** — Verify: re-run W4.1.2 dry-run against Communications domain; cross-module reference count from `LankaConnect.Domain.Communications/*` (excluding SharedKernel types) MUST be ZERO

## Consequences

### Positive

- W4.1.2 Communications extraction unblocked (Wave 4 work proceeds without cycles)
- Future capability extractions (Media, Events, Identity) similarly unblocked
- Single source of truth for cultural primitives across all 7+ future products
- Cultural calendar service can be swapped (Google Calendar → religious-calendar-feed) without touching consumers

### Negative / Trade-offs

- 2 weeks of dedicated untangling work (Wave 2) before any module extraction proceeds
- 410 reference sites need namespace updates (mechanical, sed-driven)
- New `Capabilities/CulturalIntelligence` capability project (small but adds to solution surface)

### Risks

- Risk: hidden cultural type referenced from unexpected legacy code surfaces during W2G verify → mitigated by W2A inventory's grep-all-callers step
- Risk: W2D bulk sed updates introduce subtle bugs in `using` statements → mitigated by per-batch build + test after each move
- Risk: SriLankanLanguage rename breaks existing migrations → mitigated by keeping enum value strings identical (only namespace + class name change)

## Status Update Log

- 2026-06-04: Accepted by founder after W4.1.2 BLOCKED root-cause analysis confirmed cross-cutting nature of cultural types.
