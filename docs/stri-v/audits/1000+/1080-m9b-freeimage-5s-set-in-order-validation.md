# 1080 - M9b FreeImage 5S Set in order validation

## 1) Files changed
- Reorganized FreeImage source tree into boundary-oriented folders:
  - Interop: `FreeImageStaticImports.cs`, `FreeImageWrapper.cs`, `Delegates.cs`, all `Enumerations/*.cs`, all `Structs/*.cs`.
  - Runtime: `FreeImageBitmap.cs`, `FreeImageEngine.cs`, `MemoryArray.cs`, `PluginRepository.cs`, `StreamWrapper.cs`, `FreeImageStreamIO.cs`.
  - Metadata: `ImageMetadata.cs`, `MetadataTag.cs`, `MetadataModel.cs`, `MetadataModels.cs`.
  - Compatibility: moved `System.Drawing/*.cs` under `Compatibility/System.Drawing/`.
- Added boundary map doc: `sources/tools/Stride.FreeImage/README-5s-set-in-order.md`.
- Added/updated invariant comments/docs in:
  - `sources/tools/Stride.FreeImage/Interop/FreeImageStaticImports.cs`
  - `sources/tools/Stride.FreeImage/Interop/FreeImageWrapper.cs`
  - `sources/tools/Stride.FreeImage/Runtime/FreeImageBitmap.cs`
  - `sources/tools/Stride.FreeImage/Metadata/ImageMetadata.cs`

## 2) 5S phase
- M9a was **Sort** (inventory/usage validation only).
- M9b is **Set in order** (physical/logical organization and boundary documentation).
- Shine/warning cleanup, API reduction, and replacement work are intentionally deferred.

## 3) Organization plan
- Created folder groupings:
  - `Interop/` (+ `Interop/Enumerations`, `Interop/Structs`)
  - `Runtime/`
  - `Metadata/`
  - `Compatibility/System.Drawing/`
- Namespace policy: retained existing public namespaces (e.g., `FreeImageAPI`, `FreeImageAPI.Metadata`, `FreeImageAPI.IO`) to avoid API breaks.
- Public type names preserved.
- Left several compatibility-heavy classes in `Classes/` for now (e.g., `Palette`, `Scanline`, `GifInformation`, `LocalPlugin`, `FreeImagePlugin`) to avoid high-churn moves in this pass; these can be split in a subsequent proof-driven ordering pass.

## 4) Interop/runtime architecture map
- Raw native interop layer:
  - `Interop/FreeImageStaticImports.cs` + `Interop/Structs/*` + `Interop/Delegates.cs` + `Interop/Enumerations/*`.
  - These are ABI-sensitive and should stay signature/layout-stable unless validated.
- Managed runtime bridge:
  - `Runtime/FreeImageBitmap.cs` as active bridge object on desktop image load/save path.
  - Runtime helpers in `Runtime/` keep stream/memory/plugin flow.
- Metadata/plugin layer:
  - `Metadata/*` remains broad legacy wrapper surface retained for compatibility.
  - Plugin repository/runtime access remains in `Runtime/PluginRepository.cs`.
- Compatibility/System.Drawing layer:
  - `Compatibility/System.Drawing/*` retained for compatibility behavior; no removal/replacement attempted.
- Native handle lifetime assumptions documented and preserved:
  - `FIBITMAP`, `FIMEMORY`, `FIMETADATA`, `FIMULTIBITMAP`, `FITAG`, `fi_handle` ownership/lifecycle invariants are unchanged.

## 5) Documentation changes
- Added project-local boundary map doc (`README-5s-set-in-order.md`) to encode organization rationale and deferred phases.
- Added interop ABI stability remarks to `Interop/FreeImageStaticImports.cs`.
- Added managed-wrapper boundary and stream handle ownership remarks to `Interop/FreeImageWrapper.cs`.
- Added runtime bridge role and native handle lifecycle invariants to `Runtime/FreeImageBitmap.cs`.
- Added compatibility-surface/deferred-reduction note to `Metadata/ImageMetadata.cs`.

These comments are intended to reduce accidental behavior changes during later Shine and refactor passes.

## 6) Behavior compatibility
- No behavior changes intended.
- No native interop signatures changed.
- No public type renames.
- No namespace-breaking changes.
- No System.Drawing removal.
- Build/test commands executed (see validation section) with no new errors.

## 7) Deferred work
- M9c Shine warning cleanup for `Stride.FreeImage`.
- Proof-driven API surface reduction of legacy wrapper sections.
- System.Drawing compatibility removal/replacement evaluation.
- Future codec replacement design (if pursued).
- Any native loading/refactor changes requiring dedicated interop validation.

## 8) Validation results
1. Command: `dotnet build striv/projects/Stride.FreeImage/Stride.FreeImage.csproj -c Debug -p:StriVWarningFocusProject=Stride.FreeImage`
   - Exit code: 0
   - First meaningful warning/error: `warning CS8625` in `Runtime/PluginRepository.cs` (existing nullable warning class).
   - Pass/fail: Pass
   - Output truncated: Yes

2. Command: `dotnet test striv/tests/StriV.AssetTool.Tests/StriV.AssetTool.Tests.csproj -v minimal`
   - Exit code: 0
   - First meaningful warning/error: `warning CS8604` in `striv/projects/StriV.AssetPipeline/AssetPipeline.cs`.
   - Pass/fail: Pass
   - Output truncated: No

3. Command: `dotnet test striv/tests/StriV.AssetPipeline.Tests/StriV.AssetPipeline.Tests.csproj --no-build -v minimal`
   - Exit code: 0
   - First meaningful warning/error: none emitted in captured output.
   - Pass/fail: Pass
   - Output truncated: No

4. Command: `dotnet test striv/tests/StriV.ShaderPipeline.Tests/StriV.ShaderPipeline.Tests.csproj --no-build -v minimal`
   - Exit code: 0
   - First meaningful warning/error: none emitted in captured output.
   - Pass/fail: Pass
   - Output truncated: No

5. Command: `dotnet test striv/tests/StriV.CleanGraph.Tests/StriV.CleanGraph.Tests.csproj -v minimal`
   - Exit code: 0
   - First meaningful warning/error: warnings from transitive project builds (existing nullable warnings), tests passed.
   - Pass/fail: Pass
   - Output truncated: Yes

6. Command: `./striv/build/striv-build-core.sh`
   - Exit code: 0
   - First meaningful warning/error: `warning CS1030` in `ObjectIdBuilder.cs` (`#warning PERF`), existing baseline.
   - Pass/fail: Pass
   - Output truncated: Yes

## 9) Recommended next task
- Recommended next step: **M9c Shine for `Stride.FreeImage`** (focused warning cleanup now that interop/runtime/metadata/compatibility boundaries are explicit).
