# Stri-V Asset Tool M5e closeout validation

## 1) Files changed

- `docs/stri-v/building-core.md`
- `striv/build/striv-build-assets.sh`
- `striv/build/striv-build-assets.ps1`
- `docs/stri-v/audits/860-striv-asset-tool-m5e-closeout-validation.md`

## 2) Documentation changes

Added an Asset Tool section to `docs/stri-v/building-core.md` covering:

- Canonical `dotnet run` usage for `build-assets`.
- Quiet mode (`--quiet`) example.
- JSONL diagnostics (`--diagnostics jsonl`) CI-oriented example.
- Exit codes:
  - `0`: success / no fatal diagnostics
  - `1`: fatal parse/validation/build diagnostics
  - `2`: missing manifest path
- Diagnostics formats in examples (`text` via default behavior and `jsonl` explicitly).
- DXC behavior:
  - optional by default,
  - strict failure mode via `--strict-dxc`,
  - explicit disable via `--no-dxc`.
- Current slice limitations (shader-only assets, no runtime/editor/watch/incremental features).

## 3) Helper script

Added helper wrappers:

- Bash: `striv/build/striv-build-assets.sh`
- PowerShell: `striv/build/striv-build-assets.ps1`

Behavior:

- Resolve repository root relative to script location.
- Invoke `StriV.AssetTool` `build-assets` via `dotnet run`.
- Defaults:
  - `--manifest striv/tests/fixtures/assets/shader_manifest/assets.toml`
  - `--output /tmp/striv-assets`
- Supports overriding `--manifest` and `--output`.
- Passes all additional arguments through directly to AssetTool.
- Prints concise full command before execution.
- Preserves tool exit code.

## 4) Validation results

| Command | Exit code | First meaningful warning/error | Pass/Fail | Output truncated |
|---|---:|---|---|---|
| `dotnet test striv/tests/StriV.AssetTool.Tests/StriV.AssetTool.Tests.csproj -v minimal` | 0 | `warning CS8604` in `AssetPipeline.cs` (nullability warning) | Pass | No |
| `dotnet test striv/tests/StriV.AssetPipeline.Tests/StriV.AssetPipeline.Tests.csproj --no-build -v minimal` | 0 | none | Pass | No |
| `dotnet test striv/tests/StriV.ShaderPipeline.Tests/StriV.ShaderPipeline.Tests.csproj --no-build -v minimal` | 0 | none | Pass | No |
| `dotnet test striv/tests/StriV.CleanGraph.Tests/StriV.CleanGraph.Tests.csproj --no-build -v minimal` | 0 | none | Pass | No |
| `./striv/build/striv-build-core.sh` | 0 | many existing nullability/obsoletion warnings; no errors | Pass | Yes |
| `dotnet run --project striv/projects/StriV.AssetTool/StriV.AssetTool.csproj -- build-assets --manifest striv/tests/fixtures/assets/shader_manifest/assets.toml --output /tmp/striv-asset-tool-closeout` | 0 | none | Pass | No |
| `dotnet run --project striv/projects/StriV.AssetTool/StriV.AssetTool.csproj -- build-assets --manifest striv/tests/fixtures/assets/shader_manifest/assets.toml --output /tmp/striv-asset-tool-closeout-jsonl --diagnostics jsonl` | 0 | none | Pass | No |
| `dotnet run --project striv/projects/StriV.AssetTool/StriV.AssetTool.csproj -- build-assets --manifest striv/tests/fixtures/assets/shader_manifest/assets.toml --output /tmp/striv-asset-tool-closeout-quiet --quiet` | 0 | none | Pass | No |
| `./striv/build/striv-build-assets.sh --manifest striv/tests/fixtures/assets/shader_manifest/assets.toml --output /tmp/striv-build-assets-script-smoke --diagnostics jsonl` | 0 | none | Pass | No |

## 5) Current asset CLI status

- Supported asset kind: shader only.
- Manifest format: flat TOML.
- Artifact output: JSON manifest + generated/lowered HLSL + optional SPIR-V/logs.
- Runtime/editor integration: none.

## 6) Recommended next task

Recommend a runtime shader artifact loading audit as the next slice, assuming no blocker from this validation set.
