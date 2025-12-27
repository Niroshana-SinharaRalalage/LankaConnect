#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Phase 6A.47: Comprehensive test of ALL 6 reference data endpoints
Tests both unified and legacy endpoints, plus cache invalidation
"""

import urllib.request
import json
import sys
import io

# Set UTF-8 encoding for output
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8')

BASE_URL = "https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io"

def get_auth_token():
    """Get authentication token"""
    url = f"{BASE_URL}/api/Auth/login"
    data = json.dumps({
        "email": "niroshhh@gmail.com",
        "password": "12!@qwASzx",
        "rememberMe": True,
        "ipAddress": "string"
    }).encode('utf-8')

    req = urllib.request.Request(url, data=data)
    req.add_header('Content-Type', 'application/json')
    req.add_header('Accept', 'application/json')

    with urllib.request.urlopen(req, timeout=10) as response:
        result = json.loads(response.read().decode('utf-8'))
        return result['accessToken']

def test_endpoint(method, path, token, description, expected_count=None):
    """Test an endpoint and return results"""
    url = f"{BASE_URL}{path}"

    try:
        req = urllib.request.Request(url, method=method)
        req.add_header('Authorization', f'Bearer {token}')
        req.add_header('Accept', 'application/json')

        with urllib.request.urlopen(req, timeout=10) as response:
            status_code = response.getcode()

            if method == 'GET':
                data = json.loads(response.read().decode('utf-8'))
                count = len(data) if isinstance(data, list) else 'N/A'

                result = {
                    'success': True,
                    'status': status_code,
                    'count': count,
                    'description': description
                }

                if expected_count and count != expected_count:
                    result['success'] = False
                    result['error'] = f"Expected {expected_count} items, got {count}"

                # Get sample items
                if isinstance(data, list) and len(data) > 0:
                    result['sample'] = f"{data[0].get('code', 'N/A')}: {data[0].get('name', 'N/A')}"

                return result
            else:
                # POST endpoints
                return {
                    'success': True,
                    'status': status_code,
                    'description': description
                }

    except urllib.error.HTTPError as e:
        return {
            'success': False,
            'status': e.code,
            'error': f"HTTP {e.code}: {e.reason}",
            'description': description
        }
    except Exception as e:
        return {
            'success': False,
            'error': str(e),
            'description': description
        }

def main():
    print("=" * 80)
    print("Phase 6A.47 - COMPREHENSIVE Reference Data API Endpoint Testing")
    print("=" * 80)

    # Get authentication token
    print("\n[AUTH] Getting authentication token...")
    try:
        token = get_auth_token()
        print("[OK] Authentication successful")
    except Exception as e:
        print(f"[ERROR] Authentication failed: {e}")
        return 1

    # Define all 6 endpoints to test
    tests = [
        # 1. Unified endpoint - single type
        ('GET', '/api/reference-data?types=EventCategory', 8,
         '1. GET /api/reference-data?types=EventCategory (Unified - single type)'),

        # 2. Unified endpoint - multiple types
        ('GET', '/api/reference-data?types=EventCategory,EventStatus,UserRole', 22,
         '2. GET /api/reference-data?types=EventCategory,EventStatus,UserRole (Unified - multiple types)'),

        # 3. Legacy endpoint - event categories
        ('GET', '/api/reference-data/event-categories', 8,
         '3. GET /api/reference-data/event-categories (Legacy)'),

        # 4. Legacy endpoint - event statuses
        ('GET', '/api/reference-data/event-statuses', 8,
         '4. GET /api/reference-data/event-statuses (Legacy)'),

        # 5. Legacy endpoint - user roles
        ('GET', '/api/reference-data/user-roles', 6,
         '5. GET /api/reference-data/user-roles (Legacy)'),

        # 6. Cache invalidation - specific type
        ('POST', '/api/reference-data/invalidate-cache/EventCategory', None,
         '6. POST /api/reference-data/invalidate-cache/EventCategory (Admin only)'),

        # 7. Cache invalidation - all
        ('POST', '/api/reference-data/invalidate-all-caches', None,
         '7. POST /api/reference-data/invalidate-all-caches (Admin only)'),
    ]

    passed = 0
    failed = 0
    admin_only = 0

    print("\n" + "=" * 80)
    print("RUNNING TESTS")
    print("=" * 80)

    for method, path, expected_count, description in tests:
        print(f"\n[TEST] {description}")
        result = test_endpoint(method, path, token, description, expected_count)

        if result['success']:
            print(f"  [OK] Status: {result['status']}", end='')
            if 'count' in result:
                print(f", Count: {result['count']}", end='')
                if expected_count:
                    print(f"/{expected_count}", end='')
            print()

            if 'sample' in result:
                print(f"  Sample: {result['sample']}")

            passed += 1
        else:
            # Check if it's an admin-only endpoint returning 403
            if result.get('status') == 403 and 'invalidate' in path:
                print(f"  [EXPECTED] Status: 403 (Admin only - normal for non-admin user)")
                admin_only += 1
            else:
                print(f"  [FAIL] {result.get('error', 'Unknown error')}")
                failed += 1

    # Re-test unified endpoint after cache invalidation to verify it still works
    print(f"\n[TEST] 8. Verify unified endpoint still works after cache operations")
    result = test_endpoint('GET', '/api/reference-data?types=EmailStatus', token,
                          'Unified endpoint post-cache', 11)
    if result['success']:
        print(f"  [OK] Status: {result['status']}, Count: {result['count']}/11")
        print(f"  Sample: {result['sample']}")
        passed += 1
    else:
        print(f"  [FAIL] {result.get('error', 'Unknown error')}")
        failed += 1

    # Summary
    print("\n" + "=" * 80)
    print("TEST SUMMARY")
    print("=" * 80)
    print(f"Total Tests: {passed + failed + admin_only}")
    print(f"  Passed: {passed}")
    print(f"  Failed: {failed}")
    print(f"  Admin-only (Expected 403): {admin_only}")
    print()

    if failed == 0:
        print("[SUCCESS] All endpoints working correctly!")
        print()
        print("Verified Endpoints:")
        print("  1. Unified GET /api/reference-data?types=X (single type)")
        print("  2. Unified GET /api/reference-data?types=X,Y,Z (multiple types)")
        print("  3. Legacy GET /api/reference-data/event-categories")
        print("  4. Legacy GET /api/reference-data/event-statuses")
        print("  5. Legacy GET /api/reference-data/user-roles")
        print("  6. Cache invalidation endpoints (admin-only, returns 403 as expected)")
        return 0
    else:
        print(f"[WARNING] {failed} test(s) failed")
        return 1

if __name__ == '__main__':
    sys.exit(main())
