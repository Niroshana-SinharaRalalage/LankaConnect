# Quick Start: RCA Test for Newsletter Issue #1/#2

## ✅ Deployment Status: COMPLETE

**Commit**: `d227473b`
**Deployment**: Successful (completed 2026-01-19)
**Container Status**: Running
**Enhanced Logging**: Active

---

## 🎯 What to Do Now (3 Simple Steps)

### STEP 1: Create Test Newsletter

1. Open staging: https://lankaconnect-staging.salmoncoast-d18f1f6c.eastus.azurecontainerapps.io/dashboard/my-newsletters

2. Click "Create Newsletter"

3. Fill in:
   - **Title**: `RCA TEST - Database Update Diagnostic`
   - **Content**: `Testing comprehensive logging to diagnose Issue #1/#2`
   - **Email Groups**: Select both:
     - ✅ Cleveland SL Community
     - ✅ Test Group 1
   - **Event**: None (or Aurora if testing Issue #5)
   - **Include Newsletter Subscribers**: No

4. Click "Create & Send"

5. **COPY THE NEWSLETTER ID** from the response (looks like: `a1b2c3d4-e5f6-7890-abcd-ef1234567890`)

---

### STEP 2: Capture Logs Immediately

Open terminal and run:

```bash
cd c:/Work/LankaConnect

# Replace <newsletter-id> with the actual ID you copied
bash scripts/capture_newsletter_rca_logs.sh <newsletter-id>
```

**Example**:
```bash
bash scripts/capture_newsletter_rca_logs.sh a1b2c3d4-e5f6-7890-abcd-ef1234567890
```

The script will:
- ✅ Capture logs for 60 seconds
- ✅ Filter for relevant log entries
- ✅ Save to file: `newsletter_rca_logs_<id>_<timestamp>.txt`
- ✅ Show quick analysis of what was found

---

### STEP 3: Share Results

The script will show you a quick analysis. Look for these key indicators:

**✅ GOOD SIGNS**:
- "CommitAsync was called" → Database save was triggered
- "DIAG logs present" → EF Core commit process executed
- "Newsletter entity IS tracked" → Entity tracking working

**❌ BAD SIGNS**:
- "CommitAsync NOT called" → Job exited early
- "No DIAG logs" → Commit didn't reach AppDbContext
- "Newsletter entity NOT tracked" → **ENTITY DETACHED ISSUE** (this is the bug!)

Share the log file with me for detailed analysis.

---

## 🔍 Alternative: Manual Log Capture

If the script doesn't work, use this manual command:

```bash
NEWSLETTER_ID="<paste-your-id>"

az containerapp logs show \
  --name lankaconnect-api-staging \
  --resource-group lankaconnect-staging \
  --type console \
  --tail 300 \
  --follow true \
  | grep -E "$NEWSLETTER_ID|DIAG-|Phase 6A.74|UnitOfWork"
```

Press Ctrl+C after 60 seconds or when you see "NewsletterEmailJob COMPLETED".

---

## 📊 Check Database State

After capturing logs, verify database:

```sql
-- Check newsletter status
SELECT id, status, sent_at, created_at
FROM communications.newsletters
WHERE id = '<newsletter-id>';

-- Check if history was created (CRITICAL TEST)
SELECT *
FROM communications.newsletter_email_history
WHERE newsletter_id = '<newsletter-id>';

-- Check email group data
SELECT id, name, email_addresses, LENGTH(email_addresses) as addr_length
FROM communications.email_groups
WHERE name LIKE '%Cleveland SL Community%' OR name LIKE '%Test Group 1%';
```

---

## 🎯 Expected Results

### If Bug is Entity Detached (Most Likely)

**Logs will show**:
```
✅ CommitAsync was called
✅ DIAG logs present
❌ Newsletter entity NOT tracked
✅ NewsletterEmailHistory entity IS tracked
```

**Database will show**:
```sql
-- Newsletter NOT updated
status = 'Active'
sent_at = NULL

-- History might or might not exist
```

**Fix**: Add entity tracking in NewsletterEmailJob.cs after line 228

---

### If Bug is Early Exit

**Logs will show**:
```
✅ Job started
❌ CommitAsync NOT called
❌ No DIAG logs
```

**Need to find**: Which early exit was taken (check recipient count, MarkAsSent result, etc.)

---

## 📝 Notes

- Logs only kept for ~30 minutes in Azure
- Must capture immediately after newsletter send
- Script automatically saves logs to file
- If you receive the email, the job executed successfully (even if database not updated)

---

## ✅ Ready to Test!

Enhanced logging is deployed and active. Proceed with creating the test newsletter whenever you're ready.