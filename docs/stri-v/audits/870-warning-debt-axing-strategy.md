# 870 — Warning debt closeout + axing strategy

Date: 2026-05-04 (UTC)

## 1) Files changed

- `docs/stri-v/audits/870-warning-debt-axing-strategy.md` (this report only).

## 2) Warning baseline

### Baseline command

```bash
./striv/build/striv-build-core.sh 2>&1 | tee /tmp/striv-warning-debt-baseline.log
```

### Baseline result

- Exit code: `0`.
- Build summary warning count: `2621 Warning(s)`.
- Build summary errors: `0 Error(s)`.
- Build elapsed (summary): `00:00:51.40`.

### Warning-line extraction command set

```bash
grep -E "warning (CS|CA|NU|STRIDE)[0-9]+" /tmp/striv-warning-debt-baseline.log > /tmp/striv-warning-lines.log || true
wc -l /tmp/striv-warning-lines.log
sed -E 's/.*warning ((CS|CA|NU|STRIDE)[0-9]+).*/\1/' /tmp/striv-warning-lines.log | sort | uniq -c | sort -nr | head -n 40
grep -Eo "\[[^]]+\.csproj\]" /tmp/striv-warning-lines.log | sort | uniq -c | sort -nr | head -n 40
```

### Extraction results

- Total matched warning lines: `5472` (`wc -l /tmp/striv-warning-lines.log`).
- Build-summary warnings (`2621`) are lower than matched lines (`5472`) because warnings are emitted across multiple build phases/targets and repeated in aggregated output.

Top warning codes from extraction:

- `CS8618` = 2066
- `CS8625` = 940
- `CS8604` = 500
- `CS8600` = 492
- `CS8603` = 366
- `CS8601` = 292
- `CS8602` = 230
- `CS8622` = 132
- `CS8765` = 122
- `CS0618` = 68
- `CS8767` = 36
- `CS1030` = 34
- `CA2022` = 16
- `STRIDE2000` = 14

Top warning projects from extraction:

- `Stride.Rendering.csproj` = 1650
- `Stride.Engine.csproj` = 982
- `Stride.Graphics.csproj` = 886
- `Stride.FreeImage.csproj` = 390
- `Stride.Games.csproj` = 292
- `Stride.Core.AssemblyProcessor.csproj` = 232
- `Stride.Input.csproj` = 204
- `Stride.csproj` = 188
- `Stride.Core.csproj` = 180
- `Stride.Shaders.csproj` = 164
- `Stride.Core.Serialization.csproj` = 126
- `Stride.Core.IO.csproj` = 60
- `Stride.BepuPhysics.csproj` = 34

### Output truncation status

- Interactive terminal capture was truncated in-session due to volume, but the persisted log file at `/tmp/striv-warning-debt-baseline.log` was used for all counts and extraction.
- Therefore baseline metrics in this report are derived from full-file analysis, not from truncated console view.

## 3) Warning problem statement

Current warning volume is well above a practical “signal-first” threshold for iterative LLM-assisted development.

- At 2621 build-summary warnings (and 5472 extracted warning lines), real regressions are difficult to isolate quickly.
- Dominance of repeated nullability/lifecycle warnings in large projects (Rendering/Engine/Graphics) creates high-noise logs.
- In this state, LLM agents spend context budget on known debt and duplicate warnings rather than on newly introduced defects.

Conclusion: warning cleanup should not continue blindly across the full graph before deleting/quarantining code that Stri-V Core does not intend to keep.

## 4) Delete-first principle

Stri-V warning cleanup policy before deep remediation:

1. **Only fix warnings in code intended to remain in Stri-V Core.**
2. **Delete/quarantine excluded subsystems first, then clean warnings.**
3. **Do not spend cleanup cycles on Android/iOS/UWP/VR/editor/legacy-asset paths if those paths are out-of-scope for Linux/Vulkan core runtime.**

## 5) Candidate axing categories

| Category | Example files/projects | Current clean graph status | Expected warning impact | Risk | Recommendation |
| -------- | ---------------------- | -------------------------- | ----------------------: | ---- | -------------- |
| Android/iOS/UWP platform code | `sources/core/Stride.Core.IO/System.IO.Compression.Zip/ApkExpansionSupport.cs`, mobile platform blocks across `sources/engine` | **Partially excluded** (`ApkExpansionSupport.cs` already removed in `Stride.Core.IO.csproj`) | Medium | Low | Continue explicit exclusion of mobile-only compile paths from clean projects. |
| WPF/WinForms/Desktop legacy UI glue | UI/editor-facing codepaths under legacy desktop integration surfaces | Likely still reachable through broad `**/*.cs` globs in clean projects | Medium | Medium | Audit includes/removes; quarantine desktop-editor glue not needed for runtime core. |
| VR/OpenVR/OpenXR | `VRDeviceDescription.cs`, `VROverlayRenderer.cs`, `VRRendererSettings.cs` | **Partially excluded** in `Stride.Engine.csproj` | Medium | Low | Keep VR exclusions; expand to any remaining VR-linked files in other projects. |
| Legacy Audio/Celt/OpenAL paths | `sources/engine/Stride.Engine/Audio/*.cs`, audio components | **Partially excluded** in `Stride.Engine.csproj` | Medium | Low/Medium | Keep excluded for clean core unless audio is explicitly re-scoped into phase plan. |
| Old asset/compiler/editor/Game Studio/Quantum | `AssetPackage/Assets`, editor metadata, asset compiler/editor references | Likely still present in source tree and discoverable by glob/asset references | High | Medium | Quarantine editor/toolchain-only paths from runtime clean graph before warning fixes. |
| SpriteStudio/video/optional modules | `sources/engine/Stride.SpriteStudio.Runtime/**`, video/media optional stacks | Not clearly excluded in all clean graph edges | Medium | Medium | Identify graph entry points and exclude optional runtime modules if non-core. |
| Old Direct3D/SharpDX paths (Linux/Vulkan profile) | Direct3D/SharpDX/XInput/DirectInput-legacy paths under engine/graphics sources | Potentially included by broad globs | High | Medium/High | Explicitly remove non-Vulkan backend paths from clean Linux profile projects. |
| Hand-rolled Android ZIP/APK support | `ApkExpansionSupport.cs` | Already excluded in `Stride.Core.IO.csproj` | Low/Medium | Low | Treat as validated axing precedent; replicate pattern for adjacent mobile helpers. |
| Serialization/DataContract legacy fields tied to removed paths | Nullable/unused fields in removed subsystems, legacy contracts | Currently mixed with active runtime code | Medium | Medium/High | Defer fixes until subsystem fate decided; remove with subsystem or document keep-justification. |

## 6) Fix-later categories

| Warning/category | Main projects | Why kept | Cleanup strategy | Priority |
| ---------------- | ------------- | -------- | ---------------- | -------- |
| BepuPhysics nullability/interface warnings (`CS8767`, `CS8601`, `CS8625`, `CS0169`) | `Stride.BepuPhysics` | Core physics path is in clean runtime scope | Make project green first; lifecycle annotations + safe null contracts + dead field review | P1 |
| Core.Mathematics residual warnings | `Stride.Core.Mathematics` | Foundational math layer, low coupling | Fast strict cleanup pass; keep behavior identical | P1 |
| Core.IO kept runtime paths | `Stride.Core.IO` | Required runtime IO remains core | Axing first for mobile/legacy zip branches, then nullability fixes | P2 |
| FreeImage nullable warnings | `Stride.FreeImage` | Still used by graphics/image stack | Continue bounded wrapper cleanup with focused tests | P2 |
| Rendering/Engine lifecycle warnings | `Stride.Rendering`, `Stride.Engine` | Central runtime systems, high impact | Post-axing staged cleanup with tests before lifecycle/nullability changes | P3 |
| AssemblyProcessor warnings | `Stride.Core.AssemblyProcessor` | Build pipeline dependency remains required | Separate track; prioritize deterministic behavior and null-safe CLI/config paths | P3 |
| Package/advisory warnings (`NU*`, advisories) | multiple | Dependency hygiene needed but non-functional for runtime semantics | Resolve package metadata/advisories after structural axing reduces noise | P3 |

## 7) Project-by-project green strategy

1. **Stride.BepuPhysics**
   - Goal: reduce to **0 warnings**.
   - Safe fixes: interface nullability alignment, local nullable annotations, dead private field handling with validation.
   - Defer: behavior-changing physics logic or API-shape modifications without tests.

2. **Stride.Core.Mathematics**
   - Goal: **0 warnings**.
   - Safe fixes: pure nullability/annotation cleanup, static analysis adjustments.
   - Defer: any math semantic change not covered by tests.

3. **Stride.Core.IO** *(after mobile/path axing pass)*
   - Goal: **0 warnings in kept runtime paths**.
   - Safe fixes: kept Linux runtime IO nullability and API contracts.
   - Defer: mobile/legacy branches pending exclusion decision.

4. **Stride.FreeImage**
   - Goal: **0 warnings**.
   - Safe fixes: wrapper boundary nullability and initialization flow.
   - Defer: native interop behavior changes without regression tests.

5. **Stride.Input** *(after platform input axing)*
   - Goal: **0 warnings**.
   - Safe fixes: nullability/contract cleanup in retained input backends.
   - Defer: removed platform backend codepaths.

6. **Stride.Games** *(after context/platform axing)*
   - Goal: **0 warnings**.
   - Safe fixes: runtime game-loop contract/lifecycle annotations with tests.
   - Defer: editor-integrated/game-studio-linked pathways.

7. **Stride.Graphics**
   - Goal: aggressive reduction first, then **0 warnings** if feasible in phase.
   - Safe fixes: explicit null contracts, local flow fixes, Vulkan-path cleanups.
   - Defer: backend architectural changes until after non-Vulkan path quarantine.

8. **Stride.Rendering**
   - Goal: staged reduction with documented carry-over, then zeroing in follow-up.
   - Safe fixes: nullability and lifecycle annotations covered by render tests.
   - Defer: risky render-graph behavior changes lacking focused tests.

9. **Stride.Engine**
   - Goal: staged reduction and documentation; zero warnings as final stabilization target.
   - Safe fixes: lifecycle-initialized field annotations (`= null!` only when justified), contract tightening backed by tests.
   - Defer: subsystem init-order behavior changes until validated.

## 8) Warning cleanup rules after axing

- One project per Codex session.
- Make that project zero-warning **or** document every remaining warning with rationale.
- Commit per project.
- No global nullable suppression.
- Prefer truthful annotations over suppression.
- Use `= null!` only for documented lifecycle-initialized fields.
- Add tests before behavior-changing fixes.
- Delete unused fields only when not serialization/reflection/AP-dependent.
- No public API contract changes without explicit rationale.
- If uncertain, defer and document.

## 9) Test-first refactoring policy

For warnings with potential runtime behavior implications:

1. Write focused test first.
2. Lock intended current behavior.
3. Apply minimal fix.
4. Re-run tests + project build.
5. Stop/defer when intent is ambiguous.

Applies especially to:

- `CS8602` possible null dereference in runtime paths.
- `CS8604` possible null argument.
- lifecycle-initialized engine/rendering systems.
- rendering/engine initialization fields whose nullability depends on startup order.

## 10) Suppression policy

- Targeted pragma suppression is allowed only with local comment/rationale.
- Project-level suppression is allowed only for quarantined legacy zones.
- No global suppression policy.
- No broad `<NoWarn>CS86xx</NoWarn>` patterns for active core projects.
- Every suppression must be tracked as TODO debt with file/location and removal condition.

## 11) Proposed next prompt

**M6a / Axing pass 1 — clean graph platform dead-code exclusion audit**

> Inspect clean-project compile globs and remove/exclude only out-of-scope platform/legacy paths (Android/iOS/UWP/VR/editor/asset-toolchain-only) from clean projects. Do not delete source files in this pass. Rebuild with `./striv/build/striv-build-core.sh`, report warning-count delta (previous 2621 vs new), list exact project-file exclusion edits, and stop at first build blocker with blocker details.

## 12) Validation

Report-only change validation commands:

```bash
dotnet test striv/tests/StriV.AssetTool.Tests/StriV.AssetTool.Tests.csproj -v minimal
dotnet test striv/tests/StriV.AssetPipeline.Tests/StriV.AssetPipeline.Tests.csproj --no-build -v minimal
dotnet test striv/tests/StriV.ShaderPipeline.Tests/StriV.ShaderPipeline.Tests.csproj --no-build -v minimal
dotnet test striv/tests/StriV.CleanGraph.Tests/StriV.CleanGraph.Tests.csproj --no-build -v minimal
```

