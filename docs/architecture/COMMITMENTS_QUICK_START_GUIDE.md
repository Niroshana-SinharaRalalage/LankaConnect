# Quick Start Guide: Fixing Missing Commitments

**Issue**: Rice Tay shows `committedQuantity: 2` but `commitments: []` (empty)
**Time Required**: 1-2 hours total
**Difficulty**: Easy

---

## TL;DR

1. Run diagnostic SQL to confirm root cause
2. Add 2 lines to `SignUpItemConfiguration.cs`
3. Test and deploy
4. (Optional) Run data repair if needed

---

## Step-by-Step Instructions

### Step 1: Run Diagnostic Queries (15 minutes)

**What**: Determine if commitments exist in database or if it's a data corruption issue

**How**:
1. Open `c:\Work\LankaConnect\scripts\diagnose-commitments.sql`
2. Connect to staging database
3. Run **Query 2** (Rice Tay Item State Analysis)
4. Check the results:

   ```
   actual_commitments_in_db | calculated_committed_qty | Diagnosis
   -------------------------|-------------------------|------------
   0                        | 2                       | Data corruption (go to Step 3)
   2                        | 2                       | EF Core bug (go to Step 2)
   ```

**Deliverable**: Know which scenario you're dealing with

---

### Step 2: Apply Code Fix (30 minutes)

**What**: Add EF Core backing field configuration

**File**: `src/LankaConnect.Infrastructure/Data/Configurations/SignUpItemConfiguration.cs`

**Change**: After line 67, add these 2 lines:

```csharp
// BEFORE (lines 64-67):
builder.HasMany(si => si.Commitments)
    .WithOne()
    .HasForeignKey(sc => sc.SignUpItemId)
    .OnDelete(DeleteBehavior.Cascade);

// AFTER (add lines 69-70):
builder.HasMany(si => si.Commitments)
    .WithOne()
    .HasForeignKey(sc => sc.SignUpItemId)
    .OnDelete(DeleteBehavior.Cascade);

builder.Navigation(si => si.Commitments)
    .UsePropertyAccessMode(PropertyAccessMode.Field);
```

**Build**:
```bash
dotnet build src/LankaConnect.Infrastructure/LankaConnect.Infrastructure.csproj
```

**Deliverable**: Code compiles successfully

---

### Step 3: Test Locally (15 minutes)

**What**: Verify fix works with existing data

**How**:
1. Start the API locally (or restart if already running)
2. Call the endpoint:
   ```bash
   GET https://localhost:5001/api/events/0458806b-8672-4ad5-a7cb-f5346f1b282a/signups
   ```
3. Check Rice Tay item in response:
   ```json
   {
       "id": "9dbce508-743a-4cfd-a222-0c3acafd8bbd",
       "itemDescription": "Rice Tay",
       "commitments": [ /* Should now be populated OR empty if no DB data */ ]
   }
   ```

**Expected Result**:
- If Step 1 showed `actual_commitments_in_db = 2`: Commitments array now has data ✅
- If Step 1 showed `actual_commitments_in_db = 0`: Commitments still empty (need Step 4)

**Deliverable**: API returns commitments correctly

---

### Step 4: Data Repair (Optional - If Step 1 showed 0 commitments)

**What**: Recalculate `remaining_quantity` to fix orphaned values

**SQL**:
```sql
-- Fix Rice Tay only
UPDATE sign_up_items
SET remaining_quantity = quantity - COALESCE(
    (SELECT SUM(quantity) FROM sign_up_commitments WHERE sign_up_item_id = sign_up_items.id),
    0
)
WHERE id = '9dbce508-743a-4cfd-a222-0c3acafd8bbd';
```

**Verify**:
```sql
SELECT
    item_description,
    quantity,
    remaining_quantity,
    (quantity - remaining_quantity) AS committed_qty
FROM sign_up_items
WHERE id = '9dbce508-743a-4cfd-a222-0c3acafd8bbd';
```

**Expected**: `committed_qty` should now be 0

**Test API again**: `committedQuantity` should now match commitments array

**Deliverable**: Data is consistent

---

### Step 5: System-Wide Audit (15 minutes)

**What**: Check if other items have the same issue

**SQL**: Run **Query 6** from `diagnose-commitments.sql`

**Expected**: Should return 0 rows (no discrepancies)

**If rows found**:
```sql
-- Repair all items at once
UPDATE sign_up_items si
SET remaining_quantity = si.quantity - COALESCE(
    (SELECT SUM(quantity) FROM sign_up_commitments WHERE sign_up_item_id = si.id),
    0
)
WHERE (si.quantity - si.remaining_quantity) != COALESCE(
    (SELECT SUM(quantity) FROM sign_up_commitments WHERE sign_up_item_id = si.id),
    0
);
```

**Deliverable**: All items have consistent data

---

### Step 6: Deploy to Staging (15 minutes)

**What**: Deploy code fix to staging environment

**Steps**:
1. Commit changes:
   ```bash
   git add src/LankaConnect.Infrastructure/Data/Configurations/SignUpItemConfiguration.cs
   git commit -m "fix: Add EF Core backing field config for SignUpItem.Commitments

   Fixes issue where commitments collection was not loading due to missing
   PropertyAccessMode.Field configuration. Pattern matches SignUpListConfiguration."
   ```

2. Push to staging branch:
   ```bash
   git push origin develop
   ```

3. Wait for CI/CD deployment

4. Test staging API endpoint

**Deliverable**: Staging shows commitments correctly

---

### Step 7: Production Deployment (30 minutes)

**What**: Deploy to production with data repair (if needed)

**Pre-deployment**:
- [ ] Backup production database
- [ ] Run diagnostic queries on production
- [ ] Prepare data repair script (if needed)

**Deployment**:
1. Merge to master and deploy code fix
2. Verify API returns commitments
3. If needed, run data repair script on production
4. Monitor logs for 1 hour

**Post-deployment**:
- [ ] User confirms issue resolved
- [ ] No errors in logs
- [ ] All events show correct commitment data

**Deliverable**: Production is fixed

---

## Troubleshooting

### Issue: Code won't compile

**Solution**: Check that you added the lines in the correct location (after line 67, inside the `Configure` method)

### Issue: Commitments still empty after code fix

**Possible causes**:
1. Step 1 showed `actual_commitments_in_db = 0` → Need Step 4 (data repair)
2. API cached the response → Clear cache or wait
3. Code fix not deployed → Restart API

### Issue: committedQuantity still wrong after fix

**Cause**: Data corruption (Scenario A)
**Solution**: Run Step 4 (data repair)

### Issue: Multiple items affected

**Cause**: System-wide data corruption
**Solution**: Run Step 5 (system-wide audit and repair)

---

## Verification Checklist

After completing all steps:

- [ ] Rice Tay item shows commitments (or empty array with committedQuantity = 0)
- [ ] `committedQuantity` matches `commitments.sum(c => c.Quantity)` for all items
- [ ] No EF Core errors in logs
- [ ] User confirms they can see who committed to items
- [ ] System-wide audit shows no discrepancies

---

## Rollback (If Needed)

If something goes wrong:

1. Revert code change:
   ```bash
   git revert <commit-hash>
   git push
   ```

2. If data repair was applied, restore from backup:
   ```sql
   -- Restore sign_up_items table from backup
   ```

3. Restart API

---

## Success Criteria

**You're done when**:
1. ✅ API returns commitments array with user data
2. ✅ Contact names, emails, phones are visible
3. ✅ `committedQuantity` matches actual commitment count
4. ✅ User confirms issue is resolved
5. ✅ No other items have the same issue

---

## Files Modified

- **Code**: `src/LankaConnect.Infrastructure/Data/Configurations/SignUpItemConfiguration.cs` (2 lines added)
- **Database**: `sign_up_items` table (if data repair was needed)

---

## Time Breakdown

- Diagnostic queries: 15 min
- Code fix: 30 min
- Local testing: 15 min
- Data repair (if needed): 15 min
- System-wide audit: 15 min
- Staging deployment: 15 min
- Production deployment: 30 min

**Total**: 1-2 hours (depending on whether data repair is needed)

---

## Reference Documents

- **Executive Summary**: `docs/architecture/COMMITMENTS_MISSING_EXECUTIVE_SUMMARY.md`
- **Root Cause Analysis**: `docs/architecture/ROOT_CAUSE_ANALYSIS_COMMITMENTS_MISSING.md`
- **Proposed Code Fix**: `docs/architecture/COMMITMENTS_FIX_PROPOSED_CODE.md`
- **Diagnostic SQL**: `scripts/diagnose-commitments.sql`

---

**Questions?** Refer to the executive summary for detailed explanation.

**Ready to start?** Begin with Step 1 (run diagnostic queries).
