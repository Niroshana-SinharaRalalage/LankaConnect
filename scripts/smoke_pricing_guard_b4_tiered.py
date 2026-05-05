#!/usr/bin/env python3
"""
Pricing-guard bug repro + post-fix verification (architect-approved 2026-05-04).

Authenticated RSVP smoke against the staging B4 + tiered event 616e59f3 that
exposed the latent domain pricing-validation bug. Same script runs both
pre-fix (expects HTTP 400 with the diagnostic error string) and post-fix
(expects HTTP 200 with a valid TotalPrice).

Operator-flagged process gap (2026-05-04, memory feedback_smoke_user_flows.md):
this is the smoke I should have run on Slice 7F-E.4b's submit path before
declaring it done. Unit tests + screenshot verification of the form's
rendering layer wasn't enough — the user-flow extends through to the domain
pricing guard, which 7F-E.4b doesn't touch but exercises in a new way.
"""

import json
import sys
import urllib.error
import urllib.request

import psycopg2

sys.stdout.reconfigure(encoding="utf-8")

BASE = "https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io"
EVENT_ID = "616e59f3-df84-4662-a9e3-18f285c00ac5"  # B4 + tiered + no legacy Pricing
VIP_TIER = "006a6b34-91ff-4abf-bd1f-1414319b0c33"
STD_TIER = "bfc15657-950b-42d6-86d5-f47c1bc902b2"

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
    resp = urllib.request.urlopen(req, timeout=30)
    d = json.loads(resp.read())
    return d["accessToken"], d["user"]["userId"]


def authenticated_rsvp(token: str, user_id: str) -> tuple[int, str]:
    """Submit a B4 + tiered RSVP. VIP tier × 2 + Standard × 1.

    Per 7F-E.4b merged layout, the FE aggregates per-tier 4-leaf inputs to
    registration-level adultMales/adultFemales/childMales/childFemales AND
    forwards the per-tier counts in TierCountDto.
    """
    body = json.dumps({
        "userId": user_id,
        "leadAttendeeName": "Pricing Guard Smoke",
        "email": "niroshhh@gmail.com",
        "phoneNumber": "555-0100",
        "address": "Smoke",
        "successUrl": "https://example.com/success",
        "cancelUrl": "https://example.com/cancel",
        # Phase 7E.3a head-count payload — B4 (HeadCountByAgeAndGender)
        "headCount": {
            # 4-leaf aggregated across tiers: VIP×2 (AM=1, AF=1) + STD×1 (AM=1) = AM:2, AF:1, CM:0, CF:0
            "adultMales": 2,
            "adultFemales": 1,
            "childMales": 0,
            "childFemales": 0,
            "tierCounts": [
                {"tierId": VIP_TIER, "count": 2},
                {"tierId": STD_TIER, "count": 1},
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


def find_registration_in_db(user_id: str) -> dict | None:
    conn = psycopg2.connect(**CONN)
    cur = conn.cursor()
    cur.execute(
        '''SELECT "Id", "Status"::text, total_price_amount, total_price_currency::text,
                  head_count::text, "CreatedAt"
           FROM events.registrations
           WHERE "EventId" = %s::uuid AND "UserId" = %s::uuid
           ORDER BY "CreatedAt" DESC LIMIT 1''',
        (EVENT_ID, user_id),
    )
    row = cur.fetchone()
    if not row:
        return None
    return {
        "id": row[0],
        "status": row[1],
        "total_price": f"{row[2]} {row[3]}" if row[2] is not None else None,
        "head_count": row[4],
        "created_at": str(row[5]),
    }


def main() -> int:
    print("=" * 100)
    print(f"Pricing-guard smoke — authenticated RSVP on B4+Tiered event {EVENT_ID}")
    print("=" * 100)
    token, user_id = login()
    print(f"Logged in as niroshhh@gmail.com ({user_id})")

    print("\n[1/2] POST /api/Events/{id}/rsvp ...")
    status, body = authenticated_rsvp(token, user_id)
    print(f"  HTTP {status}")
    print(f"  body: {body[:600]}")

    print("\n[2/2] DB check (events.registrations row for user×event) ...")
    reg = find_registration_in_db(user_id)
    if reg:
        print(f"  reg.id        = {reg['id']}")
        print(f"  reg.status    = {reg['status']}")
        print(f"  reg.total_price = {reg['total_price']}")
        print(f"  reg.head_count  = {(reg['head_count'] or '')[:200]}")
    else:
        print("  No registration row found.")

    print()
    print("=" * 100)
    if status in (200, 201):
        # POST-FIX: paid + tiered + no legacy Pricing must succeed.
        # Expected total: VIP×2 = 2×$50 + STD×1 = 1×$30 = $130 (until per-tier 4-leaf
        # affects pricing — for B4 + tiered, every leaf is treated as adult per current
        # domain logic, since per-tier-by-age applies only when `hasChildPricing` AND
        # the user opted into the age split which our payload didn't include).
        if reg and reg["total_price"]:
            print(f"RESULT: POST-FIX PASS — RSVP succeeded; total_price = {reg['total_price']}")
            return 0
        print("RESULT: AMBIGUOUS — HTTP success but DB row missing")
        return 1
    elif status == 400 and "pricing is not configured" in body.lower():
        print("RESULT: PRE-FIX REPRO CONFIRMED — domain guard returned the diagnostic error.")
        print("       This is the bug architect approved fixing. Apply the fix and re-run.")
        return 2  # distinct exit code so CI/wrapper can tell repro from real failure
    else:
        print(f"RESULT: UNEXPECTED — HTTP {status} but error string didn't match the known pricing-guard message.")
        return 1


if __name__ == "__main__":
    sys.exit(main())
