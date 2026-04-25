"""
Phase 7D.1 Step 17 — volunteer export endpoint staging verification.

Verifies:
1. GET /events/{id}/export?format=volunteersexcel → ZIP with xlsx inside, sheet header "Volunteer Role"
2. GET /events/{id}/export?format=volunteerszip   → ZIP with CSV files, header row "Volunteer Role,Volunteers Needed,..."
3. GET /events/{id}/export?format=signuplistsexcel → existing Items headers unchanged (regression check)

Discovers a staging event with a Kind=Volunteers signup list automatically.
"""
import urllib.request
import urllib.parse
import json
import ssl
import io
import zipfile
import sys

BASE_URL = 'https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io'
ctx = ssl.create_default_context()


def api(method, path, data=None, token=None, raw=False):
    body = json.dumps(data).encode() if data else None
    headers = {'accept': '*/*' if raw else 'application/json'}
    if data:
        headers['Content-Type'] = 'application/json'
    if token:
        headers['Authorization'] = f'Bearer {token}'
    req = urllib.request.Request(f'{BASE_URL}{path}', data=body, headers=headers, method=method)
    try:
        with urllib.request.urlopen(req, context=ctx) as r:
            payload = r.read()
            if raw:
                return r.status, payload, dict(r.headers)
            return r.status, json.loads(payload) if payload else {}
    except urllib.error.HTTPError as e:
        payload = e.read()
        if raw:
            return e.code, payload, dict(e.headers) if hasattr(e, 'headers') else {}
        try:
            return e.code, json.loads(payload)
        except Exception:
            return e.code, {'raw': payload.decode(errors='replace')[:400]}


# ── Step 1: Login ─────────────────────────────────────────
print("=== Step 1: Login ===")
status, resp = api('POST', '/api/Auth/login', {
    'email': 'niroshhh@gmail.com',
    'password': '1qaz' + chr(33) + 'QAZ',
    'rememberMe': True,
    'ipAddress': 'string'
})
assert status == 200, f"Login failed: HTTP {status} — {resp}"
token = resp['accessToken']
print(f"OK login, token {token[:24]}…")

# ── Step 2: Find an event with at least one Volunteers list ──
print("\n=== Step 2: Discover events with volunteer lists ===")
status, events_resp = api('GET', '/api/events?pageSize=50&organizerOnly=true', token=token)
assert status == 200, f"List events failed: HTTP {status} — {events_resp}"
events = events_resp.get('items', events_resp) if isinstance(events_resp, dict) else events_resp
if isinstance(events, dict) and 'data' in events:
    events = events['data']
print(f"Got {len(events) if hasattr(events, '__len__') else '?'} events")

candidate_event_id = None
for ev in events:
    eid = ev.get('id') or ev.get('eventId')
    if not eid:
        continue
    s, signups = api('GET', f'/api/events/{eid}/signups?kind=Volunteers', token=token)
    if s == 200 and isinstance(signups, list) and len(signups) > 0:
        candidate_event_id = eid
        print(f"OK found event {eid} with {len(signups)} volunteer list(s): "
              f"{[l.get('category') or l.get('title') for l in signups]}")
        break

if not candidate_event_id:
    print("ERROR: No event with a volunteer list found on staging — cannot curl-test export")
    sys.exit(2)

EVENT_ID = candidate_event_id

# ── Step 3: Hit volunteersexcel endpoint ──────────────────
print("\n=== Step 3: GET /export?format=volunteersexcel ===")
status, payload, hdrs = api('GET', f'/api/events/{EVENT_ID}/export?format=volunteersexcel',
                            token=token, raw=True)
print(f"HTTP {status}, content-type={hdrs.get('Content-Type')}, {len(payload)} bytes")
assert status == 200, f"volunteersexcel failed: {payload[:400]!r}"

with zipfile.ZipFile(io.BytesIO(payload)) as outer:
    outer_names = outer.namelist()
    print(f"  Outer ZIP entries: {outer_names}")
    xlsx_names = [n for n in outer_names if n.endswith('.xlsx')]
    assert xlsx_names, "No xlsx inside volunteersexcel ZIP"
    with outer.open(xlsx_names[0]) as xlsx:
        xlsx_bytes = xlsx.read()
    with zipfile.ZipFile(io.BytesIO(xlsx_bytes)) as xl:
        sheet = xl.read('xl/sharedStrings.xml').decode('utf-8', errors='replace')
    # Check header strings present
    for expected in ['Volunteer Role', 'Volunteers Needed', 'Volunteer Name', 'Committed']:
        assert expected in sheet, f"Missing header '{expected}' in xlsx sharedStrings"
    print(f"  OK volunteersexcel has Volunteer Role / Volunteers Needed / Volunteer Name / Committed")

# ── Step 4: Hit volunteerszip endpoint ────────────────────
print("\n=== Step 4: GET /export?format=volunteerszip ===")
status, payload, hdrs = api('GET', f'/api/events/{EVENT_ID}/export?format=volunteerszip',
                            token=token, raw=True)
print(f"HTTP {status}, content-type={hdrs.get('Content-Type')}, {len(payload)} bytes")
assert status == 200, f"volunteerszip failed: {payload[:400]!r}"

with zipfile.ZipFile(io.BytesIO(payload)) as z:
    csv_names = [n for n in z.namelist() if n.endswith('.csv')]
    print(f"  CSV entries: {csv_names}")
    assert csv_names, "No CSVs inside volunteerszip"
    first = z.read(csv_names[0]).decode('utf-8-sig', errors='replace')
    header_line = first.splitlines()[0]
    print(f"  Header: {header_line}")
    for expected in ['Volunteer Role', 'Volunteers Needed', 'Volunteer Name', 'Committed']:
        assert expected in header_line, f"Missing '{expected}' in CSV header"
    print(f"  OK volunteerszip CSV has volunteer headers")

# ── Step 5: Regression check — signuplistsexcel still has Items headers ──
print("\n=== Step 5: Regression — signuplistsexcel keeps Items headers ===")
# Look for an event that has Items signup lists
reg_event_id = None
for ev in events:
    eid = ev.get('id') or ev.get('eventId')
    if not eid:
        continue
    s, signups = api('GET', f'/api/events/{eid}/signups?kind=Items', token=token)
    if s == 200 and isinstance(signups, list) and len(signups) > 0:
        reg_event_id = eid
        print(f"  Using event {eid} with {len(signups)} Items list(s) for regression")
        break

if reg_event_id:
    status, payload, hdrs = api('GET', f'/api/events/{reg_event_id}/export?format=signuplistsexcel',
                                token=token, raw=True)
    print(f"  HTTP {status}, {len(payload)} bytes")
    assert status == 200, f"signuplistsexcel failed: {payload[:400]!r}"
    with zipfile.ZipFile(io.BytesIO(payload)) as outer:
        xlsx_names = [n for n in outer.namelist() if n.endswith('.xlsx')]
        if xlsx_names:
            with outer.open(xlsx_names[0]) as xlsx:
                xlsx_bytes = xlsx.read()
            with zipfile.ZipFile(io.BytesIO(xlsx_bytes)) as xl:
                sheet = xl.read('xl/sharedStrings.xml').decode('utf-8', errors='replace')
            for expected in ['Item Description', 'Requested Quantity', 'Contact Name']:
                assert expected in sheet, f"REGRESSION: '{expected}' missing from signuplistsexcel"
            assert 'Volunteer Role' not in sheet, "REGRESSION: signuplistsexcel contains 'Volunteer Role'"
            print("  OK signuplistsexcel unchanged (Items headers present, volunteer headers absent)")
else:
    print("  SKIP — no event with Items signup lists found")

print("\n" + "=" * 60)
print("ALL VOLUNTEER EXPORT CURL TESTS PASSED")
print("=" * 60)
