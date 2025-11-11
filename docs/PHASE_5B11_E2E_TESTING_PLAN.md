# Phase 5B.11: E2E Testing - Profile → Newsletter → Community Activity

**Status**: 🚀 ACTIVE DEVELOPMENT
**Target Date**: 2025-11-11
**Environment**: Azure Staging API (lankaconnect-api-staging)
**UI**: Local development (localhost:3000)

---

## 📋 Executive Summary

Phase 5B.11 validates the complete user workflow from profile setup through community activity display:

1. **User Profile** → Select preferred metro areas (0-20 metros)
2. **Newsletter Signup** → Subscribe with multi-metro selection
3. **Landing Page** → View community activity filtered by preferred metros
4. **Feed Display** → See events organized by preferred vs other metros

---

## 🎯 E2E Test Scenarios

### Scenario 1: Single Metro Selection & Filtering

**User Journey**:
```
1. User logs in
2. Navigate to Profile Settings
3. Select single metro area (e.g., "All Ohio")
4. Save preferences
5. Return to landing page
6. Verify "Events in Your Preferred Metros" section shows Ohio events
7. Verify event count badge displays correct number
```

**Test Cases**:
- ✓ User can select state-level metro (All Ohio)
- ✓ Selection persists after save
- ✓ Landing page filters show Ohio events only in preferred section
- ✓ Event count badge displays accurate count
- ✓ Profile settings shows selected metro in view mode

### Scenario 2: Multiple Metro Selection & Combined Filtering

**User Journey**:
```
1. User logs in
2. Navigate to Profile Settings
3. Select multiple metros (Cleveland + Columbus + Cincinnati)
4. Save preferences (verify "3 of 20 selected" counter)
5. Return to landing page
6. Verify preferred section shows events from ALL 3 metros (OR logic)
7. Verify "Other Events" section shows remaining events
```

**Test Cases**:
- ✓ User can select up to 20 metros
- ✓ Selection counter updates in real-time (X of 20 selected)
- ✓ Multi-select persists after save
- ✓ Landing page shows OR logic (event from ANY preferred metro)
- ✓ No duplicate events across sections
- ✓ Event count badges accurate for both sections

### Scenario 3: Newsletter Subscription with Multiple Metros

**User Journey**:
```
1. User navigates to Footer
2. Clicks "Subscribe to Newsletter"
3. Enters email address
4. Selects multiple metro areas
5. Submits subscription
6. Receives confirmation message
7. Login and verify newsletter metros saved to profile
```

**Test Cases**:
- ✓ Newsletter form allows 0-20 metro selection
- ✓ Multi-metro selection works in newsletter widget
- ✓ Newsletter subscribes user with selected metros
- ✓ Email validation works
- ✓ Confirmation message displays
- ✓ Selected metros sync to user profile

### Scenario 4: Feed Display & UI Components

**User Journey**:
```
1. User with preferred metros views landing page
2. Preferred Metros Section displays
   - "Events in Your Preferred Metros" heading with Sparkles icon
   - Event count badge
   - Events from preferred metros only
3. All Other Events Section displays
   - "All Other Events" heading with MapPin icon
   - Toggle button to collapse/expand
   - Event count badge
   - Events NOT in preferred metros
4. Both sections use ActivityFeed component properly
```

**Test Cases**:
- ✓ Preferred section visible only when user logged in + has metros
- ✓ Preferred section heading uses Sparkles icon (#FF7900)
- ✓ Event count badges display with correct styling
- ✓ Other section always visible (even if no preferred)
- ✓ Toggle button collapses/expands Other section
- ✓ Both sections use ActivityFeed component
- ✓ Responsive layout on mobile/tablet/desktop

### Scenario 5: Privacy & Default States

**User Journey**:
```
1. New user (no metros selected)
2. Views landing page
3. Sees only "All Other Events" section
4. User logs in (without selecting metros)
5. Still sees only "All Other Events"
6. User selects metros
7. Now sees both sections
```

**Test Cases**:
- ✓ Unauthenticated users see all events (no preferred section)
- ✓ Authenticated users without metros see all events
- ✓ Authenticated users with metros see split view
- ✓ Privacy: no events shown for unselected metros

### Scenario 6: State-Level vs City-Level Metro Filtering

**User Journey**:
```
1. User selects "All Ohio" state-level metro
2. Landing page shows ALL Ohio events (any city in Ohio)
3. User adds "Cleveland" city-level metro
4. Landing page still shows all Ohio events + any Cleveland-specific events
5. User removes "All Ohio", keeps "Cleveland"
6. Landing page shows only Cleveland events
```

**Test Cases**:
- ✓ State-level metro ("All Ohio") matches any city in state
- ✓ City-level metro ("Cleveland") matches specific city
- ✓ Multiple metros combine with OR logic
- ✓ State abbreviation conversion works (OH → Ohio)
- ✓ Case-insensitive matching

---

## 🛠️ Test Infrastructure

### Test File Structure

```
web/src/__tests__/
├── integration/
│   ├── auth-flow.test.ts (existing)
│   └── metro-areas-workflow.test.ts (NEW - Phase 5B.11)
└── pages/
    └── landing-page-metro-filtering.test.tsx (existing Phase 5B.9.4)
```

### Testing Approach

**Pattern**: Integration tests using vitest + real API calls

```typescript
// Example test structure
describe('Metro Areas E2E Workflow', () => {
  // Setup: Create test user, authenticate
  beforeAll(async () => {
    testUser = await authRepository.register({...});
    tokens = await authRepository.login({...});
  });

  // Test: Profile → Newsletter → Landing page
  it('should filter landing page events by preferred metros', async () => {
    // Step 1: Update user profile with preferred metros
    const savedMetros = await profileRepository.updatePreferredMetros([
      '39000000-0000-0000-0000-000000000001', // All Ohio
    ]);
    expect(savedMetros).toHaveLength(1);

    // Step 2: Verify profile retrieval shows selected metros
    const profile = await profileRepository.getProfile();
    expect(profile.preferredMetroAreas).toContain('39000000-0000-0000-0000-000000000001');

    // Step 3: Get events and verify filtering
    const events = await eventsRepository.getAll();
    const ohioEvents = events.filter(e => e.location.includes('Ohio'));
    expect(ohioEvents.length).toBeGreaterThan(0);
  });

  // Cleanup
  afterAll(async () => {
    // Delete test user
  });
});
```

### API Endpoints Being Tested

| Endpoint | Method | Purpose |
|----------|--------|---------|
| `POST /api/auth/register` | POST | Create test user |
| `POST /api/auth/login` | POST | Authenticate user |
| `GET /api/profile` | GET | Retrieve user profile with preferred metros |
| `PATCH /api/profile/preferred-metros` | PATCH | Update preferred metro selection |
| `GET /api/events` | GET | Fetch all events |
| `POST /api/newsletter/subscribe` | POST | Newsletter subscription (optional) |

---

## 📊 Expected Test Results

### Test Suite Coverage

```
Integration Tests: metro-areas-workflow.test.ts
├── User Registration & Auth (2 tests)
│   ✓ Register new user
│   ✓ Login with valid credentials
│
├── Profile Metro Selection (5 tests)
│   ✓ Update single metro
│   ✓ Update multiple metros (0-20 limit)
│   ✓ Clear all metros (privacy choice)
│   ✓ Verify selection persists
│   ✓ Validate max limit enforcement
│
├── Landing Page Filtering (6 tests)
│   ✓ Show all events when no metros selected
│   ✓ Filter by single state metro
│   ✓ Filter by single city metro
│   ✓ Filter by multiple metros (OR logic)
│   ✓ No duplicate events across sections
│   ✓ Event count badges accurate
│
├── Newsletter Integration (3 tests)
│   ✓ Newsletter subscription with metros
│   ✓ Newsletter metros sync to profile
│   ✓ Email validation in newsletter form
│
└── UI/UX Validation (4 tests)
    ✓ Preferred section visible only with metros
    ✓ Icons display correctly (Sparkles, MapPin)
    ✓ Badges show correct counts
    ✓ Responsive layout on different devices
```

**Total Tests**: 20
**Expected Pass Rate**: 100%
**Estimated Duration**: 3-5 minutes (including API calls)

---

## 🚀 Execution Plan

### Phase 5B.11.1: Design Test Scenarios ✅

**Completed**:
- User journey mapping
- Scenario documentation
- Test case breakdown
- API endpoint identification

### Phase 5B.11.2: Create Integration Test File

**Tasks**:
1. Create `metro-areas-workflow.test.ts`
2. Import test utilities and repositories
3. Setup test user creation/cleanup
4. Implement basic test structure

**Deliverable**: `web/src/__tests__/integration/metro-areas-workflow.test.ts`

### Phase 5B.11.3-5B.11.7: Implement Test Cases

**Breakdown by scenario**:
- 5B.11.3: Profile metro selection tests (5 tests)
- 5B.11.4: Newsletter subscription tests (3 tests)
- 5B.11.5: Landing page filtering tests (6 tests)
- 5B.11.6: Feed UI/UX tests (4 tests)
- 5B.11.7: Full test execution & fixes (all 20 tests)

**Quality Gates**:
- ✓ All 20 tests passing
- ✓ 0 TypeScript errors
- ✓ No API connection errors
- ✓ Clean test output

---

## 🔍 Debugging & Troubleshooting

### Common Issues & Solutions

#### Issue: API Connection Errors
**Symptom**: `NEXT_PUBLIC_API_URL is undefined`
**Solution**:
```bash
# Ensure .env.local is set
echo "NEXT_PUBLIC_API_URL=https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api" >> .env.local

# Run tests
npm test metro-areas-workflow.test.ts
```

#### Issue: Auth Token Expired
**Symptom**: `401 Unauthorized` errors after first test
**Solution**:
```typescript
// Ensure tokens are refreshed between tests
afterEach(() => {
  apiClient.clearAuthToken();
});

// Or use fixtures for token persistence
```

#### Issue: Stale Test Data
**Symptom**: `Duplicate email` error on test re-run
**Solution**:
```typescript
// Use dynamic test emails
const testEmail = `test.${Date.now()}@lankaconnect.test`;

// Ensure cleanup in afterAll
afterAll(async () => {
  await userRepository.deleteUser(testUser.id);
});
```

---

## 📈 Success Criteria

### Build Quality
- ✅ 0 TypeScript compilation errors
- ✅ 0 ESLint violations
- ✅ All imports properly typed
- ✅ No console warnings in test output

### Test Quality
- ✅ All 20 tests pass
- ✅ 100% pass rate
- ✅ Tests complete in < 5 minutes
- ✅ Clean error handling (no unhandled rejections)

### Integration Quality
- ✅ API responses match expected format
- ✅ Database state persists correctly
- ✅ User preferences sync across calls
- ✅ No race conditions or flakiness

### Code Quality
- ✅ Follows existing test patterns
- ✅ Reuses test utilities
- ✅ Clear, descriptive test names
- ✅ Proper documentation/comments

---

## 📚 Reference

### Existing Test Examples
- `web/src/__tests__/integration/auth-flow.test.ts` - Authentication flow pattern
- `web/src/__tests__/pages/landing-page-metro-filtering.test.tsx` - UI component testing pattern

### Key Utilities
- `authRepository` - User registration/login
- `profileRepository` - User profile CRUD
- `eventsRepository` - Event fetching
- `apiClient` - HTTP client with token management

### Environmental Setup
- API URL: `process.env.NEXT_PUBLIC_API_URL`
- Test User Pattern: `test.user.${Date.now()}@lankaconnect.test`
- Auth Tokens: Managed via `apiClient` and `beforeAll`/`afterEach`

---

## ✅ Checklist

Before starting Phase 5B.11.2:
- [ ] Read this document completely
- [ ] Review existing auth-flow.test.ts pattern
- [ ] Setup test environment (API URL in .env.local)
- [ ] Verify staging API is accessible
- [ ] Check database has seeded metro areas
- [ ] Create todo list with test cases

---

**Document Version**: 1.0
**Created**: 2025-11-11
**Phase**: 5B.11 - E2E Testing
**Status**: Planning Complete - Ready for Implementation
