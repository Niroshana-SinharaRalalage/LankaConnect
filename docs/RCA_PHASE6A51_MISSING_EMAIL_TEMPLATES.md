# Root Cause Analysis: Phase 6A.51 Missing Email Templates

**Date**: 2026-01-19
**Severity**: High
**Impact**: Email notifications for signup commitment UPDATE and CANCEL operations not being sent to users
**Status**: Analysis Complete, Fix Proposed

---

## Executive Summary

**Problem**: Users are not receiving email notifications when they update or cancel their signup commitments, despite the domain events being raised and event handlers existing.

**Root Cause**: Email templates `signup-commitment-updated` and `signup-commitment-cancelled` were never applied to the staging database due to migration deployment issues.

**Fix**: Create a single idempotent migration to add both missing templates, similar to the Phase6A54Fix approach used for `signup-commitment-confirmation`.

---

## Timeline of Events

### Phase 6A.51 (Original Implementation)
- **Migration**: `20260118235411_Phase6A51_AddMissingSignupCommitmentEmailTemplates.cs`
- **Location**: `src\LankaConnect.Infrastructure\Migrations\` (separate directory)
- **Contents**: Adds TWO templates - `signup-commitment-updated` and `signup-commitment-cancelled`
- **Designer File**: EXISTS (`20260118235411_Phase6A51_AddMissingSignupCommitmentEmailTemplates.Designer.cs`)
- **Status**: Migration was created but NEVER APPLIED to staging database

### Phase 6A.54 (Original Implementation)
- **Migration**: `20251227232000_Phase6A54_SeedNewEmailTemplates.cs`
- **Location**: `src\LankaConnect.Infrastructure\Data\Migrations\`
- **Contents**: Adds FOUR templates including `signup-commitment-confirmation`
- **Designer File**: MISSING (critical issue)
- **Status**: Migration was SKIPPED by EF Core due to missing Designer file

### Phase 6A.54 Fix (Confirmation Template)
- **Migration**: `20260119154406_Phase6A54Fix_AddMissingEmailTemplates.cs`
- **Location**: `src\LankaConnect.Infrastructure\Data\Migrations\`
- **Contents**: Adds `signup-commitment-confirmation` with idempotent SQL
- **Result**: Commitment CREATION emails now work

### Current State
- ✅ **Commit email**: Works (template `signup-commitment-confirmation` exists after Phase6A54Fix)
- ❌ **Update email**: NOT sent (template `signup-commitment-updated` missing)
- ❌ **Cancel email**: NOT sent (template `signup-commitment-cancelled` missing)

---

## Detailed Root Cause Analysis

### 1. Migration Directory Confusion

**Issue**: Two separate migration directories exist in the codebase:
1. `src\LankaConnect.Infrastructure\Migrations\` - Used by Phase6A51
2. `src\LankaConnect.Infrastructure\Data\Migrations\` - Used by Phase6A54 and all fixes

**Impact**: Phase6A51 migration was placed in a different directory than the main migration path, causing deployment confusion.

**Evidence**:
```bash
# Phase6A51 location (WRONG DIRECTORY)
src\LankaConnect.Infrastructure\Migrations\20260118235411_Phase6A51_AddMissingSignupCommitmentEmailTemplates.cs

# Phase6A54 and fixes location (CORRECT DIRECTORY)
src\LankaConnect.Infrastructure\Data\Migrations\20251227232000_Phase6A54_SeedNewEmailTemplates.cs
src\LankaConnect.Infrastructure\Data\Migrations\20260119154406_Phase6A54Fix_AddMissingEmailTemplates.cs
```

### 2. Migration Not Applied to Staging Database

**Issue**: The Phase6A51 migration exists in the codebase but was never applied to the staging database.

**Possible Reasons**:
1. Wrong directory path in EF Core configuration
2. Migration was not included in deployment pipeline
3. Migration was skipped due to directory mismatch
4. Manual oversight during deployment

**Evidence**:
- Event handlers exist and reference `signup-commitment-updated` and `signup-commitment-cancelled`
- Templates are missing from database
- Domain events are being raised correctly
- Email service is attempting to send emails but failing due to missing templates

### 3. Event Handlers Are Correctly Implemented

**Analysis**: All event handlers are properly implemented and registered via MediatR auto-discovery.

**Event Handlers Present**:
```csharp
// CommitmentUpdatedEventHandler.cs (Phase 6A.51+)
public class CommitmentUpdatedEventHandler
    : INotificationHandler<DomainEventNotification<CommitmentUpdatedEvent>>
{
    // Sends email using template: "signup-commitment-updated"
}

// CommitmentCancelledEmailHandler.cs (Phase 6A.51+)
public class CommitmentCancelledEmailHandler
    : INotificationHandler<DomainEventNotification<CommitmentCancelledEvent>>
{
    // Sends email using template: "signup-commitment-cancelled"
}
```

**Registration**: MediatR auto-discovery finds all `INotificationHandler<>` implementations in Application assembly. No manual DI registration needed.

### 4. Domain Events Are Being Raised Correctly

**Analysis**: SignUpItem entity correctly raises domain events for both update and cancel operations.

**Evidence**:
```csharp
// SignUpItem.cs:261-267 (UpdateCommitment method)
RaiseDomainEvent(new DomainEvents.CommitmentUpdatedEvent(
    Id,
    userId,
    oldQuantity,
    newQuantity,
    ItemDescription,
    DateTime.UtcNow));

// SignUpItem.cs:291 (CancelCommitment method)
RaiseDomainEvent(new DomainEvents.CommitmentCancelledEvent(
    Id,
    commitment.Id,
    userId));
```

### 5. Template Seeding Pattern Comparison

**Phase6A51 Pattern (NON-IDEMPOTENT)**:
```sql
INSERT INTO communications.email_templates
(
    "Id",
    "name",
    -- ... other fields
)
VALUES
(
    gen_random_uuid(),
    'signup-commitment-updated',
    -- ... other values
);
```

**Phase6A54Fix Pattern (IDEMPOTENT)**:
```sql
INSERT INTO communications.email_templates
(
    "Id",
    "name",
    -- ... other fields
)
SELECT
    gen_random_uuid(),
    'signup-commitment-confirmation',
    -- ... other values
WHERE NOT EXISTS (
    SELECT 1 FROM communications.email_templates
    WHERE name = 'signup-commitment-confirmation'
);
```

**Issue**: Phase6A51 migration uses non-idempotent SQL that will fail if templates already exist.

---

## Impact Assessment

### User Impact
- **Severity**: High
- **Affected Users**: All users who update or cancel signup commitments
- **User Experience**: No email confirmation when updating or canceling commitments
- **Workaround**: None (users must check website to verify changes)

### System Impact
- **Functionality**: Email notification system partially broken
- **Data Integrity**: Not affected (domain events still raised, data still saved)
- **Performance**: Not affected
- **Error Handling**: Fail-silent (logs error but doesn't throw)

### Business Impact
- **User Trust**: Reduced (no confirmation of important actions)
- **Support Tickets**: Potentially increased (users unsure if actions succeeded)
- **Compliance**: May violate email notification requirements

---

## Architectural Issues Identified

### 1. Migration Directory Structure
**Problem**: Multiple migration directories cause confusion
**Recommendation**: Consolidate all migrations into single directory path

### 2. Migration Validation in CI/CD
**Problem**: Migrations not validated before deployment
**Recommendation**: Add migration verification step to CI/CD pipeline

### 3. Non-Idempotent Migrations
**Problem**: Migrations fail if run multiple times
**Recommendation**: Always use idempotent SQL for data seeding

### 4. Missing Designer Files Detection
**Problem**: EF Core silently skips migrations without Designer files
**Recommendation**: Add build-time validation for Designer file presence

### 5. Email Template Testing
**Problem**: No automated tests to verify template existence
**Recommendation**: Add integration tests to verify all referenced templates exist

---

## Proposed Fix

### Option 1: Create New Idempotent Migration (RECOMMENDED)

**Migration Name**: `Phase6A54Fix_Part2_AddUpdateCancelTemplates`
**Location**: `src\LankaConnect.Infrastructure\Data\Migrations\`
**Approach**: Single migration for both templates with idempotent SQL

**Advantages**:
- ✅ Consistent with Phase6A54Fix approach
- ✅ Safe to run multiple times
- ✅ Same directory as other migrations
- ✅ Single atomic operation

**Template SQL Pattern**:
```sql
INSERT INTO communications.email_templates (...)
SELECT ...
WHERE NOT EXISTS (
    SELECT 1 FROM communications.email_templates
    WHERE name = 'signup-commitment-updated'
);

INSERT INTO communications.email_templates (...)
SELECT ...
WHERE NOT EXISTS (
    SELECT 1 FROM communications.email_templates
    WHERE name = 'signup-commitment-cancelled'
);
```

### Option 2: Apply Phase6A51 Migration Manually

**Approach**: Manually apply existing Phase6A51 migration to staging database

**Advantages**:
- Uses existing migration

**Disadvantages**:
- ❌ Migration is in wrong directory
- ❌ Non-idempotent (fails if templates exist)
- ❌ Doesn't fix directory structure issue
- ❌ Requires manual database intervention

### Option 3: Migrate Phase6A51 to Correct Directory

**Approach**: Move Phase6A51 migration to Data\Migrations directory

**Advantages**:
- Fixes directory structure

**Disadvantages**:
- ❌ Requires regenerating migration
- ❌ Still non-idempotent
- ❌ More complex than Option 1
- ❌ Risk of breaking migration history

---

## Recommended Solution

### Create Phase6A54Fix_Part2 Migration

**Implementation Steps**:

1. Create new migration with idempotent SQL for both templates:
   - `signup-commitment-updated`
   - `signup-commitment-cancelled`

2. Use same SQL structure as Phase6A54Fix (SELECT with WHERE NOT EXISTS)

3. Place in correct directory: `src\LankaConnect.Infrastructure\Data\Migrations\`

4. Test locally to verify:
   - ✅ Migration applies successfully
   - ✅ Templates are created in database
   - ✅ Update email sends successfully
   - ✅ Cancel email sends successfully

5. Deploy to staging and verify email functionality

### Migration Code Structure

```csharp
public partial class Phase6A54Fix_Part2_AddUpdateCancelTemplates : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Template 1: Signup Commitment Updated (idempotent)
        migrationBuilder.Sql(@"
            INSERT INTO communications.email_templates (...)
            SELECT ...
            WHERE NOT EXISTS (
                SELECT 1 FROM communications.email_templates
                WHERE name = 'signup-commitment-updated'
            );
        ");

        // Template 2: Signup Commitment Cancelled (idempotent)
        migrationBuilder.Sql(@"
            INSERT INTO communications.email_templates (...)
            SELECT ...
            WHERE NOT EXISTS (
                SELECT 1 FROM communications.email_templates
                WHERE name = 'signup-commitment-cancelled'
            );
        ");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Delete templates
        migrationBuilder.Sql(@"
            DELETE FROM communications.email_templates
            WHERE name IN (
                'signup-commitment-updated',
                'signup-commitment-cancelled'
            );
        ");
    }
}
```

---

## Prevention Strategy

### 1. CI/CD Pipeline Improvements

**Add Migration Validation Step**:
```yaml
# .github/workflows/ci.yml or azure-pipelines.yml
- name: Validate Migrations
  run: |
    # Check all migrations have Designer files
    dotnet ef migrations list --project src/LankaConnect.Infrastructure \
      --startup-project src/LankaConnect.API \
      --context AppDbContext

    # Verify no pending migrations in wrong directory
    if [ -d "src/LankaConnect.Infrastructure/Migrations" ]; then
      echo "ERROR: Migrations found in wrong directory"
      exit 1
    fi
```

**Add Designer File Check**:
```yaml
- name: Check Designer Files
  run: |
    # Find all .cs files that are migrations
    MIGRATIONS=$(find src/LankaConnect.Infrastructure/Data/Migrations -name "*_*.cs" -not -name "*.Designer.cs" -not -name "*ModelSnapshot.cs")

    # Check each migration has corresponding Designer file
    for migration in $MIGRATIONS; do
      designer="${migration%.cs}.Designer.cs"
      if [ ! -f "$designer" ]; then
        echo "ERROR: Missing Designer file for $migration"
        exit 1
      fi
    done
```

### 2. Migration Directory Consolidation

**Action**: Remove incorrect migration directory
```bash
# Move any migrations from wrong directory
mv src/LankaConnect.Infrastructure/Migrations/*.cs \
   src/LankaConnect.Infrastructure/Data/Migrations/

# Remove old directory
rmdir src/LankaConnect.Infrastructure/Migrations
```

**Update EF Core Configuration** (if needed):
```csharp
// Verify migrations path in DbContext
protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
{
    optionsBuilder.UseSqlite(
        migrationsAssembly: "LankaConnect.Infrastructure",
        migrationsHistoryTableName: "__EFMigrationsHistory",
        migrationsHistoryTableSchema: null);
}
```

### 3. Email Template Validation Tests

**Create Integration Test**:
```csharp
// tests/LankaConnect.IntegrationTests/EmailTemplateTests.cs
[Fact]
public async Task All_Referenced_Email_Templates_Should_Exist()
{
    // Arrange
    var requiredTemplates = new[]
    {
        "signup-commitment-confirmation",
        "signup-commitment-updated",
        "signup-commitment-cancelled",
        "member-email-verification",
        "registration-cancellation",
        "organizer-custom-message"
    };

    // Act
    foreach (var templateName in requiredTemplates)
    {
        var template = await _dbContext.EmailTemplates
            .FirstOrDefaultAsync(t => t.Name == templateName);

        // Assert
        Assert.NotNull(template);
        Assert.True(template.IsActive);
    }
}
```

### 4. Build-Time Template Reference Validation

**Add Analyzer**:
```csharp
// src/LankaConnect.Analyzers/EmailTemplateReferenceAnalyzer.cs
// Scan all SendTemplatedEmailAsync calls and extract template names
// Verify template names exist in database seed data
```

### 5. Documentation Updates

**Update Migration Guidelines**:
```markdown
# Migration Creation Guidelines

1. ALWAYS use the correct directory: src/LankaConnect.Infrastructure/Data/Migrations
2. ALWAYS verify Designer file is generated
3. ALWAYS use idempotent SQL for data seeding (INSERT ... SELECT ... WHERE NOT EXISTS)
4. ALWAYS test migration locally before committing
5. ALWAYS update email template reference list in tests
```

---

## Lessons Learned

### 1. Directory Structure Matters
- Multiple migration directories create confusion
- EF Core configuration should specify single migration path
- Build validation should enforce directory structure

### 2. Idempotent Migrations Are Critical
- Data seeding migrations should be safe to run multiple times
- Use WHERE NOT EXISTS pattern for all INSERT statements
- Allows for easier rollback and re-application

### 3. Automated Testing Is Essential
- Manual verification is error-prone
- Integration tests should verify template existence
- CI/CD should validate migration integrity

### 4. Designer Files Are Required
- EF Core silently skips migrations without Designer files
- Build process should validate Designer file presence
- Missing Designer files indicate migration generation issues

### 5. Fail-Silent Can Hide Issues
- Email handlers use fail-silent pattern (log but don't throw)
- Missing templates don't cause obvious errors
- Need better monitoring and alerting for email failures

---

## Action Items

### Immediate (Fix Current Issue)
- [ ] Create Phase6A54Fix_Part2 migration with idempotent SQL
- [ ] Test migration locally
- [ ] Deploy to staging
- [ ] Verify update and cancel emails send correctly
- [ ] Update PROGRESS_TRACKER.md with RCA reference

### Short Term (Prevent Recurrence)
- [ ] Add migration validation to CI/CD pipeline
- [ ] Add Designer file check to build process
- [ ] Create email template validation integration tests
- [ ] Consolidate migration directories
- [ ] Update migration creation documentation

### Long Term (Systematic Improvements)
- [ ] Implement email template reference analyzer
- [ ] Add monitoring/alerting for email failures
- [ ] Create email template management UI
- [ ] Implement template version control
- [ ] Add template usage tracking

---

## References

### Related Files
- `src/LankaConnect.Domain/Events/Entities/SignUpItem.cs` (lines 261-291)
- `src/LankaConnect.Application/Events/EventHandlers/CommitmentUpdatedEventHandler.cs`
- `src/LankaConnect.Application/Events/EventHandlers/CommitmentCancelledEmailHandler.cs`
- `src/LankaConnect.Infrastructure/Migrations/20260118235411_Phase6A51_AddMissingSignupCommitmentEmailTemplates.cs`
- `src/LankaConnect.Infrastructure/Data/Migrations/20260119154406_Phase6A54Fix_AddMissingEmailTemplates.cs`

### Related Documentation
- `docs/PHASE_6A51_SIGNUP_COMMITMENT_EMAILS_SUMMARY.md`
- `docs/EMAIL_SYSTEM_COMPREHENSIVE_ANALYSIS.md`
- `docs/PROGRESS_TRACKER.md`

### ADRs Referenced
- ADR-008: Domain Event Infrastructure Pattern (CommitmentCancelledEvent)

---

## Appendix: Complete Template Details

### Template 1: signup-commitment-updated

**Purpose**: Confirmation email when user updates commitment quantity

**Variables Required**:
- `UserName`: User's first name
- `EventTitle`: Event name
- `ItemDescription`: Committed item description
- `OldQuantity`: Previous quantity
- `NewQuantity`: Updated quantity
- `EventDate`: Event start date (formatted)
- `EventLocation`: Event location or "Location TBD"

**Handler**: `CommitmentUpdatedEventHandler.cs` (line 77)

### Template 2: signup-commitment-cancelled

**Purpose**: Confirmation email when user cancels commitment

**Variables Required**:
- `UserName`: User's first name
- `EventTitle`: Event name
- `ItemDescription`: Cancelled item description
- `Quantity`: Cancelled quantity
- `EventDate`: Event start date (formatted)
- `EventLocation`: Event location or "Location TBD"

**Handler**: `CommitmentCancelledEmailHandler.cs` (line 107)

---

**End of Root Cause Analysis**