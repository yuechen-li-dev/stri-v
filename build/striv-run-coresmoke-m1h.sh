#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"

CONFIGURATION="Debug"
SDL_VIDEO_DRIVER=""

while [[ $# -gt 0 ]]; do
  case "$1" in
    Debug|Release)
      CONFIGURATION="$1"
      shift
      ;;
    --sdl-video-driver)
      if [[ $# -lt 2 ]]; then
        echo "Error: --sdl-video-driver requires a value (for example: dummy, offscreen)." >&2
        exit 2
      fi
      SDL_VIDEO_DRIVER="$2"
      shift 2
      ;;
    *)
      echo "Error: unknown argument '$1'." >&2
      echo "Usage: $0 [Debug|Release] [--sdl-video-driver <driver>]" >&2
      exit 2
      ;;
  esac
done

CORE_SMOKE_DLL="$REPO_ROOT/samples/StriV/CoreSmoke/bin/$CONFIGURATION/net10.0/StriV.CoreSmoke.dll"

echo "[striv-coresmoke-m1h] Repo root: $REPO_ROOT"
echo "[striv-coresmoke-m1h] Configuration: $CONFIGURATION"
if [[ -n "$SDL_VIDEO_DRIVER" ]]; then
  echo "[striv-coresmoke-m1h] SDL video driver override: $SDL_VIDEO_DRIVER"
else
  echo "[striv-coresmoke-m1h] SDL video driver override: <none>"
fi

echo "[striv-coresmoke-m1h] Building CoreSmoke via M1g build script..."
"$REPO_ROOT/build/striv-build-coresmoke-m1g.sh" "$CONFIGURATION"

if [[ ! -f "$CORE_SMOKE_DLL" ]]; then
  echo "Error: CoreSmoke DLL not found at: $CORE_SMOKE_DLL" >&2
  exit 3
fi

echo "[striv-coresmoke-m1h] CoreSmoke DLL: $CORE_SMOKE_DLL"

run_exit=0
if command -v timeout >/dev/null 2>&1; then
  if [[ -n "$SDL_VIDEO_DRIVER" ]]; then
    echo "[striv-coresmoke-m1h] Run command: SDL_VIDEODRIVER=$SDL_VIDEO_DRIVER timeout 20s dotnet $CORE_SMOKE_DLL"
    set +e
    SDL_VIDEODRIVER="$SDL_VIDEO_DRIVER" timeout 20s dotnet "$CORE_SMOKE_DLL"
    run_exit=$?
    set -e
  else
    echo "[striv-coresmoke-m1h] Run command: timeout 20s dotnet $CORE_SMOKE_DLL"
    set +e
    timeout 20s dotnet "$CORE_SMOKE_DLL"
    run_exit=$?
    set -e
  fi
else
  echo "[striv-coresmoke-m1h] Warning: timeout command not found; running without timeout safety."
  if [[ -n "$SDL_VIDEO_DRIVER" ]]; then
    echo "[striv-coresmoke-m1h] Run command: SDL_VIDEODRIVER=$SDL_VIDEO_DRIVER dotnet $CORE_SMOKE_DLL"
    set +e
    SDL_VIDEODRIVER="$SDL_VIDEO_DRIVER" dotnet "$CORE_SMOKE_DLL"
    run_exit=$?
    set -e
  else
    echo "[striv-coresmoke-m1h] Run command: dotnet $CORE_SMOKE_DLL"
    set +e
    dotnet "$CORE_SMOKE_DLL"
    run_exit=$?
    set -e
  fi
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
echo "[striv-coresmoke-m1h] If output contains 'x11 not available' or display/window allocation failures, classify as environment limitation." >&2
echo "[striv-coresmoke-m1h] If output contains dummy/offscreen SDL driver unavailable errors, classify as headless probe limitation (non-authoritative for engine runtime)." >&2
echo "[striv-coresmoke-m1h] If output contains Vulkan loader/ICD/device creation failures, classify as environment limitation." >&2
echo "[striv-coresmoke-m1h] If output contains graphics device/swapchain creation failures, classify as environment limitation unless local desktop reproduction indicates engine issue." >&2
echo "[striv-coresmoke-m1h] If output contains missing native library errors, classify as environment/native packaging blocker." >&2
echo "[striv-coresmoke-m1h] If output contains managed engine/runtime exceptions, classify as potential engine/runtime blocker." >&2
exit $run_exit
