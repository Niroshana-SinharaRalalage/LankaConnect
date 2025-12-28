#!/usr/bin/env python3
"""
Test Cultural Interests API Endpoint
Phase 6A.47: Verify new endpoint exposes CulturalInterest.All via API
"""

import sys
import io
import requests
import json

# Fix Windows console encoding
sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8')

# Configuration
BASE_URL = "https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io"
LOGIN_ENDPOINT = f"{BASE_URL}/api/Auth/login"
CULTURAL_INTERESTS_ENDPOINT = f"{BASE_URL}/api/reference-data/cultural-interests"

# Test credentials
TEST_EMAIL = "niroshihh@gmail.com"
TEST_PASSWORD = "testtest"

def login():
    """Authenticate and get JWT token"""
    print("[1/3] Authenticating...")

    payload = {
        "email": TEST_EMAIL,
        "password": TEST_PASSWORD
    }

    try:
        response = requests.post(LOGIN_ENDPOINT, json=payload, timeout=30)

        if response.status_code == 200:
            data = response.json()
            token = data.get("token")
            print(f"[OK] Authentication successful")
            return token
        else:
            print(f"[ERROR] Authentication failed: {response.status_code}")
            print(f"Response: {response.text}")
            return None
    except Exception as e:
        print(f"[ERROR] Authentication request failed: {str(e)}")
        return None

def test_cultural_interests_endpoint(token):
    """Test GET /api/reference-data/cultural-interests endpoint"""
    print("\n[2/3] Testing GET /api/reference-data/cultural-interests...")

    headers = {
        "Authorization": f"Bearer {token}",
        "Accept": "application/json"
    }

    try:
        response = requests.get(CULTURAL_INTERESTS_ENDPOINT, headers=headers, timeout=30)

        print(f"Status Code: {response.status_code}")

        if response.status_code == 200:
            data = response.json()

            # Verify response structure
            if not isinstance(data, list):
                print(f"[ERROR] Expected array, got {type(data)}")
                return False

            print(f"[OK] Received {len(data)} cultural interests")

            # Verify expected count (should be 20 based on CulturalInterest.All)
            if len(data) != 20:
                print(f"[WARNING] Expected 20 cultural interests, got {len(data)}")

            # Verify structure of first item
            if len(data) > 0:
                first_item = data[0]
                required_fields = ["code", "name"]

                missing_fields = [field for field in required_fields if field not in first_item]
                if missing_fields:
                    print(f"[ERROR] Missing fields in response: {missing_fields}")
                    return False

                print(f"[OK] Response structure valid")
                print(f"Sample item: {json.dumps(first_item, indent=2)}")

            # Verify known cultural interest codes
            codes = [item.get("code") for item in data]
            expected_codes = ["SL_CUISINE", "BUDDHIST_FEST", "CRICKET", "AYURVEDA", "TEA_CULTURE"]

            found_codes = [code for code in expected_codes if code in codes]
            print(f"[OK] Found {len(found_codes)}/{len(expected_codes)} expected codes: {found_codes}")

            return True
        else:
            print(f"[ERROR] Request failed with status {response.status_code}")
            print(f"Response: {response.text}")
            return False

    except Exception as e:
        print(f"[ERROR] Request failed: {str(e)}")
        return False

def test_caching():
    """Test response caching by checking headers"""
    print("\n[3/3] Testing response caching...")

    try:
        # First request to populate cache
        response1 = requests.get(CULTURAL_INTERESTS_ENDPOINT, timeout=30)

        # Check cache headers
        cache_control = response1.headers.get("Cache-Control")

        if cache_control:
            print(f"[OK] Cache-Control header present: {cache_control}")

            if "max-age=3600" in cache_control or "public" in cache_control:
                print("[OK] Response is cacheable for 1 hour")
                return True
            else:
                print(f"[WARNING] Unexpected cache policy: {cache_control}")
                return False
        else:
            print("[WARNING] No Cache-Control header found")
            return False

    except Exception as e:
        print(f"[ERROR] Cache test failed: {str(e)}")
        return False

def main():
    """Run all tests"""
    print("=" * 60)
    print("Cultural Interests API Endpoint Test")
    print("Phase 6A.47: Verify CulturalInterest.All exposure")
    print("=" * 60)

    # Step 1: Authenticate
    token = login()
    if not token:
        print("\n[FAILED] Cannot proceed without authentication")
        sys.exit(1)

    # Step 2: Test Cultural Interests endpoint
    endpoint_ok = test_cultural_interests_endpoint(token)

    # Step 3: Test caching
    caching_ok = test_caching()

    # Summary
    print("\n" + "=" * 60)
    print("TEST SUMMARY")
    print("=" * 60)
    print(f"Authentication: [OK]")
    print(f"Cultural Interests Endpoint: {'[OK]' if endpoint_ok else '[FAILED]'}")
    print(f"Response Caching: {'[OK]' if caching_ok else '[FAILED]'}")

    all_passed = endpoint_ok and caching_ok

    if all_passed:
        print("\n[SUCCESS] All tests passed!")
        sys.exit(0)
    else:
        print("\n[FAILED] Some tests failed")
        sys.exit(1)

if __name__ == "__main__":
    main()
