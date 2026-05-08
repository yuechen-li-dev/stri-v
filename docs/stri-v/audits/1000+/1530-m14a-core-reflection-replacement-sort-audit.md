# 1530 M14a — Stride.Core.Reflection replacement/axe viability Sort audit

## 1) Files changed
- `docs/stri-v/audits/1000+/1530-m14a-core-reflection-replacement-sort-audit.md` (report-only).

## 2) Problem statement
`Stride.Core.Reflection` appears to combine runtime serialization metadata discovery, member-path mutation helpers, and older descriptor abstractions. Before warning-cleaning, this audit determines what is truly load-bearing in the current Stri-V runtime/build graph vs. what is likely legacy wrapper/deferred compatibility surface.

## 3) Project inventory
- Target csproj: `striv/projects/Stride.Core.Reflection/Stride.Core.Reflection.csproj`
- Physical file count under project folder: **29** (`/tmp/striv-m14a-reflection-files.txt`).
- `Compile` item output lines: **500** from `dotnet msbuild -getItem:Compile` (tool output includes evaluated metadata lines, not only unique C# files).
- Direct project reference: `Stride.Core.Serialization`.
- Namespace/file grouping:
  - Descriptor factory/registry: `TypeDescriptorFactory`, `AttributeRegistry`, interfaces.
  - Member descriptors: `FieldDescriptor`, `PropertyDescriptor`, `MemberDescriptorBase`.
  - Type descriptors: object/primitive/collection/list/set/dictionary/array/nullable/not-supported.
  - Path editing: `MemberPath`, `MemberPathAction`.
  - Utility wrappers: `TypeExtensions`, naming/comparer helpers.
- Focused warning baseline (project build): **31 warnings** (62 log lines due repeated summary sections), dominated by `CS8618`, then `CS8604`, plus `CS8620/CS8602/CS8603/CS0618`.

## 4) Public surface inventory (major)
- `TypeDescriptorFactory` / `ITypeDescriptorFactory`: central cache+factory that maps `Type -> ITypeDescriptor`.
- `ObjectDescriptor` and derived descriptors: runtime introspection model for members and collection behaviors.
- `IMemberDescriptor` + implementations: abstraction over field/property access + metadata (mask, alt names, defaults, should-serialize).
- `AttributeRegistry` / `IAttributeRegistry`: merged attribute source (reflection + runtime-registered attrs).
- `MemberPath`: path-based object graph mutation (`ValueSet`, collection add/remove, dictionary remove).
- `TypeExtensions`: helper extensions (`GetInterface`, `IsAnonymous`, `IsPureValueType`, etc.).

## 5) Consumer map
| API/type/file | Consumers | Active? | Runtime/build/editor? | Notes |
|---|---|---|---|---|
| `TypeDescriptorFactory`, `ObjectDescriptor`, descriptors | `Stride.Core.Serialization`, `Stride.Core.Serialization.Generator.Tests`, and transitive runtime graph | Yes | Runtime + build/AP-adjacent | Core to serialization metadata/member discovery path. |
| `AttributeRegistry` | Serialization metadata discovery and some attribute lookups | Yes | Runtime/build | Enables merged/custom attribute behavior beyond raw `System.Reflection`. |
| `MemberPath` | Used in reflection/serialization-style mutation workflows; internal references show `PropertyKey<MemberPath>` compatibility note | Likely yes | Runtime compatibility | Comment indicates AP serialization compatibility pressure. |
| `TypeExtensions` | Used inside reflection descriptors; can be replaced incrementally in callers | Yes (internal) | Runtime utility | Mostly thin wrappers; some still used in descriptor logic. |
| `OldCollectionDescriptor` | Instantiated from factory when generic collection path falls through | Yes (currently) | Runtime compatibility | Already `[Obsolete]` in warnings (CS0618), indicates planned retirement. |
| Quantum/editor/property-grid specific APIs | No explicit Quantum/property-grid namespace surface found in this project | No clear active evidence | N/A | Surface appears serializer/descriptor-centric, not editor assembly-specific. |

## 6) Responsibility classification
| Area/file group | Current role | Classification | Proposed action | Rationale |
|---|---|---|---|---|
| `TypeDescriptorFactory` + `TypeDescriptors/*` | Runtime metadata model + collection/object abstraction for serializer path | **Keep** | Keep for M14b base | Strong runtime/load-bearing evidence via serialization coupling. |
| `MemberDescriptors/*` | Unified member abstraction for descriptor model | **Keep** | Keep | Required by descriptor pipeline and member metadata logic. |
| `AttributeRegistry*` | Combined attribute retrieval and runtime registration | **Keep** | Keep | Adds behavior not covered by one-call BCL attribute lookup. |
| `MemberPath*` | Graph mutation abstraction | **Defer** | Keep until AP/sourcegen migration clarifies replacements | Entangled with compatibility usage patterns; remove risk high now. |
| `TypeExtensions.cs` | Reflection convenience wrappers | **Replace with BCL (incremental)** | Candidate for gradual callsite replacement | Many methods are thin wrappers around modern BCL APIs. |
| `Default*Comparer`, naming convention helper | Deterministic member ordering/name mapping | **Keep (minimal)** | Keep unless downstream callsites flattened | Small, low-risk utility with deterministic behavior value. |
| `OldCollectionDescriptor.cs` | Legacy collection compatibility path | **Defer / quarantine candidate** | Quarantine in M14b plan (no deletion yet) | Already obsolete and likely removable after consumer proof. |

## 7) Quantum/editor/property-grid audit
- No direct `Quantum`, `PropertyGrid`, editor module namespaces, or clear design-time-only entry points were identified in `Stride.Core.Reflection` sources.
- Existing responsibilities appear centered on runtime serialization-style descriptors and member/attribute mediation.
- Conclusion: this project is not obviously editor/property-grid heavy by itself in current Stri-V tree; risk is more about **legacy descriptor over-abstraction**, not editor coupling.

## 8) System.Reflection wrapper audit
Likely thin-wrapper zones:
- `TypeExtensions.GetInterface/HasInterface/IsNullable/Default/IsNumeric/IsIntegral/IsStruct`: mostly convenience wrappers around BCL reflection/type APIs.
- Member descriptor classes wrap `PropertyInfo`/`FieldInfo` access with uniform API; these are not purely thin wrappers because they carry naming, masks, should-serialize, alt names.
- `AttributeRegistry` wraps `Attribute.GetCustomAttributes` but adds cache + override registry, so it has non-trivial runtime value.

Replacement ease:
- `TypeExtensions` methods: **easy-to-medium** (callsite-by-callsite).
- Descriptor/member abstractions: **hard** without broad serialization/AP migration.
- `AttributeRegistry`: **medium-hard** due to custom registered-attribute behavior.

## 9) Runtime/AP/serialization dependency audit
- `Stride.Core.Reflection` references `Stride.Core.Serialization` directly in csproj, indicating tight serializer coupling.
- Descriptor pipeline reads `DataContract/DataMember/DataStyle/...` metadata and shapes member exposure; this is core serializer behavior.
- `MemberPath` includes explicit note about assembly processor serializability expectations for `PropertyKey<MemberPath>`.
- Conclusion: runtime/AP path still depends on this assembly for current behavior; broad removal should wait for sourcegen/runtime metadata migration milestones.

## 10) Proposed future purpose of `Stride.Core.Reflection`
`Stride.Core.Reflection should be a minimal runtime metadata and member-shape layer required by Stri-V serialization/source-generation and assembly-processing compatibility, not a general-purpose reflection wrapper library.`

## 11) M14b Sort implementation plan (safest)
1. **Quarantine candidate identification (no deletes yet):** mark `OldCollectionDescriptor` path and `TypeExtensions` thin wrappers as explicit migration targets.
2. **Consumer-proof pass:** map actual external callsites for `TypeExtensions` and `OldCollectionDescriptor` behavior in active projects.
3. **Introduce compatibility boundary folders/namespaces (optional):** `Compatibility/LegacyCollection` and/or `Compatibility/ReflectionWrappers` (no behavior change).
4. **BCL replacement wave:** migrate easiest `TypeExtensions` callsites to direct BCL APIs with tests.
5. **Serialization/AP safety gate:** only attempt descriptor deletions after proving `Stride.Core.Serialization` + AP path unaffected.
6. **Post-sort warning phase:** after structural narrowing, begin focused warning cleanup.

## 12) Warning baseline
- Focused project build warning count: **31** (warnings repeated in log summary output).
- Top codes from project-filtered lines:
  - `CS8618` (dominant)
  - `CS8604`
  - `CS8620`, `CS8602`, `CS8603`
  - `CS0618` (`OldCollectionDescriptor` usage)
- Clusters:
  - Nullability initialization in descriptor/member classes.
  - Nullability flow in `MemberPath` mutation logic.
  - Obsolete legacy collection descriptor path.

## 13) Validation results
| Command | Exit | First meaningful warning/error | Pass/Fail | Output truncated |
|---|---:|---|---|---|
| `dotnet build striv/projects/Stride.Core.Reflection/Stride.Core.Reflection.csproj -c Debug -p:StriVWarningFocusProject=Stride.Core.Reflection --no-incremental` | 0 | `CS8618` in `TypeDescriptors/OldCollectionDescriptor.cs` | Pass | No |
| `dotnet build striv/StriV.Core.slnx -c Debug -p:StriVWarningFocusProject=Stride.Core.Reflection --no-incremental` | 0 | `CS8618` in `MemberDescriptorBase.cs` (project focus warnings shown) | Pass | No |
| `./striv/build/striv-check-focused-projects.sh Stride.BepuPhysics Stride.Core.Mathematics Stride.Core.IO Stride.Input Stride.Games` | 0 | none (all listed as pass, 0 warnings) | Pass | No |
| `./striv/build/striv-build-core.sh` | 0 | global solution warnings (e.g., `CS0436`/nullability in non-target projects) | Pass | Yes (terminal token truncation, full log stored) |

## 14) Recommended next task
**M14b Sort implementation (planning-to-execution) with consumer-migration proof first**, specifically:
- prove `TypeExtensions` BCL replacement feasibility in active consumers,
- fence `OldCollectionDescriptor` as explicit compatibility zone,
- defer descriptor-surface removals until AP/sourcegen migration checkpoints are met.
