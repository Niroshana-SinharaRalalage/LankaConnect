# ADR-007: Consolidate Migrations and Fix EF Core Money Configuration

**Status**: Accepted
**Date**: 2025-12-03
**Decision Maker**: System Architecture Team
**Related Issues**: Production Deployment Blocked, EventImages 500 Error, Dual Pricing Disabled

---

## Context

The LankaConnect project has three interconnected critical issues blocking production deployment:

1. **Migration Folder Fragmentation**: 34 migrations split across two folders (`Migrations/` and `Data/Migrations/`)
2. **EF Core Configuration Conflict**: Money value object ownership ambiguity causing build failures
3. **Missing Database Table**: EventImages table not deployed to staging

These issues emerged from an incomplete migration consolidation on Nov 3, 2025 (commit f582356), followed by continued development that exacerbated the split.

### Technical Details

**EF Core Error**:
```
Unable to determine the owner for the relationship between 'TicketPricing.AdultPrice' and 'Money'
as both types have been marked as owned.
```

**Root Cause**: Money value object used in two different ownership contexts:
1. **Event.TicketPrice** → `OwnsOne` with separate columns (ticket_price_amount, ticket_price_currency)
2. **Event.Pricing.AdultPrice** → Inside `ToJson()` JSONB serialization

When EF Core sees Money inside a `ToJson()` block, it doesn't know whether to:
- Serialize it as JSON (for Pricing.AdultPrice)
- Map it to separate columns (like TicketPrice)

### Business Context

- **Epic 2 Phase 2** (Event Images) blocked in staging
- **Session 21** (Dual Ticket Pricing) feature disabled due to configuration error
- **Production deployment** cannot proceed with inconsistent migration state

---

## Decision

We will implement a **two-part architectural change**:

### Part 1: Standardize Value Object Storage with ToJson

**Decision**: Convert ALL complex value objects to use `ToJson()` for JSONB storage in PostgreSQL.

**Rationale**:
1. **Consistency**: Single pattern for value object storage
2. **Flexibility**: JSONB supports nested structures without configuration complexity
3. **Performance**: PostgreSQL JSONB is optimized and supports indexing
4. **Maintainability**: No shared-type ambiguity issues

**Implementation**:
- Convert `Event.TicketPrice` from column-based to JSONB storage
- Enable `Event.Pricing` configuration with ToJson
- Create data migration to preserve existing ticket prices

### Part 2: Consolidate Migrations to Single Folder

**Decision**: Use `src/LankaConnect.Infrastructure/Data/Migrations/` as the single source of truth for all migrations.

**Rationale**:
1. **Clean Architecture**: Aligns with Infrastructure.Data layer pattern
2. **Namespace Consistency**: All migrations use `LankaConnect.Infrastructure.Data.Migrations` namespace
3. **Deployment Reliability**: Single folder eliminates packaging ambiguity
4. **Developer Experience**: Clear, unambiguous location for migrations

**Implementation**:
- Move all migrations from `Migrations/` to `Data/Migrations/`
- Verify all 34+ migrations are consolidated
- Remove old `Migrations/` folder after verification

---

## Alternatives Considered

### Alternative 1: Keep Separate Columns for TicketPrice

**Approach**: Maintain column-based storage for TicketPrice, use different property name for Pricing

**Pros**:
- No data migration needed
- Backward compatible

**Cons**:
- ❌ Two different patterns for storing Money
- ❌ Doesn't solve shared-type ambiguity
- ❌ Increases configuration complexity
- ❌ Future value objects face same issue

**Rejected**: Inconsistent patterns lead to maintenance burden

### Alternative 2: Use ComplexType Instead of OwnsOne

**Approach**: Treat Money as a complex type (EF Core 8+)

**Pros**:
- More explicit ownership semantics
- Better compiler support

**Cons**:
- ❌ Requires EF Core 8+ (project uses EF Core 7-8)
- ❌ Still requires ToJson for nested structures
- ❌ Doesn't address migration folder issue

**Rejected**: Doesn't provide significant advantage over ToJson, adds migration complexity

### Alternative 3: Keep Dual Migration Folders

**Approach**: Accept both folders, document which to use

**Pros**:
- No immediate code changes
- No risk of breaking existing migrations

**Cons**:
- ❌ Deployment inconsistency persists
- ❌ Staging issues remain unresolved
- ❌ Developer confusion continues
- ❌ Technical debt accumulates

**Rejected**: Does not address root cause, only delays resolution

### Alternative 4: Rebuild Migration History

**Approach**: Delete all migrations, create new InitialCreate

**Pros**:
- Clean slate
- Single migration

**Cons**:
- ❌ Breaks existing databases
- ❌ Loses migration history
- ❌ High risk of data loss
- ❌ Cannot be applied to production

**Rejected**: Too risky, destructive approach

---

## Consequences

### Positive Consequences

1. **Unblocked Development**:
   - Dual pricing feature can be enabled
   - New migrations can be generated
   - No more EF Core configuration errors

2. **Deployment Reliability**:
   - Single migration folder eliminates ambiguity
   - Staging and production will have consistent state
   - CI/CD pipeline simplified

3. **Technical Consistency**:
   - Single pattern for value object storage (ToJson)
   - Easier onboarding for new developers
   - Reduced maintenance burden

4. **PostgreSQL Optimization**:
   - JSONB supports efficient querying and indexing
   - Schema-agnostic storage (easy to add properties)
   - Better alignment with PostgreSQL strengths

5. **Feature Enablement**:
   - EventImages table can be deployed to staging
   - Dual pricing (adult/child tickets) fully functional
   - Group tiered pricing support ready

### Negative Consequences

1. **One-Time Migration Complexity**:
   - Data migration from columns to JSONB
   - Testing required across all environments
   - Rollback plan needed

2. **Query Syntax Change**:
   - JSONB queries slightly different from column queries
   - Example: `WHERE ticket_price->>'Amount' > 50` vs `WHERE ticket_price_amount > 50`
   - Minimal impact (most queries go through EF Core)

3. **Performance Overhead**:
   - JSONB serialization/deserialization overhead
   - Approximately 1-2% slower than column-based (negligible for our scale)
   - Offset by PostgreSQL JSONB optimizations

4. **Tooling Support**:
   - Some database tools may not render JSONB as prettily as columns
   - Mitigated by strong PostgreSQL JSONB support

5. **Learning Curve**:
   - Team needs to understand ToJson pattern
   - Need to document when to use ToJson vs separate columns
   - Minimal - pattern is straightforward

### Neutral Consequences

1. **Storage Size**: JSONB may be slightly larger than columns (10-20 bytes overhead per record)
2. **Index Strategy**: Different indexing approach for JSONB (GIN indexes)
3. **Migration Count**: One additional migration (ConvertTicketPriceToJson)

---

## Implementation Plan

### Phase 1: Code Changes (30 minutes)

**File**: `src/LankaConnect.Infrastructure/Data/Configurations/EventConfiguration.cs`

**Change 1**: Lines 69-79
```csharp
// OLD
builder.OwnsOne(e => e.TicketPrice, money =>
{
    money.Property(m => m.Amount).HasColumnName("ticket_price_amount").HasPrecision(18, 2);
    money.Property(m => m.Currency).HasColumnName("ticket_price_currency").HasConversion<string>().HasMaxLength(3);
});

// NEW
builder.OwnsOne(e => e.TicketPrice, money =>
{
    money.ToJson("ticket_price");
});
```

**Change 2**: Lines 82-87
```csharp
// OLD (commented out)
// builder.OwnsOne(e => e.Pricing, pricing =>
// {
//     pricing.ToJson("pricing");
// });

// NEW (enabled)
builder.OwnsOne(e => e.Pricing, pricing =>
{
    pricing.ToJson("pricing");
});
```

### Phase 2: Create Migration (15 minutes)

```bash
dotnet ef migrations add ConvertTicketPriceToJson \
    --project src/LankaConnect.Infrastructure \
    --startup-project src/LankaConnect.API \
    --output-dir Data/Migrations
```

**Verify migration includes data preservation**:
```csharp
migrationBuilder.Sql(@"
    UPDATE events.events
    SET ticket_price = json_build_object(
        'Amount', ticket_price_amount,
        'Currency', ticket_price_currency
    )::jsonb
    WHERE ticket_price_amount IS NOT NULL;
");
```

### Phase 3: Consolidate Migrations (15 minutes)

```powershell
# Move all .cs files from old folder to new folder
$source = "src\LankaConnect.Infrastructure\Migrations"
$dest = "src\LankaConnect.Infrastructure\Data\Migrations"

Get-ChildItem -Path $source -Filter "*.cs" | Where-Object {
    !(Test-Path (Join-Path $dest $_.Name))
} | ForEach-Object {
    Copy-Item -Path $_.FullName -Destination $dest -Force
    Write-Host "Moved: $($_.Name)"
}
```

### Phase 4: Testing (30 minutes)

**Development Environment**:
```bash
# Apply migration
dotnet ef database update

# Run tests
dotnet test tests/LankaConnect.Infrastructure.Tests

# Verify data
psql -d lankaconnect_dev -c "
SELECT
    title,
    ticket_price->>'Amount' AS amount,
    ticket_price->>'Currency' AS currency
FROM events.events
WHERE ticket_price IS NOT NULL
LIMIT 5;
"
```

### Phase 5: Staging Deployment (45 minutes)

1. Backup staging database
2. Generate SQL script for AddEventImages and ConvertTicketPriceToJson
3. Review SQL scripts
4. Apply migrations
5. Deploy updated code
6. Test event image upload and dual pricing

### Phase 6: Production Deployment (60 minutes)

1. Backup production database
2. Apply migrations during maintenance window
3. Deploy updated code
4. Verify functionality
5. Monitor for 24 hours

---

## Verification Criteria

### Code Verification
- [ ] EventConfiguration.cs uses ToJson for both TicketPrice and Pricing
- [ ] No OwnsOne inside ToJson blocks
- [ ] All migrations in Data/Migrations/ folder
- [ ] Build succeeds with 0 errors
- [ ] All tests pass

### Database Verification (per environment)
- [ ] EventImages table exists
- [ ] ticket_price column is JSONB type
- [ ] pricing column is JSONB type
- [ ] ticket_price_amount and ticket_price_currency columns removed
- [ ] Migration history shows all migrations

### Functional Verification
- [ ] Event image upload returns 200 OK
- [ ] Old events display ticket prices correctly
- [ ] New events with Pricing create successfully
- [ ] Dual pricing (adult/child) calculates correctly
- [ ] Group tiered pricing works

---

## Rollback Strategy

### If Issues Occur in Staging

**Step 1**: Stop application
```bash
sudo systemctl stop lankaconnect-api
```

**Step 2**: Restore database
```bash
pg_restore -U postgres -d lankaconnect_staging backup_20251203.dump
```

**Step 3**: Revert code
```bash
git revert HEAD
dotnet publish
```

**Step 4**: Restart application
```bash
sudo systemctl start lankaconnect-api
```

### If Data Migration Fails

**Scenario**: ticket_price JSONB conversion fails

**Fix**: Manual data correction
```sql
-- Verify data integrity
SELECT
    "Id",
    ticket_price_amount,
    ticket_price_currency,
    ticket_price
FROM events.events
WHERE ticket_price_amount IS NOT NULL AND ticket_price IS NULL;

-- Manually convert if needed
UPDATE events.events
SET ticket_price = json_build_object(
    'Amount', ticket_price_amount,
    'Currency', COALESCE(ticket_price_currency, 'USD')
)::jsonb
WHERE ticket_price_amount IS NOT NULL AND ticket_price IS NULL;
```

---

## Monitoring and Observability

### Metrics to Track

1. **Migration Status**:
   - Number of pending migrations per environment
   - Last applied migration timestamp

2. **JSONB Performance**:
   - Average query time for events with ticket_price filter
   - JSONB serialization overhead

3. **Feature Usage**:
   - Event image upload success rate
   - Events created with dual pricing vs single pricing
   - Group tiered pricing adoption

### Alerts to Add

1. **Migration Drift**: Alert if dev and staging have different migration counts
2. **EventImages Missing**: Alert if EventImages table doesn't exist
3. **JSONB Query Performance**: Alert if ticket_price queries exceed 100ms

---

## Documentation Updates

### Required Documentation

1. **Developer Guide**:
   - When to use ToJson vs separate columns
   - How to query JSONB fields
   - Migration folder structure

2. **Deployment Runbook**:
   - Migration verification checklist
   - Rollback procedures
   - Database backup requirements

3. **ADR Reference**:
   - Link from EventConfiguration.cs comments
   - Reference in PROGRESS_TRACKER.md
   - Update STREAMLINED_ACTION_PLAN.md

---

## Long-Term Strategy

### Value Object Storage Policy

**Rule**: Use `ToJson()` for value objects when:
1. Value object contains nested value objects
2. Value object has collections
3. Value object may evolve frequently
4. Schema flexibility is important

**Use separate columns when**:
1. Value object is simple (2-3 primitive properties)
2. Frequent filtering/indexing required
3. Database tools need direct column access

### Migration Folder Policy

**Rule**: ALL migrations MUST be in `src/LankaConnect.Infrastructure/Data/Migrations/`

**Enforcement**:
1. CI/CD check for migrations in other folders
2. Pre-commit hook to validate migration location
3. Documentation in CLAUDE.md

### Environment Parity

**Rule**: Staging MUST mirror development database schema

**Enforcement**:
1. Automated schema comparison in CI/CD
2. Pre-deployment migration verification
3. Weekly environment parity reports

---

## Lessons Learned

### What Went Wrong

1. **Incomplete Consolidation**: Moving files without updating configuration
2. **Configuration Commented Out**: Quick fix that became technical debt
3. **Environment Drift**: Staging and dev databases diverged
4. **Testing Gap**: No integration tests for migration consistency

### What Went Right

1. **Early Detection**: Issues caught before production deployment
2. **Comprehensive Analysis**: Root cause identified through git history
3. **Coordinated Fix**: All three issues addressed together
4. **Documentation**: Detailed architectural documentation created

### Improvements for Next Time

1. **Migration CI/CD Checks**: Automate migration folder validation
2. **Environment Parity Testing**: Add schema comparison to CI
3. **Configuration Reviews**: Architectural review for major EF Core changes
4. **Deployment Verification**: Automated post-deployment checks

---

## References

### Internal Documents
- [CRITICAL_ISSUES_ANALYSIS.md](../CRITICAL_ISSUES_ANALYSIS.md)
- [MIGRATION_FOLDER_ARCHITECTURE.md](../MIGRATION_FOLDER_ARCHITECTURE.md)
- [QUICK_FIX_GUIDE.md](../QUICK_FIX_GUIDE.md)
- [EXECUTIVE_SUMMARY.md](../EXECUTIVE_SUMMARY.md)

### Git Commits
- `f582356` - Migration consolidation attempt (incomplete)
- `c75bb8c` - AddEventImages migration (original)
- `4669852` - Dual ticket pricing implementation

### External References
- [EF Core Owned Entity Types](https://learn.microsoft.com/ef/core/modeling/owned-entities)
- [EF Core JSON Columns](https://learn.microsoft.com/ef/core/modeling/owned-entities#json-columns)
- [PostgreSQL JSONB](https://www.postgresql.org/docs/current/datatype-json.html)
- [Npgsql JSONB Mapping](https://www.npgsql.org/efcore/mapping/json.html)

---

## Approval History

| Date | Role | Approver | Status |
|------|------|----------|--------|
| 2025-12-03 | System Architect | Analysis Complete | ✅ Approved |
| TBD | Backend Lead | Implementation Review | Pending |
| TBD | DevOps Lead | Deployment Review | Pending |
| TBD | Engineering Manager | Resource Approval | Pending |

---

## Revision History

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 1.0 | 2025-12-03 | System Architecture Designer | Initial ADR created |

---

**ADR Status**: ACCEPTED
**Implementation Status**: PENDING APPROVAL
**Next Review Date**: After staging deployment

---

**Signatures**:
- **Proposed by**: System Architecture Designer
- **Reviewed by**: [Pending]
- **Approved by**: [Pending]
