# 1550 M14c — Stride.Core.Reflection TypeExtensions staged replacement validation

## 1) Files changed

- `docs/stri-v/audits/1000+/1550-m14c-core-reflection-typeextensions-replacement-validation.md`

## 2) Task scope

This pass stayed within a conservative M14c scope:

- staged `TypeExtensions` replacement review only;
- no descriptor-core rewrite;
- no `OldCollectionDescriptor` removal;
- no serialization/AP behavior refactor;
- no warning-cleanup-driven churn;
- no public API/namespace deletion.

Result: no additional safe internal `TypeExtensions` callsite replacements were found beyond the M14b nullable replacement already in place.

## 3) TypeExtensions inventory/update table

| Helper | Current callsites | Decision | Action | Rationale |
| ------ | ----------------- | -------- | ------ | --------- |
| `HasInterface(Type, Type)` | No in-project callsites found (helper still public) | Keep | No code change | Compatibility surface; thin wrapper over `GetInterface` and potentially external consumers. |
| `GetInterface(Type, Type)` | `ListDescriptor`, `SetDescriptor`, `DictionaryDescriptor`, `OldCollectionDescriptor` | Keep | No code change | Generic definition matching and descriptor collection semantics are load-bearing; direct substitution risks drift. |
| `Default(Type)` | No in-project callsites found | Keep | No code change | Behavior-sensitive default creation utility; public helper retained for compatibility. |
| `IsAnonymous(Type)` | `ObjectDescriptor` | Keep | No code change | Nontrivial semantics (cache + compiler-generated heuristics) used in descriptor behavior. |
| `IsNumeric(Type)` | `ObjectDescriptor` | Keep | No code change | Classification behavior may be contract-sensitive in member/default handling. |
| `IsIntegral(Type)` | Called by `IsNumeric` | Keep | No code change | Internal utility for numeric classification; no low-risk replacement need. |
| `IsNullable(Type)` | No in-project callsites found in `Stride.Core.Reflection` (M14b already migrated primary site) | Keep | No code change | Public helper retained; internal nullable checks already use `Nullable.GetUnderlyingType`. |
| `IsStruct(Type)` | No in-project callsites found | Keep | No code change | Semantics may differ from simplistic value-type checks; defer. |
| `IsPureValueType(Type)` | Recursive self-use only | Keep | No code change | Recursive contract helper; no active internal migration target. |

## 4) Replacements applied

No new callsite replacements were applied in M14c.

Validation confirmed the prior M14b nullable replacement remains active:

- `NullableDescriptor.IsNullable(Type)` uses `Nullable.GetUnderlyingType(type) is not null`.

Build validation was executed for project and solution scope (see sections 7 and 8).

## 5) Helpers kept/deferred

All helpers were kept.

Reasons by class:

- **Descriptor-coupled helpers** (`GetInterface`, `IsAnonymous`, `IsNumeric`) were deferred to avoid descriptor-core behavior drift.
- **Compatibility/public helpers with no immediate internal wins** (`HasInterface`, `Default`, `IsStruct`, `IsPureValueType`, `IsNullable`) were retained intact.

Future migration condition:

- Replace only when a callsite can be shown to be exact-semantics equivalent and covered by focused tests or trivially provable behavior.

## 6) Behavior compatibility

- Descriptor/member/attribute-registry behavior unchanged.
- Serialization/AP path unchanged.
- `OldCollectionDescriptor` kept and still reachable via factory fallback.
- No public API or namespace breaking change introduced.

## 7) Warning snapshot

Focused build (`Stride.Core.Reflection.csproj`) after M14c:

- Build exit code: `0`
- Reported warning total in build output: `31`
- Grep-derived focused warning lines: `62` (includes duplicated warning echo in final summary section)
- Top warning codes from grep snapshot:
  - `CS8618`: 42
  - `CS8604`: 12
  - `CS8620`: 2
  - `CS8603`: 2
  - `CS8602`: 2
  - `CS0618`: 2

Compared to M14b intent, M14c did not attempt warning cleanup and did not alter descriptor semantics.

## 8) Validation results

| Command | Exit | First meaningful warning/error | Pass/Fail | Output truncated |
| ------- | ---- | ------------------------------ | --------- | ---------------- |
| `sed -n '1,360p' striv/projects/Stride.Core.Reflection/TypeExtensions.cs` | 0 | none | Pass | No |
| `rg -n "GetInterface\\(|HasInterface\\(|IsNullable\\(|NullableArgument\\(|Default\\(|IsNumeric\\(|IsIntegral\\(|IsStruct\\(|IsPureValueType\\(|IsAnonymous\\(" ...` | 0 | none | Pass | No |
| `dotnet build striv/projects/Stride.Core.Reflection/Stride.Core.Reflection.csproj -c Debug -p:StriVWarningFocusProject=Stride.Core.Reflection --no-incremental` | 0 | `CS8618` non-nullability warnings in `MemberDescriptorBase` | Pass | Yes (terminal capture limit) |
| `grep ... > /tmp/striv-m14c-reflection-warning-lines.log` | 0 | none | Pass | No |
| `wc -l /tmp/striv-m14c-reflection-warning-lines.log` | 0 | none | Pass | No |
| `sed -E 's/.*warning ((CS|CA|NU|STRIDE)[0-9]+).*/\\1/' ... | sort | uniq -c | sort -nr` | 0 | none | Pass | No |
| `dotnet build striv/StriV.Core.slnx -c Debug -p:StriVWarningFocusProject=Stride.Core.Reflection --no-incremental` | 0 | `CS8618` warnings in `Stride.Core.Reflection` | Pass | Yes (terminal capture limit) |
| `./striv/build/striv-check-focused-projects.sh Stride.BepuPhysics Stride.Core.Mathematics Stride.Core.IO Stride.Input Stride.Games` | 0 | none | Pass | No |
| `dotnet test striv/tests/Stride.Games.Tests/Stride.Games.Tests.csproj -v minimal` | 0 | none | Pass | No |
| `dotnet test striv/tests/Stride.Input.Tests/Stride.Input.Tests.csproj -v minimal` | 0 | none | Pass | No |
| `dotnet test striv/tests/StriV.CleanGraph.Tests/StriV.CleanGraph.Tests.csproj -v minimal` | 0 | build emits existing warnings in unrelated projects; tests pass | Pass | Yes (terminal capture limit) |
| `dotnet test striv/tests/StriV.AssetTool.Tests/StriV.AssetTool.Tests.csproj -v minimal` | 0 | none | Pass | No |
| `dotnet test striv/tests/StriV.AssetPipeline.Tests/StriV.AssetPipeline.Tests.csproj --no-build -v minimal` | 0 | none | Pass | No |
| `dotnet test striv/tests/StriV.ShaderPipeline.Tests/StriV.ShaderPipeline.Tests.csproj --no-build -v minimal` | 0 | one pre-existing skipped test | Pass | No |
| `./striv/build/striv-build-core.sh` | 0 | `CS8618` warnings in `Stride.Core.Reflection` during solution build stage | Pass | Yes (terminal capture limit) |

## 9) Recommended next task

Recommended: **M14d consumer-proof `OldCollectionDescriptor` quarantine plan**.

Why this next:

- M14c replacement surface for low-risk `TypeExtensions` callsites is effectively exhausted.
- `OldCollectionDescriptor` remains intentionally active and warning-visible through factory fallback.
- A consumer-proof quarantine plan can narrow future change risk without forcing premature descriptor-core rewrites.
