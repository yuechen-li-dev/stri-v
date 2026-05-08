# M16a Vendor Dominatus Source Validation

## 1) Files changed
- Added vendored source folders:
  - `striv/external/Dominatus/src/Dominatus.Core`
  - `striv/external/Dominatus/src/Dominatus.OptFlow`
  - `striv/external/Dominatus/src/Ariadne.OptFlow`
  - `striv/external/Dominatus/src/Dominatus.UtilityLite`
- Added vendor guidance README:
  - `striv/external/Dominatus/README-striv-vendored.md`
- Updated solution:
  - `striv/StriV.Core.slnx`
- Added audit report:
  - `docs/stri-v/audits/1000+/1670-m16a-vendor-dominatus-source-validation.md`

## 2) Task scope
This change is vendor/import only. No engine integration was performed. No bridge project was added. No runtime behavior changes were introduced.

## 3) Imported Dominatus subset
Imported:
- `Dominatus.Core`
- `Dominatus.OptFlow`
- `Ariadne.OptFlow`
- `Dominatus.UtilityLite`

Intentionally excluded:
- `Dominatus.Server`
- `Dominatus.StrideConn`
- `Dominatus.Actuators.*`
- `Dominatus.Llm.OptFlow`
- `Ariadne.Console`
- `tests/**`
- `samples/**`

## 4) Vendor layout
Final layout under `striv/external/Dominatus/`:
- `README-striv-vendored.md`
- `src/Ariadne.OptFlow/`
- `src/Dominatus.Core/`
- `src/Dominatus.OptFlow/`
- `src/Dominatus.UtilityLite/`

## 5) Dependency/project-reference audit
From vendored csproj inspection:
- `Dominatus.Core`: TargetFrameworks `net8.0;net10.0`, no ProjectReference, no PackageReference.
- `Dominatus.OptFlow`: TargetFramework `net8.0`, ProjectReference to `Dominatus.Core`, no PackageReference.
- `Ariadne.OptFlow`: TargetFramework `net8.0`, ProjectReference to `Dominatus.Core` and `Dominatus.OptFlow`, no PackageReference.
- `Dominatus.UtilityLite`: TargetFramework `net8.0`, ProjectReference to `Dominatus.Core` and `Dominatus.OptFlow`, no PackageReference.

No project references to non-vendored Dominatus projects were found in the selected subset. No central package version alignment changes were required.

## 6) Solution integration
Projects added to `striv/StriV.Core.slnx`:
- `striv/external/Dominatus/src/Dominatus.Core/Dominatus.Core.csproj`
- `striv/external/Dominatus/src/Dominatus.UtilityLite/Dominatus.UtilityLite.csproj`
- `striv/external/Dominatus/src/Dominatus.OptFlow/Dominatus.OptFlow.csproj`
- `striv/external/Dominatus/src/Ariadne.OptFlow/Ariadne.OptFlow.csproj`

Method: `dotnet sln striv/StriV.Core.slnx add ...`.

## 7) Build results (vendored projects)
1. `dotnet build striv/external/Dominatus/src/Dominatus.Core/Dominatus.Core.csproj -c Debug -v minimal`
   - Exit code: 0
   - First meaningful warning/error: none
   - Result: pass
   - Output truncated: no
2. `dotnet build striv/external/Dominatus/src/Dominatus.UtilityLite/Dominatus.UtilityLite.csproj -c Debug -v minimal`
   - Exit code: 0
   - First meaningful warning/error: none
   - Result: pass
   - Output truncated: no
3. `dotnet build striv/external/Dominatus/src/Dominatus.OptFlow/Dominatus.OptFlow.csproj -c Debug -v minimal`
   - Exit code: 0
   - First meaningful warning/error: none
   - Result: pass
   - Output truncated: no
4. `dotnet build striv/external/Dominatus/src/Ariadne.OptFlow/Ariadne.OptFlow.csproj -c Debug -v minimal`
   - Exit code: 0
   - First meaningful warning/error: none
   - Result: pass
   - Output truncated: no

Also run:
- `dotnet build striv/StriV.Core.slnx -c Debug -v minimal`
  - Exit code: 0
  - First meaningful warning/error: existing nullability/analysis warnings from Stri-V projects
  - Result: pass
  - Output truncated: yes (tool output limit)

## 8) Existing Stri-V validation
1. `./striv/build/striv-check-focused-projects.sh Stride.BepuPhysics Stride.Core.Mathematics Stride.Core.IO Stride.Input Stride.Games Stride.Core.Reflection`
   - Exit code: 0
   - First meaningful warning/error: none in summary
   - Result: pass
   - Output truncated: no
2. `dotnet test striv/tests/Stride.Core.Reflection.Tests/Stride.Core.Reflection.Tests.csproj -v minimal`
   - Exit code: 0
   - First meaningful warning/error: existing CS0618 warnings in tests
   - Result: pass
   - Output truncated: no
3. `dotnet test striv/tests/Stride.Games.Tests/Stride.Games.Tests.csproj -v minimal`
   - Exit code: 0
   - First meaningful warning/error: existing warnings from dependent projects
   - Result: pass
   - Output truncated: yes
4. `dotnet test striv/tests/Stride.Input.Tests/Stride.Input.Tests.csproj -v minimal`
   - Exit code: 0
   - First meaningful warning/error: none significant
   - Result: pass
   - Output truncated: included in chained command output; not truncated at test start
5. `dotnet test striv/tests/StriV.CleanGraph.Tests/StriV.CleanGraph.Tests.csproj -v minimal`
   - Exit code: 0
   - First meaningful warning/error: existing warnings from dependent projects
   - Result: pass
   - Output truncated: yes (chained output)
6. `dotnet test striv/tests/StriV.AssetTool.Tests/StriV.AssetTool.Tests.csproj -v minimal`
   - Exit code: 0
   - First meaningful warning/error: existing warnings from dependent projects
   - Result: pass
   - Output truncated: yes (chained output)
7. `dotnet test striv/tests/StriV.AssetPipeline.Tests/StriV.AssetPipeline.Tests.csproj --no-build -v minimal`
   - Exit code: 0
   - First meaningful warning/error: none significant
   - Result: pass
   - Output truncated: yes (chained output)
8. `dotnet test striv/tests/StriV.ShaderPipeline.Tests/StriV.ShaderPipeline.Tests.csproj --no-build -v minimal`
   - Exit code: 0
   - First meaningful warning/error: none significant
   - Result: pass
   - Output truncated: yes (chained output)
9. `./striv/build/striv-build-core.sh`
   - Exit code: 0
   - First meaningful warning/error: none (build succeeded)
   - Result: pass
   - Output truncated: yes (chained output)

## 9) Local modifications to vendored code
None. Vendored Dominatus source was imported unchanged, aside from path placement under `striv/external/Dominatus` and Stri-V solution inclusion.

## 10) Recommended next task
Proceed to **M16b**: create `StriV.Engine.Dominatus` bridge project (without yet changing engine behavior), then M16c for first strangler node proof.
