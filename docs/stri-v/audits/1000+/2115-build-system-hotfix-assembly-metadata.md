# 2115 — Build-system hotfix: assembly metadata/signing audit

## 1) Files changed
- `striv/Directory.Build.props` (new): explicitly disables assembly signing/public signing for all Stri-V projects.
- `docs/stri-v/audits/1000+/2115-build-system-hotfix-assembly-metadata.md` (this report).

## 2) Task scope
- Scope executed: build-system hotfix and metadata/signing audit only.
- Explicitly not touched: runtime/engine behavior, nullability cleanup, feature work.
- Friend access (`InternalsVisibleTo`) was not deleted in this pass.

## 3) Failure reproduction
### Command-line reproduction status
- `dotnet clean striv/StriV.Core.slnx -c Debug -v minimal` -> exit 0.
- `dotnet build striv/StriV.Core.slnx -c Debug -v minimal` -> exit 0.
- No `CS0006` entries were present in `/tmp/striv-build-system-slnx.log` via grep.

### First upstream non-CS0006 error
- Not found in CLI build logs because there were no build errors.
- Result: current CLI environment does **not** reproduce the Visual Studio `CS0006` cascade.

### Interpretation
- Evidence indicates `CS0006` is likely VS-only cascade behavior here (design-time build state / stale `obj/ref` / configuration mismatch), not a deterministic compiler failure in this repo state.

## 4) Assembly metadata audit
### Inventory
- Assembly metadata and build plumbing are heavily inherited from legacy Stride conventions:
  - many per-project `Properties/AssemblyInfo.cs` files under `striv/projects/*` and broader legacy tree under `sources/*`.
  - shared version/signing indirection in `sources/shared/SharedAssemblyInfo.cs`.

### InternalsVisibleTo usage
- `InternalsVisibleTo` is widely used in Stri-V project assembly info files and frequently concatenates `Stride.PublicKeys.Default`.
- Test projects still rely on internal access (validated by passing `Stride.Engine.Tests` and `StriV.Engine.Dominatus.Tests` runs).

### Signing/public key usage
- `sources/shared/SharedAssemblyInfo.cs` defines:
  - assembly version attributes,
  - `PublicKeys.Default` conditional on `STRIDE_SIGNED`.
- Many Stri-V assembly info files include `#pragma warning disable 436` due to duplicate `Stride.PublicKeys` visibility.

### Generated assembly info
- `striv/build/StriV.Core.Profile.props` already has `GenerateAssemblyInfo=false`, so legacy/manual assembly attributes are expected in this graph.

## 5) Root cause (current evidence)
- No root compile error was reproducible by CLI in this environment.
- Therefore no deterministic compile root-cause for `CS0006` was observable in command-line execution.
- Most likely current issue is VS state/configuration drift rather than an always-failing compiler path.

## 6) Fix applied (smallest safe simplification)
- Added `striv/Directory.Build.props` with:
  - `<SignAssembly>false</SignAssembly>`
  - `<PublicSign>false</PublicSign>`
  - `<DelaySign>false</DelaySign>`

### Why safe
- Does not alter runtime code or project/package graph.
- Preserves all existing `InternalsVisibleTo` declarations.
- Reduces potential VS/CLI divergence from accidental signing expectations.

### Intentionally left alone
- No bulk rewrite/removal of `AssemblyInfo.cs` files.
- No removal of `Stride.PublicKeys.Default` from friend assemblies yet.
- No nullability/warning cleanup.

## 7) Validation results
| Command | Exit | First meaningful warning/error | Pass/Fail | Output truncated |
|---|---:|---|---|---|
| `dotnet clean striv/StriV.Core.slnx -c Debug -v minimal` | 0 | none | pass | no |
| `dotnet build striv/StriV.Core.slnx -c Debug -v minimal` | 0 | warnings only (no errors) | pass | no |
| `grep -n "error \|CS0006\|MSB[0-9][0-9][0-9][0-9]\|NETSDK\|CSC" /tmp/striv-build-system-slnx.log \| head -n 200` | 0 | no matches | pass | no |
| `dotnet build striv/projects/Stride.Engine/Stride.Engine.csproj -c Debug -v minimal` | 0 | warnings only (no errors) | pass | yes (console capture truncated) |
| `dotnet build striv/projects/Stride.Rendering/Stride.Rendering.csproj -c Debug -v minimal` | 0 | none | pass | no |
| `dotnet build striv/projects/Stride.BepuPhysics/Stride.BepuPhysics.csproj -c Debug -v minimal` | 0 | none | pass | no |
| `dotnet test striv/tests/Stride.Engine.Tests/Stride.Engine.Tests.csproj -v minimal` | 0 | none | pass | no |
| `dotnet test striv/tests/StriV.Engine.Dominatus.Tests/StriV.Engine.Dominatus.Tests.csproj -v minimal` | 0 | none | pass | no |
| `dotnet build striv/StriV.Core.slnx -c Debug -v minimal 2>&1 \| tee /tmp/striv-build-system-after.log` | 0 | warnings only (no errors) | pass | yes (console capture truncated) |
| `grep -n "error \|CS0006\|MSB[0-9][0-9][0-9][0-9]\|NETSDK\|CSC" /tmp/striv-build-system-after.log \| head -n 200` | 0 | no matches | pass | no |

## 8) Remaining cleanup recommendations
1. **Friend assembly simplification (staged):** convert `InternalsVisibleTo("X" + Stride.PublicKeys.Default)` to plain `InternalsVisibleTo("X")` for active Stri-V projects, validated per project with tests.
2. **Pragma cleanup:** remove `#pragma warning disable 436` where no longer needed after step 1.
3. **Metadata centralization:** gradually migrate non-friend assembly metadata to SDK properties where possible.
4. **Scope discipline:** perform project-by-project cleanup to avoid graph-wide regressions.
5. **VS reproducibility pass:** if `CS0006` reappears only in VS, capture a binlog from VS design-time build and compare Configuration/TFM/platform with CLI.
