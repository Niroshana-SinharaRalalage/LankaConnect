#!/usr/bin/env python3
"""
Phase 7F-E.4b — create a B4 (HeadCountByAgeAndGender) + Tiered staging event so the
operator can browser-verify the merged 4-leaf per-tier layout. Operator-flagged gap
on 2026-05-03: zero published B4 + tiered events on staging meant the merged
4-leaf path had only unit-test coverage.

Event shape (architect-reviewed defaults):
  - title: "7F-E.4b smoke B4 tiered (delete after test)"
  - registration_mode: HeadCountByAgeAndGender (B4 → mergeFourLeaf branch)
  - ticketing_mode: Tiered
  - 2 tiers:
      VIP       — adult $50 + ChildPrice $25 → exercises ChildPrice-helper
                  (B4 mergeFourLeaf renders 4-leaf inline regardless of ChildPrice)
      Standard  — adult $30 only (no ChildPrice) → confirms 4-leaf still renders
                  even on tiers without child pricing
  - capacity: 50
  - max attendees per registration: 10
  - paid (tiered + non-zero prices); operator can fill the form to see the merged
    layout without needing to complete Stripe checkout

Steps:
  1. Login as niroshhh@gmail.com
  2. POST /api/Events with B4 RegistrationMode
  3. PUT /api/Events/{id}/ticketing-mode → Tiered
  4. POST /api/Events/{id}/ticket-tiers × 2
  5. POST /api/Events/{id}/publish
  6. Print the event URL for browser verification
"""

import json
import sys
import urllib.error
import urllib.request
from datetime import datetime, timedelta, timezone

sys.stdout.reconfigure(encoding="utf-8")

BASE = "https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io"
UI_BASE = "https://lankaconnect-ui-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io"


def login() -> tuple[str, str]:
    body = json.dumps({
        "email": "niroshhh@gmail.com",
        "password": "1qaz!QAZ",
        "rememberMe": True,
        "ipAddress": "string",
    }).encode()
    req = urllib.request.Request(
        f"{BASE}/api/Auth/login",
        data=body,
        headers={"Content-Type": "application/json"},
    )
    resp = urllib.request.urlopen(req, timeout=30)
    d = json.loads(resp.read())
    return d["accessToken"], d["user"]["userId"]


def call(method: str, token: str, path: str, body=None) -> tuple[int, str]:
    data = json.dumps(body).encode() if body is not None else None
    headers = {"Authorization": f"Bearer {token}"}
    if data is not None:
        headers["Content-Type"] = "application/json"
    req = urllib.request.Request(
        f"{BASE}{path}", data=data, method=method, headers=headers
    )
    try:
        resp = urllib.request.urlopen(req, timeout=60)
        text = resp.read().decode() if resp.status != 204 else ""
        return resp.status, text
    except urllib.error.HTTPError as e:
        return e.code, e.read().decode()


def main() -> int:
    print("=" * 100)
    print("Phase 7F-E.4b — create B4 + tiered staging test event")
    print("=" * 100)

    token, user_id = login()
    print(f"Logged in: {user_id}")

    # Schedule the event ~10 days out so it doesn't conflict with the operator's
    # existing reminder/notification crons but still appears in the published list.
    start = (datetime.now(timezone.utc) + timedelta(days=10)).replace(
        hour=18, minute=0, second=0, microsecond=0
    )
    end = start + timedelta(hours=4)

    # 1. Create the B4 event
    create_payload = {
        "Title": "7F-E.4b smoke B4 tiered (delete after test)",
        "Description": "Phase 7F-E.4b operator browser verification — exercises the merged 4-leaf per-tier layout. Mode=HeadCountByAgeAndGender, ticketing=Tiered. Safe to delete after the merged-form test passes.",
        "StartDate": start.isoformat(),
        "EndDate": end.isoformat(),
        "OrganizerId": user_id,
        "Capacity": 50,
        "MaxAttendeesPerRegistration": 10,
        "Category": "Community",
        "LocationAddress": "100 Test St",
        "LocationCity": "Test City",
        "LocationState": "MA",
        "LocationZipCode": "01001",
        "LocationCountry": "United States",
        "LocationName": "Test Hall",
        "PublishOrganizerContact": False,
        "IsFree": False,
        # Phase 7E.2: B4 registration mode
        "RegistrationMode": "HeadCountByAgeAndGender",
    }
    status, text = call("POST", token, "/api/Events", create_payload)
    print(f"\n[1/4] POST /api/Events  → HTTP {status}")
    if status not in (200, 201):
        print(f"  ! body: {text[:500]}")
        return 1
    payload = json.loads(text)
    # Backend returns the new GUID at top-level or under .id depending on shape
    event_id = payload if isinstance(payload, str) else payload.get("id") or payload.get("eventId")
    print(f"  event_id: {event_id}")

    # 2. Switch to Tiered ticketing
    status, text = call(
        "PUT", token, f"/api/Events/{event_id}/ticketing-mode",
        {"TicketingMode": "Tiered"},
    )
    print(f"\n[2/4] PUT /ticketing-mode Tiered  → HTTP {status}")
    if status not in (200, 204):
        print(f"  ! body: {text[:500]}")
        return 1

    # 3. Add tiers — VIP with ChildPrice + Standard without
    tiers = [
        {
            "Name": "VIP",
            "Description": "Premium tier with child pricing",
            "AdultPriceAmount": 50.00,
            "AdultPriceCurrency": "USD",
            "ChildPriceAmount": 25.00,
            "ChildPriceCurrency": "USD",
            "ChildAgeLimit": 12,
            "Capacity": 20,
            "MaxPerUser": 10,
            "SortOrder": 1,
        },
        {
            "Name": "Standard",
            "Description": "Adult-only tier (no child pricing)",
            "AdultPriceAmount": 30.00,
            "AdultPriceCurrency": "USD",
            "ChildPriceAmount": None,
            "ChildPriceCurrency": None,
            "ChildAgeLimit": None,
            "Capacity": 30,
            "MaxPerUser": 10,
            "SortOrder": 2,
        },
    ]
    for tier in tiers:
        status, text = call("POST", token, f"/api/Events/{event_id}/ticket-tiers", tier)
        print(f"\n[3/4] POST /ticket-tiers '{tier['Name']}'  → HTTP {status}")
        if status not in (200, 201):
            print(f"  ! body: {text[:500]}")
            return 1
        print(f"  tier_id: {text[:80]}")

    # 4. Publish
    status, text = call("POST", token, f"/api/Events/{event_id}/publish")
    print(f"\n[4/4] POST /publish  → HTTP {status}")
    if status not in (200, 204):
        print(f"  ! body: {text[:500]}")
        return 1

    # Done — print operator-facing summary
    print()
    print("=" * 100)
    print("RESULT: PASS — B4 + tiered staging event ready for browser verification")
    print("=" * 100)
    print(f"\nEvent ID: {event_id}")
    print(f"\nBrowse to:")
    print(f"  {UI_BASE}/events/{event_id}")
    print()
    print("Browser verification checklist (per architect plan):")
    print("  1. Log in as niroshhh@gmail.com / 1qaz!QAZ")
    print("  2. Open the event URL above")
    print("  3. Click 'RSVP'")
    print("  4. In the 'Choose your ticket tier(s)' card, click + on VIP to bump count to 2")
    print("     → Expected: per-tier 'Adult Males / Adult Females / Child Males / Child Females'")
    print("       spinners appear inline UNDER the VIP tier card")
    print("  5. Click + on Standard to bump count to 1")
    print("     → Expected: same 4-leaf spinners under Standard (B4 merge is independent of ChildPrice)")
    print("  6. Confirm there is NO separate top-level 'How many people are you bringing' section")
    print("     with 4-leaf spinners — top-level is HIDDEN under the merged layout.")
    print("  7. Adjust any leaf — others auto-rebalance so the per-tier sum equals tier count.")
    print("  8. Open DevTools → Network. Submit. The POST body's headCount must contain")
    print("     adultMales / adultFemales / childMales / childFemales summed across both tiers.")
    print("     (Will redirect to Stripe; you can cancel without paying — the form-side test is done.)")
    print()
    return 0


if __name__ == "__main__":
    sys.exit(main())
