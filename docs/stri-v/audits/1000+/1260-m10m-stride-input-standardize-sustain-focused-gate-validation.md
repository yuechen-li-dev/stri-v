# M10m Stride.Input Standardize/Sustain + focused warning gate harness repair validation

## 1) Files changed
- `striv/build/striv-check-focused-projects.sh`
- `striv/tests/StriV.CleanGraph.Tests/CleanGraphSmokeTests.cs`
- `docs/stri-v/building-core.md`
- `docs/stri-v/audits/1000+/1260-m10m-stride-input-standardize-sustain-focused-gate-validation.md`

## 2) Problem statement
Focused warning lane tests in `StriV.CleanGraph.Tests` were invoking `bash striv-check-focused-project.sh <Project>` inside `dotnet test`, which recursively triggers `dotnet build` during the test run. This is a nested-build pattern and can deadlock/timeout via `obj/` and `bin/` artifact contention between parent test build and child focused build sessions. This is not a simple slowness-only issue.

## 3) Design decision
Focused warning verification is a build-quality gate, not runtime behavior validation. It is now treated as a script/build artifact outside unit tests. Unit tests no longer spawn focused warning builds.

## 4) Focused warning scripts
Added `striv/build/striv-check-focused-projects.sh` to run multiple focused projects in one build-level gate.

Behavior:
- resolves repo root from script location;
- invokes `striv-check-focused-project.sh <Project>` for each project argument;
- collects structured records into `striv/artifacts/logs/focused-warning-summary.jsonl`;
- record fields: `project`, `exitCode`, `warningCount`, `status`, `logPath`;
- prints concise table for each project;
- preserves per-project focused logs produced by child checker;
- exits nonzero (`1`) if any project reports warnings/build failure.

Status mapping:
- `pass`: exit code 0;
- `warnings`: nonzero exit and parsed warning count > 0;
- `build-failed`: nonzero exit and warning count not > 0.

## 5) CleanGraph test changes
Removed focused warning lane tests that spawned child builds:
- `FocusedWarningLane_BepuPhysics_HasZeroWarnings`
- `FocusedWarningLane_CoreMathematics_HasZeroWarnings`
- `FocusedWarningLane_CoreIO_HasZeroWarnings`

Also removed helper process orchestration methods used only for those tests. Remaining tests are runtime/reference smoke assertions only.

Why this avoids deadlock:
- `dotnet test` no longer launches nested `dotnet build` focused gates;
- focused gates run as standalone scripts in separate build phases.

## 6) Stride.Input sustain status
`./striv/build/striv-check-focused-project.sh Stride.Input` returned zero focused warnings and exit code 0.

Completed active focused zero-warning projects:
- `Stride.BepuPhysics`
- `Stride.Core.Mathematics`
- `Stride.Core.IO`
- `Stride.Input`

Legacy bridge exception (documented separately):
- `Stride.FreeImage` (policy exception, nullable disabled)

## 7) Documentation update
Updated focused warning lane guidance in `docs/stri-v/building-core.md` to:
- include `Stride.Input` as completed focused zero-warning active project;
- add batch gate usage for `striv-check-focused-projects.sh`;
- state focused warning checks are build gates that must run outside `dotnet test`;
- retain FreeImage as legacy bridge exception.

## 8) Validation results
1. Command: `./striv/build/striv-check-focused-project.sh Stride.Input`
   - Exit code: `0`
   - First meaningful warning/error: none
   - Pass/fail: PASS
   - Output truncated: no

2. Command: `./striv/build/striv-check-focused-projects.sh Stride.BepuPhysics Stride.Core.Mathematics Stride.Core.IO Stride.Input`
   - Exit code: `0`
   - First meaningful warning/error: none
   - Pass/fail: PASS
   - Output truncated: no

3. Command: `dotnet test striv/tests/Stride.Input.Tests/Stride.Input.Tests.csproj -v minimal`
   - Exit code: `0`
   - First meaningful warning/error: none
   - Pass/fail: PASS
   - Output truncated: no

4. Command: `dotnet test striv/tests/StriV.CleanGraph.Tests/StriV.CleanGraph.Tests.csproj -v minimal`
   - Exit code: `0`
   - First meaningful warning/error: `/workspace/stri-v/striv/projects/Stride.Rendering/Properties/AssemblyInfo.cs(...): warning CS0436 ...`
   - Pass/fail: PASS
   - Output truncated: yes (command output exceeded terminal capture limit)

5. Command: `dotnet test striv/tests/StriV.AssetTool.Tests/StriV.AssetTool.Tests.csproj -v minimal`
   - Exit code: `0`
   - First meaningful warning/error: `/workspace/stri-v/striv/projects/StriV.AssetPipeline/AssetPipeline.cs(72,26): warning CS8604 ...`
   - Pass/fail: PASS
   - Output truncated: no

6. Command: `dotnet test striv/tests/StriV.AssetPipeline.Tests/StriV.AssetPipeline.Tests.csproj --no-build -v minimal`
   - Exit code: `0`
   - First meaningful warning/error: none
   - Pass/fail: PASS
   - Output truncated: no

7. Command: `dotnet test striv/tests/StriV.ShaderPipeline.Tests/StriV.ShaderPipeline.Tests.csproj --no-build -v minimal`
   - Exit code: `0`
   - First meaningful warning/error: none
   - Pass/fail: PASS
   - Output truncated: no

8. Command: `./striv/build/striv-build-core.sh`
   - Exit code: `0`
   - First meaningful warning/error: `/workspace/stri-v/sources/core/Stride.Core/Storage/ObjectIdBuilder.cs(334,10): warning CS1030 ...`
   - Pass/fail: PASS
   - Output truncated: yes (command output exceeded terminal capture limit)

## 9) Deferred work
- SDL runtime validation.
- Windows RawInput runtime validation.
- controller/XInput policy.
- deletion of `Obsolete/WindowsControllers`.
- next project 5S.

## 10) Recommended next task
Recommended next task: Input runtime smoke validation (SDL + RawInput targeted runtime checks) before selecting the next project Sort target, because M10m changed harness architecture and validating runtime behavior confidence is the highest immediate risk reducer.
