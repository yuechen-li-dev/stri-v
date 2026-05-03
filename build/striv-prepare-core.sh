#!/usr/bin/env bash
set -euo pipefail

CONFIGURATION="Debug"
DO_BUILD="false"

for arg in "$@"; do
  case "$arg" in
    --build)
      DO_BUILD="true"
      ;;
    Debug|Release)
      CONFIGURATION="$arg"
      ;;
    *)
      echo "Unknown argument: $arg" >&2
      exit 2
      ;;
  esac
done

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
AP_PROJECT="$REPO_ROOT/sources/core/Stride.Core.AssemblyProcessor/Stride.Core.AssemblyProcessor.csproj"
SOLUTION="$REPO_ROOT/build/StriV.Core.slnx"

echo "[striv] Repo root: $REPO_ROOT"
echo "[striv] Building AssemblyProcessor ($CONFIGURATION / net10.0)..."
dotnet build "$AP_PROJECT" -c "$CONFIGURATION" -f net10.0

AP_OUTPUT="$(realpath "$REPO_ROOT/sources/core/Stride.Core.AssemblyProcessor/bin/$CONFIGURATION/net10.0")/"
AP_DLL="${AP_OUTPUT}Stride.Core.AssemblyProcessor.dll"

[[ -f "$AP_DLL" ]] || { echo "AssemblyProcessor output missing: $AP_DLL" >&2; exit 1; }
[[ $(stat -c %s "$AP_DLL") -gt 1024 ]] || { echo "AssemblyProcessor output too small: $AP_DLL" >&2; exit 1; }
head -c 64 "$AP_DLL" | grep -q 'version https://git-lfs.github' && { echo "AssemblyProcessor output appears to be Git LFS pointer: $AP_DLL" >&2; exit 1; }
MAGIC="$(od -An -tx1 -N2 "$AP_DLL" | tr -d " 
")"
[[ "$MAGIC" == "4d5a" ]] || { echo "AssemblyProcessor output is not a valid PE/MZ binary: $AP_DLL" >&2; exit 1; }

PROPS=(
  "-p:StridePlatforms=Linux"
  "-p:StrideGraphicsApis=Vulkan"
  "-p:StrideIncludeShaderCompiler=false"
  "-p:StrideIncludeAudio=false"
  "-p:StrideIncludeVirtualReality=false"
  "-p:StrideAssemblyProcessorFramework=net10.0"
  "-p:StrideAssemblyProcessorBasePath=$AP_OUTPUT"
  "-p:StrideAssemblyProcessorHash=sourcebuild"
)

echo "[striv] Restoring StriV.Core.slnx with Stri-V Core profile..."
dotnet restore "$SOLUTION" "${PROPS[@]}"

if [[ "$DO_BUILD" == "true" ]]; then
  echo "[striv] Building StriV.Core.slnx with same profile properties..."
  dotnet build "$SOLUTION" -c "$CONFIGURATION" "${PROPS[@]}"
fi

echo
echo "Open build/StriV.Core.slnx in Visual Studio now."
echo "If VS still shows stale errors, close VS, delete affected obj folders or run restore again."
echo "Use Stri-V scripts for authoritative CLI validation."
