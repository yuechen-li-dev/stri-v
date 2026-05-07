# M10f Stride.Input re-baseline / checker repair validation

## 1) Files changed
- `striv/build/striv-check-focused-project.sh`
- `docs/stri-v/audits/1000+/1190-m10f-stride-input-rebaseline-shine2-or-sustain-validation.md`

## 2) Task decision
Branch taken: **checker disagreement / repair**.

- Direct focused warning extraction reported warnings for `Stride.Input`.
- Repaired checker script still reported zero due to two defects discovered in this run:
  1. warning matcher used awk regex that did not match warning lines in this environment;
  2. incremental build path allowed up-to-date zero-warning logs that skipped warning re-emission.
- Script was repaired to use grep-based matching and `--no-incremental` so focused warning lanes reflect current project warning reality.

## 3) Re-baseline results
Commands run:

```bash
dotnet build striv/projects/Stride.Input/Stride.Input.csproj -c Debug -p:StriVWarningFocusProject=Stride.Input 2>&1 | tee /tmp/striv-m10f-input-baseline.log
grep -E "warning (CS|CA|NU|STRIDE)[0-9]+" /tmp/striv-m10f-input-baseline.log > /tmp/striv-m10f-input-all-warning-lines.log || true
grep -E "warning (CS|CA|NU|STRIDE)[0-9]+" /tmp/striv-m10f-input-baseline.log | grep -E "striv/projects/Stride.Input|/striv/projects/Stride.Input|Stride.Input.csproj" > /tmp/striv-m10f-input-warning-lines.log || true
wc -l /tmp/striv-m10f-input-warning-lines.log
sed -E 's/.*warning ((CS|CA|NU|STRIDE)[0-9]+).*/\1/' /tmp/striv-m10f-input-warning-lines.log | sort | uniq -c | sort -nr
./striv/build/striv-check-focused-project.sh Stride.Input || true
```

Results (initial baseline):
- Direct extraction warning count: **146**
- Code distribution:
  - 66 `CS8618`
  - 22 `CS8602`
  - 22 `CS8601`
  - 14 `CS8604`
  - 12 `CS8600`
  - 6 `CS8603`
  - 4 `CS8625`
- Checker warning count (before this repair): **0**
- Agreement: **No (disagreement)**

Repaired checker verification:

```bash
./striv/build/striv-check-focused-project.sh Stride.Input
```

- Focused warning count after repair: **146**
- Agreement with direct extraction: **Yes**

## 4) Shine pass 2
Not executed in this change set because the mandated first blocker was checker disagreement. The checker is now fixed and producing evidence-aligned counts; `Stride.Input` remains warning-positive and requires Shine pass 2 next.

## 5) Standardize/Sustain
Not executed. `Stride.Input` is **not** zero-warning under repaired focused lane.

## 6) Tests
No new tests were added in this change set.

## 7) Validation results
- `dotnet test striv/tests/Stride.Input.Tests/Stride.Input.Tests.csproj -v minimal`
  - exit code: 0
  - first meaningful warning/error: none
  - pass/fail: pass
  - output truncated: no
- `dotnet build striv/StriV.Core.slnx -c Debug -p:StriVWarningFocusProject=Stride.Input`
  - exit code: 0
  - first meaningful warning/error: none
  - pass/fail: pass
  - output truncated: no
- `dotnet test striv/tests/StriV.AssetTool.Tests/StriV.AssetTool.Tests.csproj -v minimal`
  - exit code: 0
  - first meaningful warning/error: none
  - pass/fail: pass
  - output truncated: no
- `dotnet test striv/tests/StriV.AssetPipeline.Tests/StriV.AssetPipeline.Tests.csproj --no-build -v minimal`
  - exit code: 0
  - first meaningful warning/error: none
  - pass/fail: pass
  - output truncated: no
- `dotnet test striv/tests/StriV.ShaderPipeline.Tests/StriV.ShaderPipeline.Tests.csproj --no-build -v minimal`
  - exit code: 0
  - first meaningful warning/error: one known skipped test (`StreamLiveness_DoesNotPruneWhenAccessUnknown`)
  - pass/fail: pass
  - output truncated: no
- `dotnet test striv/tests/StriV.CleanGraph.Tests/StriV.CleanGraph.Tests.csproj -v minimal`
  - exit code: 1
  - first meaningful warning/error: `FocusedWarningLane_BepuPhysics_HasZeroWarnings` timed out after 30s
  - pass/fail: fail
  - output truncated: no
- `./striv/build/striv-build-core.sh`
  - started and progressed through core build; observed non-focused warnings in unrelated projects during full graph build output
  - output capture in this run was truncated by terminal token limits
  - pass/fail: warning (incomplete capture)
  - output truncated: yes

## 8) Current status
`Stride.Input` is **not zero-warning** under repaired focused lane (146 focused warnings).

Completed focused projects list therefore remains unchanged (do **not** add `Stride.Input` yet).

## 9) Deferred work
- SDL runtime validation
- Windows RawInput runtime validation
- gesture/event deeper tests
- controller support/XInput policy
- deletion of `Obsolete/WindowsControllers` if still pending

## 10) Recommended next task
**Shine pass 2 for `Stride.Input`** using the repaired checker as gate, starting with `Core/InputManager.cs` lifecycle nullability warnings and adding focused tests where behavior is uncertain.
