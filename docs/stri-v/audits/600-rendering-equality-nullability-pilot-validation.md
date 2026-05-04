# 600 - Rendering equality nullability pilot validation

## 1) Files changed
- sources/engine/Stride.Rendering/Rendering/Materials/** (mechanical Equals signature nullability updates)
- sources/engine/Stride.Rendering/Rendering/Lights/** (mechanical Equals signature nullability updates)

## 2) Pilot scope
- Targeted `Stride.Rendering` because it remained one of the largest warning contributors after prior pilots and had many equality-signature mismatches.
- Limited to `CS8765/CS8767` because these are mechanical signature contract mismatches and can usually be fixed without behavior changes.
- Limited to `Rendering/Materials` and `Rendering/Lights` to keep blast radius ultra-bounded while still addressing a dense warning cluster.
- Lower risk because changes were confined to nullable annotations on equality overrides/implementations (`object` -> `object?`, `T` -> `T?`) with no logic/body updates.

## 3) Baseline
- Total `Stride.Rendering` `CS8765/CS8767` before: **134** (`/tmp/striv-rendering-equality-before-only.log`).
- `Materials/Lights` subset before: **106** (`/tmp/striv-rendering-materials-lights-equality-before.log`).
- Representative warnings:
  - `MaterialDiffuseCelShadingModelFeature.Equals(MaterialDiffuseCelShadingModelFeature other)` CS8767
  - `MaterialSpecularMicrofacetEnvironmentGGXLUT.Equals(object obj)` CS8765
  - `LightGroupRendererShadow.LightGroupKey.Equals(object obj)` CS8765

## 4) Fixes applied
- Pattern A:
  - Old: `public override bool Equals(object obj)`
  - New: `public override bool Equals(object? obj)`
  - Why: aligns with `object.Equals(object? obj)` override contract.
  - Behavior change: none.
- Pattern B:
  - Old: `bool Equals(IMaterialShadingModelFeature other)`
  - New: `bool Equals(IMaterialShadingModelFeature? other)`
  - Why: aligns with nullable `IEquatable<T?>`-style contract in current compile context.
  - Behavior change: none.
- Pattern C:
  - Old: `bool Equals(TConcrete other)`
  - New: `bool Equals(TConcrete? other)`
  - Why: aligns with implemented `IEquatable<TConcrete?>` contract where warning demanded nullable parameter.
  - Behavior change: none.

## 5) Deferred warnings
- Remaining `CS8767` in targeted area were deferred where signatures are still reported by compiler despite mechanical pass and may require closer per-type contract validation:
  - `MaterialDiffuseCelShadingModelFeature`
  - `MaterialSpecularCelShadingModelFeature`
  - `MaterialSpecularHairModelFeature`
  - `MaterialSpecularMicrofacetModelFeature`
  - `MaterialSpecularThinGlassModelFeature`
- Outside target area deferred by scope:
  - `IndexExtensions`, `RenderTargetDescription`, `ColorTransformGroup`, `MaterialInstance`, `Properties`, `RenderSystem`.

## 6) After counts
- Total `Stride.Rendering` `CS8765/CS8767` after: **36** (`/tmp/striv-rendering-equality-after-only.log`).
- `Materials/Lights` subset after: **10** (`/tmp/striv-rendering-materials-lights-equality-after.log`).
- Delta:
  - Total `Stride.Rendering`: **-98**
  - `Materials/Lights`: **-96**

## 7) Validation results
1. `./striv/build/striv-build-core.sh 2>&1 | tee /tmp/striv-rendering-equality-before.log`
   - Exit code: 0
   - First meaningful warning/error: CS1030 in `ObjectIdBuilder.cs`
   - Pass/fail: pass
   - Output truncated: yes (interactive capture truncation; full log on disk)
2. `grep -E "warning CS(8765|8767)" /tmp/striv-rendering-equality-before.log | grep "Stride.Rendering" | tee /tmp/striv-rendering-equality-before-only.log`
   - Exit code: 0
   - First meaningful warning/error: CS8765 in `IndexExtensions.cs`
   - Pass/fail: pass
   - Output truncated: yes (display), full file saved
3. `wc -l /tmp/striv-rendering-equality-before-only.log`
   - Exit code: 0
   - First meaningful warning/error: n/a
   - Pass/fail: pass
   - Output truncated: no
4. `grep -E "warning CS(8765|8767)" /tmp/striv-rendering-equality-before.log | grep "Stride.Rendering" | grep -E "Rendering/(Materials|Lights)" | tee /tmp/striv-rendering-materials-lights-equality-before.log`
   - Exit code: 0
   - First meaningful warning/error: CS8765 in `MaterialCelShadingLightDefault.sdsl.cs`
   - Pass/fail: pass
   - Output truncated: yes (display), full file saved
5. `wc -l /tmp/striv-rendering-materials-lights-equality-before.log`
   - Exit code: 0
   - First meaningful warning/error: n/a
   - Pass/fail: pass
   - Output truncated: no
6. `./striv/build/striv-build-core.sh 2>&1 | tee /tmp/striv-rendering-equality-after.log`
   - Exit code: 0
   - First meaningful warning/error: CS0436 in `Stride.Rendering/Properties/AssemblyInfo.cs`
   - Pass/fail: pass
   - Output truncated: yes (interactive capture truncation; full log on disk)
7. `grep -E "warning CS(8765|8767)" /tmp/striv-rendering-equality-after.log | grep "Stride.Rendering" | tee /tmp/striv-rendering-equality-after-only.log`
   - Exit code: 0
   - First meaningful warning/error: CS8765 in `IndexExtensions.cs`
   - Pass/fail: pass
   - Output truncated: no
8. `wc -l /tmp/striv-rendering-equality-after-only.log`
   - Exit code: 0
   - First meaningful warning/error: n/a
   - Pass/fail: pass
   - Output truncated: no
9. `grep -E "warning CS(8765|8767)" /tmp/striv-rendering-equality-after.log | grep "Stride.Rendering" | grep -E "Rendering/(Materials|Lights)" | tee /tmp/striv-rendering-materials-lights-equality-after.log`
   - Exit code: 0
   - First meaningful warning/error: CS8767 in `MaterialDiffuseCelShadingModelFeature`
   - Pass/fail: pass
   - Output truncated: no
10. `wc -l /tmp/striv-rendering-materials-lights-equality-after.log`
   - Exit code: 0
   - First meaningful warning/error: n/a
   - Pass/fail: pass
   - Output truncated: no
11. `dotnet test striv/tests/StriV.CleanGraph.Tests/StriV.CleanGraph.Tests.csproj --no-build -v minimal`
   - Exit code: 0
   - First meaningful warning/error: none
   - Pass/fail: pass
   - Output truncated: no

## 8) Tests
- Command: `dotnet test striv/tests/StriV.CleanGraph.Tests/StriV.CleanGraph.Tests.csproj --no-build -v minimal`
- Result: pass (4/4)
- Proves: clean graph test project still passes after mechanical signature updates.
- Does not prove: runtime rendering behavior correctness across all lighting/material combinations.

## 9) Worktree status
Command run:
- `git status --short`

Status: modified files only in target Materials/Lights plus this audit document.

## 10) Recommended next task
**Recommendation: another low-risk Rendering equality pass.**
- Remaining targeted warnings are now concentrated in a small set of `Equals(T other)` implementations that need per-type contract confirmation.
