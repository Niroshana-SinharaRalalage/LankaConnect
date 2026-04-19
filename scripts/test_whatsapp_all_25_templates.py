"""
Phase 7B.4 / Task T6-8: End-to-end smoke test for all 25 WhatsApp templates on staging.

Sends a test message via /api/whatsapp-admin/test-message for each template with
realistic mock parameters. Collects the API response (success / error / MessageSid),
then prints a summary table so we can see exactly which templates delivered and
which are blocked (most likely on Meta approval).

This script DOES NOT query Twilio directly — the admin endpoint either returns
the provider MessageSid (success) or an error string (fail). Delivery-status
(delivered/read) is separately confirmed via Twilio webhook callbacks, which
land in communications.whatsapp_webhook_events.

Recipient is hard-coded to +18609780124 per user authorization (2026-04-19).
"""
import json
import sys
import time
from dataclasses import dataclass
from typing import Dict, List, Optional

import urllib.request
import urllib.error

API = "https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io"
ADMIN_EMAIL = "admin@lankaconnect.com"
ADMIN_PASSWORD = "Admin@2025!"
RECIPIENT = "+18609780124"


# ────────────────────────────── Template catalog ──────────────────────────────
# (template_name, {param_name: mock_value}) — param names match WhatsAppTemplateContract
# constants and TwilioTemplateCatalog positional ordering.
TEMPLATES: List[tuple] = [
    # Phase 7A — 14 original templates
    # DB param order: user_name, event_title, event_date, event_time, event_location, registration_quantity, event_url
    ("event_registration_confirmed", {
        "user_name": "Test User", "event_title": "Staging Smoke Test Event",
        "event_date": "2026-05-01", "event_time": "18:00",
        "event_location": "Test Venue", "registration_quantity": "2",
        "event_url": "https://lankaconnect.app/events/test"}),
    # DB param order: user_name, event_title, ticket_code, event_date, event_time, event_location, event_url
    ("event_ticket_confirmation", {
        "user_name": "Test User", "event_title": "Staging Smoke Test Event",
        "ticket_code": "TEST-ABC-123", "event_date": "2026-05-01",
        "event_time": "18:00", "event_location": "Test Venue",
        "event_url": "https://lankaconnect.app/events/test"}),
    ("event_reminder", {
        "user_name": "Test User", "event_title": "Staging Smoke Test Event",
        "reminder_timeframe": "24 hours", "event_date": "2026-05-01",
        "event_location": "Test Venue"}),
    ("event_cancelled", {
        "user_name": "Test User", "event_title": "Staging Smoke Test Event",
        "cancellation_reason": "Test reason", "refund_message": "Refund in 5-7 business days"}),
    # DB param order: user_name, event_title, event_date, event_location, event_url
    ("new_event_announcement", {
        "user_name": "Test User", "event_title": "New Event",
        "event_date": "2026-05-01", "event_location": "Test Venue",
        "event_url": "https://lankaconnect.app/events/new"}),
    ("event_details_update", {
        "user_name": "Test User", "event_title": "Staging Smoke Test Event",
        "event_date": "2026-05-01", "event_time": "19:00",
        "event_location": "Updated Venue", "event_url": "https://lankaconnect.app/events/test"}),
    ("newsletter_broadcast", {
        "user_name": "Test User", "newsletter_title": "April Newsletter",
        "newsletter_preview": "This is the April LankaConnect newsletter.",
        "newsletter_url": "https://lankaconnect.app/newsletter/apr"}),
    # DB param order: user_name, event_title, cancellation_reason, cancellation_date, event_url
    ("registration_cancelled", {
        "user_name": "Test User", "event_title": "Staging Smoke Test Event",
        "cancellation_reason": "Test reason", "cancellation_date": "2026-04-19",
        "event_url": "https://lankaconnect.app/events/test"}),
    ("signup_commitment_confirmed", {
        "user_name": "Test User", "event_title": "Staging Smoke Test Event",
        "list_name": "Potluck Items", "commitment_item": "Main dish",
        "commitment_quantity": "2"}),
    ("signup_commitment_updated", {
        "user_name": "Test User", "event_title": "Staging Smoke Test Event",
        "list_name": "Potluck Items", "commitment_item": "Main dish",
        "commitment_quantity": "3"}),
    # DB param order: user_name, list_name, event_title, event_url
    ("signup_commitment_cancelled", {
        "user_name": "Test User", "list_name": "Potluck Items",
        "event_title": "Staging Smoke Test Event",
        "event_url": "https://lankaconnect.app/events/test"}),
    # DB param order: user_name, refund_amount, event_title, refund_status, reference_id
    ("refund_initiated", {
        "user_name": "Test User", "refund_amount": "$50.00",
        "event_title": "Staging Smoke Test Event",
        "refund_status": "Pending", "reference_id": "REF-123"}),
    ("refund_completed", {
        "user_name": "Test User", "event_title": "Staging Smoke Test Event",
        "refund_amount": "$50.00", "reference_id": "REF-123"}),
    ("phone_verification", {
        "verification_code": "000000"}),

    # Phase 7B.3 — 11 new templates
    ("payment_pending_reminder", {
        "user_name": "Test User", "event_title": "Staging Smoke Test Event",
        "payment_amount": "$25.00", "expiry_time": "24 hours",
        "event_url": "https://lankaconnect.app/events/test"}),
    ("event_postponed", {
        "user_name": "Test User", "event_title": "Staging Smoke Test Event",
        "postponement_reason": "Weather conditions",
        "event_url": "https://lankaconnect.app/events/test"}),
    ("donation_receipt", {
        "user_name": "Test User", "donation_amount": "$100.00",
        "event_title": "Charity Event",
        "event_url": "https://lankaconnect.app/events/test"}),
    ("event_approved", {
        "user_name": "Test User", "event_title": "Staging Smoke Test Event",
        "event_url": "https://lankaconnect.app/events/test"}),
    ("event_rejected", {
        "user_name": "Test User", "event_title": "Staging Smoke Test Event",
        "rejection_reason": "Missing required information"}),
    ("attendees_added", {
        "user_name": "Test User", "event_title": "Staging Smoke Test Event",
        "added_count": "3", "new_total_count": "15",
        "event_url": "https://lankaconnect.app/events/test"}),
    ("addon_purchase_receipt", {
        "user_name": "Test User", "addon_name": "VIP Parking",
        "quantity": "1", "total_amount": "$20.00",
        "event_title": "Staging Smoke Test Event"}),
    ("collection_receipt", {
        "user_name": "Test User", "contribution_amount": "$50.00",
        "event_title": "Staging Smoke Test Event",
        "event_url": "https://lankaconnect.app/events/test"}),
    ("sponsor_confirmation", {
        "user_name": "Test User", "sponsor_type": "Gold Sponsor",
        "sponsor_details": "$500 contribution",
        "event_title": "Staging Smoke Test Event",
        "event_url": "https://lankaconnect.app/events/test"}),
    ("form_response_confirmed", {
        "user_name": "Test User", "form_title": "Guest Info Form",
        "event_title": "Staging Smoke Test Event",
        "event_url": "https://lankaconnect.app/events/test"}),
    ("photo_album_published", {
        "user_name": "Test User", "event_title": "Staging Smoke Test Event",
        "album_name": "Event Photos",
        "album_url": "https://lankaconnect.app/albums/test"}),
]


# ─────────────────────────────── Helpers ──────────────────────────────
def http_post(url: str, body: Dict, headers: Optional[Dict] = None, timeout: int = 30):
    data = json.dumps(body).encode("utf-8")
    req = urllib.request.Request(url, data=data, method="POST")
    req.add_header("Content-Type", "application/json")
    for k, v in (headers or {}).items():
        req.add_header(k, v)
    try:
        with urllib.request.urlopen(req, timeout=timeout) as resp:
            raw = resp.read().decode("utf-8")
            return resp.status, raw
    except urllib.error.HTTPError as e:
        return e.code, e.read().decode("utf-8", errors="replace")
    except Exception as e:
        return -1, f"EXCEPTION: {type(e).__name__}: {e}"


def login() -> str:
    print("Logging in…")
    status, body = http_post(f"{API}/api/Auth/login", {
        "email": ADMIN_EMAIL,
        "password": ADMIN_PASSWORD,
        "rememberMe": True,
        "ipAddress": "smoke-test",
    })
    if status != 200:
        print(f"LOGIN FAILED: status={status}, body={body[:500]}")
        sys.exit(1)
    parsed = json.loads(body)
    token = parsed.get("accessToken") or (parsed.get("data") or {}).get("accessToken")
    if not token:
        print(f"LOGIN OK but no accessToken in response. Keys: {list(parsed.keys())}")
        sys.exit(1)
    print(f"  Got token: {token[:20]}…\n")
    return token


@dataclass
class Result:
    template: str
    status: int
    success: bool
    message_sid: Optional[str]
    error: Optional[str]
    raw_snippet: str


def send_template(token: str, template_name: str, params: Dict[str, str]) -> Result:
    status, body = http_post(
        f"{API}/api/whatsapp-admin/test-message",
        {"recipientPhone": RECIPIENT, "templateName": template_name, "parameters": params},
        headers={"Authorization": f"Bearer {token}"},
    )
    success = 200 <= status < 300
    message_sid = None
    error = None
    raw_snippet = body[:300]
    try:
        parsed = json.loads(body)
        # Common envelopes we may see
        data = parsed if isinstance(parsed, dict) else {}
        message_sid = data.get("messageId") or data.get("providerMessageId") or data.get("acsMessageId")
        if not success:
            error = data.get("title") or data.get("detail") or data.get("error") or body[:200]
    except Exception:
        if not success:
            error = body[:200]
    return Result(template_name, status, success, message_sid, error, raw_snippet)


# ─────────────────────────────── Main ──────────────────────────────
def main():
    token = login()
    print(f"Sending {len(TEMPLATES)} test templates to {RECIPIENT} …\n")
    results: List[Result] = []
    for i, (name, params) in enumerate(TEMPLATES, 1):
        print(f"  [{i:02d}/{len(TEMPLATES)}] {name:40s} …", end=" ", flush=True)
        r = send_template(token, name, params)
        results.append(r)
        if r.success:
            print(f"OK  (sid={r.message_sid or '?'})")
        else:
            print(f"FAIL status={r.status}  err={(r.error or '')[:120]}")
        time.sleep(0.5)  # gentle pacing

    # Summary
    ok = [r for r in results if r.success]
    fail = [r for r in results if not r.success]
    print("\n" + "=" * 80)
    print(f"SUMMARY: {len(ok)}/{len(results)} successful")
    print("=" * 80)
    if ok:
        print("\n[SUCCEEDED]")
        for r in ok:
            print(f"  {r.template:40s}  MessageSid={r.message_sid}")
    if fail:
        print("\n[FAILED]")
        for r in fail:
            print(f"  {r.template:40s}  status={r.status}  err={(r.error or '')[:150]}")

    # JSON dump for downstream DB cross-check
    with open("c:/tmp/whatsapp_25_smoke_results.json", "w") as f:
        json.dump([r.__dict__ for r in results], f, indent=2)
    print("\n(Per-template details written to c:/tmp/whatsapp_25_smoke_results.json)")


if __name__ == "__main__":
    main()
