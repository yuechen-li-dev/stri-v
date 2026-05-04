#!/usr/bin/env bash
set -euo pipefail

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
repo_root="$(cd "${script_dir}/../.." && pwd)"

manifest="striv/tests/fixtures/assets/shader_manifest/assets.toml"
output="/tmp/striv-assets"
extra_args=()

while (($#)); do
  case "$1" in
    --manifest)
      manifest="$2"
      shift 2
      ;;
    --output)
      output="$2"
      shift 2
      ;;
    *)
      extra_args+=("$1")
      shift
      ;;
  esac
done

cmd=(
  dotnet run --project striv/projects/StriV.AssetTool/StriV.AssetTool.csproj -- build-assets
  --manifest "$manifest"
  --output "$output"
)

if ((${#extra_args[@]})); then
  cmd+=("${extra_args[@]}")
fi

printf 'Running: '
printf '%q ' "${cmd[@]}"
printf '\n'

cd "$repo_root"
"${cmd[@]}"
