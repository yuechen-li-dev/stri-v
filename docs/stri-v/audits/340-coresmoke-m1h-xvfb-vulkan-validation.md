# 340 — CoreSmoke M1h Xvfb/Vulkan validation

Date: 2026-05-03 (UTC)
Repository: `/workspace/stri-v`

## 1) Files changed
- `docs/stri-v/audits/340-coresmoke-m1h-xvfb-vulkan-validation.md` (this report only).

## 2) Package installation results
### apt commands used
```bash
sudo apt-get update
sudo apt-get install -y \
  xvfb \
  xauth \
  libsdl2-2.0-0 \
  libvulkan1 \
  mesa-vulkan-drivers \
  vulkan-tools \
  libx11-6 \
  libx11-xcb1 \
  libxext6 \
  libxrender1 \
  libxi6 \
  libxrandr2 \
  libxcursor1 \
  libxinerama1 \
  libxkbcommon0 \
  libxkbcommon-x11-0 \
  libxcb1 \
  libxcb-dri3-0 \
  libxcb-present0 \
  libxcb-randr0 \
  libxcb-shm0 \
  libxcb-sync1 \
  libxcb-xfixes0
```

### Result summary
- `apt-get update`: completed with warnings from `https://mise.jdx.dev/deb` (HTTP 403), while Ubuntu package indexes succeeded and apt continued.
- `apt-get install`: all requested packages were already present; no new installs and no removals.
- Package failures: none among requested packages.

### Tool availability after install/check
- `Xvfb`: available at `/usr/bin/Xvfb`.
- `xvfb-run`: available at `/usr/bin/xvfb-run`.
- `vulkaninfo`: available at `/usr/bin/vulkaninfo`.

## 3) Vulkan/display diagnostics
### Commands
```bash
command -v Xvfb || true
command -v xvfb-run || true
command -v vulkaninfo || true
vulkaninfo --summary || true
ldconfig -p | grep -E 'libSDL2|libvulkan|libX11' || true
```

### Observations
- `vulkaninfo --summary` ran successfully.
- Display-related notices were shown without X display context:
  - `'DISPLAY' environment variable not set... skipping surface info`
  - `error: XDG_RUNTIME_DIR is invalid or not set in the environment.`
- Vulkan ICD/device visibility:
  - Vulkan instance initialized (`Vulkan Instance Version: 1.3.275`).
  - One Vulkan physical device visible: `llvmpipe (LLVM 20.1.2, 256 bits)` with `deviceType = PHYSICAL_DEVICE_TYPE_CPU` and Mesa driver.
- `ldconfig` shows expected runtime libs present:
  - SDL: `libSDL2-2.0.so.0`
  - Vulkan loader + ICD libs: `libvulkan.so.1`, `libvulkan_lvp.so`, and multiple vendor ICDs
  - X11 libs: `libX11.so.6`, `libX11-xcb.so.1`

## 4) Runtime probe results
| Command | Exit code | First meaningful warning/error | Pass/Fail | Output truncated | Classification |
| --- | ---:| --- | --- | --- | --- |
| `./build/striv-run-coresmoke-m1h.sh` | 134 | `Cannot allocate SDL Window: x11 not available` | Fail | Yes | environment/display limitation |
| `xvfb-run -a ./build/striv-run-coresmoke-m1h.sh` | 0 | none blocking; runtime smoke passed | Pass | Yes | success |
| `xvfb-run -a ./build/striv-run-coresmoke-m1h.sh Release` | 0 | none blocking; runtime smoke passed | Pass | Yes | success |

Notes:
- Large build logs were emitted before runtime execution in all runs; captured terminal output was truncated by tooling limits, but each run's final status and runtime exit code were visible.

## 5) Runtime observations
- Default run still fails without display (`x11 not available`).
- Xvfb run gets past the prior `x11 not available` blocker.
- No Vulkan loader/ICD/device creation failure appeared in Xvfb runs.
- CoreSmoke reached clean self-exit under Xvfb (Debug and Release both `Runtime exit code: 0`).
- No new content/shader/audio/VR blocker was introduced by this probe; run used existing script settings (shader compiler/audio/VR remained disabled per script output).

## 6) M1h verdict update
| Mode                | Verdict | Current blocker | Next action |
| ------------------- | ------- | --------------- | ----------- |
| default SDL         | blocked | No X11 display in sandbox (`x11 not available`) | Use Xvfb for sandbox runtime probes |
| Xvfb                | pass    | none observed | Keep Xvfb path as sandbox runtime smoke default |
| Xvfb + explicit x11 | not needed | none | Skip unless future environment regression appears |

## 7) Recommended next task
Since Xvfb runtime smoke succeeds, recommended next task:
- Prepare M1 golden-path summary and Stri-V `.slnx` organization follow-up.
