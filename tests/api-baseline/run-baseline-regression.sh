#!/usr/bin/env bash
# Phase A W2.8 — API baseline regression
#
# Thin shell wrapper around run-baseline-regression.py for the canonical command
# the master TODO references. All actual logic lives in the Python script (jq
# is not a dev-machine dep on this project; Python already is).
#
# Usage:
#   ./run-baseline-regression.sh                  # diff against staging
#   ./run-baseline-regression.sh --target prod    # diff against prod
#   ./run-baseline-regression.sh --refresh        # refresh baseline
#
# See README.md for full docs.

set -euo pipefail
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
exec python "$SCRIPT_DIR/run-baseline-regression.py" "$@"
