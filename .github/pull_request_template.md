<!--
This template is required for ALL PRs after 2026-05-11.

For Phase A modular monolith refactor PRs:
  - Apply the `phase-a` label
  - Title format MUST be: `W#.#: <imperative summary>` (e.g., `W3.1: extract Notifications module skeleton`)
  - Allowed prefixes for non-task PRs: `Merge`, `Revert`, `[hotfix]`
  - The `phase-a` label is the activation switch for the PR-title gate (introduced by PR-B)

For operational / non-Phase-A PRs:
  - Leave the `phase-a` label OFF
  - Mark the "Operational (no Phase A scope)" checkbox under "PR type" below
  - Sections referencing Phase A tracking can be marked N/A

Plan reference: C:\Users\Niroshana\.claude\plans\yes-one-cart-per-streamed-rocket.md
Master TODO: docs/MASTER_TODO_PHASE_A_MODULAR_MONOLITH.md
-->

## Master TODO task
- **Task ID**: <!-- W#.# from MASTER_TODO_PHASE_A_MODULAR_MONOLITH.md, or N/A for operational -->
- **Playbook section**: <!-- link to PHASE_A_IMPLEMENTATION_PLAYBOOK.md anchor (W1 onwards), or N/A -->
- **PR type**:
  - [ ] Scaffolding (≤1000 LOC; module skeleton, ArchTest, DI extension, sln updates)
  - [ ] Behavior (≤400 LOC hard ceiling; functional/refactor change)
  - [ ] Operational (no Phase A scope)

## Summary
<!-- 1-3 sentences: what this PR changes and why -->

## Pass/Fail criteria from playbook
<!-- Paste evidence (curl output, SQL row count, dotnet test result) for each criterion -->
- [ ] Verification step 1: <evidence>
- [ ] Verification step 2: <evidence>
- [ ] Exit criteria observable on staging: <curl + response>

## Blast radius
<!-- For module-extraction PRs: paste grep results showing who calls the code being moved -->
<!-- For operational PRs: N/A -->

## Feature flag impact
- **New flag**: <name | none>
- **Default**: <false | n/a>
- **Cleanup-by date**: <W# | n/a>

## Rollback plan
<!-- Specific commands or steps -->
- <git revert hash | flag flip command | migration down-script confirmed>

## Architect review needed?
- [ ] **Yes** — module extraction, cleanup PR, or `point-of-no-return` label
- [ ] **No** — internal refactor within a module, or operational change

## Test plan
<!-- Bulleted checklist of what to verify (CI gates + manual smoke + staging API tests) -->
- [ ]
- [ ]
