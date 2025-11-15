# Newsletter Schema Fix - Production Commands

## Phase 1: Direct SQL Fix via Azure Portal

### Step 1: Open Azure Portal Query Editor
```
1. Go to: https://portal.azure.com
2. Navigate to: lankaconnect-staging-db (PostgreSQL server)
3. Click: "Query editor (preview)" in left sidebar
4. Login with admin credentials
```

### Step 2: Execute SQL Fix
```sql
-- Drop the problematic table (safe because no production data)
DROP TABLE IF EXISTS communications.newsletter_subscribers CASCADE;

-- Recreate with correct schema matching EF Core expectations
CREATE TABLE communications.newsletter_subscribers (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL,
    email VARCHAR(255) NOT NULL,
    receive_all_locations BOOLEAN NOT NULL DEFAULT false,
    receive_all_categories BOOLEAN NOT NULL DEFAULT false,
    preferred_locations JSONB,
    preferred_categories JSONB,
    is_active BOOLEAN NOT NULL DEFAULT true,
    subscribed_at TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    unsubscribed_at TIMESTAMP WITHOUT TIME ZONE,
    confirmation_token VARCHAR(255),
    confirmed_at TIMESTAMP WITHOUT TIME ZONE,
    version BYTEA NOT NULL DEFAULT '\x0000000000000001'::bytea,

    CONSTRAINT fk_newsletter_user
        FOREIGN KEY (user_id)
        REFERENCES identity.users(id)
        ON DELETE CASCADE
);

-- Create indexes for performance
CREATE INDEX idx_newsletter_subscribers_user_id
    ON communications.newsletter_subscribers(user_id);

CREATE INDEX idx_newsletter_subscribers_email
    ON communications.newsletter_subscribers(email);

CREATE INDEX idx_newsletter_subscribers_is_active
    ON communications.newsletter_subscribers(is_active);

-- Verify fix
SELECT column_name, data_type, is_nullable
FROM information_schema.columns
WHERE table_schema = 'communications'
  AND table_name = 'newsletter_subscribers'
  AND column_name = 'version';
```

**Expected verification output:**
```
column_name | data_type | is_nullable
------------|-----------|------------
version     | bytea     | NO
```

---

## Phase 2: Update Migration History (1 minute)

### Option A: If you want to keep migration history clean
```sql
-- Mark the migration as applied (if it exists in __EFMigrationsHistory)
INSERT INTO public."__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20251114235353_FixNewsletterVersionColumn', '9.0.0')
ON CONFLICT ("MigrationId") DO NOTHING;
```

### Option B: If you want EF to reapply on next deploy
```sql
-- Delete migration record to force reapplication
DELETE FROM public."__EFMigrationsHistory"
WHERE "MigrationId" = '20251114235353_FixNewsletterVersionColumn';
```

**RECOMMENDATION:** Use Option A to match your current migration state.

---

## Phase 3: Force Container Restart (2 minutes)

### Using Azure CLI (from Windows PowerShell):

```powershell
# Login to Azure (if not already logged in)
az login

# Set subscription (if multiple subscriptions)
az account set --subscription "your-subscription-name-or-id"

# Restart the Container App
az containerapp restart `
    --name lankaconnect-api-staging `
    --resource-group lankaconnect-staging-rg
```

### Alternative: Using Azure Portal
```
1. Go to: Azure Portal → Container Apps
2. Find: lankaconnect-api-staging
3. Click: "Restart" button in top toolbar
4. Wait: ~30-60 seconds for restart
```

---

## Phase 4: Verification (1 minute)

### Test 1: Check Database Schema
```sql
-- Should return is_nullable = 'NO'
SELECT column_name, data_type, is_nullable, column_default
FROM information_schema.columns
WHERE table_schema = 'communications'
  AND table_name = 'newsletter_subscribers';
```

### Test 2: Test Newsletter Subscription API
```powershell
# Get auth token first
$token = "your-jwt-token"

# Test subscription request
$body = @{
    receiveAllLocations = $true
    receiveAllCategories = $false
    preferredLocations = @()
    preferredCategories = @("Technology", "Business")
} | ConvertTo-Json

Invoke-RestMethod `
    -Uri "https://lankaconnect-api-staging.azurewebsites.net/api/newsletter/subscribe" `
    -Method POST `
    -Headers @{Authorization = "Bearer $token"} `
    -ContentType "application/json" `
    -Body $body
```

**Expected response:** HTTP 200 with subscription confirmation

### Test 3: Check Container Logs
```powershell
az containerapp logs show `
    --name lankaconnect-api-staging `
    --resource-group lankaconnect-staging-rg `
    --tail 50
```

**Look for:** No "null value in column 'version'" errors

---

## WHY THIS APPROACH IS BEST

### ✅ Advantages:
1. **No local tools required** - Azure Portal works from any browser
2. **Direct control** - SQL fixes schema immediately
3. **Safe operation** - No production data at risk
4. **Clean state** - Migration history stays consistent
5. **Immediate verification** - Can test in same session
6. **No network issues** - Portal has direct access to Azure resources

### ⚠️ Why other options failed:
- **Auto-migration didn't run:** Container may have cached old schema
- **Local migrations failed:** Connection string/network/credential issues
- **PowerShell scripts timeout:** Windows firewall/Azure network policies

### 🎯 This approach sidesteps all those issues:
- Uses authenticated Azure Portal session
- Direct SQL bypasses EF Core complexity
- Restart ensures clean application state
- Takes < 5 minutes total

---

## ROLLBACK PLAN (if needed)

If something goes wrong:

```sql
-- Restore to previous state
DROP TABLE IF EXISTS communications.newsletter_subscribers CASCADE;

-- Let EF Core recreate on next deployment
DELETE FROM public."__EFMigrationsHistory"
WHERE "MigrationId" LIKE '202511%Newsletter%';
```

Then redeploy via GitHub Actions to let EF migrations run fresh.

---

## SUCCESS CRITERIA

- [ ] Schema verification shows `version bytea NOT NULL`
- [ ] Migration history includes `FixNewsletterVersionColumn`
- [ ] Container app restarted successfully
- [ ] Newsletter subscription API returns HTTP 200
- [ ] No database errors in container logs
- [ ] Can create/update newsletter subscriptions without errors

---

## ESTIMATED TIME: 5 minutes total
- Phase 1 (SQL): 2 minutes
- Phase 2 (Migration): 1 minute
- Phase 3 (Restart): 2 minutes
- Phase 4 (Verify): 1 minute

**NEXT STEP:** Open Azure Portal and execute Phase 1 SQL script.
