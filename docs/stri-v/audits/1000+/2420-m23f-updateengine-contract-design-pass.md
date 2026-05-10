# 2420 — M23f UpdateEngine contract design pass

## 1) Files changed
- `striv/projects/Stride.Engine/Engine/UpdaterReflection/UpdateEngine.cs`

## 2) Task scope
Implemented a narrow UpdateEngine contract slice inspired by Dominatus runtime-shape patterns (explicit runtime-role guards and deterministic invalid-state exceptions), without importing Dominatus types and without broad UpdaterReflection redesign.

## 3) Dominatus exemplar review
Reviewed:
- `striv/external/Dominatus/ARCHITECTURE.md`
- `striv/external/Dominatus/AUTHORING_GUIDE.md`
- `striv/external/Dominatus/src/Dominatus.Core/*`
- `striv/external/Dominatus/src/Dominatus.OptFlow/*`
- `striv/projects/StriV.Engine.Dominatus*`
- `striv/tests/StriV.Engine.Dominatus.Tests/*`

Applied ideas:
- explicit runtime frame role checks (`current`, `entered`, `restored`) at operation boundaries;
- deterministic invalid-state failures (`InvalidOperationException`) instead of accidental null dereference;
- minimal local contract helper (`RequireObject`) rather than architectural rewrite;
- lifecycle-style safety: pooled stack release is now guaranteed (`try/finally`).

## 4) Before warnings
- focused warning count before: `302`
- UpdateEngine warning bucket before:
  - `Engine/UpdaterReflection/UpdateEngine.cs CS8600` = 12
  - `Engine/UpdaterReflection/UpdateEngine.cs CS8604` = 8
  - `Engine/UpdaterReflection/UpdateEngine.cs CS8601` = 4

## 5) Warning classification table
| File/site | Warning | Current pattern | Runtime meaning | Category | Action |
| --- | --- | --- | --- | --- | --- |
| UpdateEngine Run enter/leave pointer traversal | CS8600/CS8604 | nullable traversal locals passed to pointer conversion | traversal frame object expected non-null by operation contract | traversal frame state | Added role guard helper + deterministic exception |
| UpdateEngine compile-time temporary object list | CS8601/CS8604 | null object flows through temporary object list | value payload may be null depending on path/member semantics | valid null payload | Deferred (needs broader compile-model clarity) |
| UpdateEngine compile member resolution | CS8600 | resolver/member discovery with nullable results | valid lookup miss during search loops | member lookup invariant | Kept existing deterministic parse failure |

## 6) Chosen implementation slice
Slice B (operation guard helpers) + runtime resource safety:
- Introduced `RequireObject(object?, UpdateOperation?, string role)`.
- Guarded pointer/object-boundary operations in `Run`.
- Wrapped pooled stack acquire/release in `try/finally`.

Why: lowest-churn way to make runtime traversal invariants explicit and deterministic in this pass.

## 7) Tests
No new tests added in this slice; existing `Stride.Engine.Tests` suite was run to validate compatibility.

## 8) Implementation details
- `Run` now treats runtime traversal object roles as explicit contract preconditions before pointer conversion.
- Null traversal state now throws `InvalidOperationException` with operation/role context.
- Stack pool release is guaranteed even if an operation fails.

## 9) Dominatus dependency boundary
`rg` check on `striv/projects/Stride.Engine` found no `Dominatus*` references.

## 10) Warning results
- focused warning count after: `290`
- UpdateEngine warning bucket after:
  - `Engine/UpdaterReflection/UpdateEngine.cs CS8600` = 6
  - `Engine/UpdaterReflection/UpdateEngine.cs CS8604` = 2
  - `Engine/UpdaterReflection/UpdateEngine.cs CS8601` = 4
- total delta: `-12`

## 11) Deferred UpdateEngine issues
- unsafe pointer traversal remains architecture-sensitive;
- no source-generator replacement;
- compile-side temporary-object/nullability modeling still partial;
- no broader UpdaterReflection redesign.

## 12) Validation results
Executed:
- `dotnet build striv/projects/Stride.Engine/Stride.Engine.csproj -c Debug -p:StriVWarningFocusProject=Stride.Engine --no-incremental` (exit 0, warnings present, not truncated in log file)
- `dotnet test striv/tests/Stride.Engine.Tests/Stride.Engine.Tests.csproj -v minimal` (exit 0, pass)
- `rg -n "Dominatus|Ai\.Act|Ai\.Await|IActuationHandler|StriV.Engine.Dominatus|Dominatus.Core|Dominatus.OptFlow" striv/projects/Stride.Engine -g '*.cs' -g '*.csproj' || true` (exit 0, no matches)

## 13) Next recommendation
Next UpdateEngine slice should target compile-time temporary-object/nullability contracts (the remaining `CS8601`/`CS8604` in compile path), potentially via a tiny typed compile-frame model for temporary object payload semantics.
