# 2020-m21b-stride-engine-category2-dispose-null-cleanup

## 1) Files changed
- striv/projects/Stride.Engine/Engine/Game.cs
- striv/projects/Stride.Engine/Animations/AnimationClipEvaluator.cs
- striv/projects/Stride.Engine/Engine/Network/SimpleSocket.cs
- docs/stri-v/audits/1000+/2020-m21b-stride-engine-category2-dispose-null-cleanup.md

## 2) Task scope
This pass is limited to Category 2 (null as “resource released” after cleanup/dispose) in `Stride.Engine`. It is **not** a broad nullability sweep, does not include architectural lifecycle rewrites, and does not involve Dominatus migration.

## 3) Before warnings
- Focused warning lines before: **964** (`/tmp/striv-m21b-engine-warning-lines-before.log`).
- Top warning codes before: CS8618 340, CS8625 144, CS8604 84, CS8602 82, CS8600 70, CS8603 68.
- Target-site relevance in before log:
  - `Engine/Game.cs` had CS8625 at `DestroyAssetDatabase` nulling lines.
  - `Animations/AnimationClipEvaluator.cs` had CS8625 on `BlenderChannels = null` and `clip = null`.
  - `Engine/Network/SimpleSocket.cs` had CS8625 on `socket = null`.
  - `Engine/Processors/ScriptSystem.cs` had CS8625 at scheduler nulling, but is more lifecycle-coupled.

## 4) Category 2 classification table
| Site | Field/pattern | Active build? | Warning-producing? | Classification | Action |
| ---- | ------------- | ------------: | -----------------: | -------------- | ------ |
| Engine/Game.cs | `databaseFileProvider = null` and service `FileProvider = null` | Yes | Yes | Category 2 release reference | Made `databaseFileProvider` nullable; kept release semantics intact |
| Animations/AnimationClipEvaluator.cs | `BlenderChannels = null`, `clip = null` in `Cleanup()` | Yes | Yes | Category 2 release + post-cleanup guard | Made backing fields nullable and added `ObjectDisposedException` guard access pattern |
| Engine/Network/SimpleSocket.cs | `this.socket = null` in `DisposeSocket()` | Yes | Yes | Category 2 release + guard | Made socket nullable and routed public accessors through guarded `Socket` property |
| Engine/Processors/ScriptSystem.cs | `Scheduler = null` in destroy | Yes | Yes | Category 5-adjacent lifecycle orchestration | Deferred in this pass |
| Audio/AudioSystem.cs | listener slot nulls + engine singleton null | Compiled conditionally | Not in focused top bucket | Collection semantics + singleton lifecycle | Deferred |
| Animations/AnimationClip.cs | `Curves[channel.CurveIndex] = null` | Yes | Yes | Collection slot release semantics | Deferred (Pattern C, not pure dispose-field pattern) |
| TcpSocketClient/TcpSocketListener legacy paths | mixed | low | low | legacy/low-priority | Deferred |

## 5) Tests
No new tests added in this pass. Rationale: selected changes are localized nullability/dispose-guard semantics with no intended behavior change; coverage verified through existing test suites and project builds.

## 6) Fixes applied
- `Game`: converted `databaseFileProvider` backing field to nullable, preserving existing null-on-destroy release behavior and service-provider disconnect.
- `AnimationClipEvaluator`: converted `clip` and `BlenderChannels` to nullable backing fields; added explicit disposed guard on access (`Clip` and AddChannel path), while preserving cleanup nulling.
- `SimpleSocket`: converted socket backing field to nullable and added deterministic disposed guard through `Socket` property; dispose still releases and nulls reference.

## 7) Deferred sites
- `ScriptSystem.Scheduler`: deferred as lifecycle-heavy and likely Category 5-adjacent.
- `AnimationClip.Curves[...] = null`: deferred as collection association release (Pattern C), not straightforward field-dispose Category 2.
- `AudioSystem` null slots/singleton cleanup: deferred for separate pass due conditional build/runtime context.
- `TcpSocketClient`/`TcpSocketListener` exact paths from prior list did not map 1:1 in active tree and were not prioritized.

## 8) After warnings
- Focused warning lines after: **950** (`/tmp/striv-m21b-engine-warning-lines-after.log`).
- Delta: **-14** lines.
- Code distribution moved from CS8625 144 -> 136 and CS8618 340 -> 332; small increase in CS8604 (+2) due tighter guard flow in `SimpleSocket` callsite.

## 9) Validation results
Executed commands and observed exit code 0:
- `dotnet test striv/tests/Stride.Engine.Tests/Stride.Engine.Tests.csproj -v minimal`
- `dotnet build striv/projects/Stride.Engine/Stride.Engine.csproj -c Debug -p:StriVWarningFocusProject=Stride.Engine --no-incremental`
- `dotnet build striv/StriV.Core.slnx -c Debug -v minimal`
- `./striv/build/striv-check-focused-projects.sh Stride.BepuPhysics Stride.Core.Mathematics Stride.Core.IO Stride.Input Stride.Games Stride.Core.Reflection`
- `dotnet test striv/tests/StriV.Engine.Dominatus.Tests/StriV.Engine.Dominatus.Tests.csproj -v minimal`
- `dotnet test striv/tests/Stride.Core.Reflection.Tests/Stride.Core.Reflection.Tests.csproj -v minimal`
- `dotnet test striv/tests/Stride.Games.Tests/Stride.Games.Tests.csproj -v minimal`
- `dotnet test striv/tests/Stride.Input.Tests/Stride.Input.Tests.csproj -v minimal`
- `dotnet test striv/tests/StriV.CleanGraph.Tests/StriV.CleanGraph.Tests.csproj -v minimal`
- `dotnet test striv/tests/StriV.AssetTool.Tests/StriV.AssetTool.Tests.csproj -v minimal`
- `dotnet test striv/tests/StriV.AssetPipeline.Tests/StriV.AssetPipeline.Tests.csproj --no-build -v minimal`
- `dotnet test striv/tests/StriV.ShaderPipeline.Tests/StriV.ShaderPipeline.Tests.csproj --no-build -v minimal`
- `./striv/build/striv-build-core.sh`

Output was truncated in interactive capture for long-running combined command logs, but completion and exit codes were verified.

## 10) Recommended next task
Continue Category 2 with a small follow-up slice on `ScriptSystem` scheduler cleanup semantics (or alternatively `AnimationClip` collection-slot semantics) using targeted tests where possible.
