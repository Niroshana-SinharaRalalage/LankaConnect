#!/usr/bin/env python3
"""
Phase 7C.2b Chunk 2b — probe the actual greeting/header tokens present in each
of the 7 registration/lifecycle templates so we can pin the per-template
greeting invariant in the migration correctly.

The first deploy of 20260423065018_Phase7C3a_DecomposeLocationInRegistrationAndLifecycleTemplates
failed at template-event-cancellation-notifications because its body does not
contain {{UserName}} — the params-class dictionary key is not a reliable proxy
for what the template author actually interpolated.
"""

import re
import psycopg2

conn_params = {
    "host": "lankaconnect-staging-db.postgres.database.azure.com",
    "database": "LankaConnectDB",
    "user": "adminuser",
    "password": "1qaz!QAZ",
    "sslmode": "require",
}

TEMPLATES = [
    "template-paid-event-registration-confirmation-with-ticket",
    "template-event-registration-cancellation",
    "template-event-cancellation-notifications",
    "template-event-approval",
    "template-event-reminder",
    "template-attendees-added-confirmation",
    "template-preliminary-registration-payment-pending",
]

# Candidate greeting tokens to scan for — anything that names a person.
CANDIDATES = [
    "{{UserName}}",
    "{{AttendeeName}}",
    "{{OrganizerName}}",
    "{{FirstName}}",
    "{{FullName}}",
    "{{RecipientName}}",
    "{{ContactName}}",
    "{{Name}}",
]

# Also check universal-ish tokens as fallback invariant.
UNIVERSAL = [
    "{{EventTitle}}",
    "{{SupportEmail}}",
    "{{Year}}",
]


def main():
    conn = psycopg2.connect(**conn_params)
    cur = conn.cursor()
    cur.execute(
        "SELECT name, html_template, length(html_template) "
        "FROM communications.email_templates "
        "WHERE name = ANY(%s) "
        "ORDER BY name;",
        (TEMPLATES,),
    )
    rows = cur.fetchall()
    cur.close()
    conn.close()

    seen = {r[0] for r in rows}
    missing = [t for t in TEMPLATES if t not in seen]
    if missing:
        print(f"MISSING TEMPLATES: {missing}\n")

    for name, body, length in rows:
        has_event_location = "{{EventLocation}}" in body
        has_location_variant = "{{Location}}" in body and "{{EventLocation}}" not in body
        print("=" * 100)
        print(f"Template: {name}")
        print(f"  length: {length}")
        print(f"  has {{{{EventLocation}}}}: {has_event_location}")
        print(f"  has {{{{Location}}}} (standalone): {has_location_variant}")
        print("  Candidate greeting tokens:")
        for tok in CANDIDATES:
            if tok in body:
                # count and show first context
                count = body.count(tok)
                idx = body.find(tok)
                ctx = body[max(0, idx - 60): idx + len(tok) + 40]
                ctx = ctx.replace("\n", " ")
                print(f"    {tok}: count={count}  context=...{ctx}...")
        print("  Universal tokens:")
        for tok in UNIVERSAL:
            print(f"    {tok}: {'YES' if tok in body else 'NO'}")


if __name__ == "__main__":
    main()
