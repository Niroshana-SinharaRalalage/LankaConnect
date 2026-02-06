# Newsletter Issues - Quick Reference Guide

**Date**: 2026-01-14
**Status**: DIAGNOSIS COMPLETE - READY FOR FIXES

---

## Issues Summary

### CRITICAL (Blocking Workflow)

#### Issue #1: "Unknown" Status Badges
- **Status**: FIX ALREADY EXISTS - Needs deployment verification
- **Migration**: `20260114013838_Phase6A74Part9BC_FixInvalidNewsletterStatus.cs`
- **Action**: Verify migration deployed to staging
- **Time**: 5 minutes

#### Issue #5: Newsletter Create Not Working
- **Cause**: Likely validation failures (description length or business rules)
- **Action**: Check backend logs, fix description validation
- **Time**: 30 minutes (after Issue #3 fixed)

### HIGH (Core Features Broken)

#### Issue #2: Newsletter Publishing Fails (400)
- **Cause**: Related to Issue #1 (invalid status) or Issue #3 (validation)
- **Action**: Should resolve after Issues #1 and #3 are fixed
- **Time**: 10 minutes (testing)

#### Issue #3: Image Embedding Validation Error
- **Cause**: Base64 images exceed 5000 char limit
- **Frontend shows**: 1,893 / 50,000 (plain text)
- **Backend sees**: 12,000+ characters (HTML + base64)
- **Action**: Increase `NewsletterDescription.MaxLength` to 50,000
- **Time**: 30 minutes

#### Issue #4: Newsletter Update Fails (400)
- **Cause**: Same as Issue #3
- **Action**: Same fix as Issue #3
- **Time**: 0 (fixed by Issue #3)

### MEDIUM (Public-Facing)

#### Issue #6: Landing Page Shows Hardcoded Data
- **Cause**: Unknown - needs investigation
- **Possibilities**: No Active newsletters, API not called, filters too strict
- **Action**: Query database, test API endpoint
- **Time**: 10 minutes (diagnosis)

### LOW (UX Enhancements)

#### Issue #7: Newsletter List Layout (Grid → Table)
- **Cause**: Design choice
- **Request**: Change from 3-column grid to single-column table
- **Action**: Update layout CSS
- **Time**: 1 hour

#### Issue #8: Location Dropdown (MultiSelect → TreeDropdown)
- **Cause**: Wrong component used
- **Request**: Use hierarchical dropdown (State → Metros)
- **Action**: Replace with existing TreeDropdown component
- **Time**: 30 minutes

---

## Quick Fix Commands

### 1. Check Migration Status (Azure Database)
```sql
-- Check if migration exists
SELECT migration_id, product_version
FROM __EFMigrationsHistory
WHERE migration_id LIKE '%Newsletter%'
ORDER BY product_version DESC;

-- Check newsletter status values
SELECT status, COUNT(*)
FROM communications.newsletters
GROUP BY status;

-- Find invalid status newsletters
SELECT id, title, status, published_at, created_at
FROM communications.newsletters
WHERE status NOT IN ('Draft', 'Active', 'Inactive', 'Sent');
```

### 2. Fix Description Validation (Backend)
```csharp
// File: src/LankaConnect.Domain/Communications/ValueObjects/NewsletterDescription.cs
// Line 12: Change MaxLength
private const int MaxLength = 50000; // Was 5000
```

### 3. Check Published Newsletters (Landing Page)
```sql
-- Count active newsletters
SELECT COUNT(*)
FROM communications.newsletters
WHERE status = 'Active';

-- List active newsletters
SELECT id, title, published_at, expires_at
FROM communications.newsletters
WHERE status = 'Active'
ORDER BY published_at DESC;
```

### 4. Test API Endpoints
```bash
# Test published newsletters endpoint (should work without auth)
curl https://your-staging-url.azurewebsites.net/api/newsletters/published

# Test create newsletter (needs auth token)
curl -X POST https://your-staging-url.azurewebsites.net/api/newsletters \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "title": "Test Newsletter",
    "description": "Test description with at least 20 characters",
    "includeNewsletterSubscribers": true,
    "targetAllLocations": true,
    "emailGroupIds": []
  }'
```

---

## Implementation Priority

### Phase 1: IMMEDIATE (1 hour)
1. Verify Issue #1 migration deployed (**5 min**)
2. Fix Issue #3 description validation (**30 min**)
3. Test Issue #5 newsletter creation (**10 min**)
4. Test Issue #2 publishing (**10 min**)

### Phase 2: DATA QUALITY (30 min)
1. Check Issue #6 published newsletters (**10 min**)
2. Create test data if needed (**10 min**)
3. Verify landing page works (**10 min**)

### Phase 3: UX ENHANCEMENTS (2 hours)
1. Issue #8: TreeDropdown (**30 min**)
2. Issue #7: List layout (**1 hour**)
3. Issue #3: Proper character counter (**30 min**)

---

## Files to Modify

### Backend (C#)
1. `src/LankaConnect.Domain/Communications/ValueObjects/NewsletterDescription.cs`
   - Line 12: Increase MaxLength to 50000

### Database
1. Verify migration `20260114013838_Phase6A74Part9BC_FixInvalidNewsletterStatus` deployed

### Frontend (TypeScript/React) - OPTIONAL
1. `web/src/presentation/lib/validators/newsletter.schemas.ts`
   - Line 20: Update validation message to match backend

2. `web/src/presentation/components/features/newsletters/NewsletterForm.tsx`
   - Line 10: Replace MultiSelect with TreeDropdown (Issue #8)

3. `web/src/app/newsletters/page.tsx`
   - Line 222: Change grid layout to list (Issue #7)

---

## Success Criteria

### Phase 1 Complete When:
- [ ] All newsletters have valid status (Draft, Active, Inactive, Sent)
- [ ] Newsletter creation succeeds with minimal data
- [ ] Newsletter creation succeeds with embedded image
- [ ] Newsletter update succeeds
- [ ] Newsletter publishing succeeds (Draft → Active)
- [ ] No "Unknown" status badges appear

### Phase 2 Complete When:
- [ ] Landing page shows at least 1 published newsletter
- [ ] Landing page API endpoint returns data
- [ ] Filters work correctly (location, date, search)

### Phase 3 Complete When:
- [ ] Location selector uses TreeDropdown hierarchy
- [ ] Newsletter list shows one item per row
- [ ] Character counter accurately reflects HTML length
- [ ] Image embedding shows clear warning about space usage

---

## Support Information

### Documentation
- Full RCA: `NEWSLETTER_ISSUES_ROOT_CAUSE_ANALYSIS.md`
- Status fix plan: `PHASE_6A74_PART_9_UNKNOWN_STATUS_FIX_PLAN.md`
- Requirements: `Master Requirements Specification.md`

### Code References
- Domain: `src/LankaConnect.Domain/Communications/Entities/Newsletter.cs`
- Value Objects: `src/LankaConnect.Domain/Communications/ValueObjects/`
- Handlers: `src/LankaConnect.Application/Communications/Commands/`
- API: `src/LankaConnect.API/Controllers/NewslettersController.cs`
- Frontend: `web/src/presentation/components/features/newsletters/`

### Key Contacts
- Backend issues: Check Azure Application Insights logs
- Frontend issues: Check browser console and Network tab
- Database issues: Query Azure SQL staging database

---

## Common Error Messages

### "Only draft newsletters can be published"
- **Cause**: Newsletter status is not Draft
- **Fix**: Check Issue #1 - verify status values in database

### "Description must be less than 5000 characters"
- **Cause**: Base64 images in HTML content
- **Fix**: Issue #3 - increase MaxLength to 50000

### "Newsletter must have at least one recipient source"
- **Cause**: Business rule validation
- **Fix**: Ensure either emailGroupIds, includeNewsletterSubscribers=true, or eventId is set

### "Newsletter not found or not available"
- **Cause**: Create succeeded but redirect failed, or newsletter not in database
- **Fix**: Check create command logs, verify newsletter ID returned

---

## Next Steps

1. **User to provide**:
   - Azure backend logs during failed operations
   - Database access for verification queries
   - Confirmation that migration `20260114013838` deployed

2. **Developer actions**:
   - Execute Phase 1 fixes
   - Test end-to-end workflow
   - Deploy to staging
   - Verify all issues resolved

3. **Testing checklist**:
   - Create newsletter with plain text
   - Create newsletter with embedded image
   - Update newsletter content
   - Publish newsletter
   - Verify appears on landing page
   - Check status badge displays correctly

---

**Estimated Total Fix Time**: 3.5 hours
- Critical fixes: 1 hour
- Data verification: 30 minutes
- UX enhancements: 2 hours (optional)

**Deployment**: After Phase 1 complete (critical fixes)
