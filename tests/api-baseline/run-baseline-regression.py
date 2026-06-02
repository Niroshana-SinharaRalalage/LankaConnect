#!/usr/bin/env python3
"""Phase A W2.8 — API baseline regression.

Fetches the current staging (or prod) OpenAPI document from /swagger/v1/swagger.json
and diffs it against the checked-in baseline. The intent is to catch STRUCTURAL
drift during Phase A module extractions: paths disappearing, HTTP verbs on a
shared path being removed, or DTO schemas being renamed/dropped.

Exit codes:
  0  no breaking drift (baseline equal to current, or current is a strict superset)
  1  breaking drift detected (paths/verbs/schemas removed)
  2  script error (network failure, malformed JSON, etc.)

Usage:
  run-baseline-regression.py                 # diff against staging (default)
  run-baseline-regression.py --target prod   # diff against prod (read-only)
  run-baseline-regression.py --refresh       # refresh the baseline file
                                             # (only after a deliberate additive
                                             # API change is already live)
"""
import argparse
import json
import sys
import urllib.error
import urllib.request
from pathlib import Path

STAGING_URL = "https://lankaconnect-api-staging.politebay-79d6e8a2.eastus2.azurecontainerapps.io/swagger/v1/swagger.json"
PROD_URL = "https://lankaconnect-api-prod.graystone-d581eaeb.eastus2.azurecontainerapps.io/swagger/v1/swagger.json"

SCRIPT_DIR = Path(__file__).resolve().parent
BASELINE_PATH = SCRIPT_DIR / "openapi-baseline.json"


def fetch_openapi(url: str) -> dict:
    """Fetch + parse the OpenAPI document. Exit 2 on any failure."""
    try:
        with urllib.request.urlopen(url, timeout=30) as resp:
            if resp.status != 200:
                print(f"ERROR: {url} returned HTTP {resp.status}", file=sys.stderr)
                sys.exit(2)
            return json.loads(resp.read().decode("utf-8"))
    except urllib.error.HTTPError as e:
        print(f"ERROR: {url} returned HTTP {e.code}", file=sys.stderr)
        body = e.read().decode("utf-8", errors="replace")
        print(body[:500], file=sys.stderr)
        sys.exit(2)
    except (urllib.error.URLError, OSError) as e:
        print(f"ERROR: could not reach {url}: {e}", file=sys.stderr)
        sys.exit(2)
    except json.JSONDecodeError as e:
        print(f"ERROR: response from {url} was not valid JSON: {e}", file=sys.stderr)
        sys.exit(2)


def path_set(doc: dict) -> set[str]:
    return set(doc.get("paths", {}).keys())


def schema_set(doc: dict) -> set[str]:
    return set(doc.get("components", {}).get("schemas", {}).keys())


def verbs_for_path(doc: dict, path: str) -> set[str]:
    """Set of HTTP verbs (upper-case) defined on a given path."""
    methods = doc.get("paths", {}).get(path, {})
    # Filter out non-verb keys like 'parameters', 'summary'.
    verbs = {"get", "post", "put", "patch", "delete", "options", "head", "trace"}
    return {k.upper() for k in methods.keys() if k.lower() in verbs}


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--target", choices=["staging", "prod"], default="staging")
    parser.add_argument("--refresh", action="store_true", help="overwrite baseline with current target")
    args = parser.parse_args()

    target_url = STAGING_URL if args.target == "staging" else PROD_URL

    print(f"Fetching {target_url} ...")
    current = fetch_openapi(target_url)

    if args.refresh:
        BASELINE_PATH.write_text(json.dumps(current, indent=2) + "\n", encoding="utf-8")
        print(
            f"Baseline refreshed: {len(path_set(current))} paths, "
            f"{len(schema_set(current))} schemas"
        )
        return 0

    if not BASELINE_PATH.exists():
        print(f"ERROR: baseline not found at {BASELINE_PATH}", file=sys.stderr)
        print("       run with --refresh to capture an initial baseline.", file=sys.stderr)
        return 2

    baseline = json.loads(BASELINE_PATH.read_text(encoding="utf-8"))

    baseline_paths = path_set(baseline)
    current_paths = path_set(current)
    removed_paths = sorted(baseline_paths - current_paths)
    added_paths = sorted(current_paths - baseline_paths)

    baseline_schemas = schema_set(baseline)
    current_schemas = schema_set(current)
    removed_schemas = sorted(baseline_schemas - current_schemas)
    added_schemas = sorted(current_schemas - baseline_schemas)

    breaking = False
    print()
    print("=== Phase A W2.8 — API baseline regression ===")
    print(
        f"Baseline: {baseline.get('info', {}).get('title', '?')} "
        f"({len(baseline_paths)} paths, {len(baseline_schemas)} schemas)"
    )
    print(
        f"Current:  {current.get('info', {}).get('title', '?')}  "
        f"({len(current_paths)} paths, {len(current_schemas)} schemas)"
    )
    print()

    if removed_paths:
        breaking = True
        print(f"BREAKING — paths REMOVED since baseline ({len(removed_paths)}):")
        for p in removed_paths:
            print(f"  - {p}")
        print()

    if removed_schemas:
        breaking = True
        print(f"BREAKING — schemas REMOVED since baseline ({len(removed_schemas)}):")
        for s in removed_schemas:
            print(f"  - {s}")
        print()

    # Per-path verb removal — catches "GET /foo" turning into "POST /foo".
    verb_removals: list[tuple[str, set[str]]] = []
    for p in sorted(baseline_paths & current_paths):
        lost = verbs_for_path(baseline, p) - verbs_for_path(current, p)
        if lost:
            verb_removals.append((p, lost))
    if verb_removals:
        breaking = True
        print(f"BREAKING — HTTP verbs REMOVED on shared paths ({len(verb_removals)}):")
        for p, lost in verb_removals:
            print(f"  - {p}: lost {', '.join(sorted(lost))}")
        print()

    if added_paths:
        print(f"additive — paths added since baseline ({len(added_paths)}):")
        for p in added_paths[:10]:
            print(f"  + {p}")
        if len(added_paths) > 10:
            print(f"  ... and {len(added_paths) - 10} more")
        print()

    if added_schemas:
        print(
            f"additive — schemas added since baseline: {len(added_schemas)} "
            "(use --refresh after a deliberate API change to update the baseline)"
        )
        print()

    if not breaking:
        print("OK — no breaking drift")
        return 0
    print("FAIL — breaking drift detected (see above)")
    return 1


if __name__ == "__main__":
    sys.exit(main())
