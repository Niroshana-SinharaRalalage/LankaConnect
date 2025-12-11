# Phase 6 Day 1: Automated API Testing

**Date**: 2025-12-03
**Session**: 25 - Phase 6 E2E Testing (Day 1)
**Status**: 🚧 IN PROGRESS

---

## Overview

Phase 6 Day 1 focuses on automated API testing using bash/curl scripts to verify all pricing scenarios work correctly in Azure staging environment.

**Staging URL**: `https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io`

---

## Test Scenarios

### Scenario 1: Free Event Creation ✅
**Script**: `tests/e2e-api/test-scenario-1-free-event.sh`

**Tests**:
- POST `/api/events` with `isFree: true`
- GET created event - verify `isFree: true`, `ticketPriceAmount: null`
- Verify legacy format (`pricingType: null`)

**Expected Results**:
- HTTP 201 Created on POST
- HTTP 200 OK on GET
- Event properties match free event structure

---

### Scenario 2: Single Price Event ✅
**Script**: `tests/e2e-api/test-scenario-2-single-price.sh`

**Tests**:
- POST `/api/events` with `ticketPriceAmount: 25.00`, `ticketPriceCurrency: "USD"`
- GET created event - verify pricing fields
- Verify legacy format (`pricingType: null`)
- Verify `hasDualPricing: false`, `hasGroupPricing: false`

**Expected Results**:
- HTTP 201 Created on POST
- HTTP 200 OK on GET
- `ticketPriceAmount: 25.00`
- `ticketPriceCurrency: "USD"`

---

### Scenario 3: Dual Price Event (Adult/Child) ✅
**Script**: `tests/e2e-api/test-scenario-3-dual-price.sh`

**Tests**:
- POST `/api/events` with `pricing.type: "AgeDual"`
- Verify `adultPrice: $30`, `childPrice: $15`, `childAgeLimit: 12`
- GET created event - verify `hasDualPricing: true`
- Verify `pricingType: "AgeDual"`

**Expected Results**:
- HTTP 201 Created on POST
- HTTP 200 OK on GET
- Dual pricing structure correct
- Age limit validated

---

### Scenario 4: Group Tiered Event (Phase 6D) ✅
**Script**: `tests/e2e-api/test-scenario-4-group-tiered.sh`

**Tests**:
- POST `/api/events` with `pricing.type: "GroupTiered"`
- 3 pricing tiers:
  - Tier 1: 1-2 people @ $25/person
  - Tier 2: 3-5 people @ $20/person
  - Tier 3: 6+ people @ $15/person
- GET created event - verify `hasGroupPricing: true`
- Verify `pricingType: "GroupTiered"`
- Verify tier structure (min/max ranges, prices)

**Expected Results**:
- HTTP 201 Created on POST
- HTTP 200 OK on GET
- All 3 tiers present with correct ranges
- No gaps or overlaps in tier ranges

---

### Scenario 5: Legacy Events Verification ✅
**Script**: `tests/e2e-api/test-scenario-5-legacy-events.sh`

**Tests**:
- GET `/api/events?pageSize=100` - verify 27+ events
- GET specific legacy single price event (`68f675f1-327f-42a9-be9e-f66148d826c3`)
- GET specific legacy free event (`d914cc72-ce7e-45e9-9c6e-f7b07bd2405c`)
- Spot check 5 random events for accessibility
- Verify backward compatibility (legacy format works)

**Expected Results**:
- All 27 original events accessible
- Legacy pricing format preserved (`pricingType: null`)
- No data corruption
- Mix of free (12) and paid (15) events

---

### Scenario 6: Performance Testing ✅
**Script**: `tests/e2e-api/test-scenario-6-performance.sh`

**Tests**:
- Event list performance: GET `/api/events?pageSize=50` (target: < 1s)
- Event creation performance: POST `/api/events` (target: < 2s)
- Event retrieval performance: GET `/api/events/{id}` (target: < 1s)
- Health check performance: GET `/health` (target: < 0.5s)
- Concurrent requests: 3 parallel GET requests (target: < 3s total)

**Performance Targets**:
| Endpoint | Target | Measurement |
|----------|--------|-------------|
| Event List | < 1s | TBD |
| Event Creation | < 2s | TBD |
| Event Retrieval | < 1s | TBD |
| Health Check | < 0.5s | TBD |
| Concurrent Load | < 3s | TBD |

---

## Test Execution

### Running Tests

**Individual Scenario**:
```bash
bash tests/e2e-api/test-scenario-1-free-event.sh
```

**All Scenarios**:
```bash
bash tests/e2e-api/run-all-tests.sh
```

**Results Location**:
- Individual logs: `tests/e2e-api/results-scenario-*.log`
- Summary: Console output from `run-all-tests.sh`

---

## Test Scripts Created

1. ✅ `test-scenario-1-free-event.sh` - Free event creation
2. ✅ `test-scenario-2-single-price.sh` - Single price event
3. ✅ `test-scenario-3-dual-price.sh` - Dual price (adult/child)
4. ✅ `test-scenario-4-group-tiered.sh` - Group tiered pricing (Phase 6D)
5. ✅ `test-scenario-5-legacy-events.sh` - Legacy events verification
6. ✅ `test-scenario-6-performance.sh` - Performance testing
7. ✅ `run-all-tests.sh` - Master test runner

---

## Stripe Test Card Verification

**Test Card**: `4242 4242 4242 4242`

**Action Items**:
- [ ] Verify Stripe test card configuration in staging
- [ ] Document test card setup for payment testing
- [ ] Test payment redirect flow (Day 2 UI testing)

---

## Success Criteria

### API Testing (Day 1)
- [ ] All 6 test scenarios execute successfully
- [ ] Free event creation works
- [ ] Single price event creation works
- [ ] Dual price event creation works
- [ ] Group tiered event creation works (Phase 6D)
- [ ] Legacy events remain accessible and functional
- [ ] Performance targets met or documented

### Test Coverage
- [ ] 4 new event types created via API
- [ ] 27 legacy events verified
- [ ] Backward compatibility confirmed
- [ ] Performance baselines established

---

## Next Steps

### After Day 1 API Testing
1. **Review Results**: Analyze all test logs for failures/warnings
2. **Document Findings**: Update this document with actual test results
3. **Fix Issues**: Address any failed tests before Day 2
4. **Prepare for Day 2**: Share test event IDs with user for UI testing

### Day 2 (Manual UI Testing)
- User tests UI with screenshots
- Browser compatibility testing
- UX/UI issue documentation
- Payment flow testing with Stripe test card

### Day 3 (Report Compilation)
- Compile comprehensive E2E Test Execution Report
- Update tracking documents
- Commit and push all documentation

---

## Test Results

_To be filled after test execution_

### Scenario Results
| Scenario | Status | Notes |
|----------|--------|-------|
| 1. Free Event | ⏳ Pending | |
| 2. Single Price | ⏳ Pending | |
| 3. Dual Price | ⏳ Pending | |
| 4. Group Tiered | ⏳ Pending | |
| 5. Legacy Events | ⏳ Pending | |
| 6. Performance | ⏳ Pending | |

### Performance Metrics
_To be filled after performance testing_

---

## References
- [Phase 5 Deployment Summary](./PHASE_5_DEPLOYMENT_SUMMARY.md)
- [Progress Tracker](./PROGRESS_TRACKER.md)
- [Streamlined Action Plan](./STREAMLINED_ACTION_PLAN.md)
- Staging URL: https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io
