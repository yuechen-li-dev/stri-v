# 220 — Engine M1e VirtualReality opt-out validation

## 1. Files changed
- `sources/engine/Stride.Engine/Stride.Engine.csproj`
- `build/striv-build-engine-m1e.sh`
- `build/striv-build-engine-m1e.ps1`
- `docs/stri-v/building-core.md`
- `docs/stri-v/audits/220-engine-m1e-vr-optout-validation.md`

## 2. Property design
- Property name: `StrideIncludeVirtualReality`.
- Default value: `true` when unset via:
  - `<StrideIncludeVirtualReality Condition="'$(StrideIncludeVirtualReality)' == ''">true</StrideIncludeVirtualReality>`
- Legacy/default behavior preservation:
  - Unset property keeps VR project reference included, matching prior behavior.
- Stri-V M1e opt-out mechanism:
  - build scripts now pass `-p:StrideIncludeVirtualReality=false`.
- Why narrower than `StriVCore=true`:
  - This gates only one dependency edge (`Stride.Engine -> Stride.VirtualReality`) and does not alter unrelated modules or global behavior switches.

## 3. `Stride.Engine.csproj` changes
- Added property default for `StrideIncludeVirtualReality` (default-on).
- Conditioned project reference:
  - `..\Stride.VirtualReality\Stride.VirtualReality.csproj` now included only when `$(StrideIncludeVirtualReality) != false`.
- Added conditional compile symbol when excluded:
  - `STRIDE_ENGINE_WITHOUT_VIRTUAL_REALITY`.
- Other project references changed:
  - None.

## 4. Direct VR usage audit
- `Stride.Engine` source directly references VR types.
- VR usages were found in:
  - `Engine/Game.cs`
  - `Rendering/Compositing/ForwardRenderer.cs`
  - `Rendering/Compositing/VRDeviceDescription.cs`
  - `Rendering/Compositing/VROverlayRenderer.cs`
  - `Rendering/Compositing/VRRendererSettings.cs`
- In this change set, those source usages were not yet guarded/excluded.
- `Stride.VirtualReality` project/source was untouched.

## 5. Build script changes
- Added `-p:StrideIncludeVirtualReality=false` to:
  - `build/striv-build-engine-m1e.sh`
  - `build/striv-build-engine-m1e.ps1`
- Confirmed retained properties:
  - `StrideIncludeShaderCompiler=false`
  - `StrideIncludeAudio=false`
  - `StridePlatforms=Linux`
  - `StrideGraphicsApis=Vulkan`
  - `StrideAssemblyProcessorFramework=net10.0`
  - `StrideAssemblyProcessorBasePath=<source-built AP output dir>`
  - `StrideAssemblyProcessorHash=sourcebuild`

## 6. Validation results
### Command 1
- Command: `./build/striv-build-engine-m1e.sh`
- Exit code: `1`
- First meaningful warning/error:
  - first actionable compile error observed: `ForwardRenderer.cs(37,17): error CS0246: VRDeviceSystem could not be found`.
- Classification: **Fail** (compile failure)
- Output truncated: **Yes** (tool output was truncated)

### Command 2
- Not run by design because Debug failed.
- Command: `./build/striv-build-engine-m1e.sh Release`
- Classification: **Not attempted**

### Command 3 (optional)
- Not attempted in this run.
- Command: `pwsh ./build/striv-build-engine-m1e.ps1`
- Classification: **Not attempted**

## 7. VR isolation observations
- `Stride.VirtualReality` was not restored/built after opt-out (its project reference is now conditionally excluded in no-VR mode).
- The prior `libNativePath.a` / `unknown directive: version` VR-native linker blocker did not reappear in this run.
- VR types still leaked into no-VR M1e compilation via direct `Stride.Engine` source references (e.g., `VRDeviceSystem`, `VRDevice`, `VRApi`, `VROverlay`).
- Default-on behavior appears preserved because the new property defaults to `true` when unset.

## 8. Next blocker
First new blocker after VR project edge opt-out:
- Project: `sources/engine/Stride.Engine/Stride.Engine.csproj`
- Files/errors:
  - `sources/engine/Stride.Engine/Rendering/Compositing/ForwardRenderer.cs(37,17): error CS0246 (VRDeviceSystem)`
  - additional VR type misses in `Game.cs`, `VRDeviceDescription.cs`, `VROverlayRenderer.cs`, `VRRendererSettings.cs`.
- Category: **other** (engine source conditional-compilation gap after dependency edge isolation)

## 9. M1e verdict

| Candidate                     | Verdict             | Current blocker                                                                 | Next action |
| ----------------------------- | ------------------- | ------------------------------------------------------------------------------- | ----------- |
| `build/StriV.Engine.M1e.slnf` | Adopt after repair  | `Stride.Engine` direct VR type usage still compiles in no-VR mode (`CS0246`)   | Add narrow `STRIDE_ENGINE_WITHOUT_VIRTUAL_REALITY` guards/exclusions for direct VR code paths in `Stride.Engine` only |

## 10. VR TODO confirmation
Confirmed documentation now includes a bold TODO stating:
- VR/native stack is intentionally excluded from Stri-V Core M1e.
- NativePath/OpenVR/OpenXR restoration requires dedicated future audit.
- No VR/native repair was performed in this task.

## 11. Recommended next task
Because M1e currently fails due to unexpected VR residuals, recommend:
- **narrower VR guard repair** in `Stride.Engine` (source-level guards/exclusions only).
