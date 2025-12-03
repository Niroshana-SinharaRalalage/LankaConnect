# EventImages Missing Table - Issue Summary

**Date**: 2025-12-03
**Severity**: HIGH (Production Staging Impact)
**Status**: ROOT CAUSE IDENTIFIED - SOLUTION DOCUMENTED

---

## The Problem

Staging environment throwing 500 errors on event image uploads:

```
PostgresException: 42P01: relation "EventImages" does not exist
```

---

## Root Cause: Dual Migration Folder Architecture

### What Happened

Your codebase has **TWO migration folders** with migrations split between them:

```
src/LankaConnect.Infrastructure/
├── Migrations/                           ← OLD (17 migrations, INCLUDING AddEventImages)
│   └── 20251103040053_AddEventImages.cs ❌ NOT DEPLOYED TO STAGING
│
└── Data/
    └── Migrations/                       ← NEW (27 migrations, EXCLUDING AddEventImages)
        └── 20251109_AddNewsletterSubscribers.cs ✅ DEPLOYED TO STAGING
```

### Why EF Core Said "Database is Up to Date"

EF Core uses **reflection to discover migrations** and finds them in BOTH folders because:
- All migration files use the SAME namespace: `LankaConnect.Infrastructure.Data.Migrations`
- `MigrationsAssembly("LankaConnect.Infrastructure")` doesn't specify a subfolder
- EF Core scans the entire assembly

**When you ran `dotnet ef database update` locally:**
1. EF Core found migrations in BOTH folders via reflection
2. Checked `__EFMigrationsHistory` table
3. Saw all migrations from `Data/Migrations/` were applied
4. Incorrectly assumed `AddEventImages` was also applied
5. Reported "up to date" ❌ **BUT TABLE DOESN'T EXIST**

### Why Staging Deployment Failed

**Hypothesis**: Your deployment process (Azure App Service, CI/CD pipeline, or publish script) only packages files from `Data/Migrations/` folder.

**Result**:
- New migrations (in `Data/Migrations/`) ✅ Deployed
- Old migrations (in `Migrations/`) ❌ NOT Deployed
- `AddEventImages` migration ❌ NEVER APPLIED TO STAGING

---

## Timeline

| Date | Event | Impact |
|------|-------|--------|
| **Aug-Oct 2025** | Migrations created in `Migrations/` folder | Working in dev and staging |
| **Nov 3, 2025** | Commit f582356: Moved some migrations to `Data/Migrations/` | Dual folder structure created |
| **Nov 3, 2025** | `AddEventImages` created as LAST migration in old folder | Migration exists but in wrong location |
| **Nov 9+, 2025** | All new migrations created in `Data/Migrations/` only | Old folder abandoned |
| **Dec 3, 2025** | Image upload fails in staging with "table does not exist" | Issue discovered |

---

## The Fix (3 Steps)

### Step 1: Move Migration to Correct Folder (5 min)

```bash
# Copy migration to new location
cp "src\LankaConnect.Infrastructure\Migrations\20251103040053_AddEventImages.cs" `
   "src\LankaConnect.Infrastructure\Data\Migrations\20251103040053_AddEventImages.cs"

cp "src\LankaConnect.Infrastructure\Migrations\20251103040053_AddEventImages.Designer.cs" `
   "src\LankaConnect.Infrastructure\Data\Migrations\20251103040053_AddEventImages.Designer.cs"
```

### Step 2: Apply Migration to Staging Database (10 min)

**Generate SQL script:**
```bash
dotnet ef migrations script `
    20251102144315_AddEventCategoryAndTicketPrice `
    20251103040053_AddEventImages `
    --project src/LankaConnect.Infrastructure `
    --startup-project src/LankaConnect.API `
    --output apply_eventimages.sql `
    --idempotent
```

**Create backup:**
```bash
# Azure Portal: Backup > Backup now > Name: "pre-eventimages-migration-2025-12-03"
# OR pg_dump if you have direct access
```

**Apply to staging:**
```bash
psql -h your-staging-db.postgres.database.azure.com `
     -U your-admin-user `
     -d lankaconnect `
     -f apply_eventimages.sql
```

### Step 3: Verify and Deploy (10 min)

**Verify table:**
```sql
SELECT EXISTS (
    SELECT FROM information_schema.tables
    WHERE table_schema = 'events' AND table_name = 'EventImages'
) AS table_exists;
-- Expected: true
```

**Commit and deploy:**
```bash
git add "src/LankaConnect.Infrastructure/Data/Migrations/20251103040053_AddEventImages*"
git commit -m "fix: Move AddEventImages migration to Data/Migrations folder"
git push origin develop
# Deploy to staging via your normal process
```

---

## Verification Checklist

After applying the fix:

### Database Checks
- [ ] EventImages table exists in staging: `events."EventImages"`
- [ ] Table has 6 columns: Id, EventId, ImageUrl, BlobName, DisplayOrder, UploadedAt
- [ ] Table has 3 indexes: PK, IX_EventImages_EventId, IX_EventImages_EventId_DisplayOrder
- [ ] FK exists to events.events with CASCADE delete
- [ ] Migration recorded in `__EFMigrationsHistory`

### Application Checks
- [ ] Image upload returns 200 OK (not 500)
- [ ] Uploaded image appears in database
- [ ] Image URL is accessible (blob storage)
- [ ] No errors in Application Insights

### Code Checks
- [ ] Migration files exist in `Data/Migrations/`
- [ ] Code compiles successfully
- [ ] `dotnet ef migrations list` shows AddEventImages

---

## Detailed Documentation

**For step-by-step fix procedure:**
- See: `docs/EVENTIMAGES_STAGING_FIX_PROCEDURE.md` (55-minute guided process with checkpoints)

**For architectural analysis:**
- See: `docs/architecture/ADR-008-EventImages-Missing-Table-Root-Cause.md` (full root cause analysis)

**For migration folder structure:**
- See: `docs/architecture/MIGRATION_FOLDER_ARCHITECTURE.md` (visual guide and consolidation plan)

---

## Why This Matters

**Immediate Impact:**
- Image uploads broken in staging (user-facing feature)
- 500 errors in production logs
- Potential data loss if users retry uploads

**Systemic Risk:**
- Same issue could affect production deployment
- Other migrations in old folder may also be missing in staging
- Dual folder structure will cause future migration issues

**Long-Term Solution:**
- Consolidate ALL migrations to `Data/Migrations/`
- Update deployment process to verify migration folder
- Add CI/CD checks for migration consistency
- Document migration creation process

---

## Recommended Actions

### Immediate (Today)
1. ✅ Move AddEventImages migration to Data/Migrations/
2. ✅ Apply migration to staging database
3. ✅ Verify image uploads work
4. ✅ Deploy updated code to staging

### Short-Term (This Week)
1. Audit all migrations in old `Migrations/` folder
2. Verify which ones are applied in staging vs dev
3. Create plan to move remaining migrations
4. Update deployment scripts to check for dual folders

### Long-Term (Next Sprint)
1. Consolidate all migrations to single folder
2. Remove old `Migrations/` folder entirely
3. Add CI/CD validation for migration folder structure
4. Document migration creation process in CONTRIBUTING.md
5. Add database health checks for pending migrations

---

## Related Issues

This migration folder split may also explain:
- Any other "table does not exist" errors in staging
- Differences between dev and staging database schemas
- `dotnet ef database update` saying "up to date" incorrectly

**Audit Required**: Compare `__EFMigrationsHistory` between dev and staging to find all missing migrations.

---

## Questions Answered

### Q: Why does the migration say "up to date" when the table doesn't exist?

**A**: EF Core finds migrations via reflection (BOTH folders) but staging deployment only packages ONE folder. EF Core sees the migration class in local environment, but it's not deployed to staging.

### Q: How can I prevent this in the future?

**A**:
1. Keep ALL migrations in ONE folder (`Data/Migrations/`)
2. Add CI/CD check: Fail build if migrations exist in old folder
3. Add database health check: Warn if pending migrations detected
4. Document migration folder in contribution guidelines

### Q: Should I move all old migrations now?

**A**: Not immediately. Move AddEventImages first (urgent). Audit others to see which are missing. Create a plan to consolidate safely without disrupting active development.

### Q: Can I just delete the old Migrations folder?

**A**: NO - Not until you verify all migrations in it are applied to ALL environments (dev, staging, production). Deleting them could cause rollback issues.

---

## Contact for Issues

If you encounter issues during the fix:

**Before applying migration:**
- Review SQL script carefully
- Ensure backup is created
- Test in isolated environment if possible

**If migration fails:**
- Follow rollback procedure in EVENTIMAGES_STAGING_FIX_PROCEDURE.md
- Check database locks: `SELECT * FROM pg_stat_activity WHERE state = 'active'`
- Review PostgreSQL logs for detailed error

**If application breaks:**
- Rollback deployment to previous version
- Check Application Insights for exceptions
- Verify table schema matches expected structure

---

## Success Metrics

You'll know the fix worked when:

1. ✅ EventImages table exists in staging database
2. ✅ POST /api/events/{id}/images returns 200 OK
3. ✅ No "relation EventImages does not exist" errors in logs
4. ✅ Images appear in event detail pages
5. ✅ All database verification queries pass

---

## Appendix: Quick Commands

**Check if table exists (staging):**
```sql
SELECT EXISTS (
    SELECT FROM information_schema.tables
    WHERE table_schema = 'events' AND table_name = 'EventImages'
);
```

**Check if migration is applied (staging):**
```sql
SELECT COUNT(*) FROM "__EFMigrationsHistory"
WHERE "MigrationId" = '20251103040053_AddEventImages';
```

**Generate migration SQL:**
```bash
dotnet ef migrations script 20251102144315_AddEventCategoryAndTicketPrice 20251103040053_AddEventImages \
    --project src/LankaConnect.Infrastructure \
    --startup-project src/LankaConnect.API \
    --output apply_eventimages.sql \
    --idempotent
```

**Test image upload:**
```bash
curl -X POST "https://your-staging-app.azurewebsites.net/api/events/{eventId}/images" \
     -H "Authorization: Bearer {token}" \
     -F "file=@test.jpg" \
     -F "displayOrder=1"
```
