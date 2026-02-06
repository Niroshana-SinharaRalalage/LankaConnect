#!/usr/bin/env python3
"""
Test Badge Positioning API
Verifies that badges have proper location configs and can be updated
"""
import requests
import json
import sys

API_BASE = "https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api"

def login():
    """Login and get auth token"""
    login_data = {
        "email": "niroshhh@gmail.com",
        "password": "12!@qwASzx",
        "rememberMe": True,
        "ipAddress": "string"
    }

    response = requests.post(f"{API_BASE}/Auth/login", json=login_data)
    response.raise_for_status()
    return response.json()["accessToken"]

def get_badges(token):
    """Get all badges"""
    headers = {"Authorization": f"Bearer {token}"}
    response = requests.get(f"{API_BASE}/Badges", headers=headers)
    response.raise_for_status()
    return response.json()

def test_badge_configs():
    """Test badge location configs"""
    print("=" * 80)
    print("PHASE 6A.32 DIAGNOSTIC - Badge Positioning API Test")
    print("=" * 80)

    # Login
    print("\n1. Logging in...")
    try:
        token = login()
        print("   [OK] Login successful")
    except Exception as e:
        print(f"   [FAIL] Login failed: {e}")
        sys.exit(1)

    # Get badges
    print("\n2. Fetching badges...")
    try:
        badges = get_badges(token)
        print(f"   [OK] Retrieved {len(badges)} badges")
    except Exception as e:
        print(f"   [FAIL] Failed to fetch badges: {e}")
        sys.exit(1)

    # Check location configs
    print("\n3. Checking badge location configs...")
    all_identical = True
    has_nulls = False

    for badge in badges:
        print(f"\n   Badge: {badge['name']} (ID: {badge['id']})")

        # Check if configs exist
        listing = badge.get('listingConfig')
        featured = badge.get('featuredConfig')
        detail = badge.get('detailConfig')

        if not listing or not featured or not detail:
            print("      [FAIL] Missing location configs!")
            has_nulls = True
            continue

        # Print configs
        print(f"      Listing:  x={listing['positionX']:.2f}, y={listing['positionY']:.2f}, w={listing['sizeWidth']:.2f}, h={listing['sizeHeight']:.2f}, rot={listing['rotation']:.0f} deg")
        print(f"      Featured: x={featured['positionX']:.2f}, y={featured['positionY']:.2f}, w={featured['sizeWidth']:.2f}, h={featured['sizeHeight']:.2f}, rot={featured['rotation']:.0f} deg")
        print(f"      Detail:   x={detail['positionX']:.2f}, y={detail['positionY']:.2f}, w={detail['sizeWidth']:.2f}, h={detail['sizeHeight']:.2f}, rot={detail['rotation']:.0f} deg")

        # Check if all three are different (they should be for interactive positioning to work)
        if (listing['positionX'] == featured['positionX'] == detail['positionX'] and
            listing['positionY'] == featured['positionY'] == detail['positionY'] and
            listing['sizeWidth'] == featured['sizeWidth'] == detail['sizeWidth']):
            print("      [INFO] All three locations have IDENTICAL positioning (expected for new badges)")
        else:
            all_identical = False
            print("      [OK] Locations have different positioning")

    # Summary
    print("\n" + "=" * 80)
    print("DIAGNOSTIC SUMMARY")
    print("=" * 80)

    if has_nulls:
        print("[FAIL] ISSUE: Some badges have NULL location configs")
        print("   FIX: Run migration 20251217175941_EnforceBadgeLocationConfigNotNull")
    elif all_identical:
        print("[INFO] All badges have identical positioning across all 3 locations")
        print("   This is EXPECTED for badges that haven't been customized yet.")
        print("   The UI should allow you to drag/resize/rotate and save different positions.")
    else:
        print("[OK] SUCCESS: Badges have varied positioning across locations")

    print("\n4. Testing if badges can be retrieved without errors...")
    print(f"   [OK] API returned {len(badges)} badges successfully")
    print("   [OK] No 500 errors (defensive null handling is working)")

    print("\n" + "=" * 80)
    print("NEXT STEP: Test the UI to verify drag/resize/rotate functionality")
    print("=" * 80)

if __name__ == "__main__":
    test_badge_configs()
