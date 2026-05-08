# Stri-V Core M1a bootstrap build

## Current primary path

As of M3, the clean SDK-style graph under `striv/` is the primary Stri-V Core path.

Build:
```bash
./striv/build/striv-build-core.sh
```

Test:
```bash
dotnet test striv/tests/StriV.CleanGraph.Tests/StriV.CleanGraph.Tests.csproj -v minimal
```

## Focused project warning lane

Build one project with inactive warning noise suppressed:

```bash
./striv/build/striv-build-focused-project.sh Stride.BepuPhysics
```

Require a focused project to be warning-clean:

```bash
./striv/build/striv-check-focused-project.sh Stride.Input
```

Run all completed active focused warning-clean projects as a build/script gate:

```bash
./striv/build/striv-check-focused-projects.sh \
  Stride.BepuPhysics \
  Stride.Core.Mathematics \
  Stride.Core.IO \
  Stride.Input
```

The focused warning lane is a 5S Shine/Sustain build-quality gate. It does not mark inactive project warnings as fixed, and it must run outside `dotnet test` (unit tests must not spawn nested focused builds).

Completed zero-warning focused active projects:
- `Stride.BepuPhysics`
- `Stride.Core.Mathematics`
- `Stride.Core.IO`
- `Stride.Input`

Legacy bridge exception (policy exception, nullable disabled):
- `Stride.FreeImage`

Run smoke:
```bash
xvfb-run -a ./striv/build/striv-run-coresmoke.sh
```

## What Stri-V Core M1a is

Stri-V Core M1a is the current foundational core-only build slice for the Stri-V hardfork. It is intentionally minimal and is scoped to foundational projects collected in `build/StriV.Core.M1a.slnf`.

## Why AssemblyProcessor is source-built for Stri-V Core

For Stri-V Core, `Stride.Core.AssemblyProcessor` is treated as a bootstrap build tool. The bootstrap flow is:

1. Build AssemblyProcessor from source.
2. Validate that output as a real managed PE assembly.
3. Route the M1a build to that output with command-line properties.

This avoids hidden reliance on checked-in binary payloads and makes local/CI behavior repeatable.

## Why `deps/AssemblyProcessor/*` is legacy/non-authoritative

Stri-V Core bootstrap builds do not treat checked-in `deps/AssemblyProcessor/*` payloads as authoritative inputs. Those payloads can be stale or LFS pointer content, and they are outside the intended source-built bootstrap path.

## Build commands

### Linux (Debug default)

```bash
./build/striv-build-core-m1a.sh
```

### Windows PowerShell (Debug default)

```powershell
.\build\striv-build-core-m1a.ps1
```

## Optional Release builds

Linux:

```bash
./build/striv-build-core-m1a.sh Release
```

Windows:

```powershell
.\build\striv-build-core-m1a.ps1 -Configuration Release
```

Both scripts also forward additional arguments to the M1a `dotnet build` invocation.

## Stri-V Engine Foundation M1b

M1b extends M1a by adding `sources/engine/Stride/Stride.csproj` to the validated slice via `build/StriV.EngineFoundation.M1b.slnf`.

What M1b proves:
- The six foundational managed core projects from M1a still build with source-built AssemblyProcessor routing.
- The base `Stride` engine project can be built in the same bootstrap flow.

What M1b intentionally excludes:
- `sources/engine/Stride.Engine/Stride.Engine.csproj`.
- Explicit rendering, graphics, windowing, input, audio, physics, shader compiler/parser, and editor/tooling stacks.

### Linux (Debug default)

```bash
./build/striv-build-engine-foundation-m1b.sh
```

### Windows PowerShell (Debug default)

```powershell
.\build\striv-build-engine-foundation-m1b.ps1
```

### Optional Release builds

Linux:

```bash
./build/striv-build-engine-foundation-m1b.sh Release
```

Windows:

```powershell
.\build\striv-build-engine-foundation-m1b.ps1 -Configuration Release
```

Both scripts also forward additional arguments to the M1b `dotnet build` invocation.

### M1b limitations

- M1b is an engine-foundation slice only, not a full engine runtime validation.
- It does **not** prove graphics/window/input/rendering/audio/physics/editor readiness.
- Native/tooling transitive dependencies (for example through FreeImage) may still produce platform-specific issues.

### M1b troubleshooting

- **FreeImage/native dependency issues**
  - `Stride` references `Stride.FreeImage`; if native payloads are missing or incompatible, restore/build can fail.

- **Transitive project not included in `.slnf`**
  - If restore/build reports a missing project in the filtered graph, add only the required transitive project and rerun.

- **AssemblyProcessor routing/property issues**
  - Ensure `StrideAssemblyProcessorFramework=net10.0`, `StrideAssemblyProcessorBasePath=<absolute path with trailing slash>`, and `StrideAssemblyProcessorHash=sourcebuild` are being passed.

## Stri-V Platform + Graphics Basics M1c

M1c extends M1b by adding:
- `sources/engine/Stride.Games/Stride.Games.csproj`
- `sources/engine/Stride.Graphics/Stride.Graphics.csproj`

This is the first platform + graphics basics compile slice, validated through `build/StriV.PlatformGraphicsBasics.M1c.slnf`.

What M1c intentionally defers/excludes:
- `sources/engine/Stride.Input/Stride.Input.csproj` is intentionally deferred.
- `sources/engine/Stride.Engine/Stride.Engine.csproj` is intentionally excluded.
- This slice still does **not** prove rendering runtime behavior, shader compilation pipelines, input, audio, physics, editor/tooling, or asset compiler readiness.

### Linux (Debug default)

```bash
./build/striv-build-platform-graphics-basics-m1c.sh
```

### Windows PowerShell (Debug default)

```powershell
.\build\striv-build-platform-graphics-basics-m1c.ps1
```

### Optional Release builds

Linux:

```bash
./build/striv-build-platform-graphics-basics-m1c.sh Release
```

Windows:

```powershell
.\build\striv-build-platform-graphics-basics-m1c.ps1 -Configuration Release
```

Both scripts also forward additional arguments to the M1c `dotnet build` invocation.

### M1c troubleshooting

- **SDL symbol / `STRIDE_UI_SDL` issues**
  - If SDL-backed code paths are unexpectedly activated or undefined symbols appear, verify platform/symbol conditions for Linux-only M1c routing.

- **Vulkan package/native dependency issues**
  - M1c uses `StrideGraphicsApis=Vulkan`; missing/incompatible Vulkan packages or native dependencies can fail restore/build.

- **freetype/native payload issues**
  - `Stride.Graphics` transitive native payload requirements can fail if freetype-related artifacts are missing or incompatible.

- **D3D package conditions accidentally firing**
  - If D3D/Windows-only package conditions activate under Linux M1c, inspect effective properties and target conditions.

- **Transitive project not included in `.slnf`**
  - If restore/build reports a missing filtered project, add only the required transitive project and rerun.

- **AssemblyProcessor routing/property issues**
  - Ensure:
    - `StrideAssemblyProcessorFramework=net10.0`
    - `StrideAssemblyProcessorBasePath=<absolute path with trailing slash>`
    - `StrideAssemblyProcessorHash=sourcebuild`

## Optional DXC shader backend validation

`StriV.ShaderPipeline` tests can optionally compile lowered HLSL with DXC when `dxc`
is available on PATH. DXC is not required for normal clean graph builds/tests.

Probe:

```bash
./striv/build/striv-probe-dxc.sh
```

When DXC is unavailable, shader backend compile-smoke tests skip/early-return and the
test suite remains green. To enable SPIR-V validation, install DXC from a trusted source
such as the official Microsoft DirectXShaderCompiler releases or a Vulkan SDK-provided
toolchain, then ensure `dxc` is on PATH.

## Current limitations

- M1a is foundational core only, not a full engine build.
- This path does not prove windowing, rendering, input, audio, physics, asset compiler, or editor readiness.
- Asset compiler and editor remain outside this bootstrap path.

## Troubleshooting

- **AssemblyProcessor DLL is a Git LFS pointer**
  - The scripts fail if the output starts with `version https://git-lfs.github`.
  - Ensure AssemblyProcessor built from source and produced a real assembly.

- **Missing .NET 10 SDK**
  - Install a .NET 10 SDK that can build `net10.0` targets, then rerun.

- **M1a build fails after AssemblyProcessor loads**
  - Review the first failure in `dotnet build` output; this script only establishes the bootstrap path, not all downstream compatibility issues.

- **Path/trailing slash issues**
  - The scripts pass `StrideAssemblyProcessorBasePath` as an absolute path with a trailing slash, because the targets expect hash + path composition behavior.

## Next milestones

- Add engine foundation slice.
- Add SDL/window/input slice.
- Add Vulkan graphics slice.

## Stri-V Input M1d

M1d extends M1c by adding:
- `sources/engine/Stride.Input/Stride.Input.csproj`

This is an input compile-validation slice via `build/StriV.Input.M1d.slnf`.

What M1d intentionally defers/excludes:
- `sources/engine/Stride.Engine/Stride.Engine.csproj` remains intentionally excluded.
- Rendering runtime, audio, physics, editor/tooling, and asset compiler flows are still not proven in M1d.
- M1d validates compile/build graph only; it does not validate runtime input devices.

### Linux (Debug default)

```bash
./build/striv-build-input-m1d.sh
```

### Windows PowerShell (Debug default)

```powershell
.\build\striv-build-input-m1d.ps1
```

### Optional Release builds

Linux:

```bash
./build/striv-build-input-m1d.sh Release
```

Windows:

```powershell
.\build\striv-build-input-m1d.ps1 -Configuration Release
```

Both scripts also forward additional arguments to the M1d `dotnet build` invocation.

### M1d troubleshooting

- **Accidental `net10.0-windows` activation**
  - Ensure effective target framework/platform properties stay Linux-first.

- **Windows-only input packages active on Linux**
  - Inspect conditional package references and effective MSBuild conditions.

- **`STRIDE_UI_SDL` not selected**
  - Verify platform symbol routing for Linux/SDL code paths.

- **WinForms/UWP/mobile files compiling unexpectedly**
  - Re-check target conditions and include/exclude item logic.

- **SDL/gamepad/native dependency issues**
  - Validate native/input dependency availability and compatibility for Linux.

- **Transitive project not included in `.slnf`**
  - If restore/build reports a missing filtered project, add only the required transitive project and rerun.

- **AssemblyProcessor routing/property issues**
  - Ensure:
    - `StrideAssemblyProcessorFramework=net10.0`
    - `StrideAssemblyProcessorBasePath=<absolute path with trailing slash>`
    - `StrideAssemblyProcessorHash=sourcebuild`

## Stri-V Engine M1e

M1e extends M1d by adding:
- `sources/engine/Stride.Engine/Stride.Engine.csproj`

This is the next engine-admission compile-validation slice via `build/StriV.Engine.M1e.slnf`.

What M1e intentionally defers/excludes:
- `sources/engine/Stride.BepuPhysics/**` is intentionally deferred until after `Stride.Engine` admission.
- Legacy `Stride.Physics` is intentionally excluded.
- VR is not considered part of Stri-V Core; if `Stride.VirtualReality` blocks this slice, that blocker should be isolated and handled through a minimal later Stri-V-specific exclusion/condition.
- M1e is compile validation only; it does not validate runtime scene/game behavior.

### Linux (Debug default)

```bash
./build/striv-build-engine-m1e.sh
```

### Windows PowerShell (Debug default)

```powershell
.\build\striv-build-engine-m1e.ps1
```

### Optional Release builds

Linux:

```bash
./build/striv-build-engine-m1e.sh Release
```

Windows:

```powershell
.\build\striv-build-engine-m1e.ps1 -Configuration Release
```

Both scripts also forward additional arguments to the M1e `dotnet build` invocation.

### M1e troubleshooting

- **`Stride.VirtualReality` native/package blockers**
  - If VR-specific restore/build blockers appear, isolate and report the exact first blocker rather than repairing VR in M1e.

- **Shader compiler/parser/toolchain blockers**
  - If shader compiler/parser toolchain dependencies fail, capture the first meaningful error and isolate for follow-up.

- **Audio native dependency blockers**
  - If `Stride.Audio` transitive native dependencies fail, report exact package/project/target failure details.

- **Rendering dependency blockers**
  - If `Stride.Rendering` transitive dependencies block build, report the first meaningful rendering-related error.

- **`StridePackAssets` or asset-related leakage**
  - If asset packing/compiler targets leak into M1e, report the triggering target/project and condition.

- **Transitive project not included in `.slnf`**
  - If restore/build reports a missing filtered project, add only the required transitive project and rerun.

- **AssemblyProcessor routing/property issues**
  - Ensure:
    - `StrideAssemblyProcessorFramework=net10.0`
    - `StrideAssemblyProcessorBasePath=<absolute path with trailing slash>`
    - `StrideAssemblyProcessorHash=sourcebuild`

## Stri-V Engine M1e

M1e admits `sources/engine/Stride.Engine/Stride.Engine.csproj` through `build/StriV.Engine.M1e.slnf` and keeps shader source compiler integration optional.

For Stri-V Core builds, M1e passes:

- `-p:StrideIncludeShaderCompiler=false`

This excludes the legacy shader source compiler integration path from `Stride.Engine` so M1e compile validation is not blocked by the legacy CppNet/SDSL source shader compiler dependency. In addition to excluding the external `Stride.Shaders.Compiler` project reference, M1e now also conditionally excludes `Stride.Engine/Shaders.Compiler/*.cs` engine-local integration sources when `StrideIncludeShaderCompiler=false`.

This is compile-slice isolation only; it does not claim runtime shader compilation behavior is solved.

## TODO: Shader pipeline modernization

CppNet and the legacy SDSL shader preprocessing/compiler path are not part of Stri-V Core.
They remain legacy tooling for now and should eventually be removed, replaced, or quarantined
behind optional compatibility tooling once the runtime shader/effect artifact boundary is defined.


## TODO: Audio/native stack

**Audio is intentionally excluded from Stri-V Core M1e.**
The current Stride audio path depends on native payloads such as `libCelt.a`
through `NativePath`; in this checkout that artifact appears as a Git LFS
pointer rather than a valid static library. Audio should be restored later
as its own optional module slice after a dedicated native audio audit.

Future work should evaluate:
- whether to hydrate/rebuild the existing native payloads,
- whether to replace legacy Celt/custom Opus usage with a system Opus/OpenAL path,
- how audio components/systems should be modularized outside the engine core.


## TODO: VirtualReality/native stack

**VirtualReality is intentionally excluded from Stri-V Core M1e.**
The current Stride VR path depends on native/runtime payloads through `NativePath`
and platform VR stacks such as OpenVR/OpenXR. It is not part of the core runtime
spine and should be restored later only as an optional extension module after a
dedicated VR/native dependency audit.


## Stri-V Engine M1e (current no-VR guard path)

M1e introduces `sources/engine/Stride.Engine/Stride.Engine.csproj` with optional integrations:
- `StrideIncludeShaderCompiler=false`
- `StrideIncludeAudio=false`
- `StrideIncludeVirtualReality=false`

When `StrideIncludeVirtualReality=false`, Stri-V now excludes VR-only compositor source files from `Stride.Engine` compilation in addition to conditioning out the `Stride.VirtualReality` project reference. This is a source-level compile isolation step only.

**TODO (VR):** Stri-V Core still does **not** include or validate VR runtime support (OpenVR/OpenXR/device/native paths).

When `StrideIncludeAudio=false`, Stri-V also excludes engine-level audio component source files (`Engine/AudioEmitterComponent.cs` and `Engine/AudioListenerComponent.cs`) in addition to conditioning out audio project references/systems.

## Stri-V Engine Bepu M1f

M1f extends M1e by adding:
- `sources/engine/Stride.BepuPhysics/Stride.BepuPhysics/Stride.BepuPhysics.csproj`

This is the first BepuPhysics admission compile-validation slice via `build/StriV.Engine.Bepu.M1f.slnf`.

What M1f intentionally defers/excludes:
- Legacy `sources/engine/Stride.Physics*` is intentionally excluded.
- Bepu companion modules are intentionally deferred:
  - `Stride.BepuPhysics.Debug`
  - `Stride.BepuPhysics.Navigation`
  - `Stride.BepuPhysics.Soft`
  - `Stride.BepuPhysics._2D`
  - `Stride.BepuPhysics.Tests`
- Bepu samples/tests are intentionally deferred.
- M1f is compile validation only; it does not validate physics runtime behavior.

### Linux (Debug default)

```bash
./build/striv-build-engine-bepu-m1f.sh
```

### Windows PowerShell (Debug default)

```powershell
.\build\striv-build-engine-bepu-m1f.ps1
```

### Optional Release builds

Linux:

```bash
./build/striv-build-engine-bepu-m1f.sh Release
```

Windows:

```powershell
.\build\striv-build-engine-bepu-m1f.ps1 -Configuration Release
```

Both scripts also forward additional arguments to the M1f `dotnet build` invocation.

### M1f troubleshooting

- **Old `Stride.Physics` accidentally pulled**
  - If restore/build logs include `sources/engine/Stride.Physics*`, report the first introducing project/target and isolate that dependency edge.

- **Bepu companion module accidentally pulled**
  - If restore/build logs include `Stride.BepuPhysics.Debug`, `Navigation`, `Soft`, `_2D`, or `Tests`, isolate the first introducing project/target.

- **Rendering/gizmo API compile blockers**
  - If `Stride.BepuPhysics` fails in gizmo/debug rendering integration code, capture the first meaningful compile error and isolate for follow-up.

- **Bepu package restore issues**
  - If `BepuPhysics` NuGet restore fails or warns on incompatible versions, capture package/version/error details and isolate.

- **AssemblyProcessor routing/property issues**
  - Ensure:
    - `StrideAssemblyProcessorFramework=net10.0`
    - `StrideAssemblyProcessorBasePath=<absolute path with trailing slash>`
    - `StrideAssemblyProcessorHash=sourcebuild`

## Stri-V CoreSmoke M1g

M1g extends the validated M1f Engine+Bepu compile spine by adding a tiny code-first executable smoke project at `samples/StriV/CoreSmoke/StriV.CoreSmoke.csproj`.

What M1g adds over M1f:
- A compile-only executable entry point that references runtime engine code.
- A focused smoke slice for executable graph closure without introducing content pipelines.

What M1g intentionally excludes:
- No assets (`.sdpkg`, `.sdscene`, `.sdproj`) and no source YAML/package sessions.
- No Game Studio/editor/presentation or asset compiler/project metadata.
- No shader source compiler path (`StrideIncludeShaderCompiler=false`).
- No audio/native audio stack (`StrideIncludeAudio=false`).
- No VR/native VR stack (`StrideIncludeVirtualReality=false`).
- Runtime execution is not validated by default in this milestone (build-only validation).

### Linux (Debug default)

```bash
./build/striv-build-coresmoke-m1g.sh
```

### Windows PowerShell (Debug default)

```powershell
.\build\striv-build-coresmoke-m1g.ps1
```

### Optional Release builds

Linux:

```bash
./build/striv-build-coresmoke-m1g.sh Release
```

Windows:

```powershell
.\build\striv-build-coresmoke-m1g.ps1 -Configuration Release
```

Both scripts forward additional arguments to the M1g `dotnet build` invocation.

### M1g troubleshooting

- **Executable accidentally pulls assets/editor packages**
  - Ensure the smoke project references runtime projects only (for example `Stride.Engine`) and does not reference asset/editor packages.

- **`net10.0-windows` accidentally used**
  - Keep the smoke project target framework at `net10.0` for Linux-first compile validation.

- **Build-only M1g is not runtime proof**
  - A successful M1g build does not prove runtime/window execution behavior.

- **SDL/Vulkan runtime issues**
  - SDL/Vulkan environment/runtime execution concerns are deferred to a later runtime-smoke milestone.

## Stri-V CoreSmoke Runtime Smoke M1h

M1h adds an **opt-in runtime smoke** on top of the M1g compile slice. M1g remains build-only validation; M1h adds a tiny code-first runtime attempt that uses `Stride.Engine.Game`, enters the game loop, and self-exits on the first update frame.

M1h runtime smoke characteristics:
- Build-first flow: runtime script calls the M1g build script before launch.
- Self-exiting game loop (first-frame `Exit()`), to avoid indefinite run behavior.
- Opt-in only; it is intentionally separate from the M1g build script.
- No assets, no `.sdpkg`, `.sdscene`, `.sdproj`, and no Game Studio/editor YAML content paths.
- Shader compiler remains disabled.
- Audio remains disabled.
- VR remains disabled.
- Runtime smoke is environment-sensitive (display server, SDL, Vulkan loader/device/native runtime availability).

### Linux (Debug default)

```bash
./build/striv-run-coresmoke-m1h.sh
```

### Windows PowerShell (Debug default)

```powershell
.\build\striv-run-coresmoke-m1h.ps1
```

### Optional Release usage

Linux:

```bash
./build/striv-run-coresmoke-m1h.sh Release
```

Windows:

```powershell
.\build\striv-run-coresmoke-m1h.ps1 -Configuration Release
```

### M1h headless/offscreen probe

The official runtime smoke expects a real desktop display + Vulkan runtime. In headless containers, the Linux run script supports experimental SDL video-driver probes:

```bash
./build/striv-run-coresmoke-m1h.sh --sdl-video-driver dummy
./build/striv-run-coresmoke-m1h.sh --sdl-video-driver offscreen
```

You can combine configuration + probe mode:

```bash
./build/striv-run-coresmoke-m1h.sh Release --sdl-video-driver dummy
```

PowerShell exposes a matching optional parameter:

```powershell
.\build\striv-run-coresmoke-m1h.ps1 -SdlVideoDriver dummy
```

These probes are diagnostic only. Failure under dummy/offscreen does not necessarily indicate a Stri-V runtime bug; local desktop validation remains authoritative.

### M1h troubleshooting

- **SDL display unavailable**
  - Example: `Cannot allocate SDL Window: x11 not available`.
  - Classify as environment limitation (headless/sandbox display stack).

- **Vulkan loader/ICD/device unavailable**
  - Classify as environment limitation when loader/ICD/device creation cannot be established.

- **Missing native libraries**
  - Classify as environment/native packaging blocker.

- **Sandbox/headless container limitations**
  - Runtime smoke can fail in CI/sandbox even when it succeeds on a local developer machine.

- **Runtime graphics device creation failure**
  - Usually environment-dependent first (display/driver/ICD); verify locally on a graphics-capable machine.

- **Run succeeds locally but not in sandbox**
  - Treat sandbox runtime result as non-authoritative for graphics/device readiness.

## M1 golden path closeout

For the M1 closeout summary and curated project spine, see:
- `docs/stri-v/audits/350-m1-golden-path-summary.md`
- `build/StriV.Core.slnx`

These are the forward-looking Stri-V Core organizational artifacts; legacy `Stride.sln` and historical `.slnf` slices remain in place.

## Visual Studio / local development

`build/StriV.Core.slnx` is the primary Stri-V Core developer solution.
`build/Stride.sln` remains legacy/reference terrain in this hardfork and is no longer the build-authoritative local-dev entry point for Stri-V Core work.

Before opening it in Visual Studio, run:

```powershell
.\build\striv-vs-prepare-core.ps1
```

This source-builds AssemblyProcessor and restores the solution with the Stri-V Core profile:

- Linux
- Vulkan
- shader compiler disabled
- audio disabled
- VR disabled
- source-built AssemblyProcessor

Options:

- Release prep:

  ```powershell
  .\build\striv-vs-prepare-core.ps1 -Configuration Release
  ```

- Optional CLI build after restore:

  ```powershell
  .\build\striv-vs-prepare-core.ps1 -Build
  ```

- Print the effective repo-visible Stri-V profile on a project:

  ```powershell
  .\build\striv-print-core-profile.ps1
  ```

Notes:

- The Stri-V Core profile is now imported repo-wide during normal project evaluation via root `Directory.Build.props`, so Visual Studio design-time restore/build sees the same Linux/Vulkan/no-shader-compiler/no-audio/no-VR defaults as the CLI unless you explicitly override them.
- Run the prep script before opening Visual Studio so the source-built AssemblyProcessor output exists for the imported profile to consume.
- If Visual Studio still shows stale errors, close it fully, delete affected `obj`/`bin` folders for the impacted Stri-V projects, rerun prep, and reopen the solution.
- Validate final Visual Studio behavior locally on Windows.
- Do not rely on `deps/AssemblyProcessor/*` payloads for Stri-V Core development.
- The CLI golden path remains authoritative even though the Stri-V Core profile is now repo-visible for local development.

## Clean M3 Core graph (striv/)

Use the clean SDK-style graph without Stride custom SDK imports:

```bash
./striv/build/striv-build-core.sh
```

Key artifacts:
- `striv/StriV.Core.slnx`
- `striv/build/StriV.Core.Profile.props`
- `striv/build/StriV.AssemblyProcessor.targets`
- `striv/projects/*/*.csproj`

Notes:
- The clean graph uses explicit source globs into existing `sources/` and `samples/` trees.
- `Stride.Core.AssemblyProcessor` is source-built in-graph and no `deps/AssemblyProcessor` payload is consumed.

## Clean graph runtime smoke

Build clean graph:

```bash
./striv/build/striv-build-core.sh
```

Run CoreSmoke normally:

```bash
./striv/build/striv-run-coresmoke.sh
```

Run in headless sandbox:

```bash
xvfb-run -a ./striv/build/striv-run-coresmoke.sh
```

Release:

```bash
xvfb-run -a ./striv/build/striv-run-coresmoke.sh Release
```

Notes:
- The clean graph under `striv/` is the preferred M3+ path.
- Shader compiler, audio, and VR remain excluded in this runtime validation path.
- `StriV.CoreSmoke` is intentionally minimal and self-exiting.
- `xvfb-run` uses a software/Mesa Vulkan path in sandbox-style Linux environments.
- Local GPU runtime validation is still useful beyond sandbox/Xvfb coverage.

## TODO: Serialization modernization

The clean graph currently preserves the minimum legacy serializer registration behavior needed
for runtime startup. Stri-V should later replace or reduce broad reflection/AP-dependent
serialization with explicit registrations/source generation where practical, especially once
the TOML asset manifest and shader artifact pipeline are designed.

## Asset tool

`StriV.AssetTool` currently supports **shader assets only**. Asset manifests are flat TOML source-of-intent inputs; generated JSON manifests, lowered/generated HLSL, optional SPIR-V binaries, and compiler logs are output artifacts.

Build shader assets from a flat TOML manifest:

```bash
dotnet run --project striv/projects/StriV.AssetTool/StriV.AssetTool.csproj -- build-assets \
  --manifest striv/tests/fixtures/assets/shader_manifest/assets.toml \
  --output /tmp/striv-assets
```

Quiet mode:

```bash
dotnet run --project striv/projects/StriV.AssetTool/StriV.AssetTool.csproj -- build-assets \
  --manifest <assets.toml> \
  --output <output-dir> \
  --quiet
```

JSONL diagnostics / CI mode:

```bash
dotnet run --project striv/projects/StriV.AssetTool/StriV.AssetTool.csproj -- build-assets \
  --manifest <assets.toml> \
  --output <output-dir> \
  --diagnostics jsonl
```

Exit codes:

- `0`: success / no fatal diagnostics
- `1`: fatal parse/validation/build diagnostics
- `2`: missing manifest path

DXC behavior:

- DXC is optional by default; missing DXC does not fail asset builds unless strict mode is requested.
- `--strict-dxc` treats unavailable/failed DXC stages as fatal.
- `--no-dxc` disables DXC emission.

Helper script wrapper (bash):

```bash
./striv/build/striv-build-assets.sh \
  --manifest striv/tests/fixtures/assets/shader_manifest/assets.toml \
  --output /tmp/striv-assets \
  --diagnostics jsonl
```

The helper script defaults to:

- `--manifest striv/tests/fixtures/assets/shader_manifest/assets.toml`
- `--output /tmp/striv-assets`

and forwards additional arguments directly to `StriV.AssetTool`.

Current limitations for this slice:

- No new asset kinds beyond shaders.
- No runtime integration.
- No editor integration.
- No watch mode.
- No incremental cache.
