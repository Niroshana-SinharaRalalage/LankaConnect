"""
Phase 7B.4 bugfix: Inspect Twilio Content templates for 6 failing templates,
compare their placeholder counts + variable declarations with the DB-declared
parameter names from migration Phase7A_WhatsAppIntegration, and print the
mismatch plan.

Read-only (GET). Writes nothing to Twilio. Output feeds the reconciliation step.
"""
import base64
import json
import os
import re
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

# (template_name, content_sid, db_declared_params_in_order)
# DB params from migrations/20260402044620_Phase7A_WhatsAppIntegration.cs
TEMPLATES = [
    ("event_registration_confirmed", "HX0d8abbb124277e3d9bb2f07a536055cd",
     ["user_name", "event_title", "event_date", "event_time", "event_location", "registration_quantity", "event_url"]),
    ("event_ticket_confirmation", "HXb161bc8eb956f4637ab4f961d4d08ca4",
     ["user_name", "event_title", "ticket_code", "event_date", "event_time", "event_location", "event_url"]),
    ("new_event_announcement", "HXe8aba2568ab90a243b024b33e1bc0ec9",
     ["user_name", "event_title", "event_date", "event_location", "event_url"]),
    ("registration_cancelled", "HXf076845bb071e8faa76027d645f08ff0",
     ["user_name", "event_title", "cancellation_reason", "cancellation_date", "event_url"]),
    ("signup_commitment_cancelled", "HX583663d48d5e71afd843cd10af71f3d9",
     ["user_name", "list_name", "event_title", "event_url"]),
    ("refund_initiated", "HXb07ab748e3bf50fe659bc89dd0327375",
     ["user_name", "refund_amount", "event_title", "refund_status", "reference_id"]),
]


def twilio_get(url: str):
    auth = base64.b64encode(f"{ACCOUNT_SID}:{AUTH_TOKEN}".encode()).decode()
    req = urllib.request.Request(url, method="GET")
    req.add_header("Authorization", f"Basic {auth}")
    with urllib.request.urlopen(req, timeout=20) as resp:
        return json.loads(resp.read().decode())


def extract_placeholders(text: str) -> list:
    """Extract {{N}} placeholders from a string, return sorted list of ints."""
    if not text:
        return []
    nums = re.findall(r"\{\{(\d+)\}\}", text)
    return sorted(set(int(n) for n in nums))


def main():
    print("=" * 100)
    print("TWILIO CONTENT TEMPLATE INSPECTION — comparing Twilio vs DB param contracts")
    print("=" * 100)

    for name, sid, db_params in TEMPLATES:
        print(f"\n--- {name} ({sid}) ---")
        try:
            content = twilio_get(f"https://content.twilio.com/v1/Content/{sid}")
        except Exception as e:
            print(f"  FETCH FAILED: {e}")
            continue

        variables = content.get("variables") or {}
        types = content.get("types") or {}

        # Find the body text across all type variants
        bodies = []
        for tkey, tobj in types.items():
            body = tobj.get("body")
            if body:
                bodies.append((tkey, body))

        all_placeholders = set()
        for _, body in bodies:
            all_placeholders.update(extract_placeholders(body))
        placeholders_sorted = sorted(all_placeholders)

        print(f"  Twilio vars declared : {len(variables)}  keys={sorted(variables.keys(), key=int) if variables else '[]'}")
        print(f"  Twilio body placeholders (union across types): {placeholders_sorted}")
        print(f"  DB parameter_names (len={len(db_params)}): {db_params}")
        print(f"  DB would emit ContentVariables JSON keys: {list(range(1, len(db_params) + 1))}")

        # Show per-type body (ASCII-safe — Windows console chokes on emoji)
        for tkey, body in bodies:
            snippet = body.replace("\n", "\\n").encode("ascii", "replace").decode("ascii")
            if len(snippet) > 250:
                snippet = snippet[:250] + "..."
            print(f"  Type={tkey}: {snippet}")

        # Diagnose
        db_count = len(db_params)
        twilio_max = max(placeholders_sorted) if placeholders_sorted else 0
        twilio_var_count = len(variables)
        print(f"  --> Diagnosis: DB expects {db_count} vars, Twilio template uses up to {{{{{twilio_max}}}}}, "
              f"Twilio 'variables' dict has {twilio_var_count} entries.")
        if db_count != twilio_var_count or db_count != twilio_max:
            print("  !! MISMATCH — need to update Twilio template OR DB contract.")
        else:
            print("  OK — counts align, 21656 must be from empty/invalid value content.")


if __name__ == "__main__":
    main()
