# 550 Clean Graph Warning Audit (Pre-M4 / M3.5)

## 1) Files changed
- `docs/stri-v/audits/550-clean-graph-warning-audit.md` (this report)
- `striv/projects/Stride.Engine/Stride.Engine.csproj`
- `striv/projects/Stride.Graphics/Stride.Graphics.csproj`

## 2) Build warning baseline
- **Command used:** `./striv/build/striv-build-core.sh 2>&1 | tee /tmp/striv-clean-build-warnings.log`
- **Exit code:** `0`
- **Approximate warning count:**
  - Build summary reported `2775 Warning(s)` in the main solution build stage.
  - Log parsing across all warning lines in the full script output reported ~`5784` warning instances (includes repeated warnings across script stages / repeated project invocations).
- **Top warning codes by count** (parsed from log lines):
  - `CS8618` 2066
  - `CS8625` 940
  - `CS8604` 500
  - `CS8600` 494
  - `CS8603` 384
  - `CS8601` 292
  - `CS8765` 250
  - `CS8602` 224
  - `CS8622` 146
  - `CS8767` 116
- **Top projects by warning count** (parsed from log lines):
  - `Stride.Rendering` 1780
  - `Stride.Engine` 989
  - `Stride.Graphics` 891
  - `Stride.FreeImage` 562
  - `Stride.Games` 292
- **Output truncated:**
  - Console output was truncated by the terminal capture in this environment, but `/tmp/striv-clean-build-warnings.log` was used for full parsing.

## 3) Warning taxonomy

| Warning code | Count | Main projects | Category | Risk | Recommended action |
| --- | ---: | --- | --- | --- | --- |
| CS8618 | 2066 | Stride.Rendering, Stride.Engine, Stride.Graphics | nullable initialization | Medium-risk | Defer broad fixes; handle per-subsystem with explicit nullable strategy. |
| CS8625 | 940 | Stride.Rendering, Stride.Engine, Stride.Graphics | nullable annotation mismatch / null literal flow | Medium-risk | Defer; likely signature and contract updates across runtime APIs. |
| CS8604 | 500 | Stride.Rendering, Stride.Engine, Stride.Core | possible null argument | Medium-risk | Defer except for obvious local guard/annotation fixes in isolated utility code. |
| CS8600 | 494 | Stride.Rendering, Stride.Graphics, Stride.Engine | nullable conversion | Medium-risk | Defer; often coupled with serializer/runtime behavior assumptions. |
| CS8603 | 384 | Stride.Rendering, Stride.Engine, Stride.Core | nullable return mismatch | Medium-risk | Defer; requires API contract decisions. |
| CS8601 | 292 | Stride.Rendering, Stride.Graphics, Stride.Core | nullable assignment | Medium-risk | Defer except for local/internal-only helper paths. |
| CS8765 | 250 | Stride.Graphics, Stride.Rendering, Stride.Core.AssemblyProcessor | API compatibility/interface mismatch (nullability override) | Medium-risk | Batch later as focused signature-alignment pass. |
| CS8602 | 224 | Stride.Rendering, Stride.Engine, Stride.Core.AssemblyProcessor | possible null dereference (possible real bug) | High-risk / defer | Defer to subsystem correctness passes; inspect hot/runtime-critical sites first. |
| CS8622 | 146 | Stride.Engine, Stride.Rendering, Stride.Core.AssemblyProcessor | delegate signature nullability mismatch | Medium-risk | Defer to nullable strategy pass by project. |
| CS8767 | 116 | Stride.Rendering, Stride.Graphics, Stride.BepuPhysics | interface nullability mismatch | Medium-risk | Defer; may affect public API surface/behavior. |
| CS8769 | 80 | Stride.Core, Stride.Rendering | interface member nullability mismatch | Medium-risk | Defer; contract-sensitive. |
| CS0618 / STRIDE2000 | 68 / 14 | Stride.Engine, Stride.Graphics, Stride.Core | obsolete API usage (some intentional legacy) | Medium-risk | Track separately; do not force replacements in this sweep. |
| CS1030 | 34 | Stride.Core, Stride.BepuPhysics | `#warning` developer notes / legacy hotspots | Low-risk noise suppression candidate (selective) | Consider targeted suppression only where warnings are known intentional TODO notes. |
| CA2022 | 16 | (mixed) | analyzer warning (runtime/analyzer policy) | Medium-risk | Defer pending analyzer policy review; avoid blanket suppressions. |
| NU1510 | 10 (baseline) | Stride.Engine, Stride.Graphics | package pruning warning | **Low-risk sweep** | Add targeted package-level suppression (`NoWarn`) with rationale. |

## 4) Low-risk candidates

1. **NU1510 package pruning warnings in clean graph projects**
   - **Representative files:**
     - `striv/projects/Stride.Engine/Stride.Engine.csproj`
     - `striv/projects/Stride.Graphics/Stride.Graphics.csproj`
   - **Why low risk:**
     - Warning is advisory-only about pruning behavior, not runtime safety.
     - Current dependency intent is unclear; removing package refs now could be riskier than suppression.
   - **Proposed fix:**
     - Add package-level `<NoWarn>NU1510</NoWarn>` for the two specific `PackageReference` items.
   - **Expected impact:**
     - Removes repeated high-noise restore/build warnings without affecting runtime behavior.

2. **Potential follow-up low-risk candidate (not implemented here): selected CS1030**
   - **Representative files:** `ObjectIdBuilder.cs`, BEPU TODO-heavy files.
   - **Why low risk (conditional):**
     - Some `#warning` entries are known developer TODO markers and not actionable in this phase.
   - **Proposed fix:**
     - If desired, replace specific known TODO `#warning` with tracked TODO comments + issue IDs.
   - **Expected impact:**
     - Moderate noise reduction with minimal behavioral risk.

## 5) Warnings to defer

- **Large nullable wave (CS8618/CS8625/CS860x/CS876x) in runtime-heavy projects**
  - **Representative files:** `Stride.Rendering`, `Stride.Engine`, `Stride.Graphics`, `Stride.Core` source trees.
  - **Why deferred:** touches contracts and lifecycle assumptions; high chance of accidental behavior/API shifts.
  - **Future track:** Nullable strategy proposal + project-by-project migration plan.

- **Possible real bug warnings (CS8602) in runtime and rendering paths**
  - **Representative files:** multiple in `Stride.Engine`, `Stride.Rendering`.
  - **Why deferred:** needs semantic review, tests, and subsystem ownership.
  - **Future track:** correctness/stability bug-hunt pass.

- **Obsolete API warnings (CS0618, STRIDE2000)**
  - **Why deferred:** replacement may be non-trivial and behavior-affecting.
  - **Future track:** API modernization / post-shader-pipeline stabilization.

## 6) Suppression policy

Recommended policy for current phase:
- Allow **targeted, local suppressions** for known noisy advisories that do not indicate correctness issues (e.g., NU1510).
- Prefer **package-reference-level** or **project-level narrow scope** suppressions over global suppression.
- **Do not globally suppress nullable warnings** (`CS86xx`, `CS87xx`).
- **Do not suppress potential correctness warnings** (e.g., CS8602) without explicit TODO/owner tracking.

## 7) Optional implementation (performed)

Implemented low-risk sweep:
- Added package-reference-local suppression for `NU1510`:
  - `striv/projects/Stride.Engine/Stride.Engine.csproj` (`System.Threading.Tasks.Dataflow` reference)
  - `striv/projects/Stride.Graphics/Stride.Graphics.csproj` (`System.Memory` reference)

Categories fixed now:
- `NU1510` (package pruning advisory warning)

Before/after (parsed warning-line counts):
- `NU1510`: **10 -> 0**
- Total warning-line instances in captured logs: **5784 -> 1870** (not directly comparable due incremental build differences, but NU1510 elimination is direct and confirmed).

Rationale:
- Reduces non-actionable warning noise immediately while deferring risky nullable/API/refactor work.

## 8) Validation results

1. `./striv/build/striv-build-core.sh 2>&1 | tee /tmp/striv-clean-build-warnings.log`
   - Exit code: `0`
   - First meaningful warning: `NU1510` package pruning advisory.
   - Warning count: build summary reported `2775` in main solution stage.
   - Pass/fail: pass (with warnings).
   - Output truncated: console truncated, log file retained.

2. `./striv/build/striv-build-core.sh 2>&1 | tee /tmp/striv-clean-build-warnings-after.log`
   - Exit code: `0`
   - First meaningful warning: nullable warnings in core/graphics paths (NU1510 absent).
   - Warning count: build summary reported `935` in this run; parsed NU1510 count is `0`.
   - Pass/fail: pass (with warnings).
   - Output truncated: console truncated, log file retained.

3. `dotnet test striv/tests/StriV.CleanGraph.Tests/StriV.CleanGraph.Tests.csproj --no-build -v minimal`
   - Exit code: `0`
   - First meaningful warning/error: none
   - Warning count: 0 (test output clean)
   - Pass/fail: pass (`4 passed, 0 failed`)
   - Output truncated: no

## 9) Worktree status
- Use `git status --short` before commit to confirm touched files.

## 10) Recommended next task
**Recommended:** `nullable strategy proposal`.

Reason:
- The warning profile is dominated by nullable-contract warnings across runtime-heavy projects.
- A strategy pass should define project ordering, contract rules (public/internal), and “safe mechanical” vs “semantic” buckets before any broad cleanup.
