# 590 - Stride.Rendering warning hygiene audit (Pre-M4 / M3.5)

## 1) Files changed

- `sources/engine/Stride.Rendering/Streaming/StreamingManager.cs`
- `sources/engine/Stride.Rendering/Rendering/EffectSystem.cs`
- `sources/engine/Stride.Rendering/Rendering/MeshRenderFeature.cs`
- `sources/engine/Stride.Rendering/Rendering/Lights/ForwardLightingRenderFeature.cs`
- `sources/engine/Stride.Rendering/Shaders.Compiler/EffectCompileRequest.cs`
- `docs/stri-v/audits/590-rendering-warning-hygiene-audit.md`

## 2) Baseline

Command set used:

```bash
./striv/build/striv-build-core.sh 2>&1 | tee /tmp/striv-rendering-warnings-before.log
grep -E "warning CS(86|87)[0-9]+" /tmp/striv-rendering-warnings-before.log | grep "Stride.Rendering" | tee /tmp/striv-rendering-nullable-before.log
wc -l /tmp/striv-rendering-nullable-before.log
sed -E 's/.*warning (CS[0-9]+).*/\1/' /tmp/striv-rendering-nullable-before.log | sort | uniq -c | sort -nr
head -n 200 /tmp/striv-rendering-nullable-before.log
```

Baseline results:

- Total `Stride.Rendering` nullable warning lines: **1760**.
- Top warning codes:
  - `CS8618`: 868
  - `CS8625`: 298
  - `CS8600`: 166
  - `CS8765`: 106
  - `CS8604`: 86
  - `CS8602`: 66
  - `CS8603`: 64
  - `CS8601`: 48
  - `CS8767`: 32
  - `CS8622`: 14
- Representative high-frequency files/groups:
  - `Rendering/Shadows/*` renderer lifecycle classes (many repeated `CS8618`).
  - `Rendering/Images/*` effect lifecycle classes (constructor-init `CS8618`).
  - `Rendering/Materials/*` equality/nullability signature mismatch (`CS8765`/`CS8767`).
  - event handler signature mismatches (`CS8622`) in `StreamingManager`, `EffectSystem`, `MeshRenderFeature`, `ForwardLightingRenderFeature`.
- Output truncation:
  - Full build output is very large and was truncated in terminal display, but complete logs were captured to `/tmp/striv-rendering-warnings-before.log` and `/tmp/striv-rendering-nullable-before.log`.

## 3) Warning classification

| Warning group | Count | Representative files | Bucket | Risk | Action |
| ------------- | ----: | -------------------- | ------ | ---- | ------ |
| `CS8618` constructor/lifecycle fields | 868 | `Rendering/Shadows/*`, `Rendering/Images/*`, `Rendering/RenderSystem.cs` | C | Medium-High | Defer (engine/render lifecycle init; high behavior coupling) |
| `CS8625` null literal to non-nullable | 298 | `Extensions/IndexExtensions.cs`, `Rendering/Properties.cs`, many material/feature option classes | B/C | Medium | Mixed: some are contract changes; defer bulk fix |
| `CS8600` nullable conversion | 166 | `Rendering/*` and effect/material paths | B/C | Medium | Needs per-site validation, often semantics-sensitive |
| `CS8765` override nullability mismatch | 106 | many `Equals(object obj)` overrides across material/light types | A | Low | Candidate for mechanical `object?` signature updates |
| `CS8767` interface nullability mismatch | 32 | `IEquatable<T>.Equals(T other)` / comparer signatures | A/B | Low-Medium | Some mechanical; apply tiny bounded subset only |
| `CS8622` delegate signature nullability mismatch | 14 | event handlers in rendering/streaming systems | A | Low | Mechanical `object? sender` fixes |
| `CS8602` possible dereference | 66 | distributed in render logic | B | Medium-High | test-first or defer |
| `CS8603` possible null return | 64 | sprites/compute/render helpers | B | Medium | test-first contract verification |
| `CS8604` possible null argument | 86 | shader/effect/render setup paths | B | Medium-High | test-first or defer |

## 4) Low-risk candidates

Selected low-risk candidates (implemented):

1. Event handler sender nullability (`CS8622`):
   - `StreamingManager.OnEnabledChanged(object? sender, EventArgs e)`
   - `EffectSystem.FileModifiedEvent(object? sender, FileEvent e)`
   - `MeshRenderFeature.RenderFeatures_CollectionChanged(object? sender, TrackingCollectionChangedEventArgs e)`
   - `ForwardLightingRenderFeature.LightRenderers_CollectionChanged(object? sender, TrackingCollectionChangedEventArgs e)`
   - Why low risk: aligns method signature with `EventHandler<T>` sender nullability, no control-flow or logic change.
   - Expected reduction: 6 warnings in this subset.

2. `EffectCompileRequest` equality signature (`CS8765`/`CS8767`):
   - `Equals(EffectCompileRequest? other)` and `Equals(object? obj)`
   - Why low risk: preserves same null checks/behavior, only annotation alignment with framework and `IEquatable<T?>` contract.
   - Expected reduction: 2 warnings.

Estimated total reduction for implemented subset: **8 warnings**.

## 5) Test-first candidates

Potential high-value-but-test-first candidates:

1. `CS8602` / `CS8604` in render effect/material plumbing:
   - Expected behavior to test: null paths should fail predictably (or be guarded) without altering render-stage output ordering.
   - Proposed test location: `striv/tests/StriV.CleanGraph.Tests/Rendering/` (new focused unit tests around small helper components, not integration render tests).
   - Proposed code change after test: targeted guards or contract annotations in specific helper methods.

2. `CS8603` return-contract warnings in sprite/effect helper classes:
   - Expected behavior to test: call sites either already null-check or expect non-null; verify actual intent.
   - Proposed test location: same `Rendering/` test folder with minimal isolated object construction.
   - Proposed code change after test: return type to nullable (`T?`) where semantically truthful, otherwise enforce non-null path.

## 6) Deferred warnings

Deferred warning groups and reasons:

- Majority of `CS8618` in render lifecycle classes: likely initialized via pipeline lifecycle (`InitializeCore`, load/unload, runtime wiring), not constructor; risky to “fix” mechanically.
- Broad `CS8625` in material/property APIs: often tied to public/serialized contract shapes; changing nullability may affect serialization/API surface.
- `CS8600/CS8602/CS8603/CS8604` in render pipeline flow: these can alter behavior if changed without tight local tests.
- Shader/effect/render graph lifecycles: deferred per doctrine to avoid semantic changes during warning hygiene phase.

## 7) Optional implementation

A very small bounded subset was implemented (5 files, 8-warning expected reduction):

- event handler sender annotations (`object?`) for delegate contract parity.
- `EffectCompileRequest` `Equals` signature nullability alignment.

No broad cleanup attempted.

## 8) Validation results

### Command 1
- Command: `./striv/build/striv-build-core.sh 2>&1 | tee /tmp/striv-rendering-warnings-after.log`
- Exit code: `0`
- First meaningful warning/error: warning stream begins in downstream projects; first shown in captured tail was `CS1030` in `Stride.BepuPhysics`.
- Pass/Fail: Pass
- Output truncated: Yes in terminal view; full log captured to file.

### Command 2
- Command: `grep -E "warning CS(86|87)[0-9]+" /tmp/striv-rendering-warnings-after.log | grep "Stride.Rendering" | tee /tmp/striv-rendering-nullable-after.log`
- Exit code: `0`
- First meaningful warning/error: `IndexExtensions.cs` `CS8625`.
- Pass/Fail: Pass
- Output truncated: Yes in terminal view; full filtered log captured to file.

### Command 3
- Command: `wc -l /tmp/striv-rendering-nullable-after.log`
- Exit code: `0`
- First meaningful warning/error: N/A
- Pass/Fail: Pass
- Output truncated: No
- Result: `1704`

### Command 4
- Command: `dotnet test striv/tests/StriV.CleanGraph.Tests/StriV.CleanGraph.Tests.csproj --no-build -v minimal`
- Exit code: `0`
- First meaningful warning/error: N/A (tests passed)
- Pass/Fail: Pass
- Output truncated: No

Net measured change in `Stride.Rendering` nullable warning lines: **1760 -> 1704 (-56)**.

## 9) Worktree status

Command:

```bash
git status --short
```

Status at report time:

- `M sources/engine/Stride.Rendering/Rendering/EffectSystem.cs`
- `M sources/engine/Stride.Rendering/Rendering/Lights/ForwardLightingRenderFeature.cs`
- `M sources/engine/Stride.Rendering/Rendering/MeshRenderFeature.cs`
- `M sources/engine/Stride.Rendering/Shaders.Compiler/EffectCompileRequest.cs`
- `M sources/engine/Stride.Rendering/Streaming/StreamingManager.cs`
- `M docs/stri-v/audits/590-rendering-warning-hygiene-audit.md`

## 10) Recommended next task

**Recommended next task: first `Stride.Rendering` low-risk implementation pilot (phase 2).**

Narrow next prompt:

> Continue `Stride.Rendering` warning hygiene with an ultra-bounded mechanical pass focused only on `CS8765`/`CS8767` equality/comparer signature mismatches in files under `sources/engine/Stride.Rendering/Rendering/Materials/**` and `sources/engine/Stride.Rendering/Rendering/Lights/**`. Do not touch logic, hashing, field initialization, or serialized/public data contracts beyond nullability annotations in override/interface signatures. Rebuild, recount `Stride.Rendering` nullable warnings, and report exact warning delta.

