# M3 clean graph summary

## Executive summary

M3 successfully replaced the inherited Stride build surface for Stri-V Core with a clean SDK-style graph rooted under `striv/`.

The clean graph now builds, tests, and runs CoreSmoke under Xvfb on the Linux/Vulkan-first profile:

- Build: `./striv/build/striv-build-core.sh`
- Tests: `dotnet test striv/tests/StriV.CleanGraph.Tests/StriV.CleanGraph.Tests.csproj --no-build -v minimal`
- Runtime smoke: `xvfb-run -a ./striv/build/striv-run-coresmoke.sh`

`striv/StriV.Core.slnx` is now the primary Stri-V Core solution path.

Old Stride solution/project files and old M1/M2 `.slnf` slices remain in-repo as legacy/reference terrain for compatibility work and source archaeology.

## Reproduction commands

Debug/default:

```bash
./striv/build/striv-build-core.sh
dotnet test striv/tests/StriV.CleanGraph.Tests/StriV.CleanGraph.Tests.csproj --no-build -v minimal
xvfb-run -a ./striv/build/striv-run-coresmoke.sh
```

Release lane (available):

```bash
./striv/build/striv-build-core.sh Release
xvfb-run -a ./striv/build/striv-run-coresmoke.sh Release
```

## Clean graph project spine

Under `striv/projects`:

- bootstrap/tooling
  - `Stride.Core.AssemblyProcessor`
- core
  - `Stride.Core`
  - `Stride.Core.Mathematics`
  - `Stride.Core.IO`
  - `Stride.Core.MicroThreading`
  - `Stride.Core.Serialization`
  - `Stride.Core.Reflection`
- runtime
  - `Stride`
  - `Stride.FreeImage`
  - `Stride.Shaders`
  - `Stride.Graphics`
  - `Stride.Games`
  - `Stride.Input`
  - `Stride.Rendering`
  - `Stride.Engine`
- physics
  - `Stride.BepuPhysics`
- sample
  - `StriV.CoreSmoke`
- tests
  - `StriV.CleanGraph.Tests`

## What was intentionally not inherited

- old Stride custom SDK imports
- legacy pack/payload item groups
- graphics API inner-build machinery
- checked-in `deps/AssemblyProcessor` payload dependency
- `Stride.Shaders.Compiler`
- `Stride.Shaders.Parser`
- CppNet
- audio/native Celt/OpenAL stack
- VirtualReality/native stack
- old `Stride.Physics`
- Game Studio/editor/presentation/Quantum
- legacy source asset compiler/YAML pipeline
- mobile/UWP platform lanes
- Bepu companion modules
- broad old Stride tests/samples

## Important M3 repairs and lessons

- `GenerateAssemblyInfo=false` resolved duplicate attribute emission caused by shared assembly info patterns.
- Clean AssemblyProcessor flow required executable output usage and `--references-file` wiring.
- Android-only `ApkExpansionSupport.cs` was excluded from the Linux profile.
- `Stride.FreeImage` was admitted as a managed wrapper dependency without reviving old native packaging flows.
- `Stride.Shaders` was admitted as a runtime/model assembly while compiler/parser/CppNet stayed excluded.
- `STRIDE_GRAPHICS_NO_DESCRIPTOR_COPIES` was required to align Vulkan source selection.
- `Stride.Rendering` was admitted as a required runtime rendering/model dependency.
- `EffectBytecode` serializer registration issue was resolved by ensuring `Stride.Shaders` assembly load/module initialization.

## Testing doctrine

- Tests are diagnostic oracles, not coverage trophies.
- Current clean tests are aimed at type/linkage/profile invariants and a narrow serializer registration path.
- Broad behavior coverage is intentionally deferred.
- Do not lock in legacy Stride behavior too early.

## Known limitations

- CoreSmoke is intentionally tiny.
- No real scene/content pipeline is validated.
- No asset loading is validated.
- No real material/shader artifact pipeline is validated.
- Shader source compilation remains excluded.
- Audio remains excluded.
- VR remains excluded.
- Old physics remains excluded.
- Clean graph is Linux/Vulkan-first.
- Windows/DX12 profile has not been built in this track.
- Runtime success in this validation uses Xvfb + Mesa llvmpipe in sandbox conditions; local GPU validation remains valuable.
- Serializer still relies on legacy AssemblyProcessor/module-initializer registration in the short term.

## Current TODOs / future tracks

- shader pipeline prep
  - HLSL + Stri-V/SDSL-inspired extensions
  - SPIR-V for Vulkan
  - DXIL/DX12 later
  - reflection metadata
  - CppNet removal/quarantine
- asset pipeline prep
  - low-nesting TOML manifests
  - deterministic textual IDs
  - generated typed C# handles
  - no Game Studio/Quantum dependency
- serialization modernization
  - explicit registrations/source generators
  - reduce reflection/AP dependence
  - preserve only what runtime needs
- audio module
  - OwnAudioSharp/miniaudio/system Opus/OpenAL investigation
  - legacy Celt path quarantined
- VR module
  - optional OpenXR-first restoration later if desired
- System.Numerics / math modernization
  - defer until core stabilizes
- CI/build organization
  - tiny Stri-V-focused workflow around clean graph only

## Primary path

`striv/StriV.Core.slnx` and `striv/build/*` are the primary Stri-V Core development path.

The old Stride solution/project graph remains in the repository as legacy/reference terrain.
New Stri-V Core development should target the clean graph unless explicitly working on
compatibility or source archaeology.

## Recommended next task

Recommended next track: shader pipeline prep.

Reasoning: with the clean runtime/build spine now validated, shader pipeline direction is the highest-leverage prerequisite for moving beyond CoreSmoke into real rendering/content iteration without reintroducing legacy compiler/parser/C++ coupling.

## Validation snapshot (this closeout)

Commands run for this closeout are listed below in the final report. If a future re-run is skipped, retain this section and cite the most recent successful run date in that update.
