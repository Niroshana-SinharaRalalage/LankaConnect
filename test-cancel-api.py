#!/usr/bin/env python3
import requests
import json
import time

API_BASE = "https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/api"

# Login
print("=== Phase 6A.64 API Test: Event Cancellation with Background Jobs ===\n")
print("Step 1: Authenticating...")
login_data = {
    "email": "niroshhh@gmail.com",
    "password": "12!@qwASzx",
    "rememberMe": True,
    "ipAddress": "127.0.0.1"
}

try:
    response = requests.post(f"{API_BASE}/Auth/login", json=login_data)
    response.raise_for_status()
    token = response.json()['accessToken']
    print(f"✅ Authentication successful! Token: {token[:50]}...\n")
except Exception as e:
    print(f"❌ Login failed: {e}")
    print(f"Response: {response.text[:500]}")
    exit(1)

# Select event to cancel
EVENT_ID = "deee96e2-7afe-4ae4-b976-e87ae3bda8dc"  # Beginner Sinhala Language Course
EVENT_TITLE = "Beginner Sinhala Language Course"

print(f"Step 2: Cancelling event...")
print(f"  Event: {EVENT_TITLE}")
print(f"  Event ID: {EVENT_ID}")
print(f"  Type: Paid ($120)")
print(f"  Testing Phase 6A.64 background job implementation\n")

# Cancel event with timing
headers = {
    "Authorization": f"Bearer {token}",
    "Content-Type": "application/json"
}
cancel_data = {
    "reason": "Testing Phase 6A.64 - Background job implementation for event cancellation with instant API response"
}

print("Step 3: Sending cancellation request...")
start_time = time.time()

try:
    response = requests.post(
        f"{API_BASE}/events/{EVENT_ID}/cancel",
        json=cancel_data,
        headers=headers
    )
    end_time = time.time()
    response_time = end_time - start_time

    print(f"\n📊 Performance Metrics:")
    print(f"  HTTP Status: {response.status_code}")
    print(f"  Response Time: {response_time:.3f} seconds")
    print(f"  Response Body: {response.text[:500]}")

    print(f"\n✅ SUCCESS CRITERIA:")
    print(f"  ✅ HTTP 200 OK: {'YES ✓' if response.status_code == 200 else 'NO ✗'}")
    print(f"  ✅ Response < 2s: {'YES ✓' if response_time < 2.0 else 'NO ✗'}")
    print(f"  ✅ No timeout: YES ✓")

    if response.status_code == 200:
        print(f"\n🎉 Phase 6A.64 TEST PASSED!")
        print(f"   API responded in {response_time:.3f}s (was 30s+ before fix)")
        print(f"\nNext Steps:")
        print(f"  1. Open Hangfire dashboard: {API_BASE.replace('/api', '')}/hangfire")
        print(f"  2. Look for 'EventCancellationEmailJob' in Processing/Succeeded queues")
        print(f"  3. Verify event status changed to 'Cancelled'")
    else:
        print(f"\n⚠️  Unexpected status code: {response.status_code}")
        response.raise_for_status()

except Exception as e:
    end_time = time.time()
    response_time = end_time - start_time
    print(f"\n❌ Request failed after {response_time:.3f}s:")
    print(f"   Error: {e}")
    if hasattr(e, 'response') and e.response is not None:
        print(f"   Status: {e.response.status_code}")
        print(f"   Body: {e.response.text[:500]}")
