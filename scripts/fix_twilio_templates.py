"""
Phase 7B.4 bugfix: Create corrected Twilio Content templates for 2 templates
where the Twilio body was misaligned with the handler's DB-declared positional
contract:

1) event_registration_confirmed — DB param order:
   [user_name, event_title, event_date, event_time, event_location,
    registration_quantity, event_url]
   Old Twilio body used {{6}}=event_url and had no quantity slot.

2) new_event_announcement — DB param order:
   [user_name, event_title, event_date, event_location, event_url]
   Old Twilio body had extra {{4}}=event_time that handler never sends,
   shifting every subsequent placeholder by one.

Twilio Content templates are immutable once created. We must create NEW
templates and update the staging env vars TwilioContentSids to point at
them. Old templates are left in Twilio (they're harmless if unreferenced).

This script: (a) creates the 2 new templates via POST /v1/Content, (b) prints
the new HX-sids so we can set env vars in the next step.
"""
import base64
import json
import os
import sys
import urllib.request

# Credentials come from the environment — NEVER hardcode Twilio secrets
# (GitHub Push Protection blocks commits with literal AccountSid/AuthToken).
# Export before running:
#   export TWILIO_ACCOUNT_SID=AC...
#   export TWILIO_AUTH_TOKEN=...
ACCOUNT_SID = os.environ.get("TWILIO_ACCOUNT_SID", "")
AUTH_TOKEN = os.environ.get("TWILIO_AUTH_TOKEN", "")
if not ACCOUNT_SID or not AUTH_TOKEN:
    sys.exit("Set TWILIO_ACCOUNT_SID and TWILIO_AUTH_TOKEN env vars before running.")


def twilio_post(url: str, payload: dict) -> dict:
    auth = base64.b64encode(f"{ACCOUNT_SID}:{AUTH_TOKEN}".encode()).decode()
    data = json.dumps(payload).encode("utf-8")
    req = urllib.request.Request(url, data=data, method="POST")
    req.add_header("Authorization", f"Basic {auth}")
    req.add_header("Content-Type", "application/json")
    try:
        with urllib.request.urlopen(req, timeout=30) as resp:
            return json.loads(resp.read().decode())
    except urllib.error.HTTPError as e:
        err_body = e.read().decode("utf-8", errors="replace")
        print(f"HTTP {e.code}: {err_body}")
        raise


# Template 1: event_registration_confirmed (7 vars matching handler)
# Handler order: user_name, event_title, event_date, event_time, event_location,
#                registration_quantity, event_url
registration_confirmed = {
    "friendly_name": "event_registration_confirmed_v2",
    "language": "en",
    "variables": {
        "1": "user_name", "2": "event_title", "3": "event_date",
        "4": "event_time", "5": "event_location",
        "6": "registration_quantity", "7": "event_url"
    },
    "types": {
        "twilio/text": {
            "body": (
                "Hi {{1}}! You're registered for *{{2}}*.\n\n"
                "Date: {{3}} at {{4}}\n"
                "Location: {{5}}\n"
                "Tickets: {{6}}\n\n"
                "View details: {{7}}\n\n"
                "See you there!\n"
                "LankaEvents"
            )
        }
    }
}

# Template 2: new_event_announcement (5 vars matching handler)
# Handler order: user_name, event_title, event_date, event_location, event_url
new_event_announcement = {
    "friendly_name": "new_event_announcement_v2",
    "language": "en",
    "variables": {
        "1": "user_name", "2": "event_title", "3": "event_date",
        "4": "event_location", "5": "event_url"
    },
    "types": {
        "twilio/text": {
            "body": (
                "Hi {{1}}! A new event has been published on LankaEvents that "
                "you might be interested in.\n\n"
                "*{{2}}*\n\n"
                "Date: {{3}}\n"
                "Location: {{4}}\n\n"
                "Don't miss out! Spots may be limited.\n\n"
                "Register now: {{5}}\n\n"
                "LankaEvents"
            )
        }
    }
}


def create(label: str, payload: dict) -> str:
    print(f"\n--- Creating {label} ---")
    result = twilio_post("https://content.twilio.com/v1/Content", payload)
    sid = result.get("sid")
    print(f"  Created SID: {sid}")
    print(f"  friendly_name: {result.get('friendly_name')}")
    print(f"  variables: {list((result.get('variables') or {}).keys())}")
    return sid


def submit_for_approval(sid: str, name: str, category: str) -> None:
    """Submit the new Content template to Meta for WhatsApp approval."""
    print(f"  Submitting {sid} for Meta approval as '{name}' (category={category})")
    url = f"https://content.twilio.com/v1/Content/{sid}/ApprovalRequests/whatsapp"
    result = twilio_post(url, {"name": name, "category": category})
    print(f"  Approval status: {result.get('status')} / {result.get('rejection_reason', 'n/a')}")


def main():
    # Create-only; Meta approval submission is intentionally skipped — it's
    # part of T-EXT-5 (user's plate) and staging sends work without approval
    # for sandbox/test recipients.
    reg_sid = create("event_registration_confirmed_v2", registration_confirmed)
    new_sid = create("new_event_announcement_v2", new_event_announcement)

    print("\n=== NEW ContentSid values ===")
    print(f"event_registration_confirmed  = {reg_sid}")
    print(f"new_event_announcement        = {new_sid}")
    print("\nUpdate staging env via:")
    print(f"  az containerapp update -g lankaconnect-staging -n lankaconnect-api-staging \\")
    print(f'      --set-env-vars "WhatsAppSettings__TwilioContentSids__event_registration_confirmed={reg_sid}" \\')
    print(f'                     "WhatsAppSettings__TwilioContentSids__new_event_announcement={new_sid}"')


if __name__ == "__main__":
    main()
