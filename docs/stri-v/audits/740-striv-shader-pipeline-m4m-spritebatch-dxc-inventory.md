# 740 - StriV Shader Pipeline M4m SpriteBatch DXC Inventory

## 1) Files changed

- `striv/tests/StriV.ShaderPipeline.Tests/SpriteBatchDxcInventoryTests.cs`
- `docs/stri-v/audits/740-striv-shader-pipeline-m4m-spritebatch-dxc-inventory.md`

## 2) Inventory goal

This task treats SpriteBatch compilation as **diagnostic inventory**, not as a requirement to force full semantic correctness. The objective is to identify what still blocks production-ish lowering validity and classify the blockers without implementing broad missing features.

## 3) Specialized output summary

Specialization used:

- `SpriteBatchShader<bool TSRgb>` with `TSRgb=false` (primary diagnostic path).

Observed from tests:

- standalone `TSRgb` identifier: **not present** after lowering.
- raw top-level SDSL constructs (`shader`, `stage stream`, `stage override`): **not present** in lowered output.
- raw `base.VSMain()` call: **not present** (rewritten helper call exists).
- helper calls for resolved base calls: **present** (`__base_SpriteBase_VSMain`).
- diagnostics: no generic-missing/unknown or unresolved-base diagnostics are expected in this path; any remaining diagnostics are constrained to currently known unsupported base-method cases (`SD312`) when present.

## 4) DXC attempt

DXC availability was checked and compile inventory was run.

Commands used by test helper when DXC is available:

- `dxc -T vs_6_0 -E VSMain -spirv <lowered.hlsl> -Fo <temp>.vs.spv`
- `dxc -T ps_6_0 -E PSMain -spirv <lowered.hlsl> -Fo <temp>.ps.spv`

Result in this environment:

- VS compile: exit code `0`
- PS compile: exit code `0`
- temp artifacts produced for both stages.

Classification outcome:

- No immediate DXC blocker was reproduced for the `TSRgb=false` specialized pair in this sandbox run.
- Remaining work is still expected around broader production semantics outside this minimal path (e.g., deeper inheritance/generic feature surface), but not evidenced as a first-error blocker in this focused inventory.

## 5) Tiny fixes applied

No lowerer semantic changes were required. Only focused diagnostic/inventory tests were added.

## 6) Tests

Added tests:

- `SpriteBatchSpecializedFalse_LoweredOutput_HasNoStandaloneTSRgb`
  - proves bool specialization replacement removes standalone generic identifier usage.
- `SpriteBatchSpecializedFalse_LoweredOutput_HasNoRawShaderStageKeywords`
  - proves output shape does not leak raw SDSL top-level constructs and resolves `base.VSMain()` rewrite shape.
- `SpriteBatchSpecializedFalse_LoweredOutput_HasExpectedRemainingDiagnostics`
  - constrains residual diagnostics to expected unsupported-base-method bucket only (`SD312`) if any appear.
- `SpriteBatchSpecializedFalse_DxcInventory_WhenAvailable`
  - writes lowered HLSL artifact to temp, attempts DXC compile for VS/PS, and classifies failure shape when compile is not successful.

xUnit analyzer warnings:

- Existing warnings (`xUnit2029`, `xUnit2031`) remain in pre-existing test files not modified by this task.

## 7) Validation results

1. `dotnet test striv/tests/StriV.ShaderPipeline.Tests/StriV.ShaderPipeline.Tests.csproj`
   - exit: `0`
   - result: pass
   - first meaningful warning: existing xUnit analyzer warnings in pre-existing files.
   - output truncated: no

2. `dotnet test striv/tests/StriV.CleanGraph.Tests/StriV.CleanGraph.Tests.csproj --no-build -v minimal`
   - exit: `0`
   - result: pass
   - first meaningful warning/error: none emitted in captured output.
   - output truncated: no

3. `./striv/build/striv-build-core.sh`
   - exit: `0`
   - result: pass
   - first meaningful warning: existing nullable/#warning diagnostics in core engine projects.
   - output truncated: yes (tool output truncation due to volume)

4. `./striv/build/striv-probe-dxc.sh --require`
   - exit: `0`
   - result: pass
   - first meaningful warning/error: none; dxc detected with `-spirv` support and smoke compile OK.
   - output truncated: no

## 8) Recommended next task

Implement the **next smallest blocker from inventory** by expanding this same specialized path to include one additional production-ish dependency edge (for example, the first missing base-layer/type reference that appears when moving beyond the current SpriteBase/SpriteBatch pair), while preserving the no-overreach constraints.
