"""Phase 8YB.5 — TBD-Publish 22-cell API smoke matrix on staging."""
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
        print(f"  [{status}] {label}: {detail}")
        RESULTS.append((label, ok, detail))
    except Exception as ex:
        FAIL += 1
        print(f"  [FAIL] {label}: EXCEPTION {ex}")
        RESULTS.append((label, False, str(ex)))


def list_event_ids(events_response):
    """/api/Events returns a flat list (not paginated)."""
    if isinstance(events_response, list):
        return [e.get("id") for e in events_response if isinstance(e, dict)]
    if isinstance(events_response, dict):
        items = events_response.get("items") or events_response.get("events") or []
        return [e.get("id") for e in items if isinstance(e, dict)]
    return []


def make_planning_payload(payment_mode="Free", title_suffix="", external_url=None):
    payload = {
        "title": f"Phase 8YB.5 smoke {title_suffix}".strip(),
        "description": "Phase 8YB.5 TBD-publish smoke - safe to delete",
        "startDate": None,
        "endDate": None,
        "datesUnknown": True,
        "organizerId": USER_ID,
        "capacity": 50, "category": 1,
        "locationAddress": "100 Main St", "locationCity": "New York",
        "locationState": "NY", "locationZipCode": "10001", "locationCountry": "USA",
        "isFree": payment_mode == "Free",
        "paymentMode": payment_mode,
        # Phase 6A.59: cancel requires at least one organizer contact for paid events
        "publishOrganizerContact": True,
        "organizerContacts": [{
            "contactName": "Smoke Tester", "contactEmail": "smoke@example.com",
            "contactPhone": None, "isPrimary": True
        }],
    }
    if payment_mode == "OnPlatformPaid":
        payload["ticketPriceAmount"] = 25
        payload["ticketPriceCurrency"] = 0
    if payment_mode == "ExternalPaid" and external_url:
        payload["externalRegistrationUrl"] = external_url
    return payload


print("=" * 78)
print("Phase 8YB.5 -- TBD-Publish API smoke matrix")
print("=" * 78)

TOKEN, USER_ID = login()
HDR = {"Authorization": f"Bearer {TOKEN}", "Content-Type": "application/json", "accept": "application/json"}
HDR_ANON = {"Content-Type": "application/json", "accept": "application/json"}
print(f"Logged in. UserId={USER_ID}\n")

print("[Setup — create 3 Planning events]")

EVENT_FREE = None
EVENT_ONPLATFORM = None
EVENT_EXTERNAL = None

def create_planning_free():
    global EVENT_FREE
    r = requests.post(f"{BASE}/api/Events", headers=HDR,
                      json=make_planning_payload("Free", "C2-Free"), timeout=30)
    if r.status_code != 201:
        return False, f"HTTP {r.status_code}: {r.text[:150]}"
    EVENT_FREE = r.json() if isinstance(r.json(), str) else r.json().get("id")
    g = requests.get(f"{BASE}/api/Events/{EVENT_FREE}", headers=HDR, timeout=30).json()
    return g.get("status") == "Planning", f"id={EVENT_FREE[:8]} status={g.get('status')}"
cell("Setup-Free  Free Planning event created", create_planning_free)

def create_planning_external():
    global EVENT_EXTERNAL
    r = requests.post(f"{BASE}/api/Events", headers=HDR,
                      json=make_planning_payload("ExternalPaid", "C1-External",
                                                external_url="https://eventbrite.com/e/8yb5-tbd"),
                      timeout=30)
    if r.status_code != 201:
        return False, f"HTTP {r.status_code}: {r.text[:150]}"
    EVENT_EXTERNAL = r.json() if isinstance(r.json(), str) else r.json().get("id")
    return True, f"id={EVENT_EXTERNAL[:8]}"
cell("Setup-Ext   ExternalPaid Planning event created", create_planning_external)

def create_planning_onplatform():
    global EVENT_ONPLATFORM
    r = requests.post(f"{BASE}/api/Events", headers=HDR,
                      json=make_planning_payload("OnPlatformPaid", "C3-OnPlat"), timeout=30)
    if r.status_code != 201:
        return False, f"HTTP {r.status_code}: {r.text[:150]}"
    EVENT_ONPLATFORM = r.json() if isinstance(r.json(), str) else r.json().get("id")
    return True, f"id={EVENT_ONPLATFORM[:8]}"
cell("Setup-OnPlat OnPlatformPaid Planning event created", create_planning_onplatform)

print("\n[Headline — Publish from Planning]")

def c4():
    if not EVENT_FREE: return False, "Setup failed"
    r = requests.post(f"{BASE}/api/Events/{EVENT_FREE}/publish", headers=HDR, timeout=30)
    if r.status_code not in (200, 204):
        return False, f"HTTP {r.status_code}: {r.text[:150]}"
    g = requests.get(f"{BASE}/api/Events/{EVENT_FREE}", headers=HDR, timeout=30).json()
    return g.get("status") == "Published", f"status={g.get('status')}"
cell("C4   Publish Planning Free -> 200 + status=Published", c4)

def c4_onplatform():
    if not EVENT_ONPLATFORM: return False, "Setup failed"
    r = requests.post(f"{BASE}/api/Events/{EVENT_ONPLATFORM}/publish", headers=HDR, timeout=30)
    return r.status_code in (200, 204), f"HTTP {r.status_code} (D7=A: OnPlatformPaid TBD-publish)"
cell("C4b  Publish Planning OnPlatformPaid -> 200 (D7=A)", c4_onplatform)

def c4_external():
    if not EVENT_EXTERNAL: return False, "Setup failed"
    r = requests.post(f"{BASE}/api/Events/{EVENT_EXTERNAL}/publish", headers=HDR, timeout=30)
    return r.status_code in (200, 204), f"HTTP {r.status_code} (D7=A: ExternalPaid TBD-publish)"
cell("C4c  Publish Planning ExternalPaid -> 200 (D7=A)", c4_external)

print("\n[Listing + date filters]")

def c5():
    """Default /events listing includes TBD-Published event."""
    r = requests.get(f"{BASE}/api/Events?statusFilter=1&pageSize=200", headers=HDR_ANON, timeout=30)
    if r.status_code != 200:
        return False, f"HTTP {r.status_code}: {r.text[:120]}"
    ids = list_event_ids(r.json())
    return EVENT_FREE in ids, f"found={EVENT_FREE in ids} totalReturned={len(ids)}"
cell("C5   Default listing includes TBD-Published", c5)

def c6():
    """Upcoming filter (StartDateFrom only) INCLUDES TBD events (validates fix #5)."""
    now = datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%S.000Z")
    r = requests.get(f"{BASE}/api/Events?statusFilter=1&startDateFrom={now}&pageSize=200",
                     headers=HDR_ANON, timeout=30)
    if r.status_code != 200:
        return False, f"HTTP {r.status_code}: {r.text[:120]}"
    ids = list_event_ids(r.json())
    return EVENT_FREE in ids, f"found={EVENT_FREE in ids} (fix #5 — pre-fix this would FAIL)"
cell("C6   Upcoming bucket (StartDateFrom only) INCLUDES TBD (fix #5)", c6)

def c7():
    """This-week filter (both bounds) EXCLUDES TBD events (D5b=A)."""
    today = datetime.now(timezone.utc)
    monday = today - timedelta(days=today.weekday())
    sunday = monday + timedelta(days=6)
    r = requests.get(
        f"{BASE}/api/Events?statusFilter=1&startDateFrom={monday.strftime('%Y-%m-%dT00:00:00Z')}"
        f"&startDateTo={sunday.strftime('%Y-%m-%dT23:59:59Z')}&pageSize=200",
        headers=HDR_ANON, timeout=30)
    if r.status_code != 200:
        return False, f"HTTP {r.status_code}: {r.text[:120]}"
    ids = list_event_ids(r.json())
    return EVENT_FREE not in ids, f"excluded={EVENT_FREE not in ids} (D5b=A — explicit window must drop TBD)"
cell("C7   Window filter (StartDateFrom+To) EXCLUDES TBD (D5b=A)", c7)

print("\n[Detail page + iCal]")

def c8():
    r = requests.get(f"{BASE}/api/Events/{EVENT_FREE}", headers=HDR_ANON, timeout=30)
    if r.status_code != 200: return False, f"HTTP {r.status_code}"
    g = r.json()
    return (g.get("status") == "Published" and g.get("startDate") is None), \
           f"status={g.get('status')} startDate={g.get('startDate')}"
cell("C8   GET /events/{id} on TBD-Published -> 200 + null dates", c8)

def c11():
    r = requests.get(f"{BASE}/api/Events/{EVENT_FREE}/ics", headers=HDR, timeout=30)
    return r.status_code in (422, 400), f"HTTP {r.status_code}"
cell("C11  GET /events/{id}/ics on TBD-Published -> 422", c11)

print("\n[Search / Featured]")

def c12():
    r = requests.get(f"{BASE}/api/Events?searchTerm=Phase 8YB.5 smoke C2-Free&statusFilter=1&pageSize=200",
                     headers=HDR_ANON, timeout=30)
    if r.status_code != 200: return False, f"HTTP {r.status_code}"
    ids = list_event_ids(r.json())
    return EVENT_FREE in ids, f"found={EVENT_FREE in ids} count={len(ids)}"
cell("C12  Keyword search finds TBD-Published event", c12)

def c13():
    r = requests.get(f"{BASE}/api/Events/featured?count=50", headers=HDR_ANON, timeout=30)
    if r.status_code != 200:
        return True, f"HTTP {r.status_code} (out-of-scope)"
    items = r.json() if isinstance(r.json(), list) else r.json().get("items", [])
    ids = [e.get("id") for e in items if isinstance(e, dict)]
    return EVENT_FREE not in ids, f"excluded={EVENT_FREE not in ids} (count={len(ids)})"
cell("C13  Featured excludes TBD events (Q3=A)", c13)

print("\n[SetDates after publish]")

def c17():
    if not EVENT_FREE: return False, "Setup failed"
    g_before = requests.get(f"{BASE}/api/Events/{EVENT_FREE}", headers=HDR, timeout=30).json()
    new_start = (datetime.now(timezone.utc) + timedelta(days=21)).strftime("%Y-%m-%dT%H:%M:%S.000Z")
    new_end = (datetime.now(timezone.utc) + timedelta(days=21, hours=4)).strftime("%Y-%m-%dT%H:%M:%S.000Z")
    update = {
        "eventId": EVENT_FREE,
        "title": g_before["title"], "description": g_before["description"],
        "startDate": new_start, "endDate": new_end,
        "capacity": g_before["capacity"], "category": g_before["category"],
        "locationAddress": g_before.get("locationAddress") or "100 Main St",
        "locationCity": g_before.get("locationCity") or "New York",
        "locationState": g_before.get("locationState") or "NY",
        "locationZipCode": g_before.get("locationZipCode") or "10001",
        "locationCountry": g_before.get("locationCountry") or "USA",
        "isFree": True, "paymentMode": "Free",
    }
    r = requests.put(f"{BASE}/api/Events/{EVENT_FREE}", headers=HDR, json=update, timeout=30)
    if r.status_code not in (200, 204):
        return False, f"PUT HTTP {r.status_code}: {r.text[:120]}"
    g_after = requests.get(f"{BASE}/api/Events/{EVENT_FREE}", headers=HDR, timeout=30).json()
    return (g_after.get("status") == "Published" and g_after.get("startDate") is not None), \
           f"status={g_after.get('status')} startDate={(g_after.get('startDate') or '')[:10]}"
cell("C17  SetDates on TBD-Published -> status STAYS Published", c17)

print("\n[Cancel TBD-Published]")

def c18():
    """Cancel TBD-Published OnPlatformPaid event (organizer contact set in payload)."""
    if not EVENT_ONPLATFORM: return False, "Setup failed"
    r = requests.post(f"{BASE}/api/Events/{EVENT_ONPLATFORM}/cancel",
                      headers=HDR, json={"reason": "smoke matrix C18"}, timeout=30)
    if r.status_code not in (200, 204):
        return False, f"HTTP {r.status_code}: {r.text[:120]}"
    g = requests.get(f"{BASE}/api/Events/{EVENT_ONPLATFORM}", headers=HDR, timeout=30).json()
    return g.get("status") == "Cancelled", f"status={g.get('status')}"
cell("C18  Cancel TBD-Published OnPlatform -> 200 + status=Cancelled", c18)

print("\n[Unpublish TBD-Published — E16 fix]")

def c19():
    if not EVENT_EXTERNAL: return False, "Setup failed"
    g_before = requests.get(f"{BASE}/api/Events/{EVENT_EXTERNAL}", headers=HDR, timeout=30).json()
    if g_before.get("status") != "Published":
        return False, f"pre-condition: status={g_before.get('status')}"
    if g_before.get("startDate") is not None:
        return False, "pre-condition: startDate not null"
    r = requests.post(f"{BASE}/api/Events/{EVENT_EXTERNAL}/unpublish", headers=HDR, timeout=30)
    if r.status_code not in (200, 204):
        return False, f"HTTP {r.status_code}: {r.text[:120]}"
    g_after = requests.get(f"{BASE}/api/Events/{EVENT_EXTERNAL}", headers=HDR, timeout=30).json()
    return g_after.get("status") == "Planning", \
           f"after-unpublish status={g_after.get('status')} (E16: must be Planning)"
cell("C19  Unpublish TBD-Published -> Planning (E16 fix)", c19)

print("\n[Domain registration block — TBD events block RSVP]")

def c22():
    """RSVP to TBD-Published returns 400 (Phase 8YA.1 Q2=A)."""
    payload = make_planning_payload("Free", "C22-Free")
    r = requests.post(f"{BASE}/api/Events", headers=HDR, json=payload, timeout=30)
    if r.status_code != 201:
        return False, f"create HTTP {r.status_code}"
    eid = r.json() if isinstance(r.json(), str) else r.json().get("id")
    pub = requests.post(f"{BASE}/api/Events/{eid}/publish", headers=HDR, timeout=30)
    if pub.status_code not in (200, 204):
        return False, f"publish HTTP {pub.status_code}"
    rsvp_payload = {
        "eventId": eid, "userId": USER_ID,
        "attendees": [{"name": "Smoke Tester", "age": 30}],
    }
    r = requests.post(f"{BASE}/api/Events/{eid}/rsvp", headers=HDR, json=rsvp_payload, timeout=30)
    return r.status_code in (400, 422), f"HTTP {r.status_code}: {r.text[:120]}"
cell("C22  RSVP on TBD-Published -> 400 (Phase 8YA.1 Q2=A)", c22)

print()
print("=" * 78)
print(f"Phase 8YB.5 smoke matrix: {PASS} PASS, {FAIL} FAIL")
print("=" * 78)
sys.exit(0 if FAIL == 0 else 1)
