# 1280 M11a — AssemblyProcessor serialization source-generator viability audit

## 1) Files changed
- `docs/stri-v/audits/1000+/1280-m11a-assemblyprocessor-serialization-source-generator-audit.md` (report-only).

## 2) Problem statement
AssemblyProcessor (AP) serialization is currently a load-bearing but aging mechanism in Stri-V clean graph. The current system still relies on `[DataContract]`, `[DataMember]`, and `[DataMemberIgnore]` for runtime `DataSerializer<T>` generation; removing those attributes breaks AP serializer generation (already observed in prior clean-graph math cleanup audits).

The same attribute family is also used by non-runtime concerns (asset persistence shape and editor/property metadata), so runtime serializer generation is tightly coupled to concerns that should eventually evolve independently.

Therefore the strategic direction is valid: keep attributes/semantics initially, but replace AP IL weaving for serialization with source generation, reducing post-build mutation while preserving behavior.

## 3) Current AP serialization pipeline

### 3.1 Where serialization processor lives
- Core implementation: `striv/projects/Stride.Core.AssemblyProcessor/SerializationProcessor.cs`.
- AP pipeline composition/selection: `striv/projects/Stride.Core.AssemblyProcessor/AssemblyProcessorApp.cs`.
- CLI option parsing for `--serialization`: `striv/projects/Stride.Core.AssemblyProcessor/AssemblyProcessorProgram.cs`.

### 3.2 How clean graph invokes AP
- Clean graph target `StriVRunAssemblyProcessor` executes AP **after build** if:
  - `$(StriVAssemblyProcessorPath)` is set,
  - `$(StriVAssemblyProcessorOptions)` is non-empty,
  - target assembly exists.
- AP invocation is direct `dotnet "$(StriVAssemblyProcessorPath)" "$(TargetPath)" <options> --references-file=...`.
- References are materialized into `$(IntermediateOutputPath)StriV.AP.references.cache` and passed to AP.

### 3.3 What `--serialization` causes AP to do
When `SerializationAssembly=true`:
1. `AssemblyScanProcessor` runs.
2. `SerializationProcessor` runs.

`SerializationProcessor` then:
- builds serializer metadata context (`CecilSerializerContext`),
- generates `Stride.Core.DataSerializers.*` serializer types inheriting `DataSerializer<T>` or `ClassDataSerializer<T>`,
- emits per-type `Initialize` + `Serialize` methods in IL,
- creates a generated serializer factory type,
- emits `[DataSerializerGlobal]` attributes,
- injects an `Initialize` method that creates and registers `AssemblySerializers` via `DataSerializerFactory.RegisterSerializationAssembly(...)`,
- appends module-constructor call to that generated initialize method,
- writes serialization hash (`*.sdserializationhash`).

### 3.4 Build evidence of AP command lines and processed assemblies
From `./striv/build/striv-build-core.sh` log (`/tmp/striv-m11a-ap-build.log`), AP commands are logged with `[StriV AP]` and include options such as:
- `--auto-module-initializer --serialization` (`Stride.Core`, `Stride.Core.Mathematics`, `Stride.Core.Serialization`),
- `--auto-module-initializer` (`Stride.Core.IO`, `Stride.Core.MicroThreading`),
- `--serialization --parameter-key --auto-module-initializer` (e.g., `Stride.Shaders` in csproj config).

## 4) AP processor inventory

| Processor / flag | Purpose | Still needed after serializer replacement? | Replacement difficulty | Notes |
| --- | --- | --- | --- | --- |
| `--serialization` (`SerializationProcessor`) | Generates/injects serializers + registration/module init hooks | **No** (target to replace) | High | Main M11 track target. |
| `--serialization` (`AssemblyScanProcessor`) | Generates scan-registration type wiring for assembly scan attributes | Maybe | Medium | Triggered under serialization path currently. |
| `--auto-module-initializer` | Ensures module init hookup behavior | Likely yes initially | Medium | Could move to source-level `[ModuleInitializer]` or generated static init shim. |
| `--parameter-key` (`ParameterKeyProcessor`) | Injects parameter key auto-init behavior | Yes (initially) | Medium | Used by rendering/shader-heavy projects. |
| `InteropProcessor` (always-on in AP app) | Interop-related IL processing | Yes (unless independently replaced) | High | Not behind a flag in current app pipeline. |
| `DispatcherProcessor` (always-on) | Dispatcher/closure pooling IL transforms | Likely yes | High | Also always-on currently. |
| `AssemblyVersionProcessor` (always-on) | Assembly version metadata adjustments | Probably yes | Low/Med | Runs regardless of serialization flag. |

## 5) Clean graph dependency map (serialization generation consumers)
Confirmed projects with `--serialization` in current clean graph csproj options:
- `Stride.Core`
- `Stride.Core.Mathematics`
- `Stride.Core.Serialization`
- `Stride`
- `Stride.Engine`
- `Stride.Rendering`
- `Stride.Shaders`
- `Stride.BepuPhysics`

Known runtime-sensitive examples:
- `Color3` and `LightProbeComponent.Coefficients` were previously shown to fail when data-contract attributes were removed.
- `EffectBytecode` serializer availability is explicitly tested in clean graph smoke tests (`SerializerSelector.Default.GetSerializer<EffectBytecode>()`).

## 6) Source generator replacement feasibility

### 6.1 Can a generator read same attributes?
Yes. Roslyn generator can inspect symbols/attributes for `[DataContract]`, `[DataMember]`, `[DataMemberIgnore]` and preserve member ordering/name semantics.

### 6.2 Can it generate `DataSerializer<T>` source?
Yes. It can emit concrete `DataSerializer<T>`/`ClassDataSerializer<T>` partial-independent classes into compile unit source (inspectable/AOT-friendly).

### 6.3 Registration without IL rewriting
Feasible, but this is the hardest contract point:
- current AP path injects runtime registration through module constructor call.
- source generator likely needs either:
  1. generated `[ModuleInitializer]` method per assembly, or
  2. explicit generated registrar type with deterministic call site (manual bootstrap), or
  3. generated attributes + existing discovery hook updates.

### 6.4 Need partial classes?
Not strictly required for target data types (generator can emit sibling types), but partials may help for generated helper stubs.

### 6.5 Need serializer discovery changes?
Possibly minimal if generated code still produces equivalent `AssemblySerializers` registration into `DataSerializerFactory`. If registration mechanism differs, `SerializerSelector`/factory integration may need extension.

### 6.6 Coexistence with AP during migration
Yes, with guardrails:
- per-project opt-in/out flag (`UseSerializationSourceGenerator=true`),
- AP `--serialization` disabled only for opted projects,
- keep AP parameter-key/interop/module-init processors enabled as needed.

## 7) MVP proposal (smallest viable)

### 7.1 Project
- `striv/projects/Stride.Core.Serialization.Generator` (incremental generator).

### 7.2 First target
- one fixture type in tests (safest) **or** one simple real math type (e.g., a compact struct with clear `[DataMember]` shape).

### 7.3 Generated output shape
- serializer type naming parity with current conventions (or deterministic new naming + mapping).
- generated assembly registrar building `AssemblySerializers` entries.
- generated module initializer (or explicit bootstrap method for first probe).
- diagnostics:
  - unsupported member kinds,
  - inaccessible set paths,
  - generic constraint mismatch,
  - missing serializable constructor requirements where applicable.

### 7.4 MVP tests
1. Generator emits expected serializer class (snapshot/semantic assert).
2. `SerializerSelector.Default.GetSerializer<T>()` returns non-null for target type.
3. Round-trip serialize/deserialize equals baseline behavior.
4. No AP `--serialization` needed for the opted fixture project.

## 8) Migration plan (staged)
- **M11b:** formalize generator contract against AP behavior (member selection, ordering, profiles, aliases, inheritance).
- **M11c:** implement fixture-only generator MVP + tests.
- **M11d:** prove one real clean-graph type/project end-to-end.
- **M11e:** disable AP `--serialization` for that project (or globally if validated), while keeping AP for parameter-key/interop/module-init initially.

## 9) Risks
- Behavioral mismatch vs AP-generated serializers (ordering, default handling, assign-back semantics).
- Registration parity risk (module init timing, profile population, aliases, referenced modules).
- Private/internal member access mismatches from generated code context.
- Generic serializers and inherited contracts.
- Collection/nested object corner cases.
- Polymorphic/version-tolerance behavior drift.
- Distinction between content serializers and data serializers.
- Build ordering / incremental generator invalidation edge cases.
- Temporary coexistence duplicate registration collisions.

## 10) Validation

### Command 1
- Exact command: `dotnet test striv/tests/StriV.CleanGraph.Tests/StriV.CleanGraph.Tests.csproj -v minimal`
- Exit code: `0`
- First meaningful warning/error: warning `CS1030` in `striv/projects/Stride.Core/Storage/ObjectIdBuilder.cs` (existing warning debt; non-blocking).
- Pass/fail: **PASS**
- Output truncated: **Yes** (tool output truncated due size; terminal log captured at `/tmp/striv-m11a-test.log`).

### Command 2
- Exact command: `./striv/build/striv-build-core.sh`
- Exit code: `0`
- First meaningful warning/error: warning `CS1030` in `sources/core/Stride.Core/Storage/ObjectIdBuilder.cs` during AP build bootstrap.
- Pass/fail: **PASS**
- Output truncated: **Yes** (tool output truncated due size; terminal log captured at `/tmp/striv-m11a-ap-build.log`).

## 11) Recommended next task
**Recommended:** M11b source-generator design prompt/spec task.

Rationale: highest leverage now is pinning exact AP behavioral contract and registration semantics before coding. The risk center is not serializer class emission itself, but parity of registration/discovery/module-init timing.
