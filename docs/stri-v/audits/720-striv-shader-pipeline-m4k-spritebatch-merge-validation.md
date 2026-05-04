# 720 — StriV Shader Pipeline M4k SpriteBatch Merge Validation

## 1. Files changed
- `striv/tests/fixtures/shaders/sdsl/sprite/SpriteBase.sdsl`
- `striv/tests/fixtures/shaders/sdsl/sprite/SpriteBatchShader.sdsl`
- `striv/tests/StriV.ShaderPipeline.Tests/SpriteBatchPairTests.cs`

## 2. Fixture choice
- **Choice:** exact copy.
- **Files used:**
  - `sources/engine/Stride.Graphics/Shaders/SpriteBase.sdsl` -> `striv/tests/fixtures/shaders/sdsl/sprite/SpriteBase.sdsl`
  - `sources/engine/Stride.Graphics/Shaders/SpriteBatchShader.sdsl` -> `striv/tests/fixtures/shaders/sdsl/sprite/SpriteBatchShader.sdsl`
- **Why this pair next:** this is the first production-ish inheritance pair already used by real engine shader authoring and exercises base stream + base method rewrite paths without requiring clone/compose/partial-effect lowering.

## 3. Parser changes
- No parser code changes were required for this step.
- Existing parser behavior already handled:
  - multi-shader `SdslDocument` parse,
  - shader inheritance headers,
  - raw generic parameter capture (`bool TSRgb`),
  - `stage stream` extraction,
  - `stage override` method extraction,
  - `base.*` call inventory.
- Unsupported constructs encountered in copied fixtures remain tolerated as non-fatal raw text/body content (e.g., effect-system-era semantics not explicitly lowered yet).

## 4. Merge/lowering behavior
- `SpriteBase` resolves from registry when lowering `SpriteBatchShader` from combined two-file source.
- Merged stream model includes base + child stream declarations in deterministic order (base first, then child additions).
- Resolvable base calls rewrite to generated helper calls:
  - `base.VSMain()` -> `__base_SpriteBase_VSMain(...)`
  - `base.Shading()` -> `__base_SpriteBase_Shading(...)`
- Unresolved base-call diagnostics (`SD312`) are now limited to truly missing methods only (none asserted for known resolvable methods in this pair).
- `SD301` generic-unsupported diagnostic remains present as expected.
- No `SD310` unresolved-base diagnostic for `SpriteBase` in the merged pair path.

## 5. Tests
Added `striv/tests/StriV.ShaderPipeline.Tests/SpriteBatchPairTests.cs` with:
- `SpriteBatchPair_Parse_ResolvesBaseShader`
  - proves both shaders are parsed from combined document and `SpriteBase` does not trigger `SD310` when lowering `SpriteBatchShader`.
- `SpriteBatchPair_Parse_CapturesGenericParameter`
  - proves raw generic text capture (`bool TSRgb`) and continued `SD301` emission.
- `SpriteBatchPair_Merge_ProducesMergedStreamLayout`
  - proves merged output contains base + child stream fields and deterministic order.
- `SpriteBatchPair_Merge_RewritesResolvableBaseCalls`
  - proves helper generation prefix `__base_SpriteBase_` and rewrite away from `base.VSMain()`.
- `SpriteBatchPair_Merge_DiagnosesOnlyTrulyUnsupportedSemantics`
  - proves expected unsupported diagnostics remain while broad/unexpected inheritance breakage diagnostics are absent.

## 6. DXC status
- SpriteBatch DXC compile remains **not required** for this milestone.
- Existing DXC smoke behavior in current test suite remains unchanged (pass/skip based on availability).

## 7. Validation results
1) Command: `dotnet test striv/tests/StriV.ShaderPipeline.Tests/StriV.ShaderPipeline.Tests.csproj`
- Exit code: `0`
- First meaningful warning/error: xUnit analyzer warning `xUnit2031` in new tests (non-blocking style warning).
- Pass/fail: **PASS**
- Output truncated: **No**

2) Command: `dotnet test striv/tests/StriV.CleanGraph.Tests/StriV.CleanGraph.Tests.csproj --no-build -v minimal`
- Exit code: `0`
- First meaningful warning/error: none emitted.
- Pass/fail: **PASS**
- Output truncated: **No**

3) Command: `./striv/build/striv-build-core.sh`
- Exit code: `0`
- First meaningful warning/error: pre-existing nullable/#warning warnings in engine/core projects.
- Pass/fail: **PASS**
- Output truncated: **Yes** (command output is very large in this environment capture).

## 8. Limitations
- Generic specialization still unsupported (`SD301`).
- No `clone` support.
- No `compose` support.
- No `partial effect` lowering.
- No multi-base semantic resolution.
- No full production SpriteBatch SPIR-V artifact path yet.
- Raw HLSL method bodies are still largely preserved and only selectively rewritten.

## 9. Recommended next task
**Implement generic parameter specialization for `bool TSRgb`** (minimal, constrained slice), so this production-ish pair can advance from parse/merge validation toward semantically correct variant materialization without broadening scope into effect/runtime integration.
