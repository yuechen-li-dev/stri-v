# M4j Single-Base Merge Validation

## 1) Files changed
- striv/projects/StriV.ShaderPipeline/Ast/ShaderAst.cs
- striv/projects/StriV.ShaderPipeline/Parsing/ShaderParser.cs
- striv/projects/StriV.ShaderPipeline/Lowering/ShaderLowerer.cs
- striv/tests/StriV.ShaderPipeline.Tests/ShaderPipelineTests.cs
- striv/tests/fixtures/shaders/sdsl/inheritance/simple_base_shader.sdsl

## 2) Merge model design
- Added `SdslDocument` with `Shaders` and `Diagnostics`.
- Added multi-shader document parse (`ParseSdslDocument`) and single-shader compatibility path (`ParseSdsl` first shader).
- Added registry by shader name (ordinal, case-sensitive), duplicate diagnostic `SD316`.
- Added first-pass inheritance handling with diagnostics: unresolved base `SD310`, cycle `SD311`, multiple base unsupported `SD313`, generic base specialization unsupported `SD314`.

## 3) Stream merge behavior
- Base streams emitted first, child streams appended deterministically.
- Same name+type+semantic dedups implicitly.
- Same name with conflicting type/semantic: `SD315`.
- Different name with same semantic: `SD315`.

## 4) Stage method merge behavior
- Merge by method name.
- Child override replaces base method.
- Child same-name without override => `SD313`.
- Child override without base method => `SD312`.
- Missing base method for `base.*` call => `SD312`.

## 5) Base helper/rewrite behavior
- Helper naming: `__base_{BaseShader}_{Method}`.
- Helper signature uses `inout StriVStageStreams streams`.
- Helpers generated for base methods referenced by child base-calls.
- Base-call rewriting performed from modeled `BaseCall` spans inside method bodies; zero-arg and argumented calls supported.

## 6) Diagnostics used
- SD310, SD311, SD312, SD313, SD314, SD315, SD316, plus pre-existing SD301.

## 7) Tests
- Added/updated multi-shader parse, merge, helper emission, base-call rewrite, unresolved-base-diagnostic suppression for supported fixture, unresolved base diagnostics, missing base method diagnostics, cycle diagnostics.
- Existing SpriteBatch and duplicate-stream expectations updated for new merge-era diagnostics.

## 8) DXC result
- Existing optional DXC compile-smoke test suite remains passing.
- Inheritance fixture is now lowered with helper and rewritten base call shape; compile-smoke remains optional when DXC is available in environment.

## 9) Validation results
- `dotnet test striv/tests/StriV.ShaderPipeline.Tests/StriV.ShaderPipeline.Tests.csproj` => exit 0, pass, output truncated: no.
- `dotnet test striv/tests/StriV.CleanGraph.Tests/StriV.CleanGraph.Tests.csproj --no-build -v minimal` => exit 0, pass, output truncated: no.
- `./striv/build/striv-build-core.sh` => exit 0, pass with warnings only, output truncated: yes (large warning volume).

## 10) Limitations
- Single-base only.
- No clone/compose/partial effect.
- No full generics specialization.
- No multi-base resolution.
- Raw bodies retained except span-based base-call rewrites.
- SpriteBatch not expected to compile yet.

## 11) Recommended next task
- Add SpriteBase + SpriteBatch merge fixture.
