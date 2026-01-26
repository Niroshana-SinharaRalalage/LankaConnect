#!/usr/bin/env python3
"""
Phase 6A.X: Test resend confirmation endpoint
"""

import requests
import json

# API Base URL
BASE_URL = "https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io"

# Test data
EVENT_ID = "d543629f-a5ba-4475-b124-3d0fc5200f2f"
REGISTRATION_ID = "c68a9580-0de3-4648-b4d6-69a49b44e826"  # Updated for testing after original was deleted
LOGIN_EMAIL = "niroshhh@gmail.com"
LOGIN_PASSWORD = "12!@qwASzx"

print("=" * 60)
print("Testing Resend Confirmation Endpoint - Phase 6A.X")
print("=" * 60)

# Step 1: Login and get JWT token
print("\n1. Logging in to get JWT token...")
login_payload = {
    "email": LOGIN_EMAIL,
    "password": LOGIN_PASSWORD,
    "rememberMe": True,
    "ipAddress": "127.0.0.1"
}

response = requests.post(
    f"{BASE_URL}/api/Auth/login",
    json=login_payload,
    headers={"Content-Type": "application/json"}
)

if response.status_code != 200:
    print(f"   [ERROR] Login failed: {response.status_code}")
    print(f"   Response: {response.text}")
    exit(1)

login_data = response.json()

token = login_data.get("accessToken")
user_data = login_data.get("user", {})
user_role = user_data.get("role")

if not token:
    print(f"   [ERROR] No token in response")
    print(f"   Full response: {json.dumps(login_data, indent=2)}")
    exit(1)

print(f"   [OK] Login successful")
print(f"   Role: {user_role}")
print(f"   Token: {token[:50]}...")

# Step 2: Test resend confirmation endpoint
print(f"\n2. Testing resend confirmation endpoint...")
print(f"   Event ID: {EVENT_ID}")
print(f"   Registration ID: {REGISTRATION_ID}")

resend_url = f"{BASE_URL}/api/Events/{EVENT_ID}/attendees/{REGISTRATION_ID}/resend-confirmation"

response = requests.post(
    resend_url,
    headers={
        "Authorization": f"Bearer {token}",
        "Content-Type": "application/json"
    }
)

print(f"\n   HTTP Status: {response.status_code}")

if response.status_code == 200:
    print(f"   [SUCCESS] Resend confirmation successful!")
    print(f"   Response: {response.json()}")
    print("\n" + "=" * 60)
    print("RESULT: Endpoint is working correctly!")
    print("Check email inbox for confirmation email with ticket PDF")
    print("=" * 60)
elif response.status_code == 400:
    print(f"   [ERROR] Bad Request - Business validation failed")
    print(f"   Response: {response.text}")
elif response.status_code == 401:
    print(f"   [ERROR] Unauthorized - Token may be invalid")
    print(f"   Response: {response.text}")
elif response.status_code == 403:
    print(f"   [ERROR] Forbidden - User does not have permission")
    print(f"   Response: {response.text}")
else:
    print(f"   [ERROR] Unexpected status code")
    print(f"   Response: {response.text}")

print("\n" + "=" * 60)