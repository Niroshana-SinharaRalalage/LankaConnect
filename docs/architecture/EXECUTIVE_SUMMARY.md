# Executive Summary - Critical Production Blockers

**Date**: 2025-12-03
**Severity**: CRITICAL - Production Deployment Blocked
**Estimated Fix Time**: 2 hours
**Risk Level**: Medium (requires database changes)

---

## Overview

Three interconnected architectural issues have been identified that are preventing production deployment and causing runtime failures in the staging environment. These issues stem from EF Core configuration conflicts, migration folder fragmentation, and incomplete database updates.

---

## The Three Issues

### Issue 1: Dual Migration Folders (Organizational)
**Status**: 🔴 CRITICAL
**Impact**: Staging deployments missing critical migrations

**Problem**: Project has TWO migration folders with 34 migrations each:
- `src/LankaConnect.Infrastructure/Migrations/` (old)
- `src/LankaConnect.Infrastructure/Data/Migrations/` (new)

**Root Cause**: Migration consolidation in commit f582356 (Nov 3) moved some files but EF Core continued generating new migrations in the new folder, creating a split.

**Impact**:
- ❌ EventImages migration in old folder not visible to staging
- ❌ Deployment inconsistency across environments
- ❌ Cannot guarantee which migrations are applied

**Solution**: Consolidate all migrations to `Data/Migrations/` folder

---

### Issue 2: EF Core Money Configuration Conflict (Technical)
**Status**: 🔴 CRITICAL
**Impact**: Cannot generate new migrations, blocks all development

**Problem**: EF Core error when configuring Money value object:
```
Unable to determine the owner for the relationship between 'TicketPricing.AdultPrice' and 'Money'
as both types have been marked as owned.
```

**Root Cause**: Money value object used in TWO different ownership contexts:
1. **Event.TicketPrice** → Configured as `OwnsOne` with separate columns (`ticket_price_amount`, `ticket_price_currency`)
2. **Event.Pricing.AdultPrice** → Inside `ToJson()` JSONB serialization

EF Core cannot resolve which mapping to use for Money when it appears inside a JSON-serialized entity.

**Additional Complexity**: `GroupPricingTier.PricePerPerson` (Money) is nested THREE levels deep:
- Event → Pricing (ToJson) → GroupTiers (Collection) → PricePerPerson (Money)

**Impact**:
- ❌ `Pricing` configuration commented out (dual pricing feature disabled)
- ❌ Cannot create new migrations
- ❌ Build fails when configuration is enabled
- ❌ Blocks Session 21 dual pricing feature

**Solution**: Convert `Event.TicketPrice` to also use `ToJson()` for consistency

---

### Issue 3: EventImages Table Missing in Staging (Deployment)
**Status**: 🔴 CRITICAL
**Impact**: Image upload feature returns 500 errors in staging

**Problem**: EventImages table doesn't exist in staging PostgreSQL database, causing image upload failures.

**Root Cause**: The `AddEventImages` migration (20251103040053) exists in the OLD migration folder (`Migrations/`) but staging deployment only scanned the NEW folder (`Data/Migrations/`).

**Migration File**: Located at `src/LankaConnect.Infrastructure/Migrations/20251103040053_AddEventImages.cs`
**Created**: Nov 3, 2025
**Status**: Not applied to staging

**Impact**:
- ❌ Image upload endpoint returns 500 Internal Server Error
- ❌ Event image gallery feature broken in staging
- ❌ Cannot test Epic 2 Phase 2 features

**Solution**: Apply AddEventImages migration to staging database

---

## How They're Connected

```
Issue 2 (EF Core Error)
       │
       ├─► Cannot generate new migrations
       │
       └─► Configuration commented out to proceed
               │
               └─► Issue 1 (Dual Folders)
                       │
                       ├─► Old folder: AddEventImages migration
                       │
                       └─► New folder: Recent migrations (Nov 9+)
                               │
                               └─► Issue 3 (Missing Table)
                                       │
                                       └─► Staging only scanned new folder
```

**Timeline**:
1. **Nov 2-3**: AddEventImages migration created in old folder
2. **Nov 3**: Consolidation attempt (commit f582356) moved some files
3. **Nov 3+**: EF Core error discovered with Pricing configuration
4. **Nov 3+**: Pricing configuration commented out to unblock development
5. **Nov 9+**: New migrations generated in new folder only
6. **Nov 30+**: Staging deployment missing EventImages migration
7. **Dec 3**: All three issues identified as interconnected

---

## Business Impact

### Immediate Impact
- **Production Deployment**: BLOCKED - cannot deploy with inconsistent migration state
- **Feature Delivery**: DELAYED - Epic 2 Phase 2 (event images) cannot be released
- **Testing**: BLOCKED - staging environment not reflecting dev environment
- **Development**: SLOWED - dual pricing feature (Session 21) disabled

### Risk to Business Goals
- **User Experience**: Event organizers cannot upload images in staging
- **Feature Completeness**: Dual pricing (adult/child tickets) not available
- **Technical Debt**: Migration folder confusion accumulating
- **Deployment Confidence**: Low - environment inconsistency

---

## Recommended Solution

### Single Coordinated Fix (2 hours)

**Phase 1: Fix EF Core Configuration** (30 minutes)
1. Convert `Event.TicketPrice` to use `ToJson("ticket_price")`
2. Enable `Event.Pricing` configuration with `ToJson("pricing")`
3. Create migration to convert ticket_price columns to JSONB
4. Test in development

**Phase 2: Consolidate Migrations** (30 minutes)
1. Move all migrations to `Data/Migrations/` folder
2. Verify migration list shows all 34+ migrations
3. Update namespaces if needed
4. Build and test

**Phase 3: Apply to Staging** (45 minutes)
1. Backup staging database
2. Generate SQL script for AddEventImages migration
3. Apply AddEventImages migration
4. Apply ConvertTicketPriceToJson migration
5. Deploy updated code
6. Test event image upload and dual pricing

**Phase 4: Verification** (15 minutes)
1. Verify EventImages table exists
2. Test image upload endpoint
3. Test dual pricing event creation
4. Check migration history consistency

---

## Technical Approach

### EF Core Configuration Change

**Before (Conflicting)**:
```csharp
// Event.TicketPrice - separate columns
builder.OwnsOne(e => e.TicketPrice, money =>
{
    money.Property(m => m.Amount).HasColumnName("ticket_price_amount");
    money.Property(m => m.Currency).HasColumnName("ticket_price_currency");
});

// Event.Pricing - JSONB (COMMENTED OUT due to conflict)
// builder.OwnsOne(e => e.Pricing, pricing =>
// {
//     pricing.ToJson("pricing");  // ← Money inside here conflicts with above
// });
```

**After (Consistent)**:
```csharp
// Event.TicketPrice - JSONB
builder.OwnsOne(e => e.TicketPrice, money =>
{
    money.ToJson("ticket_price");  // ← Consistent with Pricing
});

// Event.Pricing - JSONB
builder.OwnsOne(e => e.Pricing, pricing =>
{
    pricing.ToJson("pricing");  // ← No conflict now
});
```

### Database Schema Change

**Before**:
```sql
events.events
├── ticket_price_amount (numeric(18,2))   ← Separate columns
├── ticket_price_currency (varchar(3))    ←
└── pricing (missing - feature disabled)
```

**After**:
```sql
events.events
├── ticket_price (jsonb)   ← Single JSONB column
└── pricing (jsonb)        ← Single JSONB column
```

**Data Migration** (automatic):
```sql
-- Convert existing data from columns to JSONB
UPDATE events.events
SET ticket_price = json_build_object(
    'Amount', ticket_price_amount,
    'Currency', ticket_price_currency
)::jsonb
WHERE ticket_price_amount IS NOT NULL;
```

---

## Success Metrics

**Technical Success Criteria**:
- ✅ Zero compilation errors
- ✅ All migrations in single folder (`Data/Migrations/`)
- ✅ EventImages table exists in staging
- ✅ Image upload returns 200 OK
- ✅ Dual pricing creates events successfully
- ✅ Migration history consistent across environments

**Business Success Criteria**:
- ✅ Production deployment unblocked
- ✅ Epic 2 Phase 2 ready for release
- ✅ Session 21 dual pricing feature enabled
- ✅ Staging environment matches dev environment

---

## Risk Assessment

### Implementation Risks

| Risk | Likelihood | Impact | Mitigation |
|------|-----------|--------|------------|
| Data loss during JSONB conversion | Low | High | Data migration SQL tested in dev first |
| Migration fails in staging | Medium | High | Generate SQL script, review before apply |
| Application downtime | Low | Medium | Apply during maintenance window |
| Rollback complexity | Medium | Medium | Database backup before changes |

### Mitigation Strategies

1. **Database Backup**: Full backup before any staging changes
2. **SQL Script Review**: Manual review of all generated SQL
3. **Development Testing**: Test complete flow in dev first
4. **Incremental Deployment**: Apply migrations one at a time
5. **Rollback Plan**: Document rollback steps for each phase

---

## Rollback Plan

**If Issue Occurs in Staging**:

1. **Stop Application**:
   ```bash
   sudo systemctl stop lankaconnect-api
   ```

2. **Restore Database**:
   ```bash
   pg_restore -U postgres -d lankaconnect_staging backup.dump
   ```

3. **Revert Code**:
   ```bash
   git checkout previous-working-commit
   dotnet publish
   ```

4. **Restart Application**:
   ```bash
   sudo systemctl start lankaconnect-api
   ```

5. **Investigate**: Review error logs and migration history

**Recovery Time**: ~15 minutes (with backup ready)

---

## Resource Requirements

### People
- **1 Backend Developer**: EF Core configuration and migrations
- **1 DevOps Engineer**: Staging deployment and database access
- **1 QA Engineer**: Testing and verification

### Infrastructure
- **Database Access**: PostgreSQL admin access to staging
- **Deployment Access**: CI/CD pipeline or manual deployment capability
- **Backup Storage**: 2GB for staging database backup

### Time Allocation
- Development: 1 hour
- Testing: 30 minutes
- Staging Deployment: 30 minutes
- Verification: 30 minutes
- **Total**: 2.5 hours

---

## Timeline

**Recommended Schedule**:

| Time | Phase | Activity | Owner |
|------|-------|----------|-------|
| T+0:00 | Preparation | Database backup, code review | DevOps |
| T+0:15 | Phase 1 | Fix EF Core configuration | Backend Dev |
| T+0:45 | Phase 2 | Consolidate migrations | Backend Dev |
| T+1:15 | Phase 3 | Apply to staging | DevOps |
| T+2:00 | Phase 4 | Testing and verification | QA |
| T+2:30 | Complete | Documentation and monitoring | All |

**Best Time to Execute**: During low-traffic period (early morning or weekend)

---

## Communication Plan

### Stakeholder Updates

**Before Deployment**:
- Notify team of maintenance window
- Share rollback plan
- Confirm backup status

**During Deployment**:
- Real-time status updates in Slack/Teams
- Alert if issues occur
- Document any deviations from plan

**After Deployment**:
- Success confirmation
- Metrics report
- Lessons learned

### Escalation Path

1. **Issue Detected** → Backend Developer (immediate)
2. **Cannot Resolve in 15 min** → DevOps Lead (escalate)
3. **Data Loss Risk** → CTO (immediate escalation)

---

## Long-Term Improvements

### Process Improvements
1. **Migration CI/CD Check**: Automated check for migration folder consistency
2. **Environment Parity**: Ensure staging mirrors dev database state
3. **Pre-Deployment Testing**: Staging deployment dry-run in CI
4. **Migration Documentation**: Document all EF Core patterns used

### Technical Improvements
1. **Standardize Value Object Storage**: Always use ToJson for complex value objects
2. **Migration Folder Policy**: Single source of truth for migrations
3. **Database Schema Monitoring**: Alert on staging/dev schema drift
4. **Automated Rollback**: Script-based rollback procedures

---

## Decision Required

**Option 1: Implement Full Fix** (Recommended)
- **Timeline**: 2.5 hours
- **Risk**: Medium (requires database changes)
- **Benefit**: Unblocks production, enables all features
- **Recommendation**: ✅ PROCEED

**Option 2: Partial Fix (EventImages only)**
- **Timeline**: 30 minutes
- **Risk**: Low (single table addition)
- **Benefit**: Unblocks image uploads only
- **Limitation**: Dual pricing still disabled, migration folder issue remains
- **Recommendation**: ⚠️ SHORT-TERM ONLY

**Option 3: Defer to Next Sprint**
- **Timeline**: N/A
- **Risk**: Low (no changes)
- **Impact**: Production deployment blocked, features delayed
- **Recommendation**: ❌ NOT RECOMMENDED

---

## Approval Required

**Technical Approval**:
- [ ] Backend Lead - EF Core configuration changes
- [ ] DevOps Lead - Staging database changes
- [ ] QA Lead - Testing and verification plan

**Business Approval**:
- [ ] Product Owner - Feature impact and timeline
- [ ] Engineering Manager - Resource allocation

**Sign-off**:
- [ ] CTO - Production deployment strategy

---

## Appendices

### A. Detailed Technical Analysis
See: [CRITICAL_ISSUES_ANALYSIS.md](./CRITICAL_ISSUES_ANALYSIS.md)

### B. Visual Architecture Guide
See: [MIGRATION_FOLDER_ARCHITECTURE.md](./MIGRATION_FOLDER_ARCHITECTURE.md)

### C. Step-by-Step Fix Guide
See: [QUICK_FIX_GUIDE.md](./QUICK_FIX_GUIDE.md)

### D. Git Commit References
- `f582356` - Migration consolidation attempt (incomplete)
- `c75bb8c` - AddEventImages migration (original)
- `4669852` - Dual ticket pricing implementation

### E. Database Schema Scripts
Available in: `c:\Work\LankaConnect\scripts\`

---

## Contact Information

**For Questions**: Backend Development Team
**For Escalation**: DevOps Lead
**Emergency Contact**: CTO

**Documentation Location**: `c:\Work\LankaConnect\docs\architecture\`

---

**Last Updated**: 2025-12-03
**Next Review**: After staging deployment
**Status**: AWAITING APPROVAL
