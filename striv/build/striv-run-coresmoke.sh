#!/usr/bin/env bash
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
CONFIGURATION="Debug"
USE_XVFB=0

for arg in "$@"; do
  case "$arg" in
    Debug|Release)
      CONFIGURATION="$arg"
      ;;
    --xvfb)
      USE_XVFB=1
      ;;
    *)
      echo "Unknown argument: $arg"
      echo "Usage: $0 [Debug|Release] [--xvfb]"
      exit 2
      ;;
  esac
done

CORESMOKE_DLL="$ROOT/striv/projects/StriV.CoreSmoke/bin/$CONFIGURATION/net10.0/StriV.CoreSmoke.dll"
BUILD_SCRIPT="$ROOT/striv/build/striv-build-core.sh"

echo "Repo root: $ROOT"
echo "Configuration: $CONFIGURATION"
echo "CoreSmoke DLL: $CORESMOKE_DLL"

run() { echo "+ $*"; "$@"; }

run "$BUILD_SCRIPT" "$CONFIGURATION"

if [[ ! -f "$CORESMOKE_DLL" ]]; then
  echo "Runtime failure classification: missing build output"
  echo "CoreSmoke DLL not found: $CORESMOKE_DLL"
  exit 1
fi

RUN_CMD=(dotnet "$CORESMOKE_DLL")
if command -v timeout >/dev/null 2>&1; then
  TIMEOUT_CMD=(timeout 20s)
  echo "Timeout behavior: enforcing 20s timeout via 'timeout'."
else
  TIMEOUT_CMD=()
  echo "Timeout behavior: WARNING - 'timeout' unavailable; running without timeout."
fi

if [[ "$USE_XVFB" -eq 1 ]]; then
  RUN_CMD=(xvfb-run -a "${RUN_CMD[@]}")
  echo "Display mode: xvfb-run -a"
else
  echo "Display mode: direct"
fi

FULL_CMD=("${TIMEOUT_CMD[@]}" "${RUN_CMD[@]}")
echo "Run command: ${FULL_CMD[*]}"

set +e
"${FULL_CMD[@]}"
EXIT_CODE=$?
set -e

echo "Runtime exit code: $EXIT_CODE"

if [[ "$EXIT_CODE" -eq 0 ]]; then
  echo "Runtime status: success"
  exit 0
fi

if [[ "$EXIT_CODE" -eq 124 ]]; then
  echo "Runtime failure classification: timeout/hang"
  exit "$EXIT_CODE"
fi

echo "Runtime failure classification hints:"
echo "- SDL/display/X11 unavailable"
echo "- Vulkan loader/ICD/device failure"
echo "- missing native library"
echo "- content/shader/effect runtime failure"
echo "- managed engine/runtime exception"

echo "Runtime status: failure"
exit "$EXIT_CODE"
