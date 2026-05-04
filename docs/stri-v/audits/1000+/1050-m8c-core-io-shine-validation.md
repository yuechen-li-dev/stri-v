# M8c — Stride.Core.IO Shine validation

## 1) Files changed
- `sources/core/Stride.Core.IO/Providers/DriveFileProvider.cs`
- `sources/core/Stride.Core.IO/Providers/VirtualFileProviderBase.cs`
- `sources/core/Stride.Core.IO/VirtualFileStream.cs`
- `sources/core/Stride.Core.IO/VirtualFileSystem/VirtualFileSystem.cs`
- `sources/core/Stride.Core.IO/Watching/DirectoryWatcher.Desktop.cs`
- `docs/stri-v/audits/1000+/1050-m8c-core-io-shine-validation.md`

## 2) 5S phase
M8c is **Shine** for `Stride.Core.IO`: warning cleanup and local hygiene only.

- M8a (Sort) and M8b (Set in order) are treated as complete inputs.
- This pass intentionally avoids VFS architecture redesign.
- Larger VFS lifecycle/static-init cleanup remains deferred to future phase work.

## 3) Before warnings
Command:
```bash
dotnet build striv/projects/Stride.Core.IO/Stride.Core.IO.csproj -c Debug -p:StriVWarningFocusProject=Stride.Core.IO 2>&1 | tee /tmp/striv-m8c-coreio-before.log
grep -E "warning (CS|CA|NU|STRIDE)[0-9]+" /tmp/striv-m8c-coreio-before.log | grep "Stride.Core.IO" > /tmp/striv-m8c-coreio-warning-lines-before.log || true
wc -l /tmp/striv-m8c-coreio-warning-lines-before.log
sed -E 's/.*warning ((CS|CA|NU|STRIDE)[0-9]+).*/\1/' /tmp/striv-m8c-coreio-warning-lines-before.log | sort | uniq -c | sort -nr
```

Focused warning count before: **22 lines** (includes duplicated compiler summary lines in log collection).

Warning codes before:
- CS8625: 6
- CS8618: 6
- CS8604: 6
- CS8602: 2
- CS8600: 2

Representative sites:
- `Providers/DriveFileProvider.cs(32,71)` CS8604
- `Watching/DirectoryWatcher.Desktop.cs(127,20)` CS8600
- `Watching/DirectoryWatcher.Desktop.cs(307,54)` CS8604
- `VirtualFileSystem/VirtualFileSystem.cs(88,12)` CS8618
- `VirtualFileStream.cs(38,12)` CS8618

## 4) Fixes applied
### `Providers/DriveFileProvider.cs`
- Warning: CS8604 on `ResolveProvider(RootPath, ...)` where `RootPath` is nullable by base contract.
- Fix: used `RootPath!` in this call site.
- Behavior: unchanged; `DriveFileProvider` constructor always supplies non-null root path.
- Guardrail: no provider resolution logic changes.

### `Providers/VirtualFileProviderBase.cs`
- Warning: CS8625 assigning null to non-null `out string`.
- Fix: initialize `filePath` to `string.Empty` on false-return path.
- Behavior: unchanged; method still returns `false` and signals no location.
- Guardrail: no path semantics change.

### `VirtualFileStream.cs`
- Warnings: CS8618/CS8625 around disposed-state fields.
- Fixes:
  - `virtualFileStream` annotated nullable.
  - `InternalStream = null!;` in dispose for lifecycle teardown.
- Behavior: unchanged stream ownership/disposal behavior.
- Guardrail: no stream routing or bounds logic changed.

### `Watching/DirectoryWatcher.Desktop.cs`
- Warnings: CS8600/CS8602/CS8604 around nullable path/name/rename args.
- Fixes:
  - Used local `string? directoryPath` flow for `Path.GetDirectoryName` result.
  - Pattern-matched `RenamedEventArgs` before use.
  - Used `e.Name ?? string.Empty` when creating file events.
- Behavior: unchanged event routing; only null-safe construction.
- Guardrail: no watcher tree, tracking, or path normalization behavior changed.

### `VirtualFileSystem/VirtualFileSystem.cs`
- Warnings: CS8618 for deferred static fields.
- Fixes:
  - `ApplicationObjectDatabase = null!;`
  - `ApplicationDatabase = null!;`
- Behavior: unchanged lifecycle assumptions; explicit null-forgiving assignment documents deferred initialization model.
- Guardrail: static initialization order untouched.

## 5) VFS refactor deferral
Deferred explicitly (not Shine scope):
- VirtualFileSystem static initialization refactor.
- `ApplicationObjectDatabase` explicit lifecycle/ownership redesign.
- Migration to `System.IO.Abstractions`.

Reason: each is architectural and cross-cutting; M8c targets focused warning cleanup only.

## 6) Tests
No new tests added.

Rationale: all edits are nullability/lifecycle annotations or null-safe equivalent control-flow with no intended behavior change.

## 7) After warnings
Command set executed per plan (same as before plus focused check script).

- Focused warning count after: **0**
- `./striv/build/striv-check-focused-project.sh Stride.Core.IO`: **pass**
- `Stride.Core.IO` is zero-warning under focused warning lane.

## 8) Validation results
| Command | Exit | First meaningful warning/error | Pass/Fail | Output truncated |
|---|---:|---|---|---|
| `dotnet build striv/projects/Stride.Core.IO/Stride.Core.IO.csproj -c Debug -p:StriVWarningFocusProject=Stride.Core.IO 2>&1 | tee /tmp/striv-m8c-coreio-before.log` | 0 | `VirtualFileStream.cs(38,12) warning CS8618` | Pass | No |
| `grep -E "warning (CS|CA|NU|STRIDE)[0-9]+" /tmp/striv-m8c-coreio-before.log | grep "Stride.Core.IO" > /tmp/striv-m8c-coreio-warning-lines-before.log || true` | 0 | n/a | Pass | No |
| `wc -l /tmp/striv-m8c-coreio-warning-lines-before.log` | 0 | n/a | Pass | No |
| `sed -E 's/.*warning ((CS|CA|NU|STRIDE)[0-9]+).*/\1/' /tmp/striv-m8c-coreio-warning-lines-before.log | sort | uniq -c | sort -nr` | 0 | n/a | Pass | No |
| `dotnet build striv/projects/Stride.Core.IO/Stride.Core.IO.csproj -c Debug -p:StriVWarningFocusProject=Stride.Core.IO 2>&1 | tee /tmp/striv-m8c-coreio-after.log` | 0 | none | Pass | No |
| `grep -E "warning (CS|CA|NU|STRIDE)[0-9]+" /tmp/striv-m8c-coreio-after.log | grep "Stride.Core.IO" > /tmp/striv-m8c-coreio-warning-lines-after.log || true` | 0 | n/a | Pass | No |
| `wc -l /tmp/striv-m8c-coreio-warning-lines-after.log` | 0 | n/a | Pass | No |
| `sed -E 's/.*warning ((CS|CA|NU|STRIDE)[0-9]+).*/\1/' /tmp/striv-m8c-coreio-warning-lines-after.log | sort | uniq -c | sort -nr` | 0 | n/a | Pass | No |
| `./striv/build/striv-check-focused-project.sh Stride.Core.IO` | 0 | none | Pass | No |
| `dotnet test striv/tests/StriV.AssetTool.Tests/StriV.AssetTool.Tests.csproj -v minimal` | 0 | `StriV.AssetPipeline/AssetPipeline.cs(72,26) warning CS8604` | Pass | No |
| `dotnet test striv/tests/StriV.AssetPipeline.Tests/StriV.AssetPipeline.Tests.csproj --no-build -v minimal` | 0 | none | Pass | No |
| `dotnet test striv/tests/StriV.ShaderPipeline.Tests/StriV.ShaderPipeline.Tests.csproj --no-build -v minimal` | 0 | none | Pass | No |
| `dotnet test striv/tests/StriV.CleanGraph.Tests/StriV.CleanGraph.Tests.csproj -v minimal` | 0 | upstream warnings outside target project | Pass | Yes |
| `./striv/build/striv-build-core.sh` | 0 | upstream warnings outside target project | Pass | Yes |

## 9) Standard/Sustain recommendation
Recommended next phase: **M8d Standardize/Sustain for `Stride.Core.IO`**.

No blocker found for moving to M8d on this project.

## 10) Recommended next task
**M8d Standardize/Sustain for `Stride.Core.IO`**.
