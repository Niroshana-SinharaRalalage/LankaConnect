"""Phase 8X.12 — combined-slice API smoke matrix on staging.

Builds on phase8x11_smoke.py (8 cells C1-C8 + Q1-Q3 unchanged) and adds D3
(pricing optional on ExternalPaid) cells S.9-S.12.

D3 acceptance:
  S.9  ExternalPaid + null pricing -> 201 (NEW)
  S.10 ExternalPaid + null pricing GET shows null pricing (NEW)
  S.11 ExternalPaid + price=25 still allowed (regression) (NEW)
  S.12 Update existing ExternalPaid: clear pricing -> 200 (NEW)
"""
import json
import sys
from datetime import datetime, timedelta, timezone

import requests

BASE = "https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io"

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
    return data["accessToken"], data["user"]["userId"]


def cell(label, fn):
    global PASS, FAIL
    try:
        ok, detail = fn()
        status = "PASS" if ok else "FAIL"
        if ok: PASS += 1
        else:  FAIL += 1
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
        "title": f"Phase 8X.12 smoke {title_suffix}".strip(),
        "description": "Phase 8X.12 smoke matrix test event - safe to delete",
        "startDate": start, "endDate": end,
        "organizerId": USER_ID, "capacity": 100, "category": 1,
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
print("Phase 8X.12 -- combined-slice API smoke matrix (8X.11 carry-forward + D3)")
print("=" * 78)

TOKEN, USER_ID = login()
HDR = {"Authorization": f"Bearer {TOKEN}", "Content-Type": "application/json", "accept": "application/json"}
print(f"Logged in. UserId={USER_ID}\n")

print("[8X.11 carry-forward — Create matrix]")

def c1():
    r = requests.post(f"{BASE}/api/Events", headers=HDR,
        json=make_payload(payment_mode="ExternalPaid", is_free=False,
                          external_url="https://eventbrite.com/e/c1-8x12", title_suffix="C1"),
        timeout=30)
    if r.status_code != 201: return False, f"HTTP {r.status_code}: {r.text[:150]}"
    eid = r.json() if isinstance(r.json(), str) else r.json().get("id")
    g = requests.get(f"{BASE}/api/Events/{eid}", headers=HDR, timeout=30).json()
    return g.get("registrationMode") == "External", f"id={eid[:8]} regMode={g.get('registrationMode')}"
cell("C1  ExternalPaid + URL-only -> 201, regMode=External", c1)

def c2():
    r = requests.post(f"{BASE}/api/Events", headers=HDR,
        json=make_payload(payment_mode="ExternalPaid", is_free=False,
                          external_url=None,
                          external_instructions="Pay $25 cash at door",
                          title_suffix="C2"),
        timeout=30)
    if r.status_code != 201: return False, f"HTTP {r.status_code}: {r.text[:150]}"
    eid = r.json() if isinstance(r.json(), str) else r.json().get("id")
    g = requests.get(f"{BASE}/api/Events/{eid}", headers=HDR, timeout=30).json()
    return (g.get("registrationMode") == "External"
            and g.get("externalRegistrationUrl") in (None, "")
            and "cash" in (g.get("externalRegistrationInstructions") or "")), \
           f"id={eid[:8]} regMode={g.get('registrationMode')} url={g.get('externalRegistrationUrl')!r}"
cell("C2  ExternalPaid + instructions only -> 201", c2)

def c3():
    r = requests.post(f"{BASE}/api/Events", headers=HDR,
        json=make_payload(payment_mode="ExternalPaid", is_free=False,
                          external_url=None, external_instructions=None,
                          external_vendor=None, title_suffix="C3"),
        timeout=30)
    return r.status_code == 201, f"HTTP {r.status_code}: {r.text[:120]}"
cell("C3  ExternalPaid + all-three-empty -> 201 (Q2=B)", c3)

def c4():
    r = requests.post(f"{BASE}/api/Events", headers=HDR,
        json=make_payload(payment_mode="ExternalPaid", is_free=False,
                          external_url="https://eventbrite.com/e/c4-8x12",
                          registration_mode="NoRegistration", title_suffix="C4"),
        timeout=30)
    return r.status_code == 400, f"HTTP {r.status_code}: {r.text[:120]}"
cell("C4  ExternalPaid + regMode=NoRegistration -> 400", c4)

def c5():
    r = requests.post(f"{BASE}/api/Events", headers=HDR,
        json=make_payload(payment_mode="ExternalPaid", is_free=False,
                          external_url="https://eventbrite.com/e/c5-8x12",
                          registration_mode="External", title_suffix="C5"),
        timeout=30)
    return r.status_code == 201, f"HTTP {r.status_code}: {r.text[:120]}"
cell("C5  ExternalPaid + regMode=External (explicit) -> 201", c5)

def c6():
    r = requests.post(f"{BASE}/api/Events", headers=HDR,
        json=make_payload(is_free=True, registration_mode="External", title_suffix="C6"),
        timeout=30)
    return r.status_code == 400, f"HTTP {r.status_code}: {r.text[:120]}"
cell("C6  Free + regMode=External -> 400", c6)

def c7():
    r = requests.post(f"{BASE}/api/Events", headers=HDR,
        json=make_payload(payment_mode="OnPlatformPaid", is_free=False,
                          registration_mode="External", title_suffix="C7"),
        timeout=30)
    return r.status_code == 400, f"HTTP {r.status_code}: {r.text[:120]}"
cell("C7  OnPlatformPaid + regMode=External -> 400", c7)

def c8():
    r = requests.post(f"{BASE}/api/Events", headers=HDR,
        json=make_payload(payment_mode="ExternalPaid", is_free=False,
                          external_url="https://eventbrite.com/e/c8-8x12",
                          donations_enabled=True, title_suffix="C8"),
        timeout=30)
    return r.status_code == 400, f"HTTP {r.status_code}: {r.text[:120]}"
cell("C8  ExternalPaid + donationsEnabled -> 400 (Q5=B)", c8)

print("\n[D3 — pricing optional on ExternalPaid (NEW)]")

S9_EVENT_ID = None

def s9():
    """ExternalPaid + null pricing -> 201 (D3 acceptance)."""
    global S9_EVENT_ID
    pl = make_payload(payment_mode="ExternalPaid", is_free=False,
                      external_url="https://eventbrite.com/e/s9-8x12",
                      title_suffix="S9")
    pl.pop("ticketPriceAmount", None); pl.pop("ticketPriceCurrency", None)
    r = requests.post(f"{BASE}/api/Events", headers=HDR, json=pl, timeout=30)
    if r.status_code != 201: return False, f"HTTP {r.status_code}: {r.text[:150]}"
    eid = r.json() if isinstance(r.json(), str) else r.json().get("id")
    S9_EVENT_ID = eid
    return True, f"id={eid[:8]}"
cell("S.9   ExternalPaid + null pricing -> 201 (D3)", s9)

def s10():
    """GET S.9 event: pricing summary null + regMode External."""
    if not S9_EVENT_ID: return False, "S.9 didn't create event"
    g = requests.get(f"{BASE}/api/Events/{S9_EVENT_ID}", headers=HDR, timeout=30).json()
    return (g.get("registrationMode") == "External"
            and g.get("ticketPriceAmount") in (None, 0)
            and g.get("paymentMode") in (2, "ExternalPaid")), \
           f"price={g.get('ticketPriceAmount')!r} regMode={g.get('registrationMode')} payMode={g.get('paymentMode')}"
cell("S.10  ExternalPaid null-pricing GET -> price=null/0, regMode=External (D3)", s10)

def s11():
    """ExternalPaid + price=25 still works (regression)."""
    r = requests.post(f"{BASE}/api/Events", headers=HDR,
        json=make_payload(payment_mode="ExternalPaid", is_free=False,
                          external_url="https://eventbrite.com/e/s11-8x12",
                          ticket_amount=25, title_suffix="S11"),
        timeout=30)
    if r.status_code != 201: return False, f"HTTP {r.status_code}: {r.text[:150]}"
    eid = r.json() if isinstance(r.json(), str) else r.json().get("id")
    g = requests.get(f"{BASE}/api/Events/{eid}", headers=HDR, timeout=30).json()
    return g.get("ticketPriceAmount") == 25, f"id={eid[:8]} price={g.get('ticketPriceAmount')}"
cell("S.11  ExternalPaid + price=25 -> 201 (regression)", s11)

def s12():
    """Update S.9 ExternalPaid: pass null pricing again -> 200; pricing stays null."""
    if not S9_EVENT_ID: return False, "S.9 didn't create event"
    g = requests.get(f"{BASE}/api/Events/{S9_EVENT_ID}", headers=HDR, timeout=30).json()
    update = {
        "eventId": S9_EVENT_ID,
        "title": g["title"] + " (updated)",
        "description": g["description"],
        "startDate": g["startDate"], "endDate": g["endDate"],
        "capacity": g["capacity"], "category": g["category"],
        "locationAddress": g.get("locationAddress") or "100 Main St",
        "locationCity": g.get("locationCity") or "New York",
        "locationState": g.get("locationState") or "NY",
        "locationZipCode": g.get("locationZipCode") or "10001",
        "locationCountry": g.get("locationCountry") or "USA",
        "isFree": False, "paymentMode": "ExternalPaid",
        "externalRegistrationUrl": g.get("externalRegistrationUrl") or "https://eventbrite.com/e/s12-8x12",
    }
    r = requests.put(f"{BASE}/api/Events/{S9_EVENT_ID}", headers=HDR, json=update, timeout=30)
    if r.status_code not in (200, 204): return False, f"PUT HTTP {r.status_code}: {r.text[:150]}"
    g2 = requests.get(f"{BASE}/api/Events/{S9_EVENT_ID}", headers=HDR, timeout=30).json()
    return g2.get("ticketPriceAmount") in (None, 0), \
           f"after-update price={g2.get('ticketPriceAmount')!r} title={g2.get('title')[:40]}"
cell("S.12  Update ExternalPaid + null pricing -> 200, price stays null (D3)", s12)

print("\n[Allowed modes endpoint — carry-forward]")

def q1():
    r = requests.get(f"{BASE}/api/Events/allowed-registration-modes?paymentMode=ExternalPaid&isFreeAttendance=false&hasDualPricing=false",
                     headers=HDR, timeout=30)
    if r.status_code != 200: return False, f"HTTP {r.status_code}: {r.text[:120]}"
    modes = r.json()
    return modes == ["External"], f"modes={modes}"
cell("Q1  GET allowed-registration-modes?paymentMode=ExternalPaid -> [External]", q1)

print()
print("=" * 78)
print(f"Phase 8X.12 smoke matrix: {PASS} PASS, {FAIL} FAIL")
print("=" * 78)
sys.exit(0 if FAIL == 0 else 1)
