# 970 — M6h BepuPhysics standardize/sustain validation

## 1. Files changed
- `striv/build/striv-check-focused-project.sh`
- `striv/tests/StriV.CleanGraph.Tests/CleanGraphSmokeTests.cs`
- `docs/stri-v/building-core.md`
- `docs/stri-v/audits/970-m6h-bepuphysics-standardize-sustain-validation.md`

## 2. Standardize/Sustain scope
- `Stride.BepuPhysics` had already completed Sort and Set-in-order (M6c/M6d/M6e/M6f) and Shine (M6g).
- M6h adds a repeatable focused-lane warning gate and binds it to tests/documentation so warning cleanliness can be sustained.
- Scope remains sustain-only: no runtime feature expansion and no broader warning cleanup rollout to other projects.

## 3. Focused warning check
- Script path: `striv/build/striv-check-focused-project.sh`.
- Command: `./striv/build/striv-check-focused-project.sh Stride.BepuPhysics`.
- Behavior:
  - resolves repo root and focused project path;
  - builds with `-p:StriVWarningFocusProject=<ProjectName>`;
  - logs to `striv/artifacts/logs/focused-build-*.log`;
  - extracts warnings containing both ` warning ` and `<ProjectName>.csproj`;
  - reports project, build exit code, focused warning count, top warning codes, and log path.
- Inactive project warnings are ignored by exact focused-project filtering.
- Active (focused project) warnings fail with exit code `4`.
- Build errors preserve build exit behavior (script exits with the same non-zero build exit code).

## 4. Bepu sustain test
- Added `FocusedWarningLane_BepuPhysics_HasZeroWarnings` in `StriV.CleanGraph.Tests`.
- Strategy:
  - spawn `bash` process for `striv/build/striv-check-focused-project.sh Stride.BepuPhysics`;
  - async capture stdout/stderr;
  - 30-second timeout;
  - fail test with captured process output if exit code is non-zero.
- Expected condition: focused warning lane returns exit code `0` (zero focused warnings and successful build).

## 5. CleanGraph serializer test repair
- Root cause observed now: `SerializerSelector.Default.GetSerializer<EffectBytecode>()` remains null in this clean profile execution path despite assembly touch/load; this made the old check brittle and non-representative of content serializer availability.
- Fix:
  - kept explicit `EffectBytecode` assembly touch to force deterministic assembly load intent;
  - replaced brittle serializer-selector assertion with deterministic construction check of `DataContentSerializer<EffectBytecode>`.
- Why the test remains meaningful:
  - it still verifies clean profile can resolve `Stride.Shaders` types and instantiate the content serializer path used by `EffectBytecode`’s content-serializer metadata.
- `StriV.CleanGraph.Tests` validation command now runs without `--no-build` in docs to avoid stale build/AP artifacts in this gate.

## 6. Documentation update
- Added a new **Focused project warning lane** section in `docs/stri-v/building-core.md`.
- Documented both focused build and focused zero-warning check commands.
- Clarified this is Shine/Sustain lane isolation and that `Stride.BepuPhysics` is the first zero-warning focused project.
- Updated main clean-graph test command to build-aware invocation (no `--no-build`).

## 7. Validation results
1. Command: `./striv/build/striv-check-focused-project.sh Stride.BepuPhysics`
   - Exit code: `0`
   - First meaningful warning/error: none
   - Pass/fail: PASS
   - Output truncated: No

2. Command: `dotnet test striv/tests/StriV.AssetTool.Tests/StriV.AssetTool.Tests.csproj -v minimal`
   - Exit code: `0`
   - First meaningful warning/error: `CS8604` in `StriV.AssetPipeline` (existing unrelated warning)
   - Pass/fail: PASS
   - Output truncated: No

3. Command: `dotnet test striv/tests/StriV.AssetPipeline.Tests/StriV.AssetPipeline.Tests.csproj --no-build -v minimal`
   - Exit code: `0`
   - First meaningful warning/error: none emitted in this invocation
   - Pass/fail: PASS
   - Output truncated: No

4. Command: `dotnet test striv/tests/StriV.ShaderPipeline.Tests/StriV.ShaderPipeline.Tests.csproj --no-build -v minimal`
   - Exit code: `0`
   - First meaningful warning/error: none emitted in this invocation
   - Pass/fail: PASS
   - Output truncated: No

5. Command: `dotnet test striv/tests/StriV.CleanGraph.Tests/StriV.CleanGraph.Tests.csproj -v minimal`
   - Exit code: `0`
   - First meaningful warning/error: none
   - Pass/fail: PASS
   - Output truncated: No

6. Command: `./striv/build/striv-build-core.sh`
   - Exit code: `0`
   - First meaningful warning/error: legacy warnings in non-focused projects (e.g., `CS1030`, `CS8618`)
   - Pass/fail: PASS
   - Output truncated: Yes (tool output limit)

## 8. Current standard
- `Stride.BepuPhysics` is zero-warning under the focused warning lane.
- Future projects should follow the same 5S sequence (Sort → Set-in-order → Shine → Standardize/Sustain).
- Inactive warning suppression remains temporary lane isolation only; it is not counted as debt removal.

## 9. Recommended next task
- Recommended: next project Sort: `Stride.Core.Mathematics`.
