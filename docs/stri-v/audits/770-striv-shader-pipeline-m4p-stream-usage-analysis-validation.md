# 770 - StriV Shader Pipeline M4p Stream Usage Analysis Validation

## 1) Files changed
- `striv/projects/StriV.ShaderPipeline/Lowering/StreamUsageAnalyzer.cs`
- `striv/projects/StriV.ShaderPipeline/Lowering/ShaderLowerer.cs`
- `striv/tests/StriV.ShaderPipeline.Tests/StreamUsageAnalyzerTests.cs`
- `striv/tests/StriV.ShaderPipeline.Tests/SpriteBatchDxcInventoryTests.cs`

## 2) Usage model
- Added `StreamAccessKind` (`Read`, `Write`, `ReadWrite`, `Unknown`).
- Added `StageKind` (`Vertex`, `Pixel`, `Unknown`).
- Added `StreamAccess` (`StreamName`, `Stage`, `Kind`, `Span`).
- Added `StreamUsage` aggregate per stream with booleans for VS/PS/Unknown read/write.
- Added `StreamUsageAnalysisResult` carrying `Accesses`, `Usage`, and `Diagnostics`.

## 3) Scanner behavior
- Handwritten scanner over raw method body text.
- Skips string literals, line comments, block comments.
- Detects token pattern `streams.<identifier>`.
- Supports member chains/swizzles (`streams.Color.rgb`) and attributes write/readwrite to base stream (`Color`).
- Classification:
  - `=` => `Write`
  - compound assignment => `ReadWrite`
  - pre/post increment/decrement => `ReadWrite`
  - relational/equality and most expression punctuation context => `Read`
  - fallback => `Unknown` + `SD341`
- Limitations: no full expression parser; complex contexts may classify conservatively as unknown/read.

## 4) Integration
- Added `StreamUsage` payload onto `LoweringResult`.
- `ShaderLowerer` now runs analysis after method merge using declared merged stream names.
- Analysis currently runs over `VSMain`, `PSMain`, and `Shading` only.
- Lowering does not consume usage to prune IO yet.
- Diagnostics from usage analysis are appended as non-fatal diagnostics.

## 5) Diagnostics
- `SD340`: reference to undeclared stream.
- `SD341`: uncertain access classification (falls back conservative).

## 6) Tests
- Added `StreamUsage_DetectsVsWrite`.
- Added `StreamUsage_DetectsVsRead`.
- Added `StreamUsage_DetectsCompoundReadWrite`.
- Added `StreamUsage_DetectsSwizzleWrite`.
- Added `StreamUsage_IgnoresCommentsAndStrings`.
- Added `SpriteBatch_UsageAnalysis_CapturesColorOrSwizzleUsage`.
- Added `StreamUsage_DiagnosesUndeclaredStream`.
- Updated inventory diagnostic whitelist to include `SD340`.
- xUnit2031 cleanup was not applied globally; existing warnings remain in untouched legacy assertions.

## 7) DXC result
- Focused SpriteBatch DXC inventory test command passes in this environment.

## 8) Validation results
1. `dotnet test striv/tests/StriV.ShaderPipeline.Tests/StriV.ShaderPipeline.Tests.csproj`
   - exit code: 0
   - first meaningful warning/error: xUnit analyzer warnings (xUnit2031/xUnit2029) in existing files
   - pass/fail: pass
   - output truncated: no
2. `dotnet test striv/tests/StriV.CleanGraph.Tests/StriV.CleanGraph.Tests.csproj --no-build -v minimal`
   - exit code: 0
   - first meaningful warning/error: none emitted
   - pass/fail: pass
   - output truncated: no
3. `./striv/build/striv-build-core.sh`
   - exit code: 0
   - first meaningful warning/error: repository-wide existing build warnings
   - pass/fail: pass
   - output truncated: yes (very large output)
4. `dotnet test striv/tests/StriV.ShaderPipeline.Tests/StriV.ShaderPipeline.Tests.csproj --filter "FullyQualifiedName~SpriteBatchSpecializedFalse_DxcInventory_WhenAvailable" -v normal`
   - exit code: 0
   - first meaningful warning/error: none
   - pass/fail: pass
   - output truncated: no

## 9) Limitations
- Not full expression parsing.
- No liveness-driven output pruning yet.
- Stage association is name-based (`VSMain`/`PSMain`/`Shading`).
- `Shading` classification remains provisional.
- No runtime or asset-pipeline integration.

## 10) Recommended next task
- Implement liveness-driven output pruning using the new usage model while preserving deterministic lowering behavior.
