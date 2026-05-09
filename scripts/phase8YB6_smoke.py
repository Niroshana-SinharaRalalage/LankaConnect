"""Phase 8YB.6 — TBD-as-regular API smoke (C23/C24/C25)."""
import json
import sys
from datetime import datetime, timedelta, timezone

import requests

BASE = "https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io"

PASS = 0
FAIL = 0


def login():
    r = requests.post(f"{BASE}/api/Auth/login", json={
        "email": "niroshhh@gmail.com", "password": "1qaz!QAZ",
        "rememberMe": True, "ipAddress": "127.0.0.1"
    }, timeout=30)
    r.raise_for_status()
    data = r.json()
    return data["accessToken"], data["user"]["userId"]


def cell(label, fn):
    global PASS, FAIL
    try:
        ok, detail = fn()
        status = "PASS" if ok else "FAIL"
        if ok: PASS += 1
        else:  FAIL += 1
        print(f"  [{status}] {label}: {detail}")
    except Exception as ex:
        FAIL += 1
        print(f"  [FAIL] {label}: EXCEPTION {ex}")


def make_planning_payload(payment_mode="Free", title_suffix="", external_url=None,
                          vendor=None, instructions=None):
    payload = {
        "title": f"Phase 8YB.6 smoke {title_suffix}".strip(),
        "description": "Phase 8YB.6 TBD-as-regular - safe to delete",
        "startDate": None, "endDate": None,
        "datesUnknown": True,
        "organizerId": USER_ID,
        "capacity": 50, "category": 1,
        "locationAddress": "100 Main St", "locationCity": "New York",
        "locationState": "NY", "locationZipCode": "10001", "locationCountry": "USA",
        "isFree": payment_mode == "Free",
        "paymentMode": payment_mode,
        "publishOrganizerContact": True,
        "organizerContacts": [{
            "contactName": "Smoke Tester", "contactEmail": "smoke@example.com",
            "contactPhone": None, "isPrimary": True
        }],
    }
    if payment_mode == "OnPlatformPaid":
        payload["ticketPriceAmount"] = 25
        payload["ticketPriceCurrency"] = "USD"
    if payment_mode == "ExternalPaid":
        if external_url: payload["externalRegistrationUrl"] = external_url
        if vendor:       payload["externalRegistrationVendorName"] = vendor
        if instructions: payload["externalRegistrationInstructions"] = instructions
    return payload


print("=" * 78)
print("Phase 8YB.6 -- TBD-as-regular API smoke (C23/C24/C25)")
print("=" * 78)

TOKEN, USER_ID = login()
HDR = {"Authorization": f"Bearer {TOKEN}", "Content-Type": "application/json", "accept": "application/json"}
HDR_ANON = {"Content-Type": "application/json", "accept": "application/json"}
print(f"Logged in. UserId={USER_ID}\n")

# ─────────────────────────────────────────────────────────────────────────────
# C23 — RSVP on TBD-Published Free event succeeds
# ─────────────────────────────────────────────────────────────────────────────

def c23():
    """Free TBD event -> publish -> RSVP -> 200/201 (NEW: was 400 in 8YA.1)."""
    create = requests.post(f"{BASE}/api/Events", headers=HDR,
                           json=make_planning_payload("Free", "C23-Free"), timeout=30)
    if create.status_code != 201:
        return False, f"create HTTP {create.status_code}"
    eid = create.json() if isinstance(create.json(), str) else create.json().get("id")

    pub = requests.post(f"{BASE}/api/Events/{eid}/publish", headers=HDR, timeout=30)
    if pub.status_code not in (200, 204):
        return False, f"publish HTTP {pub.status_code}"

    # RsvpRequest shape: top-level Email + PhoneNumber (NOT inside contactEmail/contactPhone)
    rsvp = {
        "userId": USER_ID,
        "email": "smoke-c23@example.com",
        "phoneNumber": "+15555550100",
        "attendees": [{
            "name": "C23 Smoke Tester", "ageCategory": "Adult"
        }],
    }
    r = requests.post(f"{BASE}/api/Events/{eid}/rsvp", headers=HDR, json=rsvp, timeout=30)
    return r.status_code in (200, 201, 204), \
           f"id={eid[:8]} rsvp HTTP {r.status_code}: {r.text[:120]}"
cell("C23  RSVP on TBD-Published Free -> 200 (8YB.6 overturn)", c23)

# ─────────────────────────────────────────────────────────────────────────────
# C24 — RSVP on TBD-Published OnPlatformPaid event succeeds
# ─────────────────────────────────────────────────────────────────────────────

def c24():
    """OnPlatformPaid TBD event -> publish -> RSVP -> 200 (Stripe URL or pending)."""
    create = requests.post(f"{BASE}/api/Events", headers=HDR,
                           json=make_planning_payload("OnPlatformPaid", "C24-OnPlat"), timeout=30)
    if create.status_code != 201:
        return False, f"create HTTP {create.status_code}: {create.text[:120]}"
    eid = create.json() if isinstance(create.json(), str) else create.json().get("id")

    pub = requests.post(f"{BASE}/api/Events/{eid}/publish", headers=HDR, timeout=30)
    if pub.status_code not in (200, 204):
        return False, f"publish HTTP {pub.status_code}"

    rsvp = {
        "userId": USER_ID,
        "email": "smoke-c24@example.com",
        "phoneNumber": "+15555550100",
        "successUrl": "https://example.com/success",
        "cancelUrl": "https://example.com/cancel",
        "attendees": [{
            "name": "C24 Smoke Tester", "ageCategory": "Adult"
        }],
    }
    r = requests.post(f"{BASE}/api/Events/{eid}/rsvp", headers=HDR, json=rsvp, timeout=30)
    if r.status_code not in (200, 201, 204):
        return False, f"id={eid[:8]} rsvp HTTP {r.status_code}: {r.text[:200]}"
    return True, f"id={eid[:8]} rsvp HTTP {r.status_code} (paid TBD RSVP works)"
cell("C24  RSVP on TBD-Published OnPlatformPaid -> 200 (8YB.6)", c24)

# ─────────────────────────────────────────────────────────────────────────────
# C25 — TBD-Published ExternalPaid public detail returns external registration fields
# ─────────────────────────────────────────────────────────────────────────────

def c25():
    """ExternalPaid TBD with vendor + instructions -> publish -> GET returns the
       external registration fields (validates the CTA will render with vendor info)."""
    create = requests.post(f"{BASE}/api/Events", headers=HDR,
        json=make_planning_payload("ExternalPaid", "C25-Ext",
                                   vendor="XYZ Test Vendor",
                                   instructions="Connect with XYZ for more info"),
        timeout=30)
    if create.status_code != 201:
        return False, f"create HTTP {create.status_code}: {create.text[:120]}"
    eid = create.json() if isinstance(create.json(), str) else create.json().get("id")

    pub = requests.post(f"{BASE}/api/Events/{eid}/publish", headers=HDR, timeout=30)
    if pub.status_code not in (200, 204):
        return False, f"publish HTTP {pub.status_code}"

    g = requests.get(f"{BASE}/api/Events/{eid}", headers=HDR_ANON, timeout=30).json()
    ok = (g.get("status") == "Published"
          and g.get("startDate") is None
          and g.get("paymentMode") == "ExternalPaid"
          and g.get("externalRegistrationVendorName") == "XYZ Test Vendor"
          and "Connect with XYZ" in (g.get("externalRegistrationInstructions") or ""))
    return ok, (f"id={eid[:8]} status={g.get('status')} startDate={g.get('startDate')} "
                f"vendor={g.get('externalRegistrationVendorName')!r}")
cell("C25  TBD ExternalPaid GET surfaces vendor + instructions (CTA will render)", c25)

def c25b():
    """Niroshana's repro event 541876b8: GET returns the external registration fields."""
    g = requests.get(f"{BASE}/api/Events/541876b8-1ba9-46f3-ab38-3aee2c1b305e",
                     headers=HDR_ANON, timeout=30).json()
    return (g.get("paymentMode") == "ExternalPaid"
            and g.get("externalRegistrationVendorName") == "XYZ"
            and "XYZ" in (g.get("externalRegistrationInstructions") or "")), \
           f"vendor={g.get('externalRegistrationVendorName')!r}"
cell("C25b Niroshana repro 541876b8: vendor+instructions still set", c25b)

print()
print("=" * 78)
print(f"Phase 8YB.6 smoke: {PASS} PASS, {FAIL} FAIL")
print("=" * 78)
sys.exit(0 if FAIL == 0 else 1)
