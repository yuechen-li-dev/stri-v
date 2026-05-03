# 030 – Asset/Serialization/Quantum/AssemblyProcessor Static Architecture Audit

## 1) Evidence collection
- **Scope respected:** static source/asset inspection only; no builds/tests/tools executed.
- **Commands used (representative):**
  - `find .. -name AGENTS.md -print` (no AGENTS.md found).
  - `rg -n "..." sources build ...` with the requested search terms.
  - `rg --files ... | rg '\.(sdpkg|sdscene|sdmat|sdfnt|sdfx|sdsl)$'` to enumerate on-disk asset samples.
  - `sed -n 'start,endp' <file>` for targeted file reads.
- **Search terms used:** `AssetId`, `Yaml`, `AssetReference`, `AssetItem`, `PackageSession`, `AssetCompiler`, `ContentManager`, `ObjectId`, `Guid`, `Quantum`, `NodeContainer`, `AssetPropertyGraph`, `Override`, `Archetype`, `BasePart`, `StrideAssemblyProcessor`, `AssemblyProcessorTask`, `ModuleInitializer`, `DataSerializer`, `AssemblyRegistry`, `DataContract`, `DataMember`, `ContentSerializerAttribute`.
- **Files opened/read:** core evidence from `sources/assets/Stride.Core.Assets*`, `sources/assets/Stride.Core.Assets.Quantum*`, `sources/core/Stride.Core.Yaml*`, `sources/core/Stride.Core.Serialization*`, `sources/core/Stride.Core.AssemblyProcessor*`, `sources/sdk/Stride.Build.Sdk/Sdk/Stride.AssemblyProcessor.targets`, plus representative `.sd*` assets under `sources/engine/*`.
- **Skipped commands:** full `dotnet build`, tests, asset compiler invocations (per constraints).
- **No-modification confirmation:** before report creation this was read-only inspection; this report file is the only intended new artifact for audit output.

## 2) High-level system map
```text
On-disk source assets (.sd*)
  -> Package/session model (Package, PackageSession, AssetItem)
  -> YAML asset serialization (AssetFileSerializer -> YamlAssetSerializer -> AssetYamlSerializer)
     -> Asset IDs/refs (AssetId + AssetReference "GUID:Location")
     -> YAML metadata sidechannels (override/object-reference metadata)
  -> (Editor-time) Quantum property graph layer
     -> override/archetype/base-part reconciliation + node graph diffs
  -> Asset compiler/build pipeline (IAssetCompiler/AssetCompilerRegistry + BuildEngine commands)
  -> runtime content DB/chunks (ObjectId/ObjectUrl + ContentManager/ContentSerializer)
  -> build-time AssemblyProcessor (MSBuild task + IL processing/codegen/module init/serializer registration)
Game Studio/editor sits across package/yaml/quantum/compiler layers.
Runtime loading uses ContentManager/DataSerializer path, not Quantum.
```

## 3) On-disk asset format audit
Representative files:
- `sources/engine/Stride.Graphics/Stride.Graphics.sdpkg` – package YAML (`!Package`, `SerializedVersion`, `AssetFolders`).
- `sources/engine/Stride.Engine.Tests/GameAssets/MainScene.sdscene` – scene YAML (`!SceneAsset`, `Id`, `Hierarchy`).
- `sources/engine/Stride.Engine.Tests/GameAssets/01-Default.sdmat` – material YAML with typed tags and inline asset reference (`Texture: <guid>:megalodon`).
- `sources/engine/Stride.Engine.Tests/GameAssets/Font.sdfnt` – font YAML (`!SpriteFont`, `FontSource`, `FontType`).
- `sources/engine/Stride.Graphics/Shaders/SpriteBatch.sdfx` and `.sdsl` – effect/shader source DSL (not YAML object graph like `.sdmat`).

Observed traits:
- Strong YAML type tags (`!MaterialAsset`, `!ComputeTextureColor`, etc.).
- Asset identity persisted as GUIDs (`Id:`), references often as `GUID:location` strings.
- Serialized version map present for upgrade logic.
- Hand-editability: medium for simple assets; low-medium when nested polymorphic graphs/override metadata are involved.
- LLM-editability: medium with strict validation tooling; risks from hidden metadata and cross-asset ID coupling.

## 4) Asset identity and GUID/ID model
- `AssetId` is a `Guid` wrapper; generation uses `Guid.NewGuid()` via `AssetId.New()` (non-deterministic). 
- `AssetReference` stores **both** `Id` and `Location`; parser expects `GUID:Location` (or legacy prefixed variant).
- `AssetItem.Id` delegates to contained `Asset.Id` and location is independently tracked.
- References are therefore mixed identity: ID + path-like location; changing IDs without coordinated rewrite can break ref resolution and inheritance links.
- Collision/repair behavior exists in tests/analysis utilities, but no evidence of globally deterministic ID regen as default workflow.

## 5) YAML serialization architecture
- YAML stack: in-repo SharpYaml fork (`Stride.Core.Yaml`, fork note in `YamlAssemblyRegistry.cs` header).
- Entry points:
  - `AssetFileSerializer` selects serializer by extension.
  - `YamlAssetSerializer` loads/saves and processes attached YAML metadata.
  - `AssetYamlSerializer` configures serializer pipeline (`ProfileSerializerFactorySelector(..., "Assets")`, type tags, backend).
- Custom backend: `AssetObjectSerializerBackend` handles override postfix parsing/emit, path tracking (`YamlAssetPath`), collection item identity hooks, and metadata keys (`OverrideDictionaryKey`, `ObjectReferencesKey`).
- Versioning/upgrades: asset/package classes carry `SerializedVersion` + `AssetFormatVersion`/`AssetUpgrader` attributes.
- Polymorphism encoded with YAML tags (`!TypeName`); aliases depend on assembly-processed serializer metadata.
- Important boundary: this YAML path is primarily source/editor/package layer, not runtime content DB format.

## 6) Package/project/session architecture
- `Package` is serializable package root (`.sdpkg`) with asset folders, root assets, bundles, template/resource folders, serialized versioning, and state machine (`Raw/DependenciesReady/UpgradeFailed/AssetsReady`).
- `AssetItem` binds `(Location, Asset, Package)` + dirty/version/source-folder metadata and YAML metadata attachment.
- `PackageSession` + containers manage multi-package dependency topology, save/load orchestration, path normalization, project integration.
- Dependency/project data linked to MSBuild and NuGet-oriented package structures.

## 7) Quantum audit
- Quantum here is **property graph/editing infrastructure**, centered in `Stride.Core.Assets.Quantum` and `Stride.Core.Quantum`.
- Core problem solved: live editable graph with inheritance/archetype reconciliation, per-node override tracking, and propagation between base/derived assets.
- Core types: `AssetPropertyGraph`, `AssetQuantumRegistry`, `AssetNodeContainer`, `IAssetNode`/`IAssetObjectNode`/`IAssetMemberNode`, linker/reconciler/visitor types.
- Startup registration via `[ModuleInitializer]` in `Stride.Core.Assets.Quantum/Module.cs`.
- Runtime use: no direct evidence that runtime content loading depends on Quantum; it appears editor/source-asset centric.
- Coupling: moderate-high to reflection/type metadata and YAML override metadata, but conceptually isolatable behind editor boundary.

## 8) Asset property graph / override system
- Override model spans YAML metadata and Quantum graph:
  - YAML backend parses override suffixes and stores path-based override metadata.
  - `AssetPropertyGraph` applies overrides, links to archetype/base nodes, reconciles in passes, tracks node/item changes.
- Archetype/base composition is explicit in `Asset.Archetype`/`BasePart` references (ID+location-based links).
- This is likely editor-critical, high complexity for manual edits.

## 9) Asset compiler/build pipeline audit
- Compiler abstractions in `Stride.Core.Assets.Compiler`: `IAssetCompiler`, `AssetCompilerBase`, `AssetCompilerRegistry`, `AssetCompilerResult`, `AssetCompilerContext`.
- Discovery is reflection + `AssetCompilerAttribute` across assemblies registered in `AssemblyRegistry` category `Assets`.
- Per-asset prepare yields build steps/commands (e.g., `RawAssetCompiler` emits `ImportStreamCommand`).
- Build outputs then flow through build engine/object database (`ObjectId`, `ObjectUrl`) and content indexing.
- External/native tooling paths exist for certain asset classes (not executed here), so simplification potential is asset-type dependent.

## 10) Runtime content loading/serialization audit
- Runtime loader path: `Stride.Core.Serialization.Contents.ContentManager` + `ContentSerializer` + database/file provider services.
- Uses object DB URLs/chunks and `DataSerializer` infrastructure, distinct from source YAML.
- `AssetId` exists in core serialization namespace, but runtime content references are largely URL/object-db mediated.
- No evidence runtime loading requires Quantum.

## 11) AssemblyProcessor audit
- Implementation: `sources/core/Stride.Core.AssemblyProcessor/*` (`AssemblyProcessorTask`, `AssemblyProcessorProgram`, `AssemblyProcessorApp`, `SerializationProcessor`, etc.).
- Invocation: `sources/sdk/Stride.Build.Sdk/Sdk/Stride.AssemblyProcessor.targets` (and legacy core targets) copy processor binaries into temp hashed folder and `UsingTask` loads `AssemblyProcessorTask` from that DLL.
- Options widely enabled in csproj (`StrideAssemblyProcessor=true` + flags such as `--serialization`, `--auto-module-initializer`, `--parameter-key`).
- Responsibilities (direct evidence): IL/post-processing with Mono.Cecil, serializer code generation (`SerializationProcessor`), module initializer wiring, scan/registry artifacts.
- Why “Bad IL format” might occur on Linux sandbox (inference): `UsingTask AssemblyFile=...netstandard2.0\Stride.Core.AssemblyProcessor.dll` loaded by MSBuild host; mismatch/corruption/loader incompatibility in copied temp processor payload would fail before compile.
- If disabled broadly, serializer alias metadata/module initialization/parameter-key bootstrap and other generated registration likely break across runtime+editor.

## 12) Reflection/codegen/registration model
- Reflection: assembly scans for compilers and YAML serializable factories.
- Codegen/IL weaving: AssemblyProcessor generates serializers and related registration metadata (instead of purely runtime reflection).
- Registration concepts connected:
  - `[DataContract]/[DataMember]` feed serializers and YAML aliases.
  - `DataSerializerFactory.GetAssemblySerializers(...)` data consumed by YAML registry; warns when assembly wasn’t processed with `--serialization`.
  - `[ModuleInitializer]` used for auto-registration (e.g., Quantum module).
  - `AssemblyRegistry` categories route discovery across subsystems.

## 13) Human/LLM editability assessment
- `.sdpkg`: hand **high**, LLM **high-** (simple but schema-sensitive).
- `.sdscene`: hand **medium**, LLM **medium** (graph consistency, IDs).
- `.sdmat`: hand **medium-low**, LLM **low-medium** (deep polymorphic tags + references).
- `.sdfnt`: hand **medium**, LLM **medium**.
- `.sdfx/.sdsl`: hand **high** (for shader engineers), LLM **medium-high**.

Main hazards:
- Non-deterministic GUIDs/AssetIds.
- Dual ID+location references.
- Override/archetype/base-part metadata (often implicit via metadata path maps).
- Compiler side effects and generated runtime artifacts.
- Potential order/style sensitivity from serializer conventions.

Needed safety tooling for reliable manual/LLM edits:
- ID/reference validator + repair dry-run.
- YAML schema/type-tag linter.
- Graph/override inspector (read-only first).
- deterministic formatter/normalizer that preserves semantic metadata.

## 14) Hardfork simplification opportunities
- **Safe documentation/tooling:** asset reference validator; YAML tag/schema docs; read-only graph inspector.
- **Low-risk isolation:** enforce Quantum/editor-only boundary in build graph; keep runtime content loader independent.
- **Medium-risk refactor:** asset manifest/index generation; ID/reference reconciliation utilities; visual-scripting asset path quarantine.
- **High-risk rewrite:** replacing AssemblyProcessor end-to-end; replacing YAML authoring with new DSL; deterministic ID redesign across legacy assets.
- **Do not touch yet:** broad removal of AssemblyProcessor before build/runnable canary is restored.

## 15) Load-bearing risk register
| System | Representative paths/types | Appears runtime/editor/build-time | Load-bearing level | Why | Can remove? | Audit confidence |
|---|---|---|---|---|---|---|
| YAML asset serialization | `AssetFileSerializer`, `YamlAssetSerializer`, `AssetYamlSerializer` | editor/source/build input | High | Entry format for source assets/packages | Not short-term | High |
| Package/session model | `Package`, `PackageSession`, `AssetItem` | editor/build orchestration | High | Organizes all source assets/dependencies | Not short-term | High |
| Asset IDs/references | `AssetId`, `AssetReference` | source+build(+some runtime metadata) | High | Core identity/linking | Only with migration tooling | High |
| Quantum | `AssetPropertyGraph`, `AssetQuantumRegistry` | editor | Medium-High | Override/archetype editing backbone | Likely isolatable, hard to delete immediately | Medium |
| Property graph/overrides | Quantum + YAML override metadata | editor/source fidelity | High (for editor) | Needed for nontrivial inheritance workflows | Runtime path might avoid, editor cannot | Medium |
| Asset compiler | `IAssetCompiler`, registry/context | build-time | High | Source -> runtime content transform | No (unless replacing pipeline) | High |
| Runtime content serialization | `ContentManager`, `ContentSerializer`, object DB | runtime | Critical | Actual game content load path | No | High |
| AssemblyProcessor | `AssemblyProcessorTask`, sdk targets, processor code | build-time cross-cutting | Critical currently | Enables serializer/module-init/etc. and currently blocks build when broken | Not before replacement | High |
| Data serializers/registration | `DataSerializer*`, assembly serializer metadata | runtime+build | Critical | Runtime serialization and YAML alias mapping depend on it | No | Medium-High |
| Shader/effect path | `.sdsl/.sdfx`, graphics compilers | build+runtime | High for rendering | Required for graphics pipeline | No | Medium |
| Font/MSDF path | `.sdfnt` + font toolchain | build/runtime | Medium-High | Font content path; native-tool simplification possible later | Partial | Medium |
| Visual scripting path | (not deeply sampled here) | editor/build | Medium | likely removable per hardfork goals but with migration impacts | Yes, staged | Low-Medium |

## 16) Recommended next audit
**Next most important audit: AssemblyProcessor-focused deep dive.**

Reason: current canary progression is blocked by `MSB4062`/`AssemblyProcessorTask` loading failure, and this processor underpins serializer/module-init/registration behavior across most projects. A precise audit of binary production/copy/load pipeline (source build vs deps payload, framework/host compatibility, task-loading mechanics) is the highest leverage prerequisite before safe subsystem removals.

## Unclear/needs deeper follow-up
- Exact per-asset-type external tool dependencies (texture/font/model) were only sampled, not fully enumerated.
- Visual scripting asset integration was not fully traced in this pass.
- Precise runtime dependence on generated serialization hash files and project-level consumption paths deserves dedicated AssemblyProcessor + build-graph follow-up.
