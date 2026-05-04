# M6b Engine/Rendering Axing Validation

## 1) Files changed

- `striv/projects/Stride.Engine/Stride.Engine.csproj`
- `striv/projects/Stride.Rendering/Stride.Rendering.csproj`
- `docs/stri-v/audits/910-m6b-engine-rendering-axing-validation.md`

## 2) Scope recap

This pass targeted only clean-project compile graph shaping for:

- `Stride.Engine`
- `Stride.Rendering`

Constraints followed:

- No source files deleted.
- No legacy upstream project files modified.
- Only clean project exclusions were added.
- Platform doctrine preserved: Linux+Windows desktop intent, Vulkan-first, RawInput retained, Direct3D not re-enabled.

## 3) Baseline method

Commands run:

```bash
./striv/build/striv-warning-baseline.sh --log /tmp/striv-m6b-before.log
```

Result (from baseline helper output):

- Build exit code: `0`
- Build-summary warning count: `2621`
- Extracted warning lines: `5472`
- Top warning codes:
  - `CS8618: 2066`
  - `CS8625: 940`
  - `CS8604: 500`
  - `CS8600: 492`
  - `CS8603: 366`
- Top warning projects:
  - `Stride.Rendering: 1650`
  - `Stride.Engine: 982`
  - `Stride.Graphics: 886`
  - `Stride.FreeImage: 390`
  - `Stride.Games: 292`

Build-state note:

- The baseline helper run compiled broadly (non-zero warning-rich output, not a warm/zero-warning short-circuit).

## 4) Compile item audit

Commands run:

```bash
dotnet msbuild striv/projects/Stride.Engine/Stride.Engine.csproj -getItem:Compile > /tmp/striv-m6b-engine-compile.txt
dotnet msbuild striv/projects/Stride.Rendering/Stride.Rendering.csproj -getItem:Compile > /tmp/striv-m6b-rendering-compile.txt
rg -n "VirtualReality|OpenVR|OpenXR|VR|Audio|Sound|Media|Video|ShaderCompiler|Shaders.Compiler|EffectCompiler|AssetCompiler|GameStudio|Quantum|Editor|Presentation|UWP|WindowsStore|Android|iOS|Direct3D|SharpDX|SpriteStudio" striv/projects/Stride.Engine striv/projects/Stride.Rendering sources/engine/Stride.Engine sources/engine/Stride.Rendering
grep -Ei "VirtualReality|OpenVR|OpenXR|VR|Audio|Sound|Media|Video|ShaderCompiler|Shaders.Compiler|EffectCompiler|AssetCompiler|GameStudio|Quantum|Editor|Presentation|UWP|WindowsStore|Android|iOS|Direct3D|SharpDX|SpriteStudio" /tmp/striv-m6b-engine-compile.txt || true
grep -Ei "VirtualReality|OpenVR|OpenXR|VR|Audio|Sound|Media|Video|ShaderCompiler|Shaders.Compiler|EffectCompiler|AssetCompiler|GameStudio|Quantum|Editor|Presentation|UWP|WindowsStore|Android|iOS|Direct3D|SharpDX|SpriteStudio" /tmp/striv-m6b-rendering-compile.txt || true
```

### Stride.Engine

Suspicious included paths found:

- `Rendering/Compositing/EditorTopLevelCompositor.cs`
- `Rendering/Compositing/ForwardRenderer.VRUtils.cs`

Paths excluded in this pass:

- `Rendering/Compositing/EditorTopLevelCompositor.cs`
- `Rendering/Compositing/ForwardRenderer.VRUtils.cs`

Suspicious paths left alone and why:

- Existing audio and VR removes were already present and retained.
- No additional mobile/UWP/editor folder-level removes were added due to mixed runtime content risk outside clearly scoped files.

### Stride.Rendering

Suspicious included paths found:

- `Rendering/Editor/**/*.cs` (multiple editor shader/generated files)
- `Rendering/Shaders/GlobalVR.sdsl.cs`
- `Shaders.Compiler/EffectCompileRequest.cs`

Paths excluded in this pass:

- `Rendering/Editor/**/*.cs`
- `Rendering/Shaders/GlobalVR.sdsl.cs`

Suspicious paths left alone and why:

- `Shaders.Compiler/EffectCompileRequest.cs` was left included due to potential runtime shader/effect compile plumbing use; exclusion risk judged non-trivial without deeper dependency proof.
- Non-editor rendering abstractions and runtime renderer paths were not excluded by policy.

## 5) Exclusions implemented

| Project | Exclusion | Reason | Risk |
| ------- | --------- | ------ | ---- |
| `Stride.Engine` | `Rendering/Compositing/EditorTopLevelCompositor.cs` | Editor-specific compositor glue is out-of-scope for clean runtime core. | Low |
| `Stride.Engine` | `Rendering/Compositing/ForwardRenderer.VRUtils.cs` | VR-specific helper path; VR excluded for Stri-V core. | Low/Medium (partial-class coupling risk) |
| `Stride.Rendering` | `Rendering/Editor/**/*.cs` | Editor/Game Studio presentation shader path out-of-scope. | Low |
| `Stride.Rendering` | `Rendering/Shaders/GlobalVR.sdsl.cs` | VR-specific shader key plumbing out-of-scope. | Low/Medium |

## 6) Warning impact

After-edit validation commands:

```bash
./striv/build/striv-build-core.sh 2>&1 | tee /tmp/striv-m6b-after.log
grep -E "warning (CS|CA|NU|STRIDE)[0-9]+" /tmp/striv-m6b-after.log > /tmp/striv-m6b-warning-lines-after.log || true
wc -l /tmp/striv-m6b-warning-lines-after.log
sed -E 's/.*warning ((CS|CA|NU|STRIDE)[0-9]+).*/\1/' /tmp/striv-m6b-warning-lines-after.log | sort | uniq -c | sort -nr | head -n 40
grep -Eo "\[[^]]+\.csproj\]" /tmp/striv-m6b-warning-lines-after.log | sort | uniq -c | sort -nr | head -n 40
```

Reported:

- After summary warnings: `1333`
- After extracted warning lines: `2664`
- Top warning codes after:
  - `CS8618: 1198`
  - `CS8625: 456`
  - `CS8600: 252`
  - `CS8604: 162`
  - `CS8603: 132`
- Top warning projects after:
  - `Stride.Rendering: 1650`
  - `Stride.Engine: 980`
  - `Stride.BepuPhysics: 34`

Comparability caveat:

- Before used canonical baseline helper and produced the canonical high count (`2621/5472`).
- After used direct `striv-build-core.sh` in a warmed/incremental state; warning counts are not directly comparable across differing cache/build warmth states (per methodology report 900).
- Project-local signal indicates a small reduction in `Stride.Engine` warning lines (`982 -> 980`) and unchanged `Stride.Rendering` extracted project line count in this warmed run (`1650`).

## 7) Build/test validation

| Command | Exit code | First meaningful warning/error | Pass/Fail | Output truncated |
| --- | ---: | --- | --- | --- |
| `./striv/build/striv-warning-baseline.sh --log /tmp/striv-m6b-before.log` | 0 | First warning observed: `CS1030` in `ObjectIdBuilder.cs` | Pass | No (tool output complete) |
| `dotnet msbuild striv/projects/Stride.Engine/Stride.Engine.csproj -getItem:Compile > /tmp/striv-m6b-engine-compile.txt` | 0 | None | Pass | No |
| `dotnet msbuild striv/projects/Stride.Rendering/Stride.Rendering.csproj -getItem:Compile > /tmp/striv-m6b-rendering-compile.txt` | 0 | None | Pass | No |
| `./striv/build/striv-build-core.sh 2>&1 | tee /tmp/striv-m6b-after.log` | 0 | First warning observed: `CS0436` in `Stride.Rendering/Properties/AssemblyInfo.cs` | Pass | No (saved to log) |
| `dotnet test striv/tests/StriV.AssetTool.Tests/StriV.AssetTool.Tests.csproj -v minimal` | 0 | None | Pass | No |
| `dotnet test striv/tests/StriV.AssetPipeline.Tests/StriV.AssetPipeline.Tests.csproj --no-build -v minimal` | 0 | None | Pass | No |
| `dotnet test striv/tests/StriV.ShaderPipeline.Tests/StriV.ShaderPipeline.Tests.csproj --no-build -v minimal` | 0 | No errors; one expected skipped test | Pass | No |
| `dotnet test striv/tests/StriV.CleanGraph.Tests/StriV.CleanGraph.Tests.csproj --no-build -v minimal` | 0 | None | Pass | No |

## 8) First blocker

No blocker encountered. Build and all requested tests passed.

## 9) Deferred exclusions

Deferred as suspicious but ambiguous/risky:

- `sources/engine/Stride.Rendering/Shaders.Compiler/EffectCompileRequest.cs`
  - Rationale: likely connected to runtime dynamic effect compilation paths; exclusion could break shader/effect runtime behavior.
- Any broad `Shaders.Compiler/**` folder removal in `Stride.Rendering`
  - Rationale: insufficient evidence in this pass that entire folder is non-runtime for clean core.

## 10) Recommended next task

Recommended next task: **another axing pass** focused on tightly-scoped, evidence-backed exclusions in `Stride.Rendering` (especially remaining non-runtime/editor-adjacent or optional integration islands), while keeping canonical-before/after runs in comparable build-state conditions.
