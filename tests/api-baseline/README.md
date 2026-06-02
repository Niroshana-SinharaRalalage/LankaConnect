# API Baseline Regression — Phase A W2.8

Catches structural drift in the public API surface during Phase A module
extractions (W3+). The intent is **not** functional coverage — it's a tripwire
that fires the moment a path disappears, a verb on a shared path is removed,
or a DTO schema is renamed/dropped.

## How it works

`openapi-baseline.json` is the OpenAPI v3 document captured from staging on
**2026-06-02** (the day Phase A W2.6/2.7/2.8 closed) via
`GET /swagger/v1/swagger.json`. At capture: **312 paths, 403 schemas**.

`run-baseline-regression.sh` fetches the current document from staging
(or production with `--target prod`), then `jq`-diffs three sets:

| Diff dimension | Detection | Breaking? |
|---|---|---|
| paths in baseline missing from current | `comm -23` on sorted path lists | **yes** |
| verbs on a shared path removed | per-path `keys` diff | **yes** |
| schemas in baseline missing from current | `comm -23` on sorted schema lists | **yes** |
| paths/verbs/schemas added | `comm -13` (additive) | no |

Exit code `0` on no breaking drift; `1` on breaking drift; `2` on script error
(jq missing, target unreachable, etc.).

## Usage

```bash
# Diff against staging (default)
./run-baseline-regression.sh

# Diff against production
./run-baseline-regression.sh --target prod

# Refresh the baseline file — only after a DELIBERATE additive API change
# has already shipped to the target and you want subsequent runs to start
# from the new shape.
./run-baseline-regression.sh --refresh
```

Requires `jq` and `curl` on PATH.

## When to refresh the baseline

Refresh after:
- a Phase A module extraction lands and adds new endpoints to the public surface
- a deliberate additive contract change ships (new DTOs, new HTTP verbs on existing paths)

Do NOT refresh after:
- a refactor that "shouldn't change anything" — let the script confirm it didn't
- a breaking change — that's the bug the script is built to catch

## Known follow-ups

- **Field-level schema drift** is currently *not* detected. The script compares
  schema *names* only. A schema that gains a required field, loses a property,
  or changes a property type will pass. If that becomes a real problem during
  W3+ module extractions, extend the script to deep-diff `components.schemas`
  via `jq -r '.components.schemas | to_entries'` and a per-property comparator.
  Punted now because (a) we don't have a module extraction in flight yet and
  (b) deep diff produces a lot of noise that needs curation.

- The baseline includes a stale `info.version: "v1"` typo (`v` prefix) from the
  current Program.cs SwaggerDoc registration — harmless, but worth cleaning up
  next time someone touches Program.cs swagger config.

## History

| Date | Action | Notes |
|---|---|---|
| 2026-06-02 | initial capture | After `6308af3c` fixed the staging swagger 500 (ContentController.UploadImage `[FromForm] IFormFile` Swashbuckle bug). 312 paths / 403 schemas. |
