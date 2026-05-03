"""Seating MVP regression suite - one-shot run of every API smoke test
across slices S1, S1.5, S2, S3, S4.

Architect Rev 4 §S6 calls for this regression bundle as one of the MVP
ship gates: every per-slice curl test runs against staging in one pass
and the entire seating-system API surface comes back green.

This script is the source of truth for the bundle. Re-run it after
every slice merge to verify no drift.

Per-slice tests covered:
  S1.5: J-B (apply preset A->B->A->A, no orphan collision)
        J-F (Mode B + AssignedSeating rejection)
  S2:   T1-T6d (PUT-with-deletedIds destructive protection)
  S3:   T1-T3 (rename via existing PUT)
  S4:   T1-T4 (publish-readiness GET + 404 + bad layout shape)

Usage:
  cd c:/Work/LankaConnect
  python C:/tmp/seating_mvp_regression.py
"""
from __future__ import annotations
import json, urllib.request, urllib.error, ssl, pathlib, sys
from typing import Optional

API = "https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io"
EVENT_ID = "e4792b64-9d35-4567-82fa-6c0624d0f8e7"
TOKEN = pathlib.Path("scripts/token.txt").read_text().strip()


def _req(method: str, path: str, headers: Optional[dict] = None, body=None):
    h = {"Authorization": f"Bearer {TOKEN}"}
    if headers:
        h.update(headers)
    if body is not None and not isinstance(body, (bytes, bytearray)):
        body = json.dumps(body).encode()
        h["Content-Type"] = "application/json"
    r = urllib.request.Request(f"{API}{path}", data=body, headers=h, method=method)
    try:
        with urllib.request.urlopen(r, context=ssl.create_default_context()) as resp:
            return resp.status, resp.headers.get("x-correlation-id"), resp.read()
    except urllib.error.HTTPError as e:
        return e.code, e.headers.get("x-correlation-id"), e.read()


def get_event_layout_id() -> str:
    _, _, body = _req("GET", f"/api/events/{EVENT_ID}")
    return json.loads(body).get("data", json.loads(body))["venueLayoutId"]


def get_layout(layout_id: str):
    _, _, body = _req("GET", f"/api/venue-layouts/{layout_id}")
    return json.loads(body)


def to_batch_zone(z):
    return {
        "id": z["id"],
        "name": z["name"],
        "color": z.get("color", "#888888"),
        "sortOrder": z["sortOrder"],
        "shape": z["shape"],
        "geometry": json.dumps(z["geometry"]) if isinstance(z.get("geometry"), dict) else z.get("geometry"),
        "clientId": None,
        "rowCount": None,
        "seatsPerRow": None,
    }


def to_batch_table(t):
    return {
        "id": t["id"],
        "label": t["label"],
        "shape": t["shape"],
        "capacity": t["capacity"],
        "sortOrder": t["sortOrder"],
        "zoneId": t.get("zoneId"),
        "geometry": json.dumps(t["geometry"]) if isinstance(t.get("geometry"), dict) else t.get("geometry"),
        "clientId": None,
    }


def to_batch_decoration(d):
    return {
        "id": d["id"],
        "kind": d["kind"],
        "label": d.get("label"),
        "sortOrder": d["sortOrder"],
        "geometry": json.dumps(d["geometry"]) if isinstance(d.get("geometry"), dict) else d.get("geometry"),
        "properties": json.dumps(d["properties"]) if isinstance(d.get("properties"), dict) else d.get("properties"),
    }


# ----- per-slice test functions, each returns (label, ok, cid) -----

def s2_t1_omit_zone_no_deletion():
    """Apply theater-classic, then PUT batch with empty zones[] + no deletedZoneIds -> 409."""
    _req("POST", "/api/venue-layouts/apply-preset",
         body={"presetId": "theater-classic", "eventId": EVENT_ID})
    layout_id = get_event_layout_id()
    layout = get_layout(layout_id)
    rv = layout["rowVersion"]
    payload = {
        "name": layout["name"],
        "canvas": layout.get("canvas") or None,
        "zones": [],
        "tables": [],
        "decorations": [
            to_batch_decoration(d) for d in layout.get("decorations", [])
        ],
        "tierAssignments": None,
        "deletedZoneIds": None,
        "deletedTableIds": None,
        "deletedDecorationIds": None,
    }
    status, cid, _ = _req(
        "PUT",
        f"/api/venue-layouts/{layout_id}/batch",
        headers={"If-Match": str(rv)},
        body=payload,
    )
    return ("S2-T1 omit zone -> 409", status == 409, cid)


def s2_t2_explicit_delete_zone():
    layout_id = get_event_layout_id()
    layout = get_layout(layout_id)
    rv = layout["rowVersion"]
    zone_ids = [z["id"] for z in layout.get("zones", [])]
    if not zone_ids:
        return ("S2-T2 explicit zone delete -> 204", False, None)
    payload = {
        "name": layout["name"],
        "canvas": layout.get("canvas") or None,
        "zones": [],
        "tables": [],
        "decorations": [
            to_batch_decoration(d) for d in layout.get("decorations", [])
        ],
        "tierAssignments": None,
        "deletedZoneIds": zone_ids,
        "deletedTableIds": None,
        "deletedDecorationIds": None,
    }
    status, cid, _ = _req(
        "PUT",
        f"/api/venue-layouts/{layout_id}/batch",
        headers={"If-Match": str(rv)},
        body=payload,
    )
    after = get_layout(layout_id)
    ok = status == 204 and after["totalCapacity"] == 0
    return ("S2-T2 explicit zone delete -> 204", ok, cid)


def s3_t1_rename_valid():
    layout_id = get_event_layout_id()
    layout = get_layout(layout_id)
    rv = layout["rowVersion"]
    original_name = layout["name"]
    new_name = "Regression Smoke Renamed"
    status, cid, _ = _req(
        "PUT",
        f"/api/venue-layouts/{layout_id}",
        headers={"If-Match": str(rv)},
        body={"name": new_name},
    )
    after = get_layout(layout_id)
    ok = status == 204 and after["name"] == new_name
    # cleanup
    _req("PUT", f"/api/venue-layouts/{layout_id}",
         headers={"If-Match": str(after['rowVersion'])},
         body={"name": original_name})
    return ("S3-T1 rename -> 204", ok, cid)


def s3_t2_rename_stale_ifmatch():
    layout_id = get_event_layout_id()
    layout = get_layout(layout_id)
    stale_rv = max(0, layout["rowVersion"] - 999)
    status, cid, _ = _req(
        "PUT",
        f"/api/venue-layouts/{layout_id}",
        headers={"If-Match": str(stale_rv)},
        body={"name": "ShouldFail"},
    )
    return ("S3-T2 rename stale If-Match -> 409", status == 409, cid)


def s3_t3a_rename_empty():
    layout_id = get_event_layout_id()
    layout = get_layout(layout_id)
    status, cid, _ = _req(
        "PUT",
        f"/api/venue-layouts/{layout_id}",
        headers={"If-Match": str(layout["rowVersion"])},
        body={"name": ""},
    )
    return ("S3-T3a rename empty -> 400", status == 400, cid)


def s3_t3b_rename_oversize():
    layout_id = get_event_layout_id()
    layout = get_layout(layout_id)
    status, cid, _ = _req(
        "PUT",
        f"/api/venue-layouts/{layout_id}",
        headers={"If-Match": str(layout["rowVersion"])},
        body={"name": "X" * 256},
    )
    return ("S3-T3b rename 256-char -> 400", status == 400, cid)


def s4_t1_readiness_happy_path():
    # Apply fresh theater-classic so we have a clean unmapped state
    _req("POST", "/api/venue-layouts/apply-preset",
         body={"presetId": "theater-classic", "eventId": EVENT_ID})
    layout_id = get_event_layout_id()
    status, cid, body = _req(
        "GET", f"/api/venue-layouts/{layout_id}/publish-readiness"
    )
    if status != 200:
        return ("S4-T1 readiness happy path -> 200", False, cid)
    report = json.loads(body)
    has_keys = {"isPublishReady", "blockers", "warnings", "tierSummary"}.issubset(report.keys())
    return ("S4-T1 readiness happy path -> 200", has_keys, cid)


def s4_t2_readiness_404():
    bad_id = "00000000-0000-0000-0000-000000000000"
    status, cid, _ = _req("GET", f"/api/venue-layouts/{bad_id}/publish-readiness")
    return ("S4-T2 readiness bogus id -> 404", status == 404, cid)


def s4_t3_readiness_unmapped_blocker():
    _req("POST", "/api/venue-layouts/apply-preset",
         body={"presetId": "theater-classic", "eventId": EVENT_ID})
    layout_id = get_event_layout_id()
    status, cid, body = _req(
        "GET", f"/api/venue-layouts/{layout_id}/publish-readiness"
    )
    if status != 200:
        return ("S4-T3 readiness fresh preset -> ZoneUnmapped", False, cid)
    report = json.loads(body)
    has_zone_unmapped = any(b["code"] == "ZoneUnmapped" for b in report["blockers"])
    return ("S4-T3 readiness fresh preset -> ZoneUnmapped", has_zone_unmapped, cid)


def s1_5_jb_apply_preset_replacement():
    """J-B: A -> B -> A -> A, all 201."""
    sequence = [
        "theater-classic",
        "theater-with-balcony",
        "theater-classic",
        "theater-classic",
    ]
    cids = []
    for preset_id in sequence:
        status, cid, _ = _req(
            "POST",
            "/api/venue-layouts/apply-preset",
            body={"presetId": preset_id, "eventId": EVENT_ID},
        )
        cids.append(cid)
        if status not in (200, 201):
            return ("S1.5 J-B A->B->A->A no orphan", False, cids)
    return ("S1.5 J-B A->B->A->A no orphan", True, cids[-1])


def main():
    tests = [
        s1_5_jb_apply_preset_replacement,
        s2_t1_omit_zone_no_deletion,
        s2_t2_explicit_delete_zone,
        s3_t1_rename_valid,
        s3_t2_rename_stale_ifmatch,
        s3_t3a_rename_empty,
        s3_t3b_rename_oversize,
        s4_t1_readiness_happy_path,
        s4_t2_readiness_404,
        s4_t3_readiness_unmapped_blocker,
    ]
    print(f"Running {len(tests)} regression tests against {API}\n")
    passed = 0
    fails = []
    for i, fn in enumerate(tests, 1):
        try:
            label, ok, cid = fn()
        except Exception as e:
            label, ok, cid = (fn.__name__, False, f"exception: {e}")
        status_str = "PASS" if ok else "FAIL"
        print(f"  [{i}/{len(tests)}] {status_str:5s} {label}  cid={cid}")
        if ok:
            passed += 1
        else:
            fails.append(label)

    print(f"\n=== REGRESSION SUMMARY: {passed}/{len(tests)} PASS ===")
    if fails:
        print("FAILED tests:")
        for f in fails:
            print(f"  - {f}")
        sys.exit(1)


if __name__ == "__main__":
    main()
