# M21k — Game.cs nullability/lifecycle cleanup

## 1) Files changed
- `striv/projects/Stride.Engine/Engine/Game.cs`
- `striv/tests/Stride.Engine.Tests/GameLifecycleTests.cs`

## 2) Task scope
Focused pass on `Game.cs` lifecycle/nullability callsites. No game loop rewrite, no services/container rewrite, no Dominatus migration.

## 3) Before warnings
- Focused warnings before: **758**.
- `Engine/Game.cs` warnings before: CS8618/CS8600/CS8602/CS8625/CS8604 (notably CS8602 bucket count 16).

## 4) Game lifecycle classification table
| File/site | Warning | Pattern | Intended behavior | Category | Action |
| --- | --- | --- | --- | --- | --- |
| `PrepareContext` DB provider lookup | CS8600/CS8602 | service lookup cast+deref | Service can be absent in tests/headless | optional service lookup | guarded with pattern match before use |
| `PrepareContext` graphics device manager cast | CS8600/CS8602 | unchecked base-field cast | Use settings only when manager available | runtime/load-content initialized | guarded cast, early-return in default-settings branch |
| `ConfirmRenderingSettings` manager cast | CS8600/CS8602 | unchecked cast | No-op if manager unavailable | headless/test optional | guarded cast + return |
| `EndDraw` input + executable directory | CS8602/CS8604 | lifecycle-dependent deref | screenshot path should be safe in runtime only | lifecycle guard | `Input?.HasKeyboard == true`, `Path.GetDirectoryName(...) ?? string.Empty` |
| events/log listener | CS8618 | ctor-init not represented | optional listener / no subscribers allowed | constructor-required state | nullable event and listener annotations |
| `Input`/`EffectSystem` | CS8618 | runtime initialized members | validly absent before Initialize | runtime initialized field | annotated nullable |

## 5) Tests
Added `GameLifecycleTests`:
- `Game_DefaultConstruction_HasServiceRegistryAndCoreSystems`
- `Game_InitializeAssetDatabase_ReturnsProvider`

`Game.Dispose` idempotence test was intentionally not kept because disposal requires a created window/platform runtime and fails in headless test path.

## 6) Fixes applied
- Replaced unchecked service dereference with guarded `is DatabaseFileProviderService` pattern.
- Replaced unchecked `graphicsDeviceManager` casts with guarded patterns.
- Added lifecycle-safe null guards for `Input` and screenshot path directory.
- Adjusted event/listener/runtime-initialized members to nullable where lifecycle-valid.
- Introduced non-null `Settings` contract backed by constructor default `new GameSettings()` and loaded override from content.

## 7) Deferred issues
- Remaining `Game.cs` `CS8625` on static event invocation argument nullability style.
- Broader engine lifecycle contracts (window/platform-dependent destroy path) remain outside M21k scope.

## 8) After warnings
- Focused warnings after: **718**.
- `Game.cs` bucket reduced from broad CS8602 cluster to remaining CS8625 lines only.
- Total delta: **-40** (758 -> 718).

## 9) Next bucket recommendation (M21l)
Recommend: `Rendering/ModelRenderProcessor.cs CS8604` (14 occurrences).
- High count top bucket.
- Medium risk localized to material resolution callsites.
- Good testability via processor unit-style tests similar to existing lifecycle tests.

## 10) Validation results
See command transcript in this session. Core focused build succeeded; targeted test project currently fails when it attempts to compile/run with existing broader warning/error pressure and window-lifecycle constraints for full `Game` disposal scenarios.
