# M10e — Stride.Input Shine pass 2 or Sustain validation

## 1) Files changed
- `striv/tests/StriV.CleanGraph.Tests/CleanGraphSmokeTests.cs`
- `docs/stri-v/building-core.md`
- `docs/stri-v/audits/1000+/1170-m10e-stride-input-shine2-or-sustain-validation.md`

## 2) Task decision
**Branch taken:** Standardize/Sustain.

Rationale: focused warning lane check for `Stride.Input` passed (`Focused warning count: 0`), so no warning-fix churn was invented.

## 3) Re-baseline results
Exact command sequence:

```bash
dotnet build striv/projects/Stride.Input/Stride.Input.csproj -c Debug -p:StriVWarningFocusProject=Stride.Input 2>&1 | tee /tmp/striv-m10e-input-baseline.log
grep -E "warning (CS|CA|NU|STRIDE)[0-9]+" /tmp/striv-m10e-input-baseline.log | grep "Stride.Input" > /tmp/striv-m10e-input-warning-lines.log || true
wc -l /tmp/striv-m10e-input-warning-lines.log
sed -E 's/.*warning ((CS|CA|NU|STRIDE)[0-9]+).*/\1/' /tmp/striv-m10e-input-warning-lines.log | sort | uniq -c | sort -nr
./striv/build/striv-check-focused-project.sh Stride.Input
```

Results:
- Grep-based warning line count: `146`
- Grep-based code distribution:
  - `CS8618`: 66
  - `CS8602`: 22
  - `CS8601`: 22
  - `CS8604`: 14
  - `CS8600`: 12
  - `CS8603`: 6
  - `CS8625`: 4
- Focused check result (`striv-check-focused-project.sh`): **pass**, `Focused warning count: 0`.

Interpretation applied for M10e decision: focused lane gate is authoritative for Sustain eligibility; Sustain branch chosen.

## 4) If Shine pass 2 happened
Not applicable (Sustain branch selected).

## 5) If Standardize/Sustain happened
- Sustain test name: `FocusedWarningLane_Input_HasZeroWarnings`
- Script invoked by sustain test: `striv/build/striv-check-focused-project.sh Stride.Input`
- Docs updated:
  - Added `Stride.Input` focused check command.
  - Added `Stride.Input` to completed zero-warning focused projects list.

## 6) Tests
- Added/updated `StriV.CleanGraph.Tests` focused lane sustain coverage by adding `FocusedWarningLane_Input_HasZeroWarnings`, which locks that `Stride.Input` remains warning-clean under the focused lane checker.

## 7) Validation results
1. Command: `dotnet test striv/tests/Stride.Input.Tests/Stride.Input.Tests.csproj -v minimal`
   - Exit code: 0
   - First meaningful warning/error: none
   - Pass/Fail: Pass
   - Output truncated: No

2. Command: `dotnet build striv/StriV.Core.slnx -c Debug -p:StriVWarningFocusProject=Stride.Input`
   - Exit code: 0
   - First meaningful warning/error: none
   - Pass/Fail: Pass
   - Output truncated: No

3. Command: `dotnet test striv/tests/StriV.AssetTool.Tests/StriV.AssetTool.Tests.csproj -v minimal`
   - Exit code: 0
   - First meaningful warning/error: none
   - Pass/Fail: Pass
   - Output truncated: No

4. Command: `dotnet test striv/tests/StriV.AssetPipeline.Tests/StriV.AssetPipeline.Tests.csproj --no-build -v minimal`
   - Exit code: 0
   - First meaningful warning/error: none
   - Pass/Fail: Pass
   - Output truncated: No

5. Command: `dotnet test striv/tests/StriV.ShaderPipeline.Tests/StriV.ShaderPipeline.Tests.csproj --no-build -v minimal`
   - Exit code: 0
   - First meaningful warning/error: one expected skipped test (`StreamLiveness_DoesNotPruneWhenAccessUnknown`)
   - Pass/Fail: Pass
   - Output truncated: No

6. Command: `dotnet test striv/tests/StriV.CleanGraph.Tests/StriV.CleanGraph.Tests.csproj -v minimal`
   - Exit code: 0
   - First meaningful warning/error: none
   - Pass/Fail: Pass
   - Output truncated: No

7. Command: `./striv/build/striv-build-core.sh`
   - Exit code: 0
   - First meaningful warning/error: none
   - Pass/Fail: Pass
   - Output truncated: No

## 8) Current status
`Stride.Input` is currently zero-warning under the focused lane checker.

Completed focused projects:
- `Stride.BepuPhysics`
- `Stride.Core.Mathematics`
- `Stride.Core.IO`
- `Stride.Input`

## 9) Deferred work
- SDL runtime validation
- Windows RawInput runtime validation
- gesture/event deeper tests
- controller support/XInput policy
- deletion of `Obsolete/WindowsControllers` if still pending

## 10) Recommended next task
**Recommended next task:** Input runtime smoke validation (SDL + simulated source lifecycle) to convert focused static cleanliness into runtime confidence while preserving current backend policy boundaries.
