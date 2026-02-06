# Excel Export ZIP Bug - Immediate Fix

**Priority**: HIGH
**Time to Implement**: 5 minutes
**Time to Deploy**: 15 minutes
**Impact**: Fixes user-facing export feature

---

## The Problem (One Sentence)

Excel export returns a ZIP file that browsers treat as a single Excel file, causing double-decompression that exposes internal XML structure instead of `.xlsx` files.

---

## The Root Cause (Two Sentences)

1. **Stale Deployment**: Azure App Service is running old code without stream position fix (commits `3fcb1399`, `d163df2c`)
2. **Content-Type Override**: ASP.NET Core auto-detects XLSX MIME type from filename, overriding `application/zip` to `application/vnd.openxmlformats-officedocument.spreadsheetml.sheet`

---

## The Fix (Three Changes)

### Change 1: Force Redeploy ⚠️ **DO THIS FIRST**

```bash
# Trigger rebuild
git commit --allow-empty -m "chore: Force rebuild for Excel export fix"
git push origin develop

# Wait for deployment (5-10 min)

# Restart app service
az webapp restart --name lankaconnect-api --resource-group LankaConnect-RG

# Wait 2 minutes, then test
```

**Why**: Deploys correct code with stream position fix

---

### Change 2: Force Content-Type in Controller

**File**: `src/LankaConnect.API/Controllers/EventsController.cs`
**Line**: 1963-1967

**Current Code**:
```csharp
return File(
    result.Value.FileContent,
    result.Value.ContentType,  // ❌ Gets overridden to XLSX MIME type
    result.Value.FileName
);
```

**New Code**:
```csharp
return File(
    result.Value.FileContent,
    "application/zip",  // ✅ FORCE application/zip
    result.Value.FileName
);
```

**Why**: Prevents ASP.NET Core from auto-detecting XLSX MIME type

---

### Change 3: Remove "excel" from Filename

**File**: `src/LankaConnect.Application/Events/Queries/ExportEventAttendees/ExportEventAttendeesQueryHandler.cs`
**Line**: 155

**Current Code**:
```csharp
fileName = $"event-{request.EventId}-signup-lists-excel-{DateTime.UtcNow:yyyyMMdd-HHmmss}.zip";
```

**New Code**:
```csharp
fileName = $"event-{request.EventId}-signup-lists-{DateTime.UtcNow:yyyyMMdd-HHmmss}.zip";
```

**Why**: Prevents MIME type detection from filename hints

---

## Quick Test (After Deployment)

```bash
# 1. Download file
curl "https://api.lankaconnect.com/api/Events/0458806b-8672-4ad5-a7cb-f5346f1b282a/export?format=SignUpListsExcel" -o test.zip

# 2. Check Content-Type
curl -I "https://api.lankaconnect.com/api/Events/0458806b-8672-4ad5-a7cb-f5346f1b282a/export?format=SignUpListsExcel"
# Should show: Content-Type: application/zip

# 3. List ZIP contents
unzip -l test.zip
# Should show: Food-and-Drinks.xlsx and API-Test-Sign-Up-List.xlsx

# 4. Extract and open in Excel
unzip test.zip
# Open Food-and-Drinks.xlsx - should work without errors
```

**Success**: ZIP contains `.xlsx` files that open in Excel

---

## Complete Fix (Copy-Paste Ready)

### Step 1: Update Controller

```bash
# Open file
code src/LankaConnect.API/Controllers/EventsController.cs
```

Find line 1963-1967 and replace with:

```csharp
        return File(
            result.Value.FileContent,
            "application/zip",
            result.Value.FileName
        );
```

---

### Step 2: Update Query Handler

```bash
# Open file
code src/LankaConnect.Application/Events/Queries/ExportEventAttendees/ExportEventAttendeesQueryHandler.cs
```

Find line 155 and replace with:

```csharp
                fileName = $"event-{request.EventId}-signup-lists-{DateTime.UtcNow:yyyyMMdd-HHmmss}.zip";
```

---

### Step 3: Commit and Deploy

```bash
# Stage changes
git add src/LankaConnect.API/Controllers/EventsController.cs
git add src/LankaConnect.Application/Events/Queries/ExportEventAttendees/ExportEventAttendeesQueryHandler.cs

# Commit
git commit -m "fix(phase-6a73): Force application/zip Content-Type and remove 'excel' from filename

- Controller now explicitly sets Content-Type to 'application/zip' instead of trusting query handler
- Removed 'excel' from filename to prevent ASP.NET Core MIME type auto-detection
- This fixes bug where ZIP was treated as single XLSX file, causing double-decompression
- Fixes issue #[issue-number]"

# Push
git push origin develop

# Wait for deployment (check Azure DevOps pipeline)
# Restart app service after deployment
az webapp restart --name lankaconnect-api --resource-group LankaConnect-RG
```

---

## Verification Checklist

After deployment, verify these:

- [ ] Application Insights shows logs: `"Phase 6A.73: Starting Excel ZIP export"`
- [ ] HTTP response has `Content-Type: application/zip`
- [ ] ZIP file extracts to show `.xlsx` files (not XML)
- [ ] Excel opens files without errors
- [ ] Sheets are visible: "Mandatory Items", "Suggested Items", "Open Items"
- [ ] Data is correctly formatted
- [ ] CSV export still works (regression test)

---

## If It Still Doesn't Work

### Symptom: No logs in Application Insights
**Cause**: Deployment didn't complete
**Solution**: Check Azure DevOps pipeline, retry deployment

### Symptom: Logs appear, but ZIP still contains XML
**Cause**: Azure middleware is transforming response
**Solution**: Check `web.config` for URL rewrite rules, contact DevOps

### Symptom: Content-Type is still XLSX
**Cause**: IIS/Kestrel middleware override
**Solution**: Add custom middleware to lock Content-Type (see MIME_TYPE_OVERRIDE_ANALYSIS.md)

---

## Rollback (If Needed)

```bash
# Revert commits
git revert HEAD~2..HEAD --no-commit
git commit -m "revert: Rollback Excel export fix"
git push origin develop

# Restart app service
az webapp restart --name lankaconnect-api --resource-group LankaConnect-RG
```

---

## Time Estimate

| Task | Time |
|------|------|
| Make code changes | 2 minutes |
| Commit and push | 1 minute |
| Wait for deployment | 5-10 minutes |
| Restart app service | 1 minute |
| Test and verify | 5 minutes |
| **Total** | **15-20 minutes** |

---

## Related Documents

- **Full Root Cause Analysis**: `EXCEL_EXPORT_ZIP_BUG_ROOT_CAUSE_ANALYSIS.md` (70+ KB detailed analysis)
- **Action Plan**: `EXCEL_EXPORT_FIX_ACTION_PLAN.md` (step-by-step guide)
- **MIME Type Deep Dive**: `EXCEL_EXPORT_MIME_TYPE_OVERRIDE_ANALYSIS.md` (technical explanation)

---

## Support

**Questions**: Check full root cause analysis document
**Issues**: Create GitHub issue with "Excel Export" label
**Urgent**: Contact DevOps team for Azure App Service config review
