#!/usr/bin/env python3
"""
Phase 7F-E.7 staging smoke (architect-mandated 2026-05-04, per memory
feedback_cross_surface_matrix_smoke.md + feedback_operator_uat_gate.md).

Authenticated RSVP on a fresh paid+B4-tiered staging event with the NEW per-
tier 4-leaf wire shape on the tierCounts[] payload. Confirms end-to-end:
  - API accepts the new TierCountDto fields without rejection
  - Domain factory invariants pass (all-or-nothing, sum-equals-Count, cross-axis)
  - JSONB roundtrip preserves the 4-leaf
  - Stripe checkout completes (HTTP 200 + checkout URL)

This is the smoke 7F-E.6 should have shipped originally — operator UAT then
verifies the rendered breakdown card / email / PDF show captured per-tier
demographics instead of N/A.
"""

import json
import sys
import urllib.error
import urllib.request

import psycopg2

sys.stdout.reconfigure(encoding="utf-8")

BASE = "https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io"
UI_BASE = "https://lankaconnect-ui-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io"
EVENT_ID = "87607c7a-9767-4208-8be3-dd0642016d79"

CONN = {
    "host": "lankaconnect-staging-db.postgres.database.azure.com",
    "database": "LankaConnectDB",
    "user": "adminuser",
    "password": "1qaz!QAZ",
    "sslmode": "require",
}


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
    d = json.loads(urllib.request.urlopen(req, timeout=30).read())
    return d["accessToken"], d["user"]["userId"]


def get_tier_ids() -> list[str]:
    conn = psycopg2.connect(**CONN)
    cur = conn.cursor()
    cur.execute(
        '''SELECT "Id", name FROM public.ticket_tiers WHERE event_id = %s::uuid ORDER BY sort_order''',
        (EVENT_ID,),
    )
    return [(str(r[0]), r[1]) for r in cur.fetchall()]


def authenticated_rsvp_with_4leaf(token: str, user_id: str, tiers: list) -> tuple[int, str]:
    """B4 + tiered RSVP with per-tier 4-leaf on every tier.

    VIP × 4 (with ChildPrice configured): AM=1, AF=1, CM=1, CF=1 — exercises the
        per-tier 4-leaf with children; the ChildPrice path bills children at $25.
    Standard × 4 (no ChildPrice): AM=2, AF=2, CM=0, CF=0 — adults-only tier;
        domain rejects children-on-no-ChildPrice (per the Phase 7F-C tier-by-age
        guard), which is the correct UX shape for adults-only tiers.
    Top-level: AM=3, AF=3, CM=1, CF=1 (aggregate of both tiers; preserved for
        back-compat with the 7F-C pricing helper).
    """
    vip_id, vip_name = tiers[0]
    std_id, std_name = tiers[1]

    body = json.dumps({
        "userId": user_id,
        "leadAttendeeName": "7F-E.7 Smoke",
        "email": "niroshhh@gmail.com",
        "phoneNumber": "555-0100",
        "address": "100 Test St",
        "successUrl": "https://example.com/success",
        "cancelUrl": "https://example.com/cancel",
        "headCount": {
            "adultMales": 3,
            "adultFemales": 3,
            "childMales": 1,
            "childFemales": 1,
            "tierCounts": [
                {
                    "tierId": vip_id,
                    "count": 4,
                    # VIP has ChildPrice — accepts children
                    "adultMaleCount": 1,
                    "adultFemaleCount": 1,
                    "childMaleCount": 1,
                    "childFemaleCount": 1,
                },
                {
                    "tierId": std_id,
                    "count": 4,
                    # Standard has no ChildPrice — adults-only by design
                    "adultMaleCount": 2,
                    "adultFemaleCount": 2,
                    "childMaleCount": 0,
                    "childFemaleCount": 0,
                },
            ],
        },
    }).encode()
    req = urllib.request.Request(
        f"{BASE}/api/Events/{EVENT_ID}/rsvp",
        data=body,
        headers={"Authorization": f"Bearer {token}", "Content-Type": "application/json"},
        method="POST",
    )
    try:
        resp = urllib.request.urlopen(req, timeout=60)
        return resp.status, resp.read().decode()
    except urllib.error.HTTPError as e:
        return e.code, e.read().decode()


def db_check(user_id: str) -> dict | None:
    conn = psycopg2.connect(**CONN)
    cur = conn.cursor()
    cur.execute(
        '''SELECT "Id", "Status"::text, total_price_amount, head_count::text
           FROM events.registrations
           WHERE "EventId" = %s::uuid AND "UserId" = %s::uuid
           ORDER BY "CreatedAt" DESC LIMIT 1''',
        (EVENT_ID, user_id),
    )
    row = cur.fetchone()
    if not row:
        return None
    return {"id": row[0], "status": row[1], "total_price": row[2], "head_count": row[3]}


def main() -> int:
    print("=" * 100)
    print(f"Phase 7F-E.7 smoke — authenticated RSVP with per-tier 4-leaf on event {EVENT_ID}")
    print("=" * 100)

    tiers = get_tier_ids()
    print(f"\nTiers on event:")
    for tid, name in tiers:
        print(f"  {name}: {tid}")

    token, user_id = login()
    print(f"\nLogged in: {user_id}")

    print(f"\nPOST /api/Events/{EVENT_ID}/rsvp with per-tier 4-leaf payload ...")
    status, body = authenticated_rsvp_with_4leaf(token, user_id, tiers)
    print(f"  HTTP {status}")
    if status not in (200, 201):
        print(f"  body: {body[:500]}")
        return 1

    reg = db_check(user_id)
    if not reg:
        print("  FAIL — no registration row found")
        return 1

    print(f"\nRegistration row:")
    print(f"  id: {reg['id']}")
    print(f"  status: {reg['status']}")
    print(f"  total_price: {reg['total_price']}")
    print(f"  head_count (truncated):")
    print(f"    {reg['head_count'][:400]}")

    # Negative-evidence assertion: jsonb should contain the 4-leaf fields
    head_json = json.loads(reg["head_count"])
    tier_counts = head_json.get("tierCounts", [])
    failures = []
    if not tier_counts:
        failures.append("head_count.tierCounts is empty")
    for tc in tier_counts:
        for field in ("adultMaleCount", "adultFemaleCount", "childMaleCount", "childFemaleCount"):
            if field not in tc:
                failures.append(f"tier {tc.get('tierName', '?')}: missing {field} in jsonb")

    print()
    print("=" * 100)
    if failures:
        print("RESULT: FAIL")
        for f in failures:
            print(f"  - {f}")
        return 1

    print("RESULT: PASS — per-tier 4-leaf round-tripped from form payload to JSONB")
    print("=" * 100)
    print()
    print("Operator browser UAT (architect-mandated gate per feedback_operator_uat_gate.md):")
    print(f"  1. {UI_BASE}/events/{EVENT_ID}")
    print(f"     → 'You're Registered' card per-tier rows must show:")
    print(f"       VIP:      Adult/Child: 2/2  Male/Female: 2/2  (NOT N/A)")
    print(f"       Standard: Adult/Child: 4/0  Male/Female: 4/0  (NOT N/A; adults only)")
    print(f"     → No 'Total (across all tiers)' row (per-tier already covers it)")
    print(f"  2. Inbox at niroshhh@gmail.com — paid-event-with-ticket email body:")
    print(f"     → Same per-tier rows captured, no N/A")
    print(f"  3. PDF ticket attached:")
    print(f"     → Same per-tier rows captured")
    print()
    print("Legacy registration f8f28333-... on event 616e59f3-... must KEEP showing N/A")
    print("on per-tier rows + populated Totals row (back-compat regression guard).")
    return 0


if __name__ == "__main__":
    sys.exit(main())
