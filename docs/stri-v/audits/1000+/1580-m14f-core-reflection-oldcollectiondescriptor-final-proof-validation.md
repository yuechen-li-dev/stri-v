# 1580 — M14f Core.Reflection OldCollectionDescriptor final proof/removal validation

## 1) Files changed
- `striv/tests/Stride.Core.Reflection.Tests/TypeDescriptorFactoryCollectionFallbackTests.cs`
- `striv/projects/Stride.Core.Reflection/TypeDescriptorFactory.cs`

## 2) Task scope
This pass is limited to a test-first final proof/removal decision for `OldCollectionDescriptor` inside `Stride.Core.Reflection` only. It is not a broad Reflection cleanup and does not include Shine work.

## 3) Remaining old fallback analysis
Tested concrete shapes:
- Generic `List<int>`
- Generic `Dictionary<string,int>`
- Generic `HashSet<int>`
- Generic custom `LegacyIntCollection : ICollection<int>`
- Non-generic `NonGenericListCollection : ArrayList`

Observed routing:
- Modern generic shapes route to `ListDescriptor`/`DictionaryDescriptor`/`SetDescriptor`/`GenericCollectionDescriptor`.
- Non-generic `ArrayList` shape routes to `OldCollectionDescriptor`.

Conclusion:
- `OldCollectionDescriptor` still has an active job for non-generic list shapes (legacy `IList` lineage).
- Removing it now would change behavior for that shape and risks compatibility.

## 4) Tests added/updated
Updated `TypeDescriptorFactoryCollectionFallbackTests`:
- `TypeDescriptorFactory_NonGenericCollectionShape_CurrentFallback_IsDocumented`
  - Shape: `NonGenericListCollection : ArrayList`
  - Expectation: `OldCollectionDescriptor` selected; add/count/indexer characteristics preserved.
  - Result: green.
- `OldCollectionDescriptor_NoLongerSelected_ForKnownModernShapes`
  - Shape set: `List<int>`, `Dictionary<string,int>`, `HashSet<int>`, `LegacyIntCollection : ICollection<int>`
  - Expectation: `OldCollectionDescriptor` not selected.
  - Result: green.
- Existing M14e guard tests for `LegacyIntCollection` -> `GenericCollectionDescriptor` remain green.

## 5) Decision
Chosen path: **Path B — keep but narrow/fence**.

Why:
- Tests prove there is still a real non-generic fallback consumer (`ArrayList` lineage).
- No replacement non-generic descriptor was introduced in this pass.
- Removal would be an unproven compatibility break.

## 6) Production changes
- Kept existing factory behavior.
- Clarified factory comment to explicitly state the obsolete branch is retained for non-generic `IList` compatibility fallback.
- No descriptor move/delete/compile exclusion performed.

## 7) OldCollectionDescriptor final status
- Compiled: **yes**.
- Referenced: **yes**, from `TypeDescriptorFactory` fallback branch.
- `CS0618`: **still present intentionally** (focused project build).
- Future deletion condition: introduce/validate safe non-generic fallback replacement or explicitly de-support non-generic legacy shapes with consumer sign-off.

## 8) Behavior compatibility
- Serialization/AP risk avoided: no runtime behavior removal for legacy non-generic collection shapes.
- Tested M14e behavior preserved for generic fallback.
- No public API/namespace removal.

## 9) Warning snapshot
Focused `Stride.Core.Reflection` warning lines:
- Before: 62 lines
- After: 62 lines
- Top codes both before/after: `CS8618`(42), `CS8604`(12), `CS8620`(2), `CS8603`(2), `CS8602`(2), `CS0618`(2)
- `CS0618` status: unchanged, still emitted from `TypeDescriptorFactory` fallback reference.

## 10) Validation results
- `rg -n "OldCollectionDescriptor|GenericCollectionDescriptor|CollectionDescriptor|IsCollection|ICollection|IList|IEnumerable" striv/projects/Stride.Core.Reflection striv/projects/Stride.Core.Serialization striv/projects striv/tests`
  - exit: 0, pass, output truncated: no.
- `dotnet build striv/projects/Stride.Core.Reflection/Stride.Core.Reflection.csproj -c Debug -p:StriVWarningFocusProject=Stride.Core.Reflection --no-incremental`
  - before exit: 0, pass, first meaningful warning: CS8618/CS0618 family, output truncated: yes (captured full in log).
- warning extraction/aggregation commands (before)
  - exit: 0, pass, output truncated: no.
- `dotnet test striv/tests/Stride.Core.Reflection.Tests/Stride.Core.Reflection.Tests.csproj -v minimal`
  - exit: 0, pass, first meaningful warning: test project CS0618 assertions (expected in proof tests), output truncated: no.
- `dotnet build striv/projects/Stride.Core.Reflection/Stride.Core.Reflection.csproj -c Debug -p:StriVWarningFocusProject=Stride.Core.Reflection --no-incremental`
  - after exit: 0, pass, first meaningful warning: CS8618/CS0618 family, output truncated: yes (captured full in log).
- warning extraction/aggregation commands (after)
  - exit: 0, pass, output truncated: no.
- `dotnet build striv/StriV.Core.slnx -c Debug -p:StriVWarningFocusProject=Stride.Core.Reflection --no-incremental`
  - exit: 0, pass, first meaningful warning: Reflection nullability + CS0618 in focused area, output truncated: yes.
- `./striv/build/striv-check-focused-projects.sh Stride.BepuPhysics Stride.Core.Mathematics Stride.Core.IO Stride.Input Stride.Games`
  - exit: 0, pass, first meaningful warning/error: none, output truncated: no.
- standard test suite commands from task prompt (all listed)
  - exit: 0, pass, first meaningful warning/error: none that failed runs; one known skipped shader test, output truncated: yes on combined chained run.
- `./striv/build/striv-build-core.sh`
  - exit: 0, pass, first meaningful warning/error: none, output truncated: yes.

## 11) Recommended next task
**Defer deeper Reflection cleanup until serialization sourcegen work** (or a dedicated compatibility decision for non-generic legacy collection support), since M14f proved the old descriptor still has a live non-generic compatibility role.
