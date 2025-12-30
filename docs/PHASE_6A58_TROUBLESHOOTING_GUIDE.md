# Phase 6A.58: Troubleshooting Guide - Database Column Naming Issues

**Date**: 2025-12-30
**Purpose**: Quick reference for diagnosing and fixing column naming errors
**Audience**: All developers working with raw SQL or EF Core

---

## Quick Diagnosis

### Symptom 1: "column [name] does not exist"

**Error Example**:
```
PostgreSQL Error 42703: column e.status does not exist
Hint: Perhaps you meant to reference the column "e.Status".
```

**Diagnosis**: Column name case mismatch

**Fix**: Use the column name suggested in the hint (usually with quotes):
```sql
-- WRONG:
WHERE e.status = 'Published'

-- CORRECT:
WHERE e."Status" = 'Published'
```

### Symptom 2: "operator does not exist: character varying = integer"

**Error Example**:
```
PostgreSQL Error: operator does not exist: character varying = integer
```

**Diagnosis**: Comparing enum string to integer value

**Fix**: Use string value instead of enum integer:
```csharp
// WRONG:
parameters.Add((int)EventStatus.Published);

// CORRECT:
parameters.Add(EventStatus.Published.ToString());
```

### Symptom 3: EF Core query works, raw SQL fails

**Diagnosis**: Raw SQL uses wrong column names

**Fix**: Check actual database schema:
```sql
SELECT column_name, data_type
FROM information_schema.columns
WHERE table_name = 'your_table'
ORDER BY ordinal_position;
```

---

## Step-by-Step Troubleshooting

### Step 1: Identify the Error

**Check Azure Container Logs**:
```bash
az containerapp logs show \
  --name lankaconnect-api-staging \
  --resource-group <resource-group> \
  --tail 100
```

**Look for**:
- PostgreSQL error code (e.g., 42703)
- Column name mentioned in error
- Hint message from PostgreSQL

### Step 2: Verify Database Schema

**Connect to PostgreSQL**:
```bash
psql "<connection_string>"
```

**Check table columns**:
```sql
-- List all columns in events table
SELECT column_name, data_type, is_nullable
FROM information_schema.columns
WHERE table_schema = 'events'
  AND table_name = 'events'
ORDER BY ordinal_position;
```

**Expected Output** (Phase 6A.58 current state):
```
column_name            | data_type                   | is_nullable
-----------------------|----------------------------|------------
Id                     | uuid                       | NO
title                  | character varying          | NO
description            | character varying          | NO
StartDate              | timestamp with time zone   | NO
EndDate                | timestamp with time zone   | NO
Status                 | character varying          | NO
Category               | character varying          | NO
...
```

### Step 3: Check Specific Column

**Test if column exists (lowercase)**:
```sql
SELECT column_name
FROM information_schema.columns
WHERE table_name = 'events'
  AND column_name = 'status';  -- lowercase
```

**If no results**: Column is NOT lowercase.

**Test if column exists (PascalCase)**:
```sql
SELECT column_name
FROM information_schema.columns
WHERE table_name = 'events'
  AND column_name = 'Status';  -- PascalCase
```

**If returns result**: Column IS PascalCase, must use quotes in SQL.

### Step 4: Test SQL Query Manually

**Test with lowercase (unquoted)**:
```sql
SELECT e."Id", e.title, e.status
FROM events.events e
LIMIT 1;
```

**If fails**: Column is NOT lowercase.

**Test with PascalCase (quoted)**:
```sql
SELECT e."Id", e.title, e."Status"
FROM events.events e
LIMIT 1;
```

**If succeeds**: Column IS PascalCase, use this syntax.

### Step 5: Verify Enum Storage

**Check how enum is stored**:
```sql
SELECT "Status", COUNT(*)
FROM events.events
GROUP BY "Status";
```

**Expected Output** (string storage):
```
Status    | count
----------|------
Draft     | 5
Published | 10
Cancelled | 2
```

**If you see integers** (0, 1, 2), enum is stored as integer (different fix needed).

### Step 6: Update Code

**Option A: Database has PascalCase columns**

```csharp
// Use quoted PascalCase
var sql = @"
    SELECT e.*
    FROM events.events e
    WHERE e.""Status"" = {0}
      AND e.""Category"" = {1}";

var parameters = new object[]
{
    EventStatus.Published.ToString(),  // String value
    EventCategory.Community.ToString()
};
```

**Option B: Database has snake_case columns**

```csharp
// Use unquoted snake_case
var sql = @"
    SELECT e.*
    FROM events.events e
    WHERE CAST(e.status AS text) = {0}
      AND CAST(e.category AS text) = {1}";

var parameters = new object[]
{
    EventStatus.Published.ToString(),  // String value
    EventCategory.Community.ToString()
};
```

Note: CAST may be needed if column type is enum or varchar.

---

## Common Mistakes and Fixes

### Mistake 1: Assuming PostgreSQL is Case-Insensitive

**Wrong Assumption**:
```sql
-- "PostgreSQL doesn't care about case"
SELECT * FROM Events WHERE Status = 'Published';
```

**Reality**:
```sql
-- PostgreSQL converts to lowercase unless quoted
SELECT * FROM Events WHERE Status = 'Published';
-- Actually becomes:
SELECT * FROM events WHERE status = 'Published';
-- Fails if column is "Status" (PascalCase)
```

**Fix**: Use quotes for mixed case:
```sql
SELECT * FROM events WHERE "Status" = 'Published';
```

### Mistake 2: Using Enum Integer Values

**Wrong**:
```csharp
parameters.Add((int)EventStatus.Published);  // 1
```

**SQL Result**:
```sql
WHERE e."Status" = 1
-- Error: operator does not exist: character varying = integer
```

**Fix**:
```csharp
parameters.Add(EventStatus.Published.ToString());  // "Published"
```

**SQL Result**:
```sql
WHERE e."Status" = 'Published'
-- Success!
```

### Mistake 3: Inconsistent Quoting

**Wrong**:
```sql
SELECT
    e."Status",    -- Quoted
    e.Category,    -- Not quoted
    e."StartDate"  -- Quoted
FROM events.events e;
```

**Result**: Inconsistent, hard to maintain

**Fix**: Be consistent:
```sql
-- Option A: Quote all PascalCase
SELECT
    e."Status",
    e."Category",
    e."StartDate"
FROM events.events e;

-- Option B: Use snake_case (if database has it)
SELECT
    e.status,
    e.category,
    e.start_date
FROM events.events e;
```

### Mistake 4: Forgetting Schema Prefix

**Wrong**:
```sql
SELECT * FROM events WHERE "Status" = 'Published';
```

**Error**: Ambiguous table reference (if multiple schemas)

**Fix**:
```sql
SELECT * FROM events.events WHERE "Status" = 'Published';
--            ^^^^^^^ schema.table
```

### Mistake 5: JSONB Property Access Case

**Wrong**:
```sql
-- JSONB properties are case-sensitive
WHERE e.ticket_price->>'amount' = 0  -- lowercase 'amount'
```

**Fix**:
```sql
-- Use exact property name from C#
WHERE e.ticket_price->>'Amount' = 0  -- PascalCase 'Amount'
```

---

## Testing Checklist

### Before Committing Code

- [ ] Build succeeds with 0 errors
- [ ] All unit tests pass
- [ ] Integration tests pass
- [ ] Manual API test succeeds locally

**Manual API Test**:
```bash
# Start API
dotnet run --project src/LankaConnect.API

# Test endpoint
curl -X GET "http://localhost:5000/api/events?searchTerm=test" \
  -H "Accept: application/json" \
  -v

# Expected: HTTP 200 with JSON response
```

### After Deployment

- [ ] GitHub Actions build succeeds
- [ ] Container deployment completes
- [ ] Container is running (not restarting)
- [ ] API endpoint returns HTTP 200
- [ ] No errors in Azure logs

**Verify Deployment**:
```bash
# Check container status
az containerapp show \
  --name lankaconnect-api-staging \
  --resource-group <resource-group> \
  --query "properties.runningStatus"

# Expected: "Running"

# Test API
curl -X GET "https://lankaconnect-api-staging.../api/events?searchTerm=test" \
  -H "Accept: application/json" \
  -v

# Expected: HTTP 200

# Check logs for errors
az containerapp logs show \
  --name lankaconnect-api-staging \
  --resource-group <resource-group> \
  --tail 50 | grep -i error

# Expected: No PostgreSQL errors
```

---

## Decision Tree

```
┌─────────────────────────────────────────┐
│ PostgreSQL Error 42703                  │
│ "column [name] does not exist"          │
└─────────────────┬───────────────────────┘
                  │
                  ▼
         ┌────────────────────┐
         │ Check hint message │
         └────────┬───────────┘
                  │
         ┌────────┴──────────┐
         │                   │
         ▼                   ▼
┌────────────────┐   ┌──────────────────┐
│ Hint suggests  │   │ No hint provided │
│ "Column.Name"  │   │                  │
└────────┬───────┘   └────────┬─────────┘
         │                    │
         ▼                    ▼
┌────────────────────┐   ┌─────────────────────┐
│ Use quoted version │   │ Run schema query    │
│ e."Column.Name"    │   │ to find column name │
└────────────────────┘   └────────┬────────────┘
                                  │
                         ┌────────┴────────┐
                         │                 │
                         ▼                 ▼
                  ┌──────────────┐   ┌─────────────┐
                  │ PascalCase   │   │ snake_case  │
                  │ "ColumnName" │   │ column_name │
                  └──────────────┘   └─────────────┘
                         │                  │
                         ▼                  ▼
                  ┌──────────────┐   ┌─────────────┐
                  │ Use quotes:  │   │ No quotes:  │
                  │ e."Status"   │   │ e.status    │
                  └──────────────┘   └─────────────┘
```

---

## PostgreSQL Case Sensitivity Reference

### Unquoted Identifiers

```sql
-- PostgreSQL converts to lowercase
SELECT Status FROM events;
-- Becomes:
SELECT status FROM events;
-- Only matches if column is "status" (lowercase)
```

### Quoted Identifiers

```sql
-- PostgreSQL preserves exact case
SELECT "Status" FROM events;
-- Looks for exact match: "Status"
-- Only matches if column is "Status" (PascalCase)
```

### Mixed Case Columns

```sql
-- If database has "EventDate" (PascalCase):

-- WRONG (fails):
SELECT EventDate FROM events;

-- CORRECT (succeeds):
SELECT "EventDate" FROM events;
```

### Standard Practice

**PostgreSQL Convention**: Use snake_case, no quotes needed
```sql
-- Recommended
SELECT event_date, event_status FROM events;
```

**Current LankaConnect State**: Mixed case, requires quotes
```sql
-- Current requirement
SELECT e."StartDate", e."Status", e.title FROM events.events e;
```

---

## Quick Reference Card

### Column Naming by Type

| Property Type | Current DB Column | SQL Reference |
|--------------|-------------------|---------------|
| Primary Key | `Id` | `e."Id"` |
| Enums | `Status`, `Category` | `e."Status"`, `e."Category"` |
| Dates | `StartDate`, `EndDate` | `e."StartDate"`, `e."EndDate"` |
| Value Objects | `title`, `description` | `e.title`, `e.description` |
| Nested VO | `address_street` | `e.address_street` |
| JSONB | `ticket_price`, `pricing` | `e.ticket_price` |
| Generated | `search_vector` | `e.search_vector` |
| Audit | `CreatedAt`, `UpdatedAt` | `e."CreatedAt"`, `e."UpdatedAt"` |

### Enum Comparison

```csharp
// ALWAYS use .ToString() for enum values in raw SQL
EventStatus.Published.ToString()  // "Published"
EventCategory.Community.ToString()  // "Community"

// NEVER use enum integer:
(int)EventStatus.Published  // 1 - WRONG!
```

---

## Helpful SQL Queries

### Find All Columns in Table

```sql
SELECT
    column_name,
    data_type,
    is_nullable,
    column_default
FROM information_schema.columns
WHERE table_schema = 'events'
  AND table_name = 'events'
ORDER BY ordinal_position;
```

### Find Columns with Mixed Case

```sql
SELECT column_name
FROM information_schema.columns
WHERE table_schema = 'events'
  AND table_name = 'events'
  AND column_name != lower(column_name);  -- Not all lowercase
```

### Test Column Case Sensitivity

```sql
-- Test lowercase
SELECT COUNT(*) FROM information_schema.columns
WHERE table_name = 'events' AND column_name = 'status';

-- Test PascalCase
SELECT COUNT(*) FROM information_schema.columns
WHERE table_name = 'events' AND column_name = 'Status';

-- If first query returns 0 and second returns 1,
-- column is PascalCase, needs quotes
```

### Check Enum Storage Format

```sql
-- See actual values in database
SELECT DISTINCT "Status" FROM events.events;

-- Expected (string storage):
-- Draft
-- Published
-- Cancelled

-- If you see numbers (0, 1, 2), enum stored as integer
```

---

## Emergency Rollback

### If Fix Makes Things Worse

**Step 1: Revert code**
```bash
git revert HEAD
git push origin develop
```

**Step 2: Monitor deployment**
```bash
# Wait for GitHub Actions
# Check container status
az containerapp show --name lankaconnect-api-staging ...
```

**Step 3: Verify rollback**
```bash
# Test API
curl -X GET "https://lankaconnect-api-staging.../api/events?searchTerm=test"

# Check logs
az containerapp logs show --name lankaconnect-api-staging ... --tail 50
```

### If Rollback Fails

**Emergency procedure**:
1. Stop container app temporarily
2. Review Azure deployment logs
3. Check database connectivity
4. Escalate to DevOps lead

---

## Getting Help

### Self-Service Resources

1. **Root Cause Analysis**: [PHASE_6A58_ROOT_CAUSE_ANALYSIS.md](./PHASE_6A58_ROOT_CAUSE_ANALYSIS.md)
2. **Fix Plan**: [PHASE_6A58_FIX_PLAN.md](./PHASE_6A58_FIX_PLAN.md)
3. **Prevention Strategy**: [PHASE_6A58_PREVENTION_STRATEGY.md](./PHASE_6A58_PREVENTION_STRATEGY.md)

### Escalation Path

1. **First**: Check this troubleshooting guide
2. **Second**: Review root cause analysis
3. **Third**: Ask in team chat with error details
4. **Fourth**: Escalate to tech lead with:
   - Error message
   - What you tried
   - Schema verification results

### Information to Provide

When asking for help, include:
- [ ] Exact error message from logs
- [ ] PostgreSQL error code (e.g., 42703)
- [ ] Result of schema verification query
- [ ] Code snippet of your SQL query
- [ ] What you've tried already

---

**Document Version**: 1.0
**Created**: 2025-12-30
**Last Updated**: 2025-12-30
**Purpose**: Quick reference for database column naming issues
