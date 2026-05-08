# M14d — Stride.Core.Reflection OldCollectionDescriptor quarantine validation

## 1) Files changed
- Moved: `striv/projects/Stride.Core.Reflection/TypeDescriptors/OldCollectionDescriptor.cs` -> `striv/projects/Stride.Core.Reflection/TypeDescriptors/Compatibility/OldCollectionDescriptor.cs`
- Updated: `striv/projects/Stride.Core.Reflection/README-5s-boundary.md`
- Added: `striv/tests/Stride.Core.Reflection.Tests/Stride.Core.Reflection.Tests.csproj`
- Added: `striv/tests/Stride.Core.Reflection.Tests/TypeDescriptorFactoryCollectionFallbackTests.cs`
- Updated: `striv/StriV.Core.slnx`

## 2) Task scope
This pass is a consumer-proof Sort/quarantine pass for `OldCollectionDescriptor` selection behavior only. It is not broad `Stride.Core.Reflection` cleanup and not a descriptor-core rewrite.

## 3) OldCollectionDescriptor selection path
`TypeDescriptorFactory.Create` selection order is:
1. primitive
2. dictionary (`DictionaryDescriptor.IsDictionary`)
3. list (`ListDescriptor.IsList`)
4. set (`SetDescriptor.IsSet`)
5. collection fallback (`CollectionDescriptor.IsCollection` -> `OldCollectionDescriptor`)
6. array
7. nullable
8. object

Observed shape routing:
- `IDictionary<TKey,TValue>` implementations -> `DictionaryDescriptor`.
- `IList<T>` implementations -> `ListDescriptor`.
- `ISet<T>` implementations -> `SetDescriptor`.
- 1D arrays -> `ArrayDescriptor`.
- Any non-array `IList` or `ICollection<T>` shape that is not dictionary/list/set -> `OldCollectionDescriptor` fallback.

`OldCollectionDescriptor` is therefore still active as a legacy compatibility fallback (not dead code).

## 4) Tests added
Added targeted tests in `TypeDescriptorFactoryCollectionFallbackTests`:
- `TypeDescriptorFactory_UsesSpecificDescriptor_ForGenericList` (`List<int>` -> `ListDescriptor`, not old fallback).
- `TypeDescriptorFactory_UsesSpecificDescriptor_ForDictionary` (`Dictionary<string,int>` -> `DictionaryDescriptor`).
- `TypeDescriptorFactory_UsesOldCollectionDescriptor_OnlyForLegacyFallbackShape` (`LegacyIntCollection : ICollection<int>` -> `OldCollectionDescriptor`).
- `OldCollectionDescriptor_FallbackBehavior_IsDocumented` validates fallback add/remove/clear/count and descriptor element/indexer metadata for legacy `ICollection<T>` shape.

## 5) Quarantine decision
Decision: **moved to Compatibility folder and retained in compile**.

Reason: tests prove active fallback usage for legacy `ICollection<T>` shapes, so deletion or compile-exclusion would be unsafe in M14d.

## 6) Behavior compatibility
- Runtime behavior preserved: `TypeDescriptorFactory` still routes legacy collection fallback shapes to `OldCollectionDescriptor`.
- Serialization/AP compatibility risk minimized: no selection rewrite; only file move + fencing comments + tests.
- Namespace/type identity preserved (`Stride.Core.Reflection.OldCollectionDescriptor`).

## 7) Boundary documentation
Updated `README-5s-boundary.md` with explicit compatibility status and no-new-callsites rule for `OldCollectionDescriptor`.

## 8) Warning snapshot
Focused build warning snapshot (`Stride.Core.Reflection`-scoped filter):
- Warning lines matched: **62**
- Top codes:
  - `CS8618`: 42
  - `CS8604`: 12
  - `CS8620`: 2
  - `CS8603`: 2
  - `CS8602`: 2
  - `CS0618`: 2

`CS0618` status: still present (factory fallback usage + obsolete compatibility type), consistent with intentional retention.

## 9) Validation results
- `dotnet test striv/tests/Stride.Core.Reflection.Tests/Stride.Core.Reflection.Tests.csproj -v minimal`
  - exit: 0, pass, first meaningful warning: CS0618 on explicit obsolete fallback assertions, output truncated: no.
- `dotnet build striv/projects/Stride.Core.Reflection/Stride.Core.Reflection.csproj -c Debug -p:StriVWarningFocusProject=Stride.Core.Reflection --no-incremental`
  - exit: 0, pass, first meaningful warning: CS8618 in `MemberDescriptorBase`, output truncated: no.
- `dotnet build striv/StriV.Core.slnx -c Debug -p:StriVWarningFocusProject=Stride.Core.Reflection --no-incremental`
  - exit: 0, pass, first meaningful warning: CS8618 in `MemberDescriptorBase`, output truncated: yes.
- `./striv/build/striv-check-focused-projects.sh Stride.BepuPhysics Stride.Core.Mathematics Stride.Core.IO Stride.Input Stride.Games`
  - exit: 0, pass, first meaningful warning/error: none, output truncated: no.
- `dotnet test striv/tests/Stride.Games.Tests/Stride.Games.Tests.csproj -v minimal`
  - exit: 0, pass, first meaningful warning/error: none, output truncated: no.
- `dotnet test striv/tests/Stride.Input.Tests/Stride.Input.Tests.csproj -v minimal`
  - exit: 0, pass, first meaningful warning/error: none, output truncated: no.
- `dotnet test striv/tests/StriV.CleanGraph.Tests/StriV.CleanGraph.Tests.csproj -v minimal`
  - exit: 0, pass, first meaningful warning: downstream existing warnings in other projects during build graph, output truncated: yes.
- `dotnet test striv/tests/StriV.AssetTool.Tests/StriV.AssetTool.Tests.csproj -v minimal`
  - exit: 0, pass, first meaningful warning: downstream existing warnings in other projects during build graph, output truncated: yes.
- `dotnet test striv/tests/StriV.AssetPipeline.Tests/StriV.AssetPipeline.Tests.csproj --no-build -v minimal`
  - exit: 0, pass, first meaningful warning/error: none, output truncated: yes.
- `dotnet test striv/tests/StriV.ShaderPipeline.Tests/StriV.ShaderPipeline.Tests.csproj --no-build -v minimal`
  - exit: 0, pass, first meaningful warning/error: none, output truncated: yes.
- `./striv/build/striv-build-core.sh`
  - exit: 0, pass, first meaningful warning: existing nullable/obsolete warnings in full graph, output truncated: yes.

## 10) Recommended next task
**M14e descriptor fallback migration**: migrate known legacy `ICollection<T>` consumer shapes to modern descriptor routing (or add dedicated descriptor) so `OldCollectionDescriptor` callsite can eventually be retired with proof.
