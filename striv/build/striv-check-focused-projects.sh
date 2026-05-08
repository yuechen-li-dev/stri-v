#!/usr/bin/env bash
set -euo pipefail

if [[ $# -lt 1 ]]; then
  echo "Usage: $0 <ProjectName> [ProjectName ...]" >&2
  exit 2
fi

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
repo_root="$(cd "$script_dir/../.." && pwd)"
check_script="$script_dir/striv-check-focused-project.sh"

if [[ ! -x "$check_script" ]]; then
  echo "Focused project checker not executable: $check_script" >&2
  exit 3
fi

log_dir="$repo_root/striv/artifacts/logs"
mkdir -p "$log_dir"
summary_path="$log_dir/focused-warning-summary.jsonl"
: > "$summary_path"

printf '%-32s %-8s %-10s %-13s %s\n' "Project" "Exit" "Warnings" "Status" "Log"
printf '%-32s %-8s %-10s %-13s %s\n' "-------" "----" "--------" "------" "---"

any_fail=0
for project in "$@"; do
  tmp_output="$(mktemp)"
  set +e
  bash "$check_script" "$project" >"$tmp_output" 2>&1
  exit_code=$?
  set -e

  warning_count="$(sed -n 's/^Focused warning count: \([0-9][0-9]*\)$/\1/p' "$tmp_output" | tail -n 1)"
  if [[ -z "$warning_count" ]]; then
    warning_count=-1
  fi

  log_path="$(sed -n 's/^Log path: \(.*\)$/\1/p' "$tmp_output" | tail -n 1)"
  if [[ -z "$log_path" ]]; then
    log_path="(missing)"
  fi

  status="pass"
  if [[ $exit_code -ne 0 ]]; then
    if [[ "$warning_count" -gt 0 ]]; then
      status="warnings"
    else
      status="build-failed"
    fi
    any_fail=1
  fi

  escaped_project="${project//\"/\\\"}"
  escaped_log="${log_path//\"/\\\"}"
  printf '{"project":"%s","exitCode":%d,"warningCount":%d,"status":"%s","logPath":"%s"}\n' \
    "$escaped_project" "$exit_code" "$warning_count" "$status" "$escaped_log" >> "$summary_path"

  printf '%-32s %-8d %-10s %-13s %s\n' "$project" "$exit_code" "$warning_count" "$status" "$log_path"

  rm -f "$tmp_output"
done

echo "Summary artifact: $summary_path"

if [[ $any_fail -ne 0 ]]; then
  exit 1
fi

exit 0
