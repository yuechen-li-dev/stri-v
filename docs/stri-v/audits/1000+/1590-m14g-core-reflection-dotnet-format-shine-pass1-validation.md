# 1590 — M14g Core Reflection dotnet format Shine pass 1 validation

## 1) Files changed
- `striv/projects/Stride.Core.Reflection/AttributeRegistry.cs`
- `striv/projects/Stride.Core.Reflection/IMemberNamingConvention.cs`
- `striv/projects/Stride.Core.Reflection/TypeDescriptors/DictionaryDescriptor.cs`

## 2) Task scope
M14g performed a **scoped mechanical pre-pass** for `Stride.Core.Reflection` using `dotnet format` only for safe diagnostics (`CS8618`, `CS8625`, `CS8600`, `CS8601`, `CS8603`) and then reviewed resulting diffs. This was **not** a descriptor-core rewrite, **not** an `OldCollectionDescriptor` deletion, and **not** a whole-solution formatting/refactoring pass.

## 3) Before warnings
Focused baseline commands were run and captured to `/tmp/striv-m14g-reflection-before.log`.

- Total focused warning lines before: **62**
- Distribution before:
  - `CS8618`: 42
  - `CS8604`: 12
  - `CS8620`: 2
  - `CS8603`: 2
  - `CS8602`: 2
  - `CS0618`: 2

Bucket table before (from `/tmp/striv-m14g-reflection-warning-buckets-before.log`):

| Count | File | Warning |
|---:|---|---|
| 22 | `MemberDescriptors/MemberDescriptorBase.cs` | `CS8618` |
| 12 | `TypeDescriptors/Compatibility/OldCollectionDescriptor.cs` | `CS8618` |
| 8 | `MemberPath.cs` | `CS8604` |
| 6 | `TypeDescriptors/ObjectDescriptor.cs` | `CS8618` |
| 4 | `TypeDescriptors/ObjectDescriptor.cs` | `CS8604` |
| 2 | `TypeDescriptors/CollectionDescriptor.cs` | `CS8618` |
| 2 | `TypeDescriptorFactory.cs` | `CS0618` |
| 2 | `MemberPath.cs` | `CS8620` |
| 2 | `MemberPath.cs` | `CS8603` |
| 2 | `MemberPath.cs` | `CS8602` |

## 4) Dotnet format command
Command executed:

```bash
dotnet format striv/StriV.Core.slnx \
  --diagnostics CS8618 CS8625 CS8600 CS8601 CS8603 \
  --severity warn \
  --include striv/projects/Stride.Core.Reflection/ \
  --verbosity diagnostic \
  2>&1 | tee /tmp/striv-m14g-reflection-format.log
```

- Included diagnostics: `CS8618`, `CS8625`, `CS8600`, `CS8601`, `CS8603`
- Intentionally excluded: `CS8602`, `CS8604`
- Log path: `/tmp/striv-m14g-reflection-format.log`

## 5) Diff review

| File | Change pattern | Accepted/reverted | Rationale |
|---|---|---|---|
| `AttributeRegistry.cs` | spacing normalization in arithmetic expression | Accepted | Mechanical formatting only; no behavior change. |
| `IMemberNamingConvention.cs` | BOM removed from file header comment line | Accepted | Encoding/header normalization only; API/behavior unchanged. |
| `TypeDescriptors/DictionaryDescriptor.cs` | whitespace around `#pragma` line | Accepted | Formatting-only; nullability behavior unchanged. |

No suspicious lifecycle/API churn was introduced; no reverts were necessary.

## 6) Manual fixes
No manual warning fixes were applied after format, because the scoped format pass produced only mechanical edits and remaining warnings are in deferred/manual-reasoning families (`CS8602`, `CS8604`) or intentional compatibility paths (`CS0618`) and existing `CS8618` lifecycle areas.

## 7) After warnings
Focused post-pass results captured to `/tmp/striv-m14g-reflection-after.log`.

- Total focused warning lines after: **62**
- Distribution after:
  - `CS8618`: 42
  - `CS8604`: 12
  - `CS8620`: 2
  - `CS8603`: 2
  - `CS8602`: 2
  - `CS0618`: 2
- Bucket table after: unchanged from before (see `/tmp/striv-m14g-reflection-warning-buckets-after.log`).
- Focused checker exit status: **4** (`./striv/build/striv-check-focused-project.sh Stride.Core.Reflection`)
- Delta from before: **0** focused warning-line reduction.

## 8) Remaining warnings
Remaining buckets are unchanged and concentrated in:
- `MemberDescriptors/MemberDescriptorBase.cs` (`CS8618`)
- `TypeDescriptors/Compatibility/OldCollectionDescriptor.cs` (`CS8618`)
- `TypeDescriptors/ObjectDescriptor.cs` (`CS8618`, `CS8604`)
- `MemberPath.cs` (`CS8602`, `CS8604`, `CS8620`, `CS8603`)
- `TypeDescriptors/CollectionDescriptor.cs` (`CS8618`)
- `TypeDescriptorFactory.cs` (`CS0618` intentional)

Why not fixed in M14g:
- `CS8602` / `CS8604` require lifecycle/ownership reasoning and test-first targeted handling.
- `CS0618` is intentional due to compatibility fallback path involving `OldCollectionDescriptor`.
- Remaining `CS8618` sites are descriptor lifecycle-sensitive and were not safe for blind mechanical edits in this pass.

## 9) Behavior compatibility
- Descriptor/member/attribute-registry behavior: unchanged.
- Serialization/AP path: preserved.
- Reflection tests: pass.

## 10) Validation results

| Command | Exit | First meaningful warning/error | Pass/Fail | Output truncated |
|---|---:|---|---|---|
| `dotnet build striv/projects/Stride.Core.Reflection/Stride.Core.Reflection.csproj -c Debug -p:StriVWarningFocusProject=Stride.Core.Reflection --no-incremental` | 0 | `CS8618` on `ObjectDescriptor`/`MemberDescriptorBase` | Pass | No |
| `dotnet format striv/StriV.Core.slnx --diagnostics CS8618 CS8625 CS8600 CS8601 CS8603 --severity warn --include striv/projects/Stride.Core.Reflection/ --verbosity diagnostic` | 0 | none (format completed) | Pass | No |
| `dotnet build striv/projects/Stride.Core.Reflection/Stride.Core.Reflection.csproj -c Debug -p:StriVWarningFocusProject=Stride.Core.Reflection --no-incremental` (after) | 0 | `CS8618` unchanged | Pass | No |
| `./striv/build/striv-check-focused-project.sh Stride.Core.Reflection` | 4 | focused warning gate failed at 62 warnings | Fail (expected gate behavior) | No |
| `dotnet test striv/tests/Stride.Core.Reflection.Tests/Stride.Core.Reflection.Tests.csproj -v minimal` | 0 | intentional `CS0618` in fallback tests | Pass | No |
| `dotnet build striv/StriV.Core.slnx -c Debug -p:StriVWarningFocusProject=Stride.Core.Reflection --no-incremental` | 0 | Reflection warnings as above | Pass | No |
| `./striv/build/striv-check-focused-projects.sh Stride.BepuPhysics Stride.Core.Mathematics Stride.Core.IO Stride.Input Stride.Games` | 0 | none | Pass | No |
| `dotnet test striv/tests/Stride.Games.Tests/Stride.Games.Tests.csproj -v minimal` | 0 | none | Pass | No |
| `dotnet test striv/tests/Stride.Input.Tests/Stride.Input.Tests.csproj -v minimal` | 0 | none | Pass | No |
| `dotnet test striv/tests/StriV.CleanGraph.Tests/StriV.CleanGraph.Tests.csproj -v minimal` | 0 | non-focused solution warnings during build steps | Pass | Yes (long output) |
| `dotnet test striv/tests/StriV.AssetTool.Tests/StriV.AssetTool.Tests.csproj -v minimal` | 0 | non-focused solution warnings during build steps | Pass | Yes (long output) |
| `dotnet test striv/tests/StriV.AssetPipeline.Tests/StriV.AssetPipeline.Tests.csproj --no-build -v minimal` | 0 | none | Pass | Yes (combined log) |
| `dotnet test striv/tests/StriV.ShaderPipeline.Tests/StriV.ShaderPipeline.Tests.csproj --no-build -v minimal` | 0 | none | Pass | Yes (combined log) |
| `./striv/build/striv-build-core.sh` | 0 | carries existing non-focused warnings | Pass | Yes (long output) |

## 11) Recommended next task
**M14h targeted test-first fix for remaining `CS8602` / `CS8604` in `MemberPath` and `ObjectDescriptor`**, then reassess descriptor-lifecycle `CS8618` sites with explicit initialization contracts. This isolates the highest-risk nullable warnings without broad descriptor-core churn.
