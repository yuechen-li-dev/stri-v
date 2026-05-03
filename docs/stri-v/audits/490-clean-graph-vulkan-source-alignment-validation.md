# 490 - clean graph Vulkan source alignment validation

## 1) Files changed
- `striv/projects/Stride.Graphics/Stride.Graphics.csproj`
- `docs/stri-v/audits/490-clean-graph-vulkan-source-alignment-validation.md`

## 2) Blocker recap
- Failing project (original blocker): `striv/projects/Stride.Graphics/Stride.Graphics.csproj`.
- Failing files (representative):
  - `sources/engine/Stride.Graphics/Vulkan/DescriptorPool.Vulkan.cs`
  - `sources/engine/Stride.Graphics/Vulkan/CommandList.Vulkan.cs`
- Missing members reported:
  - `GraphicsDevice.NextFenceValue`
  - `GraphicsDevice.descriptorPools`
  - `VkDescriptorSet.DescriptorStartOffset`
  - `VkDescriptorSet.HeapObjects`
  - `ResourcePool<VkDescriptorPool>.GetObject(ulong)` signature mismatch at callsites using 0 arguments.
- Why this is a Stride-side source/config mismatch (not raw Vortice): missing symbols are Stride wrapper members/constants/descriptor-set fields and fence/pool helper usage patterns in Stride partials, not Vulkan binding API surface.

## 3) Vulkan source audit
- Definitions/usages found:
  - `ResourcePool<T>.GetObject(ulong completedFenceValue)` is defined in `sources/engine/Stride.Graphics/Vulkan/GraphicsDevice.Vulkan.cs`.
  - `DescriptorSet.DescriptorStartOffset` and `DescriptorSet.HeapObjects` are defined only under `#if STRIDE_GRAPHICS_API_DIRECT3D11 || (STRIDE_GRAPHICS_API_VULKAN && STRIDE_GRAPHICS_NO_DESCRIPTOR_COPIES)` in `sources/engine/Stride.Graphics/DescriptorSet.cs`.
  - `DescriptorSet.Vulkan.cs` and `DescriptorPool.Vulkan.cs` are compiled only when `!STRIDE_GRAPHICS_NO_DESCRIPTOR_COPIES`.
  - `DescriptorPool.Vulkan.cs` mixes old/new usage (`DescriptorPools` vs `descriptorPools`, and `GetObject()` with no parameter), indicating stale non-NO_DESCRIPTOR_COPIES path.
- Old project behavior:
  - `sources/engine/Stride.Graphics/Stride.Graphics.csproj` sets `DefineConstants += STRIDE_GRAPHICS_NO_DESCRIPTOR_COPIES` globally.
- Clean project behavior (before fix):
  - `striv/projects/Stride.Graphics/Stride.Graphics.csproj` did not define `STRIDE_GRAPHICS_NO_DESCRIPTOR_COPIES`, so stale Vulkan descriptor-copy files were admitted.
- Compile set comparison:
  - `dotnet msbuild ... -getItem:Compile` for old vs clean showed broad path-shape differences (linked relative identities vs direct), but the key behavioral delta was preprocessor constants, not missing physical source files.
- Vortice package findings:
  - Central package version is `Vortice.Vulkan` `3.0.3` in `sources/Directory.Packages.props`.
  - Clean `Stride.Graphics` references `Vortice.Vulkan` explicitly.

## 4) Fix implemented
- Added `STRIDE_GRAPHICS_NO_DESCRIPTOR_COPIES` define to clean `striv/projects/Stride.Graphics/Stride.Graphics.csproj` to match old Stride.Graphics project behavior.
- No source moves, no dummy member additions, no old SDK target import, no Direct3D re-enable.
- Rationale: minimal, evidence-based source-selection fix that excludes stale Vulkan descriptor-copy path and re-enables the descriptor entry path where `DescriptorStartOffset`/`HeapObjects` are intentionally defined.

## 5) Validation results
1. Command:
   - `dotnet msbuild sources/engine/Stride.Graphics/Stride.Graphics.csproj -p:StridePlatforms=Linux -p:StrideGraphicsApis=Vulkan -p:TargetFramework=net10.0 -getItem:Compile > /tmp/old-graphics-compile.txt`
   - Exit code: 0
   - First meaningful warning/error: none
   - Pass/Fail: Pass
   - Output truncated: No

2. Command:
   - `dotnet msbuild striv/projects/Stride.Graphics/Stride.Graphics.csproj -p:TargetFramework=net10.0 -getItem:Compile > /tmp/clean-graphics-compile.txt`
   - Exit code: 0
   - First meaningful warning/error: none
   - Pass/Fail: Pass
   - Output truncated: No

3. Command:
   - `diff -u /tmp/old-graphics-compile.txt /tmp/clean-graphics-compile.txt | head -n 200`
   - Exit code: 0
   - First meaningful warning/error: none
   - Pass/Fail: Pass
   - Output truncated: Yes (intentionally head-limited)

4. Command:
   - `./striv/build/striv-build-core.sh`
   - Exit code: 1
   - First meaningful warning/error: build advanced past previous Vulkan mismatch and then failed in `Stride.Engine` with missing rendering/runtime types (e.g. `SkeletonUpdater`, `RenderGroup`, `RenderContext`, `RenderModel`).
   - Pass/Fail: Fail
   - Output truncated: Yes (tool output truncated)

## 6) First new blocker
- Project: `striv/projects/Stride.Engine/Stride.Engine.csproj`
- Representative file/error:
  - `sources/engine/Stride.Engine/Engine/ModelComponent.cs(110,16): error CS0246: SkeletonUpdater not found`
  - many similar missing rendering/runtime symbols in Stride.Engine.
- Likely cause:
  - clean graph source-selection/reference gap in rendering/model/engine runtime assemblies now that graphics blocker is cleared.
- Smallest proposed next repair:
  - audit clean `Stride.Engine` include/exclude/reference set against old `sources/engine/Stride.Engine` evaluated compile/reference inputs; restore missing clean runtime/model/render assemblies (without re-enabling excluded shader compiler/CpNet/audio/VR).

## 7) Test results
- Not run, per instruction, because core build failed (`./striv/build/striv-build-core.sh` exit code 1).

## 8) Worktree status
- Run: `git status --short`
- Reported modified/new files are captured in section (1) and current status output.

## 9) Recommended next task
- **next clean graph repair**: repair `Stride.Engine` runtime rendering/model source-selection and project-reference alignment as the next blocker after this Vulkan alignment fix.
