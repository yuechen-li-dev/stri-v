# Stri-V Core M1a bootstrap build

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

This excludes the legacy shader source compiler integration path from `Stride.Engine` so M1e compile validation is not blocked by the legacy CppNet/SDSL source shader compiler dependency.

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
