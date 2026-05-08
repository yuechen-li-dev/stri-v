# 1540 — M14b `Stride.Core.Reflection` Sort implementation + validation

## 1) Files changed
- `striv/projects/Stride.Core.Reflection/TypeDescriptors/NullableDescriptor.cs`
- `striv/projects/Stride.Core.Reflection/TypeDescriptorFactory.cs`
- `striv/projects/Stride.Core.Reflection/README-5s-boundary.md` (new)

No file moves performed.

## 2) Task scope
This pass is a conservative **Sort** pass for `Stride.Core.Reflection`, not a Shine pass:
- no descriptor core removals;
- no broad warning cleanup;
- no serialization behavior refactor;
- no API deletions without consumer proof.

## 3) TypeExtensions consumer-proof table
| Helper | What it wraps/does | Consumers | Decision | Action |
| ------ | ------------------ | --------- | -------- | ------ |
| `HasInterface(Type)` | boolean over `GetInterface` | none in clean graph | Defer/future | Keep for compatibility surface; no safe deletion proof yet. |
| `GetInterface(Type)` | generic/interface assignability helper with generic-def handling | Internal: `ListDescriptor`, `SetDescriptor`, `DictionaryDescriptor`, `OldCollectionDescriptor`; external clean graph: none found | Keep | Heavily used by descriptor code; replacement would add churn in load-bearing area. |
| `Default(Type)` | default value via `Activator.CreateInstance` for value types | none found | Defer/future | Keep; no forced migration in M14b. |
| `IsAnonymous(Type)` | cached anonymous-type detection | Internal: `ObjectDescriptor` | Keep | Descriptor/member behavior coupling; no change. |
| `IsNumeric(Type)` | numeric classification | Internal: `ObjectDescriptor` | Keep | Descriptor behavior sensitive; no churn. |
| `IsIntegral(Type)` | integral primitive classification | only via `IsNumeric` | Keep | No standalone callsite migration. |
| `IsNullable(Type)` | `Nullable.GetUnderlyingType(type) != null` wrapper | Internal: `NullableDescriptor` (only) | Replace now | Replaced single internal callsite in `NullableDescriptor` static helper with direct BCL call. |
| `IsStruct(Type)` | non-primitive non-enum value type check | none found | Defer/future | Keep pending broader compatibility audit. |
| `IsPureValueType(Type)` | recursive value-type purity check | none found | Defer/future | Keep as compatibility utility; no current migration. |

## 4) TypeExtensions changes
One mechanical replacement performed:
- Old: `NullableDescriptor.IsNullable(Type)` returned `type.IsNullable()`.
- New: returns `Nullable.GetUnderlyingType(type) is not null`.
- Equivalence: this is the exact logic used by `TypeExtensions.IsNullable`; semantics unchanged.

No helper deletions were performed.

## 5) OldCollectionDescriptor decision
Evidence shows it is still active in runtime descriptor creation path:
- `TypeDescriptorFactory.Create` still instantiates `OldCollectionDescriptor` on `CollectionDescriptor.IsCollection(type)` fallback.
- Therefore it is not removable in M14b.

Action in M14b:
- kept file in place (no move);
- added explicit compatibility/fallback comment at factory callsite to fence intent.

## 6) Boundary documentation
Added `README-5s-boundary.md` with:
- project purpose;
- keep zones;
- candidate replacement zones;
- deferred AP/sourcegen coupling;
- rule preferring BCL reflection APIs for new Stri-V code.

## 7) Behavior compatibility
- Descriptor/member/attribute-registry core behavior was not changed.
- Serialization/AP path preserved.
- No public API deletions or namespace changes.

## 8) Warning snapshot
Focused project build log aggregation produced 62 warning lines because MSBuild echoes warnings twice (live + summary). Unique code distribution from extracted lines:
- `CS8618`: 42
- `CS8604`: 12
- `CS8620`: 2
- `CS8603`: 2
- `CS8602`: 2
- `CS0618`: 2

Interpretation: no meaningful warning-profile change from this conservative pass.

## 9) Validation results
| Command | Exit | First meaningful warning/error | Pass/Fail | Output truncated |
| --- | ---: | --- | --- | --- |
| `dotnet build striv/projects/Stride.Core.Reflection/Stride.Core.Reflection.csproj -c Debug -p:StriVWarningFocusProject=Stride.Core.Reflection --no-incremental 2>&1 | tee /tmp/striv-m14b-reflection-build.log` | 0 | `CS8618` in `ObjectDescriptor` | Pass | No |
| `grep -E "warning (CS|CA|NU|STRIDE)[0-9]+" /tmp/striv-m14b-reflection-build.log | grep -E "striv/projects/Stride.Core.Reflection|/striv/projects/Stride.Core.Reflection|Stride.Core.Reflection.csproj" > /tmp/striv-m14b-reflection-warning-lines.log || true` | 0 | n/a | Pass | No |
| `wc -l /tmp/striv-m14b-reflection-warning-lines.log` | 0 | n/a | Pass | No |
| `sed -E 's/.*warning ((CS|CA|NU|STRIDE)[0-9]+).*/\1/' /tmp/striv-m14b-reflection-warning-lines.log | sort | uniq -c | sort -nr` | 0 | n/a | Pass | No |
| `dotnet build striv/StriV.Core.slnx -c Debug -p:StriVWarningFocusProject=Stride.Core.Reflection --no-incremental 2>&1 | tee /tmp/striv-m14b-reflection-slnx.log` | 0 | `CS8618` in `Stride.Core.Reflection` | Pass | No |
| `./striv/build/striv-check-focused-projects.sh Stride.BepuPhysics Stride.Core.Mathematics Stride.Core.IO Stride.Input Stride.Games` | 0 | none | Pass | No |
| `dotnet test striv/tests/Stride.Games.Tests/Stride.Games.Tests.csproj -v minimal` | 0 | none | Pass | No |
| `dotnet test striv/tests/Stride.Input.Tests/Stride.Input.Tests.csproj -v minimal` | 0 | none | Pass | No |
| `dotnet test striv/tests/StriV.CleanGraph.Tests/StriV.CleanGraph.Tests.csproj -v minimal` | 0 | widespread existing nullable warnings in transitive projects during build stage | Pass | Yes (tool output capped) |
| `dotnet test striv/tests/StriV.AssetTool.Tests/StriV.AssetTool.Tests.csproj -v minimal` | 0 | none | Pass | Yes (tool output capped) |
| `dotnet test striv/tests/StriV.AssetPipeline.Tests/StriV.AssetPipeline.Tests.csproj --no-build -v minimal` | 0 | none | Pass | Yes (tool output capped) |
| `dotnet test striv/tests/StriV.ShaderPipeline.Tests/StriV.ShaderPipeline.Tests.csproj --no-build -v minimal` | 0 | one intentional skipped test | Pass | Yes (tool output capped) |
| `./striv/build/striv-build-core.sh` | 0 | existing `Stride.Core.Reflection` nullable warnings surfaced during solution build stage | Pass | Yes (tool output capped) |

## 10) Recommended next task
**Recommended:** M14c `TypeExtensions` staged replacement.

Rationale:
- consumer-proof map is now explicit;
- one mechanical BCL substitution was validated;
- next safe progression is incremental callsite migration for low-risk helpers while keeping descriptor-core behavior unchanged.
