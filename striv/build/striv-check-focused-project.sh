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

log_dir="$repo_root/striv/artifacts/logs"
mkdir -p "$log_dir"
log_path="$log_dir/focused-build-${focus_project//./_}-$(date -u +%Y%m%dT%H%M%SZ).log"

set +e
dotnet build "$project_path" -c "$configuration" --no-incremental "-p:StriVWarningFocusProject=$focus_project" >"$log_path" 2>&1
build_exit=$?
set -e

project_dir="$repo_root/striv/projects/$focus_project"
project_dir_abs="$(cd "$project_dir" && pwd)/"
project_dir_rel="striv/projects/$focus_project/"
project_marker="$focus_project.csproj"

focused_warning_lines="$(grep -E 'warning (CS|CA|NU|STRIDE)[0-9]+' "$log_path" | grep -E "$project_dir_abs|$project_dir_rel|$project_marker" || true)"
focused_warning_count=$(printf '%s\n' "$focused_warning_lines" | sed '/^$/d' | wc -l | tr -d ' ')

echo "Focused project: $focus_project"
echo "Build exit code: $build_exit"
echo "Focused warning count: $focused_warning_count"

if [[ "$focused_warning_count" -gt 0 ]]; then
  echo "Top focused warning codes:"
  printf '%s\n' "$focused_warning_lines" \
    | sed -n 's/.* warning \([A-Z]\+[0-9]\+\).*/\1/p' \
    | sort | uniq -c | sort -nr | head -n 10
fi

echo "Log path: $log_path"

if [[ $build_exit -ne 0 ]]; then
  exit $build_exit
fi

if [[ "$focused_warning_count" -ne 0 ]]; then
  exit 4
fi

