# Phase 6A.47 Seed Data - Execution Instructions

## Problem Identified
The check constraint `ck_reference_values_enum_type` is blocking inserts for enum types beyond the original three (EventCategory, EventStatus, UserRole). This constraint must be dropped before seeding.

## Connection String
```
Host=lankaconnect-staging-db.postgres.database.azure.com;
Database=LankaConnectDB;
Username=adminuser;
Password=1qaz!QAZ;
SslMode=Require;
```

## Execution Steps

### Step 1: Drop the blocking constraint
Execute the file: `c:\Work\LankaConnect\scripts\01_drop_constraint.sql`

**Using psql CLI:**
```bash
psql "host=lankaconnect-staging-db.postgres.database.azure.com dbname=LankaConnectDB user=adminuser sslmode=require" -f c:/Work/LankaConnect/scripts/01_drop_constraint.sql
```

**Using Azure Cloud Shell:**
```bash
psql "host=lankaconnect-staging-db.postgres.database.azure.com dbname=LankaConnectDB user=adminuser password=1qaz!QAZ sslmode=require" -f 01_drop_constraint.sql
```

**Using pgAdmin or any SQL client:**
- Connect using the connection string above
- Open and execute `01_drop_constraint.sql`

**Expected output:**
```
ALTER TABLE
constraints_remaining
----------------------
                    0
```

### Step 2: Execute the seed script
Execute the file: `c:\Work\LankaConnect\scripts\seed_reference_data_hotfix.sql`

**Using psql CLI:**
```bash
psql "host=lankaconnect-staging-db.postgres.database.azure.com dbname=LankaConnectDB user=adminuser sslmode=require" -f c:/Work/LankaConnect/scripts/seed_reference_data_hotfix.sql
```

**Using Azure Cloud Shell:**
```bash
psql "host=lankaconnect-staging-db.postgres.database.azure.com dbname=LankaConnectDB user=adminuser password=1qaz!QAZ sslmode=require" -f seed_reference_data_hotfix.sql
```

**Using pgAdmin or any SQL client:**
- Connect using the connection string above
- Open and execute `seed_reference_data_hotfix.sql`

**Expected output:**
```
NOTICE:  Seeding reference data (402 rows across 41 enum types)...
NOTICE:  ✅ Successfully seeded 402 reference values across 41 enum types
```

### Step 3: Verify the data
Execute the file: `c:\Work\LankaConnect\scripts\02_verify_seed.sql`

**Using psql CLI:**
```bash
psql "host=lankaconnect-staging-db.postgres.database.azure.com dbname=LankaConnectDB user=adminuser sslmode=require" -f c:/Work/LankaConnect/scripts/02_verify_seed.sql
```

**Expected results:**
- `total_rows`: 402
- `distinct_enum_types`: 41
- Breakdown showing all 41 enum types with correct counts
- No duplicate rows

### Step 4: Test the API
```bash
curl https://lankaconnect-api-staging.azurewebsites.net/api/reference-data?types=EmailStatus
```

**Expected:** JSON array with 11 EmailStatus items

## Quick Copy-Paste Commands

**If you have psql installed locally:**
```bash
# Set password as environment variable (Windows PowerShell)
$env:PGPASSWORD="1qaz!QAZ"

# Execute all three steps
psql "host=lankaconnect-staging-db.postgres.database.azure.com dbname=LankaConnectDB user=adminuser sslmode=require" -f c:/Work/LankaConnect/scripts/01_drop_constraint.sql
psql "host=lankaconnect-staging-db.postgres.database.azure.com dbname=LankaConnectDB user=adminuser sslmode=require" -f c:/Work/LankaConnect/scripts/seed_reference_data_hotfix.sql
psql "host=lankaconnect-staging-db.postgres.database.azure.com dbname=LankaConnectDB user=adminuser sslmode=require" -f c:/Work/LankaConnect/scripts/02_verify_seed.sql
```

**If using Azure Cloud Shell:**
```bash
# Upload the three SQL files to Cloud Shell, then:
export PGPASSWORD="1qaz!QAZ"
psql "host=lankaconnect-staging-db.postgres.database.azure.com dbname=LankaConnectDB user=adminuser sslmode=require" -f 01_drop_constraint.sql
psql "host=lankaconnect-staging-db.postgres.database.azure.com dbname=LankaConnectDB user=adminuser sslmode=require" -f seed_reference_data_hotfix.sql
psql "host=lankaconnect-staging-db.postgres.database.azure.com dbname=LankaConnectDB user=adminuser sslmode=require" -f 02_verify_seed.sql
```

## Troubleshooting

### Error: "relation does not exist"
- The migration hasn't been applied yet. Run `dotnet ef database update` first.

### Error: "duplicate key value violates unique constraint"
- Data already exists. The script is idempotent, so this is safe to ignore.
- Or run: `DELETE FROM reference_data.reference_values;` then re-execute seed script.

### Error: Still getting check constraint violation
- The constraint drop didn't work. Verify by running:
  ```sql
  SELECT conname, contype, pg_get_constraintdef(oid)
  FROM pg_constraint
  WHERE conrelid = 'reference_data.reference_values'::regclass;
  ```
- If constraint still exists, try: `ALTER TABLE reference_data.reference_values DROP CONSTRAINT ck_reference_values_enum_type CASCADE;`

## Alternative: Direct SQL Execution

If you have a SQL client connected to the staging database, you can copy-paste the SQL directly:

1. **First, drop the constraint:**
   ```sql
   ALTER TABLE reference_data.reference_values DROP CONSTRAINT IF EXISTS ck_reference_values_enum_type;
   ```

2. **Then, execute the entire contents of:** `seed_reference_data_hotfix.sql`

3. **Finally, verify:**
   ```sql
   SELECT COUNT(*) FROM reference_data.reference_values; -- Should be 402
   SELECT COUNT(DISTINCT enum_type) FROM reference_data.reference_values; -- Should be 41
   ```
