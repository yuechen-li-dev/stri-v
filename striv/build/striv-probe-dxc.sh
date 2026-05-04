#!/usr/bin/env bash
set -euo pipefail

require=0
compile_smoke=1

for arg in "$@"; do
  case "$arg" in
    --require) require=1 ;;
    --no-compile-smoke) compile_smoke=0 ;;
    -h|--help)
      cat <<'USAGE'
Usage: striv-probe-dxc.sh [--require] [--no-compile-smoke]

Probes DXC availability and optional SPIR-V compile smoke.
  --require           return nonzero if DXC is missing/unhealthy
  --no-compile-smoke  skip tiny temp HLSL -> SPIR-V compile check
USAGE
      exit 0
      ;;
    *)
      echo "Unknown argument: $arg" >&2
      exit 2
      ;;
  esac
done

echo "[dxc-probe] checking for dxc on PATH..."
if ! command -v dxc >/dev/null 2>&1; then
  echo "[dxc-probe] dxc: NOT FOUND"
  if [[ "$require" -eq 1 ]]; then
    echo "[dxc-probe] strict mode enabled (--require): failing" >&2
    exit 1
  fi
  exit 0
fi

dxc_path="$(command -v dxc)"
echo "[dxc-probe] dxc: FOUND"
echo "[dxc-probe] path: $dxc_path"

echo "[dxc-probe] running 'dxc --help' health check..."
if ! help_output="$(dxc --help 2>&1)"; then
  echo "[dxc-probe] dxc --help: FAILED"
  if [[ "$require" -eq 1 ]]; then
    exit 1
  fi
  exit 0
fi
echo "[dxc-probe] dxc --help: OK"

version_line="$(printf '%s\n' "$help_output" | head -n 1 || true)"
if [[ -n "$version_line" ]]; then
  echo "[dxc-probe] version: $version_line"
fi

if printf '%s\n' "$help_output" | grep -q -- '-spirv'; then
  echo "[dxc-probe] spirv flag support: YES (-spirv found in help)"
  has_spirv=1
else
  echo "[dxc-probe] spirv flag support: NO (-spirv not found in help)"
  has_spirv=0
fi

if [[ "$compile_smoke" -eq 1 && "$has_spirv" -eq 1 ]]; then
  tmpdir="$(mktemp -d)"
  trap 'rm -rf "$tmpdir"' EXIT
  hlsl="$tmpdir/probe.hlsl"
  spv="$tmpdir/probe.spv"
  cat > "$hlsl" <<'HLSL'
float4 main() : SV_Target0
{
    return float4(1.0, 0.0, 0.0, 1.0);
}
HLSL

  echo "[dxc-probe] compile smoke: attempting tiny HLSL -> SPIR-V"
  if dxc -spirv -T ps_6_0 -E main "$hlsl" -Fo "$spv" >/dev/null 2>&1; then
    if [[ -s "$spv" ]]; then
      echo "[dxc-probe] compile smoke: OK"
    else
      echo "[dxc-probe] compile smoke: FAILED (empty output)"
      [[ "$require" -eq 1 ]] && exit 1
    fi
  else
    echo "[dxc-probe] compile smoke: FAILED"
    [[ "$require" -eq 1 ]] && exit 1
  fi
elif [[ "$compile_smoke" -eq 1 ]]; then
  echo "[dxc-probe] compile smoke: skipped (-spirv not available)"
else
  echo "[dxc-probe] compile smoke: skipped (--no-compile-smoke)"
fi

exit 0
