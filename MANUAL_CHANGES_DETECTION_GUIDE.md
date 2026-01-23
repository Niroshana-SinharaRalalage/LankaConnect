# Manual Changes Detection Guide

Since we can't run psql directly, here are the exact queries to run to detect your manual changes.

---

## How to Run These Queries

### Option 1: Azure Portal
1. Go to Azure Portal → lankaconnect-staging-db
2. Click "Query editor" in left menu
3. Login with credentials
4. Run the queries below

### Option 2: Azure Data Studio
1. Open Azure Data Studio
2. Connect to: `lankaconnect-staging-db.postgres.database.azure.com`
3. Database: `LankaConnectDB`
4. User: `adminuser`
5. Password: `1qaz!QAZ`
6. Run the queries below

### Option 3: VS Code with PostgreSQL Extension
1. Install "PostgreSQL" extension
2. Connect to staging database
3. Run queries

---

## Query 1: Find Manually Modified Templates

```sql
-- This finds templates that were updated AFTER creation
-- (indicates manual editing)

SELECT
    name,
    type,
    category,
    created_at,
    updated_at,
    updated_at - created_at as time_since_creation,
    CASE
        WHEN updated_at > created_at + interval '1 minute' THEN 'MANUALLY EDITED'
        ELSE 'Original from migration'
    END as status
FROM communications.email_templates
WHERE updated_at IS NOT NULL
ORDER BY updated_at DESC;
```

**What to look for:**
- If `updated_at` is significantly AFTER `created_at` = Manual edit
- If `status` = 'MANUALLY EDITED' = You changed this template

---

## Query 2: Count All Reference Data

```sql
-- Verify reference data counts match expectations

SELECT
    'Metro Areas' as table_name,
    COUNT(*) as count,
    '22+' as expected
FROM events.metro_areas

UNION ALL

SELECT
    'Email Templates',
    COUNT(*),
    '15+'
FROM communications.email_templates

UNION ALL

SELECT
    'Reference Values',
    COUNT(*),
    '50+'
FROM reference_data.reference_values

UNION ALL

SELECT
    'State Tax Rates',
    COUNT(*),
    '51'
FROM reference_data.state_tax_rates;
```

**Expected results:**
- Metro Areas: 22+
- Email Templates: 15-20
- Reference Values: 50+
- State Tax Rates: 51

---

## Query 3: Export Manual Changes as SQL

```sql
-- This generates UPDATE statements for manually modified templates
-- Copy the output and use it to create a migration

SELECT
    '-- Manual change for: ' || name || E'\n' ||
    'UPDATE communications.email_templates SET' || E'\n' ||
    '  subject_template = ' || quote_literal(subject_template) || ',' || E'\n' ||
    '  html_template = ' || quote_literal(html_template) || ',' || E'\n' ||
    '  text_template = ' || quote_literal(text_template) || ',' || E'\n' ||
    '  updated_at = NOW()' || E'\n' ||
    'WHERE name = ' || quote_literal(name) || ';' || E'\n\n' as migration_sql
FROM communications.email_templates
WHERE updated_at IS NOT NULL
  AND updated_at > created_at + interval '1 minute'
ORDER BY name;
```

**What to do with output:**
- If output is empty = No manual changes!
- If output has SQL = Copy it to create a migration

---

## Query 4: List All Email Templates

```sql
-- Simple list of all templates in database

SELECT
    name,
    type,
    category,
    is_active
FROM communications.email_templates
ORDER BY name;
```

**Compare with migrations:**
Check if all these templates exist in migration files under:
`src/LankaConnect.Infrastructure/Data/Migrations/`

---

## Interpretation Guide

### Scenario A: No Manual Changes ✅

**Query 1 results:**
- All templates have `status` = 'Original from migration'
- OR all `updated_at` = `created_at` or NULL

**What this means:**
- ✅ Database matches migrations exactly
- ✅ Safe to deploy with existing migrations
- ✅ No extra work needed

**Action:** Proceed with deployment!

---

### Scenario B: Some Manual Changes ⚠️

**Query 1 results:**
- 1-5 templates have `status` = 'MANUALLY EDITED'
- `updated_at` is days/hours after `created_at`

**What this means:**
- You manually updated a few templates
- Those changes exist only in staging
- Production will get original versions

**Action:** Create a migration with Query 3 output

---

### Scenario C: Many Manual Changes ⚠️⚠️

**Query 1 results:**
- 6+ templates have `status` = 'MANUALLY EDITED'
- Most templates were edited

**What this means:**
- Significant manual work in staging
- Production will be very different

**Action Options:**
1. Create comprehensive migration with all changes
2. OR accept defaults and update production later
3. OR export entire `email_templates` table

---

## Decision Tree

```
Run Query 1
    ↓
Are there manually edited templates?
    ↓
NO → ✅ Deploy with existing migrations (done!)
    ↓
YES → How many?
    ↓
1-3 templates → Create small migration (15 min)
    ↓
4-10 templates → Create comprehensive migration (30 min)
    ↓
10+ templates → Consider accepting defaults + iterate (0 min)
```

---

## Creating Migration from Query 3 Output

If you find manual changes, here's how to create a migration:

### Step 1: Run Query 3 and copy output

### Step 2: Create migration file
```bash
cd src/LankaConnect.API
dotnet ef migrations add Phase6A77_ApplyManualEmailTemplateChanges \
  --project ../LankaConnect.Infrastructure
```

### Step 3: Edit the migration
Open the new file: `Phase6A77_ApplyManualEmailTemplateChanges.cs`

Replace the `Up` method with:
```csharp
protected override void Up(MigrationBuilder migrationBuilder)
{
    migrationBuilder.Sql(@"
        -- Paste Query 3 output here
        UPDATE communications.email_templates SET ...
    ");
}
```

### Step 4: Test
```bash
dotnet ef database update
```

### Step 5: Commit and deploy
```bash
git add .
git commit -m "feat: Apply manual email template changes from staging"
git push
```

---

## Quick Start: Run This First

**Start here:**
1. Open Azure Portal → lankaconnect-staging-db → Query editor
2. Run **Query 1** (Find Manually Modified Templates)
3. Check if any results have `status` = 'MANUALLY EDITED'

**Results:**
- **No results** → ✅ You're good! Deploy with existing migrations
- **Some results** → Run Query 3 and create migration
- **Many results** → Consider accepting defaults

---

## My Recommendation

Based on typical development patterns:

**Most likely scenario:**
- You have 0-3 manually edited templates
- These are cosmetic HTML/styling changes
- Not critical for launch

**Best action:**
1. Run Query 1 to confirm
2. If 0-3 changes → Accept defaults, update post-launch
3. If 4+ changes → Run Query 3, create migration

**Philosophy:**
> "Perfect is the enemy of done"

Get to production, iterate on polish.

---

## Summary

| Action | Tool | Time | Result |
|--------|------|------|--------|
| Run Query 1 | Azure Portal | 2 min | Know what changed |
| No changes | - | 0 min | Deploy now! ✅ |
| 1-3 changes | Query 3 + Migration | 15 min | Perfect match |
| 4+ changes | Accept defaults | 0 min | Ship & iterate |

**Start with Query 1** - everything else depends on that result.
