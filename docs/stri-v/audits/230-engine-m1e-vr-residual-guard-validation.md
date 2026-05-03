# 230 - Engine M1e VR residual guard validation

## 1) Files changed
- sources/engine/Stride.Engine/Stride.Engine.csproj
- sources/engine/Stride.Engine/Rendering/Compositing/ForwardRenderer.cs
- sources/engine/Stride.Engine/Engine/Game.cs
- docs/stri-v/building-core.md
- docs/stri-v/audits/230-engine-m1e-vr-residual-guard-validation.md

## 2) VR residual problem recap
- `Stride.VirtualReality` project reference was already conditioned out by `StrideIncludeVirtualReality=false` in previous work.
- The previous VR native linker blocker (`libNativePath.a`) was no longer the first blocker.
- `Stride.Engine` still had direct VR source usages (`Game` and `ForwardRenderer`, plus VR compositor files), which caused compile-time type resolution failures when VR was excluded.
- This task adds no-VR source guards and no-VR file exclusions in `Stride.Engine` without restoring VR.

## 3) `Stride.Engine.csproj` changes
- Added conditional compile removals for VR-only compositor files when `StrideIncludeVirtualReality=false`:
  - `Rendering/Compositing/VRDeviceDescription.cs`
  - `Rendering/Compositing/VROverlayRenderer.cs`
  - `Rendering/Compositing/VRRendererSettings.cs`
- Kept `StrideIncludeVirtualReality` default (`true`) unchanged.
- Kept conditioned `Stride.VirtualReality` project reference unchanged.
- No unrelated project references were changed.

## 4) Source guard changes
### `Rendering/Compositing/ForwardRenderer.cs`
- Guarded VR-only `using Stride.VirtualReality`, VR field (`VRDeviceSystem`), VR settings property (`VRRendererSettings`), and VR-only logic in `InitializeCore`, `CollectCore`, and `DrawCore` with `#if !STRIDE_ENGINE_WITHOUT_VIRTUAL_REALITY`.
- Left non-VR renderer flow intact by preserving the existing non-VR `else` branches.
- No fake VR stubs were introduced.
- `CopyOrScaleTexture` now falls back to direct copy in no-VR builds instead of referencing VR mirror scaler.

### `Engine/Game.cs`
- Guarded `using Stride.VirtualReality`, `VRDeviceSystem` property, construction/service registration, and `GameSystems.Add(VRDeviceSystem)` behind `#if !STRIDE_ENGINE_WITHOUT_VIRTUAL_REALITY`.
- Left unrelated game/service initialization untouched.
- No fake VR stubs were introduced.

## 5) Search verification (inside `sources/engine/Stride.Engine`)
Search terms run:
- `Stride.VirtualReality`
- `VRDevice`
- `VRApi`
- `VROverlay`
- `VRRenderer`
- `VirtualReality`

Classification:
- guarded:
  - `Rendering/Compositing/ForwardRenderer.cs` VR hits are in guarded regions.
  - `Engine/Game.cs` VR hits are in guarded regions.
- excluded file:
  - `Rendering/Compositing/VRDeviceDescription.cs`
  - `Rendering/Compositing/VROverlayRenderer.cs`
  - `Rendering/Compositing/VRRendererSettings.cs`
  (excluded from compilation when `StrideIncludeVirtualReality=false`)
- comments/docs:
  - none significant in `Stride.Engine` source for these terms.
- still problematic:
  - none VR-specific surfaced as first compiler blocker after this patch.
  - first new blocker moved to non-VR audio/input residual references in `Stride.Engine`.

## 6) Documentation update
- Updated `docs/stri-v/building-core.md` with an M1e note clarifying that no-VR mode excludes VR compositor source files in addition to conditioning out the VR project reference.
- Kept explicit bold VR TODO and did not claim runtime VR readiness.

## 7) Validation results
### Command 1
- Command: `./build/striv-build-engine-m1e.sh`
- Exit code: `1`
- First meaningful warning/error: first **error** after compile progressed was non-VR residual (`ScriptComponent.cs` missing `Stride.Audio` / `Stride.Input` namespaces); first listed: `Engine/ScriptComponent.cs(7,14): error CS0234: namespace 'Audio' does not exist in namespace 'Stride'`.
- Classification: fail (expected for current slice due to new blocker).
- Output truncated: yes (tool output was truncated due to length limits).

### Command 2
- Command: `./build/striv-build-engine-m1e.sh Release`
- Not executed, per instruction to stop after Debug failure.

### Command 3 (optional PowerShell)
- Command: `pwsh ./build/striv-build-engine-m1e.ps1`
- Not executed (optional, skipped after Debug failure stop condition).

## 8) VR isolation observations
- `Stride.VirtualReality` was not restored or reintroduced.
- `libNativePath.a` VR linker blocker did not reappear as first blocker in this run.
- Direct VR type errors (e.g., `VRDeviceSystem` missing in `ForwardRenderer`) no longer appeared as first compile blockers.
- Default-on behavior appears preserved because default `StrideIncludeVirtualReality=true` property and conditioned VR project reference remain intact.

## 9) Next blocker
First new blocker after VR residual guard repair:
- Project: `sources/engine/Stride.Engine/Stride.Engine.csproj`
- File/error examples:
  - `Engine/ScriptComponent.cs(7,14): error CS0234: namespace 'Audio' does not exist in namespace 'Stride'`
  - `Engine/ScriptComponent.cs(15,14): error CS0234: namespace 'Input' does not exist in namespace 'Stride'`
  - multiple related missing `AudioSystem`, `InputManager`, `SoundBase`, etc.
- Category: assets/compiler boundary? no. Rendering? no. AssemblyProcessor? no. Native? no. **Other** (audio/input residual source guarding mismatch under current M1e flags/graph).

## 10) M1e verdict

| Candidate                     | Verdict             | Current blocker                                                     | Next action |
| ----------------------------- | ------------------- | ------------------------------------------------------------------- | ----------- |
| `build/StriV.Engine.M1e.slnf` | Adopt after repair  | `Stride.Engine` compile errors from audio/input residual references | Narrow audio/input residual guard repair audit |

## 11) Recommended next task
Because M1e currently fails due to non-VR residual compile issues (audio/input source references while optional paths are disabled), recommend: **asset/compiler boundary audit** is not accurate; **rendering compositor isolation** is not current blocker; choose an immediate **narrow audio/input residual guard repair** follow-up (outside predefined list).

