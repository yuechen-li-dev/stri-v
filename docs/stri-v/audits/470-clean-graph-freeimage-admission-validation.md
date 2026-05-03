# 470 Clean Graph FreeImage Admission Validation

## 1. Files changed
- `striv/projects/Stride.FreeImage/Stride.FreeImage.csproj`
- `striv/projects/Stride/Stride.csproj`
- `striv/StriV.Core.slnx`
- `docs/stri-v/audits/470-clean-graph-freeimage-admission-validation.md`

## 2. Blocker recap
- Failing project: `striv/projects/Stride/Stride.csproj`.
- Failing file: `sources/engine/Stride/Graphics/StandardImageHelper.Desktop.cs`.
- Missing types: `FreeImageAPI` namespace and `FreeImageBitmap` type.
- Why clean graph exposed this: the clean `Stride` project had no explicit reference to the old `Stride.FreeImage` assembly expected by desktop image helper code.

## 3. FreeImage audit
- Old project path: `sources/tools/Stride.FreeImage/Stride.FreeImage.csproj`.
- Source file set: wrapper and model files under `sources/tools/Stride.FreeImage/**` (e.g. `FreeImageWrapper.cs`, `FreeImageStaticImports.cs`, `Classes/FreeImageBitmap.cs`, enums/structs/delegates).
- Namespaces/types provided:
  - `FreeImageAPI` namespace (wrapper surface consumed by `StandardImageHelper.Desktop.cs`).
  - `FreeImageBitmap` class.
- Package refs in old project:
  - `System.Drawing.Common` conditioned on old framework property.
- Native/PInvoke indicators:
  - Extensive P/Invoke declarations (`DllImport`) in wrapper files.
  - Static import/wrapper code indicates runtime dependency on native FreeImage library.
- Old SDK/native/packaging behavior:
  - Uses old Stride SDK imports.
  - Uses old `StrideNativeLib` item group to package native `deps/FreeImage/Release/**/*.dll` payloads.
- Decision:
  - **Admit `Stride.FreeImage` into clean graph** with a minimal SDK-style managed project and explicit reference from clean `Stride`.
  - Do **not** reintroduce old SDK imports or native payload packaging behaviors in this step.
  - Excluding `StandardImageHelper.Desktop.cs` would be more destructive (removes core desktop image load/save path) and is not needed to clear this blocker.

## 4. Clean project changes
- New clean project:
  - `striv/projects/Stride.FreeImage/Stride.FreeImage.csproj`
- Source inclusion:
  - `Compile Include="../../../sources/tools/Stride.FreeImage/**/*.cs"`
- References/packages:
  - `ProjectReference` to `../Stride.Core/Stride.Core.csproj`
  - `PackageReference` to `System.Drawing.Common`
- Assembly/root namespace:
  - `AssemblyName` = `Stride.FreeImage`
  - `RootNamespace` = `FreeImageAPI`
- AP on/off:
  - AssemblyProcessor **not enabled** for this new project (no AP import added).
- Graph updates:
  - Added `ProjectReference` from `striv/projects/Stride/Stride.csproj` to `../Stride.FreeImage/Stride.FreeImage.csproj`.
  - Added project entry to `striv/StriV.Core.slnx`.

## 5. Validation results
1) Command:
- `./striv/build/striv-build-core.sh`
- Exit code: `1`
- First meaningful warning/error:
  - First blocking error moved past FreeImage and now fails in `Stride.Graphics` with missing `Stride.Shaders` types (`CS0234`, `CS0246`).
- Pass/fail: **FAIL**
- Output truncated: **Yes**

## 6. First new blocker
- Project: `striv/projects/Stride.Graphics/Stride.Graphics.csproj`
- Representative errors:
  - `sources/engine/Stride.Graphics/BatchBase.cs(4,14): error CS0234 ... 'Stride.Shaders'`
  - `sources/engine/Stride.Graphics/Shaders/SpriteAlphaCutoff.sdsl.cs(27,59): error CS0246 ... 'IShaderMixinBuilder'`
- Likely cause:
  - Clean graph excludes shader compiler/runtime shader assemblies; current `Stride.Graphics` compile set still includes shader-dependent sources.
- Smallest proposed next repair:
  - Add narrow clean-profile compile exclusions for shader-dependent generated/source files in `Stride.Graphics` (or admit the minimal shader runtime assembly if policy allows) and stop at next blocker.

## 7. Test results
- Tests were **not run** because build failed, per instruction.

## 8. Worktree status
- Command: `git status --short`
- Output:
  - ` M striv/StriV.Core.slnx`
  - ` M striv/projects/Stride/Stride.csproj`
  - `?? striv/projects/Stride.FreeImage/`

## 9. Recommended next task
- **next clean graph repair**
  - Repair `Stride.Graphics` shader-type compile blocker with narrow compile-source conditioning/exclusions aligned with current clean profile doctrine.
