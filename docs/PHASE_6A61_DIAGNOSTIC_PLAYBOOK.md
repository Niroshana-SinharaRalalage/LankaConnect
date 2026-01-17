# Phase 6A.61 Event Notification - Diagnostic Playbook

**Purpose**: Quick reference guide for diagnosing event notification issues
**Status**: ACTIVE INCIDENT - recipientCount: 0 regression
**Created**: 2026-01-17

---

## Quick Diagnosis Commands

### 1. Check Hangfire Dashboard (FASTEST)

```
URL: https://lankaconnect-staging.azurewebsites.net/hangfire
```

**If dashboard is disabled**: Add to Program.cs and redeploy
```csharp
app.UseHangfireDashboard("/hangfire");
```

**What to look for**:
- Jobs in "Enqueued" queue → Worker not processing
- Jobs in "Failed" queue → Check exception details
- No jobs at all → Enqueue failing silently

---

### 2. Query Hangfire Database (DIRECT EVIDENCE)

```sql
-- Check recent jobs
SELECT
    j.id,
    j.createdat,
    j.invocationdata::text as job_details,
    s.name as current_state,
    s.createdat as state_time,
    s.data as state_data
FROM hangfire.job j
LEFT JOIN hangfire.state s ON j.stateid = s.id
WHERE j.createdat >= NOW() - INTERVAL '2 hours'
ORDER BY j.createdat DESC
LIMIT 20;

-- Check job state history
SELECT
    js.jobid,
    js.name as state_name,
    js.createdat as state_time,
    js.reason,
    js.data
FROM hangfire.jobstate js
WHERE js.createdat >= NOW() - INTERVAL '2 hours'
ORDER BY js.createdat DESC;

-- Check for failed jobs with exception details
SELECT
    j.id,
    j.createdat,
    j.invocationdata::text,
    s.name as state,
    s.data::json->'ExceptionDetails' as exception
FROM hangfire.job j
JOIN hangfire.state s ON j.stateid = s.id
WHERE s.name = 'Failed'
  AND j.createdat >= NOW() - INTERVAL '2 hours'
ORDER BY j.createdat DESC;
```

**Connection string**: Check Azure App Settings → `ConnectionStrings:DefaultConnection`

---

### 3. Check Azure Container Logs

```bash
# Real-time log streaming
az webapp log tail --name lankaconnect-staging --resource-group <resource-group>

# Historical logs (last 100 lines)
az webapp log download --name lankaconnect-staging --resource-group <resource-group> --log-file logs.zip

# Search for specific patterns
az webapp log tail --name lankaconnect-staging --resource-group <resource-group> | grep -E "DIAG-CMD-HANDLER|DIAG-NOTIF-JOB|Phase 6A.61|Hangfire"
```

**Expected logs if working**:
```
[Phase 6A.61] API: Sending event notification for event {EventId}
[DIAG-CMD-HANDLER] Hangfire job enqueued - JobId: {...}
[Phase 6A.61][{CorrelationId}] Starting event notification job...
[DIAG-NOTIF-JOB][{CorrelationId}] STARTING EMAIL SEND...
[DIAG-NOTIF-JOB][{CorrelationId}] Sending email 1/5 to: user@example.com
```

---

### 4. Check Event Email Groups (DATA VALIDATION)

```sql
-- Verify event has email groups
SELECT
    e.id as event_id,
    e.title,
    e.status,
    eeg.emailgroupid,
    eg.name as group_name,
    COUNT(egm.email) as member_count
FROM events.events e
LEFT JOIN events.event_email_groups eeg ON e.id = eeg.eventid
LEFT JOIN communications.email_groups eg ON eeg.emailgroupid = eg.id
LEFT JOIN communications.email_group_members egm ON eg.id = egm.emailgroupid
WHERE e.id = 'd543629f-a5ba-4475-b124-3d0fc5200f2f'
GROUP BY e.id, e.title, e.status, eeg.emailgroupid, eg.name;

-- Check confirmed registrations
SELECT
    r.id,
    r.userid,
    r.status,
    r.quantity,
    u.email
FROM events.registrations r
LEFT JOIN users.users u ON r.userid = u.id
WHERE r.eventid = 'd543629f-a5ba-4475-b124-3d0fc5200f2f'
  AND r.status = 'Confirmed';

-- Check newsletter subscribers (if event has location)
SELECT
    ns.email,
    ns.metroareaid,
    ns.subscriptiontype,
    ns.confirmed
FROM communications.newsletter_subscribers ns
WHERE ns.confirmed = true
ORDER BY ns.createdat DESC
LIMIT 20;
```

**Expected**: Event should have email groups OR registrations OR newsletter subscribers.

---

### 5. Check Notification History

```sql
-- Get notification history for event
SELECT
    enh.id,
    enh.eventid,
    enh.sentbyuserid,
    enh.sentat,
    enh.recipientcount,
    enh.successfulsends,
    enh.failedsends,
    enh.createdat,
    enh.updatedat
FROM events.event_notification_history enh
WHERE enh.eventid = 'd543629f-a5ba-4475-b124-3d0fc5200f2f'
ORDER BY enh.sentat DESC;
```

**Diagnosis**:
- `recipientcount = 0` AND `updatedat = createdat` → Job never executed
- `recipientcount > 0` AND `successfulsends = 0` → Job executed but emails failed
- `recipientcount > 0` AND `successfulsends > 0` → Working correctly

---

## Common Issues and Fixes

### Issue 1: recipientCount = 0, NO logs

**Root Cause**: Hangfire job not executing

**Diagnostic Steps**:
1. Check Hangfire dashboard → Are jobs enqueued?
2. Check Hangfire database → What is job state?
3. Check Azure logs → Is Hangfire server starting?

**Possible Fixes**:

#### Fix A: Hangfire Worker Not Running
```bash
# Check app service logs for Hangfire startup
az webapp log tail --name lankaconnect-staging | grep -i "hangfire server"

# Expected: "Starting Hangfire Server using job storage: 'PostgreSqlStorage'"
```

**If missing**: Hangfire server not starting. Check:
- `services.AddHangfireServer()` in DependencyInjection.cs (line 317)
- App service restarts successfully
- No startup exceptions

#### Fix B: Hangfire Database Connection Failed
```bash
# Test database connection
psql $DATABASE_URL -c "SELECT * FROM hangfire.server LIMIT 1;"

# Expected: List of Hangfire servers (at least 1)
```

**If empty**: No Hangfire workers registered. Check:
- Connection string is correct
- Database exists and is accessible
- Hangfire schema migrations ran successfully

#### Fix C: Job Serialization Failure
```sql
-- Check for jobs with deserialization errors
SELECT
    j.id,
    j.invocationdata,
    s.name,
    s.data::json->'ExceptionMessage' as error
FROM hangfire.job j
JOIN hangfire.state s ON j.stateid = s.id
WHERE s.name = 'Failed'
  AND s.data::text LIKE '%serializ%'
ORDER BY j.createdat DESC;
```

**If found**: EventNotificationEmailJob has non-serializable dependencies. Check:
- All constructor parameters are registered in DI
- No circular dependencies
- No stateful/singleton dependencies

---

### Issue 2: recipientCount > 0, successfulSends = 0

**Root Cause**: Job executed, but email sending failed

**Diagnostic Steps**:
1. Check logs for `[DIAG-NOTIF-JOB]` → See exact failure reason
2. Check email service configuration
3. Check Azure Communication Services status

**Possible Fixes**:

#### Fix A: Email Template Not Found
```sql
-- Check if event-details template exists
SELECT id, name, subject, active
FROM communications.email_templates
WHERE name = 'event-details';
```

**If missing**: Run migration to create event-details template
```bash
dotnet ef migrations add AddEventDetailsTemplate --project src/LankaConnect.Infrastructure
dotnet ef database update --project src/LankaConnect.Infrastructure
```

#### Fix B: Azure Communication Services Connection Failed
```bash
# Check app settings for email service
az webapp config appsettings list --name lankaconnect-staging | grep -E "Email|Azure"
```

**Required settings**:
- `Email:AzureCommunicationServices:ConnectionString`
- `Email:AzureCommunicationServices:SenderAddress`

#### Fix C: Invalid Email Addresses
```sql
-- Check for invalid email addresses in email groups
SELECT
    eg.name,
    egm.email,
    egm.email ~ '^[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Z|a-z]{2,}$' as is_valid
FROM communications.email_group_members egm
JOIN communications.email_groups eg ON egm.emailgroupid = eg.id
WHERE egm.email !~ '^[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Z|a-z]{2,}$';
```

---

### Issue 3: Logs appear, then stop suddenly

**Root Cause**: Exception thrown during email sending

**Diagnostic Steps**:
1. Check logs for exception stack traces
2. Check `[DIAG-NOTIF-JOB]` logs for last email sent
3. Check notification history for `failedsends` count

**Possible Fixes**:

#### Fix A: Null Reference on Event Properties
```csharp
// Common null reference errors:
// - @event.Title.Value → Title is null
// - @event.Location.Address.City → Location or Address is null
// - @event.TicketPrice.Amount → TicketPrice is null for free events
```

**Solution**: Already fixed in EventNotificationEmailJob.cs:
- Line 209: Null-safe `@event.Title?.Value ?? "Untitled Event"`
- Line 263-281: Defensive `GetEventLocationString()` method
- Line 214: Conditional `IsFree()` check before accessing TicketPrice

#### Fix B: Email Service Rate Limiting
```
[DIAG-NOTIF-JOB] Email 42/50 FAILED to: user@example.com, Error: Rate limit exceeded
```

**Solution**: Add retry logic with exponential backoff
```csharp
// In EventNotificationEmailJob.cs, wrap email send in Polly retry policy
var retryPolicy = Policy
    .Handle<HttpRequestException>()
    .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));

await retryPolicy.ExecuteAsync(async () =>
{
    await _emailService.SendTemplatedEmailAsync("event-details", email, templateData, cancellationToken);
});
```

---

## Emergency Fixes (Production Hotfixes)

### Hotfix 1: Bypass Hangfire (Synchronous Send)

**When to use**: Hangfire completely broken, need to send notifications NOW

**Implementation**:
```csharp
// Add new command: SendEventNotificationSyncCommand.cs
public class SendEventNotificationSyncCommandHandler : IRequestHandler<SendEventNotificationSyncCommand, Result<int>>
{
    public async Task<Result<int>> Handle(SendEventNotificationSyncCommand request, CancellationToken cancellationToken)
    {
        // 1. Resolve recipients (same as background job)
        var recipientResult = await _recipientService.ResolveRecipientsAsync(request.EventId, cancellationToken);

        // 2. Send emails SYNCHRONOUSLY (blocks API request)
        foreach (var email in recipientResult.EmailAddresses)
        {
            await _emailService.SendTemplatedEmailAsync("event-details", email, templateData, cancellationToken);
        }

        // 3. Update history immediately
        history.UpdateSendStatistics(recipientResult.EmailAddresses.Count, successCount, failedCount);
        await _unitOfWork.CommitAsync(cancellationToken);

        return Result<int>.Success(recipientResult.EmailAddresses.Count);
    }
}
```

**Pros**: Guaranteed execution, no Hangfire dependency
**Cons**: Slow API response (30-60 seconds for 50 emails), no retry on failure

---

### Hotfix 2: Manual SQL Update (Last Resort)

**When to use**: Notification already sent manually, just need to update history

```sql
-- Update history record manually
UPDATE events.event_notification_history
SET
    recipientcount = 5,
    successfulsends = 5,
    failedsends = 0,
    updatedat = NOW()
WHERE id = '2e39f57b-99bd-47ff-aee1-c21b54706109';
```

**Warning**: Only use if you **actually sent the emails** via another method.

---

## Proactive Monitoring

### Application Insights Queries

```kusto
// Alert: No job execution logs in last 5 minutes after enqueue
let enqueueLogs = traces
| where timestamp > ago(5m)
| where message contains "[DIAG-CMD-HANDLER] Hangfire job enqueued"
| project timestamp, jobId = extract("JobId: ([a-f0-9-]+)", 1, message);

let executionLogs = traces
| where timestamp > ago(5m)
| where message contains "[Phase 6A.61]" and message contains "Starting event notification job";

enqueueLogs
| join kind=leftanti executionLogs on $left.timestamp == $right.timestamp
| where isnotempty(jobId);

// Alert: High failure rate
traces
| where timestamp > ago(1h)
| where message contains "[DIAG-NOTIF-JOB]" and (message contains "FAILED" or message contains "EXCEPTION")
| summarize failureCount = count() by bin(timestamp, 5m)
| where failureCount > 5;
```

### Health Check Endpoint

```csharp
[HttpGet("health/event-notifications")]
public async Task<IActionResult> GetEventNotificationHealth()
{
    var checks = new
    {
        hangfireServer = CheckHangfireServer(),
        emailService = await CheckEmailServiceAsync(),
        database = await CheckDatabaseAsync(),
        recentFailures = await GetRecentFailuresAsync()
    };

    var isHealthy = checks.hangfireServer && checks.emailService && checks.database;

    return isHealthy
        ? Ok(checks)
        : StatusCode(500, checks);
}
```

---

## Rollback Plan

### If commit 8df1c378 needs to be reverted

```bash
# Create revert commit
git revert 8df1c378 --no-commit

# CRITICAL: DO NOT revert DI registration
# Only revert diagnostic logging if it's causing issues
git checkout HEAD -- src/LankaConnect.Infrastructure/DependencyInjection.cs

# Commit selective revert
git commit -m "revert(phase-6a61): Remove diagnostic logging only, keep DI registration"

# Push and deploy
git push origin develop
```

**Note**: DI registration (line 287) **MUST NOT** be reverted. It's required for Hangfire to instantiate the job.

---

## Escalation Path

1. **Level 1** (Self-service): Use this playbook
2. **Level 2** (DevOps): Check Hangfire dashboard + database queries
3. **Level 3** (Backend Developer): Review EventNotificationEmailJob.cs execution
4. **Level 4** (System Architect): Review full SPARC RCA document

**Contact**: See PHASE_6A61_REGRESSION_RCA.md for comprehensive analysis.

---

## Success Criteria

- [ ] Hangfire dashboard shows jobs in "Succeeded" state
- [ ] Azure logs show `[DIAG-NOTIF-JOB]` execution logs
- [ ] Notification history shows `recipientCount > 0`
- [ ] Notification history shows `successfulSends > 0`
- [ ] Email recipients receive event-details email
- [ ] History `updatedat` timestamp is AFTER `createdat`

---

**Document Version**: 1.0
**Last Updated**: 2026-01-17 17:45 UTC
**Status**: ACTIVE INCIDENT
