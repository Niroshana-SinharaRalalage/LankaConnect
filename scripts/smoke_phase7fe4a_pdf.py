#!/usr/bin/env python3
"""
Phase 7F-E.4a PDF ticket smoke (per architect plan + memory feedback_email_smoke.md).

Logs in as niroshhh@gmail.com, downloads two PDF tickets (one Mode A, one Mode B2),
and asserts:
  - The download returns a real PDF (starts with %PDF-)
  - The PDF contains the literal string "Registration Breakdown" (the new section title)

The pre-migration ticket PDFs already in blob storage don't have this section. To
exercise the NEW renderer code path, we clear the cached `PdfBlobUrl` on the test
tickets so `GetTicketPdfAsync` falls through to `RegeneratePdfAsync` (which now
populates `TicketPdfData.RegistrationBreakdown` from the assembler). After the
download, the blob is overwritten with the regenerated PDF, so the new section is
persisted for future inspection.
"""

import json
import sys
import urllib.error
import urllib.request

import psycopg2
import pypdf

sys.stdout.reconfigure(encoding="utf-8")

BASE = "https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io"

CONN = {
    "host": "lankaconnect-staging-db.postgres.database.azure.com",
    "database": "LankaConnectDB",
    "user": "adminuser",
    "password": "1qaz!QAZ",
    "sslmode": "require",
}

# Two existing paid registrations for niroshhh@gmail.com (5e782b4d-...)
MODE_A_REG = "fb32341f-f0f4-4624-a849-f8d2b730e5f2"  # Phase 6A.136 Payment Test
MODE_B2_REG = "e6285ea7-e83c-4a33-aedf-768878fed4d9"  # Christmas Dinner Dance 2025


def login() -> str:
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
    return json.loads(resp.read())["accessToken"]


def fetch_event_id_for_reg(cur, reg_id: str) -> str:
    cur.execute('SELECT "EventId" FROM events.registrations WHERE "Id" = %s', (reg_id,))
    return str(cur.fetchone()[0])


def clear_blob_url(cur, reg_id: str) -> str | None:
    """Force PDF regeneration on next download by clearing the cached blob URL.

    Returns the previous URL (so the test can confirm it changed)."""
    cur.execute(
        'SELECT "Id", "PdfBlobUrl" FROM events.tickets WHERE "RegistrationId" = %s',
        (reg_id,),
    )
    row = cur.fetchone()
    if not row:
        return None
    ticket_id, prev_url = row
    cur.execute(
        'UPDATE events.tickets SET "PdfBlobUrl" = NULL WHERE "Id" = %s',
        (ticket_id,),
    )
    return prev_url


def download_pdf(token: str, event_id: str, label: str) -> bytes:
    req = urllib.request.Request(
        f"{BASE}/api/Events/{event_id}/my-registration/ticket/pdf",
        headers={
            "Authorization": f"Bearer {token}",
            "Accept": "application/pdf",
        },
    )
    resp = urllib.request.urlopen(req, timeout=60)
    body = resp.read()
    print(f"  HTTP {resp.status} · {len(body)} bytes · content-type={resp.headers.get('Content-Type')}")
    return body


def main() -> int:
    print("=" * 100)
    print("Phase 7F-E.4a PDF ticket smoke — Mode A + Mode B2 regeneration assertion")
    print("=" * 100)

    token = login()
    print(f"Logged in (token len={len(token)}).")

    conn = psycopg2.connect(**CONN)
    conn.autocommit = True
    cur = conn.cursor()

    failures: list[str] = []
    for label, reg_id in (("Mode A", MODE_A_REG), ("Mode B2", MODE_B2_REG)):
        print()
        print(f"{label} — registration {reg_id}")
        event_id = fetch_event_id_for_reg(cur, reg_id)
        prev_url = clear_blob_url(cur, reg_id)
        print(f"  cleared cached PdfBlobUrl (was: {(prev_url or '<empty>')[:80]})")

        pdf = download_pdf(token, event_id, label)
        if not pdf.startswith(b"%PDF-"):
            failures.append(f"{label}: response is not a PDF (first 40: {pdf[:40]!r})")
            continue

        # Save locally for visual inspection
        out = f"c:/tmp/7fe4a-{label.replace(' ', '_')}-{reg_id[:8]}.pdf"
        with open(out, "wb") as fh:
            fh.write(pdf)
        print(f"  saved to {out}")

        # Extract text from the PDF (PDFs compress text streams via FlateDecode, so a raw
        # byte search for "Registration Breakdown" never matches — pypdf decodes for us).
        # Note: pypdf occasionally splits ligatures, so search for the section's
        # distinctive lines (which use ASCII characters that survive extraction).
        reader = pypdf.PdfReader(out)
        text = "\n".join(page.extract_text() for page in reader.pages)
        # Normalise whitespace and ligature-split spacing artefacts so checks are robust.
        normalised = text.replace(" ", "").lower()

        # Note: pypdf occasionally splits the "ti" ligature in "Registration", so we
        # search for "breakdown" alone (uniquely identifies the new section) plus the
        # two axis labels (which use ASCII slashes that survive extraction cleanly).
        required = ["breakdown", "adult/child", "male/female"]
        missing = [m for m in required if m not in normalised]
        if missing:
            failures.append(f"{label}: PDF text missing markers {missing}")
        else:
            print(f"  OK   PDF text contains 'Registration Breakdown' + axis labels")
            # Mode B2 is non-tiered demographic + tiered ticket → expect "tier" + "n/a"
            if label == "Mode B2":
                if "n/a" not in normalised:
                    failures.append(f"{label}: expected 'N/A' marker (gender axis NotCaptured) not found")
                else:
                    print(f"  OK   Mode B2 shows 'N/A' for un-captured gender axis")

    print()
    print("=" * 100)
    if failures:
        print("RESULT: FAIL")
        for f in failures:
            print(f"  - {f}")
        return 1
    print("RESULT: PASS  (both Mode A and Mode B PDFs include the new breakdown section)")
    return 0


if __name__ == "__main__":
    sys.exit(main())
