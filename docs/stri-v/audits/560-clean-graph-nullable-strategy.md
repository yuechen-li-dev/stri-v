# 560 — Clean Graph Nullable Strategy (Pre-M4 / M3.5)

## 1) Files changed

- `docs/stri-v/audits/560-clean-graph-nullable-strategy.md` (this report only; audit-only run, no code cleanup implemented).

## 2) Nullable baseline

### Command used

```bash
./striv/build/striv-build-core.sh 2>&1 | tee /tmp/striv-nullable-baseline.log
```

### Build result

- Exit code: `0` (build succeeded).
- Compiler summary in build log: `2771 Warning(s), 0 Error(s)`.

### Nullable extraction

```bash
grep -E "warning CS(86|87)[0-9]+" /tmp/striv-nullable-baseline.log > /tmp/striv-nullable-warnings.log || true
```

- Extracted nullable warning lines: `5542`.
- Note: this count is warning *instances in log output* and can include multi-target/repeated emissions for the same source warning location.

### Top nullable warning codes

From:

```bash
sed -E 's/.*warning (CS[0-9]+).*/\1/' /tmp/striv-nullable-warnings.log | sort | uniq -c | sort -nr
```

| Code | Count |
|---|---:|
| CS8618 | 2066 |
| CS8625 | 940 |
| CS8604 | 500 |
| CS8600 | 494 |
| CS8603 | 384 |
| CS8601 | 292 |
| CS8765 | 250 |
| CS8602 | 224 |
| CS8622 | 146 |
| CS8767 | 116 |
| CS8769 | 80 |

### Top projects by nullable warning volume (approx)

From:

```bash
grep -Eo "\[[^]]+\.csproj\]" /tmp/striv-nullable-warnings.log | sort | uniq -c | sort -nr
```

| Project | Count |
|---|---:|
| Stride.Rendering | 1722 |
| Stride.Engine | 954 |
| Stride.Graphics | 824 |
| Stride.FreeImage | 562 |
| Stride.Games | 284 |
| Stride.Core.AssemblyProcessor | 224 |
| Stride.Input | 204 |
| Stride | 182 |
| Stride.Core | 170 |
| Stride.Shaders | 160 |
| Stride.Core.Serialization | 126 |
| Stride.Core.Reflection | 60 |
| Stride.Core.IO | 42 |
| Stride.Core.MicroThreading | 16 |
| Stride.BepuPhysics | 8 |

### Output truncation status

- Interactive terminal output was truncated due to volume, but baseline data was captured in `/tmp/striv-nullable-baseline.log` and all summaries above were generated from that full log file.

## 3) Nullable warning taxonomy

| Warning code | Count | Meaning | Main projects | Risk | Default action |
| ------------ | ----: | ------- | ------------- | ---- | -------------- |
| CS8618 | 2066 | Non-null member not initialized by constructor | Rendering/Engine/Graphics heavy | Medium-High | Bucket by lifecycle vs truly missing init; avoid blind `!` |
| CS8625 | 940 | Assigning `null` to non-null reference | Rendering/Engine/Graphics | Medium | Decide contract truth; nullable annotations where honest |
| CS8604 | 500 | Possible null arg passed to non-null param | Rendering/Engine/Core AP | High | Add guards or adjust nullable contracts only with evidence |
| CS8600 | 494 | Null/possible null to non-null conversion | Rendering/Graphics/Engine | Medium | Use nullable local types or guards; avoid silent coercion |
| CS8601 | 292 | Possible null assignment | Rendering/Engine/Core | Medium | Make destination nullable when valid or guard assignment |
| CS8602 | 224 | Possible null dereference | Engine/Rendering/Core AP | High | Treat as potential bug; verify path, add guard/tests |
| CS8603 | 384 | Possible null return | Rendering/Graphics/Core AP | Medium-High | Return nullable where truthful (non-public first) or guard |
| CS8622 | 146 | Delegate nullability mismatch | Engine/Rendering/Core AP | Medium | Contract alignment pass; low-risk private handlers first |
| CS8765 | 250 | Override parameter nullability mismatch | FreeImage-heavy + others | Medium | Mostly contract alignment, often mechanical in wrappers |
| CS8767 | 116 | Interface implementation nullability mismatch | FreeImage-heavy | Medium | Contract alignment; likely safe in interop structs but audit |
| CS8769 | 80 | Explicit interface member nullability mismatch | FreeImage-heavy | Medium | Mechanical alignment with BCL interface signatures |

## 4) Cleanup buckets

### Bucket A — Safe mechanical candidates

Likely safe now:
- Private/internal helper methods with `CS8603` where nullable return is already effectively possible and does not alter public API.
- Local variable type relaxations (`T` -> `T?`) for `CS8600/CS8601` in contained methods.
- Test code/internal tooling (if present in target pass).
- FreeImage explicit interface/signature alignment (`CS8765/CS8767/CS8769`) where behavior remains identical.

### Bucket B — Lifecycle initialization

Common in `Stride.Engine`, `Stride.Games`, `Stride.Rendering`, `Stride.Graphics`:
- Fields/services initialized during `Initialize`, `LoadContent`, game system wiring, runtime device setup.
- Intentionally-null-before-start members.

Action:
- Defer broad fixes.
- For isolated fixes, document lifecycle invariant in code comments when necessary.
- Avoid `null!` except when invariant is strong and proven.

### Bucket C — Serialization/AP/reflection

Present in `Stride.Core`, `Stride.Core.Serialization`, `Stride.Core.AssemblyProcessor`.

Action:
- Defer broad cleanup until serialization/nullability policy is explicit.
- Do not alter `[DataMember]`/serialized shape or AP reflection assumptions casually.

### Bucket D — Public API / interface contract

Notable in `Stride.FreeImage` and other projects via `CS8765/CS8767/CS8769`, plus delegate contract warnings.

Action:
- Defer broad public contract shifts.
- Allow narrow, compatibility-preserving signature alignment only when directly matching upstream interface/override expectations.

### Bucket E — Possible real bugs

Key warnings:
- `CS8602` dereference paths.
- Non-obvious `CS8604` arg flows in runtime systems.

Action:
- Investigate with targeted tests before change.
- Prefer guards/assertions over optimistic annotations.

## 5) Project triage

| Project | Nullable warning volume | Risk | Recommended nullable strategy |
| ------- | ----------------------: | ---- | ----------------------------- |
| Stride.FreeImage | 562 | Medium | First pilot candidate: mostly contract mismatch/mechanical wrapper signatures; keep behavior unchanged |
| Stride.Core | 170 | Medium-High | Defer broad pass; split serialization-heavy files from private helpers |
| Stride.Core.IO | 42 | Medium | Small bounded follow-up candidate after FreeImage; verify file-system lifecycle assumptions |
| Stride.Games | 284 | High | Lifecycle-heavy; defer except narrow private mechanical fixes |
| Stride.Graphics | 824 | High | Large runtime/lifecycle surface; defer broad cleanup |
| Stride.Rendering | 1722 | Very High | Defer broad work; only micro-target private helper subset in dedicated pass |
| Stride.Engine | 954 | Very High | Defer broad pass; high lifecycle + runtime risk |
| Stride.BepuPhysics | 8 | Medium | Tiny warning count; can be opportunistic later but not primary reduction target |

## 6) First pilot recommendation

**Recommend first pilot: `Stride.FreeImage` narrow contract-alignment subset.**

Why:
- High warning count with bounded, wrapper-centric patterns.
- Many warnings are repetitive `CS8765/CS8767/CS8769` mismatch signatures (override/interface).
- Lower lifecycle/serialization sensitivity than Engine/Rendering.
- Good warning reduction per risk if constrained to signature truthfulness.

Pilot shape:
- Focus only on clearly mechanical signature nullability alignment in structs/classes implementing BCL interfaces or overriding `object` methods.
- Exclude broad behavioral logic changes.

## 7) Nullable cleanup rules for Codex

1. Never use `!` as blanket silencing.
2. Use `!` only for proven lifecycle invariants, sparingly; add brief rationale comment when non-obvious.
3. Prefer truthful nullable annotations over suppression.
4. Do not alter `[DataMember]` or serialized member shape without explicit approval.
5. Do not broadly change public API contracts outside dedicated contract-alignment tasks.
6. Prefer local guards where null is runtime-plausible.
7. Treat `CS8602`/`CS8604` as potential defects; add focused tests where practical.
8. No global suppression of `CS86xx`/`CS87xx`.
9. Keep nullable cleanup PRs small and project-scoped.
10. Stop and defer when a fix requires architectural interpretation.

## 8) Suppression policy

- No global nullable suppression.
- Project-level suppression only for explicitly quarantined legacy zones with TODO + rationale.
- `#pragma` allowed only for tiny unavoidable interop/generated regions.
- Every suppression must explain why warning is currently non-actionable.

## 9) Optional pilot implementation

- **Audit-only in this task.**
- No source cleanup changes applied.
- Rationale: warning landscape and risk taxonomy were still being established; safer to begin with a narrowly-scoped dedicated pilot next.

## 10) Recommended next prompt

Use this prompt for the first pilot:

> Perform a narrow nullable cleanup pilot in `sources/tools/Stride.FreeImage` targeting only `CS8765`, `CS8767`, and `CS8769` signature mismatches for overrides and explicit/implicit interface implementations. Allowed changes: nullability annotations on method parameters/return types, and minimal local guard adjustments only when required for compile correctness. Forbidden changes: behavior refactors, serialization shape changes, public API redesign beyond signature alignment, blanket `!`, global suppressions, project file warning suppression, file moves/renames. Validate with:
> 1) `./striv/build/striv-build-core.sh 2>&1 | tee /tmp/striv-nullable-pilot.log`
> 2) `grep -E "warning CS(8765|8767|8769)" /tmp/striv-nullable-pilot.log | grep "Stride.FreeImage" | wc -l`
> 3) `dotnet test striv/tests/StriV.CleanGraph.Tests/StriV.CleanGraph.Tests.csproj --no-build -v minimal`
> Stop criteria: any required change touches lifecycle-sensitive systems, serialization contract semantics, or non-trivial behavior logic.
