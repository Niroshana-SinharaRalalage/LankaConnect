# Phase 6A.45 Attendee Management - Deployment & Testing Plan

**Created**: 2025-12-24
**Phase**: 6A.45 - Attendee Management & Export
**Target Environment**: Azure Staging → Production
**Deployment Type**: Backend + Frontend (Full Stack)

---

## Pre-Deployment Checklist

### Code Verification
- [ ] **Build Status**: Backend compiles with 0 errors, 0 warnings
- [ ] **Build Status**: Frontend compiles with 0 TypeScript errors
- [ ] **Git Status**: All changes committed to feature branch
- [ ] **Code Review**: At least one peer review completed
- [ ] **Tests Pass**: All existing unit tests pass
- [ ] **Linting**: No linting errors in modified files

### Documentation Review
- [ ] **RCA Document**: `PHASE_6A45_ATTENDEE_MANAGEMENT_RCA.md` completed
- [ ] **Issue Tracking**: All 7 issues documented with status
- [ ] **API Changes**: No breaking changes to endpoints
- [ ] **Database Changes**: No migrations required

### Stakeholder Approvals
- [ ] **Business Decision**: Hard delete vs soft delete approved
- [ ] **Product Owner**: Fixes align with user requirements
- [ ] **Security Review**: Data export authorization reviewed
- [ ] **Compliance**: GDPR implications of export feature reviewed

---

## Deployment Steps

### Phase 1: Pre-Deployment Testing (Local/Dev Environment)

#### Step 1.1: Backend Local Testing
```bash
# Navigate to backend project
cd c:\Work\LankaConnect

# Restore dependencies
dotnet restore

# Build solution
dotnet build --configuration Release

# Run unit tests
dotnet test

# Start backend API locally
dotnet run --project src/LankaConnect.WebApi

# Expected output:
# Now listening on: https://localhost:5001
# Application started. Press Ctrl+C to shut down.
```

**Verification**:
- API starts without errors
- Swagger UI accessible at https://localhost:5001/swagger
- Health check endpoint returns 200 OK

#### Step 1.2: Frontend Local Testing
```bash
# Navigate to frontend project
cd c:\Work\LankaConnect\web

# Install dependencies
npm install

# Run type checking
npm run typecheck

# Run linting
npm run lint

# Build production bundle
npm run build

# Start development server
npm run dev

# Expected output:
# Local: http://localhost:3000
# Ready in 2.5s
```

**Verification**:
- No TypeScript errors
- Build completes successfully
- Development server starts
- No console errors in browser

#### Step 1.3: Integration Testing (Local)
**Test Scenario 1: Duplicate Registration Prevention**
```bash
# 1. Create test event (via UI or API)
POST /api/events
{
  "title": "Test Event - Phase 6A.45",
  "description": "Testing duplicate registration",
  "startDate": "2025-12-30T10:00:00Z",
  "endDate": "2025-12-30T18:00:00Z",
  "capacity": 100,
  "pricing": { "adultPrice": 0, "type": "Single" }
}

# 2. Register with email (anonymous)
POST /api/events/{eventId}/register-anonymous
{
  "attendees": [
    { "name": "John Doe", "ageCategory": 1 }
  ],
  "contact": {
    "email": "testuser@example.com",
    "phoneNumber": "+1234567890"
  }
}
# Expected: 200 OK

# 3. Try to register again with SAME email
POST /api/events/{eventId}/register-anonymous
{
  "attendees": [
    { "name": "Jane Doe", "ageCategory": 1 }
  ],
  "contact": {
    "email": "testuser@example.com",  # Same email
    "phoneNumber": "+1234567890"
  }
}
# Expected: 400 Bad Request
# Error: "This email is already registered for this event. Each email can only register once."
```

**Test Scenario 2: Gender Distribution Formatting**
```bash
# 1. Register with multiple attendees
POST /api/events/{eventId}/register-anonymous
{
  "attendees": [
    { "name": "John Doe", "ageCategory": 1, "gender": 1 },    # Male Adult
    { "name": "Jane Doe", "ageCategory": 1, "gender": 2 },    # Female Adult
    { "name": "Bob Smith", "ageCategory": 1, "gender": 1 }    # Male Adult
  ],
  "contact": {
    "email": "family@example.com",
    "phoneNumber": "+1234567890"
  }
}

# 2. Fetch attendees
GET /api/events/{eventId}/attendees

# Expected response includes:
{
  "genderDistribution": "2 Male, 1 Female"  // NOT "2M, 1F"
}
```

**Test Scenario 3: Badge Display (Frontend)**
```javascript
// 1. Navigate to event management page
// http://localhost:3000/events/{eventId}/manage

// 2. Switch to "Attendee Management" tab

// 3. Verify badge labels (NOT numbers):
// - Status badges: "Confirmed", "Pending", NOT "1", "0"
// - Payment badges: "N/A", "Completed", NOT "4", "1"

// 4. Check browser console for warnings:
// Should see NO "Invalid registration status" warnings
```

**Test Scenario 4: Additional Attendees Formatting (Frontend)**
```javascript
// 1. Register with 5 attendees (1 main + 4 additional)

// 2. View attendee list

// 3. Verify "Additional Attendees" column shows:
// • Jane Doe
// • Bob Smith
// • Alice Johnson
// • Charlie Brown

// NOT: "Jane Doe, Bob Smith, Alice Johnson, Charlie Brown"
```

**Test Scenario 5: Payment Status for Free Events**
```javascript
// 1. Create FREE event (no pricing)

// 2. Register attendees

// 3. View attendee management tab

// 4. Verify Payment Status column shows:
// - Plain text "N/A" (NOT badge)
// - Color: neutral gray (NOT green/red/amber)
```

---

### Phase 2: Azure Staging Deployment

#### Step 2.1: Backend Deployment
```bash
# Option A: Azure App Service Deployment via GitHub Actions
git checkout develop
git pull origin develop
git push origin develop  # Triggers CI/CD pipeline

# Option B: Manual Deployment via Azure CLI
az login
az account set --subscription "LankaConnect-Subscription"

# Build and publish
dotnet publish src/LankaConnect.WebApi -c Release -o ./publish

# Deploy to staging slot
az webapp deployment source config-zip \
  --resource-group LankaConnect-RG \
  --name lankaconnect-api-staging \
  --src ./publish.zip
```

**Verification**:
```bash
# Check deployment status
az webapp deployment list \
  --resource-group LankaConnect-RG \
  --name lankaconnect-api-staging \
  --query "[0].{status:status, deployer:deployer, start:startTime}"

# Test health endpoint
curl https://lankaconnect-api-staging.azurewebsites.net/health
# Expected: {"status":"Healthy","version":"1.0.0"}

# Test Swagger UI
# Open: https://lankaconnect-api-staging.azurewebsites.net/swagger
# Verify: Endpoints visible and Swagger loads
```

#### Step 2.2: Frontend Deployment
```bash
# Navigate to frontend
cd c:\Work\LankaConnect\web

# Build production bundle
npm run build

# Deploy to Azure Static Web Apps (or App Service)
# Option A: Via GitHub Actions (automatic on push to develop)
git push origin develop

# Option B: Manual deployment via Azure CLI
az staticwebapp deploy \
  --name lankaconnect-web-staging \
  --resource-group LankaConnect-RG \
  --source ./out
```

**Verification**:
```bash
# Test frontend URL
curl -I https://staging.lankaconnect.com
# Expected: HTTP/2 200

# Open in browser
# https://staging.lankaconnect.com
# Verify: Page loads, no console errors
```

#### Step 2.3: Database Verification (Azure PostgreSQL)
```sql
-- Connect to staging database
psql -h lankaconnect-db-staging.postgres.database.azure.com \
     -U adminuser@lankaconnect-db-staging \
     -d lankaconnect_staging

-- Verify schema (no changes expected)
\d registrations
-- Expected: Existing columns, no new migrations

-- Check for test data (cleanup if needed)
SELECT COUNT(*) FROM registrations WHERE contact_email LIKE '%@example.com';
-- If > 0: DELETE FROM registrations WHERE contact_email LIKE '%@example.com';
```

---

### Phase 3: Staging Environment Testing

#### Test Suite 1: API Testing (Postman/curl)

**Test 1.1: Duplicate Registration Prevention (Authenticated User)**
```bash
# Setup: Login as test user
POST https://staging.lankaconnect.com/api/auth/login
{
  "email": "testuser@lankaconnect.com",
  "password": "TestPass123!"
}
# Save access_token

# Create test event
POST https://staging.lankaconnect.com/api/events
Authorization: Bearer {access_token}
{
  "title": "Staging Test Event",
  "description": "Testing Phase 6A.45 fixes",
  "startDate": "2025-12-30T10:00:00Z",
  "endDate": "2025-12-30T18:00:00Z",
  "capacity": 50,
  "pricing": { "adultPrice": { "amount": 0, "currency": "USD" }, "type": 0 }
}
# Save eventId

# Register (first time - should succeed)
POST https://staging.lankaconnect.com/api/events/{eventId}/register
Authorization: Bearer {access_token}
{
  "attendees": [
    { "name": "Test User", "ageCategory": 1 }
  ],
  "contact": {
    "email": "testuser@lankaconnect.com",
    "phoneNumber": "+1234567890"
  }
}
# Expected: 200 OK, registration successful

# Try to register again (should FAIL)
POST https://staging.lankaconnect.com/api/events/{eventId}/register
Authorization: Bearer {access_token}
{
  "attendees": [
    { "name": "Test User 2", "ageCategory": 1 }
  ],
  "contact": {
    "email": "testuser@lankaconnect.com",
    "phoneNumber": "+1234567890"
  }
}
# Expected: 400 Bad Request
# Error: "You are already registered for this event. To change your registration details, please cancel your current registration first."
```

**Test 1.2: Duplicate Registration Prevention (Anonymous User)**
```bash
# Register anonymously (first time - should succeed)
POST https://staging.lankaconnect.com/api/events/{eventId}/register-anonymous
{
  "attendees": [
    { "name": "Anonymous User 1", "ageCategory": 1 }
  ],
  "contact": {
    "email": "anonymous@example.com",
    "phoneNumber": "+9876543210"
  }
}
# Expected: 200 OK

# Try to register with SAME email (should FAIL)
POST https://staging.lankaconnect.com/api/events/{eventId}/register-anonymous
{
  "attendees": [
    { "name": "Anonymous User 2", "ageCategory": 1 }
  ],
  "contact": {
    "email": "anonymous@example.com",  # Same email, different case
    "phoneNumber": "+9876543210"
  }
}
# Expected: 400 Bad Request
# Error: "This email is already registered for this event. Each email can only register once."

# Try with case-insensitive email (should FAIL)
POST https://staging.lankaconnect.com/api/events/{eventId}/register-anonymous
{
  "attendees": [
    { "name": "Anonymous User 3", "ageCategory": 1 }
  ],
  "contact": {
    "email": "ANONYMOUS@EXAMPLE.COM",  # Same email, uppercase
    "phoneNumber": "+9876543210"
  }
}
# Expected: 400 Bad Request (case-insensitive check)
```

**Test 1.3: Cancel & Re-register Flow**
```bash
# 1. Register
POST https://staging.lankaconnect.com/api/events/{eventId}/register-anonymous
{
  "attendees": [{ "name": "User X", "ageCategory": 1 }],
  "contact": { "email": "userx@example.com", "phoneNumber": "+1111111111" }
}
# Expected: 200 OK

# 2. Cancel registration
DELETE https://staging.lankaconnect.com/api/events/{eventId}/rsvp
Authorization: Bearer {access_token}
{
  "deleteSignUpCommitments": false
}
# Expected: 200 OK (hard delete)

# 3. Verify registration deleted from database
GET https://staging.lankaconnect.com/api/events/{eventId}/attendees
Authorization: Bearer {access_token}
# Expected: attendees list does NOT include userx@example.com

# 4. Re-register with SAME email (should SUCCEED)
POST https://staging.lankaconnect.com/api/events/{eventId}/register-anonymous
{
  "attendees": [{ "name": "User X Again", "ageCategory": 1 }],
  "contact": { "email": "userx@example.com", "phoneNumber": "+1111111111" }
}
# Expected: 200 OK (allowed after deletion)
```

**Test 1.4: Gender Distribution Formatting**
```bash
# Register with mixed genders
POST https://staging.lankaconnect.com/api/events/{eventId}/register-anonymous
{
  "attendees": [
    { "name": "Male 1", "ageCategory": 1, "gender": 1 },
    { "name": "Male 2", "ageCategory": 1, "gender": 1 },
    { "name": "Female 1", "ageCategory": 1, "gender": 2 },
    { "name": "Female 2", "ageCategory": 1, "gender": 2 },
    { "name": "Female 3", "ageCategory": 1, "gender": 2 }
  ],
  "contact": { "email": "mixedgroup@example.com", "phoneNumber": "+2222222222" }
}

# Fetch attendees
GET https://staging.lankaconnect.com/api/events/{eventId}/attendees
Authorization: Bearer {access_token}

# Verify response:
{
  "attendees": [
    {
      "registrationId": "...",
      "genderDistribution": "2 Male, 3 Female",  // ✓ Correct format
      // NOT "2M, 3F"
      ...
    }
  ]
}
```

**Test 1.5: Excel Export (API Level)**
```bash
# Export attendees as Excel
POST https://staging.lankaconnect.com/api/events/{eventId}/export
Authorization: Bearer {access_token}
Content-Type: application/json
{
  "format": "excel"
}

# Save response to file
--output staging-export.xlsx

# Verify file integrity
file staging-export.xlsx
# Expected: "Microsoft Excel 2007+"

# Check file size
ls -lh staging-export.xlsx
# Expected: > 0 bytes (not empty)

# Inspect ZIP structure (Excel is ZIP-based)
unzip -l staging-export.xlsx
# Expected:
# - xl/workbook.xml
# - xl/worksheets/sheet1.xml
# - xl/worksheets/sheet2.xml (if signup lists exist)
# - [Content_Types].xml

# Try opening file
# Option 1: Excel (Windows)
start staging-export.xlsx

# Option 2: LibreOffice (Linux/Mac)
libreoffice staging-export.xlsx

# Verification:
# - File opens without error
# - Sheet 1: "Registrations" tab exists
# - Headers: Registration ID, Main Attendee, Additional Attendees, etc.
# - Data rows populated
# - Gender Distribution shows "2 Male, 3 Female" (NOT "2M, 3F")
```

**Test 1.6: CSV Export (API Level)**
```bash
# Export attendees as CSV
POST https://staging.lankaconnect.com/api/events/{eventId}/export
Authorization: Bearer {access_token}
Content-Type: application/json
{
  "format": "csv"
}

# Save response to file
--output staging-export.csv

# Verify file content
cat staging-export.csv
# Expected:
# Line 1: Headers (RegistrationId,MainAttendee,AdditionalAttendees,...)
# Line 2+: Data rows

# Count lines
wc -l staging-export.csv
# Expected: 1 header + N data rows

# Check encoding
file staging-export.csv
# Expected: "UTF-8 Unicode text"

# Open in Excel
# Verify:
# - CSV imports correctly
# - Gender distribution NOT interpreted as formula
# - Special characters render correctly
```

#### Test Suite 2: UI Testing (Manual - Browser)

**Test 2.1: Attendee Management Tab - Badge Display**
```
1. Login to https://staging.lankaconnect.com
2. Navigate to Events → [Your Event] → Manage Event
3. Click "Attendee Management" tab
4. Verify:
   ✓ Status badges show labels: "Confirmed", "Pending", "Checked In"
   ✗ Status badges do NOT show numbers: "1", "0", "3"
   ✓ Payment badges show labels: "Completed", "N/A", "Pending"
   ✗ Payment badges do NOT show numbers: "1", "4", "0"
5. Open browser console (F12)
6. Verify NO console warnings:
   ✗ "Invalid registration status: 1"
   ✗ "Unknown payment status value: 4"
```

**Test 2.2: Additional Attendees Formatting**
```
1. Navigate to Attendee Management tab
2. Find registration with multiple attendees (4+ people)
3. Verify "Additional Attendees" column shows:
   ✓ Bullet list format:
     • Attendee 2 Name
     • Attendee 3 Name
     • Attendee 4 Name
   ✗ NOT comma-separated: "Attendee 2, Attendee 3, Attendee 4"
4. Hover over truncated names (if any)
5. Verify full name appears in tooltip
```

**Test 2.3: Payment Status for Free Events**
```
1. Navigate to FREE event (no pricing configured)
2. View Attendee Management tab
3. Find "Payment Status" column
4. Verify:
   ✓ Shows plain text "N/A" (neutral gray color)
   ✗ Does NOT show badge with "N/A"
   ✓ Amount column shows "—" (em dash)
```

**Test 2.4: Export Excel (Browser Download)**
```
1. Navigate to Attendee Management tab
2. Click "Export Excel" button (orange)
3. Wait for download to complete
4. Verify:
   ✓ File downloads successfully
   ✓ Filename format: event-{guid}-attendees-{timestamp}.xlsx
   ✓ File size > 0 bytes
5. Open downloaded file in Excel
6. Verify:
   ✓ File opens without error
   ✗ NO "File format or extension is not valid" error
   ✓ Sheet 1 "Registrations" exists
   ✓ Headers match expected columns
   ✓ Data rows populated
   ✓ Gender Distribution shows "2 Male, 1 Female"
   ✗ NO Excel formula errors (#REF!, #NAME?)
7. If event has signup lists:
   ✓ Sheet 2 "Mandatory Items" exists (if applicable)
   ✓ Sheet 3 "Suggested Items" exists (if applicable)
   ✓ Sheet 4 "Open Items" exists (if applicable)
```

**Test 2.5: Export CSV (Browser Download)**
```
1. Navigate to Attendee Management tab
2. Click "Export CSV" button (white outline)
3. Wait for download to complete
4. Verify:
   ✓ File downloads successfully
   ✓ Filename format: event-{guid}-attendees-{timestamp}.csv
   ✓ File size > 0 bytes
5. Open CSV in text editor
6. Verify:
   ✓ Line 1: Header row exists
   ✓ Line 2+: Data rows exist
   ✗ NOT just headers with no data
7. Open CSV in Excel
8. Verify:
   ✓ Imports correctly
   ✓ Gender Distribution NOT interpreted as formula
   ✓ Special characters render correctly
```

**Test 2.6: Loading & Error States**
```
1. Navigate to Attendee Management tab
2. While loading:
   ✓ Shows skeleton loading state (pulsing gray boxes)
   ✗ No "undefined" or error messages
3. For event with NO registrations:
   ✓ Shows empty state: "No Registrations Yet"
   ✓ Export buttons disabled
4. For network error:
   ✓ Shows error message: "Failed to Load Attendees"
   ✓ Shows error details (if available)
```

**Test 2.7: Expandable Row Details**
```
1. Click expand icon (chevron) on any registration row
2. Verify expanded section shows:
   ✓ Attendee Details (left column)
     - Each attendee card with name
     - Age category badge (Adult/Child)
     - Gender badge (Male/Female/Other)
   ✓ Contact Information (right column)
     - Email with envelope icon
     - Phone with phone icon
     - Address with map pin icon
   ✓ Registration Info
     - Formatted date/time
     - Registration ID (truncated GUID)
3. Click chevron again
4. Verify section collapses
```

#### Test Suite 3: Edge Cases & Stress Testing

**Test 3.1: Special Characters in Names**
```bash
# Register with Unicode characters
POST https://staging.lankaconnect.com/api/events/{eventId}/register-anonymous
{
  "attendees": [
    { "name": "José García", "ageCategory": 1 },
    { "name": "李明 (Li Ming)", "ageCategory": 1 },
    { "name": "Müller François", "ageCategory": 1 },
    { "name": "🎉 Party Planner", "ageCategory": 1 }  // Emoji
  ],
  "contact": {
    "email": "unicode@example.com",
    "phoneNumber": "+1234567890"
  }
}

# Export as Excel
POST https://staging.lankaconnect.com/api/events/{eventId}/export
{ "format": "excel" }

# Verify:
# - Excel opens without corruption
# - Names display correctly (no garbled text)
# - Emoji renders (or shows placeholder)
```

**Test 3.2: Large Event (Stress Test)**
```bash
# Create event with high capacity
POST /api/events
{
  "title": "Large Event Test",
  "capacity": 500,
  ...
}

# Register 200 attendees (script or bulk import)
for i in {1..200}; do
  POST /api/events/{eventId}/register-anonymous
  {
    "attendees": [
      { "name": "User $i", "ageCategory": 1, "gender": $((RANDOM % 2 + 1)) }
    ],
    "contact": {
      "email": "user$i@example.com",
      "phoneNumber": "+1234567890"
    }
  }
done

# Export as Excel
POST /api/events/{eventId}/export
{ "format": "excel" }

# Measure:
# - Export duration (< 10 seconds acceptable)
# - File size (< 5 MB for 200 attendees)
# - Excel opens without timeout
```

**Test 3.3: Concurrent Registration Attempts**
```bash
# Attempt simultaneous registrations with same email
# (Use Apache Bench or similar tool)
ab -n 10 -c 10 -T 'application/json' -p register.json \
  https://staging.lankaconnect.com/api/events/{eventId}/register-anonymous

# Verify:
# - Only ONE registration succeeds
# - Others receive 400 Bad Request
# - Database contains exactly 1 registration for that email
```

**Test 3.4: Registration with All Fields Empty**
```bash
POST /api/events/{eventId}/register-anonymous
{
  "attendees": [
    { "name": "", "ageCategory": 1 }  # Empty name
  ],
  "contact": {
    "email": "empty@example.com",
    "phoneNumber": ""  # Empty phone
  }
}

# Expected: 400 Bad Request with validation errors
```

---

### Phase 4: Production Deployment

#### Step 4.1: Pre-Production Checklist
- [ ] **Staging Tests Complete**: All test suites pass
- [ ] **Excel Export Verified**: File opens correctly in Excel
- [ ] **CSV Export Verified**: Data populates correctly
- [ ] **Performance Acceptable**: Export completes < 10s for 200 attendees
- [ ] **No Critical Bugs**: Issues #6 and #7 resolved or documented
- [ ] **Stakeholder Approval**: Product owner signs off
- [ ] **Rollback Plan Ready**: Documented procedure to revert

#### Step 4.2: Production Deployment (During Maintenance Window)
```bash
# Recommended: Off-peak hours (e.g., Sunday 2:00 AM UTC)

# 1. Announce maintenance (if downtime expected)
# Email users: "System maintenance in progress 2:00-2:30 AM UTC"

# 2. Deploy backend
az webapp deployment slot swap \
  --resource-group LankaConnect-RG \
  --name lankaconnect-api \
  --slot staging \
  --target-slot production

# 3. Deploy frontend
az staticwebapp deploy \
  --name lankaconnect-web \
  --resource-group LankaConnect-RG \
  --source ./out \
  --environment production

# 4. Verify deployment
curl https://api.lankaconnect.com/health
curl -I https://www.lankaconnect.com

# 5. Smoke test critical endpoints
curl -X GET https://api.lankaconnect.com/api/events \
  -H "Authorization: Bearer {prod_token}"
```

#### Step 4.3: Post-Deployment Verification
```bash
# Immediate verification (within 5 minutes)
1. Health check: https://api.lankaconnect.com/health
   Expected: 200 OK

2. Frontend loads: https://www.lankaconnect.com
   Expected: Page renders, no console errors

3. Login flow works
   Expected: User can login successfully

4. Event listing loads
   Expected: Events display correctly

# Regression testing (within 30 minutes)
5. Create new event
   Expected: Event created successfully

6. Register for event
   Expected: Registration succeeds

7. View attendee management
   Expected: Attendees display with correct badges

8. Export Excel
   Expected: File downloads and opens correctly

9. Export CSV
   Expected: File downloads with data

# Data integrity check (within 1 hour)
10. Verify no duplicate registrations created
    SELECT event_id, contact_email, COUNT(*)
    FROM registrations
    GROUP BY event_id, contact_email
    HAVING COUNT(*) > 1;
    Expected: 0 rows

11. Verify registration counts match
    SELECT event_id, COUNT(*) as reg_count
    FROM registrations
    WHERE status NOT IN (5, 6)  -- Exclude Cancelled/Refunded
    GROUP BY event_id;
    Expected: Matches UI display
```

---

## Rollback Plan

### Rollback Trigger Criteria
Initiate rollback if ANY of the following occur within 1 hour of deployment:
1. **Critical Bug**: Users unable to register for events
2. **Data Loss**: Registrations deleted unexpectedly
3. **Export Failure**: Excel/CSV exports fail for > 50% of attempts
4. **Performance Degradation**: API response times > 5 seconds
5. **Database Corruption**: Database errors logged
6. **User Reports**: > 5 user complaints about critical functionality

### Rollback Procedure

#### Step 1: Immediate Rollback (< 5 minutes)
```bash
# Rollback backend (swap slots back)
az webapp deployment slot swap \
  --resource-group LankaConnect-RG \
  --name lankaconnect-api \
  --slot production \
  --target-slot staging

# Rollback frontend
az staticwebapp deploy \
  --name lankaconnect-web \
  --resource-group LankaConnect-RG \
  --source ./out-previous \  # Previous build
  --environment production

# Verify rollback
curl https://api.lankaconnect.com/health
# Expected: Previous version running
```

#### Step 2: Database Cleanup (if needed)
```sql
-- If hard delete caused issues, restore from backup
-- Option A: Point-in-time restore (Azure PostgreSQL)
az postgres server restore \
  --resource-group LankaConnect-RG \
  --name lankaconnect-db \
  --restore-point-in-time "2025-12-24T01:59:00Z" \  # Before deployment
  --source-server lankaconnect-db

-- Option B: Selective data restore
-- Restore registrations table from backup
\copy registrations FROM '/backup/registrations_20251224.csv' CSV HEADER;
```

#### Step 3: Communication
```
# Notify stakeholders
Email: "Deployment of Phase 6A.45 has been rolled back due to {reason}.
System is now stable and running previous version.
Next steps: {investigation plan}"

# Update status page
"Incident resolved. System restored to previous version."
```

#### Step 4: Post-Rollback Investigation
1. Analyze logs for root cause of failure
2. Reproduce issue in dev/staging environment
3. Create fix plan with additional testing
4. Schedule re-deployment after fixes verified

---

## Monitoring & Alerts

### Key Metrics to Monitor (First 24 Hours)
1. **Registration Success Rate**
   - Target: > 95%
   - Alert if: < 90% for 5 minutes

2. **Export Success Rate**
   - Target: > 98%
   - Alert if: < 95% for 10 minutes

3. **API Response Time (P95)**
   - Target: < 500ms
   - Alert if: > 2000ms for 5 minutes

4. **Error Rate**
   - Target: < 1%
   - Alert if: > 5% for 5 minutes

5. **Database Connection Errors**
   - Target: 0
   - Alert if: > 0 for 1 minute

### Logging Queries (Azure Application Insights)
```kusto
// Failed registrations (duplicate prevention)
requests
| where timestamp > ago(24h)
| where url contains "register"
| where resultCode == 400
| where customDimensions.error contains "already registered"
| summarize count() by bin(timestamp, 5m)

// Export failures
requests
| where timestamp > ago(24h)
| where url contains "export"
| where resultCode >= 400
| summarize count() by resultCode, bin(timestamp, 15m)

// Gender distribution formatting errors
traces
| where timestamp > ago(24h)
| where message contains "Invalid registration status"
| summarize count() by message

// Performance tracking
requests
| where timestamp > ago(24h)
| where url contains "/api/events/"
| summarize avg(duration), percentile(duration, 95) by operation_Name
```

---

## Post-Deployment Validation Checklist

### Day 1 (Deployment Day)
- [ ] **Hour 1**: Smoke tests pass
- [ ] **Hour 4**: No critical errors logged
- [ ] **Hour 8**: Export functionality verified
- [ ] **Hour 24**: User feedback reviewed

### Week 1
- [ ] **Day 2**: Performance metrics within SLA
- [ ] **Day 3**: No duplicate registration reports
- [ ] **Day 5**: Export success rate > 98%
- [ ] **Day 7**: Stakeholder review meeting

### Week 2-4
- [ ] **Week 2**: Analytics review (registration trends)
- [ ] **Week 3**: User satisfaction survey
- [ ] **Week 4**: Post-deployment retrospective

---

## Success Criteria

Deployment is considered successful when ALL of the following are met:
1. ✓ All 7 issues from user testing are resolved
2. ✓ No new critical bugs introduced
3. ✓ Export success rate > 95% over 7 days
4. ✓ No duplicate registration incidents reported
5. ✓ User feedback positive (> 80% satisfaction)
6. ✓ Performance SLAs maintained (P95 < 500ms)
7. ✓ Database integrity verified (no orphaned records)

---

## Contact & Escalation

### Deployment Team
- **Backend Lead**: {Name} - {Email} - {Phone}
- **Frontend Lead**: {Name} - {Email} - {Phone}
- **DevOps Lead**: {Name} - {Email} - {Phone}
- **Product Owner**: {Name} - {Email} - {Phone}

### Escalation Path
1. **Level 1**: Deployment team attempts fix (< 15 minutes)
2. **Level 2**: Rollback initiated (< 30 minutes)
3. **Level 3**: Incident commander notified (< 1 hour)
4. **Level 4**: Executive stakeholders informed (> 2 hours downtime)

---

## Appendix: API Testing Collection

### Postman Collection Export
```json
{
  "info": {
    "name": "Phase 6A.45 - Attendee Management Tests",
    "schema": "https://schema.getpostman.com/json/collection/v2.1.0/collection.json"
  },
  "item": [
    {
      "name": "1. Duplicate Registration - Authenticated",
      "request": {
        "method": "POST",
        "header": [
          { "key": "Authorization", "value": "Bearer {{access_token}}" }
        ],
        "url": "{{base_url}}/api/events/{{eventId}}/register",
        "body": {
          "mode": "raw",
          "raw": "{\n  \"attendees\": [\n    { \"name\": \"Test User\", \"ageCategory\": 1 }\n  ],\n  \"contact\": {\n    \"email\": \"testuser@example.com\",\n    \"phoneNumber\": \"+1234567890\"\n  }\n}"
        }
      }
    },
    {
      "name": "2. Gender Distribution Check",
      "request": {
        "method": "GET",
        "header": [
          { "key": "Authorization", "value": "Bearer {{access_token}}" }
        ],
        "url": "{{base_url}}/api/events/{{eventId}}/attendees"
      },
      "tests": [
        "pm.test('Gender distribution formatted correctly', function() {",
        "  const response = pm.response.json();",
        "  const genderDist = response.attendees[0].genderDistribution;",
        "  pm.expect(genderDist).to.match(/\\d+ Male/);",
        "  pm.expect(genderDist).to.not.include('M,');",
        "});"
      ]
    },
    {
      "name": "3. Export Excel",
      "request": {
        "method": "POST",
        "header": [
          { "key": "Authorization", "value": "Bearer {{access_token}}" }
        ],
        "url": "{{base_url}}/api/events/{{eventId}}/export",
        "body": {
          "mode": "raw",
          "raw": "{ \"format\": \"excel\" }"
        }
      }
    },
    {
      "name": "4. Export CSV",
      "request": {
        "method": "POST",
        "header": [
          { "key": "Authorization", "value": "Bearer {{access_token}}" }
        ],
        "url": "{{base_url}}/api/events/{{eventId}}/export",
        "body": {
          "mode": "raw",
          "raw": "{ \"format\": \"csv\" }"
        }
      }
    }
  ]
}
```

---

## Document Version History

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 1.0 | 2025-12-24 | System Architect | Initial deployment plan created |
