#!/usr/bin/env python3
"""
Phase 6A.X: Test attendees endpoint to verify ticket data with QR codes
"""

import requests
import json

# API Base URL
BASE_URL = "https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io"

# Test data
EVENT_ID = "d543629f-a5ba-4475-b124-3d0fc5200f2f"
REGISTRATION_ID = "18422a29-61f7-4575-87d2-72ac0b1581d1"
LOGIN_EMAIL = "niroshhh@gmail.com"
LOGIN_PASSWORD = "12!@qwASzx"

print("=" * 60)
print("Testing Attendees API - Phase 6A.X QR Code Feature")
print("=" * 60)

# Step 1: Login and get JWT token
print("\n1. Logging in...")
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
    exit(1)

login_data = response.json()
token = login_data.get("accessToken")

print(f"   [OK] Login successful")

# Step 2: Get attendees list
print(f"\n2. Getting attendees list for event {EVENT_ID}...")

response = requests.get(
    f"{BASE_URL}/api/Events/{EVENT_ID}/attendees",
    headers={
        "Authorization": f"Bearer {token}",
        "Content-Type": "application/json"
    }
)

print(f"   HTTP Status: {response.status_code}")

if response.status_code != 200:
    print(f"   [ERROR] Request failed")
    print(f"   Response: {response.text}")
    exit(1)

attendees_data = response.json()
print(f"   Response type: {type(attendees_data)}")

if isinstance(attendees_data, dict):
    print(f"   Response keys: {attendees_data.keys()}")
    # Maybe the attendees are in a 'data' or 'attendees' field?
    attendees = attendees_data.get('data', attendees_data.get('attendees', []))
    if not attendees:
        print(f"   [DEBUG] Full response: {json.dumps(attendees_data, indent=2)[:500]}")
        attendees = []
else:
    attendees = attendees_data

print(f"   [OK] Retrieved {len(attendees)} attendees")

# Step 3: Find our test registration
print(f"\n3. Finding registration {REGISTRATION_ID}...")

test_attendee = None
for attendee in attendees:
    if attendee.get("registrationId") == REGISTRATION_ID:
        test_attendee = attendee
        break

if not test_attendee:
    print(f"   [ERROR] Registration {REGISTRATION_ID} not found in attendees list")
    exit(1)

print(f"   [OK] Found registration")
print(f"\n   Registration Details:")
print(f"   - Registration ID: {test_attendee.get('registrationId')}")
print(f"   - Status: {test_attendee.get('status')}")
print(f"   - Payment Status: {test_attendee.get('paymentStatus')}")
print(f"   - Has Ticket: {test_attendee.get('hasTicket')}")
print(f"   - Ticket Code: {test_attendee.get('ticketCode')}")
print(f"   - QR Code Data: {test_attendee.get('qrCodeData')[:50] if test_attendee.get('qrCodeData') else 'None'}...")

# Step 4: Verify ticket data
print(f"\n4. Verifying ticket data...")

has_ticket = test_attendee.get('hasTicket')
ticket_code = test_attendee.get('ticketCode')
qr_code_data = test_attendee.get('qrCodeData')

if has_ticket and ticket_code and qr_code_data:
    print(f"   [SUCCESS] Ticket data is present!")
    print(f"   - Ticket Code format: {ticket_code}")
    print(f"   - QR Code Data length: {len(qr_code_data)} characters")
else:
    print(f"   [WARNING] Ticket data missing:")
    print(f"   - Has Ticket: {has_ticket}")
    print(f"   - Ticket Code: {ticket_code}")
    print(f"   - QR Code Data: {'Present' if qr_code_data else 'Missing'}")

print("\n" + "=" * 60)
print("SUMMARY:")
print(f"- Attendees API: Working")
print(f"- LEFT JOIN: {'Working' if 'ticketCode' in test_attendee else 'Missing ticket fields'}")
print(f"- QR Code Display: {'Ready' if qr_code_data else 'Ticket not generated yet'}")
print("=" * 60)
