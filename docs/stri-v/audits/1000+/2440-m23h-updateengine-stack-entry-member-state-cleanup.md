# 2440 — M23h UpdateEngine compile-frame stack-entry / member-state cleanup

## 1) Files changed
- `striv/projects/Stride.Engine/Engine/UpdaterReflection/UpdateEngine.cs`
- `docs/stri-v/audits/1000+/2440-m23h-updateengine-stack-entry-member-state-cleanup.md`

## 2) Task scope
Narrow pass on remaining `CS8601` in `UpdateEngine.cs` compile-frame stack/member state handling. No broad rewrite, no warning suppression, no unsafe traversal redesign, no source generation work, and no Dominatus dependency additions.

## 3) Before warnings
- Focused warning count before: **282** (`/tmp/striv-m23h-engine-warning-lines-before.log`).
- UpdateEngine `CS8601` before:
  - `UpdateEngine.cs(336,34)` member assignment in leave operation
  - `UpdateEngine.cs(360,41)` last-child member assignment from stack entry

## 4) Classification table
| File/site | Warning | Current assignment | Compile-frame meaning | Null valid? | Action |
| --- | --- | --- | --- | ---: | --- |
| `UpdateEngine.cs:336` | CS8601 | `UpdateOperation.Member = stackPathPart.Member` | Creating a concrete leave operation that must target a real member accessor | No | Added deterministic `RequireMember(...)` helper and used it at assignment site; throw `InvalidOperationException` if unresolved |
| `UpdateEngine.cs:360` | CS8601 | `state.LastChildMember = stackPathPart.Member` | Transitional compile-frame state while popping stack; source stack entry member can be absent for non-member path nodes | Yes | Made `LastChildMember` nullable (`UpdatableMember?`) to represent valid staging state |

## 5) Chosen implementation slice
Used **Option A + B hybrid**:
- Nullable compile-frame member staging (`LastChildMember`) where null is valid.
- Deterministic require helper for operation construction where member is required (`RequireMember`).

This keeps the scope local and preserves runtime semantics while removing nullable flow ambiguity.

## 6) Tests
No new tests were added. Existing `Stride.Engine.Tests` suite passes after the change, and behavior change is limited to deterministic exception text in an invalid compile-frame path that previously only surfaced as nullable-flow warning.

## 7) Fixes applied
### `UpdateEngine.cs`
- Old pattern: non-nullable compile-frame `LastChildMember` accepted nullable assignments from stack entries.
- New pattern: `LastChildMember` is explicitly nullable.
- Old pattern: leave operation directly assigned `stackPathPart.Member` to non-nullable `UpdateOperation.Member`.
- New pattern: leave operation uses `RequireMember(stackPathPart.Member, stackPathPart.LeaveOperation)`.
- Correctness: leaves transitional compile-frame null state explicit, and enforces non-null member only when creating executable update operations.

## 8) Dominatus boundary
Command:
- `rg -n "Dominatus|Ai\.Act|Ai\.Await|IActuationHandler|StriV.Engine.Dominatus|Dominatus.Core|Dominatus.OptFlow" striv/projects/Stride.Engine -g '*.cs' -g '*.csproj' || true`

Result:
- no matches.

## 9) Warning results
- Focused warning count after: **278** (`/tmp/striv-m23h-engine-warning-lines-after.log`).
- UpdateEngine warning delta:
  - `UpdateEngine.cs CS8601`: `4 -> 0` (2 unique locations duplicated by target framework/output grouping).
- Total focused warning delta: **-4** (`282 -> 278`).

## 10) Deferred issues
- unsafe pointer traversal architecture
- source-generator replacement
- broader compile model split
- remaining runtime traversal design

## 11) Validation results
See `/tmp/striv-m23h-validation.log` for full stream.
All requested validation commands completed with exit code 0.

Notable first warning/error samples encountered during validation runs:
- `dotnet build striv/StriV.Core.slnx -c Debug -v minimal`: first warning `CS1030` in `ObjectIdBuilder.cs`.
- `dotnet test striv/tests/Stride.Core.Reflection.Tests/...`: first warning `CS0618` obsolete `OldCollectionDescriptor`.
- Other test/build commands: completed successfully; warnings present in legacy areas, no new errors.

## 12) Next recommendation
Proceed with **next UpdateEngine slice**: target remaining UpdaterReflection-adjacent compile/runtime contracts outside `UpdateEngine.cs` (if any), then shift to `EntityManager` policy slice once UpdateEngine warning bucket is exhausted.
