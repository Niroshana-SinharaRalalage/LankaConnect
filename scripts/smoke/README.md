# `scripts/smoke/` — Staging API Smoke Harness

> **Purpose**: enforce CLAUDE.md §13.2 ("Smoke must exercise the WRITE path that the
> commit's code is in") by providing four reusable scripts that satisfy the S1-S6
> smoke classes. Every commit's `S-class:` line in its commit message MUST name one
> or more of these scripts that will execute against staging post-deploy.
>
> **Founder constraint** (2026-06-07): no local Docker, no canary, no second
> database. Staging is the only test environment. These scripts are the
> dev-side replacement for local-Postgres dry-run, plus the post-deploy
> verification gate for the no-Docker discipline.

---

## The 4-script contract

| Script | Purpose | Smoke class |
|---|---|---|
| `Invoke-Login.ps1` | Authenticate against staging and export `$env:LC_BEARER` so subsequent scripts can call protected endpoints | Foundation (any S-class) |
| `Smoke-Mutator.ps1` | CREATE → re-fetch → assert audit fields → PATCH → re-fetch → assert `updatedAt > createdAt`. Generic harness for mutator commits | S2 (mutator), S3 (mapping change), S4 (new endpoint) |
| `Smoke-LogSilence.ps1` | Query Azure Container Apps logs for the last 60 seconds of a given endpoint and FAIL on any `42703` / `22P02` / `NpgsqlException` / `DatabaseConfigurationError` | Mandatory tail of every S3/S4/S5/S6 smoke |
| `Smoke-Probe.ps1` | Schema/column probe against staging Postgres via psql equivalent — confirms tables / columns / row-counts in expected post-migration shape | S5 (schema migration), S6 (module-context touch) |

---

## Authentication

`Invoke-Login.ps1` reads credentials from environment variables to avoid checking
them into the repo:

- `$env:LC_LOGIN_EMAIL` — default `niroshhh@gmail.com` (per `[[reference-staging-credentials]]`)
- `$env:LC_LOGIN_PASSWORD` — default `1qaz!QAZ`
- `$env:LC_STAGING_URL` — default `https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io`

On success it exports `$env:LC_BEARER` (the JWT) and `$env:LC_USER_ID` (for cross-check
in Smoke-Mutator assertions).

## Running a full smoke for a mutator commit

```powershell
# 1. Authenticate
pwsh scripts/smoke/Invoke-Login.ps1

# 2. Exercise the mutator
pwsh scripts/smoke/Smoke-Mutator.ps1 -Resource user -Mode UpdateLocation

# 3. Verify no Postgres / EF errors landed in container logs
pwsh scripts/smoke/Smoke-LogSilence.ps1 -Endpoint /api/users

# 4. If the commit involves schema, also probe
pwsh scripts/smoke/Smoke-Probe.ps1 -Schema identity -Table users
```

Each script:
- Returns exit code `0` on success, non-zero on failure (CI-compatible)
- Writes a single-line success summary to stdout for status reports
- Writes detailed assertion failures to stderr on non-zero exit

## When to use which script

| Commit type | Required scripts |
|---|---|
| Read-only refactor (S1) | `Invoke-Login` + `Smoke-Mutator -Mode ReadOnly` (just GET assertions) |
| Mutator refactor (S2) | `Invoke-Login` + `Smoke-Mutator -Mode <Create\|Update>` + `Smoke-LogSilence` |
| EF mapping change (S3) | `Invoke-Login` + `Smoke-Mutator` + `Smoke-LogSilence` (mandatory) |
| New endpoint (S4) | `Invoke-Login` + `Smoke-Mutator` full lifecycle + `Smoke-LogSilence` |
| Schema migration (S5) | `Smoke-Probe` pre + post + `Smoke-Mutator` + `Smoke-LogSilence` |
| Module-context pivot (S6) | `Invoke-Login` + `Smoke-Mutator` + `Smoke-Probe` + `Smoke-LogSilence` |

## CI integration

These scripts are also wired into `.github/workflows/deploy-staging.yml`'s
post-deploy verification step. A failing smoke aborts the deploy + alerts the
founder. See CLAUDE.md §13.4 for the full pre-deploy verification flow.

---

**Built**: Gap G0 (2026-06-08) per `docs/architecture/TESTING_DISCIPLINE_RULING.md` §B.
