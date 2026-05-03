#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"

CONFIGURATION="${1:-Debug}"
if [[ "$CONFIGURATION" != "Debug" && "$CONFIGURATION" != "Release" ]]; then
  echo "Error: configuration must be Debug or Release (got '$CONFIGURATION')." >&2
  exit 2
fi
shift $(( $# > 0 ? 1 : 0 ))
EXTRA_ARGS=("$@")

AP_PROJECT="$REPO_ROOT/sources/core/Stride.Core.AssemblyProcessor/Stride.Core.AssemblyProcessor.csproj"
AP_OUTPUT_DIR="$REPO_ROOT/sources/core/Stride.Core.AssemblyProcessor/bin/$CONFIGURATION/net10.0/"
AP_DLL="${AP_OUTPUT_DIR}Stride.Core.AssemblyProcessor.dll"
M1F_SLNF="$REPO_ROOT/build/StriV.Engine.Bepu.M1f.slnf"

echo "[striv-engine-bepu-m1f] Repo root: $REPO_ROOT"
echo "[striv-engine-bepu-m1f] Configuration: $CONFIGURATION"
echo "[striv-engine-bepu-m1f] Platform: Linux"
echo "[striv-engine-bepu-m1f] Graphics API: Vulkan"
echo "[striv-engine-bepu-m1f] Shader compiler: disabled"
echo "[striv-engine-bepu-m1f] Audio: disabled"
echo "[striv-engine-bepu-m1f] VR: disabled"
echo "[striv-engine-bepu-m1f] AssemblyProcessor output directory: $AP_OUTPUT_DIR"
echo "[striv-engine-bepu-m1f] M1f solution filter: $M1F_SLNF"

echo "[striv-engine-bepu-m1f] Building AssemblyProcessor..."
dotnet build "$AP_PROJECT" -c "$CONFIGURATION" -v minimal

if [[ ! -f "$AP_DLL" ]]; then
  echo "Error: AssemblyProcessor DLL not found at: $AP_DLL" >&2
  exit 3
fi

file_size=$(wc -c < "$AP_DLL")
if (( file_size <= 1024 )); then
  echo "Error: AssemblyProcessor DLL is unexpectedly small (${file_size} bytes): $AP_DLL" >&2
  exit 4
fi

head_text=$(head -c 64 "$AP_DLL" | tr -d '\0' || true)
if [[ "$head_text" == version\ https://git-lfs.github* ]]; then
  echo "Error: AssemblyProcessor DLL appears to be a Git LFS pointer file: $AP_DLL" >&2
  exit 5
fi

first2_hex=$(od -An -tx1 -N2 "$AP_DLL" | tr -d ' \n')
if [[ "$first2_hex" != "4d5a" ]]; then
  echo "Error: AssemblyProcessor DLL is not a valid PE payload (expected MZ header, got $first2_hex): $AP_DLL" >&2
  exit 6
fi

echo "[striv-engine-bepu-m1f] AssemblyProcessor payload validation passed (size=${file_size} bytes, header=MZ)."

echo "[striv-engine-bepu-m1f] Building Stri-V Engine Bepu M1f..."
dotnet build "$M1F_SLNF" -c "$CONFIGURATION" -v minimal \
  -p:StridePlatforms=Linux \
  -p:StrideGraphicsApis=Vulkan \
  -p:StrideIncludeShaderCompiler=false \
  -p:StrideIncludeAudio=false \
  -p:StrideIncludeVirtualReality=false \
  -p:StrideAssemblyProcessorFramework=net10.0 \
  "-p:StrideAssemblyProcessorBasePath=$AP_OUTPUT_DIR" \
  -p:StrideAssemblyProcessorHash=sourcebuild \
  "${EXTRA_ARGS[@]}"

echo "[striv-engine-bepu-m1f] Build completed successfully."
