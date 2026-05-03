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
