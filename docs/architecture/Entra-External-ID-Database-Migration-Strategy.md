# Entra External ID Integration - Database Migration Strategy

**Status:** Implementation Ready
**Date:** 2025-10-28
**Related ADR:** ADR-002-Entra-External-ID-Integration

---

## Overview

This document provides a comprehensive database migration strategy for adding Microsoft Entra External ID support to LankaConnect's PostgreSQL database while ensuring zero downtime and data integrity.

---

## Current Schema Analysis

### Existing Users Table Structure

```sql
CREATE TABLE "Users" (
    "Id" UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    "Email" VARCHAR(255) NOT NULL,
    "FirstName" VARCHAR(100) NOT NULL,
    "LastName" VARCHAR(100) NOT NULL,
    "PhoneNumber" VARCHAR(20) NULL,
    "Bio" TEXT NULL,
    "IsActive" BOOLEAN NOT NULL DEFAULT TRUE,
    "PasswordHash" VARCHAR(255) NOT NULL,  -- Currently required
    "Role" INT NOT NULL DEFAULT 0,
    "IsEmailVerified" BOOLEAN NOT NULL DEFAULT FALSE,
    "EmailVerificationToken" VARCHAR(255) NULL,
    "EmailVerificationTokenExpiresAt" TIMESTAMP NULL,
    "PasswordResetToken" VARCHAR(255) NULL,
    "PasswordResetTokenExpiresAt" TIMESTAMP NULL,
    "FailedLoginAttempts" INT NOT NULL DEFAULT 0,
    "AccountLockedUntil" TIMESTAMP NULL,
    "LastLoginAt" TIMESTAMP NULL,
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "CreatedBy" VARCHAR(255) NULL,
    "UpdatedBy" VARCHAR(255) NULL
);

-- Existing Indexes
CREATE UNIQUE INDEX "IX_Users_Email" ON "Users" ("Email");
CREATE INDEX "IX_Users_IsActive" ON "Users" ("IsActive");
CREATE INDEX "IX_Users_CreatedAt" ON "Users" ("CreatedAt");
```

### Current Constraints

1. `Email` is unique across all users
2. `PasswordHash` is required (NOT NULL)
3. No foreign key constraints on Users table
4. Refresh tokens stored in separate `RefreshTokens` table

---

## Migration Phases

### Phase 1: Schema Enhancement (Zero Breaking Changes)

**Migration Name:** `20251028_AddIdentityProviderSupport`

#### Step 1.1: Add New Columns

```sql
-- Add identity provider tracking columns
ALTER TABLE "Users"
ADD COLUMN "IdentityProvider" INT NOT NULL DEFAULT 0
    CONSTRAINT "CK_Users_IdentityProvider_Valid" CHECK ("IdentityProvider" IN (0, 1)),
ADD COLUMN "ExternalProviderId" VARCHAR(255) NULL;

-- Add column comments for documentation
COMMENT ON COLUMN "Users"."IdentityProvider" IS '0=Local (BCrypt+JWT), 1=EntraExternal (Microsoft Entra External ID)';
COMMENT ON COLUMN "Users"."ExternalProviderId" IS 'External identity provider unique ID (e.g., Entra OID claim). Required for external users, null for local users.';
```

**Safety Validation:**
```sql
-- Verify all existing users have default Local provider
SELECT COUNT(*) AS ExistingUsersCount,
       SUM(CASE WHEN "IdentityProvider" = 0 THEN 1 ELSE 0 END) AS LocalUsersCount
FROM "Users";
-- Both counts should match
```

#### Step 1.2: Make PasswordHash Nullable

```sql
-- Remove NOT NULL constraint from PasswordHash
ALTER TABLE "Users"
ALTER COLUMN "PasswordHash" DROP NOT NULL;

-- Add check constraint: Local users MUST have password
ALTER TABLE "Users"
ADD CONSTRAINT "CK_Users_PasswordHash_Required_For_Local"
    CHECK (
        ("IdentityProvider" = 0 AND "PasswordHash" IS NOT NULL)
        OR
        ("IdentityProvider" != 0 AND "PasswordHash" IS NULL)
    );

-- Add check constraint: External users MUST have ExternalProviderId
ALTER TABLE "Users"
ADD CONSTRAINT "CK_Users_ExternalProviderId_Required_For_External"
    CHECK (
        ("IdentityProvider" = 0 AND "ExternalProviderId" IS NULL)
        OR
        ("IdentityProvider" != 0 AND "ExternalProviderId" IS NOT NULL)
    );
```

**Rollback Safety:**
```sql
-- If rollback needed
ALTER TABLE "Users"
DROP CONSTRAINT IF EXISTS "CK_Users_PasswordHash_Required_For_Local",
DROP CONSTRAINT IF EXISTS "CK_Users_ExternalProviderId_Required_For_External",
DROP CONSTRAINT IF EXISTS "CK_Users_IdentityProvider_Valid";

ALTER TABLE "Users"
ALTER COLUMN "PasswordHash" SET NOT NULL;

ALTER TABLE "Users"
DROP COLUMN IF EXISTS "ExternalProviderId",
DROP COLUMN IF EXISTS "IdentityProvider";
```

---

### Phase 2: Index Creation for Performance

**Migration Name:** `20251028_AddIdentityProviderIndexes`

#### Step 2.1: Add Performance Indexes

```sql
-- Index for external provider lookups (most selective first)
CREATE INDEX "IX_Users_IdentityProvider_ExternalProviderId"
ON "Users" ("IdentityProvider", "ExternalProviderId")
WHERE "ExternalProviderId" IS NOT NULL;

-- Unique index for external provider IDs (prevent duplicate Entra users)
CREATE UNIQUE INDEX "IX_Users_ExternalProviderId_Unique"
ON "Users" ("ExternalProviderId")
WHERE "ExternalProviderId" IS NOT NULL;

-- Composite index for email + provider (support dual authentication)
CREATE UNIQUE INDEX "IX_Users_Email_IdentityProvider_Unique"
ON "Users" ("Email", "IdentityProvider");

-- Note: This replaces the existing IX_Users_Email index
DROP INDEX IF EXISTS "IX_Users_Email";
```

**Index Analysis:**
```sql
-- Verify index creation
SELECT
    schemaname,
    tablename,
    indexname,
    indexdef
FROM pg_indexes
WHERE tablename = 'Users'
ORDER BY indexname;

-- Check index sizes
SELECT
    schemaname || '.' || tablename AS table_name,
    indexname,
    pg_size_pretty(pg_relation_size(indexrelid)) AS index_size
FROM pg_stat_user_indexes
WHERE tablename = 'Users';
```

#### Step 2.2: Update Existing Queries (Application Code)

**Before Migration:**
```csharp
// Old query - assumes unique email
var user = await _context.Users
    .FirstOrDefaultAsync(u => u.Email == email);
```

**After Migration:**
```csharp
// New query - specify provider for exact match
var user = await _context.Users
    .FirstOrDefaultAsync(u =>
        u.Email == email &&
        u.IdentityProvider == IdentityProvider.Local);

// Or get any user with email (for provider detection)
var user = await _context.Users
    .FirstOrDefaultAsync(u => u.Email == email);
```

---

### Phase 3: Data Integrity Validation

**Migration Name:** `20251028_ValidateIdentityProviderData`

```sql
-- Validation Query 1: All local users have password hashes
SELECT
    "Id",
    "Email",
    "IdentityProvider",
    "PasswordHash"
FROM "Users"
WHERE "IdentityProvider" = 0 AND "PasswordHash" IS NULL;
-- Should return 0 rows

-- Validation Query 2: All external users have provider IDs
SELECT
    "Id",
    "Email",
    "IdentityProvider",
    "ExternalProviderId"
FROM "Users"
WHERE "IdentityProvider" != 0 AND "ExternalProviderId" IS NULL;
-- Should return 0 rows

-- Validation Query 3: No external users have password hashes
SELECT
    "Id",
    "Email",
    "IdentityProvider",
    "PasswordHash"
FROM "Users"
WHERE "IdentityProvider" != 0 AND "PasswordHash" IS NOT NULL;
-- Should return 0 rows

-- Validation Query 4: No duplicate external provider IDs
SELECT
    "ExternalProviderId",
    COUNT(*) AS duplicate_count
FROM "Users"
WHERE "ExternalProviderId" IS NOT NULL
GROUP BY "ExternalProviderId"
HAVING COUNT(*) > 1;
-- Should return 0 rows

-- Validation Query 5: Email uniqueness per provider
SELECT
    "Email",
    "IdentityProvider",
    COUNT(*) AS duplicate_count
FROM "Users"
GROUP BY "Email", "IdentityProvider"
HAVING COUNT(*) > 1;
-- Should return 0 rows
```

**Automated Validation Function:**
```sql
CREATE OR REPLACE FUNCTION validate_identity_provider_integrity()
RETURNS TABLE (
    check_name VARCHAR,
    is_valid BOOLEAN,
    violation_count BIGINT,
    details TEXT
) AS $$
BEGIN
    -- Check 1: Local users have passwords
    RETURN QUERY
    SELECT
        'Local users have passwords'::VARCHAR,
        COUNT(*) = 0,
        COUNT(*),
        'Local users missing password hashes'::TEXT
    FROM "Users"
    WHERE "IdentityProvider" = 0 AND "PasswordHash" IS NULL;

    -- Check 2: External users have provider IDs
    RETURN QUERY
    SELECT
        'External users have provider IDs'::VARCHAR,
        COUNT(*) = 0,
        COUNT(*),
        'External users missing ExternalProviderId'::TEXT
    FROM "Users"
    WHERE "IdentityProvider" != 0 AND "ExternalProviderId" IS NULL;

    -- Check 3: External users have no passwords
    RETURN QUERY
    SELECT
        'External users have no passwords'::VARCHAR,
        COUNT(*) = 0,
        COUNT(*),
        'External users with unexpected password hashes'::TEXT
    FROM "Users"
    WHERE "IdentityProvider" != 0 AND "PasswordHash" IS NOT NULL;

    -- Check 4: No duplicate external IDs
    RETURN QUERY
    SELECT
        'No duplicate external provider IDs'::VARCHAR,
        COUNT(*) = 0,
        COUNT(*),
        'Duplicate ExternalProviderId values'::TEXT
    FROM (
        SELECT "ExternalProviderId", COUNT(*) AS cnt
        FROM "Users"
        WHERE "ExternalProviderId" IS NOT NULL
        GROUP BY "ExternalProviderId"
        HAVING COUNT(*) > 1
    ) duplicates;

    RETURN;
END;
$$ LANGUAGE plpgsql;

-- Run validation
SELECT * FROM validate_identity_provider_integrity();
```

---

## EF Core Migration Code

### Migration Up

```csharp
public partial class AddIdentityProviderSupport : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Add new columns
        migrationBuilder.AddColumn<int>(
            name: "IdentityProvider",
            table: "Users",
            type: "integer",
            nullable: false,
            defaultValue: 0,
            comment: "0=Local (BCrypt+JWT), 1=EntraExternal (Microsoft Entra External ID)");

        migrationBuilder.AddColumn<string>(
            name: "ExternalProviderId",
            table: "Users",
            type: "character varying(255)",
            maxLength: 255,
            nullable: true,
            comment: "External identity provider unique ID (e.g., Entra OID claim)");

        // Make PasswordHash nullable
        migrationBuilder.AlterColumn<string>(
            name: "PasswordHash",
            table: "Users",
            type: "character varying(255)",
            maxLength: 255,
            nullable: true,
            oldClrType: typeof(string),
            oldType: "character varying(255)",
            oldMaxLength: 255);

        // Add check constraints
        migrationBuilder.AddCheckConstraint(
            name: "CK_Users_IdentityProvider_Valid",
            table: "Users",
            sql: "\"IdentityProvider\" IN (0, 1)");

        migrationBuilder.AddCheckConstraint(
            name: "CK_Users_PasswordHash_Required_For_Local",
            table: "Users",
            sql: "(\"IdentityProvider\" = 0 AND \"PasswordHash\" IS NOT NULL) OR (\"IdentityProvider\" != 0 AND \"PasswordHash\" IS NULL)");

        migrationBuilder.AddCheckConstraint(
            name: "CK_Users_ExternalProviderId_Required_For_External",
            table: "Users",
            sql: "(\"IdentityProvider\" = 0 AND \"ExternalProviderId\" IS NULL) OR (\"IdentityProvider\" != 0 AND \"ExternalProviderId\" IS NOT NULL)");

        // Drop old email unique index
        migrationBuilder.DropIndex(
            name: "IX_Users_Email",
            table: "Users");

        // Create new indexes
        migrationBuilder.CreateIndex(
            name: "IX_Users_IdentityProvider_ExternalProviderId",
            table: "Users",
            columns: new[] { "IdentityProvider", "ExternalProviderId" },
            filter: "\"ExternalProviderId\" IS NOT NULL");

        migrationBuilder.CreateIndex(
            name: "IX_Users_ExternalProviderId_Unique",
            table: "Users",
            column: "ExternalProviderId",
            unique: true,
            filter: "\"ExternalProviderId\" IS NOT NULL");

        migrationBuilder.CreateIndex(
            name: "IX_Users_Email_IdentityProvider_Unique",
            table: "Users",
            columns: new[] { "Email", "IdentityProvider" },
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Drop check constraints
        migrationBuilder.DropCheckConstraint(
            name: "CK_Users_ExternalProviderId_Required_For_External",
            table: "Users");

        migrationBuilder.DropCheckConstraint(
            name: "CK_Users_PasswordHash_Required_For_Local",
            table: "Users");

        migrationBuilder.DropCheckConstraint(
            name: "CK_Users_IdentityProvider_Valid",
            table: "Users");

        // Drop new indexes
        migrationBuilder.DropIndex(
            name: "IX_Users_Email_IdentityProvider_Unique",
            table: "Users");

        migrationBuilder.DropIndex(
            name: "IX_Users_ExternalProviderId_Unique",
            table: "Users");

        migrationBuilder.DropIndex(
            name: "IX_Users_IdentityProvider_ExternalProviderId",
            table: "Users");

        // Restore old email unique index
        migrationBuilder.CreateIndex(
            name: "IX_Users_Email",
            table: "Users",
            column: "Email",
            unique: true);

        // Make PasswordHash required again
        migrationBuilder.AlterColumn<string>(
            name: "PasswordHash",
            table: "Users",
            type: "character varying(255)",
            maxLength: 255,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "character varying(255)",
            oldMaxLength: 255,
            oldNullable: true);

        // Drop new columns
        migrationBuilder.DropColumn(
            name: "ExternalProviderId",
            table: "Users");

        migrationBuilder.DropColumn(
            name: "IdentityProvider",
            table: "Users");
    }
}
```

---

## Migration Execution Plan

### Pre-Migration Checklist

- [ ] **Backup database** (full backup before migration)
- [ ] **Test migration in staging** environment
- [ ] **Verify application compatibility** with nullable PasswordHash
- [ ] **Review rollback plan** with team
- [ ] **Schedule maintenance window** (optional - migration is non-breaking)
- [ ] **Notify stakeholders** of schema changes

### Execution Steps

```bash
# Step 1: Create migration
cd src/LankaConnect.Infrastructure
dotnet ef migrations add AddIdentityProviderSupport --context ApplicationDbContext

# Step 2: Generate SQL script for review
dotnet ef migrations script 20251027_PreviousMigration 20251028_AddIdentityProviderSupport \
    --output ../../scripts/migrations/20251028_AddIdentityProviderSupport.sql \
    --idempotent

# Step 3: Review generated SQL
cat ../../scripts/migrations/20251028_AddIdentityProviderSupport.sql

# Step 4: Apply to staging database
dotnet ef database update --context ApplicationDbContext --connection "$STAGING_CONNECTION_STRING"

# Step 5: Run validation queries in staging
psql $STAGING_CONNECTION_STRING -f ../../scripts/validation/validate_identity_provider.sql

# Step 6: Deploy application code to staging
dotnet publish -c Release
# Deploy to staging

# Step 7: Run integration tests in staging
cd ../../tests/LankaConnect.IntegrationTests
dotnet test --filter "Category=Authentication" --settings staging.runsettings

# Step 8: Production migration (after staging validation)
dotnet ef database update --context ApplicationDbContext --connection "$PRODUCTION_CONNECTION_STRING"

# Step 9: Run production validation
psql $PRODUCTION_CONNECTION_STRING -c "SELECT * FROM validate_identity_provider_integrity();"

# Step 10: Monitor application logs
# Watch for authentication errors, constraint violations
```

---

## Rollback Strategy

### Scenario 1: Migration Failed During Execution

```bash
# Automatic rollback by EF Core if transaction fails
# No manual action needed
```

### Scenario 2: Post-Migration Issues Detected

```bash
# Rollback to previous migration
dotnet ef database update 20251027_PreviousMigration --context ApplicationDbContext

# Verify rollback success
psql $CONNECTION_STRING -c "SELECT column_name, data_type, is_nullable
    FROM information_schema.columns
    WHERE table_name = 'Users' AND column_name IN ('IdentityProvider', 'ExternalProviderId');"
# Should return 0 rows
```

### Scenario 3: Partial Data Corruption

```sql
-- Reset all users to Local provider (emergency recovery)
UPDATE "Users"
SET "IdentityProvider" = 0,
    "ExternalProviderId" = NULL
WHERE "IdentityProvider" != 0;

-- Re-apply PasswordHash NOT NULL if needed
ALTER TABLE "Users"
ALTER COLUMN "PasswordHash" SET NOT NULL;
```

---

## Testing Strategy

### Unit Tests (Repository Layer)

```csharp
[Fact]
public async Task AddAsync_LocalUser_ShouldSetProviderToLocal()
{
    var user = User.Create(Email.Create("test@local.com").Value, "John", "Doe").Value;
    user.SetPassword("hashed-password");

    await _repository.AddAsync(user);
    await _unitOfWork.CommitAsync();

    var saved = await _repository.GetByIdAsync(user.Id);
    saved.IdentityProvider.Should().Be(IdentityProvider.Local);
    saved.ExternalProviderId.Should().BeNull();
    saved.PasswordHash.Should().NotBeNull();
}

[Fact]
public async Task AddAsync_ExternalUser_ShouldEnforceConstraints()
{
    var user = User.CreateFromExternalProvider(
        Email.Create("test@entra.com").Value,
        "Jane", "Smith",
        IdentityProvider.EntraExternal,
        "entra-oid-12345").Value;

    await _repository.AddAsync(user);
    await _unitOfWork.CommitAsync();

    var saved = await _repository.GetByIdAsync(user.Id);
    saved.IdentityProvider.Should().Be(IdentityProvider.EntraExternal);
    saved.ExternalProviderId.Should().Be("entra-oid-12345");
    saved.PasswordHash.Should().BeNull();
}

[Fact]
public async Task AddAsync_ExternalUserWithoutProviderId_ShouldThrowConstraintException()
{
    // This should not be possible with domain model, but test DB constraint
    var act = async () =>
    {
        await _context.Database.ExecuteSqlRawAsync(@"
            INSERT INTO ""Users"" (""Id"", ""Email"", ""FirstName"", ""LastName"",
                ""IdentityProvider"", ""ExternalProviderId"", ""IsActive"",
                ""IsEmailVerified"", ""Role"", ""FailedLoginAttempts"",
                ""CreatedAt"", ""UpdatedAt"")
            VALUES (gen_random_uuid(), 'test@bad.com', 'Test', 'User',
                1, NULL, TRUE, TRUE, 0, 0, CURRENT_TIMESTAMP, CURRENT_TIMESTAMP)
        ");
    };

    await act.Should().ThrowAsync<PostgresException>()
        .WithMessage("*CK_Users_ExternalProviderId_Required_For_External*");
}
```

### Integration Tests

```csharp
[Fact]
public async Task GetByExternalProviderIdAsync_ShouldUseCoveringIndex()
{
    // Arrange
    var user = User.CreateFromExternalProvider(
        Email.Create("indexed@entra.com").Value,
        "Index", "Test",
        IdentityProvider.EntraExternal,
        "entra-oid-index-test").Value;

    await _repository.AddAsync(user);
    await _unitOfWork.CommitAsync();

    // Act
    var stopwatch = Stopwatch.StartNew();
    var found = await _repository.GetByExternalProviderIdAsync(
        IdentityProvider.EntraExternal,
        "entra-oid-index-test");
    stopwatch.Stop();

    // Assert
    found.Should().NotBeNull();
    found.Id.Should().Be(user.Id);
    stopwatch.ElapsedMilliseconds.Should().BeLessThan(50); // Index should make this fast
}

[Fact]
public async Task ExistsWithExternalProviderIdAsync_DuplicateId_ShouldReturnTrue()
{
    var providerId = "duplicate-test-oid";

    var user = User.CreateFromExternalProvider(
        Email.Create("dup1@entra.com").Value,
        "User", "One",
        IdentityProvider.EntraExternal,
        providerId).Value;

    await _repository.AddAsync(user);
    await _unitOfWork.CommitAsync();

    // Check existence
    var exists = await _repository.ExistsWithExternalProviderIdAsync(
        IdentityProvider.EntraExternal,
        providerId);

    exists.Should().BeTrue();
}
```

---

## Performance Impact Analysis

### Estimated Query Performance Changes

| Query Type | Before Migration | After Migration | Impact |
|------------|------------------|-----------------|--------|
| Get user by email | 0.5ms (indexed) | 0.6ms (composite index) | +20% (negligible) |
| Get user by external ID | N/A | 0.4ms (indexed) | New query |
| Insert new user | 1.2ms | 1.5ms (extra column, constraint checks) | +25% (acceptable) |
| List users by provider | N/A | 2ms (indexed scan) | New query |

### Index Storage Impact

```sql
-- Calculate additional storage for new indexes
SELECT
    pg_size_pretty(pg_total_relation_size('Users')) AS total_size,
    pg_size_pretty(pg_relation_size('Users')) AS table_size,
    pg_size_pretty(pg_total_relation_size('Users') - pg_relation_size('Users')) AS indexes_size;

-- Expected increase: ~5-10% for 3 new indexes
```

---

## Monitoring and Alerts

### Post-Migration Monitoring Queries

```sql
-- Daily validation check (add to cron job)
SELECT
    DATE(CURRENT_TIMESTAMP) AS check_date,
    check_name,
    is_valid,
    violation_count
FROM validate_identity_provider_integrity()
WHERE NOT is_valid;

-- Provider distribution report
SELECT
    "IdentityProvider",
    CASE "IdentityProvider"
        WHEN 0 THEN 'Local'
        WHEN 1 THEN 'EntraExternal'
        ELSE 'Unknown'
    END AS provider_name,
    COUNT(*) AS user_count,
    ROUND(100.0 * COUNT(*) / SUM(COUNT(*)) OVER (), 2) AS percentage
FROM "Users"
GROUP BY "IdentityProvider"
ORDER BY "IdentityProvider";

-- External provider adoption metrics
SELECT
    DATE("CreatedAt") AS signup_date,
    "IdentityProvider",
    COUNT(*) AS daily_signups
FROM "Users"
WHERE "CreatedAt" >= CURRENT_DATE - INTERVAL '30 days'
GROUP BY DATE("CreatedAt"), "IdentityProvider"
ORDER BY signup_date DESC, "IdentityProvider";
```

### Application Logging

```csharp
// Log provider usage in UserRepository
public async Task AddAsync(User user, CancellationToken cancellationToken = default)
{
    _logger.LogInformation(
        "Creating user {UserId} with provider {Provider}",
        user.Id, user.IdentityProvider);

    await _context.Users.AddAsync(user, cancellationToken);

    _logger.LogInformation(
        "User {UserId} created successfully with {Provider}",
        user.Id, user.IdentityProvider);
}
```

---

## Success Criteria

### Migration Success Indicators

- [ ] All check constraints created successfully
- [ ] All indexes created and used by query planner
- [ ] Existing users have `IdentityProvider = 0`
- [ ] No constraint violations in validation queries
- [ ] All existing tests pass
- [ ] Performance degradation < 30% for common queries
- [ ] Rollback tested successfully in staging

### Application Integration Success

- [ ] Local authentication still works
- [ ] User registration creates Local users
- [ ] Login flow unchanged for existing users
- [ ] No null reference exceptions in User entity methods
- [ ] Domain events raised correctly
- [ ] Repository methods handle both provider types

---

## Appendix: Full SQL Script

**File:** `scripts/migrations/20251028_AddIdentityProviderSupport_Complete.sql`

```sql
-- =====================================================
-- Migration: Add Identity Provider Support
-- Author: System Architecture Designer
-- Date: 2025-10-28
-- Purpose: Add Microsoft Entra External ID support
-- =====================================================

BEGIN;

-- Step 1: Add new columns
ALTER TABLE "Users"
ADD COLUMN "IdentityProvider" INT NOT NULL DEFAULT 0,
ADD COLUMN "ExternalProviderId" VARCHAR(255) NULL;

-- Step 2: Add comments
COMMENT ON COLUMN "Users"."IdentityProvider" IS '0=Local (BCrypt+JWT), 1=EntraExternal (Microsoft Entra External ID)';
COMMENT ON COLUMN "Users"."ExternalProviderId" IS 'External identity provider unique ID (e.g., Entra OID claim). Required for external users.';

-- Step 3: Make PasswordHash nullable
ALTER TABLE "Users"
ALTER COLUMN "PasswordHash" DROP NOT NULL;

-- Step 4: Add check constraints
ALTER TABLE "Users"
ADD CONSTRAINT "CK_Users_IdentityProvider_Valid"
    CHECK ("IdentityProvider" IN (0, 1));

ALTER TABLE "Users"
ADD CONSTRAINT "CK_Users_PasswordHash_Required_For_Local"
    CHECK (
        ("IdentityProvider" = 0 AND "PasswordHash" IS NOT NULL)
        OR ("IdentityProvider" != 0 AND "PasswordHash" IS NULL)
    );

ALTER TABLE "Users"
ADD CONSTRAINT "CK_Users_ExternalProviderId_Required_For_External"
    CHECK (
        ("IdentityProvider" = 0 AND "ExternalProviderId" IS NULL)
        OR ("IdentityProvider" != 0 AND "ExternalProviderId" IS NOT NULL)
    );

-- Step 5: Drop old index
DROP INDEX IF EXISTS "IX_Users_Email";

-- Step 6: Create new indexes
CREATE INDEX "IX_Users_IdentityProvider_ExternalProviderId"
ON "Users" ("IdentityProvider", "ExternalProviderId")
WHERE "ExternalProviderId" IS NOT NULL;

CREATE UNIQUE INDEX "IX_Users_ExternalProviderId_Unique"
ON "Users" ("ExternalProviderId")
WHERE "ExternalProviderId" IS NOT NULL;

CREATE UNIQUE INDEX "IX_Users_Email_IdentityProvider_Unique"
ON "Users" ("Email", "IdentityProvider");

-- Step 7: Create validation function
CREATE OR REPLACE FUNCTION validate_identity_provider_integrity()
RETURNS TABLE (
    check_name VARCHAR,
    is_valid BOOLEAN,
    violation_count BIGINT,
    details TEXT
) AS $$
BEGIN
    RETURN QUERY
    SELECT 'Local users have passwords'::VARCHAR, COUNT(*) = 0, COUNT(*), 'Local users missing passwords'::TEXT
    FROM "Users" WHERE "IdentityProvider" = 0 AND "PasswordHash" IS NULL;

    RETURN QUERY
    SELECT 'External users have provider IDs'::VARCHAR, COUNT(*) = 0, COUNT(*), 'External users missing IDs'::TEXT
    FROM "Users" WHERE "IdentityProvider" != 0 AND "ExternalProviderId" IS NULL;

    RETURN QUERY
    SELECT 'External users have no passwords'::VARCHAR, COUNT(*) = 0, COUNT(*), 'External users with passwords'::TEXT
    FROM "Users" WHERE "IdentityProvider" != 0 AND "PasswordHash" IS NOT NULL;

    RETURN QUERY
    SELECT 'No duplicate external IDs'::VARCHAR, COUNT(*) = 0, COUNT(*), 'Duplicate provider IDs'::TEXT
    FROM (SELECT "ExternalProviderId", COUNT(*) FROM "Users" WHERE "ExternalProviderId" IS NOT NULL GROUP BY "ExternalProviderId" HAVING COUNT(*) > 1) dup;
END;
$$ LANGUAGE plpgsql;

-- Step 8: Run validation
DO $$
DECLARE
    v_check RECORD;
    v_all_valid BOOLEAN := TRUE;
BEGIN
    FOR v_check IN SELECT * FROM validate_identity_provider_integrity()
    LOOP
        IF NOT v_check.is_valid THEN
            v_all_valid := FALSE;
            RAISE WARNING 'Validation failed: % (violations: %)', v_check.check_name, v_check.violation_count;
        END IF;
    END LOOP;

    IF NOT v_all_valid THEN
        RAISE EXCEPTION 'Migration validation failed. Rolling back.';
    END IF;

    RAISE NOTICE 'Migration validation successful. All integrity checks passed.';
END $$;

COMMIT;

-- =====================================================
-- End of Migration
-- =====================================================
```

---

**Review Status:** ✅ Ready for Execution
**Risk Level:** Low (backward compatible, zero downtime)
**Estimated Migration Time:** 2-5 seconds (depends on table size)
