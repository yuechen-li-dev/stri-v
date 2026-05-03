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
