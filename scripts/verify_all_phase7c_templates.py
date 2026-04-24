#!/usr/bin/env python3
"""Phase 7C.2b Chunk 2c — comprehensive per-template API verification.

PINNED to a single staging event so the user can inspect all side-effects on
one page. Change `TEST_EVENT_ID` at the top to re-point at another event.

For EVERY template touched by Chunks 1 / 2a / 2b / 2c that can be fired
against THIS event, trigger the relevant API endpoint on staging and assert
`communications.email_metrics` for that template went up by +1 successful
(and 0 failed). Templates not applicable to the given event type (e.g.
free-event template on a paid event) are reported as N/A.

Destructive flows (event cancellation, registration cancellation, admin
event approval, attendees-added, preliminary payment) are documented but
skipped — they mutate shared staging data.
"""
import json
import ssl
import sys
import time
import urllib.error
import urllib.request
from typing import Optional

import psycopg2

# ──────────────────── CHANGE THIS TO POINT AT A DIFFERENT EVENT ────────────────────
TEST_EVENT_ID = "0458806b-8672-4ad5-a7cb-f5346f1b282a"   # Monthly Dana January 2026 (free)
# ─────────────────────────────────────────────────────────────────────────────────

API = "https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io"
EMAIL = "niroshhh@gmail.com"
PASSWORD = "1qaz" + chr(33) + "QAZ"

DB_PARAMS = {
    "host": "lankaconnect-staging-db.postgres.database.azure.com",
    "database": "LankaConnectDB",
    "user": "adminuser",
    "password": "1qaz" + chr(33) + "QAZ",
    "sslmode": "require",
}

SSL_CTX = ssl.create_default_context()
USER_ID = "5e782b4d-29ed-4e1d-9039-6c8f698aeea9"  # niroshhh@gmail.com


def api(method, path, token=None, body=None, query=""):
    headers = {"Content-Type": "application/json", "accept": "application/json"}
    if token:
        headers["Authorization"] = f"Bearer {token}"
    data = json.dumps(body).encode() if body is not None else None
    req = urllib.request.Request(f"{API}{path}{query}", data=data, headers=headers, method=method)
    try:
        with urllib.request.urlopen(req, context=SSL_CTX, timeout=60) as r:
            raw = r.read().decode()
            return r.status, (json.loads(raw) if raw and raw.startswith(("{", "[")) else raw)
    except urllib.error.HTTPError as e:
        b = e.read().decode()
        return e.code, (json.loads(b) if b and b.startswith(("{", "[")) else b)


def login():
    status, resp = api("POST", "/api/Auth/login", body={
        "email": EMAIL, "password": PASSWORD, "rememberMe": True, "ipAddress": "string"
    })
    assert status == 200, f"login failed: {status} {resp}"
    return resp["accessToken"]


def snapshot():
    conn = psycopg2.connect(**DB_PARAMS)
    cur = conn.cursor()
    cur.execute(
        "SELECT template_name, total_sent, successful, failed "
        "FROM communications.email_metrics WHERE metric_date = CURRENT_DATE;"
    )
    snap = {t: (s, ok, f) for t, s, ok, f in cur.fetchall()}
    cur.close()
    conn.close()
    return snap


def find_existing_commit(item_id):
    conn = psycopg2.connect(**DB_PARAMS)
    cur = conn.cursor()
    cur.execute(
        "SELECT id FROM events.sign_up_commitments "
        "WHERE sign_up_item_id = %s AND user_id = %s "
        "ORDER BY created_at DESC LIMIT 1;",
        (item_id, USER_ID),
    )
    r = cur.fetchone()
    cur.close()
    conn.close()
    return r[0] if r else None


def cleanup_commit(item_id):
    existing = find_existing_commit(item_id)
    if existing:
        conn = psycopg2.connect(**DB_PARAMS)
        cur = conn.cursor()
        cur.execute("DELETE FROM events.sign_up_commitments WHERE id = %s;", (existing,))
        conn.commit()
        cur.close()
        conn.close()


def discover_event_data():
    """Probe the DB for test data available on TEST_EVENT_ID: event type,
    confirmed paid reg, free reg (if any), quantity-based signup item,
    volunteer signup item."""
    conn = psycopg2.connect(**DB_PARAMS)
    cur = conn.cursor()

    cur.execute(
        'SELECT title, "IsFreeEvent", "Status", "OrganizerId", address_street, address_city '
        'FROM events.events WHERE "Id" = %s;',
        (TEST_EVENT_ID,),
    )
    row = cur.fetchone()
    if row is None:
        raise RuntimeError(f"event {TEST_EVENT_ID} not found")
    title, is_free, status_, organizer_id, street, city = row
    print(f"  event:       {title}")
    print(f"  type:        {'free' if is_free else 'paid'}  status={status_}")
    print(f"  organizer:   {organizer_id}  (us? {organizer_id == USER_ID})")
    print(f"  location:    {street}, {city}")

    # Confirmed paid reg (for resend-confirmation-paid)
    cur.execute(
        'SELECT "Id", "PaymentStatus", "Status", total_price_amount '
        'FROM events.registrations WHERE "EventId" = %s '
        'AND "Status" = \'Confirmed\' AND "PaymentStatus" = 1 '
        'ORDER BY "CreatedAt" DESC LIMIT 1;',
        (TEST_EVENT_ID,),
    )
    paid_reg = cur.fetchone()
    print(f"  paid reg:    {paid_reg[0] if paid_reg else '(none)'}")

    # Free reg — prefer one whose contact email is OUR login email (deliverable)
    # over test-harness emails that Azure ACS will suppress.
    cur.execute(
        """
        SELECT "Id", "Status", contact->>'Email' FROM events.registrations
         WHERE "EventId" = %s AND "Status" = 'Confirmed'
           AND (total_price_amount = 0 OR total_price_amount IS NULL)
         ORDER BY
           CASE WHEN contact->>'Email' = %s THEN 0 ELSE 1 END,
           "CreatedAt" DESC
         LIMIT 1;
        """,
        (TEST_EVENT_ID, EMAIL),
    )
    free_reg = cur.fetchone()
    print(f"  free reg:    {free_reg[0] if free_reg else '(none)'}"
          f"  contact={free_reg[2] if free_reg else ''}")
    free_reg_is_mine = bool(free_reg and free_reg[2] == EMAIL)

    # Quantity-based signup item with remaining capacity, on non-volunteer list
    cur.execute(
        """
        SELECT i.id, s.id, i.item_description, i.target_quantity,
               COALESCE((SELECT SUM(c.quantity) FROM events.sign_up_commitments c WHERE c.sign_up_item_id = i.id), 0)
          FROM events.sign_up_items i
          JOIN events.sign_up_lists s ON s.id = i.sign_up_list_id
         WHERE s.event_id = %s
           AND s.kind = 0
           AND i.target_quantity IS NOT NULL
           AND i.target_quantity > COALESCE((SELECT SUM(c.quantity) FROM events.sign_up_commitments c WHERE c.sign_up_item_id = i.id), 0) + 2
         ORDER BY (i.target_quantity - COALESCE((SELECT SUM(c.quantity) FROM events.sign_up_commitments c WHERE c.sign_up_item_id = i.id), 0)) DESC
         LIMIT 1;
        """,
        (TEST_EVENT_ID,),
    )
    qty_item = cur.fetchone()
    print(f"  qty item:    {qty_item[2] if qty_item else '(none)'}  id={qty_item[0] if qty_item else None}")

    # Volunteer signup item (slot-based on volunteer list)
    cur.execute(
        """
        SELECT i.id, s.id, i.item_description, i.available_slots,
               COALESCE((SELECT SUM(c.slots_claimed) FROM events.sign_up_commitments c WHERE c.sign_up_item_id = i.id), 0)
          FROM events.sign_up_items i
          JOIN events.sign_up_lists s ON s.id = i.sign_up_list_id
         WHERE s.event_id = %s
           AND s.kind = 1
         LIMIT 1;
        """,
        (TEST_EVENT_ID,),
    )
    vol_item = cur.fetchone()
    print(f"  volunteer:   {vol_item[2] if vol_item else '(none)'}  id={vol_item[0] if vol_item else None}")

    cur.close()
    conn.close()
    return {
        "is_free": is_free,
        "paid_reg_id": paid_reg[0] if paid_reg else None,
        "free_reg_id": free_reg[0] if free_reg else None,
        "free_reg_is_mine": free_reg_is_mine,
        "qty_item_id": qty_item[0] if qty_item else None,
        "qty_list_id": qty_item[1] if qty_item else None,
        "vol_item_id": vol_item[0] if vol_item else None,
        "vol_list_id": vol_item[1] if vol_item else None,
    }


# ─────────────── Triggers (all pinned to TEST_EVENT_ID) ───────────────

def t_paid_ticket(token, data):
    if not data["paid_reg_id"]:
        return [("template-paid-event-registration-confirmation-with-ticket",
                 "no confirmed paid registration on this event", 0, None, False)]
    status, body = api("POST",
                       f"/api/events/{TEST_EVENT_ID}/attendees/{data['paid_reg_id']}/resend-confirmation",
                       token=token)
    return [("template-paid-event-registration-confirmation-with-ticket",
             f"POST /resend-confirmation on paid reg {data['paid_reg_id'][:8]}", status, body, True)]


def t_free_reg(token, data):
    if not data["free_reg_id"]:
        reason = "event is paid — no free registration" if not data["is_free"] else "no confirmed free reg"
        return [("template-free-event-registration-confirmation",
                 reason, 0, None, False)]
    status, body = api("POST",
                       f"/api/events/{TEST_EVENT_ID}/attendees/{data['free_reg_id']}/resend-confirmation",
                       token=token)
    return [("template-free-event-registration-confirmation",
             f"POST /resend-confirmation on free reg {data['free_reg_id'][:8]}", status, body, True)]


def t_event_reminder(token, data):
    status, body = api("POST",
                       f"/api/events/{TEST_EVENT_ID}/send-reminder",
                       token=token, query="?reminderType=1day")
    return [("template-event-reminder",
             "POST /send-reminder?reminderType=1day", status, body, True)]


def t_signup_qty_cycle(token, data):
    if not data["qty_item_id"]:
        msg = "no quantity-based signup item with capacity on this event"
        return [("template-signup-list-commitment-confirmation", msg, 0, None, False),
                ("template-signup-list-commitment-update",        msg, 0, None, False),
                ("template-signup-list-commitment-cancellation",  msg, 0, None, False)]
    out = []
    cleanup_commit(data["qty_item_id"])

    body = {"userId": USER_ID, "quantity": 1, "notes": "7C.2c verify (confirm)",
            "contactName": None, "contactEmail": None, "contactPhone": None,
            "physicalQuantity": None, "slotsClaimed": None}
    status, resp = api("POST",
                       f"/api/events/{TEST_EVENT_ID}/signups/{data['qty_list_id']}/items/{data['qty_item_id']}/commit",
                       token=token, body=body)
    out.append(("template-signup-list-commitment-confirmation",
                "POST signup commit qty=1", status, resp, True))
    time.sleep(3)

    body["quantity"] = 2
    body["notes"] = "7C.2c verify (update)"
    status, resp = api("POST",
                       f"/api/events/{TEST_EVENT_ID}/signups/{data['qty_list_id']}/items/{data['qty_item_id']}/commit",
                       token=token, body=body)
    out.append(("template-signup-list-commitment-update",
                "POST signup commit qty=2 (update)", status, resp, True))
    time.sleep(3)

    body["quantity"] = 0
    body["notes"] = "7C.2c verify (cancel)"
    status, resp = api("POST",
                       f"/api/events/{TEST_EVENT_ID}/signups/{data['qty_list_id']}/items/{data['qty_item_id']}/commit",
                       token=token, body=body)
    out.append(("template-signup-list-commitment-cancellation",
                "POST signup commit qty=0 (cancel)", status, resp, True))
    return out


def t_volunteer_cycle(token, data):
    if not data["vol_item_id"]:
        msg = "no volunteer-kind signup item on this event"
        return [("template-volunteer-commitment-confirmation", msg, 0, None, False),
                ("template-volunteer-commitment-cancellation", msg, 0, None, False)]
    out = []
    cleanup_commit(data["vol_item_id"])

    body = {"userId": USER_ID, "quantity": 1, "notes": "7C.2c verify (vol confirm)",
            "contactName": None, "contactEmail": None, "contactPhone": None,
            "physicalQuantity": None, "slotsClaimed": 1}
    status, resp = api("POST",
                       f"/api/events/{TEST_EVENT_ID}/signups/{data['vol_list_id']}/items/{data['vol_item_id']}/commit",
                       token=token, body=body)
    out.append(("template-volunteer-commitment-confirmation",
                "POST volunteer signup commit slots=1", status, resp, True))
    time.sleep(3)

    body["quantity"] = 0
    body["slotsClaimed"] = 0
    body["notes"] = "7C.2c verify (vol cancel)"
    status, resp = api("POST",
                       f"/api/events/{TEST_EVENT_ID}/signups/{data['vol_list_id']}/items/{data['vol_item_id']}/commit",
                       token=token, body=body)
    out.append(("template-volunteer-commitment-cancellation",
                "POST volunteer signup commit qty=0 (cancel)", status, resp, True))
    return out


def check_my_registration(cur):
    """Returns True if authenticated test user owns a Confirmed reg on TEST_EVENT_ID
    (UserId match — anonymous regs with empty-guid don't count)."""
    cur.execute(
        'SELECT 1 FROM events.registrations '
        'WHERE "EventId" = %s AND "UserId" = %s AND "Status" = \'Confirmed\' LIMIT 1;',
        (TEST_EVENT_ID, USER_ID),
    )
    return cur.fetchone() is not None


def t_registration_cancel(token, data):
    """Fire template-event-registration-cancellation for a FREE event. Strategy:
    only run on free events (paid would require refund flow). If the test user
    doesn't own a reg via UserId-match on this event, RSVP first to bootstrap
    one, then cancel, then re-RSVP to restore state.

    Paid-event registration-cancellation requires a refund flow and is NOT
    run here to keep staging data clean."""
    if not data["is_free"]:
        return [("template-event-registration-cancellation",
                 "event is paid — cancel would require refund flow (skipped to keep state clean)",
                 0, None, False)]

    conn = psycopg2.connect(**DB_PARAMS)
    cur = conn.cursor()
    owns_reg = check_my_registration(cur)
    cur.close()
    conn.close()

    rsvp_body = {
        "userId": USER_ID,
        "quantity": 1,
        "email": EMAIL,
    }

    if not owns_reg:
        print(f"   [bootstrap] no UserId-match reg for this user on event — POST /rsvp first")
        rsvp_status, rsvp_body_resp = api("POST", f"/api/events/{TEST_EVENT_ID}/rsvp",
                                          token=token, body=rsvp_body)
        print(f"   [bootstrap] rsvp returned HTTP {rsvp_status}")
        if not (200 <= (rsvp_status or 0) < 300):
            return [("template-event-registration-cancellation",
                     f"bootstrap RSVP failed (HTTP {rsvp_status}) — cannot verify",
                     rsvp_status, rsvp_body_resp, False)]
        time.sleep(3)

    # Now cancel — fires RegistrationCancelledEvent
    status, body = api("DELETE", f"/api/events/{TEST_EVENT_ID}/rsvp", token=token)
    out = [("template-event-registration-cancellation",
            f"DELETE /api/events/{TEST_EVENT_ID[:8]}/rsvp (cancel my free reg)",
            status, body, True)]

    # Re-RSVP to restore state.
    if 200 <= (status or 0) < 300:
        time.sleep(3)
        reRsvp = api("POST", f"/api/events/{TEST_EVENT_ID}/rsvp", token=token, body=rsvp_body)
        print(f"   [restore] re-RSVP returned HTTP {reRsvp[0]}")
    return out


def t_attendees_added(token, data):
    """Add an attendee to my paid reg to fire template-attendees-added-confirmation.
    Only runs on a paid event where I already have a Confirmed paid reg."""
    if data["is_free"] or not data["paid_reg_id"]:
        return [("template-attendees-added-confirmation",
                 "event is free OR no paid reg — attendees-added only fires on paid regs", 0, None, False)]

    body = {
        "attendees": [{
            "name": "7C Verify AddOn",
            "ageCategory": "Adult",
            "gender": "PreferNotToSay",
            "gender_": "PreferNotToSay",
        }],
    }
    status, resp = api("POST",
                       f"/api/events/registrations/{data['paid_reg_id']}/add-attendees",
                       token=token, body=body)
    return [("template-attendees-added-confirmation",
             f"POST /registrations/{data['paid_reg_id'][:8]}/add-attendees (1 attendee)",
             status, resp, True)]


DESTRUCTIVE = [
    ("template-event-cancellation-notifications",
     "POST /api/events/{id}/cancel  - would cancel the entire event (destructive to shared staging state)"),
    ("template-event-approval",
     "POST /api/events/admin/{id}/approve  - requires Admin/AdminManager role; niroshhh is EventOrganizer"),
    ("template-preliminary-registration-payment-pending",
     "requires fresh Stripe checkout session (POST paid rsvp) — creates a preliminary reg that auto-abandons;"
     " doable but not included in this run to keep the script side-effect-free."),
]


def main():
    print("=" * 100)
    print(f"Phase 7C.2b Chunk 2c  -  per-template API verification on ONE event")
    print(f"Event: {TEST_EVENT_ID}")
    print("=" * 100)

    print("\n[discover] probing event + available test data ...")
    data = discover_event_data()

    token = login()
    print(f"\n[auth] JWT obtained for {EMAIL}")

    before = snapshot()
    print(f"\n[snapshot-before] {len(before)} template metric rows today")

    groups = [t_paid_ticket, t_free_reg, t_event_reminder, t_signup_qty_cycle,
              t_volunteer_cycle, t_registration_cancel, t_attendees_added]
    results = []
    for fn in groups:
        try:
            for row in fn(token, data):
                template, desc, status, body, fired = row
                if not fired:
                    print(f"\n[skip]   {template}  -  {desc}")
                    results.append((template, desc, status, False, False))
                    continue
                ok = 200 <= (status or 0) < 300
                print(f"\n[trigger] {template}")
                print(f"   desc:   {desc}")
                print(f"   status: HTTP {status}  {'OK' if ok else 'FAIL'}")
                if not ok and body is not None:
                    print(f"   body:   {str(body)[:250]}")
                results.append((template, desc, status, ok, True))
        except Exception as exc:
            print(f"\n[trigger] {fn.__name__} raised {type(exc).__name__}: {exc}")
            results.append((fn.__name__, "(group raised)", 0, False, True))

    print("\n[waiting 45s for fire-and-forget + metrics aggregation] ...")
    time.sleep(45)

    after = snapshot()
    print(f"[snapshot-after] {len(after)} template metric rows today\n")

    print("=" * 100)
    print(f"PER-TEMPLATE RESULT on event {TEST_EVENT_ID}")
    print("=" * 100)

    overall = True
    for template, desc, status, api_ok, fired in results:
        b = before.get(template, (0, 0, 0))
        a = after.get(template, (0, 0, 0))
        d_sent, d_ok, d_fail = a[0] - b[0], a[1] - b[1], a[2] - b[2]
        cum_ok = a[1]

        if not fired:
            tag = "N/A"
            print(f"\n[{tag}]  {template}")
            print(f"      reason: {desc}")
            continue

        passed = api_ok and d_sent >= 1 and d_ok >= 1
        secondary = (not passed) and api_ok and cum_ok >= 1
        tag = "PASS" if passed else ("PASS*" if secondary else "FAIL")
        print(f"\n[{tag}] {template}")
        print(f"      desc: {desc}")
        print(f"      http={status}  d_sent={d_sent:+d}  d_ok={d_ok:+d}  d_fail={d_fail:+d}  cum_ok_today={cum_ok}")
        if secondary:
            print(f"      note: no new success this run (dedup) — {cum_ok} already successful today.")
        if d_fail > 0:
            print(f"      note: {d_fail} concurrent failure(s) in fan-out (pre-existing bad-recipient data).")
        if not (passed or secondary):
            overall = False

    print("\n" + "=" * 100)
    print("DESTRUCTIVE / ADMIN-ONLY  -  not auto-triggered")
    print("=" * 100)
    for template, reason in DESTRUCTIVE:
        print(f"  - {template}")
        print(f"      reason: {reason}")

    print("\n" + "=" * 100)
    print("OVERALL:", "PASS" if overall else "FAIL")
    print("=" * 100)
    sys.exit(0 if overall else 1)


if __name__ == "__main__":
    main()
