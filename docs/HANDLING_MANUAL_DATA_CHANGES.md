# Handling Manual Data Changes for Production Migration

**Issue**: Manual updates to staging database (e.g., email template HTML) won't automatically transfer to production.

**Impact**: Email templates, reference data, or other manual edits exist only in staging DB.

---

## The Problem

### What Happens with EF Core Migrations:
1. ✅ Migrations run SQL code **in the .cs files**
2. ✅ Reference data **in migrations** is seeded in production
3. ❌ Manual database edits **outside migrations** are NOT transferred
4. ❌ Production will have **original migration data**, not your manual updates

### Example:
If you manually updated `email_templates.html_template` in staging:
- **Staging**: Has your updated HTML
- **Production**: Will have the original HTML from the migration
- **Result**: Production templates will be outdated!

---

## Solution: 3 Options

### Option 1: Create a New Migration (RECOMMENDED) ✅

**Best for**: Changes you want to keep permanently

#### Steps:

**1. Document what you manually changed in staging:**
```sql
-- Example: Get the current templates you modified
SELECT
    name,
    LENGTH(html_template) as html_length,
    LENGTH(text_template) as text_length,
    updated_at
FROM communications.email_templates
ORDER BY updated_at DESC;
```

**2. Create a new migration to apply those changes:**
```bash
cd c:/Work/LankaConnect/src/LankaConnect.API
dotnet ef migrations add Phase6A76_UpdateEmailTemplatesWithManualChanges --project ../LankaConnect.Infrastructure
```

**3. Edit the migration file to include your manual changes:**
```csharp
public partial class Phase6A76_UpdateEmailTemplatesWithManualChanges : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
            UPDATE communications.email_templates
            SET
                html_template = '<html>YOUR UPDATED HTML HERE</html>',
                text_template = 'YOUR UPDATED TEXT HERE',
                updated_at = NOW()
            WHERE name = 'newsletter';
        ");

        // Repeat for each template you manually updated
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Optionally revert to original
    }
}
```

**4. Test in staging:**
```bash
dotnet ef database update --connection "$STAGING_DB"
```

**5. Commit and deploy:**
```bash
git add .
git commit -m "feat: Update email templates with manual changes"
git push
```

---

### Option 2: Export Manual Changes as SQL Script

**Best for**: Quick fix or one-time data transfer

#### Steps:

**1. Export the manually changed data from staging:**
```sql
-- Create a SQL script with your manual changes
SELECT
    'UPDATE communications.email_templates SET html_template = ' || quote_literal(html_template) ||
    ', text_template = ' || quote_literal(text_template) ||
    ', updated_at = NOW() WHERE name = ' || quote_literal(name) || ';'
FROM communications.email_templates
WHERE updated_at > '2026-01-20'  -- Adjust date to when you made manual changes
ORDER BY name;
```

**2. Save output to file:**
```bash
# Save to: manual_email_template_updates.sql
```

**3. Run script against production AFTER migrations:**
```bash
# After migrations complete in production
psql "$PROD_DB_CONNECTION" -f manual_email_template_updates.sql
```

---

### Option 3: Use Data Seeder in Application Startup

**Best for**: Dynamic data that changes frequently

#### Steps:

**1. Create a data seeder class:**
```csharp
// src/LankaConnect.Infrastructure/Data/Seeders/EmailTemplateSeed.cs
public class EmailTemplateSeeder
{
    public static async Task SeedAsync(AppDbContext context)
    {
        var newsletterTemplate = await context.EmailTemplates
            .FirstOrDefaultAsync(t => t.Name == "newsletter");

        if (newsletterTemplate != null)
        {
            newsletterTemplate.HtmlTemplate = @"
                <!-- YOUR UPDATED HTML HERE -->
            ";
            newsletterTemplate.TextTemplate = @"
                YOUR UPDATED TEXT HERE
            ";
            newsletterTemplate.UpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();
        }
    }
}
```

**2. Call seeder in Program.cs:**
```csharp
// In Program.cs after app.Run()
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await EmailTemplateSeeder.SeedAsync(context);
}
```

**Pros**: Updates apply automatically on app startup
**Cons**: Runs every time app starts (add check to avoid redundant updates)

---

## Recommended Workflow

### For Your Situation (Email Template HTML Updates):

**Use Option 1 - Create Migration** ✅

This is the **cleanest, most maintainable** approach:

1. **Document what you changed**: List all templates you manually updated
2. **Create migration**: Captures changes in version control
3. **Test in staging**: Verify migration works
4. **Deploy to production**: Changes apply automatically

---

## How to Find What You Manually Changed

### Query Staging Database:

```sql
-- Find recently updated email templates
SELECT
    name,
    type,
    category,
    updated_at,
    LENGTH(html_template) as html_length,
    LENGTH(text_template) as text_length
FROM communications.email_templates
WHERE updated_at IS NOT NULL
ORDER BY updated_at DESC;

-- Find templates created/updated after a specific date
SELECT name, created_at, updated_at
FROM communications.email_templates
WHERE updated_at > '2026-01-15'  -- Adjust to when you started manual changes
ORDER BY updated_at DESC;
```

### Export Full Template Content:

```sql
-- Export specific template
SELECT
    name,
    html_template,
    text_template
FROM communications.email_templates
WHERE name = 'newsletter';
```

---

## Step-by-Step: Create Migration for Your Manual Changes

### 1. Identify What You Changed

List all templates you manually edited:
- [ ] `newsletter`
- [ ] `registration-confirmation`
- [ ] `event-details`
- [ ] Others?

### 2. Export Current Values from Staging

For each template, get:
- `name`
- `html_template` (full HTML)
- `text_template` (full text)
- `subject_template` (if changed)

### 3. Create Migration

```bash
cd src/LankaConnect.API
dotnet ef migrations add Phase6A76_ApplyManualEmailTemplateUpdates \
  --project ../LankaConnect.Infrastructure \
  --context AppDbContext
```

### 4. Edit Migration File

```csharp
public partial class Phase6A76_ApplyManualEmailTemplateUpdates : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Update newsletter template (example)
        migrationBuilder.Sql(@"
            UPDATE communications.email_templates
            SET
                html_template = '<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"">
    <style>
        /* YOUR STYLES */
    </style>
</head>
<body>
    <!-- YOUR HTML -->
</body>
</html>',
                text_template = 'YOUR TEXT VERSION',
                updated_at = NOW()
            WHERE name = 'newsletter';
        ");

        // Repeat for each template you modified
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Optional: Revert to original
    }
}
```

### 5. Test Migration

```bash
# Test in local dev
dotnet ef database update

# Or test in staging
DB_CONNECTION="Host=lankaconnect-staging-db.postgres.database.azure.com;..."
dotnet ef database update --connection "$DB_CONNECTION"
```

### 6. Commit and Deploy

```bash
git add .
git commit -m "feat(templates): Apply manual email template HTML updates

Updated email templates with manually refined HTML/text content:
- newsletter: Enhanced styling and layout
- event-details: Added organizer contact section
- [list other changes]

These changes were manually tested in staging and are now
captured in a migration for production deployment."

git push origin develop
# Then merge to main for production
```

---

## Quick Checklist

Before deploying to production:

- [ ] List all manual database changes made in staging
- [ ] Choose approach (Migration recommended)
- [ ] Create migration with manual changes
- [ ] Test migration in staging
- [ ] Verify templates render correctly
- [ ] Commit migration to repository
- [ ] Deploy to production
- [ ] Verify templates in production match staging

---

## Tools to Help Export Data

### Tool 1: Use Azure Data Studio

1. Connect to staging database
2. Run query to get templates
3. Export results to JSON/CSV
4. Convert to migration SQL

### Tool 2: Use pgAdmin

1. Connect to staging PostgreSQL
2. Right-click table → View/Edit Data
3. Export selected rows
4. Generate INSERT/UPDATE statements

### Tool 3: Use psql Command Line

```bash
psql "$STAGING_DB" -c "
COPY (
    SELECT name, html_template, text_template
    FROM communications.email_templates
    WHERE updated_at > '2026-01-15'
) TO STDOUT WITH CSV HEADER;" > email_templates_export.csv
```

---

## Summary

| Approach | Pros | Cons | Use When |
|----------|------|------|----------|
| **Option 1: Migration** | ✅ Version controlled<br>✅ Automatic deployment<br>✅ Testable | Requires migration creation | **Permanent changes** |
| **Option 2: SQL Script** | ✅ Quick<br>✅ Simple | ❌ Manual execution<br>❌ Not in version control | One-time fixes |
| **Option 3: App Seeder** | ✅ Automatic on startup | ❌ Runs every time<br>❌ Performance impact | Frequent updates |

**Recommendation**: **Use Option 1 (Migration)** for your email template HTML updates.

---

## Next Steps

1. **Tell me which templates you manually updated** (names)
2. **I'll help you create a migration** with those changes
3. **We'll test it in staging** to verify
4. **Then deploy to production** with confidence

Would you like me to help you create the migration for your manual email template changes?
