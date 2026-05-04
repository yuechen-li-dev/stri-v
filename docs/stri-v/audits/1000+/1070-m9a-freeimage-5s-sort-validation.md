# 1070 — M9a FreeImage 5S Sort Validation

## 1) Files changed
- `docs/stri-v/audits/1000+/1070-m9a-freeimage-5s-sort-validation.md` (report-only; no product code changes).

## 2) 5S phase
M9a is **Sort** only. This pass classifies current `Stride.FreeImage` surface into keep/quarantine/defer buckets and captures evidence for later Set-in-order/Shine work. No replacement effort, no behavior changes, no interop signature edits, and no cleanup beyond reporting were performed.

## 3) Project inventory
- Target project: `striv/projects/Stride.FreeImage/Stride.FreeImage.csproj`.
- Compile item inventory command output file line-count: `1230` (`dotnet msbuild ... -getItem:Compile` redirect). 
- Physical source files under `sources/tools/Stride.FreeImage`: `69` files.
- Major source groups observed:
  - `Classes/*` (wrapper object model such as `FreeImageBitmap`, metadata, plugin repository).
  - `Structs/*` and `Enumerations/*` (interop ABI/value types and enums).
  - `FreeImageWrapper.cs` + `FreeImageStaticImports.cs` (P/Invoke and high-level wrapper API).
  - `System.Drawing/*` compatibility shim-like types (`PixelFormat`, `RotateFlipType`, `PropertyItem`, etc.).

## 4) Reference audit
### Who references `Stride.FreeImage`
- Clean `Stride` project references `Stride.FreeImage` directly in clean graph project file.
- Desktop image helper (`sources/engine/Stride/Graphics/StandardImageHelper.Desktop.cs`) directly uses `FreeImageAPI` and `FreeImageBitmap` for load/save paths.
- Tooling side additionally references FreeImage API (`sources/tools/Stride.TextureConverter/.../FITexLib.cs`).

### Active types used in runtime image path
- `FreeImageBitmap` constructor/load/save/rotate usage is explicit in `StandardImageHelper.Desktop.cs` for GIF/TIFF/BMP/JPEG/PNG load/save flow.
- `FREE_IMAGE_FORMAT` is used in the save format routing.
- `NativeLibraryHelper.PreloadLibrary("freeimage", typeof(StandardImageHelper))` shows runtime dependency on native library preloading on desktop path.

Conclusion for M9a question (1): clean graph runtime today uses `StandardImageHelper.Desktop` + `FreeImageBitmap` + related FreeImage enums as an active bridge.

## 5) Native interop audit
- Interop boundary is broad and centralized mostly in `FreeImageStaticImports.cs`/`FreeImageWrapper.cs` with many FreeImage entry points and managed helper wrappers.
- Critical value/handle types include `FIBITMAP`, `FIMEMORY`, `FIMETADATA`, `FIMULTIBITMAP`, `FITAG`, `Plugin`, `fi_handle`, and many bitmap color structs.
- Interop-related patterns observed: `DllImport`, `Marshal`, `IntPtr`, `unsafe`, handle wrapping, and FreeImage plugin delegates.
- Platform/native loading assumptions:
  - Desktop loader explicitly preloads library name `freeimage` via Stride helper.
  - Project still includes native-bridge-oriented wrapper design from original FreeImage .NET layer.
- Legal/license concerns: source files contain original FreeImage wrapper headers and must be preserved.

Conclusion for M9a questions (2) and (6): native interop layer remains required while bridge remains active; it carries desktop/native assumptions (dynamic native lib availability) that must be respected across Linux/Windows desktop clean core.

## 6) System.Drawing audit
- `System.Drawing.Common` package is referenced in `striv/projects/Stride.FreeImage/Stride.FreeImage.csproj`.
- Numerous FreeImage structs/classes use `System.Drawing` types (notably `Color`, legacy pixel/color conversion helpers, and compatibility metadata/property shapes).
- Project-local `sources/tools/Stride.FreeImage/System.Drawing/*` also suggests compatibility façade patterns.

Assessment:
- `System.Drawing.Common` appears currently necessary for this bridge surface as-is.
- Replacement/removal is feasible only as **future work** after API-surface reduction or alternative color/pixel abstractions (deferred by doctrine).

## 7) Classification table
| Source area/file group | Classification | Reason | Action |
| --- | --- | --- | --- |
| `FreeImageWrapper.cs`, `FreeImageStaticImports.cs`, `Delegates.cs`, interop `Structs/*`, `Enumerations/*` | Keep | Core ABI bridge and signatures required by existing runtime/tooling callsites. | Keep unchanged in M9a. |
| `Classes/FreeImageBitmap.cs`, `Classes/FreeImageEngine.cs`, stream/plugin core classes | Keep | Directly used by `StandardImageHelper.Desktop` and format/plugin path expectations. | Keep; potential boundary tightening in M9b. |
| `Classes/ImageMetadata.cs`, `MetadataTag.cs`, `MetadataModel*.cs`, `PluginRepository.cs` | Defer | Used by wrapper behavior and metadata conversion; high warning density but behavior-sensitive. | Defer cleanup to focused Shine phase. |
| `sources/tools/Stride.FreeImage/System.Drawing/*` + `System.Drawing`-based color conversions in structs | Obsolete/quarantine (candidate) | Compatibility-heavy surface likely larger than clean runtime need; but proof of full unreachability is incomplete. | Keep now; prepare usage proof for future quarantine split. |
| Legacy compatibility interfaces (`IConvertible`, `IFormattable`, `[Serializable]` on many structs) | Obsolete/quarantine (candidate) | Indicates historical .NET compatibility baggage; may be unnecessary for active bridge path. | Do not modify now; target for explicit API-minimization review later. |
| Any replacement of FreeImage with other codecs | Defer | Explicitly out of M9a scope. | Defer to future replacement design track. |

## 8) Changes applied
No product-code changes applied.

Rationale: evidence indicates `Stride.FreeImage` is still active in runtime desktop image load/save path, with a broad legacy wrapper surface that is nontrivial to reduce safely in Sort without deeper callsite proof. Therefore M9a is classification-only to avoid behavior or ABI risk.

## 9) Warning baseline
Focused baseline command produced:
- Focused warning lines (matching `Stride.FreeImage`): **390**.
- Warning code distribution from focused extraction:
  - `CS8625`: 84
  - `CS8604`: 80
  - `CS8600`: 80
  - `CS8618`: 70
  - `CS8603`: 68
  - `CS8602`: 8

No after-pass delta was run because no code changes were made.

Assessment: project is partially warning-cleaned but still high-noise; likely needs structured M9b boundary ordering before Shine.

## 10) Project standard draft (first pass)
### What belongs in `Stride.FreeImage`
- Minimal managed bridge required by active desktop image loading/saving path in `Stride`.
- Required native interop signatures, structs, enums, and handle plumbing needed by current runtime calls.
- Metadata/format helpers demonstrably required by active image decode/encode behaviors.

### What should become future image-codec module material
- Compatibility-heavy color/metadata/system-drawing conversion utilities not required by runtime image path.
- Broad plugin/model helpers used only by tools or historic scenarios.

### What should not be added here
- New general-purpose image processing features.
- New compatibility surface unrelated to current bridge needs.
- New platform-specific assumptions beyond current bridge necessity.

### Replacement candidates (deferred)
- Future codec replacement or split strategy is valid, but out of M9a. Current recommendation is to treat FreeImage as active bridge with planned surface minimization, then replacement design once usage-reduced.

## 11) Validation results
| Command | Exit code | First meaningful warning/error | Pass/Fail | Output truncated |
| --- | ---: | --- | --- | --- |
| `dotnet msbuild striv/projects/Stride.FreeImage/Stride.FreeImage.csproj -getItem:Compile > /tmp/striv-m9a-freeimage-compile.txt` + `wc -l` | 0 | None | Pass | No |
| `find sources/tools/Stride.FreeImage -type f | sort > /tmp/striv-m9a-freeimage-files.txt` + `wc -l` | 0 | None | Pass | No |
| `find sources/tools/Stride.FreeImage -maxdepth 3 -type f | sort` | 0 | None | Pass | No |
| `rg -n ...` reference audit | 0 | None | Pass | No |
| `rg -n ...` native interop audit | 0 | None | Pass | No |
| `rg -n ...` obsolete/compat audit | 0 | None | Pass | No |
| `dotnet build striv/projects/Stride.FreeImage/Stride.FreeImage.csproj -c Debug -p:StriVWarningFocusProject=Stride.FreeImage ...` | 0 | `CS8618` etc in `FreeImageBitmap.cs` | Pass | Yes |
| `dotnet test striv/tests/StriV.AssetTool.Tests/StriV.AssetTool.Tests.csproj -v minimal` | 0 | `CS8604` in `StriV.AssetPipeline` during build | Pass | No |
| `dotnet test striv/tests/StriV.AssetPipeline.Tests/StriV.AssetPipeline.Tests.csproj --no-build -v minimal` | 0 | None emitted | Pass | No |
| `dotnet test striv/tests/StriV.ShaderPipeline.Tests/StriV.ShaderPipeline.Tests.csproj --no-build -v minimal` | 0 | None emitted | Pass | No |
| `dotnet test striv/tests/StriV.CleanGraph.Tests/StriV.CleanGraph.Tests.csproj -v minimal` | 0 | existing warning noise across graph | Pass | Yes |
| `./striv/build/striv-build-core.sh` | 0 | existing warning noise (e.g., `CS1030`, nullable warnings) | Pass | Yes |

## 12) Recommended next step
**Recommend: M9b Set-in-order for FreeImage interop boundary.**

Why:
- Runtime dependency is confirmed active and nontrivial.
- Surface is broad with legacy/compatibility areas mixed with required bridge code.
- A Set-in-order pass should map and partition “runtime-required bridge” vs “tooling/compatibility legacy” before Shine warning work, reducing risk and improving later warning cleanup ROI.
