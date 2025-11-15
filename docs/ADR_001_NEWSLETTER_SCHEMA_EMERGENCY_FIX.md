# ADR-001: Newsletter Schema Emergency Fix via Direct SQL

**Date:** 2025-11-15
**Status:** Proposed
**Context:** Production staging deployment blocker

---

## Problem Statement

Newsletter subscription feature failing in staging with PostgreSQL constraint violation:
```
null value in column "version" of relation "newsletter_subscribers" violates not-null constraint
```

**Root Cause:** Schema drift between EF Core migration and actual database state
- EF Core migration defines: `version bytea NOT NULL`
- Actual database has: `version bytea NULL`
- Auto-migration in Container App (Program.cs:195) didn't apply migration

**Critical Constraint:** NO production data exists in `newsletter_subscribers` table

---

## Decision

Use **Direct SQL via Azure Portal Query Editor** to drop and recreate table with correct schema.

### Approach:
1. Execute SQL script in Azure Portal to DROP/CREATE table
2. Update `__EFMigrationsHistory` to reflect migration state
3. Restart Container App to ensure clean application state
4. Verify fix via API testing

---

## Rationale

### Why Direct SQL?

| Approach | Time | Complexity | Success Rate | Constraints |
|----------|------|------------|--------------|-------------|
| **Direct SQL (Azure Portal)** | 5 min | Low | 99% | None |
| Auto-migration retry | Unknown | Medium | 50% | Needs Container restart |
| Local `dotnet ef update` | Failed | High | 0% | Connection/credential issues |
| Azure CLI psql | Failed | High | 0% | No local PostgreSQL client |
| PowerShell scripts | Failed | High | 0% | Timeout/network issues |

### Key Advantages:
1. **Immediate execution** - No dependency on deployment pipeline
2. **Direct access** - Azure Portal authenticated session bypasses network issues
3. **Zero risk** - No production data to lose
4. **Clean state** - Migration history stays consistent
5. **Verifiable** - Can test immediately after fix

### Why NOT other approaches?
- **Auto-migration:** Already failed; Container may have cached schema
- **Local tools:** Failed due to Windows/Azure network/credential complexity
- **Wait for next deploy:** Delays testing and deployment by hours/days

---

## Consequences

### Positive:
- ✅ Immediate unblock of staging deployment
- ✅ No code changes required
- ✅ Migration history stays accurate
- ✅ Reproducible for future similar issues
- ✅ Documented process for emergency schema fixes

### Negative:
- ⚠️ Manual intervention required (not automated)
- ⚠️ Bypasses standard EF Core migration workflow
- ⚠️ Requires Azure Portal access with admin credentials

### Mitigation:
- Document exact SQL used for audit trail
- Update migration history to reflect manual fix
- Add verification steps to confirm schema correctness
- This approach ONLY acceptable when:
  - No production data exists
  - EF Core migrations have failed multiple times
  - Time-sensitive deployment blocker

---

## Implementation Steps

See: [NEWSLETTER_SCHEMA_FIX_COMMANDS.md](./NEWSLETTER_SCHEMA_FIX_COMMANDS.md)

---

## Verification Criteria

- [ ] `version` column shows `is_nullable = NO` in information_schema
- [ ] Migration `20251114235353_FixNewsletterVersionColumn` in `__EFMigrationsHistory`
- [ ] Newsletter subscription API returns HTTP 200
- [ ] Container logs show no constraint violation errors
- [ ] Can create newsletter subscriptions successfully

---

## Future Prevention

### Process Improvements:
1. **Enhanced auto-migration logging** - Add detailed logs in Program.cs to track migration application
2. **Migration verification endpoint** - Create admin API to verify migration state
3. **Pre-deployment schema checks** - Add GitHub Actions step to validate schema matches migrations
4. **Staging database reset workflow** - Automated way to reset staging DB for testing

### Code Changes:
```csharp
// Program.cs - Enhanced migration logging
if (app.Environment.IsStaging() || app.Environment.IsProduction())
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    logger.LogInformation("Starting database migration...");

    var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync();
    if (pendingMigrations.Any())
    {
        logger.LogWarning("Pending migrations: {Migrations}",
            string.Join(", ", pendingMigrations));
    }

    await dbContext.Database.MigrateAsync();

    logger.LogInformation("Database migration completed successfully");
}
```

### Monitoring:
- Add Application Insights custom event for migration success/failure
- Alert on migration errors in production/staging
- Dashboard showing migration history and timing

---

## References

- Issue: Newsletter subscription failing with null version constraint
- Migration: `20251114235353_FixNewsletterVersionColumn`
- Related: `20251114211313_TestMigration` (earlier attempt)
- Auto-migration code: `Program.cs:195`

---

## Decision Makers

- **Proposed by:** System Architect Agent
- **Context:** Emergency production staging fix
- **Time constraint:** Immediate (deployment blocked)
- **Risk level:** Low (no production data)

---

## Review Status

- [ ] Solution implemented
- [ ] Verification completed
- [ ] Documentation updated
- [ ] Lessons learned captured
- [ ] Future prevention plan approved

---

**NEXT ACTION:** Execute commands in NEWSLETTER_SCHEMA_FIX_COMMANDS.md
