# M12d — Stride.Games Shine pass 1 validation

## 1) Files changed
- striv/projects/Stride.Games/GraphicsBridge/GraphicsDeviceManager.cs
- striv/projects/Stride.Games/GraphicsBridge/GameWindowRenderer.cs
- striv/projects/Stride.Games/Host/GameBase.cs
- striv/projects/Stride.Games/Host/GameContextFactory.cs
- striv/projects/Stride.Games/Host/GamePlatform.cs
- striv/projects/Stride.Games/SDL/GameWindowSDL.cs
- striv/projects/Stride.Games/SDL/SDLMessageLoop.cs
- striv/projects/Stride.Games/Systems/GameSystemCollection.cs
- striv/projects/Stride.Games/Windowing/GameWindow.cs

## 2) 5S phase
M12d is Shine pass 1. M12a/M12b/M12c completed planning Sort, Sort implementation, and Set in order. This pass focused on easy/mechanical nullable warnings first (mainly delegate nullability mismatches and a subset of obvious contract annotations), with lifecycle-heavy initialization buckets intentionally deferred.

## 3) Before warnings
- Focused warning lines before: **292**
- Distribution before:
  - CS8618: 122
  - CS8622: 66
  - CS8625: 46
  - CS8603: 22
  - CS8601: 10
  - CS8600: 8
  - CS0162: 6
  - CS8604: 4
  - CS8602: 4
  - CS8767: 2
  - CS8073: 2
- Easy-warning candidate lines before (CS8622/CS8603/CS8625/CS8767/CS8765/CS8600/CS8601): **154**

## 4) Warning categorization
| Category | Codes | Example files | Decision |
| --- | --- | --- | --- |
| easy mechanical | CS8622 | Host/GameBase.cs, GraphicsBridge/GraphicsDeviceManager.cs, Systems/GameSystemCollection.cs, SDL/SDLMessageLoop.cs | fixed now |
| nullable contract/sentinel | CS8603, CS8625 | Host/GameContextFactory.cs, Windowing/GameWindowHeadless.cs | fixed if obvious; many sentinel/lifecycle sites deferred |
| local flow | CS8600/CS8601 | Systems/GameSystemCollection.cs | fixed only where existing flow already null-safe |
| lifecycle-heavy | CS8618 | Host/GameBase.cs, Host/GamePlatform.cs, Windowing/GameWindow.cs, GraphicsBridge/* | deferred lifecycle |
| behavior-sensitive | run/update/draw/device-window lifecycle | Host/GameBase.cs, GraphicsBridge/GameWindowRenderer.cs | deferred/test-needed |

## 5) Fixes applied
- Delegate/event sender nullability aligned to `object?` in mechanical handler signatures to satisfy .NET event delegate nullability contracts without changing subscription/unsubscription behavior.
- `GamePlatform.ChangeOrCreateDevice` parameter `currentDevice` aligned to nullable interface contract (`GraphicsDevice?`) as signature-only compatibility fix.
- `GameContextFactory` platform factory helpers that intentionally return `null` on unsupported builds/platforms now declare nullable return types where obvious.
- In `GameSystemCollection`, update/draw order callbacks now pattern-match sender type before use, preserving behavior and only removing nullable-flow warnings from cast-based paths.
- No tests added for these signature/annotation fixes because no behavior path was intentionally changed.

## 6) Deferred warnings
Major remaining buckets:
- lifecycle fields/events/properties (`CS8618`) across Host, Windowing, GraphicsBridge, SDL.
- graphics bridge/windowing lifecycle (presenter/window/device sequencing).
- game system/service initialization lifecycle.
- additional nullable sentinel/null-flow sites requiring broader contract/lifecycle review.

## 7) After warnings
- Focused warning lines after: **238**
- Distribution after:
  - CS8618: 122
  - CS8625: 46
  - CS8600: 26
  - CS8603: 10
  - CS8601: 10
  - CS0162: 6
  - CS8634: 4
  - CS8620: 4
  - CS8604: 4
  - CS8602: 4
  - CS8073: 2
- Delta from baseline 292: **-54** warning lines.
- Focused checker exit status: **4** (warnings remain), expected.

## 8) Validation results
| Command | Exit code | First meaningful warning/error | Pass/Fail | Output truncated |
| --- | ---: | --- | --- | --- |
| `dotnet build striv/projects/Stride.Games/Stride.Games.csproj -c Debug -p:StriVWarningFocusProject=Stride.Games --no-incremental` (before) | 0 | CS8625 in `IGamePlatform.cs` | Pass | Yes |
| focused warning extraction/count commands (before) | 0 | n/a | Pass | No |
| `dotnet build striv/projects/Stride.Games/Stride.Games.csproj -c Debug -p:StriVWarningFocusProject=Stride.Games --no-incremental` (after) | 0 | CS8625 in `IGamePlatform.cs` | Pass | Yes |
| focused warning extraction/count commands (after) | 0 | n/a | Pass | No |
| `./striv/build/striv-check-focused-project.sh Stride.Games` | 4 | focused warning gate failed (remaining warnings) | Pass (expected gate behavior) | No |
| `dotnet build striv/StriV.Core.slnx -c Debug -p:StriVWarningFocusProject=Stride.Games --no-incremental` | 0 | CS8625/CS8603/CS8618 in Stride.Games | Pass | Yes |
| `dotnet test striv/tests/Stride.Input.Tests/Stride.Input.Tests.csproj -v minimal` | 0 | none | Pass | No |
| `dotnet test striv/tests/StriV.AssetTool.Tests/StriV.AssetTool.Tests.csproj -v minimal` | 0 | none | Pass | No |
| `dotnet test striv/tests/StriV.AssetPipeline.Tests/StriV.AssetPipeline.Tests.csproj --no-build -v minimal` | 0 | none | Pass | No |
| `dotnet test striv/tests/StriV.ShaderPipeline.Tests/StriV.ShaderPipeline.Tests.csproj --no-build -v minimal` | 0 | one pre-existing skipped test | Pass | No |
| `dotnet test striv/tests/StriV.CleanGraph.Tests/StriV.CleanGraph.Tests.csproj -v minimal` | 0 | none | Pass | No |
| `./striv/build/striv-build-core.sh` | 0 | none | Pass | Yes |

## 9) Recommended next task
**M12e Shine pass 2 for remaining easy bucket**: prioritize the next mechanical nullability cluster without touching lifecycle-heavy CS8618 or run/update/draw/device initialization ordering.
