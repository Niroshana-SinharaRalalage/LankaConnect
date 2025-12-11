#!/bin/bash
# Phase 6 E2E API Test Suite - Master Test Runner
# Runs all API test scenarios and generates comprehensive report

echo "╔════════════════════════════════════════════════════════════╗"
echo "║        Phase 6 E2E API Test Suite - Test Runner           ║"
echo "║              LankaConnect Staging Environment              ║"
echo "╚════════════════════════════════════════════════════════════╝"
echo ""
echo "Test Suite: API Automated Testing"
echo "Environment: Azure Staging"
echo "Date: $(date '+%Y-%m-%d %H:%M:%S')"
echo ""
echo "════════════════════════════════════════════════════════════"
echo ""

# Test script directory
TEST_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

# Results summary
TOTAL_TESTS=6
PASSED_TESTS=0
FAILED_TESTS=0

# Run each test scenario
echo "Running Test Scenarios..."
echo ""

# Scenario 1: Free Event
echo "▶ Running Scenario 1: Free Event Creation..."
bash "$TEST_DIR/test-scenario-1-free-event.sh" > "$TEST_DIR/results-scenario-1.log" 2>&1
if grep -q "Test Scenario 1 Complete" "$TEST_DIR/results-scenario-1.log"; then
    echo "  ✅ Scenario 1 completed"
    ((PASSED_TESTS++))
else
    echo "  ❌ Scenario 1 failed"
    ((FAILED_TESTS++))
fi
echo ""

# Scenario 2: Single Price Event
echo "▶ Running Scenario 2: Single Price Event..."
bash "$TEST_DIR/test-scenario-2-single-price.sh" > "$TEST_DIR/results-scenario-2.log" 2>&1
if grep -q "Test Scenario 2 Complete" "$TEST_DIR/results-scenario-2.log"; then
    echo "  ✅ Scenario 2 completed"
    ((PASSED_TESTS++))
else
    echo "  ❌ Scenario 2 failed"
    ((FAILED_TESTS++))
fi
echo ""

# Scenario 3: Dual Price Event
echo "▶ Running Scenario 3: Dual Price Event (Adult/Child)..."
bash "$TEST_DIR/test-scenario-3-dual-price.sh" > "$TEST_DIR/results-scenario-3.log" 2>&1
if grep -q "Test Scenario 3 Complete" "$TEST_DIR/results-scenario-3.log"; then
    echo "  ✅ Scenario 3 completed"
    ((PASSED_TESTS++))
else
    echo "  ❌ Scenario 3 failed"
    ((FAILED_TESTS++))
fi
echo ""

# Scenario 4: Group Tiered Event
echo "▶ Running Scenario 4: Group Tiered Event (Phase 6D)..."
bash "$TEST_DIR/test-scenario-4-group-tiered.sh" > "$TEST_DIR/results-scenario-4.log" 2>&1
if grep -q "Test Scenario 4 Complete" "$TEST_DIR/results-scenario-4.log"; then
    echo "  ✅ Scenario 4 completed"
    ((PASSED_TESTS++))
else
    echo "  ❌ Scenario 4 failed"
    ((FAILED_TESTS++))
fi
echo ""

# Scenario 5: Legacy Events
echo "▶ Running Scenario 5: Legacy Events Verification..."
bash "$TEST_DIR/test-scenario-5-legacy-events.sh" > "$TEST_DIR/results-scenario-5.log" 2>&1
if grep -q "Test Scenario 5 Complete" "$TEST_DIR/results-scenario-5.log"; then
    echo "  ✅ Scenario 5 completed"
    ((PASSED_TESTS++))
else
    echo "  ❌ Scenario 5 failed"
    ((FAILED_TESTS++))
fi
echo ""

# Scenario 6: Performance Testing
echo "▶ Running Scenario 6: Performance Testing..."
bash "$TEST_DIR/test-scenario-6-performance.sh" > "$TEST_DIR/results-scenario-6.log" 2>&1
if grep -q "Test Scenario 6 Complete" "$TEST_DIR/results-scenario-6.log"; then
    echo "  ✅ Scenario 6 completed"
    ((PASSED_TESTS++))
else
    echo "  ❌ Scenario 6 failed"
    ((FAILED_TESTS++))
fi
echo ""

echo "════════════════════════════════════════════════════════════"
echo ""
echo "Test Suite Complete!"
echo ""
echo "Results Summary:"
echo "  Total Scenarios: $TOTAL_TESTS"
echo "  Passed: $PASSED_TESTS"
echo "  Failed: $FAILED_TESTS"
echo ""

if [ "$FAILED_TESTS" -eq 0 ]; then
    echo "✅ ALL TESTS PASSED"
    echo ""
    echo "Next Steps:"
    echo "1. Review detailed logs in tests/e2e-api/results-scenario-*.log"
    echo "2. Proceed with Phase 6 Day 2: Manual UI Testing"
    echo "3. Compile comprehensive E2E Test Report"
else
    echo "⚠️ SOME TESTS FAILED"
    echo ""
    echo "Failed Scenarios: $FAILED_TESTS"
    echo "Please review logs for details:"
    for i in {1..6}; do
        if ! grep -q "Complete" "$TEST_DIR/results-scenario-$i.log" 2>/dev/null; then
            echo "  - results-scenario-$i.log"
        fi
    done
fi

echo ""
echo "Detailed results available in:"
echo "  $TEST_DIR/results-scenario-*.log"
echo ""
echo "════════════════════════════════════════════════════════════"
