# 1010 — M7d Core.Mathematics standardize/sustain validation

## 1. Files changed
- `striv/tests/StriV.CleanGraph.Tests/CleanGraphSmokeTests.cs`
- `docs/stri-v/building-core.md`
- `docs/stri-v/audits/1000+/1010-m7d-core-mathematics-standardize-sustain-validation.md`

## 2. Audit folder convention
- 1000+ reports now live under `docs/stri-v/audits/1000+/`.
- This prevents ordering weirdness in mixed-width numeric prefixes in the parent audits folder and keeps four-digit reports grouped/scannable.

## 3. Standardize/Sustain scope
- M7a/M7b/M7c completed Sort / Set-in-order / Shine for `Stride.Core.Mathematics`.
- M7d locks and sustains the focused warning gate for the already-cleaned project.
- Scope remains sustain-only; no new cleanup lane expansion.

## 4. Sustain test
- Added test: `FocusedWarningLane_CoreMathematics_HasZeroWarnings`.
- Script invoked by the test: `striv/build/striv-check-focused-project.sh Stride.Core.Mathematics`.
- Process strategy mirrors existing focused-lane behavior:
  - async stdout/stderr capture,
  - shared timeout (`30s`),
  - deterministic nonzero-exit assertion with captured output,
  - timeout failure includes partial captured output.
- Expected condition: focused warning check exits `0` with zero warnings for `Stride.Core.Mathematics`.

## 5. Documentation update
- Updated focused warning lane docs in `docs/stri-v/building-core.md`.
- Added focused check command for `Stride.Core.Mathematics`.
- Updated completed focused zero-warning project list to include:
  - `Stride.BepuPhysics`
  - `Stride.Core.Mathematics`

## 6. Validation results
1. Command: `./striv/build/striv-check-focused-project.sh Stride.Core.Mathematics`
   - Exit code: `0`
   - First meaningful warning/error: none
   - Pass/fail: PASS
   - Output truncated: No

2. Command: `dotnet test striv/tests/StriV.CleanGraph.Tests/StriV.CleanGraph.Tests.csproj -v minimal`
   - Exit code: `0`
   - First meaningful warning/error: `CS8604` in non-focused `Stride.Core.MicroThreading` during build
   - Pass/fail: PASS
   - Output truncated: Yes

3. Command: `dotnet test striv/tests/StriV.AssetTool.Tests/StriV.AssetTool.Tests.csproj -v minimal`
   - Exit code: `0`
   - First meaningful warning/error: `CS8604` in `StriV.AssetPipeline/AssetPipeline.cs(72,26)`
   - Pass/fail: PASS
   - Output truncated: No

4. Command: `dotnet test striv/tests/StriV.AssetPipeline.Tests/StriV.AssetPipeline.Tests.csproj --no-build -v minimal`
   - Exit code: `0`
   - First meaningful warning/error: none
   - Pass/fail: PASS
   - Output truncated: No

5. Command: `dotnet test striv/tests/StriV.ShaderPipeline.Tests/StriV.ShaderPipeline.Tests.csproj --no-build -v minimal`
   - Exit code: `0`
   - First meaningful warning/error: none
   - Pass/fail: PASS
   - Output truncated: No

6. Command: `./striv/build/striv-build-core.sh`
   - Exit code: `0`
   - First meaningful warning/error: `CS1030` in `Stride.Core.AssemblyProcessor` (`ObjectIdBuilder.cs` PERF warning)
   - Pass/fail: PASS
   - Output truncated: Yes

## 7. Current standard
- `Stride.Core.Mathematics` is zero-warning under the focused warning lane.
- Completed focused projects now include:
  - `Stride.BepuPhysics`
  - `Stride.Core.Mathematics`

## 8. Recommended next task
- Recommended next task: **M7e serialization attribute proof/removal for `Stride.Core.Mathematics`**.
- Rationale: M7d now sustains warning cleanliness; M7e can proceed as a separate, deliberate proof/removal lane without mixing scope in sustain work.
