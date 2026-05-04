# Stri-V Shader Pipeline M4q Liveness Pruning Validation

## 1. Files changed
- striv/projects/StriV.ShaderPipeline/Lowering/ShaderLowerer.cs
- striv/tests/StriV.ShaderPipeline.Tests/ShaderPipelineTests.cs
- striv/tests/StriV.ShaderPipeline.Tests/GenericSpecializationTests.cs
- striv/tests/StriV.ShaderPipeline.Tests/SpriteBatchPairTests.cs

## 2. Liveness pruning design
- Implemented conservative pruning for `StriVPSInput` only.
- A stream is pruned from `PSInput` when semantic classification is known-noncritical and there is no `PSRead`/`UnknownRead` usage in stream usage analysis.
- Intentionally **not** doing full expression/liveness analysis; analysis remains lexical/classification-based.
- Safety rules enforced:
  - `SV_Position` always retained.
  - Pixel outputs are never included in PS input (existing behavior retained).
  - Unknown semantics are retained conservatively.
  - Streams with uncertain access diagnostics (`SD341`) are retained conservatively.
- Unknown/uncertain usage blocks pruning.

## 3. Lowering changes
- `BuildStageIoLayout` now takes stream usage info and uses it to decide PS input inclusion.
- Added helper `ShouldIncludeInPsInput(...)`.
- `PSInput` behavior now removes clearly unused interpolants while retaining conservative cases (`SV_Position`, unknown semantic, uncertain access).
- `VSOutput` behavior remains conservative (no aggressive additional pruning in M4q).
- SpriteBatch lowered output remained valid; focused DXC inventory test passed.

## 4. Tests added/updated
Added tests:
- `StreamLiveness_PrunesUnusedInterpolantFromPsInput`: verifies unused interpolant is omitted from `StriVPSInput`.
- `StreamLiveness_KeepsPixelReadInterpolantInPsInput`: verifies pixel-read interpolant remains.
- `StreamLiveness_KeepsSvPositionInPsInput`: verifies `SV_Position` remains.
- `StreamLiveness_DoesNotPruneUnknownSemantic`: verifies unknown semantic stays conservative.

Updated/skipped test:
- `StreamLiveness_DoesNotPruneWhenAccessUnknown` added but marked skipped with explanation: currently not practical to produce `SD341` via valid parsed stage methods without introducing invalid syntax.

xUnit cleanup test updates:
- Replaced `Assert.Single(collection.Where(...))` with `Assert.Single(collection, predicate)`.
- Replaced `Assert.Empty(collection.Where(...))` with `Assert.DoesNotContain(collection, predicate)`.

## 5. xUnit analyzer warning cleanup
Warnings before:
- `xUnit2031` in `GenericSpecializationTests.cs`, `SpriteBatchPairTests.cs`.
- `xUnit2029` in `SpriteBatchPairTests.cs`, `ShaderPipelineTests.cs`.

Files changed:
- striv/tests/StriV.ShaderPipeline.Tests/GenericSpecializationTests.cs
- striv/tests/StriV.ShaderPipeline.Tests/SpriteBatchPairTests.cs
- striv/tests/StriV.ShaderPipeline.Tests/ShaderPipelineTests.cs

Fixes applied:
- Predicate overloads for `Assert.Single`.
- `Assert.DoesNotContain` for absence checks.

Warnings after:
- `grep -E "xUnit20(29|31)" /tmp/striv-shaderpipeline-xunit-after.log` produced no matches.
- `xUnit2029`: none remain in StriV.ShaderPipeline.Tests.
- `xUnit2031`: none remain in StriV.ShaderPipeline.Tests.

## 6. DXC result
- Focused `SpriteBatchSpecializedFalse_DxcInventory_WhenAvailable` passed.

## 7. Validation results
1) `dotnet test striv/tests/StriV.ShaderPipeline.Tests/StriV.ShaderPipeline.Tests.csproj`
- Exit code: 0
- First meaningful warning/error: none
- Pass/fail: PASS
- Output truncated: No

2) `dotnet test striv/tests/StriV.CleanGraph.Tests/StriV.CleanGraph.Tests.csproj --no-build -v minimal`
- Exit code: 0
- First meaningful warning/error: none
- Pass/fail: PASS
- Output truncated: No

3) `./striv/build/striv-build-core.sh`
- Exit code: 0
- First meaningful warning/error: existing non-blocking warning in unrelated project (`CS1030` in `Stride.Core.AssemblyProcessor`)
- Pass/fail: PASS
- Output truncated: Yes (terminal capture truncated long existing warnings)

4) `dotnet test striv/tests/StriV.ShaderPipeline.Tests/StriV.ShaderPipeline.Tests.csproj --filter "FullyQualifiedName~SpriteBatchSpecializedFalse_DxcInventory_WhenAvailable" -v normal`
- Exit code: 0
- First meaningful warning/error: none
- Pass/fail: PASS
- Output truncated: No

xUnit-check commands:
- `dotnet test ... -v normal | tee /tmp/striv-shaderpipeline-xunit-before.log`: exit 0, warnings present.
- `grep -E "xUnit20(29|31)" /tmp/striv-shaderpipeline-xunit-before.log || true`: matched xUnit2029/xUnit2031 warnings.
- `dotnet test ... -v normal | tee /tmp/striv-shaderpipeline-xunit-after.log`: exit 0, warnings removed.
- `grep -E "xUnit20(29|31)" /tmp/striv-shaderpipeline-xunit-after.log || true`: no matches.

## 8. Limitations
- Not full liveness analysis.
- No full expression parsing.
- Stage association remains name-based.
- VS output pruning remains conservative.
- No runtime integration.
- No artifact model changes.

## 9. Recommended next task
- Refine liveness/usage analysis (next best incremental step after conservative PSInput pruning).
