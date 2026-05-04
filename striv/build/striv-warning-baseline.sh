#!/usr/bin/env bash
set -euo pipefail

usage() {
  cat <<'USAGE'
Usage: striv-warning-baseline.sh [--log <path>]

Runs the canonical Stri-V warning baseline build and prints:
- build exit code
- build summary warning count (last "N Warning(s)" line)
- extracted warning-line count
- top warning codes
- top warning projects
USAGE
}

LOG_PATH=""
while [[ $# -gt 0 ]]; do
  case "$1" in
    --log)
      [[ $# -ge 2 ]] || { echo "--log requires a path" >&2; exit 2; }
      LOG_PATH="$2"
      shift 2
      ;;
    -h|--help)
      usage
      exit 0
      ;;
    *)
      echo "Unknown argument: $1" >&2
      usage >&2
      exit 2
      ;;
  esac
done

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
LOG_PATH="${LOG_PATH:-$(mktemp /tmp/striv-warning-baseline.XXXXXX.log)}"
WARN_LINES="${LOG_PATH%.log}.warnings.log"

set +e
(
  cd "$ROOT"
  ./striv/build/striv-build-core.sh
) 2>&1 | tee "$LOG_PATH"
BUILD_EXIT=${PIPESTATUS[0]}
set -e

grep -E "warning (CS|CA|NU|STRIDE)[0-9]+" "$LOG_PATH" > "$WARN_LINES" || true
SUMMARY_WARNINGS=$(grep -E '^[[:space:]]*[0-9]+ Warning\(s\)' "$LOG_PATH" | tail -n 1 | awk '{print $1}')
SUMMARY_WARNINGS=${SUMMARY_WARNINGS:-N/A}

printf '\n== Stri-V warning baseline ==\n'
printf 'Build exit code: %s\n' "$BUILD_EXIT"
printf 'Build log: %s\n' "$LOG_PATH"
printf 'Extracted warnings log: %s\n' "$WARN_LINES"
printf 'Build-summary warning count: %s\n' "$SUMMARY_WARNINGS"
printf 'Extracted warning-line count: %s\n' "$(wc -l < "$WARN_LINES")"

printf '\nTop warning codes:\n'
if [[ -s "$WARN_LINES" ]]; then
  sed -E 's/.*warning ((CS|CA|NU|STRIDE)[0-9]+).*/\1/' "$WARN_LINES" | sort | uniq -c | sort -nr | head -n 20
else
  echo '(none)'
fi

printf '\nTop warning projects:\n'
if [[ -s "$WARN_LINES" ]]; then
  grep -Eo '\[[^]]+\.csproj\]' "$WARN_LINES" | sort | uniq -c | sort -nr | head -n 20
else
  echo '(none)'
fi

exit "$BUILD_EXIT"
