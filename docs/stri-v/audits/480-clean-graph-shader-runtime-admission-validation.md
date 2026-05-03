# 480 — Clean graph shader runtime admission validation

## 1) Files changed
- `striv/projects/Stride.Shaders/Stride.Shaders.csproj`
- `striv/projects/Stride.Graphics/Stride.Graphics.csproj`
- `striv/StriV.Core.slnx`

## 2) Blocker recap
- Failing project before repair: `striv/projects/Stride.Graphics/Stride.Graphics.csproj`.
- Representative failing files/symbols from prior context: `BatchBase.cs` (`using Stride.Shaders`) and `Shaders/SpriteAlphaCutoff.sdsl.cs` (`IShaderMixinBuilder`).
- This indicates missing shader runtime/model assembly types (`Stride.Shaders` namespace/types), not shader source compiler toolchain requirements.

## 3) Shader project audit
- `IShaderMixinBuilder` is defined in `sources/engine/Stride.Shaders/IShaderMixinBuilder.cs`.
- Old `sources/engine/Stride.Shaders/Stride.Shaders.csproj` references `Stride` and does **not** directly reference parser/compiler projects.
- Old `sources/engine/Stride.Shaders.Parser/Stride.Shaders.Parser.csproj` references both `Stride.Core.Shaders` and `Stride.Shaders`.
- Old `sources/engine/Stride.Shaders.Compiler/Stride.Shaders.Compiler.csproj` references `Stride.Shaders.Parser`.
- Old `sources/shaders/Stride.Core.Shaders/Stride.Core.Shaders.csproj` directly references `CppNet` and `Irony`; naive inclusion would reintroduce forbidden legacy parser/preprocessor dependencies.

## 4) Clean shader project changes
### `striv/projects/Stride.Shaders/Stride.Shaders.csproj`
- Source glob: `sources/engine/Stride.Shaders/**/*.cs`.
- Excludes: `bin/obj` only.
- Package refs: none added.
- Project refs: `../Stride/Stride.csproj`.
- AP decision: includes `StriV.AssemblyProcessor.targets`, mirroring clean graph conventions.
- Compiler/parser exclusion: no references were added to `Stride.Shaders.Parser`, `Stride.Shaders.Compiler`, `Stride.Core.Shaders`, `CppNet`, or `Irony`.

## 5) Graphics project changes
- Added `ProjectReference` from clean `Stride.Graphics` to clean `Stride.Shaders`.
- No references added to compiler/parser projects.
- No CppNet introduced.

## 6) Validation results
### Command
`./striv/build/striv-build-core.sh`
- Exit code: `1`
- First meaningful warning/error: build reaches `Stride.Graphics`; first blocking errors are Vulkan API/type mismatches (not shader-missing-type errors).
- Pass/fail: **fail**
- Output truncated: **yes** (tool output truncation occurred).

## 7) First new blocker
- Project: `striv/projects/Stride.Graphics/Stride.Graphics.csproj`
- Errors:
  - `sources/engine/Stride.Graphics/Vulkan/DescriptorPool.Vulkan.cs(19,73): CS1061` (`GraphicsDevice.NextFenceValue` missing)
  - `sources/engine/Stride.Graphics/Vulkan/DescriptorPool.Vulkan.cs(20,67): CS7036` (`ResourcePool<VkDescriptorPool>.GetObject(ulong)` missing arg)
  - `sources/engine/Stride.Graphics/Vulkan/DescriptorPool.Vulkan.cs(70,51): CS1061` (`GraphicsDevice.descriptorPools` missing)
  - `sources/engine/Stride.Graphics/Vulkan/DescriptorPool.Vulkan.cs(86,73): CS1061` (`GraphicsDevice.NextFenceValue` missing)
  - `sources/engine/Stride.Graphics/Vulkan/CommandList.Vulkan.cs(332,66): CS1061` (`VkDescriptorSet.DescriptorStartOffset` missing)
  - `sources/engine/Stride.Graphics/Vulkan/CommandList.Vulkan.cs(332,44): CS1061` (`VkDescriptorSet.HeapObjects` missing)
- Likely cause: post-shader-admission build progressed to a subsequent Vulkan-side clean-graph drift/API mismatch.
- Smallest next repair: align/restore missing Vulkan-side members/shape in clean `Stride.Graphics` (or exclude stale Vulkan paths if intentionally unsupported) without touching shader compiler/parser toolchain.

## 8) Test results
- Not run, per instruction (build failed).

## 9) Worktree status
`git status --short` showed:
- `M striv/StriV.Core.slnx`
- `M striv/projects/Stride.Graphics/Stride.Graphics.csproj`
- `?? striv/projects/Stride.Shaders/`

## 10) Recommended next task
- **next clean graph repair** focused on Vulkan runtime source/API alignment in `Stride.Graphics` (new first blocker), while preserving shader compiler/parser exclusion.
