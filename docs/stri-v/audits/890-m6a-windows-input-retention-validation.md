# 890 - M6a Windows Input Retention Validation

## 1) Files changed

- `striv/projects/Stride.Input/Stride.Input.csproj`
- `docs/stri-v/audits/890-m6a-windows-input-retention-validation.md`

## 2) Policy correction

Stri-V clean core should remain Vulkan-first, but not Linux-only forever. For local validation and future portability confidence, a minimal Windows desktop runtime/input path should remain available.

Current corrected doctrine applied in this follow-up:

- Keep Linux runtime path.
- Keep a minimal Windows desktop input path for Vulkan desktop validation.
- Direct3D is still out of scope for this phase.
- Continue excluding Android, iOS/UWP/mobile, VR/OpenVR/OpenXR, editor/Game Studio/Quantum, and old asset compiler/source asset pipeline paths.

## 3) Windows input audit

| Backend | File | Purpose | Dependencies | Keep/exclude | Rationale |
| ------- | ---- | ------- | ------------ | ------------ | --------- |
| RawInput | `sources/engine/Stride.Input/Windows/InputSourceWindowsRawInput.cs` | Windows desktop keyboard/mouse raw device input path (via WinForms/game context). | `SharpDX.RawInput` APIs already used in Windows input codepath. No controller stack required. | **Keep** | Best minimal desktop validation path for keyboard/mouse on Windows Vulkan without restoring broader legacy input stacks. |
| XInput | `sources/engine/Stride.Input/Windows/InputSourceWindowsXInput.cs` | Xbox-style gamepad/controller input support. | `SharpDX.XInput`, controller/runtime availability expectations. | **Exclude** | Not required for minimal keyboard/mouse desktop validation; adds controller-focused surface area. |
| DirectInput | `sources/engine/Stride.Input/Windows/InputSourceWindowsDirectInput.cs` | Legacy joystick/game controller enumeration and input path. | `SharpDX.DirectInput` and broader game-controller path complexity. | **Exclude** | Higher complexity/legacy risk, not needed for minimal Windows desktop keyboard/mouse path. |

## 4) Implementation

- Retained RawInput by removing the clean-graph exclusion for `InputSourceWindowsRawInput.cs` in `striv/projects/Stride.Input/Stride.Input.csproj`.
- Kept exclusions for:
  - `InputSourceWindowsDirectInput.cs`
  - `InputSourceWindowsXInput.cs`
- Kept existing Android/UWP exclusions unchanged.
- Updated inline policy comment in `Stride.Input.csproj` to document Vulkan-first + minimal Windows desktop validation intent.

## 5) Warning/build impact

Baseline (M6a report):

- Build summary warnings: **691**
- Extracted warning lines: **1382**

After this Windows input adjustment (current run):

- Build summary warnings (from `striv-build-core.sh` run): **2621**
- Extracted warning lines: **5472**

Delta vs M6a baseline:

- Summary warnings: **+1930**
- Extracted warning lines: **+4090**

Note: this run reproduced a high-warning full-core build profile rather than the reduced-warning M6a snapshot profile, so warning totals are not directly comparable as an isolated RawInput-only signal.

## 6) Validation results

| Command | Exit code | First meaningful warning/error | Pass/fail | Output truncated |
| ------- | --------- | ------------------------------ | --------- | ---------------- |
| `./striv/build/striv-build-core.sh 2>&1 | tee /tmp/striv-m6a-windows-input-after.log` | 0 | `warning CS8604` (`StriV.AssetPipeline/AssetPipeline.cs(72,26)`) | Pass | Yes (interactive capture truncated in terminal; full log preserved in `/tmp/striv-m6a-windows-input-after.log`) |
| `grep -E "warning (CS|CA|NU|STRIDE)[0-9]+" /tmp/striv-m6a-windows-input-after.log > /tmp/striv-m6a-windows-input-warning-lines.log || true` | 0 | None | Pass | No |
| `wc -l /tmp/striv-m6a-windows-input-warning-lines.log` | 0 | None | Pass | No |
| `grep -Eo "\[[^]]+\.csproj\]" /tmp/striv-m6a-windows-input-warning-lines.log | sort | uniq -c | sort -nr | head -n 40` | 0 | None | Pass | No |
| `dotnet test striv/tests/StriV.AssetTool.Tests/StriV.AssetTool.Tests.csproj -v minimal` | 0 | None | Pass | No |
| `dotnet test striv/tests/StriV.AssetPipeline.Tests/StriV.AssetPipeline.Tests.csproj --no-build -v minimal` | 0 | None | Pass | No |
| `dotnet test striv/tests/StriV.ShaderPipeline.Tests/StriV.ShaderPipeline.Tests.csproj --no-build -v minimal` | 0 | None (1 test skipped) | Pass | No |
| `dotnet test striv/tests/StriV.CleanGraph.Tests/StriV.CleanGraph.Tests.csproj --no-build -v minimal` | 0 | None | Pass | No |

## 7) Recommended next task

**Recommended:** fix first blocker in warning-accounting methodology before resuming M6b.

Reason: this follow-up validates that minimal Windows RawInput can be retained while DirectInput/XInput remain excluded and the clean build still passes, but warning totals were gathered under a different effective profile than the M6a reduced-warning snapshot. Re-baseline warning collection in the same profile/filters used by M6a, then continue M6b Engine/Rendering axing with trustworthy deltas.
