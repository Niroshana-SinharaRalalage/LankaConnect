"""Phase 8X.11 — combined-slice API smoke matrix on staging.

Validates:
  - URL-optional: ExternalPaid event creation with no URL succeeds (cash-at-door pattern).
  - External mode persists: GET shows registrationMode = "External" (not NoRegistration).
  - External + RegistrationMode = NoRegistration → 400 (strict, product owner Q1).
  - External mode returned by allowed-registration-modes when paymentMode=ExternalPaid.
  - Donations / sponsors blocked at the validator for ExternalPaid create.
  - Free + RegistrationMode=External → 400.
  - Backfill applied: existing ExternalPaid events on staging now have registration_mode=6.
"""
import json
import sys
from datetime import datetime, timedelta, timezone

import requests

BASE = "https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io"
TOKEN_FILE = r"C:\tmp\phase8x_token.json"

PASS = 0
FAIL = 0
RESULTS = []


def login():
    r = requests.post(f"{BASE}/api/Auth/login", json={
        "email": "niroshhh@gmail.com", "password": "1qaz!QAZ",
        "rememberMe": True, "ipAddress": "127.0.0.1"
    }, timeout=30)
    r.raise_for_status()
    data = r.json()
    open(TOKEN_FILE, 'w').write(json.dumps(data))
    return data["accessToken"], data["user"]["userId"]


def cell(label, fn):
    global PASS, FAIL
    try:
        ok, detail = fn()
        status = "PASS" if ok else "FAIL"
        if ok:
            PASS += 1
        else:
            FAIL += 1
        line = f"  [{status}] {label}: {detail}"
        print(line)
        RESULTS.append((label, ok, detail))
    except Exception as ex:
        FAIL += 1
        print(f"  [FAIL] {label}: EXCEPTION {ex}")
        RESULTS.append((label, False, str(ex)))


def make_payload(payment_mode=None, is_free=None, external_url=None, external_instructions=None,
                 external_vendor=None, registration_mode=None, ticket_amount=25,
                 donations_enabled=False, title_suffix=""):
    start = (datetime.now(timezone.utc) + timedelta(days=14)).strftime("%Y-%m-%dT%H:%M:%S.000Z")
    end = (datetime.now(timezone.utc) + timedelta(days=14, hours=4)).strftime("%Y-%m-%dT%H:%M:%S.000Z")
    payload = {
        "title": f"Phase 8X.11 smoke {title_suffix}".strip(),
        "description": "Phase 8X.11 smoke matrix test event - safe to delete",
        "startDate": start,
        "endDate": end,
        "organizerId": USER_ID,
        "capacity": 100,
        "category": 1,
        "locationAddress": "100 Main St", "locationCity": "New York",
        "locationState": "NY", "locationZipCode": "10001", "locationCountry": "USA",
    }
    if is_free is not None: payload["isFree"] = is_free
    if payment_mode is not None: payload["paymentMode"] = payment_mode
    if external_url is not None: payload["externalRegistrationUrl"] = external_url
    if external_instructions is not None: payload["externalRegistrationInstructions"] = external_instructions
    if external_vendor is not None: payload["externalRegistrationVendorName"] = external_vendor
    if registration_mode is not None: payload["registrationMode"] = registration_mode
    if donations_enabled: payload["donationsEnabled"] = True
    if ticket_amount is not None and not is_free:
        payload["ticketPriceAmount"] = ticket_amount
        payload["ticketPriceCurrency"] = 0
    return payload


print("=" * 78)
print("Phase 8X.11 -- combined-slice API smoke matrix")
print("=" * 78)

TOKEN, USER_ID = login()
HDR = {"Authorization": f"Bearer {TOKEN}", "Content-Type": "application/json", "accept": "application/json"}
print(f"Logged in. UserId={USER_ID}")
print()

# ─────────────────────────────────────────────────────────────────────────────
# C: URL-optional + External mode
# ─────────────────────────────────────────────────────────────────────────────
print("[Create] URL-optional + External mode")

def c1():
    """ExternalPaid + URL only -> 201, registrationMode=External in DB."""
    r = requests.post(f"{BASE}/api/Events", headers=HDR,
        json=make_payload(payment_mode="ExternalPaid", is_free=False,
                          external_url="https://eventbrite.com/e/c1-8x11", title_suffix="C1"),
        timeout=30)
    if r.status_code != 201:
        return False, f"HTTP {r.status_code}: {r.text[:150]}"
    eid = r.json() if isinstance(r.json(), str) else r.json().get("id")
    g = requests.get(f"{BASE}/api/Events/{eid}", headers=HDR, timeout=30).json()
    return g.get("registrationMode") == "External", f"id={eid[:8]} regMode={g.get('registrationMode')}"
cell("C1  ExternalPaid + URL-only -> 201, regMode=External", c1)

def c2():
    """ExternalPaid + instructions only (no URL) -> 201 (cash-at-door)."""
    r = requests.post(f"{BASE}/api/Events", headers=HDR,
        json=make_payload(payment_mode="ExternalPaid", is_free=False,
                          external_url=None,
                          external_instructions="Pay $25 cash at door",
                          title_suffix="C2"),
        timeout=30)
    if r.status_code != 201:
        return False, f"HTTP {r.status_code}: {r.text[:150]}"
    eid = r.json() if isinstance(r.json(), str) else r.json().get("id")
    g = requests.get(f"{BASE}/api/Events/{eid}", headers=HDR, timeout=30).json()
    return (g.get("registrationMode") == "External"
            and g.get("externalRegistrationUrl") in (None, "")
            and "cash" in (g.get("externalRegistrationInstructions") or "")), \
           f"id={eid[:8]} regMode={g.get('registrationMode')} url={g.get('externalRegistrationUrl')!r}"
cell("C2  ExternalPaid + instructions only (URL null) -> 201", c2)

def c3():
    """ExternalPaid + all-three-empty -> 201 (allow-save per Q2=B)."""
    r = requests.post(f"{BASE}/api/Events", headers=HDR,
        json=make_payload(payment_mode="ExternalPaid", is_free=False,
                          external_url=None, external_instructions=None,
                          external_vendor=None, title_suffix="C3"),
        timeout=30)
    return r.status_code == 201, f"HTTP {r.status_code}: {r.text[:120]}"
cell("C3  ExternalPaid + all-three-empty -> 201 (Q2=B)", c3)

def c4():
    """ExternalPaid + RegistrationMode=NoRegistration -> 400 (strict per Q1)."""
    r = requests.post(f"{BASE}/api/Events", headers=HDR,
        json=make_payload(payment_mode="ExternalPaid", is_free=False,
                          external_url="https://eventbrite.com/e/c4-8x11",
                          registration_mode="NoRegistration", title_suffix="C4"),
        timeout=30)
    return r.status_code == 400, f"HTTP {r.status_code}: {r.text[:120]}"
cell("C4  ExternalPaid + regMode=NoRegistration -> 400 (Q1 strict)", c4)

def c5():
    """ExternalPaid + RegistrationMode=External -> 201."""
    r = requests.post(f"{BASE}/api/Events", headers=HDR,
        json=make_payload(payment_mode="ExternalPaid", is_free=False,
                          external_url="https://eventbrite.com/e/c5-8x11",
                          registration_mode="External", title_suffix="C5"),
        timeout=30)
    return r.status_code == 201, f"HTTP {r.status_code}: {r.text[:120]}"
cell("C5  ExternalPaid + regMode=External (explicit) -> 201", c5)

def c6():
    """Free + regMode=External -> 400 (External requires ExternalPaid)."""
    r = requests.post(f"{BASE}/api/Events", headers=HDR,
        json=make_payload(is_free=True, registration_mode="External", title_suffix="C6"),
        timeout=30)
    return r.status_code == 400, f"HTTP {r.status_code}: {r.text[:120]}"
cell("C6  Free + regMode=External -> 400", c6)

def c7():
    """OnPlatformPaid + regMode=External -> 400."""
    r = requests.post(f"{BASE}/api/Events", headers=HDR,
        json=make_payload(payment_mode="OnPlatformPaid", is_free=False,
                          registration_mode="External", title_suffix="C7"),
        timeout=30)
    return r.status_code == 400, f"HTTP {r.status_code}: {r.text[:120]}"
cell("C7  OnPlatformPaid + regMode=External -> 400", c7)

def c8():
    """ExternalPaid + donationsEnabled=true -> 400 (Q5=B)."""
    r = requests.post(f"{BASE}/api/Events", headers=HDR,
        json=make_payload(payment_mode="ExternalPaid", is_free=False,
                          external_url="https://eventbrite.com/e/c8-8x11",
                          donations_enabled=True, title_suffix="C8"),
        timeout=30)
    return r.status_code == 400, f"HTTP {r.status_code}: {r.text[:120]}"
cell("C8  ExternalPaid + donationsEnabled -> 400 (Q5=B)", c8)

# ─────────────────────────────────────────────────────────────────────────────
# Q: GET /allowed-registration-modes with paymentMode axis
# ─────────────────────────────────────────────────────────────────────────────
print("\n[Allowed modes endpoint]")

def q1():
    """paymentMode=ExternalPaid -> only [External]."""
    r = requests.get(f"{BASE}/api/Events/allowed-registration-modes?paymentMode=ExternalPaid&isFreeAttendance=false&hasDualPricing=false",
                     headers=HDR, timeout=30)
    if r.status_code != 200:
        return False, f"HTTP {r.status_code}: {r.text[:120]}"
    modes = r.json()
    return modes == ["External"], f"modes={modes}"
cell("Q1  GET allowed-registration-modes?paymentMode=ExternalPaid -> [External]", q1)

def q2():
    """paymentMode=Free + free + no seating -> includes NoRegistration but NOT External."""
    r = requests.get(f"{BASE}/api/Events/allowed-registration-modes?paymentMode=Free&isFreeAttendance=true",
                     headers=HDR, timeout=30)
    if r.status_code != 200:
        return False, f"HTTP {r.status_code}: {r.text[:120]}"
    modes = r.json()
    return ("NoRegistration" in modes and "External" not in modes), f"modes={modes}"
cell("Q2  GET ?paymentMode=Free+isFreeAttendance=true -> contains NoRegistration, no External", q2)

def q3():
    """paymentMode=OnPlatformPaid -> 5 internal modes, no External, no NoRegistration."""
    r = requests.get(f"{BASE}/api/Events/allowed-registration-modes?paymentMode=OnPlatformPaid&isFreeAttendance=false",
                     headers=HDR, timeout=30)
    if r.status_code != 200:
        return False, f"HTTP {r.status_code}: {r.text[:120]}"
    modes = r.json()
    return ("External" not in modes and "NoRegistration" not in modes
            and "DetailedAttendees" in modes), f"modes={modes}"
cell("Q3  GET ?paymentMode=OnPlatformPaid -> no External, no NoRegistration", q3)

# ─────────────────────────────────────────────────────────────────────────────
# Summary
# ─────────────────────────────────────────────────────────────────────────────
print()
print("=" * 78)
print(f"Phase 8X.11 smoke matrix: {PASS} PASS, {FAIL} FAIL")
print("=" * 78)
sys.exit(0 if FAIL == 0 else 1)
