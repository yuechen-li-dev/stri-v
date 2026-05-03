# CoreSmoke M1h runtime validation

## 1) Files changed

- `samples/StriV/CoreSmoke/Program.cs`
- `build/striv-run-coresmoke-m1h.sh`
- `build/striv-run-coresmoke-m1h.ps1`
- `docs/stri-v/building-core.md`
- `docs/stri-v/audits/320-coresmoke-m1h-runtime-validation.md`

## 2) Program changes

- Added `CoreSmokeGame : Game` subclass in `Program.cs`.
- Added first-frame exit condition via `Update(GameTime)` frame counter (`Exit()` when frame count reaches 1).
- Entry point remains tiny and code-first (`using var game = new CoreSmokeGame(); game.Run();`).
- No assets, no `.sdpkg`, `.sdscene`, `.sdproj`, no editor/Game Studio usage added.
- No audio/VR/shader compiler API usage added.
- No Bepu behavior added.

## 3) Run script design

### Linux (`build/striv-run-coresmoke-m1h.sh`)

- Build-first behavior: invokes `./build/striv-build-coresmoke-m1g.sh <Configuration>`.
- Configuration handling: accepts `Debug` (default) or `Release`.
- DLL path detection: `samples/StriV/CoreSmoke/bin/<Configuration>/net10.0/StriV.CoreSmoke.dll` and explicit existence check.
- Timeout safety: uses `timeout 20s` when available; otherwise warns and runs directly.
- Diagnostics: prints configuration, DLL path, run command, and runtime exit code.
- Failure classification strategy is printed for:
  - SDL/display errors => environment limitation.
  - Vulkan loader/ICD/device errors => environment limitation.
  - Missing native library errors => environment/native packaging blocker.
  - Other managed/runtime exceptions => potential engine/runtime blocker.

### PowerShell (`build/striv-run-coresmoke-m1h.ps1`)

- Build-first behavior: invokes `build/striv-build-coresmoke-m1g.ps1 -Configuration <...>`.
- Configuration handling: `-Configuration Debug|Release`.
- DLL path detection with explicit existence check.
- Timeout safety: starts `dotnet` process and enforces 20-second timeout via `WaitForExit(20000)` + `Kill()`.
- Diagnostics and failure classification messaging aligned with Linux script.
- Exits nonzero on runtime failure.

## 4) Validation results

### Attempt 1
- Exact command: `./build/striv-run-coresmoke-m1h.sh`
- Exit code: `134`
- First meaningful warning/error: `Unhandled exception. System.Exception: Cannot allocate SDL Window: x11 not available`
- Pass/fail classification: **Fail**
- Output truncated: **Yes** (tool output was truncated in capture)
- Blocker classification: **Environment limitation** (headless/sandbox display stack, SDL/X11 unavailable)

### Attempt 2 (optional PowerShell)
- Exact command: `pwsh ./build/striv-run-coresmoke-m1h.ps1`
- Exit code: `127`
- First meaningful warning/error: `/bin/bash: line 1: pwsh: command not found`
- Pass/fail classification: **Fail (tool unavailable in environment)**
- Output truncated: **No**
- Blocker classification: **Environment limitation** (PowerShell not installed)

### Release run decision
- `./build/striv-run-coresmoke-m1h.sh Release` was **not run** because Debug already failed with a clear environment limitation (`x11 not available`), per task guidance.

## 5) Runtime observations

- CoreSmoke build stage completed before runtime launch.
- CoreSmoke attempted launch but did not reach clean self-exit due to SDL window allocation failure.
- SDL/display error observed: `x11 not available`.
- No Vulkan loader/ICD/device creation error was reached before failure.
- No missing native library error was observed in this run.
- No content/shader/audio/VR-specific runtime errors were observed.

## 6) M1h verdict

| Candidate               | Verdict             | Current blocker                                  | Next action |
| ----------------------- | ------------------- | ------------------------------------------------ | ----------- |
| CoreSmoke runtime smoke | Adopt locally only  | Sandbox/headless environment lacks SDL/X11 display | Validate on a local Linux dev machine with display/Vulkan runtime |

## 7) Worktree status

Command run:

```bash
git status --short
```

Observed status after M1h changes:

- `M docs/stri-v/building-core.md`
- `M samples/StriV/CoreSmoke/Program.cs`
- `?? build/striv-run-coresmoke-m1h.ps1`
- `?? build/striv-run-coresmoke-m1h.sh`
- `?? docs/stri-v/audits/320-coresmoke-m1h-runtime-validation.md`

## 8) Recommended next task

Because runtime failed due to display environment limitations, recommend:

- **Local dev-machine validation and classify sandbox as non-authoritative** for runtime graphics startup.
