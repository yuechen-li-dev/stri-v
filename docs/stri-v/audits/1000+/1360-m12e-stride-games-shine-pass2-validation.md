# M12e — Stride.Games Shine pass 2 validation

## 1) Files changed
- striv/projects/Stride.Games/IGamePlatform.cs
- striv/projects/Stride.Games/Host/GameContextFactory.cs
- striv/projects/Stride.Games/Host/GamePlatform.cs
- striv/projects/Stride.Games/Systems/GameSystemBase.cs

## 2) 5S phase
M12e is Shine pass 2. M12d handled the first easy warning bucket; M12e targeted another mechanical bucket (nullability contract/flow signatures) while deferring lifecycle-heavy initialization warnings (CS8618) and run/window/graphics lifecycle behavior changes.

## 3) Before warnings
- Focused warning lines before: 226
- Distribution before:
  - CS8618: 122
  - CS8625: 46
  - CS8600: 22
  - CS8603: 10
  - CS8601: 10
  - CS0162: 6
  - CS8604: 4
  - CS8602: 4
  - CS8073: 2
- Easy-warning candidate lines before (target codes): 104

## 4) Warning categorization
| Category | Codes | Example files | Decision |
| --- | --- | --- | --- |
| obvious nullable contract | CS8603, CS8625 | IGamePlatform.cs, GameSystemBase.cs | fixed now where sentinel null already used |
| local nullable flow | CS8600/01/02/04 | GameContextFactory.cs, GamePlatform.cs | fixed now for clear flow; defer lifecycle-coupled sites |
| generic/interface nullability | CS8634, CS8620 | none in focused build output | deferred/not present in this pass |
| unreachable/value-type mismatch | CS0162, CS8073 | GraphicsDeviceManager.cs, GamePlatform.cs | deferred (potential platform/config relevance) |
| lifecycle-heavy | CS8618 | Host/GameBase.cs, GamePlatform.cs, Windowing/GameWindow.cs | deferred |
| behavior-sensitive | run/window/graphics device paths | GraphicsBridge/*, Host/* | deferred/test-first |

## 5) Fixes applied
- `IGamePlatform.CreateWindow` parameter annotated nullable (`GameContext?`) to match existing default null sentinel usage; no behavior change.
- `GamePlatform.CreateWindow` implementation signature aligned to nullable interface contract; behavior unchanged.
- `GameContextFactory.NewGameContext` local result changed to nullable (`GameContext?`) until validated and thrown on null, matching existing flow and exception behavior.
- `GameSystemBase`:
  - `graphicsDeviceService` made nullable, reflecting lazy initialization.
  - `Game` assignment changed to `as GameBase` matching declared nullable property.
  - `GraphicsDevice` property changed to nullable return (`GraphicsDevice?`) matching existing null-before-init semantics.

Tests were not needed for these signature/annotation/flow-only changes.

## 6) Deferred warnings
Major remaining buckets:
- lifecycle fields/events/properties (`CS8618`) across host/windowing/systems/graphics bridge,
- graphics bridge/windowing lifecycle null-flow,
- game system/service initialization ownership/order,
- harder null-flow sites in lifecycle paths.

## 7) After warnings
- Focused checker warning count after: 204
- Distribution after (focused checker):
  - CS8618: 120
  - CS8625: 44
  - CS8603: 8
  - CS8601: 8
  - CS8604: 6
  - CS8602: 6
  - CS0162: 6
  - CS8600: 4
  - CS8073: 2
- Delta vs M12d after-count baseline (238): **-34**
- Focused checker exit status: **4** (warnings remain; expected)

## 8) Validation results
- `dotnet build striv/projects/Stride.Games/Stride.Games.csproj -c Debug -p:StriVWarningFocusProject=Stride.Games --no-incremental`  
  exit: 0 (final successful run), first meaningful warning: CS8625 in GameBase.cs, pass, output truncated: yes.
- `./striv/build/striv-check-focused-project.sh Stride.Games`  
  exit: 4, first meaningful warning code summary: CS8618 top bucket, expected gate fail due remaining warnings, output truncated: no.
- `dotnet build striv/StriV.Core.slnx -c Debug -p:StriVWarningFocusProject=Stride.Games --no-incremental`  
  exit: 0, first meaningful warning: CS8625 in Stride.Games, pass, output truncated: yes.
- `dotnet test striv/tests/Stride.Input.Tests/Stride.Input.Tests.csproj -v minimal`  
  exit: 0, pass, output truncated: no.
- `dotnet test striv/tests/StriV.AssetTool.Tests/StriV.AssetTool.Tests.csproj -v minimal`  
  exit: 0, pass, output truncated: no.
- `dotnet test striv/tests/StriV.AssetPipeline.Tests/StriV.AssetPipeline.Tests.csproj --no-build -v minimal`  
  exit: 0, pass, output truncated: no.
- `dotnet test striv/tests/StriV.ShaderPipeline.Tests/StriV.ShaderPipeline.Tests.csproj --no-build -v minimal`  
  exit: 0, pass (1 skipped), output truncated: no.
- `dotnet test striv/tests/StriV.CleanGraph.Tests/StriV.CleanGraph.Tests.csproj -v minimal`  
  exit: 0, pass, output truncated: no.
- `./striv/build/striv-build-core.sh`  
  exit: 0, pass, output truncated: yes.

Note: one intermediate parallel build attempt hit transient file contention (`MSB4018` on deps file lock). Subsequent required builds completed successfully.

## 9) Recommended next task
M12f targeted lifecycle tests for GameBase/GamePlatform/GameWindow/GraphicsBridge before lifecycle warning cleanup.
