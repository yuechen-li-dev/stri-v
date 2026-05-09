# 2100 — M21j CloneSerializer nullability cleanup

## 1) Files changed
- `striv/projects/Stride.Engine/Engine/Design/CloneSerializer.cs`
- `striv/projects/Stride.Engine/Engine/Design/EntityCloner.cs`
- `striv/tests/Stride.Engine.Tests/EntityClonerTests.cs`
- `docs/stri-v/audits/1000+/2100-m21j-cloneserializer-nullability-cleanup.md`

## 2) Task scope
Targeted nullability cleanup around CloneSerializer/EntityCloner lifecycle contracts only. No serializer architecture rewrite, no clone graph behavior rewrite, and no Dominatus migration.

## 3) Before warnings
- Focused warnings before: **774**
- `CloneSerializer` warning bucket before: **20** (`Engine/Design/CloneSerializer.cs CS8602`)
- Top relevant warning codes before: CS8618 (250), CS8625 (108), CS8602 (88), CS8604 (72)

## 4) CloneSerializer classification table
| File/site | Warning | Pattern | Intended behavior | Category | Action |
| --- | --- | --- | --- | --- | --- |
| CloneSerializer.cs (context get) | CS8602 | context retrieved from serializer stream assumed non-null | Clone path requires initialized CloneContext | clone context lifecycle | Added `GetCloneContext(...)` guard helper with deterministic `InvalidOperationException` |
| CloneSerializer.cs (EntitySerializerSelector use) | CS8602 | selector assumed initialized | clone path requires selector setup in EntityCloner.Clone | serializer invariant not visible to compiler | Added `GetEntitySerializerSelector(...)` guard helper |
| CloneSerializer.cs (SharedObjects.Add(mappedObject)) | CS8604 | mapped object can be null even when shared | shared object table should store non-null clone/shared entry | optional reference mapping | fallback to `obj` when mapped callback returns true with null out value |
| CloneSerializer.cs (obj/EntityComponent deref in serialize flow) | CS8602 remaining | generic + serializer flow analysis limitation | object is expected non-null under DataSerializer<T> flow | serializer invariant not visible to compiler | deferred; retained semantics, documented as broader serializer contract topic |

## 5) Tests
- Added `EntityCloner_Clone_NullEntity_ThrowsArgumentNullException`.
- Added `EntityCloner_Clone_WithoutSerializerRegistration_ThrowsArgumentException` characterization.
- Full successful clone in isolated test host is currently blocked by serializer registration/runtime initialization (`No serializer available for type Stride.Engine.Entity`), so success-path clone assertions were intentionally not faked.

## 6) Fixes applied
- `CloneSerializer`: replaced implicit nullable assumptions with local guard helpers (`GetCloneContext`, `GetEntitySerializerSelector`).
- `CloneSerializer`: changed shared-object add from raw `mappedObject` to `mappedObject ?? obj` to preserve non-null stored reference without changing clone traversal.
- `EntityCloner.Clone(Entity)`: added explicit argument null guard to make failure deterministic and testable.

## 7) Deferred clone serializer issues
- Remaining `CloneSerializer.cs CS8602` lines are in generic serializer dereference flow where runtime serializer invariants are stronger than current static contracts.
- Broader cleanup would require explicit non-null contracts for serializer invocation pipeline and possibly API-wide nullable signatures.

## 8) After warnings
- Focused warnings after: **758**
- `CloneSerializer` bucket after: **6** (`Engine/Design/CloneSerializer.cs CS8602`)
- Total delta: **-16** warnings
- CloneSerializer delta: **-14** warning lines

## 9) Next bucket recommendation (M21k)
Recommended: `Engine/Game.cs CS8602`.
- Count: 16 (largest remaining focused CS8602 bucket)
- Risk: medium, but mostly lifecycle initialization checks
- Testability: decent via existing Engine tests with constructor/lifecycle guard patterns
- Expected reduction: high single-bucket impact with bounded surface.

## 10) Validation results
(See command transcript in shell history for full output.)
- `dotnet test striv/tests/Stride.Engine.Tests/Stride.Engine.Tests.csproj -v minimal` => exit 0, pass.
- `dotnet build striv/projects/Stride.Engine/Stride.Engine.csproj -c Debug -p:StriVWarningFocusProject=Stride.Engine --no-incremental` => exit 0, warnings only.
- `dotnet build striv/StriV.Core.slnx -c Debug -v minimal` => exit 0, warnings only.
- `./striv/build/striv-check-focused-projects.sh ...` => exit 0, all pass.
- Remaining standard test/build commands in requested set were executed and passed (one known test skip in ShaderPipeline test suite).
