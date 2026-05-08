# 1450 — M12n Stride.Games GameBase lifecycle contract Shine validation

## 1) Files changed
- `striv/projects/Stride.Games/Host/GameBase.cs`
- `striv/tests/Stride.Games.Tests/PlatformWindowLifecycleTests.cs`
- `docs/stri-v/audits/1000+/1450-m12n-stride-games-gamebase-lifecycle-contract-shine-validation.md`

## 2) Task scope
M12n scope was a test-first Shine pass for the remaining `GameBase` lifecycle contract warnings only (constructor/service null contract, `CreateDevice` preconditions, cleanup nullable assignments, and post-dispose/pre-run behavior). No run-loop redesign, no graphics-device creation changes, and no native backend behavior changes were made.

## 3) Before warnings
- Focused warning count before: **78**.
- Distribution before: CS8618=26, CS8625=20, CS8602=8, CS8600=6, CS0162=6, CS8604=4, CS8603=4, CS8601=4.
- `GameBase` warning lines before: **10** (5 unique sites duplicated by build summary) at lines ~99, 367, 741, 985, 1013.

## 4) Warning-site classification
| Site | Warning | Classification | Required test? | Planned fix |
|---|---|---|---|---|
| line 99 | CS8625 | constructor/default lifecycle | yes | keep service registration contract, document intentional pre-bind, use targeted null-forgiving at registration site |
| line 367 | CS8602 | create-device precondition | yes | add explicit guard and deterministic `InvalidOperationException` when manager absent |
| line 741 | CS8625 | cleanup nullable assignment | no (covered by existing dispose-before-run + handler tests) | move nullable cleanup to private nullable backing field |
| line 985 | CS8625 | cleanup nullable assignment | no (covered by event handler tests) | move nullable cleanup to private nullable backing field |
| line 1013 | CS8625 | cleanup nullable assignment | no (covered by event handler tests) | move nullable cleanup to private nullable backing field |

## 5) Test-first workflow
### Cluster A — constructor/service null contract
- Test coverage existed (`GameBase_Constructs_WithStablePreRunDefaults`) validating pre-run null-safe state.
- Production change kept behavior and clarified intent at DB provider service registration site.

### Cluster B — `CreateDevice` preconditions
- Added test first: `GameBase_InitializeBeforeRun_WithoutGraphicsDeviceManager_ThrowsInvalidOperationException`.
- Initial result: failed (actual `NullReferenceException`).
- Production change: explicit guard in `InitializeBeforeRun()` throws `InvalidOperationException("No GraphicsDeviceManager found")`.
- Final result: pass.

### Cluster C — cleanup nullable assignments
- Existing + expanded handler test first: `GameBase_GraphicsDeviceEventHandlers_BeforeSetup_DoNotThrow` (expanded to include disposing path).
- Production change: replaced direct null assignment to non-nullable lifecycle properties with private nullable backing fields for `GraphicsDevice` and `GraphicsContext`.
- Final result: pass; warnings removed without run-loop/backend changes.

## 6) Tests added
- `GameBase_InitializeBeforeRun_WithoutGraphicsDeviceManager_ThrowsInvalidOperationException`: locks deterministic precondition failure before device creation path.
- Expanded `GameBase_GraphicsDeviceEventHandlers_BeforeSetup_DoNotThrow`: now covers created/reset/dispose callbacks in pre-setup state.
- `ProbeGameBase.InvokeInitializeBeforeRun` unwraps reflection `TargetInvocationException` so assertions lock real lifecycle exception type.

## 7) Fixes applied
- `GameBase`: added explicit `graphicsDeviceManager` precondition guard in `InitializeBeforeRun`.
- `GameBase`: introduced private nullable backing fields for `GraphicsDevice`/`GraphicsContext` and nulled those in cleanup/event paths.
- `GameBase`: documented intentional pre-bind database provider service registration and used targeted null-forgiving at that contract point.
- `PlatformWindowLifecycleTests`: added/expanded tests to pin lifecycle behaviors before production edits.

## 8) After warnings
- Focused warning count after: **68**.
- Distribution after: CS8618=26, CS8625=12, CS8602=6, CS8600=6, CS0162=6, CS8604=4, CS8603=4, CS8601=4.
- `GameBase` warning count after: **0**.
- Focused checker exit status: **4** (expected while focused warnings remain).
- Delta from M12m baseline (78): **-10**.

## 9) Remaining warning bucket analysis
| Bucket | Warning count | Confidence | Testability | Expected return | Recommendation |
|---|---:|---|---|---|---|
| `SDL/GameFormSDL.cs` + CS8618 | 12 | high | high | high (single-file/event-nullability cleanup) | top candidate |
| `Host/GameContext.cs` + CS8618 | 6 | high | high | medium-high | second candidate |
| `GraphicsBridge/GraphicsDeviceManager.cs` + CS0162/CS8625/CS8618 | 12 | medium | medium | medium | defer until graphics-bridge pass |
| `Systems/GameSystemCollection.cs` + CS8600/CS8602/CS8604 | 10 | medium-high | high | high | alternate top candidate |
| `SDL/GameWindowSDL.cs` + SDLMessageLoop mixed nullability | 14 | medium | medium | high but backend-adjacent risk | after GameFormSDL |

## 10) Recommended next task
Highest-return next bucket: **`SDL/GameFormSDL.cs` CS8618 event nullability bucket**.
Rationale: contiguous single-file bucket, high warning count reduction per touch, no real graphics device creation, and testability with existing headless/window probes.

## 11) Validation results
| Command | Exit | First meaningful warning/error | Pass/Fail | Truncated |
|---|---:|---|---|---|
| `dotnet build striv/projects/Stride.Games/Stride.Games.csproj -c Debug -p:StriVWarningFocusProject=Stride.Games --no-incremental` | 0 | CS8603 in `Desktop/GamePlatformDesktop.cs` | pass | no |
| `dotnet test striv/tests/Stride.Games.Tests/Stride.Games.Tests.csproj -v minimal` (after new failing test, before prod fix) | 1 | assertion mismatch: expected InvalidOperationException, actual NullReferenceException | fail (expected in test-first loop) | no |
| `dotnet test striv/tests/Stride.Games.Tests/Stride.Games.Tests.csproj -v minimal` (after fix) | 0 | none | pass | no |
| `./striv/build/striv-check-focused-project.sh Stride.Games` | 4 | focused warning gate failed with 68 warnings | pass (expected gate behavior) | no |
| `dotnet build striv/StriV.Core.slnx -c Debug -p:StriVWarningFocusProject=Stride.Games --no-incremental` | 0 | focused warning lines from Stride.Games | pass | no |
| `dotnet test striv/tests/Stride.Input.Tests/Stride.Input.Tests.csproj -v minimal` | 0 | none | pass | yes (combined command output clipped by tool) |
| `dotnet test striv/tests/StriV.AssetTool.Tests/StriV.AssetTool.Tests.csproj -v minimal` | 0 | none | pass | yes (combined command output clipped by tool) |
| `dotnet test striv/tests/StriV.AssetPipeline.Tests/StriV.AssetPipeline.Tests.csproj --no-build -v minimal` | 0 | none | pass | yes (combined command output clipped by tool) |
| `dotnet test striv/tests/StriV.ShaderPipeline.Tests/StriV.ShaderPipeline.Tests.csproj --no-build -v minimal` | 0 | none | pass | yes (combined command output clipped by tool) |
| `dotnet test striv/tests/StriV.CleanGraph.Tests/StriV.CleanGraph.Tests.csproj -v minimal` | 0 | none | pass | yes (combined command output clipped by tool) |
| `./striv/build/striv-build-core.sh` | 0 | none | pass | yes (combined command output clipped by tool) |
