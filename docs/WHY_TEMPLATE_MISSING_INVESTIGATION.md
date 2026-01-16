# WHY IS event-details TEMPLATE MISSING FROM DATABASE?

## DISCOVERY

✅ **Migration file EXISTS:** `20260113020400_Phase6A61_AddEventDetailsTemplate.cs`
✅ **Migration created:** January 13, 2026  
✅ **Template in migration:** Proper `event-details` template with correct variables
❌ **Template in database:** NOT FOUND (based on your screenshot)

## POSSIBLE REASONS

### 1. Migration Never Ran
**Check:** Query the migrations history table
```sql
SELECT * FROM "__EFMigrationsHistory" 
WHERE "MigrationId" = '20260113020400_Phase6A61_AddEventDetailsTemplate';
```

**If returns 0 rows:** Migration was never applied to staging database

### 2. Migration Ran But Failed Silently
**Check:** Look for errors in deployment logs around Jan 13
```bash
gh run list --created "2026-01-13" --limit 20
```

**Possible causes:**
- SQL syntax error in migration
- Table name mismatch (communications.email_templates vs EmailTemplates)
- Column name mismatch
- Transaction rollback

### 3. Database Was Restored from Old Backup
**Check:** Compare migration timestamps with database creation date
```sql
SELECT created_at FROM communications.email_templates ORDER BY created_at DESC LIMIT 5;
```

**If all templates are from before Jan 13:** Database was restored from backup taken before migration

### 4. Dual Migration Strategy Conflict (MOST LIKELY)
**Your deployment has TWO migration execution points:**

**Point A:** GitHub Actions (.github/workflows/deploy-staging.yml)
```yaml
- name: Run EF Migrations
  run: dotnet ef database update
```

**Point B:** Container Startup (src/LankaConnect.API/Program.cs)
```csharp
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await context.Database.MigrateAsync();
}
```

**Problem:** Race condition or rollback scenario
1. GitHub Actions runs migration successfully
2. Migration recorded in __EFMigrationsHistory
3. Container startup runs migration again
4. Transaction conflict or deadlock
5. Insert statement fails but migration already marked as applied
6. Result: Migration shows as "applied" but data not inserted

### 5. Schema Name Mismatch

**Migration uses:** `communications.email_templates`
```sql
INSERT INTO communications.email_templates (...)
```

**Your database might use:** `"EmailTemplates"` (without schema prefix)

**Check:**
```sql
-- Try both
SELECT * FROM communications.email_templates WHERE name = 'event-details';
SELECT * FROM "EmailTemplates" WHERE "Name" = 'event-details';
```

## DIAGNOSTIC QUERIES

Run these against your staging database:

```sql
-- 1. Check if migration was recorded
SELECT "MigrationId", "ProductVersion"
FROM "__EFMigrationsHistory"
WHERE "MigrationId" LIKE '%Event%Details%'
ORDER BY "MigrationId";

-- 2. Check table structure
SELECT table_schema, table_name 
FROM information_schema.tables 
WHERE table_name ILIKE '%email%template%';

-- 3. Check column names
SELECT column_name, data_type 
FROM information_schema.columns 
WHERE table_name ILIKE '%email%template%'
ORDER BY ordinal_position;

-- 4. List all template names
SELECT name FROM communications.email_templates;
-- OR
SELECT "Name" FROM "EmailTemplates";

-- 5. Check when templates were created
SELECT name, created_at 
FROM communications.email_templates 
ORDER BY created_at DESC;
```

## MOST LIKELY ROOT CAUSE

Based on the evidence:
1. ✅ Migration file exists with correct template
2. ❌ Template not in database
3. 🔄 Dual migration strategy in place

**Conclusion:** The migration probably RAN and was RECORDED in history, but the INSERT statement FAILED or was ROLLED BACK, leaving the template missing despite the migration showing as "applied".

**Why this happens:**
- Container startup migrations run AFTER GitHub Actions
- If container restarts during deployment, migrations run again
- PostgreSQL transactions can conflict
- EF Core sees migration already applied, skips Up() method
- But previous insert may have been rolled back

## THE FIX

**Option 1: Manual Insert (FASTEST - 5 minutes)**
Run the emergency SQL script to insert the template directly

**Option 2: Re-run Migration**
```bash
# Mark migration as not applied
DELETE FROM "__EFMigrationsHistory" 
WHERE "MigrationId" = '20260113020400_Phase6A61_AddEventDetailsTemplate';

# Re-run migrations
dotnet ef database update
```

**Option 3: Fix Dual Migration Strategy**
Remove one of the two migration points to prevent future issues

## VERIFICATION

After fix, verify template exists:
```sql
SELECT 
    name,
    description,
    subject_template,
    is_active,
    created_at
FROM communications.email_templates
WHERE name = 'event-details';
```

Should return 1 row with:
- name = 'event-details'
- is_active = true
- subject_template = '{{EventTitle}} - Event Details'
