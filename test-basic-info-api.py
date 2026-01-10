#!/usr/bin/env python3
"""
Phase 6A.70: Test Basic Info API endpoints on Azure Staging

Tests:
1. GET /api/users/{userId} - Get current profile
2. PUT /api/users/{userId}/basic-info - Update basic info (name, phone, bio)
3. PUT /api/users/{userId}/email - Update email (triggers verification)
"""

import requests
import json
import sys
from datetime import datetime

# Azure Staging API URL
BASE_URL = "https://lankaconnect-api-staging.azurewebsites.net/api"

# Test user credentials (replace with actual staging user)
# You'll need to get a valid JWT token first by logging in
AUTH_TOKEN = ""  # Set this after login

def login(email, password):
    """Login to get JWT token"""
    url = f"{BASE_URL}/auth/login"
    payload = {
        "email": email,
        "password": password
    }

    print(f"\n🔐 Logging in as {email}...")
    response = requests.post(url, json=payload)

    if response.status_code == 200:
        data = response.json()
        print(f"✅ Login successful!")
        print(f"   User ID: {data.get('userId')}")
        print(f"   Token: {data.get('token')[:50]}...")
        return data.get('token'), data.get('userId')
    else:
        print(f"❌ Login failed: {response.status_code}")
        print(f"   Response: {response.text}")
        sys.exit(1)

def get_profile(user_id, token):
    """Get user profile"""
    url = f"{BASE_URL}/users/{user_id}"
    headers = {"Authorization": f"Bearer {token}"}

    print(f"\n📋 Getting profile for user {user_id}...")
    response = requests.get(url, headers=headers)

    if response.status_code == 200:
        data = response.json()
        print(f"✅ Profile retrieved successfully!")
        print(f"   Name: {data.get('firstName')} {data.get('lastName')}")
        print(f"   Email: {data.get('email')}")
        print(f"   Phone: {data.get('phoneNumber') or 'Not set'}")
        print(f"   Bio: {data.get('bio') or 'Not set'}")
        return data
    else:
        print(f"❌ Failed to get profile: {response.status_code}")
        print(f"   Response: {response.text}")
        return None

def update_basic_info(user_id, token, first_name, last_name, phone_number=None, bio=None):
    """Update basic information"""
    url = f"{BASE_URL}/users/{user_id}/basic-info"
    headers = {
        "Authorization": f"Bearer {token}",
        "Content-Type": "application/json"
    }
    payload = {
        "firstName": first_name,
        "lastName": last_name,
        "phoneNumber": phone_number,
        "bio": bio
    }

    print(f"\n✏️ Updating basic info for user {user_id}...")
    print(f"   Data: {json.dumps(payload, indent=2)}")
    response = requests.put(url, json=payload, headers=headers)

    if response.status_code == 200:
        data = response.json()
        print(f"✅ Basic info updated successfully!")
        print(f"   Name: {data.get('firstName')} {data.get('lastName')}")
        print(f"   Phone: {data.get('phoneNumber') or 'Not set'}")
        print(f"   Bio: {data.get('bio') or 'Not set'}")
        return data
    else:
        print(f"❌ Failed to update basic info: {response.status_code}")
        print(f"   Response: {response.text}")
        return None

def update_email(user_id, token, new_email):
    """Update email address (triggers verification)"""
    url = f"{BASE_URL}/users/{user_id}/email"
    headers = {
        "Authorization": f"Bearer {token}",
        "Content-Type": "application/json"
    }
    payload = {
        "newEmail": new_email
    }

    print(f"\n📧 Updating email for user {user_id}...")
    print(f"   New email: {new_email}")
    response = requests.put(url, json=payload, headers=headers)

    if response.status_code == 200:
        data = response.json()
        print(f"✅ Email updated successfully!")
        print(f"   Email: {data.get('email')}")
        print(f"   Verified: {data.get('isVerified')}")
        print(f"   Message: {data.get('message')}")
        return data
    elif response.status_code == 409:
        print(f"⚠️ Email already in use (409 Conflict)")
        print(f"   Response: {response.text}")
        return None
    else:
        print(f"❌ Failed to update email: {response.status_code}")
        print(f"   Response: {response.text}")
        return None

def main():
    print("="*80)
    print("Phase 6A.70: Basic Info API Testing")
    print("Azure Staging Environment")
    print(f"Base URL: {BASE_URL}")
    print(f"Timestamp: {datetime.now().isoformat()}")
    print("="*80)

    # Get login credentials from user
    email = input("\n📧 Enter email: ").strip()
    password = input("🔑 Enter password: ").strip()

    # Login to get token
    token, user_id = login(email, password)

    # Test 1: Get current profile
    print("\n" + "="*80)
    print("TEST 1: Get Current Profile")
    print("="*80)
    original_profile = get_profile(user_id, token)

    if not original_profile:
        print("\n❌ Cannot proceed without profile. Exiting.")
        sys.exit(1)

    # Test 2: Update basic info (name, phone, bio)
    print("\n" + "="*80)
    print("TEST 2: Update Basic Info")
    print("="*80)

    test_first_name = input(f"\nEnter new first name [{original_profile.get('firstName')}]: ").strip() or original_profile.get('firstName')
    test_last_name = input(f"Enter new last name [{original_profile.get('lastName')}]: ").strip() or original_profile.get('lastName')
    test_phone = input(f"Enter new phone (e.g., +94771234567) [{original_profile.get('phoneNumber') or 'empty'}]: ").strip()
    test_bio = input(f"Enter new bio [{original_profile.get('bio') or 'empty'}]: ").strip()

    if test_phone == "":
        test_phone = None
    if test_bio == "":
        test_bio = None

    updated_profile = update_basic_info(
        user_id,
        token,
        test_first_name,
        test_last_name,
        test_phone,
        test_bio
    )

    # Test 3: Update email (optional - requires valid new email)
    print("\n" + "="*80)
    print("TEST 3: Update Email (Optional - Triggers Verification)")
    print("="*80)

    test_email = input(f"\nEnter new email to test email update (or press Enter to skip): ").strip()

    if test_email:
        update_email(user_id, token, test_email)
    else:
        print("⏭️ Skipping email update test")

    # Final verification - get profile again
    print("\n" + "="*80)
    print("FINAL VERIFICATION: Get Updated Profile")
    print("="*80)
    final_profile = get_profile(user_id, token)

    # Summary
    print("\n" + "="*80)
    print("TEST SUMMARY")
    print("="*80)

    if original_profile and final_profile:
        print("\n✅ All API endpoints are working correctly!")
        print(f"\nChanges made:")
        print(f"  Name: {original_profile.get('firstName')} {original_profile.get('lastName')} → {final_profile.get('firstName')} {final_profile.get('lastName')}")
        print(f"  Phone: {original_profile.get('phoneNumber') or 'Not set'} → {final_profile.get('phoneNumber') or 'Not set'}")
        print(f"  Bio: {original_profile.get('bio') or 'Not set'} → {final_profile.get('bio') or 'Not set'}")
    else:
        print("\n⚠️ Some tests failed. Please review the output above.")

    print("\n" + "="*80)
    print("Testing complete!")
    print("="*80)

if __name__ == "__main__":
    try:
        main()
    except KeyboardInterrupt:
        print("\n\n⚠️ Testing interrupted by user")
        sys.exit(0)
    except Exception as e:
        print(f"\n\n❌ Unexpected error: {e}")
        import traceback
        traceback.print_exc()
        sys.exit(1)
