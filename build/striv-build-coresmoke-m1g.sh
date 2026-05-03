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
M1G_SLNF="$REPO_ROOT/build/StriV.CoreSmoke.M1g.slnf"

echo "[striv-coresmoke-m1g] Repo root: $REPO_ROOT"
echo "[striv-coresmoke-m1g] Configuration: $CONFIGURATION"
echo "[striv-coresmoke-m1g] Platform: Linux"
echo "[striv-coresmoke-m1g] Graphics API: Vulkan"
echo "[striv-coresmoke-m1g] Shader compiler: disabled"
echo "[striv-coresmoke-m1g] Audio: disabled"
echo "[striv-coresmoke-m1g] VR: disabled"
echo "[striv-coresmoke-m1g] AssemblyProcessor output directory: $AP_OUTPUT_DIR"
echo "[striv-coresmoke-m1g] M1g solution filter: $M1G_SLNF"

echo "[striv-coresmoke-m1g] Building AssemblyProcessor..."
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

echo "[striv-coresmoke-m1g] AssemblyProcessor payload validation passed (size=${file_size} bytes, header=MZ)."

echo "[striv-coresmoke-m1g] Building Stri-V CoreSmoke M1g..."
dotnet build "$M1G_SLNF" -c "$CONFIGURATION" -v minimal \
  -p:StridePlatforms=Linux \
  -p:StrideGraphicsApis=Vulkan \
  -p:StrideIncludeShaderCompiler=false \
  -p:StrideIncludeAudio=false \
  -p:StrideIncludeVirtualReality=false \
  -p:StrideAssemblyProcessorFramework=net10.0 \
  "-p:StrideAssemblyProcessorBasePath=$AP_OUTPUT_DIR" \
  -p:StrideAssemblyProcessorHash=sourcebuild \
  "${EXTRA_ARGS[@]}"

echo "[striv-coresmoke-m1g] Build completed successfully."
