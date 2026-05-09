# M21e SceneInstance/SceneSystem lifecycle nullability cleanup

## 1) Files changed
- `striv/projects/Stride.Engine/Engine/SceneInstance.cs`
- `striv/projects/Stride.Engine/Engine/SceneSystem.cs`
- `striv/tests/Stride.Engine.Tests/SceneInstanceLifecycleTests.cs`

## 2) Task scope
Targeted, test-first lifecycle cleanup on `SceneInstance`/`SceneSystem` only. No Dominatus migration, no SceneSystem architecture rewrite, no scene loading rewrite.

## 3) Before warnings
- Focused warning lines before: `948`
- SceneInstance/SceneSystem had concentrated CS8618/CS8625/CS860x/CS8620 diagnostics.

## 4) Lifecycle classification table
| File/site | Warning | Null pattern | Category | Action |
|---|---|---|---|---|
| SceneInstance.RootScene | CS8625/CS860x | `RootScene = null` | Lifecycle state-machine marker + public clear contract | Kept behavior; made nullable contract explicit; retained STRIV-TODO |
| SceneInstance collection callbacks | CS8600/CS8604/CS8622 | `(T)e.Item`, non-null sender | Collection slot semantics | Switched to pattern matching + nullable sender |
| SceneSystem.SceneInstance | CS8625/CS8618 | cleared on destroy, optional pre-load | Optional runtime connection/lifecycle | Declared nullable contract |
| SceneSystem URLs/tasks | CS8618/CS8625 | absent until configured/started | Not loaded yet/optional connection | Declared nullable fields/properties |
| SceneSystem renderContext/renderDrawContext | CS8618 | initialized in LoadContent | Needs deeper audit | deferred |
| SceneSystem compositor tag keys | CS8620 | nullable mismatch with PropertyKey<T> | Public API nullable contract mismatch | deferred |

## 5) Tests
Added `SceneInstanceLifecycleTests`:
- `SceneInstance_RootScene_Set_AddsEntitiesToEntityManager`
- `SceneInstance_RootScene_Clear_RemovesEntitiesFromEntityManager`
- `SceneInstance_RootSceneChanged_FiresOnSetAndClear`
- `SceneInstance_RootScene_Clear_IsIdempotent`

## 6) Fixes applied
- `SceneInstance`: explicit nullable `RootScene` contract and nullable event; collection-changed handlers now pattern-match items instead of direct casts.
- `SceneSystem`: nullable contracts for lifecycle/optional fields (`SceneInstance`, URLs, tasks, `GraphicsCompositor`, splash texture) with guarded splash render use.

## 7) Deferred lifecycle markers
- Existing marker kept in `SceneInstance.Destroy()` for `RootScene = null` lifecycle teardown marker (future explicit state transition model).

## 8) After warnings
- Focused warning lines after: `860`.
- Net delta: `-88` focused warning lines.
- SceneInstance warning concentration reduced substantially; SceneSystem still has render-context and tag nullability mismatches.

## 9) Validation results
- `dotnet test striv/tests/Stride.Engine.Tests/Stride.Engine.Tests.csproj -v minimal` => exit 0, pass.
- `dotnet build striv/projects/Stride.Engine/Stride.Engine.csproj -c Debug -p:StriVWarningFocusProject=Stride.Engine --no-incremental` => exit 0; warnings only; output captured.
- Broader validation sequence started; produced extensive existing warning output across solution and exceeded practical turn budget before full completion.

## 10) Recommended next task
Continue `SceneInstance/SceneSystem` with a focused follow-up on remaining `SceneSystem` nullability edges (`renderContext`/`renderDrawContext` lifecycle and PropertyKey nullability boundaries) using small tests or guarded accessors.
