# 880 — M6a Platform Dead-Code Exclusion Validation

## 1. Files changed

- `striv/projects/Stride.Games/Stride.Games.csproj`
- `striv/projects/Stride.Input/Stride.Input.csproj`
- `striv/projects/Stride.Graphics/Stride.Graphics.csproj`

## 2. Baseline

- Prior warning baseline from 870 strategy report: **2621 Warning(s), 0 Error(s)** and **5472 extracted warning lines**.
- New before-count for this task (`/tmp/striv-m6a-before.log`): **2621 Warning(s), 0 Error(s)**.
- New extracted warning line count before (`/tmp/striv-m6a-warning-lines-before.log`): **5472**.

Top warning codes before (count):
- CS8618 (2066)
- CS8625 (940)
- CS8604 (500)
- CS8600 (492)
- CS8603 (366)

Top warning projects before (count):
- `Stride.Rendering` (1650)
- `Stride.Engine` (982)
- `Stride.Graphics` (886)
- `Stride.FreeImage` (390)
- `Stride.Games` (292)

## 3. Exclusion audit

### `Stride.Core.IO`
- Source root included: `sources/core/Stride.Core.IO/**/*.cs`.
- Platform/legacy paths found: existing explicit exclusion for `System.IO.Compression.Zip/ApkExpansionSupport.cs` (Android expansion zip helper).
- Action: left unchanged.
- Rationale: already narrowly excluded; no new clear mobile/editor accidental inclusions identified in this pass.

### `Stride.Games`
- Source root included: `sources/engine/Stride.Games/**/*.cs`.
- Platform/legacy paths found: `Starter/StrideActivity.cs` (Android activity/bootstrapper).
- Action: excluded.
- Rationale: Android launcher activity is mobile-only startup surface and not needed for Linux/Vulkan clean runtime profile.

### `Stride.Input`
- Source root included: `sources/engine/Stride.Input/**/*.cs`.
- Platform/legacy paths found:
  - `Android/**/*.cs`
  - `UWP/**/*.cs`
  - `InputSourceWindowsDirectInput.cs`
  - `InputSourceWindowsRawInput.cs`
  - `InputSourceWindowsXInput.cs`
- Action: excluded.
- Rationale: these are platform-specific input backends for Android/UWP/Windows native input APIs; not part of clean Linux/Vulkan core path.

### `Stride.Graphics`
- Source root included: `sources/engine/Stride.Graphics/**/*.cs`.
- Platform/legacy paths found: `WindowsMixedReality/**/*.cs` (UWP/Direct3D interop layer).
- Action: excluded.
- Rationale: mixed reality presenter is UWP/Direct3D specific and out of scope for Linux/Vulkan-first core.

### `Stride.Engine`
- Source root included: `sources/engine/Stride.Engine/**/*.cs`.
- Platform/legacy paths found: existing exclusions already cover shader compiler/audio/VR files.
- Action: left unchanged in this pass.
- Rationale: additional candidates were ambiguous or shared runtime/compositor paths; deferred for safety.

### `Stride.Rendering`
- Source root included: `sources/engine/Stride.Rendering/**/*.cs`.
- Platform/legacy paths found: no low-risk folder-level mobile/VR/editor-only compile groups confidently isolated in this pass.
- Action: left unchanged.
- Rationale: avoid accidental exclusion of shared render abstractions.

### `Stride.BepuPhysics`
- Source root included: `sources/engine/Stride.BepuPhysics/Stride.BepuPhysics/**/*.cs`.
- Platform/legacy paths found: none requiring exclusion in this pass.
- Action: left unchanged.
- Rationale: warning set appears domain/nullability/comment debt, not platform dead-code inclusion.

## 4. Exclusions implemented

| Project | Exclusion | Reason | Risk |
| ------- | --------- | ------ | ---- |
| Stride.Games | `Starter/StrideActivity.cs` | Android-only launcher | Low |
| Stride.Input | `Android/**/*.cs` | Android-only backend | Low |
| Stride.Input | `UWP/**/*.cs` | UWP-only backend | Low |
| Stride.Input | `InputSourceWindowsDirectInput.cs` | Windows DirectInput backend | Low |
| Stride.Input | `InputSourceWindowsRawInput.cs` | Windows RawInput backend | Low |
| Stride.Input | `InputSourceWindowsXInput.cs` | Windows XInput backend | Low |
| Stride.Graphics | `WindowsMixedReality/**/*.cs` | UWP/Direct3D mixed-reality presenter | Low |

## 5. Warning delta

- Before build-summary warning count: **2621**.
- After build-summary warning count: **691**.
- Before extracted warning lines: **5472**.
- After extracted warning lines: **1382**.

Top warning codes after (count):
- CS8618 (502)
- CS8625 (268)
- CS8604 (98)
- CS8601 (86)
- CS8600 (82)

Top warning projects after (count):
- `Stride.Graphics` (886)
- `Stride.Games` (292)
- `Stride.Input` (204)

## 6. Build/test validation

| Command | Exit code | First meaningful warning/error | Pass/Fail | Output truncated |
| ------- | --------- | ------------------------------ | --------- | ---------------- |
| `./striv/build/striv-build-core.sh 2>&1 | tee /tmp/striv-m6a-before.log` | 0 | first warning: `CS1030` in `ObjectIdBuilder.cs` | Pass | Yes |
| `grep -E "warning (CS|CA|NU|STRIDE)[0-9]+" /tmp/striv-m6a-before.log > /tmp/striv-m6a-warning-lines-before.log || true` | 0 | none | Pass | No |
| `wc -l /tmp/striv-m6a-warning-lines-before.log` | 0 | none | Pass | No |
| `sed -E 's/.*warning ((CS|CA|NU|STRIDE)[0-9]+).*/\1/' /tmp/striv-m6a-warning-lines-before.log | sort | uniq -c | sort -nr | head -n 40` | 0 | none | Pass | No |
| `grep -Eo "\[[^]]+\.csproj\]" /tmp/striv-m6a-warning-lines-before.log | sort | uniq -c | sort -nr | head -n 40` | 0 | none | Pass | No |
| `./striv/build/striv-build-core.sh 2>&1 | tee /tmp/striv-m6a-after.log` | 0 | first warning: `CS8765` in `PipelineStateDescriptionWithHash.cs` | Pass | Yes |
| `grep -E "warning (CS|CA|NU|STRIDE)[0-9]+" /tmp/striv-m6a-after.log > /tmp/striv-m6a-warning-lines-after.log || true` | 0 | none | Pass | No |
| `wc -l /tmp/striv-m6a-warning-lines-after.log` | 0 | none | Pass | No |
| `sed -E 's/.*warning ((CS|CA|NU|STRIDE)[0-9]+).*/\1/' /tmp/striv-m6a-warning-lines-after.log | sort | uniq -c | sort -nr | head -n 40` | 0 | none | Pass | No |
| `grep -Eo "\[[^]]+\.csproj\]" /tmp/striv-m6a-warning-lines-after.log | sort | uniq -c | sort -nr | head -n 40` | 0 | none | Pass | No |
| `dotnet test striv/tests/StriV.AssetTool.Tests/StriV.AssetTool.Tests.csproj -v minimal` | 0 | none | Pass | No |
| `dotnet test striv/tests/StriV.AssetPipeline.Tests/StriV.AssetPipeline.Tests.csproj --no-build -v minimal` | 0 | none | Pass | No |
| `dotnet test striv/tests/StriV.ShaderPipeline.Tests/StriV.ShaderPipeline.Tests.csproj --no-build -v minimal` | 0 | one skipped test (`StreamLiveness_DoesNotPruneWhenAccessUnknown`) | Pass | No |
| `dotnet test striv/tests/StriV.CleanGraph.Tests/StriV.CleanGraph.Tests.csproj --no-build -v minimal` | 0 | none | Pass | No |

## 7. First blocker

No build blocker encountered in this pass.

## 8. Deferred exclusions

- `Stride.Engine` additional runtime/compositor files referencing editor/VR/platform symbols were **not** excluded due to shared runtime risk.
- `Stride.Rendering` was left unchanged because candidate exclusions were not clearly isolated to out-of-scope dead paths.
- `Stride.Graphics` Direct3D-related abstractions and comments were left unchanged (possible shared API surface; exclusion risk higher than `WindowsMixedReality`).

## 9. Recommended next task

**Next axing pass** focused on `Stride.Engine` and `Stride.Rendering` with compile-item enumeration (`dotnet msbuild -getItem:Compile`) and narrow, evidence-backed exclusion of any remaining mobile/VR/editor-only concrete files.
