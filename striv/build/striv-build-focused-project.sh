#!/usr/bin/env bash
set -euo pipefail

if [[ $# -lt 1 || $# -gt 2 ]]; then
  echo "Usage: $0 <ProjectName> [Configuration]" >&2
  exit 2
fi

focus_project="$1"
configuration="${2:-Debug}"

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
repo_root="$(cd "$script_dir/../.." && pwd)"
project_path="$repo_root/striv/projects/$focus_project/$focus_project.csproj"

if [[ ! -f "$project_path" ]]; then
  echo "Focused project file not found: $project_path" >&2
  exit 3
fi

cmd=(dotnet build "$project_path" -c "$configuration" "-p:StriVWarningFocusProject=$focus_project")
echo "Running: ${cmd[*]}"
"${cmd[@]}"
