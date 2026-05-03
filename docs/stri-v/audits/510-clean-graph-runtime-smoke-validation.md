# 510 - Clean graph runtime smoke validation

## 1. Files changed
- `striv/build/striv-run-coresmoke.sh`
- `striv/build/striv-run-coresmoke.ps1`
- `docs/stri-v/building-core.md`
- `docs/stri-v/audits/510-clean-graph-runtime-smoke-validation.md`

## 2. Clean runtime smoke design
- **Script path:** `striv/build/striv-run-coresmoke.sh`.
- **Build-first behavior:** always calls `./striv/build/striv-build-core.sh <Configuration>` before run.
- **Configuration handling:** supports `Debug` (default) and `Release` positional argument.
- **DLL path:** resolves `striv/projects/StriV.CoreSmoke/bin/<Configuration>/net10.0/StriV.CoreSmoke.dll` relative to repo root.
- **Timeout behavior:** uses `timeout 20s` when available; warns and runs directly if not available.
- **Failure classification:** emits explicit timeout classification and hint categories for SDL/X11, Vulkan/device, missing native lib, content/shader/effect, and managed runtime.
- **Xvfb behavior/documentation:** default behavior remains unchanged; doc includes `xvfb-run -a ./striv/build/striv-run-coresmoke.sh` and Release variant.
- **PowerShell parity:** `striv/build/striv-run-coresmoke.ps1` added with build-first flow, 20s timeout, and matching diagnostics.

## 3. Validation results

| Command | Exit code | First meaningful warning/error | Pass/Fail | Output truncated | Classification |
|---|---:|---|---|---|---|
| `./striv/build/striv-run-coresmoke.sh` | `134` | `System.Exception: Cannot allocate SDL Window: x11 not available` | Fail | Yes | environment limitation |
| `xvfb-run -a ./striv/build/striv-run-coresmoke.sh` | `134` | `System.InvalidOperationException: Could not find serializer for type Stride.Shaders.EffectBytecode.` | Fail | No | managed runtime blocker |
| `dotnet test striv/tests/StriV.CleanGraph.Tests/StriV.CleanGraph.Tests.csproj` | `0` | NU1510 package-pruning warnings only | Pass | No | success |

Notes:
- Per instruction, `Release` runtime smoke was **not** run because Debug under Xvfb failed.

## 4. Runtime observations
- Clean CoreSmoke **builds** through the clean graph (build phase in run script succeeds).
- CoreSmoke **launches** but does not complete successfully.
- Default (non-Xvfb) runtime fails with SDL/X11 unavailability (`x11 not available`).
- Xvfb runtime bypasses SDL/X11 blocker but fails with a managed exception in shader bytecode serializer resolution (`Stride.Shaders.EffectBytecode`).
- No explicit Vulkan loader/ICD/device failure was observed in the captured failure path.
- The observed runtime failure is consistent with a content/shader/runtime boundary issue and managed runtime exception.
- No missing native library error was observed in the captured Xvfb failure path.

## 5. Test results
- **Command:** `dotnet test striv/tests/StriV.CleanGraph.Tests/StriV.CleanGraph.Tests.csproj`
- **Result:** Pass (`3` passed, `0` failed).
- **What tests prove:** clean graph structure/profile invariants and exclusion expectations covered by the existing clean graph tests still hold.
- **What tests do not prove:** runtime execution path correctness for CoreSmoke (windowing/device init, shader/effect runtime serializer boundaries, or end-to-end self-exit behavior under headless execution).

## 6. M3 clean graph verdict

| Candidate | Verdict | Current blocker | Next action |
|---|---|---|---|
| clean graph build | Pass | None | Keep as baseline build path. |
| clean graph tests | Pass | None | Keep test gate and extend coverage as needed. |
| clean graph CoreSmoke runtime | Blocked | `Stride.Shaders.EffectBytecode` serializer resolution failure under Xvfb | Perform clean runtime shader/content boundary audit focused on serializer registration/runtime loading path. |

## 7. Worktree status
Command run:

```bash
git status --short
```

Observed at report authoring time:

```text
 M docs/stri-v/building-core.md
?? striv/build/striv-run-coresmoke.ps1
?? striv/build/striv-run-coresmoke.sh
?? docs/stri-v/audits/510-clean-graph-runtime-smoke-validation.md
```

## 8. Recommended next task
Because runtime failed under Xvfb with a managed `EffectBytecode` serializer error, the recommended next task is:

- **Clean runtime shader/content boundary audit** for serializer registration and runtime effect-bytecode deserialization path in clean graph CoreSmoke execution.
