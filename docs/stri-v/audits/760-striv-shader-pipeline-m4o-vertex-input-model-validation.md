# 760 - StriV Shader Pipeline M4o Vertex Input Model Validation

## 1) Files changed
- `striv/projects/StriV.ShaderPipeline/Lowering/ShaderLowerer.cs`
- `striv/tests/StriV.ShaderPipeline.Tests/ShaderPipelineTests.cs`
- `striv/tests/fixtures/shaders/sdsl/simple_vertex_input_shader.sdsl`

## 2) Problem recap
`POSITION` is an input-assembler vertex semantic (source data entering VS), while `SV_Position` is a system-value semantic representing clip-space position output from VS and consumed by rasterization/PS. M4n still over-transited vertex inputs through interpolant paths. M4o adds explicit VS input modeling so lowering can emit deterministic stage carriers.

## 3) Stage IO model changes
- `StageIoLayout` now actively uses `VSInput`, `VSOutput`, `PSInput`, `PSOutput` during emission.
- Classification rules now include:
  - input-only: `POSITION`, `BLENDINDICES*`, `BLENDWEIGHT*`
  - input + interpolant: `TEXCOORD*`, `COLOR*`, `NORMAL*`, `TANGENT*`, `BINORMAL*`
  - VS output / PS input: `SV_Position`
  - PS output: `SV_Target*`
  - unknown: interpolant fallback with `SD330`
- Input-only streams are emitted in `StriVVSInput` and excluded from `StriVVSOutput`/`StriVPSInput`.
- Input+interpolant streams are emitted in all relevant structs.

## 4) Lowering changes
- `StriVVSInput` is generated only when there are VS input streams.
- `VSMain` signature is now:
  - `StriVVSOutput VSMain(StriVVSInput input)` when VS inputs exist
  - `StriVVSOutput VSMain()` otherwise
- `VSMain` still creates local `StriVVSOutput streams;` and copies pass-through fields (`input`+`output` overlap) into `streams`.
- VS body applies token-aware rewrite for input-only references: `streams.<InputOnly>` -> `input.<InputOnly>` (excluding comments/strings).
- `PSMain` remains `PSMain(StriVPSInput streams)` with refined PS input fields.
- Base helper signature remains stage-specific stream carriers (`inout StriVVSOutput` for VS, `inout StriVPSInput` for PS).

## 5) Diagnostics
- Preserved:
  - `SD330` unknown semantic fallback
  - `SD331` pixel-output excluded from VS output
- `SD333`/`SD334` not added in this patch (current focused implementation rewrites in VS main only and avoids broad helper analysis).

## 6) Tests
Added tests in `ShaderPipelineTests`:
- `StreamLayout_ClassifiesPositionAsVsInputOnly`
- `StreamLayout_ClassifiesTexCoordAsInputAndInterpolant`
- `VSMain_WithInputStreams_AcceptsStriVVSInput`
- `VSMain_RewritesInputOnlyStreamsToInput`
- `PSMain_DoesNotReceiveInputOnlyPosition`

Added fixture:
- `simple_vertex_input_shader.sdsl`

## 7) DXC results
- SpriteBatch focused DXC inventory command executed (see validation section).
- No separate new fixture DXC compile test was added in this patch.

## 8) Validation results
- `dotnet test striv/tests/StriV.ShaderPipeline.Tests/StriV.ShaderPipeline.Tests.csproj`
  - exit code: 0
  - first meaningful warning/error: xUnit analyzer warning `xUnit2031` in `SpriteBatchPairTests`
  - pass/fail: pass
  - output truncated: no
- `dotnet test striv/tests/StriV.CleanGraph.Tests/StriV.CleanGraph.Tests.csproj --no-build -v minimal`
  - exit code: 0
  - first meaningful warning/error: none
  - pass/fail: pass
  - output truncated: no
- `./striv/build/striv-build-core.sh`
  - exit code: 0
  - first meaningful warning/error: existing repository-wide C# warnings (e.g., CS1030 in `ObjectIdBuilder.cs`)
  - pass/fail: pass
  - output truncated: yes (tool output truncation due to log volume)
- `dotnet test striv/tests/StriV.ShaderPipeline.Tests/StriV.ShaderPipeline.Tests.csproj --filter "FullyQualifiedName~SpriteBatchSpecializedFalse_DxcInventory_WhenAvailable" -v normal`
  - exit code: 0
  - first meaningful warning/error: none
  - pass/fail: pass
  - output truncated: no

## 9) Limitations
- Vertex layout/runtime binding is not implemented.
- Input classification remains conservative.
- No full cross-stage usage analysis.
- Input-only handling is currently VS-body text rewrite (`streams` -> `input`) for input-only fields.
- No clone/compose/partial effect support.
- No runtime integration.

## 10) Recommended next task
Refine vertex input usage analysis so VS output emission can be liveness-driven (emit only values needed post-VS), including base-helper-aware input-only access handling.
