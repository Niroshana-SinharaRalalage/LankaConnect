#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Phase 6A.47: Test reference data API endpoints
"""

import urllib.request
import json
import sys
import io

# Set UTF-8 encoding for output
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8')

# Authentication token
TOKEN = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJuYW1laWQiOiI1ZTc4MmI0ZC0yOWVkLTRlMWQtOTAzOS02YzhmNjk4YWVlYTkiLCJlbWFpbCI6Im5pcm9zaGhoQGdtYWlsLmNvbSIsInVuaXF1ZV9uYW1lIjoiTmlyb3NoYW5hIFNpbmhhcmEgUmFsYWxhZ2UiLCJyb2xlIjoiRXZlbnRPcmdhbml6ZXIiLCJmaXJzdE5hbWUiOiJOaXJvc2hhbmEiLCJsYXN0TmFtZSI6IlNpbmhhcmEgUmFsYWxhZ2UiLCJpc0FjdGl2ZSI6InRydWUiLCJqdGkiOiIxYmRhMWMzZS05ZWEwLTQ3NDMtYTU1NS1hN2ZhMjc5YmI3ZTMiLCJpYXQiOjE3NjY4NzY0OTEsIm5iZiI6MTc2Njg3NjQ5MSwiZXhwIjoxNzY2ODc4MjkxLCJpc3MiOiJodHRwczovL2xhbmthY29ubmVjdC1hcGktc3RhZ2luZy5henVyZXdlYnNpdGVzLm5ldCIsImF1ZCI6Imh0dHBzOi8vbGFua2Fjb25uZWN0LXN0YWdpbmcuYXp1cmV3ZWJzaXRlcy5uZXQifQ.ckjaSvBPJoo5_aIooAccpQPdDkReWG5fi_9w-qi-zA0"
BASE_URL = "https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io"

def test_endpoint(enum_types, expected_count=None):
    """Test a reference data endpoint"""
    url = f"{BASE_URL}/api/reference-data?types={enum_types}"

    try:
        req = urllib.request.Request(url)
        req.add_header('Authorization', f'Bearer {TOKEN}')
        req.add_header('Accept', 'application/json')

        with urllib.request.urlopen(req, timeout=10) as response:
            status_code = response.getcode()
            data = json.loads(response.read().decode('utf-8'))

            result = {
                'status': status_code,
                'count': len(data),
                'types': enum_types,
                'success': True
            }

            if expected_count and len(data) != expected_count:
                result['success'] = False
                result['message'] = f"Expected {expected_count} items, got {len(data)}"

            # Group by enum type
            from collections import Counter
            type_counts = Counter([item['enumType'] for item in data])
            result['breakdown'] = dict(type_counts)

            # Sample items
            result['samples'] = [{'code': item['code'], 'name': item['name']} for item in data[:3]]

            return result

    except Exception as e:
        return {
            'status': 'ERROR',
            'types': enum_types,
            'success': False,
            'error': str(e)
        }

def main():
    print("=" * 70)
    print("Phase 6A.47 API Endpoint Testing")
    print("=" * 70)

    tests = [
        ('EmailStatus', 11),
        ('EventCategory', 8),
        ('EventStatus', 8),
        ('UserRole', 6),
        ('Currency', 6),
        ('GeographicRegion', 35),
        ('EmailType', 9),
        ('BuddhistFestival', 11),
    ]

    passed = 0
    failed = 0

    for enum_type, expected in tests:
        print(f"\n[TEST] {enum_type} (expect {expected})")
        result = test_endpoint(enum_type, expected)

        if result['success']:
            print(f"  [OK] Status: {result['status']}, Count: {result['count']}/{expected}")
            if result.get('samples'):
                print("  Samples:")
                for sample in result['samples']:
                    print(f"    - {sample['code']}: {sample['name']}")
            passed += 1
        else:
            print(f"  [FAIL] {result.get('message', result.get('error', 'Unknown error'))}")
            failed += 1

    # Test multiple types
    print(f"\n[TEST] Multiple Types (EventCategory,EventStatus,UserRole)")
    result = test_endpoint('EventCategory,EventStatus,UserRole')

    if result['success']:
        print(f"  [OK] Status: {result['status']}, Total: {result['count']}")
        print("  Breakdown:")
        for enum_type, count in sorted(result['breakdown'].items()):
            print(f"    {enum_type}: {count}")
        passed += 1
    else:
        print(f"  [FAIL] {result.get('error', 'Unknown error')}")
        failed += 1

    # Summary
    print("\n" + "=" * 70)
    print(f"TEST SUMMARY: {passed} passed, {failed} failed")
    print("=" * 70)

    if failed == 0:
        print("\n[SUCCESS] All API endpoints are working correctly!")
        return 0
    else:
        print(f"\n[WARNING] {failed} test(s) failed")
        return 1

if __name__ == '__main__':
    sys.exit(main())
