# 330 - CoreSmoke M1h headless/offscreen probe validation

Date: 2026-05-03 (UTC)

## 1) Files changed
- `build/striv-run-coresmoke-m1h.sh`
- `build/striv-run-coresmoke-m1h.ps1`
- `docs/stri-v/building-core.md`
- `docs/stri-v/audits/330-coresmoke-m1h-headless-probe-validation.md`

## 2) Script changes

### New CLI options
- Linux script now supports optional `--sdl-video-driver <driver>`.
- Existing positional configuration remains (`Debug` default, or `Release`).
- Supported forms:
  - `./build/striv-run-coresmoke-m1h.sh --sdl-video-driver dummy`
  - `./build/striv-run-coresmoke-m1h.sh --sdl-video-driver offscreen`
  - `./build/striv-run-coresmoke-m1h.sh Release --sdl-video-driver dummy`
- PowerShell script now supports `-SdlVideoDriver <driver>`.

### Parsing behavior
- Linux script parses args in-order with a simple `while/case` loop.
- Recognized tokens:
  - `Debug` / `Release`
  - `--sdl-video-driver <value>`
- Unknown args fail fast with usage text and exit code 2.

### SDL_VIDEODRIVER scoping
- Linux: `SDL_VIDEODRIVER` is set only for the runtime process invocation (inline env assignment before `dotnet`/`timeout`), not exported globally.
- PowerShell: `Start-Process -Environment @{ SDL_VIDEODRIVER = <value> }` scopes to the child process only.

### Diagnostics/classification changes
- Runtime diagnostics now always print selected SDL driver override (`<none>` when unset).
- Classification guidance expanded with explicit categories for:
  - X11/display allocation unavailable.
  - Dummy/offscreen SDL mode limitations (headless probe limitation, non-authoritative).
  - Vulkan loader/ICD/device issues.
  - Graphics device/swapchain creation failures.
  - Missing native library issues.
  - Managed engine/runtime exceptions.

### Timeout behavior
- Linux still uses `timeout 20s` when available and preserves exit-code 124 timeout classification.
- PowerShell still enforces 20s via `WaitForExit(20000)` and kill-on-timeout.

## 3) Probe results

| Command | SDL video driver | Exit code | First meaningful warning/error | Classification | Output truncated | Limitation vs blocker |
|---|---|---:|---|---|---|---|
| `./build/striv-run-coresmoke-m1h.sh` | default (`<none>`) | 134 | `Cannot allocate SDL Window: x11 not available` | Environment limitation (display/X11 unavailable) | Yes (build logs truncated by terminal capture, runtime error captured) | Environment limitation |
| `./build/striv-run-coresmoke-m1h.sh --sdl-video-driver dummy` | `dummy` | 134 | `Cannot allocate SDL Window: Vulkan support is either not configured in SDL or not available in current SDL video driver (dummy) or platform` | Headless probe limitation (dummy driver path cannot provide required Vulkan window support here) | Yes (build logs truncated by terminal capture, runtime error captured) | Environment/headless limitation |
| `./build/striv-run-coresmoke-m1h.sh --sdl-video-driver offscreen` | `offscreen` | 134 | `Cannot allocate SDL Window: Vulkan support is either not configured in SDL or not available in current SDL video driver (offscreen) or platform` | Headless probe limitation (offscreen driver path cannot provide required Vulkan window support here) | Yes (build logs truncated by terminal capture, runtime error captured) | Environment/headless limitation |

## 4) Runtime observations
- Default mode still fails with `x11 not available`: **yes**.
- Dummy driver available as env override path: **script path works**, but runtime fails before progress due to missing Vulkan support in that SDL driver/platform combination.
- Offscreen driver available as env override path: **script path works**, but runtime fails similarly on Vulkan support unavailability.
- Did either probe get past SDL window allocation? **No**.
- Vulkan loader/ICD/device errors explicitly observed? **No explicit loader/ICD/device init error string; failure occurs at SDL window allocation with Vulkan capability requirement message.**
- Missing native library errors observed? **No**.
- Content/shader/audio/VR errors observed? **No**.
- Any clean CoreSmoke exit in tested modes? **No**.

## 5) M1h verdict update

| Mode | Verdict | Current blocker | Next action |
|---|---|---|---|
| default SDL | Fail in sandbox | X11/display unavailable (`x11 not available`) | Validate runtime on local desktop/WSLg machine with display + Vulkan runtime |
| dummy | Fail in sandbox headless probe | SDL dummy mode lacks usable Vulkan window support on this platform/runtime combo | Treat as non-authoritative headless limitation; continue compile-slice work |
| offscreen | Fail in sandbox headless probe | SDL offscreen mode lacks usable Vulkan window support on this platform/runtime combo | Treat as non-authoritative headless limitation; continue compile-slice work |

## 6) Worktree status
Command:
```bash
git status --short
```

Output (after probe and scoped edits):
```text
 M build/striv-run-coresmoke-m1h.ps1
 M build/striv-run-coresmoke-m1h.sh
 M docs/stri-v/building-core.md
?? docs/stri-v/audits/330-coresmoke-m1h-headless-probe-validation.md
```

## 7) Recommended next task
Recommended path: **local/WSLg/dev-machine runtime validation and proceed with compile-slice organization work**.

Reasoning: all tested modes failed due to display/headless/Vulkan-enablement limitations in this sandbox path, with no evidence yet of a managed engine startup bug or missing native runtime library blocker.
