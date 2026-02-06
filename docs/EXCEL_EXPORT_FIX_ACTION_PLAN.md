# Excel Export ZIP Bug - Action Plan (Quick Reference)

**Status**: Ready for Execution
**Priority**: High
**Estimated Time**: 30-60 minutes

---

## TL;DR - Root Cause

**Primary Issue**: Deployed code is stale (old version without fixes)
**Secondary Issue**: ASP.NET Core auto-detecting XLSX MIME type and overriding `application/zip`

**Evidence**:
- ❌ No logs from added `_logger.LogInformation()` statements
- ❌ File size unchanged (10,867 bytes) despite multiple commits
- ❌ Content-Type mismatch: Code says `application/zip`, HTTP response says `application/vnd.openxmlformats-officedocument.spreadsheetml.sheet`

---

## Action Steps (Do in Order)

### Step 1: Verify Current Deployment ⚠️ **START HERE**

```bash
# 1. Check what commit is deployed
az webapp deployment source show \
  --name lankaconnect-api \
  --resource-group LankaConnect-RG \
  --query "id" -o tsv

# 2. Compare with latest Git commit
git rev-parse HEAD

# 3. Check recent commits
git log --oneline -5
```

**Expected**: Commits `3fcb1399` and `d163df2c` should be deployed
**If not deployed**: Proceed to Step 2

---

### Step 2: Force Rebuild and Deploy

```bash
# 1. Ensure you're on develop branch
git checkout develop
git pull origin develop

# 2. Trigger rebuild (empty commit forces deployment)
git commit --allow-empty -m "chore: Force rebuild to verify Excel export fix"
git push origin develop

# 3. Monitor Azure DevOps pipeline
# - Go to Azure DevOps: https://dev.azure.com/{org}/{project}/_build
# - Wait for build to complete (usually 5-10 minutes)

# 4. Verify deployment completed
az webapp deployment list \
  --name lankaconnect-api \
  --resource-group LankaConnect-RG \
  --output table

# 5. Restart app service to clear cache
az webapp restart \
  --name lankaconnect-api \
  --resource-group LankaConnect-RG
```

**Wait**: 2 minutes after restart before testing

---

### Step 3: Test for Logs (Verify Deployment Success)

```bash
# 1. Make API request to trigger export
curl -v \
  "https://api.lankaconnect.com/api/Events/0458806b-8672-4ad5-a7cb-f5346f1b282a/export?format=SignUpListsExcel" \
  -o test.zip \
  -H "Authorization: Bearer YOUR_TOKEN"

# 2. Check Azure Application Insights
# - Go to: Azure Portal > Application Insights > Logs
# - Run query:
```

```kusto
traces
| where timestamp > ago(10m)
| where message contains "Phase 6A.73"
| order by timestamp desc
| project timestamp, message
```

**Expected Logs**:
```
Phase 6A.73: Starting Excel ZIP export for event 0458806b-... - 2 signup lists
Phase 6A.73: Saved Excel workbook for signup list 'Food and Drinks' - XXXX bytes
Phase 6A.73: Added 'Food-and-Drinks.xlsx' to ZIP archive - XXXX bytes
Phase 6A.73: Saved Excel workbook for signup list 'API Test Sign Up List' - XXXX bytes
Phase 6A.73: Added 'API-Test-Sign-Up-List.xlsx' to ZIP archive - XXXX bytes
Phase 6A.73: Successfully created Excel ZIP archive for event 0458806b-... - YYYY bytes total
```

**If logs appear**: ✅ Deployment successful, proceed to Step 4
**If no logs**: ❌ Deployment failed, check Azure DevOps pipeline logs

---

### Step 4: Verify ZIP Structure

```bash
# 1. Extract downloaded ZIP
unzip -l test.zip

# Expected output:
# Archive:  test.zip
#   Length      Date    Time    Name
# ---------  ---------- -----   ----
#     XXXX  2026-01-12 10:00   Food-and-Drinks.xlsx
#     XXXX  2026-01-12 10:00   API-Test-Sign-Up-List.xlsx
# ---------                     -------
#     YYYY                     2 files

# 2. Extract files
unzip test.zip -d extracted/

# 3. Try opening in Excel (manual test)
# Windows: start excel "extracted/Food-and-Drinks.xlsx"
# Mac: open -a "Microsoft Excel" "extracted/Food-and-Drinks.xlsx"
```

**Success Criteria**:
- ✅ ZIP contains `.xlsx` files (NOT `xl/workbook.xml`)
- ✅ Excel opens files without errors
- ✅ Sheets visible: "Mandatory Items", "Suggested Items", "Open Items"
- ✅ Data is correctly formatted

**If ZIP still contains XML structure**: Proceed to Step 5

---

### Step 5: Apply Content-Type Fix (If Needed)

If logs appear (Step 3 passed) but ZIP still contains XML (Step 4 failed):

#### Edit Controller to Force Content-Type

**File**: `src/LankaConnect.API/Controllers/EventsController.cs`
**Line**: 1963-1967

**Current Code**:
```csharp
return File(
    result.Value.FileContent,
    result.Value.ContentType,  // ❌ This gets overridden to XLSX MIME type
    result.Value.FileName
);
```

**New Code**:
```csharp
return File(
    result.Value.FileContent,
    "application/zip",  // ✅ FORCE application/zip, ignore auto-detection
    result.Value.FileName
);
```

**Alternative Fix** (if above doesn't work):
```csharp
// Remove .xlsx hints from filename to prevent MIME type detection
var safeFileName = result.Value.FileName.Contains("excel")
    ? result.Value.FileName.Replace("excel", "archive")
    : result.Value.FileName;

return File(
    result.Value.FileContent,
    "application/zip",
    safeFileName
);
```

#### Deploy Fix

```bash
# 1. Commit change
git add src/LankaConnect.API/Controllers/EventsController.cs
git commit -m "fix(phase-6a73): Force application/zip Content-Type for Excel ZIP export"

# 2. Push and deploy
git push origin develop

# 3. Wait for deployment (5-10 minutes)

# 4. Restart app service
az webapp restart \
  --name lankaconnect-api \
  --resource-group LankaConnect-RG

# 5. Wait 2 minutes, then retest Step 4
```

---

### Step 6: Verify HTTP Response Headers

```bash
# Check Content-Type in HTTP response
curl -I \
  "https://api.lankaconnect.com/api/Events/0458806b-8672-4ad5-a7cb-f5346f1b282a/export?format=SignUpListsExcel" \
  -H "Authorization: Bearer YOUR_TOKEN"

# Look for:
# Content-Type: application/zip
# Content-Disposition: attachment; filename="event-...-signup-lists-excel-....zip"
```

**Success Criteria**:
- ✅ `Content-Type: application/zip` (NOT `application/vnd.openxmlformats-officedocument.spreadsheetml.sheet`)
- ✅ `Content-Disposition` has `.zip` filename

---

## Quick Diagnostics

### Problem: No logs in Application Insights
**Cause**: Old code is deployed
**Solution**: Run Step 2 (Force Rebuild and Deploy)

### Problem: Logs appear, but ZIP contains XML
**Cause**: Content-Type override in ASP.NET Core
**Solution**: Run Step 5 (Apply Content-Type Fix)

### Problem: Logs appear, Content-Type is correct, but ZIP still broken
**Cause**: Azure App Service middleware transforming response
**Solution**: Check `web.config` for URL rewrite rules or compression settings

### Problem: File size is still 10,867 bytes after redeployment
**Cause**: Build didn't pick up changes
**Solution**: Clear build cache, rebuild, redeploy

---

## Success Checklist

- [ ] Logs appear in Application Insights with "Phase 6A.73" messages
- [ ] Log shows 2 signup lists for test event
- [ ] HTTP response has `Content-Type: application/zip`
- [ ] ZIP extracts to show `.xlsx` files (not XML)
- [ ] Excel opens files without errors
- [ ] Sheets are visible with correct data
- [ ] File size is different from 10,867 bytes

---

## Rollback Plan (If Needed)

If deployment causes other issues:

```bash
# 1. Get previous commit hash
git log --oneline -10

# 2. Revert to previous version
git revert HEAD --no-commit
git commit -m "revert: Rollback Excel export fix"
git push origin develop

# 3. Wait for deployment

# 4. Restart app service
az webapp restart \
  --name lankaconnect-api \
  --resource-group LankaConnect-RG
```

---

## Testing Commands Reference

```bash
# Download file
curl "https://api.lankaconnect.com/api/Events/{eventId}/export?format=SignUpListsExcel" \
  -o test.zip \
  -H "Authorization: Bearer YOUR_TOKEN"

# List ZIP contents
unzip -l test.zip

# Extract ZIP
unzip test.zip -d extracted/

# View file signatures (should show PK for ZIP)
xxd test.zip | head

# Check if XLSX files are valid
file extracted/*.xlsx
# Should show: "Microsoft Excel 2007+"

# Open in Excel (Windows)
start excel "extracted/Food-and-Drinks.xlsx"

# Open in Excel (Mac)
open -a "Microsoft Excel" "extracted/Food-and-Drinks.xlsx"
```

---

## Environment Details

**Test Event ID**: `0458806b-8672-4ad5-a7cb-f5346f1b282a`
**API Endpoint**: `/api/Events/{id}/export?format=SignUpListsExcel`
**Expected Signup Lists**: 2 ("Food and Drinks", "API Test Sign Up List")
**Expected File Size**: > 10,867 bytes (current size indicates old code)

**Azure Resources**:
- App Service: `lankaconnect-api`
- Resource Group: `LankaConnect-RG`
- Application Insights: `lankaconnect-appinsights`

---

## Contacts

**If deployment fails**: Check Azure DevOps pipeline logs
**If logs don't appear**: Verify Application Insights connection string
**If bug persists**: Escalate to DevOps team for Azure App Service config review

---

## Related Documents

- **Full Root Cause Analysis**: `EXCEL_EXPORT_ZIP_BUG_ROOT_CAUSE_ANALYSIS.md`
- **Code Files**:
  - `src/LankaConnect.Infrastructure/Services/Export/ExcelExportService.cs` (line 47-152)
  - `src/LankaConnect.Application/Events/Queries/ExportEventAttendees/ExportEventAttendeesQueryHandler.cs` (line 151-156)
  - `src/LankaConnect.API/Controllers/EventsController.cs` (line 1963-1967)
