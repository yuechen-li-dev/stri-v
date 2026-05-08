# 1570 M14e — Core.Reflection OldCollectionDescriptor migration validation

## 1) Files changed
- striv/projects/Stride.Core.Reflection/TypeDescriptorFactory.cs
- striv/projects/Stride.Core.Reflection/TypeDescriptors/GenericCollectionDescriptor.cs (new)
- striv/tests/Stride.Core.Reflection.Tests/TypeDescriptorFactoryCollectionFallbackTests.cs

## 2) Task scope
Targeted migration of `ICollection<T>` fallback routing only; no broad reflection cleanup, no Shine pass.

## 3) Current old fallback behavior
M14d confirmed `LegacyIntCollection : ICollection<int>` used `OldCollectionDescriptor`; list/dictionary used specific descriptors. M14e replaces only that generic fallback route.

## 4) Replacement design
Option B chosen: new narrow `GenericCollectionDescriptor` for generic `ICollection<T>` non-list/non-set/non-dictionary shapes. `OldCollectionDescriptor` retained as compatibility last-resort path.

## 5) Tests added/updated
- `TypeDescriptorFactory_UsesModernDescriptor_ForLegacyICollectionShape`: expects `GenericCollectionDescriptor`, not old.
- `ModernCollectionDescriptor_FallbackBehavior_MatchesLegacyICollectionShape`: add/remove/clear/count/element type/indexer behavior parity.
- `OldCollectionDescriptor_IsNotSelected_ForKnownFallbackShapes`: old descriptor no longer chosen for tested legacy shape.
- Red/green: green on `dotnet test ...Stride.Core.Reflection.Tests...`.

## 6) Production changes
- `TypeDescriptorFactory`: generic `ICollection<>` now routes to `GenericCollectionDescriptor`; otherwise keeps `OldCollectionDescriptor` fallback.
- `GenericCollectionDescriptor`: modern concrete descriptor implementing add/remove/clear/count/read-only and explicit non-indexed behavior.

## 7) OldCollectionDescriptor status
- Still compiled and referenced as last-resort fallback branch.
- Still selected for non-generic collection shapes (if encountered).
- `CS0618` in `TypeDescriptorFactory` remains (reduced migration scope).
- Next removal condition: prove no runtime paths require old descriptor and remove fallback branch.

## 8) Warning snapshot
Focused build log warning lines for Reflection project: 62 lines. Top codes: CS8618 (42), CS8604 (12), CS0618 (2), CS8602 (2), CS8603 (2), CS8620 (2). CS0618 still present from explicit compatibility fallback.

## 9) Behavior compatibility
Tested legacy shape behavior preserved for add/remove/clear/count and element/indexer expectations. No namespace/public type identity changes to old descriptor. Serialization/AP risk minimized by preserving last-resort fallback.

## 10) Validation results
Commands executed with exit code 0:
- all required project/test/build commands from task prompt (including focused warning extraction, solution build, focused project checks, and core build script).
- One long combined test command produced truncated console capture in agent output, but command completed successfully.

## 11) Recommended next task
M14f: add targeted non-generic legacy-shape tests, then remove/compile-exclude `OldCollectionDescriptor` only if fallback path is proven unused.
