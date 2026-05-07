# 1120 — Local source `StriV.Core.slnx` build validation

## 1) Files changed
- `striv/projects/Stride.BepuPhysics/Stride.BepuPhysics.csproj`
- `striv/projects/Stride.Core.AssemblyProcessor/Stride.Core.AssemblyProcessor.csproj`
- `striv/projects/Stride.Core.IO/Stride.Core.IO.csproj`
- `striv/projects/Stride.Core.Mathematics/Stride.Core.Mathematics.csproj`
- `striv/projects/Stride.Core.MicroThreading/Stride.Core.MicroThreading.csproj`
- `striv/projects/Stride.Core.Reflection/Stride.Core.Reflection.csproj`
- `striv/projects/Stride.Core.Serialization/Stride.Core.Serialization.csproj`
- `striv/projects/Stride.Core/Stride.Core.csproj`
- `striv/projects/Stride.Engine/Stride.Engine.csproj`
- `striv/projects/Stride.FreeImage/Stride.FreeImage.csproj`
- `striv/projects/Stride.Games/Stride.Games.csproj`
- `striv/projects/Stride.Graphics/Stride.Graphics.csproj`
- `striv/projects/Stride.Rendering/Stride.Rendering.csproj`
- `striv/projects/Stride.Shaders/Stride.Shaders.csproj`
- `striv/projects/Stride/Stride.csproj`

## 2) Goal recap
This pass moves clean-project compilation ownership toward `striv/projects/**` local copied trees where available, while treating `sources/**` as reference terrain. The task stabilizes the full `striv/StriV.Core.slnx` build against project-local copied code and keeps intentional shared/transitional `sources/**` links only.

## 3) Project audit table
| Project | Local source present? | Old sources compile include before? | Action | Status |
| ------- | --------------------- | ----------------------------------- | ------ | ------ |
| StriV.AssetPipeline | yes | no | no source migration needed | unchanged |
| StriV.AssetTool | yes | no | no source migration needed | unchanged |
| StriV.CoreSmoke | sample path only | no (`samples/**`) | no source migration needed | unchanged |
| StriV.ShaderPipeline | yes | no | no source migration needed | unchanged |
| Stride.BepuPhysics | yes | yes | switched to local source | done |
| Stride.Core.AssemblyProcessor | yes | yes | switched to local source | done |
| Stride.Core.IO | yes | yes | switched to local source | done |
| Stride.Core.Mathematics | yes | yes | switched to local source | done |
| Stride.Core.MicroThreading | yes | yes | switched to local source | done |
| Stride.Core.Reflection | yes | yes | switched to local source | done |
| Stride.Core.Serialization | yes | yes | switched to local source | done |
| Stride.Core | yes | yes | switched to local source | done |
| Stride.Engine | yes | yes | switched to local source | done |
| Stride.FreeImage | yes | yes | switched to local source | done |
| Stride.Games | yes | yes | switched to local source | done |
| Stride.Graphics | yes | yes | switched to local source | done |
| Stride.Input | yes | no (already local) | already local | unchanged |
| Stride.Rendering | yes | yes | switched to local source | done |
| Stride.Shaders | yes | yes | switched to local source | done |
| Stride | yes | yes | switched to local source | done |

## 4) Project file changes
Common shape update for switched projects:
- old shape: `<Compile Include="../../../sources/.../**/*.cs" .../>`
- new shape: `<Compile Include="**/*.cs" Exclude="**/bin/**;**/obj/**" />`

Additional updates:
- Converted `Compile Remove` entries from old `../../../sources/...` paths to local project-relative paths in:
  - `Stride.BepuPhysics`
  - `Stride.Core.IO`
  - `Stride.Core.Mathematics`
  - `Stride.Engine`
  - `Stride.Games`
  - `Stride.Graphics`
  - `Stride.Rendering`
- Preserved intentional linked shared assembly info references (`../../../sources/shared/SharedAssemblyInfo.cs`) where present.
- Preserved transitional linked core files in `Stride.Core.AssemblyProcessor` (specific linked files from `sources/core/Stride.Core/*`).

## 5) Build result
- Command: `dotnet build striv/StriV.Core.slnx -c Debug 2>&1 | tee /tmp/striv-local-source-slnx-build.log`
- Exit code: `0`
- First meaningful warning/error: warning `CS1030` (`#warning: 'PERF: Do not copy byte-for-byte.'`) from `sources/core/Stride.Core/Storage/ObjectIdBuilder.cs` via linked files in `Stride.Core.AssemblyProcessor`.
- Result: **pass**
- Output truncated: **yes** (interactive console output clipped; full log kept at `/tmp/striv-local-source-slnx-build.log`).

## 6) Remaining `sources/**` references
Command:
`rg -n "\.\./\.\./\.\./sources|sources/" striv/projects -g '*.csproj'`

Classification:
- Allowed references:
  - `../../../sources/shared/SharedAssemblyInfo.cs` links in core/engine projects.
- Deferred transitional references:
  - `Stride.Core.AssemblyProcessor.csproj` linked compile items from `sources/core/Stride.Core/*` (explicitly narrow and intentional transitional linkage).
- Suspicious references:
  - none detected after migration.

## 7) Tests/validation
- `dotnet test striv/tests/StriV.AssetTool.Tests/StriV.AssetTool.Tests.csproj -v minimal`
  - exit code: `0`; first warning/error: none; pass; output truncated: no.
- `dotnet test striv/tests/StriV.AssetPipeline.Tests/StriV.AssetPipeline.Tests.csproj --no-build -v minimal`
  - exit code: `0`; first warning/error: none; pass; output truncated: no.
- `dotnet test striv/tests/StriV.ShaderPipeline.Tests/StriV.ShaderPipeline.Tests.csproj --no-build -v minimal`
  - exit code: `0`; first warning/error: one test skipped (`StreamLiveness_DoesNotPruneWhenAccessUnknown`); pass; output truncated: no.
- `dotnet test striv/tests/StriV.CleanGraph.Tests/StriV.CleanGraph.Tests.csproj -v minimal`
  - exit code: `0`; first warning/error: none; pass; output truncated: no.
- `./striv/build/striv-build-core.sh`
  - exit code: `0`; first warning/error: none; pass; output truncated: yes (console clipping while command continued).

## 8) Deferred work
Projects still touching old `sources/**`:
- `Stride.Core.AssemblyProcessor`: explicit linked `Stride.Core` data contract/serialization/core types retained as transitional dependency.

Reason deferred:
- dependency is narrow, intentional, and currently build-stable; replacing with project-local equivalents should be handled as a dedicated follow-up to avoid behavior refactor risk.

Next migration candidates:
- `Stride.Core.AssemblyProcessor` linked `sources/core/Stride.Core/*` items.

## 9) Recommended next task
Proceed with **`Stride.Input` 5S Sort** on the local copied tree unless a higher-priority blocker emerges from additional assembly-processor ownership migration.
