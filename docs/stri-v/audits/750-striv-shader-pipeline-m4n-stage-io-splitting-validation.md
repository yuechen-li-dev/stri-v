# 750 - StriV Shader Pipeline M4n Stage IO Splitting Validation

## 1. Files changed
- striv/projects/StriV.ShaderPipeline/Lowering/ShaderLowerer.cs
- striv/tests/StriV.ShaderPipeline.Tests/ShaderPipelineTests.cs
- docs/stri-v/audits/750-striv-shader-pipeline-m4n-stage-io-splitting-validation.md

## 2. Problem recap
A single `StriVStageStreams` carrier mixed stage semantics across VS and PS boundaries. This is structurally fragile because `SV_Target*` is pixel output-only, while `SV_Position` and interpolants represent VS output / PS input transition data. Claude flagged this semantic over-merging risk; M4n addresses it with lowering-time stage IO splitting while preserving unified authoring streams.

## 3. Stage IO model
- Authoring model remains unified (`SdslStream`).
- New lowering-time semantic classification enum: `StreamSemanticKind`.
- New stage layout container: `StageIoLayout` with `VSInput`, `VSOutput`, `PSInput`, `PSOutput`.
- Classification rules implemented:
  - `SV_Target*` => pixel output only.
  - `SV_Position` => VS output (+ PS input by conservative pass-through).
  - `COLOR*`, `TEXCOORD*`, `NORMAL*`, `TANGENT*`, `BINORMAL*` => interpolants (VS output + PS input).
  - `POSITION` => vertex input kind (currently conservatively passed through like interpolant in this subset).
  - unknown => conservative interpolant path (VS output + PS input) plus diagnostic.
- Generated structs now:
  - `StriVVSOutput`
  - `StriVPSInput`
  - `StriVPSOutput`
- Ordering is deterministic by merged stream order.

## 4. Lowering changes
- `VSMain` now returns `StriVVSOutput`.
- `VSMain` local authoring variable remains `streams`, now typed as `StriVVSOutput`.
- `PSMain` now accepts `StriVPSInput streams`.
- `PSMain` still supports `float4` return with `: SV_Target` suffix for current subset.
- Base helper signatures now stage-aware for supported methods:
  - `__base_*_VSMain(inout StriVVSOutput streams)`
  - `__base_*_PSMain(inout StriVPSInput streams)`
- `streams.X` references remain intact; only carrier type changed per stage.

## 5. Diagnostics
- `SD330`: unknown stream semantic classified as interpolant.
- `SD331`: pixel-output semantic excluded from vertex output.

## 6. Tests
Added/updated in `ShaderPipelineTests`:
- `StreamLayout_ClassifiesSvTargetAsPixelOutputOnly`
- `StreamLayout_ClassifiesSvPositionAsVsOutputAndPsInput`
- `StreamLayout_ClassifiesColorAsInterpolant`
- `SimpleStreamShader_Lowering_UsesSeparateStageStructs` (renamed/updated from prior deterministic stage stream model test)
- Updated base helper assertion to `StriVVSOutput`.

## 7. DXC results
- Simple stream shader compile smoke remains expected to compile when DXC is available.
- SpriteBatch specialized false inventory test remains in suite; see validation section for run status.
- No additional blocker triage needed in this pass.

## 8. Validation results
### Command 1
- Command: `dotnet test striv/tests/StriV.ShaderPipeline.Tests/StriV.ShaderPipeline.Tests.csproj`
- Exit code: 0
- First meaningful warning/error: xUnit analyzer warnings only (non-fatal).
- Pass/fail: pass
- Output truncated: no

### Command 2
- Command: `dotnet test striv/tests/StriV.CleanGraph.Tests/StriV.CleanGraph.Tests.csproj --no-build -v minimal`
- Exit code: 0
- First meaningful warning/error: none emitted in captured output.
- Pass/fail: pass
- Output truncated: no

### Command 3
- Command: `./striv/build/striv-build-core.sh`
- Exit code: 0
- First meaningful warning/error: many existing Stride compiler warnings; no errors.
- Pass/fail: pass
- Output truncated: yes (tool truncation)

### Optional focused command
- Command: `dotnet test striv/tests/StriV.ShaderPipeline.Tests/StriV.ShaderPipeline.Tests.csproj --filter "FullyQualifiedName~SpriteBatchSpecializedFalse_DxcInventory_WhenAvailable" -v normal`
- Exit code: 0
- First meaningful warning/error: none; focused test passed.
- Pass/fail: pass
- Output truncated: no

## 9. Limitations
- Vertex input binding/source mapping is still primitive.
- Semantic classification is conservative.
- No full HLSL semantic analysis.
- No clone/compose/partial effect.
- No runtime integration.
- No shader artifact model in this pass.

## 10. Recommended next task
Refine vertex input modeling so `VSInput` can be emitted and wired deterministically for authoring streams tagged with vertex-input semantics.
