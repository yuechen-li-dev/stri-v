#!/usr/bin/env bash
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
AP_PROJ="$ROOT/striv/projects/Stride.Core.AssemblyProcessor/Stride.Core.AssemblyProcessor.csproj"
SOLN="$ROOT/striv/StriV.Core.slnx"
AP_DLL="$ROOT/striv/projects/Stride.Core.AssemblyProcessor/bin/Debug/net10.0/Stride.Core.AssemblyProcessor.dll"
run(){ echo "+ $*"; "$@"; }
run dotnet build "$AP_PROJ" -c Debug
[[ -f "$AP_DLL" ]] || { echo "AP missing: $AP_DLL"; exit 1; }
[[ $(stat -c%s "$AP_DLL") -gt 1024 ]] || { echo "AP too small"; exit 1; }
head -n 1 "$AP_DLL" | grep -q "version https://git-lfs" && { echo "AP is LFS pointer"; exit 1; } || true
head -c 2 "$AP_DLL" | od -An -tx1 | tr -d ' \n' | grep -qi '^4d5a$' || { echo "AP is not MZ PE"; exit 1; }
run dotnet restore "$SOLN"
run dotnet build "$SOLN" -c Debug -p:StriVAssemblyProcessorPath="$AP_DLL"
