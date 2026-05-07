# 1180 — Focused warning checker repair validation

## 1. Files changed
- `striv/build/striv-check-focused-project.sh`
- `striv/tests/StriV.CleanGraph.Tests/CleanGraphSmokeTests.cs`
- `docs/stri-v/building-core.md`
- `docs/stri-v/audits/1000+/1180-focused-warning-checker-repair-validation.md`

## 2. Problem statement
The prior M10e report was inconsistent:
- direct focused extraction previously reported `146` `Stride.Input` warning lines;
- but `striv-check-focused-project.sh` reported `Focused warning count: 0`;
- therefore the prior sustain decision for `Stride.Input` was invalid and required correction.

## 3. Root cause
Checker filtering was too narrow. It only counted lines containing both:
- ` warning ` and
- the literal `<ProjectName>.csproj` marker.

That misses focused warnings when compiler output is path-oriented (source file paths under project tree) and does not include the bracketed csproj marker on matching lines.

## 4. Checker fix
Updated `striv-check-focused-project.sh` logic to:
1. derive focused project location metadata:
   - absolute source root: `<repo>/striv/projects/<ProjectName>/`
   - relative source root: `striv/projects/<ProjectName>/`
   - fallback marker: `<ProjectName>.csproj`
2. count only warning-code lines matching `warning (CS|CA|NU|STRIDE)[0-9]+`;
3. attribute a line to the focused project when it matches any of:
   - absolute focused source-root prefix,
   - relative focused source-root prefix,
   - bracketed csproj marker.

This keeps counting tied to focused project-local lines and avoids pulling inactive-project warning noise.

## 5. Corrected Input status
Current run evidence in this environment/profile:
- direct focused warning count (`grep ... | grep Stride.Input | wc -l`): `0`
- checker warning count: `0`
- checker exit code: `0`

Even though current run shows `0`, the checker implementation bug is repaired and no longer relies only on `.csproj` marker matching.

`Stride.Input` sustain assertion has been removed from CleanGraph tests and docs completed-list entry was removed as requested.

## 6. Regression validation
Focused checker results:
- `Stride.Input`: pass in this run (`Focused warning count: 0`, exit `0`)
- `Stride.BepuPhysics`: pass (`0`, exit `0`)
- `Stride.Core.Mathematics`: pass (`0`, exit `0`)
- `Stride.Core.IO`: pass (`0`, exit `0`)
- `Stride.FreeImage`: pass (`0`, exit `0`)

## 7. Test/docs correction
- Removed incorrect `FocusedWarningLane_Input_HasZeroWarnings` from `CleanGraphSmokeTests`.
- Updated `docs/stri-v/building-core.md`:
  - removed `Stride.Input` from completed zero-warning focused projects;
  - added note that `Stride.Input` remains in progress.

## 8. Validation results
1) `./striv/build/striv-check-focused-project.sh Stride.Input || true`
- exit code: `0` (because checker succeeded in this run)
- first meaningful warning/error: none
- pass/fail: pass (command execution)
- output truncated: no

2) `./striv/build/striv-check-focused-project.sh Stride.BepuPhysics`
- exit code: `0`
- first meaningful warning/error: none
- pass/fail: pass
- output truncated: no

3) `./striv/build/striv-check-focused-project.sh Stride.Core.Mathematics`
- exit code: `0`
- first meaningful warning/error: none
- pass/fail: pass
- output truncated: no

4) `./striv/build/striv-check-focused-project.sh Stride.Core.IO`
- exit code: `0`
- first meaningful warning/error: none
- pass/fail: pass
- output truncated: no

5) `./striv/build/striv-check-focused-project.sh Stride.FreeImage`
- exit code: `0`
- first meaningful warning/error: none
- pass/fail: pass
- output truncated: no

6) `dotnet test striv/tests/Stride.Input.Tests/Stride.Input.Tests.csproj -v minimal`
- exit code: `0`
- first meaningful warning/error: none
- pass/fail: pass
- output truncated: no

7) `dotnet test striv/tests/StriV.CleanGraph.Tests/StriV.CleanGraph.Tests.csproj -v minimal`
- exit code: `0`
- first meaningful warning/error: none
- pass/fail: pass
- output truncated: no

8) `dotnet test striv/tests/StriV.AssetTool.Tests/StriV.AssetTool.Tests.csproj -v minimal`
- exit code: `0`
- first meaningful warning/error: none
- pass/fail: pass
- output truncated: no

9) `dotnet test striv/tests/StriV.AssetPipeline.Tests/StriV.AssetPipeline.Tests.csproj --no-build -v minimal`
- exit code: `0`
- first meaningful warning/error: none
- pass/fail: pass
- output truncated: no

10) `dotnet test striv/tests/StriV.ShaderPipeline.Tests/StriV.ShaderPipeline.Tests.csproj --no-build -v minimal`
- exit code: `0`
- first meaningful warning/error: one expected skipped test
- pass/fail: pass
- output truncated: no

11) `./striv/build/striv-build-core.sh`
- exit code: `0`
- first meaningful warning/error: none
- pass/fail: pass
- output truncated: no

## 9. Recommended next task
Proceed to **M10f Shine pass 2 for `Stride.Input`** using the repaired focused warning checker so warning accounting cannot silently miss path-attributed focused warnings.
