# 1290 — M11b Serialization Source Generator Contract (AP `--serialization` replacement spec)

## 1) Files changed

- `docs/stri-v/audits/1000+/1290-m11b-serialization-source-generator-contract.md` (new report-only spec).

---

## 2) Scope and non-goals

### Scope (M11b)
Define an evidence-based **contract/spec** for a Roslyn source generator that can incrementally replace `Stride.Core.AssemblyProcessor` serialization IL weaving (`--serialization`) while preserving runtime behavior.

### Non-goals (explicit)
- No generator implementation in M11b.
- No removal of AP serialization path globally.
- No removal of `[DataContract]`, `[DataMember]`, `[DataMemberIgnore]`.
- No runtime serializer architecture replacement (binary runtime ownership redesign is out-of-scope here).
- No TOML asset serialization migration in this task.

---

## 3) Current AP serialization behavior contract

### 3.1 Entry-point activation
AP serialization runs only when `AssemblyProcessorApp.SerializationAssembly == true`; processor pipeline then includes `AssemblyScanProcessor` and `SerializationProcessor`. CLI option is `--serialization`. Current projects pass this via `StriVAssemblyProcessorOptions`. 

### 3.2 Attributes and metadata AP consumes
From `CecilSerializerContext` pipeline + lookup logic:
- `DataSerializerGlobalAttribute` (local + referenced assembly registration data).
- `DataSerializerAttribute` (explicit serializer assignment and generic mode behavior).
- `DataContractAttribute` (drives generated serializer creation and `Inherited` behavior).
- `DataMemberCustomSerializerAttribute` (member dependency resolution exception path).
- `DataAliasAttribute` / `DataContractAttribute` aliases via `DataContractAliasProcessor` into alias registration table.

`SerializationHelpers.GetSerializableItems(...)` is the member selector used by AP for generated serializers, and it is called with current ignored-member set.

### 3.3 Serializable type discovery
`CecilSerializerContext.ResolveSerializer(...)` contract:
1. Check already-known serializers in per-profile tables.
2. Handle arrays via `ArraySerializer<TElement>` if element serializable.
3. Handle generic instances by matching open generic serializer registrations and instantiating mode-driven closed serializer type (`DataSerializerGenericMode`).
4. For `Default` profile, inspect type metadata via `FindSerializerInfo(...)`:
   - enums -> `EnumSerializer<TEnum>`
   - `[DataSerializer]` -> explicit serializer handling
   - `[DataContract]` -> auto-generated serializer collection
   - inherited serializer rules via parent chain
5. If still unresolved and `force == true`, AP may allow abstract/interface/object entries with null serializer placeholder.

### 3.4 Member selection/order/ignore contract
AP relies on `SerializationHelpers.GetSerializableItems(type, includeBase, ignoredMembers)` to produce `SerializableItem[]` used in emitted `Serialize` method. Ordering and assign-back semantics are frozen by this array and then serialized in that order. Ignored members are dynamically added when no serializer can be resolved; AP warns and excludes them.

`[DataMemberIgnore]` is effectively honored by the helper path and by final ignored set; members without valid serializers are additionally added to ignored members.

### 3.5 Fields vs properties
Generated serialize path treats members as:
- **Field + assignback true**: pass field address directly (`ldflda`) when possible.
- **Property or non-assignback field**: use temp local (`tmp`), serialize ref tmp, and assign back on deserialize when needed.

### 3.6 Classes vs structs
AP chooses base serializer type per generated serializer:
- `ClassDataSerializer<T>` when type is non-abstract class with public empty ctor.
- else `DataSerializer<T>`.

### 3.7 Generics
For generic data contracts AP:
- creates generic serializer type definitions mirroring type generic params/constraints,
- uses `DataSerializerGenericMode.GenericArguments` for generated open serializers,
- instantiates closed serializers for generic instances per mode handling.

Nested types referencing parent generic parameters are explicitly unsupported (`AddSerializableType` throws).

### 3.8 Inheritance
AP resolves nearest serializable base and stores `parentSerializer` field. Generated `Serialize` calls parent serializer first. `DataContract(Inherited=true)` and inherited `[DataSerializer]` modes are honored in `FindInheritedSerializerInfo`.

### 3.9 Collections and nested serializers
Member serializer fields are generated per unique member type and initialized with `MemberSerializer<T>.Create(serializerSelector, true)`. Arrays and supported generic collections are resolved through serializer registry/generic instantiation mechanisms.

### 3.10 Profiles / content-vs-default distinction
AP maintains `SerializableTypesProfiles` and emits profile-keyed `AssemblySerializers.Profiles` entries. Runtime selector can use profiles (`Default`, `Content`, etc.) and merges via `SerializerSelector` profile set.

### 3.11 Serialization hash
AP writes `.sdserializationhash` next to output assembly when serialization ran. Hash includes:
- global `DataSerializer.BinaryFormatVersion`,
- per generated type fullname,
- parent marker when applicable,
- each serializable member type/name/assignback.

---

## 4) Generated serializer shape today (source-equivalent)

### 4.1 Namespace/type naming
Generated into namespace `Stride.Core.DataSerializers`.
- Per-contract serializer class name from `SerializationHelpers.SerializerTypeName(...)`.
- Factory type: `{AssemblyNameSanitized}SerializerFactory`.

### 4.2 Base class
Per type:
- `ClassDataSerializer<T>` or
- `DataSerializer<T>`.

### 4.3 Members generated
- Optional `parentSerializer` field (`DataSerializer<ParentType>`).
- One private serializer field per unique member type: `DataSerializer<MemberType>`.

### 4.4 Constructor
Public parameterless ctor calling base ctor.

### 4.5 `Initialize(SerializerSelector)`
- Resolve parent via `serializerSelector.GetSerializer<ParentType>()` when needed.
- Resolve each member via `MemberSerializer<T>.Create(serializerSelector, true)`.

### 4.6 `Serialize(ref T obj, ArchiveMode mode, SerializationStream stream)`
- Parent serialize first.
- Branch on mode serialize/deserialize.
- Serialize mode: load member value (or field ref) and serialize.
- Deserialize mode: initialize tmp/default, serialize into tmp, assign back where applicable.

### 4.7 Null/reference/value semantics
Handled by selected underlying member serializers and chosen base serializer class behavior; generated body only orchestrates read/write/member assignment flow.

---

## 5) Registration/discovery contract (critical)

### 5.1 How AP registers today
`GenerateSerializerFactory` emits:
1. Factory type containing many `[DataSerializerGlobal(...)]` attributes.
2. `Initialize` method that builds `AssemblySerializers` object with:
   - `DataContractAliases`
   - referenced `Modules`
   - profile entries (`AssemblySerializerEntry(id,type,serializerType)`)
3. Calls `DataSerializerFactory.RegisterSerializationAssembly(assemblySerializers)`.
4. Calls `AssemblyRegistry.Register(assembly, ["Engine"])`.
5. Injects call to factory `Initialize` into module constructor (`<Module>..cctor`).
6. Emits assembly `[AssemblySerializerFactory(Type = factoryType)]` attribute.

### 5.2 Runtime discovery path
- `SerializerSelector.Default.GetSerializer<T>()` uses cached maps built from `DataSerializerFactory` registrations.
- `DataSerializerFactory.RegisterSerializationAssembly(AssemblySerializers)` updates profile/type maps and invalidates selectors.
- Additional module ctors in `AssemblySerializers.Modules` are run during registration.
- `RegisterSerializationAssembly(Assembly)` fallback can load already-available assembly serializer manifests.

### 5.3 Why EffectBytecode test has been brittle
`SerializerSelector.Default.GetSerializer<EffectBytecode>() != null` depends on **assembly load + registration timing**, not just serializer type existence. If `Stride.Shaders` serializer registration wasn’t invoked (or module init not yet executed), selector returns null despite serializer code presence.

### 5.4 What source generator must preserve for parity
At minimum, equivalent behavior must preserve:
- same serializer type identity for each data type/profile,
- same assembly-level availability metadata (`AssemblySerializerFactoryAttribute` + `DataSerializerGlobal` equivalent effects),
- same `AssemblySerializers` runtime object shape registration,
- same init timing guarantees (module init or deterministic bootstrap before first `GetSerializer<T>` use),
- same alias/profile tables,
- same parent/member serializer wiring semantics.

### 5.5 Registration option comparison

#### A) Generated `[ModuleInitializer]` per assembly
- **Pros:** closest to AP auto-run timing; no manual bootstrap burden.
- **Cons:** order across modules still nuanced; linker/AOT may trim if not rooted incorrectly.
- **Risk:** medium (timing differences vs IL-injected module cctor).
- **AOT:** good if static and rooted.
- **Testability:** good with load-order tests.

#### B) Generated explicit registrar + manual bootstrap
- **Pros:** deterministic explicit call site; very testable.
- **Cons:** callers must remember to invoke; behavior drift likely.
- **Risk:** high for incremental parity.
- **AOT:** excellent.
- **Testability:** excellent unit-wise, weaker integration safety.

#### C) Generated attributes/tables + existing scan path
- **Pros:** can reuse reflection-based discovery.
- **Cons:** may still require runtime scanner changes; potential startup overhead and behavior drift.
- **Risk:** medium-high.
- **AOT:** mixed (reflection scanning).
- **Testability:** medium.

#### D) Hybrid: sourcegen serializers + AP module init initially
- **Pros:** safest incremental cut; registration timing unchanged.
- **Cons:** temporary dual mechanism complexity.
- **Risk:** lowest for MVP.
- **AOT:** medium (still AP step for registration hookup).
- **Testability:** high for parity gating.

### 5.6 MVP recommendation
**Recommend D initially**, then evolve to **A** once parity is proven.
- Phase 1: generator emits serializer + factory manifest types; AP still handles module cctor hookup and/or reads emitted manifest.
- Phase 2: switch to pure generated module initializer once load-order tests pass for target assemblies.

---

## 6) Incremental migration switch design

### Proposed property
`<StriVUseSerializationSourceGenerator>true|false</StriVUseSerializationSourceGenerator>` (default false).

### Behavior
- **false**: unchanged AP options (`--serialization` remains).
- **true**:
  - include generator/analyzer package/project reference,
  - remove `--serialization` from AP command for that project only,
  - keep other AP options (`--parameter-key`, `--auto-module-initializer`) as-is.

### Where to implement
Best location: `striv/build/StriV.AssemblyProcessor.targets` + shared props defaults.
- compute effective AP options via conditional property transform, e.g. remove `--serialization` token when sourcegen enabled.
- keep project-level override in csproj for pilot projects.

---

## 7) Generator MVP contract

### Proposed projects
- `striv/projects/Stride.Core.Serialization.Generator` (new incremental source generator).
- `striv/tests/Stride.Core.Serialization.Generator.Tests` (or fixture in existing test project).

### MVP fixture scope
One `[DataContract]` type with:
- primitive members,
- explicit ordered `[DataMember(n)]`,
- one `[DataMemberIgnore]`,
- one field + one property.

### Generator must emit (MVP)
1. concrete serializer type in `Stride.Core.DataSerializers`.
2. factory/registrar type carrying equivalent metadata.
3. registration hook (hybrid path allowed for MVP).
4. diagnostics for unsupported shapes.

### MVP proof criteria
- generated source compiles,
- `SerializerSelector.Default.GetSerializer<T>()` non-null,
- round-trip serialization works,
- fixture project succeeds without AP `--serialization`.

---

## 8) First real-type proof target recommendation

**Pick `Stride.Core.Mathematics.Color3` first.**

Why:
- previously demonstrated load-bearing in attribute-removal attempts,
- small/simple struct with high graph reach,
- lower behavioral complexity than `EffectBytecode`,
- catches real engine usage earlier than synthetic-only fixture.

`EffectBytecode` should remain a later gate focused on registration/load-order robustness.

---

## 9) Behavioral equivalence tests before disabling AP serialization per project

Required gate suite:
1. Serializer existence (`GetSerializer<T>() != null`).
2. Round-trip equality for representative instances.
3. Member order parity (binary shape contract test).
4. `[DataMemberIgnore]` exclusion verified.
5. Null/reference behavior parity.
6. Collection member behavior parity.
7. Inherited member behavior parity (if type hierarchy involved).
8. Generic behavior parity (if generic contracts involved).
9. Registration timing parity (assembly load/module init/bootstrap ordering).
10. Profile-specific serializer resolution parity (`Default`/`Content` as applicable).

---

## 10) Unsupported/deferred cases (MVP must diagnose/reject)

- complex/open generic edge cases beyond fixture contract,
- nested types depending on outer generic parameters,
- inaccessible private member shapes requiring non-public access patterns not yet implemented,
- polymorphic/version-tolerance advanced scenarios,
- custom serializer/member custom serializer combinations not in MVP matrix,
- content-specific profile subtleties not explicitly covered,
- advanced reference-tracking semantics if fixture does not exercise them,
- serializer-profile interactions outside `Default` unless explicitly implemented.

---

## 11) Coexistence hazards

1. Duplicate registration when AP + sourcegen register same type.
2. Registration precedence conflicts (`TryAdd` behavior can hide one path silently).
3. Module initializer ordering differences.
4. Partial migration across dependent projects causing mixed serializer tables.
5. Generated type-name collisions in `Stride.Core.DataSerializers`.
6. Incremental generator stale outputs causing drift.
7. Circular dependency risk if generator package referenced from core build layers improperly.

Mitigation requirement: per-project opt-in + duplicate-detection diagnostics + parity tests per assembly.

---

## 12) Proposed M11c implementation prompt

> Implement only a **test-fixture MVP** of `Stride.Core.Serialization.Generator`.
> 
> Constraints:
> - Do not migrate real runtime assemblies yet.
> - Do not remove AP serialization globally.
> - Keep `[DataContract]/[DataMember]/[DataMemberIgnore]` semantics.
> 
> Deliver:
> 1. Generator emits serializer + registrar for one fixture `[DataContract]` type.
> 2. Fixture assembly builds with `StriVUseSerializationSourceGenerator=true` and without AP `--serialization`.
> 3. Tests prove compilation, registration discovery (`SerializerSelector.Default.GetSerializer<T>()`), and round-trip correctness.
> 4. Add diagnostics for unsupported member/type shapes rather than silent behavior changes.

---

## 13) Validation

### Command 1
- Command: `dotnet test striv/tests/StriV.CleanGraph.Tests/StriV.CleanGraph.Tests.csproj -v minimal`
- Exit code: _recorded below after execution in this task_.
- First meaningful warning/error: _recorded below_.
- Pass/fail: _recorded below_.
- Output truncated: _recorded below_.

### Command 2
- Command: `./striv/build/striv-build-core.sh`
- Exit code: _recorded below after execution in this task_.
- First meaningful warning/error: _recorded below_.
- Pass/fail: _recorded below_.
- Output truncated: _recorded below_.


### Executed results (M11b run)
- `dotnet test striv/tests/StriV.CleanGraph.Tests/StriV.CleanGraph.Tests.csproj -v minimal`
  - Exit code: `0`
  - First meaningful warning/error: warning `CS1030` in `ObjectIdBuilder.cs` (`#warning: 'PERF: Do not copy byte-for-byte.'`).
  - Pass/fail: **Pass** (4 passed, 0 failed).
  - Output truncated: **Yes** (tool output token truncation).

- `./striv/build/striv-build-core.sh`
  - Exit code: `0`
  - First meaningful warning/error: warning `CS1030` in `sources/core/Stride.Core/Storage/ObjectIdBuilder.cs` during AP build stage.
  - Pass/fail: **Pass** (build succeeded).
  - Output truncated: **Yes** (tool output token truncation).
