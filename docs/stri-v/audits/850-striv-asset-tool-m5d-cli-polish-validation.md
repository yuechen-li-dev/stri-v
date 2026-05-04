# 850 - StriV AssetTool M5d CLI polish validation

## 1) Files changed
- `striv/projects/StriV.AssetTool/Program.cs`
- `striv/projects/StriV.AssetTool/CliDiagnosticFormatter.cs`
- `striv/tests/StriV.AssetTool.Tests/AssetToolCliTests.cs`
- `striv/tests/StriV.AssetTool.Tests/StriV.AssetTool.Tests.csproj`

## 2) Test harness repair
### Old hanging cause
The prior tests launched nested `dotnet run --project ...` for every assertion and synchronously depended on that nested process behavior. In this environment the nested launch path could stall and made `dotnet test` non-deterministic/hanging.

### New process strategy
Tests now:
- run the already-built CLI DLL directly via `dotnet <...>/StriV.AssetTool.dll ...`,
- ensure the CLI project is built by adding a `ProjectReference` from test project to `StriV.AssetTool` (`ReferenceOutputAssembly=false`),
- avoid nested `dotnet run` in test execution.

### Timeout behavior
Each launched CLI process uses a hard timeout of 20 seconds. If timeout elapses, tests kill the process tree and fail with captured partial stdout/stderr.

### stdout/stderr capture behavior
Harness uses `ProcessStartInfo` with:
- `RedirectStandardOutput = true`
- `RedirectStandardError = true`
- `UseShellExecute = false`
- `CreateNoWindow = true`

Both stdout/stderr are consumed asynchronously (`ReadToEndAsync`) while process exit is awaited with timeout logic.

## 3) CLI changes
### `--quiet` behavior
Added `--quiet` option to `build-assets`.
- suppresses non-diagnostic success output (`OK ...` and final `SUCCESS ...`),
- does not suppress diagnostics,
- does not change failure handling or exit codes.

### JSONL artifact/success records
For `--diagnostics jsonl`, CLI now emits additional success records for built artifacts when not quiet, e.g.:
`{"kind":"artifact","id":"shader.sprite_batch","manifestPath":"...","fatal":false}`

Diagnostics stay as diagnostic-shaped records (with `code`, `severity`, etc.).

### Unchanged exit codes
- `0` success/no fatal diagnostics
- `1` fatal parse/validation/build diagnostics
- `2` missing manifest

### System.CommandLine retained
CLI continues using `System.CommandLine` options/commands, with incremental additions only.

## 4) Tests
Updated/added in `StriV.AssetTool.Tests`:
- `Help_RendersBuiltInSystemCommandLineHelp`: verifies root help includes `build-assets` and command help includes `--manifest`/`--output`.
- `BuildAssets_ValidManifest_ReturnsZero`: verifies exit 0, artifact manifest exists, and success line is emitted in default text mode.
- `BuildAssets_QuietSuppressesSuccessText`: verifies `--quiet` suppresses success text.
- `BuildAssets_InvalidManifest_ReturnsNonZeroAndJsonlDiagnostic`: verifies exit 1 and JSONL includes `AM201`.
- `BuildAssets_JsonlCanEmitArtifactRecord`: verifies JSONL success artifact record emission.

Also updated harness internals to direct DLL invocation, asynchronous IO capture, and timeout enforcement.

## 5) Validation results
| Command | Exit | First meaningful warning/error | Pass/Fail | Output truncated |
|---|---:|---|---|---|
| `dotnet test striv/tests/StriV.AssetTool.Tests/StriV.AssetTool.Tests.csproj -v minimal` | 0 | none | Pass | No |
| `dotnet test striv/tests/StriV.AssetPipeline.Tests/StriV.AssetPipeline.Tests.csproj --no-build -v minimal` | 0 | none | Pass | No |
| `dotnet test striv/tests/StriV.ShaderPipeline.Tests/StriV.ShaderPipeline.Tests.csproj --no-build -v minimal` | 0 | none | Pass | No |
| `dotnet test striv/tests/StriV.CleanGraph.Tests/StriV.CleanGraph.Tests.csproj --no-build -v minimal` | 0 | none | Pass | No |
| `./striv/build/striv-build-core.sh` | 0 | existing repo warnings (first: `CS1030` in `ObjectIdBuilder.cs`) | Pass | Yes |
| `dotnet run --project striv/projects/StriV.AssetTool/StriV.AssetTool.csproj -- build-assets --manifest striv/tests/fixtures/assets/shader_manifest/assets.toml --output /tmp/striv-asset-tool-smoke-m5d` | 0 | none | Pass | No |
| `dotnet run --project striv/projects/StriV.AssetTool/StriV.AssetTool.csproj -- build-assets --manifest striv/tests/fixtures/assets/shader_manifest/assets.toml --output /tmp/striv-asset-tool-smoke-m5d-quiet --quiet` | 0 | none | Pass | No |
| `dotnet run --project striv/projects/StriV.AssetTool/StriV.AssetTool.csproj -- build-assets --manifest striv/tests/fixtures/assets/invalid_manifest/assets.toml --output /tmp/striv-asset-tool-invalid-m5d --diagnostics jsonl` | 1 | `AM201` / `AM202` fatal diagnostics | Pass | No |

## 6) Limitations
- Shader assets only.
- No watch mode.
- No incremental cache.
- No runtime/editor integration.
- No material/texture/scene support.
- CLI intentionally minimal.

## 7) Recommended next task
**Asset CLI closeout**: finalize small CLI behavior/docs consistency and lock regression coverage before moving to runtime/editor integration workstreams.
