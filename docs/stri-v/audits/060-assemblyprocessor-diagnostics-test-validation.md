# 060 - AssemblyProcessor diagnostics test validation (M1b)

## 1) Files changed
- Added `tests/Stride.Core.AssemblyProcessor.Diagnostics.Tests/Stride.Core.AssemblyProcessor.Diagnostics.Tests.csproj`.
- Added `tests/Stride.Core.AssemblyProcessor.Diagnostics.Tests/AssemblyProcessorDiagnosticsTests.cs`.
- Added `build/StriV.AssemblyProcessor.Diagnostics.M1b.slnf`.
- Added this report file.
- Updated `build/Stride.sln` only to include the new test project (required so the `.slnf` can reference a solution project).

## 2) Test project design
- **Path:** `tests/Stride.Core.AssemblyProcessor.Diagnostics.Tests`.
- **TFM:** `net10.0`.
- **Test framework:** xUnit (`xunit`, `xunit.runner.visualstudio`, `Microsoft.NET.Test.Sdk`).
- **References:** package-only, no `ProjectReference` to avoid broad graph coupling and circular triggering.
- **AssemblyProcessor opt-in:** explicitly disabled with `<StrideAssemblyProcessor>false</StrideAssemblyProcessor>`.
- **Circular failure avoidance:** this project does not require AssemblyProcessor to build; it performs path-based and reflection-based diagnostics against existing payload files.

## 3) AssemblyProcessor source/build-path map
- Source project: `sources/core/Stride.Core.AssemblyProcessor/Stride.Core.AssemblyProcessor.csproj`.
- Build target usage discovered in `sources/sdk/Stride.Build.Sdk/Sdk/Stride.AssemblyProcessor.targets`:
  - task assembly path from `$(StrideRootDir)deps/AssemblyProcessor/netstandard2.0/Stride.Core.AssemblyProcessor.dll`.
  - copied to `/tmp/Stride/AssemblyProcessor/netstandard2.0/<hash>/Stride.Core.AssemblyProcessor.dll` before task load.
- Additional related build flow examined in `sources/targets/Stride.targets` for assembly-processor integration points.
- In this repo state, `deps/AssemblyProcessor/netstandard2.0/Stride.Core.AssemblyProcessor.dll` exists but is a Git LFS pointer text file (131 bytes), not a managed PE file.

## 4) Validation command results
1. `dotnet restore build/StriV.AssemblyProcessor.Diagnostics.M1b.slnf`
   - Exit code: `0`
   - Classification: success
   - Note: restored the new diagnostics test project.

2. `dotnet build build/StriV.AssemblyProcessor.Diagnostics.M1b.slnf -c Debug -v minimal`
   - Exit code: `0`
   - Classification: success
   - Note: diagnostics project builds cleanly.

3. `dotnet test build/StriV.AssemblyProcessor.Diagnostics.M1b.slnf -c Debug -v normal`
   - Exit code: `1`
   - Classification: expected diagnostic failure
   - First meaningful failure:
     - `BadImageFormatException: Unknown file format` from `AssemblyName.GetAssemblyName` on `deps/AssemblyProcessor/netstandard2.0/Stride.Core.AssemblyProcessor.dll`.
     - `BadImageFormatException: Bad IL format` when trying to load that same file in `AssemblyLoadContext`.

## 5) Diagnostic test results
- `AssemblyProcessorDependencyPayload_IsPresentOrReportsUsefulMissingPath`: **Pass**.
  - Proves a candidate payload path exists and is inspectable.
- `AssemblyProcessorBinary_HasValidManagedAssemblyMetadata`: **Fail**.
  - Evidence: size `131`, SHA256 `8EBF...450B`, first bytes decode to `version https://git-lfs.github...`, no `MZ` header.
  - Proves candidate payload is not a valid managed assembly.
- `AssemblyProcessorTask_TypeCanBeLocated`: **Fail**.
  - Evidence: `Bad IL format` while loading payload.
  - Proves task type cannot be resolved because binary is invalid as loaded.
- `AssemblyProcessorProgram_CanBeInvokedForHelpOrNoArgs`: **Pass** (controlled error output accepted).
  - Confirms invocation path can execute and produce diagnostic output.

## 6) Bad IL diagnosis
Most likely diagnosis: **wrong file copied / invalid payload artifact**.
- Evidence strongly indicates the `deps/.../Stride.Core.AssemblyProcessor.dll` is a Git LFS pointer rather than the real DLL.
- This explains both `Unknown file format` and `Bad IL format` without requiring dependency-resolution hypotheses.
- Current evidence does **not** indicate a first-order missing dependency issue; loader fails before managed metadata is valid.

## 7) Recommended next action
**Repair deps/AssemblyProcessor payload generation (or retrieval) first**.
- Ensure actual LFS binary content for `deps/AssemblyProcessor/netstandard2.0/Stride.Core.AssemblyProcessor.dll` is present (not pointer text).
- Re-run this diagnostics suite immediately after payload correction to confirm task type load and dependency state.

## 8) Optional fixture processing
Not attempted in this first pass because the primary payload is non-managed and fails before functional invocation. A fixture processing test would not add signal until payload validity is fixed.
