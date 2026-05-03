#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"

CONFIGURATION="${1:-Debug}"
if [[ "$CONFIGURATION" != "Debug" && "$CONFIGURATION" != "Release" ]]; then
  echo "Error: configuration must be Debug or Release (got '$CONFIGURATION')." >&2
  exit 2
fi

CORE_SMOKE_DLL="$REPO_ROOT/samples/StriV/CoreSmoke/bin/$CONFIGURATION/net10.0/StriV.CoreSmoke.dll"

echo "[striv-coresmoke-m1h] Repo root: $REPO_ROOT"
echo "[striv-coresmoke-m1h] Configuration: $CONFIGURATION"

echo "[striv-coresmoke-m1h] Building CoreSmoke via M1g build script..."
"$REPO_ROOT/build/striv-build-coresmoke-m1g.sh" "$CONFIGURATION"

if [[ ! -f "$CORE_SMOKE_DLL" ]]; then
  echo "Error: CoreSmoke DLL not found at: $CORE_SMOKE_DLL" >&2
  exit 3
fi

echo "[striv-coresmoke-m1h] CoreSmoke DLL: $CORE_SMOKE_DLL"

run_exit=0
run_cmd="dotnet $CORE_SMOKE_DLL"
if command -v timeout >/dev/null 2>&1; then
  run_cmd="timeout 20s dotnet $CORE_SMOKE_DLL"
  echo "[striv-coresmoke-m1h] Run command: $run_cmd"
  set +e
  timeout 20s dotnet "$CORE_SMOKE_DLL"
  run_exit=$?
  set -e
else
  echo "[striv-coresmoke-m1h] Warning: timeout command not found; running without timeout safety."
  echo "[striv-coresmoke-m1h] Run command: $run_cmd"
  set +e
  dotnet "$CORE_SMOKE_DLL"
  run_exit=$?
  set -e
fi

echo "[striv-coresmoke-m1h] Runtime exit code: $run_exit"

if [[ $run_exit -eq 0 ]]; then
  echo "[striv-coresmoke-m1h] Runtime smoke passed."
  exit 0
fi

if [[ $run_exit -eq 124 ]]; then
  echo "[striv-coresmoke-m1h] Runtime failed: timeout reached (possible runtime hang)." >&2
  exit $run_exit
fi

echo "[striv-coresmoke-m1h] Runtime failed. Classifying first blocker..." >&2
echo "[striv-coresmoke-m1h] If output contains SDL display initialization failures, classify as environment limitation." >&2
echo "[striv-coresmoke-m1h] If output contains Vulkan loader/ICD/device creation failures, classify as environment limitation." >&2
echo "[striv-coresmoke-m1h] If output contains missing native library errors, classify as environment/native packaging blocker." >&2
echo "[striv-coresmoke-m1h] Otherwise classify as potential engine/runtime blocker." >&2
exit $run_exit
