# Architecture Decision Records (ADRs) — Canonical Home

> **All NEW architectural decisions land here.** This directory is the canonical home for Architecture Decision Records (ADRs) under the format established 2026-06-29.

## Format

Each ADR file is `ADR-NNN-<slug>.md` where `NNN` is a globally-unique zero-padded number. The next available number is `007` (ADR-001 through ADR-006 are the foundational Phase A ADRs migrated from `docs/architecture/ADR-NNN-*-phase-a.md`).

Each ADR contains five sections:

1. **Decision** — the choice made, in one sentence
2. **Context** — what triggered the need for a decision; the problem space
3. **Alternatives considered** — at least 2 alternatives with their trade-offs
4. **Final reasoning** — why the chosen alternative wins; what was traded away
5. **Consequences** — downstream effects, including any new debt or constraints introduced

This prevents future agents from re-opening resolved debates. "We already decided X because Y" is the single most valuable artifact in a multi-agent project.

## Authoring rules

- Pair with the **System Architect** persona (via SendMessage) before drafting any ADR — non-negotiable per `PLATFORM_MASTER_PLAN.md` §3.5
- Reference the ADR by its number in commit messages: `ADR-007 ratifies X`
- ADRs are append-only once accepted. To supersede an ADR, author a new one that references the old by number and declares it superseded — do not edit the original

## Migration history (2026-06-29)

Six foundational Phase A ADRs migrated from `docs/architecture/` to this directory with globally-unique renumbering:

| New ADR | Migrated from |
|---|---|
| ADR-001-i18n.md | ADR-001-i18n-scope-phase-a.md |
| ADR-002-layer-topology.md | ADR-006-layer-topology-phase-a.md |
| ADR-003-auditable-interceptor.md | ADR-007-auditable-interceptor-phase-a.md |
| ADR-004-cultural-shared-kernel.md | ADR-008-cultural-shared-kernel-phase-a.md |
| ADR-005-outbox-everything.md | ADR-009-outbox-everything-phase-a.md |
| ADR-006-repository-per-aggregate.md | ADR-010-repository-per-aggregate-phase-a.md |

The remaining 28 files in `docs/architecture/` were post-hoc incident analyses (root-cause reports, deployment runbooks, fix postmortems) that shared the `ADR-NNN-*` naming purely because they were authored using the ADR template by various agents. Multiple files used the same ADR number (e.g., 7 files all titled `ADR-007-*`), creating a collision that polluted cross-references.

Those 28 files were renamed in place from `ADR-NNN-<slug>.md` to `ANALYSIS-<slug>.md` to break the collision permanently and preserve their historical content. They are NOT architectural decisions; they are incident artifacts.

## Cross-references

- [PLATFORM_MASTER_PLAN.md](../../PLATFORM_MASTER_PLAN.md) — read first, every time
- [ENTERPRISE_ARCHITECTURE_BLUEPRINT.md](../ENTERPRISE_ARCHITECTURE_BLUEPRINT.md) — authoritative architecture; ADRs ratify specific decisions within this blueprint
- [docs/architect-consults/](../../architect-consults/) — append-only architect ruling log; an architect ruling that becomes durable architecture is promoted to an ADR here
