# 2430 — M23g UpdateEngine compile-frame / temporary-object contract slice

## 1) Files changed
- `striv/projects/Stride.Engine/Engine/UpdaterReflection/UpdateEngine.cs`
- `striv/projects/Stride.Engine/Engine/UpdaterReflection/CompiledUpdate.cs`

## 2) Task scope
Narrow UpdateEngine compile-frame / temporary-object contract cleanup only. No broad UpdateEngine rewrite, no source-generator replacement, no unsafe traversal redesign, no Dominatus dependency added.

## 3) Before warnings
- Focused warning count before: `290` (`/tmp/striv-m23g-engine-warning-lines-before.log`).
- UpdateEngine warning lines before (duplicated due to multi-target build):
  - `UpdateEngine.cs(199,77) CS8600`
  - `UpdateEngine.cs(215,91) CS8600`
  - `UpdateEngine.cs(288,81) CS8600`
  - `UpdateEngine.cs(339,34) CS8601`
  - `UpdateEngine.cs(363,41) CS8601`
  - `UpdateEngine.cs(436,54) CS8604`
- Top warning codes before: CS8618, CS8602, CS8625, CS8601, CS8603, CS8600, CS8604.

## 4) Compile-side classification table
| File/site | Warning | Current pattern | Compile-time meaning | Category | Action |
| --- | --- | --- | --- | --- | --- |
| UpdateEngine.Compile resolver lookups | CS8600 | `TryGetValue(..., out resolver)` into non-nullable local | Resolver may not resolve; nullability false-positive on `out` variable | member lookup miss | Use inline `out var resolver` nullable flow-safe pattern |
| UpdateEngine.ProcessMember temporary struct list | CS8604 | `Activator.CreateInstance(...)` into `List<object>` | Temporary storage object creation can be null at compile time by signature | compile-frame state / invalid compile state should throw | Add explicit helper `CreateTemporaryStructStorage` that throws deterministic `InvalidOperationException` if null |
| CompiledUpdate.TemporaryObjects container | CS8601/CS8604 pressure | raw `object[]` with nullable values possible by API signatures | Distinguish payload nullability vs unresolved compile slot | object list contains nullable values | Mark container as `object?[]`, require non-null where runtime strictly needs object |

## 5) Chosen implementation slice
Chosen slice: **A + C hybrid**.
- **A**: explicit nullable temporary payload container (`object?[]` / `List<object?>`).
- **C**: deterministic compile guard helper (`CreateTemporaryStructStorage`) and existing runtime `RequireObject` for strict non-null consumers.
Reason: smallest local change that clarifies temporary object contract without altering runtime update semantics.

## 6) Tests
- Re-ran `Stride.Engine.Tests` to pin no behavior regression in UpdaterReflection and surrounding engine test surface.
- Existing deterministic compile error test coverage in `UpdaterReflectionLifecycleTests` remained valid.

## 7) Fixes applied
### UpdateEngine.cs
- Old: `List<object>` temporary storage and direct `Activator.CreateInstance` add.
- New: `List<object?>` plus `CreateTemporaryStructStorage(Type)` guard helper.
- Why correct: compile-frame temporary storage for struct property traversal is required; unresolved null now throws deterministic compile-time exception.

- Old: resolver lookup locals with explicit non-nullable declarations causing CS8600.
- New: inline `out var resolver` lookup locals.
- Why correct: avoids non-nullable local mismatch while preserving lookup behavior.

- Old: direct `currentObj = temporaryObjects[...]` from possibly-null array slot.
- New: `currentObj = RequireObject(temporaryObjects[...], ...)`.
- Why correct: runtime requires concrete temporary object for struct unboxing path.

### CompiledUpdate.cs
- Old: `internal object[] TemporaryObjects;`
- New: `internal object?[] TemporaryObjects;`
- Why correct: container now explicitly models nullable compile payload while runtime-required callsites validate non-null deterministically.

## 8) Dominatus boundary
Command run:
`rg -n "Dominatus|Ai\.Act|Ai\.Await|IActuationHandler|StriV.Engine.Dominatus|Dominatus.Core|Dominatus.OptFlow" striv/projects/Stride.Engine -g '*.cs' -g '*.csproj' || true`

Result: no matches.

## 9) Warning results
- Focused warning count after: `282`.
- Total delta: `290 -> 282` (`-8`).
- UpdateEngine warning bucket:
  - `CS8600`: `6 -> 0`
  - `CS8604`: `2 -> 0`
  - `CS8601`: `4 -> 4` (remaining at lines 336/360, duplicated by target)

## 10) Deferred issues
- unsafe pointer traversal architecture redesign (deferred)
- source-generator replacement (deferred)
- broader compile model split between temporary payload and unresolved traversal (deferred)
- full runtime traversal redesign (deferred)

## 11) Validation results
Commands executed during this slice included:
- `dotnet build striv/projects/Stride.Engine/Stride.Engine.csproj -c Debug -p:StriVWarningFocusProject=Stride.Engine --no-incremental` (pass)
- warning extraction/bucketing pipelines for before/after (pass)
- `dotnet test striv/tests/Stride.Engine.Tests/Stride.Engine.Tests.csproj -v minimal` (pass)
- Dominatus boundary grep (pass)
- `dotnet build striv/StriV.Core.slnx -c Debug -v minimal` and chained validation/test script sequence (output very large; terminal output truncated; no terminating command error observed in captured output)

For long chained validation output: truncated = yes.

## 12) Next recommendation
Proceed with a **next UpdateEngine slice** focused narrowly on the remaining `CS8601` sites in compile-frame state (`UpdateEngine.cs` lines near 336 and 360), likely requiring a tiny compile-state helper or tighter nullable annotations on stack-entry member state.
