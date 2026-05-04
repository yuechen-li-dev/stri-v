# 790 - StriV Shader Pipeline M4r SpriteAlphaCutoff effect parse validation

## 1) Files changed
- `striv/tests/fixtures/shaders/sdsl/sprite/SpriteAlphaCutoff.sdsl`
- `striv/projects/StriV.ShaderPipeline/Ast/ShaderAst.cs`
- `striv/projects/StriV.ShaderPipeline/Parsing/ShaderParser.cs`
- `striv/projects/StriV.ShaderPipeline/Lowering/ShaderLowerer.cs`
- `striv/tests/StriV.ShaderPipeline.Tests/SpriteAlphaCutoffEffectTests.cs`

## 2) Fixture choice
- Choice: **exact copy**.
- Source path: `sources/engine/Stride.Graphics/Shaders/SpriteAlphaCutoff.sdsl`.
- Fixture path: `striv/tests/fixtures/shaders/sdsl/sprite/SpriteAlphaCutoff.sdsl`.
- Features exercised: shader declaration with bool generic parameter, streams, stage override methods, `streams.*`, `base.*`, namespace wrapper, `partial effect`, `using params`, and `mixin`.

## 3) Effect block model
- Added `SdslEffectBlock` record to AST.
- Added document-level `EffectBlocks` collection on `SdslDocument`.
- Namespace handling: captured namespace string as `NamespaceName`.
- Using params capture: list of parsed entries (`UsingParams`).
- Mixin capture: raw mixin invocation text list (`Mixins`) without semantic resolution.
- Raw body/span strategy: capture `RawBodyText`, plus `BodySpan` and top-level `Span` via balanced-block parsing.

## 4) Parser behavior
- Namespace parsing: tolerates and scans `namespace <qualified.name> { ... }` blocks.
- Partial effect parsing: scans `partial effect <Name> { ... }` using balanced-block capture.
- Unsupported diagnostics: emits deterministic non-fatal `SD400/SD401/SD402` diagnostics with effect name and location.
- Remains raw: mixin generic arguments and parameter key references are preserved as text and not bound.

## 5) Lowering behavior
- Shader lowering remains independent and unchanged for shader-class output path.
- Effect block is not lowered into runtime effect artifacts.
- Unsupported semantics are represented through parser diagnostics only.

## 6) Diagnostics
- `SD400`: partial effect parsed but effect lowering/artifact generation is not implemented.
- `SD401`: `using params` parsed but parameter binding is not implemented.
- `SD402`: mixin entry parsed but effect composition is not implemented.

## 7) Tests
- Added `SpriteAlphaCutoff_Parse_FindsShaderAndEffectBlock`:
  - Verifies shader `SpriteAlphaCutoff` and one effect block `SpriteAlphaCutoffEffect`.
  - Verifies namespace `Stride.Rendering`.
- Added `SpriteAlphaCutoff_Parse_CapturesUsingParamsAndMixin`:
  - Verifies `SpriteBaseKeys` capture and mixin text capture.
- Added `SpriteAlphaCutoff_Parse_EmitsEffectUnsupportedDiagnostics`:
  - Verifies diagnostics `SD400`, `SD401`, `SD402`.
- Added `SpriteAlphaCutoff_ShaderLowering_DoesNotRequireEffectLowering`:
  - Verifies shader lowering continues and lowered HLSL omits raw `partial effect` text.

## 8) Validation results
1. Command: `dotnet test striv/tests/StriV.ShaderPipeline.Tests/StriV.ShaderPipeline.Tests.csproj`
   - Exit code: `0`
   - First meaningful warning/error: none (one existing skipped test)
   - Pass/fail: **PASS**
   - Output truncated: **no**

2. Command: `dotnet test striv/tests/StriV.CleanGraph.Tests/StriV.CleanGraph.Tests.csproj --no-build -v minimal`
   - Exit code: `0`
   - First meaningful warning/error: none
   - Pass/fail: **PASS**
   - Output truncated: **no**

3. Command: `./striv/build/striv-build-core.sh`
   - Exit code: `0`
   - First meaningful warning/error: pre-existing solution warnings (e.g. `CS1030` and nullable warnings)
   - Pass/fail: **PASS**
   - Output truncated: **yes** (very large build log)

4. Command: `dotnet test striv/tests/StriV.ShaderPipeline.Tests/StriV.ShaderPipeline.Tests.csproj --filter "FullyQualifiedName~SpriteBatchSpecializedFalse_DxcInventory_WhenAvailable" -v normal`
   - Exit code: `0`
   - First meaningful warning/error: none
   - Pass/fail: **PASS**
   - Output truncated: **no**

## 9) Limitations
- No effect artifact model yet.
- No parameter key binding.
- No mixin instantiation from effect block.
- No runtime integration.
- No asset pipeline integration.
- No clone/compose.
- SpriteAlphaCutoff SPIR-V not required/implemented in this milestone.

## 10) Recommended next task
- **partial effect artifact design**: define document-level artifact model and deterministic lowering boundaries before runtime integration.
