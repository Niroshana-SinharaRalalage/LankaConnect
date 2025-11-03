# Quick Reference: Event API Fix

**Last Updated**: 2025-11-03
**Status**: Emergency Procedure
**Time Required**: 2 hours

---

## Problem

- Container crashes on startup
- Error: `column "status" does not exist`
- Event APIs not visible in Swagger (0 endpoints, should be 20)

---

## Root Cause

**PostgreSQL column name case mismatch**:
- Migration created: `"Status"` (PascalCase)
- Migration references: `status` (lowercase)
- PostgreSQL case-sensitive → error

---

## Emergency Fix (Execute Now)

### Step 1: Connect to Staging Database (5 min)

```bash
az postgres flexible-server connect \
  --name lankaconnect-staging \
  --admin-user adminuser \
  --database-name lankaconnect
```

### Step 2: Verify Problem (2 min)

```sql
-- Check if Events table exists
SELECT table_name FROM information_schema.tables WHERE table_schema = 'events';

-- Check if Status column exists
SELECT column_name FROM information_schema.columns
WHERE table_schema = 'events' AND table_name = 'events' AND column_name = 'Status';

-- If no results, Status column is missing (PROBLEM CONFIRMED)
```

### Step 3: Drop Events Schema (10 min)

```sql
-- DESTRUCTIVE - NO UNDO - VERIFY NO PRODUCTION DATA FIRST
SELECT COUNT(*) FROM events.events;
-- If count > 0, STOP and escalate

-- Drop schema
DROP SCHEMA IF EXISTS events CASCADE;

-- Clean migration history
DELETE FROM "__EFMigrationsHistory"
WHERE "MigrationId" IN (
    '20251102000000_CreateEventsAndRegistrationsTables',
    '20251102061243_AddEventLocationWithPostGIS',
    '20251102144315_AddEventCategoryAndTicketPrice',
    '20251103040053_AddEventImages'
);

-- Verify clean state
SELECT "MigrationId" FROM "__EFMigrationsHistory" WHERE "MigrationId" LIKE '%Event%';
-- Expected: (0 rows)

\q
```

### Step 4: Redeploy Application (30 min)

```bash
# Trigger deployment via GitHub Actions
gh workflow run deploy-staging.yml --ref develop

# Monitor deployment
gh run watch

# Wait for completion (expected: 10-15 minutes)
```

### Step 5: Monitor Container Logs (15 min)

```bash
# Watch logs in real-time
az containerapp logs show \
  --name lankaconnect-api-staging \
  --resource-group lankaconnect-staging \
  --tail 100 \
  --follow

# Look for these SUCCESS messages:
# [INF] Applying database migrations...
# [INF] Applied migration '20251102061243_AddEventLocationWithPostGIS'
# [INF] Database migrations applied successfully
# [INF] LankaConnect API started successfully

# If you see ERROR, STOP and escalate
```

### Step 6: Verify Fix (10 min)

```bash
# 1. Health check
curl https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/health

# Expected: {"Status":"Healthy", ...}

# 2. Check Event endpoint count
curl https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/swagger/v1/swagger.json \
  | jq '.paths | keys | map(select(contains("event"))) | length'

# Expected: 20

# 3. Open Swagger UI in browser
# https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io

# Expected: "Events" section visible with 20 endpoints
```

### Step 7: Test Event Creation (10 min)

```bash
# Get auth token (use test user credentials)
TOKEN=$(curl -X POST \
  https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"email":"test@lankaconnect.com","password":"TestPassword123!","ipAddress":"127.0.0.1"}' \
  | jq -r '.token')

# Create test event
curl -X POST \
  https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api/events \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "title": "Test Event After Fix",
    "description": "Verifying Event API works",
    "startDate": "2025-12-01T18:00:00Z",
    "endDate": "2025-12-01T21:00:00Z",
    "capacity": 100,
    "category": "Community",
    "ticketPrice": {"amount": 0, "currency": "USD"}
  }'

# Expected: 201 Created with event ID
```

---

## Success Criteria

✅ **Fix Successful When**:
- Health check returns 200 OK
- Swagger shows 20 Event endpoints
- Event creation returns 201 Created
- No errors in container logs
- Container NOT crash looping

---

## Rollback Procedure (If Fix Fails)

```bash
# Restore previous deployment
PREVIOUS_COMMIT=$(gh run list --workflow=deploy-staging.yml --status=success --limit=2 --json headSha --jq '.[1].headSha')

gh workflow run deploy-staging.yml --ref $PREVIOUS_COMMIT

# Escalate to on-call engineer
```

---

## Common Issues

### Issue 1: Migration Still Fails

**Symptoms**: Container logs show same error after fix

**Solution**:
1. Verify Events schema actually dropped: `\dt events.*`
2. Verify migration history cleaned: `SELECT * FROM "__EFMigrationsHistory" WHERE "MigrationId" LIKE '%Event%'`
3. If entries remain, manually delete: `DELETE FROM "__EFMigrationsHistory" WHERE "MigrationId" = '...'`

### Issue 2: Health Check Returns 503

**Symptoms**: `/health` returns Service Unavailable

**Solution**:
1. Check database connection: `az postgres flexible-server show --name lankaconnect-staging`
2. Verify database firewall allows Azure Container Apps
3. Check connection string secret in Key Vault

### Issue 3: Swagger Shows 0 Endpoints

**Symptoms**: `/swagger` loads but no Event endpoints visible

**Solution**:
1. Check container logs for controller loading errors
2. Verify EventsController.cs compiled into image: `docker exec <container> ls /app/LankaConnect.API.dll`
3. Restart container: `az containerapp revision restart-revision --name lankaconnect-api-staging`

---

## Post-Fix Actions

### Immediate (Next 24 Hours)

- [ ] Monitor staging for errors
- [ ] Notify QA team for testing
- [ ] Update status dashboard
- [ ] Document what happened (post-mortem)

### Short-term (This Week)

- [ ] Fix migration column name case bugs
- [ ] Add CI/CD migration validation
- [ ] Implement schema health check
- [ ] Create migration code review checklist

### Long-term (This Month)

- [ ] Pre-deployment schema backups
- [ ] Migration dry-run automation
- [ ] Team training on PostgreSQL best practices
- [ ] Quarterly disaster recovery drill

---

## Contact Information

**Emergency Escalation**:
- On-Call: oncall@lankaconnect.com
- DBA Team: dba@lankaconnect.com
- DevOps: devops@lankaconnect.com

**Slack Channels**:
- #incidents (for emergency)
- #backend-team (for questions)
- #devops (for deployment issues)

---

## Related Documentation

1. **Full Analysis**: `docs/architectural-analysis-event-api-visibility.md`
2. **Step-by-Step Guide**: `docs/implementation-guide-event-api-fix.md`
3. **Architecture Diagrams**: `docs/diagrams/event-api-failure-c4-context.md`
4. **Architecture Decision**: `docs/adr/ADR-007-database-migration-safety.md`
5. **Executive Summary**: `docs/EXECUTIVE_SUMMARY_EVENT_API_FAILURE.md`

---

## Database Backup Script (Save Before Fix)

```sql
-- Save migration history
\copy (SELECT * FROM "__EFMigrationsHistory" ORDER BY "MigrationId") TO 'migration-history-backup.csv' WITH CSV HEADER;

-- Save Events data (if any)
\copy (SELECT * FROM events.events) TO 'events-backup.csv' WITH CSV HEADER;

-- Save Registrations data (if any)
\copy (SELECT * FROM events.registrations) TO 'registrations-backup.csv' WITH CSV HEADER;
```

---

## Quick Commands Cheatsheet

```bash
# Connect to staging database
az postgres flexible-server connect --name lankaconnect-staging --admin-user adminuser --database-name lankaconnect

# Trigger deployment
gh workflow run deploy-staging.yml --ref develop

# Watch deployment
gh run watch

# View container logs
az containerapp logs show --name lankaconnect-api-staging --resource-group lankaconnect-staging --tail 100 --follow

# Check health
curl https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/health

# Count Event endpoints
curl https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/swagger/v1/swagger.json | jq '.paths | keys | map(select(contains("event"))) | length'

# Restart container
az containerapp revision restart-revision --name lankaconnect-api-staging --resource-group lankaconnect-staging
```

---

## Decision Tree

```
Is container crash looping?
├─ YES → Check logs for "column status does not exist"
│  ├─ ERROR PRESENT → Execute emergency fix (this guide)
│  └─ DIFFERENT ERROR → Escalate to DevOps
└─ NO → Check Swagger endpoint count
   ├─ COUNT = 0 → Execute emergency fix (this guide)
   ├─ COUNT = 20 → FIXED! Proceed to testing
   └─ COUNT = other → Escalate to Backend team
```

---

**Remember**: This is an emergency procedure. Once fixed, implement long-term solutions to prevent recurrence.

**Approval Required**: Database drop is DESTRUCTIVE. Verify no production data before executing.

**Timeline**: Allow 2 hours for complete emergency fix execution.
