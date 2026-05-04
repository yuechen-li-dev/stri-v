# Stri-V Shader Pipeline M4i: First Inheritance/Mixin Merge Plan

## 1) Evidence collection

### Commands run
1. `pwd && rg -n --files -g 'AGENTS.md'`
2. `rg -n "shader .*:|stage override|base\.|abstract|clone|compose|partial effect|mixin |using params|stage stream|streams\." sources/engine/Stride.Graphics/Shaders striv/tests/fixtures/shaders docs/stri-v -g '*.sdsl' -g '*.txt'`
3. `rg -n "class .*Mixin|ShaderMixin|ShaderClass|base\.|clone|abstract|override|Mixin|Composition|ShaderSource" sources/engine/Stride.Shaders.Compiler sources/engine/Stride.Shaders.Parser sources/shaders/Stride.Core.Shaders sources/engine/Stride.Shaders`
4. `sed -n '1,260p' striv/projects/StriV.ShaderPipeline/Ast/ShaderAst.cs`
5. `sed -n '1,320p' striv/projects/StriV.ShaderPipeline/Parsing/ShaderParser.cs`
6. `sed -n '1,260p' striv/projects/StriV.ShaderPipeline/Parsing/BaseCallScanner.cs`
7. `sed -n '1,320p' striv/projects/StriV.ShaderPipeline/Lowering/ShaderLowerer.cs`
8. `sed -n '1,360p' striv/tests/StriV.ShaderPipeline.Tests/ShaderPipelineTests.cs`
9. `sed -n '1,260p' docs/stri-v/audits/690-striv-shader-pipeline-m4h-base-call-validation.md`

### Files inspected (high-signal)
- Current handwritten pipeline:
  - `striv/projects/StriV.ShaderPipeline/Ast/ShaderAst.cs`
  - `striv/projects/StriV.ShaderPipeline/Parsing/ShaderParser.cs`
  - `striv/projects/StriV.ShaderPipeline/Parsing/BaseCallScanner.cs`
  - `striv/projects/StriV.ShaderPipeline/Lowering/ShaderLowerer.cs`
  - `striv/tests/StriV.ShaderPipeline.Tests/ShaderPipelineTests.cs`
  - `striv/tests/fixtures/shaders/sdsl/simple_stream_shader.sdsl`
  - `striv/tests/fixtures/shaders/sdsl/SpriteBatchShader.sdsl`
- Real SDSL references:
  - `sources/engine/Stride.Graphics/Shaders/SpriteBatchShader.sdsl`
  - `sources/engine/Stride.Graphics/Shaders/SpriteAlphaCutoff.sdsl`
  - `sources/engine/Stride.Graphics/Shaders/SpriteBase.sdsl`
  - `sources/engine/Stride.Graphics/Shaders/ShaderBase.sdsl`
  - `sources/engine/Stride.Graphics/Shaders/SpriteSignedDistanceFieldFontShader.sdsl`
- Notes/reference text:
  - `docs/stri-v/Claude on SDSL Extension to SPIR-V.txt`

### Real SDSL patterns observed
- Shader inheritance includes both single and multi-base lists (`: SpriteBase`, `: ShaderBase, Texturing`).
- Stage stream declarations are widely used and semantically meaningful across VS/PS methods.
- Base calls occur in both statement and expression contexts (`base.VSMain();`, `base.Shading()` and swizzle/member-chained forms).
- Generic shader headers occur in production fixtures (`shader SpriteBatchShader<bool TSRgb> : SpriteBase`).
- `partial effect`, `using params`, `mixin`, and other composition DSL constructs exist in real files but are out of current parser/lowerer subset.

### Uncertainty
- Legacy exact semantics for ambiguous multiple-base method resolution vs stage uniqueness are not yet fully extracted from compiler internals.
- Full generic base specialization behavior is intentionally deferred.
- Expression-context base call rewriting (e.g., `base.Shading().rrrr`) is possible with span-based replacement but should be validated carefully in M4j tests.

## 2) Current pipeline state

- AST currently models:
  - shader name,
  - generic header text,
  - base shader list,
  - stage streams,
  - stage methods,
  - base call inventory (`BaseCall` with name/args/count/span).
- Lowering currently supports deterministic stage-stream IO for the first subset:
  - `StriVStageStreams` struct emission,
  - generated `VSMain()` returning streams,
  - generated `PSMain(StriVStageStreams streams)` consuming streams.
- Base calls are scanned and diagnosed (`SD302`) but unresolved.
- SpriteBatch fixture parse/lower is currently diagnostic-first (SD300/SD301/SD302 + TODO comments), not semantic merge.

## 3) Mixin/inheritance model proposal

### Shader registry model
- Introduce an in-memory registry at merge phase input:
  - `Dictionary<string, SdslShader>` keyed by shader name (ordinal case-sensitive for first pass).
- Input for first pass should be a parsed document set (not just a single shader string) so inheritance links can resolve deterministically.

### Inheritance graph
- Build directed edges `Child -> Base[i]` preserving base list order as parsed.
- First pass behavior:
  - **Supported:** zero or one base shader.
  - **Parsed but diagnostic-only:** multiple base shaders.
- For multi-base first pass, emit explicit ambiguity/deferred diagnostics and skip deep merge.

### Cycle detection
- DFS with 3-color state (`unvisited`, `visiting`, `done`).
- Any back-edge yields `SD311` with cycle path text.
- Abort merge for nodes in cycle; keep parse artifacts for diagnostics.

### Unresolved base and duplicate base diagnostics
- If base name not found in registry: `SD310` unresolved base shader.
- If a shader repeats same base in list (exact match): emit deterministic duplicate-base diagnostic (new code optional; if not introduced in M4j, fold into `SD313`/message).

### Generic base references
- Parse raw base entries as today.
- If base entry contains generic specialization syntax or references generic arguments, emit `SD314` unsupported generic base specialization and skip semantic merge for that edge.

## 4) Merge order semantics

Deterministic order for first supported slice:
1. Recursively merge immediate base first.
2. Then apply child declarations.
3. For multi-base lists (future), preserve listed order left-to-right.

Rules:
- Streams:
  - start with merged base stream layout,
  - append child streams in source order,
  - reject incompatible redefinitions by diagnostics.
- Stage methods:
  - resolve by method name,
  - child declaration can override base implementation under explicit override rules,
  - unresolved/ambiguous situations diagnose and do not silently pick a winner.

This maps to current subset because lowering already depends on a single deterministic `StriVStageStreams` layout and method bodies retained as raw text.

## 5) Stream merge semantics

### Identity
- First pass stream identity is **both name and semantic constrained**:
  - primary key: stream name,
  - uniqueness guard: semantic token case-insensitive uniqueness (matching current lowerer behavior).

### Conflict rules
- Same name + same type + same semantic:
  - allow dedup (idempotent redeclaration), no duplicate emitted.
- Same name + different type or semantic:
  - `SD315` incompatible stream redeclaration.
- Different name + same semantic:
  - diagnostic (reuse current semantics rule spirit); recommend keep as error-level diagnostic for determinism.

### Ordering
- Base merged streams first in stable order.
- Child unique additions appended in child source order.

### Diagnostics to emit
- Duplicate stream name (`SD200` existing local duplicate; cross-inheritance can map to SD315 if incompatible).
- Duplicate semantic (`SD201` existing behavior, extended across merged layout).
- Incompatible redeclaration (`SD315`).

## 6) Stage method merge semantics

For M4j first implementation subset:
- Method identity: by `Name` only.
- Base method body retained as generated helper body for resolvable base calls.
- Derived method body is final stage method body when overriding.
- `base.Method(args)` in derived body resolves to immediate base implementation helper.
- Missing base method for a base call => `SD312`.
- Multiple bases defining same method (when multi-base parsed) => `SD313` ambiguous base method (defer resolution).
- `override` without base method => diagnostic (recommend `SD312` variant message).
- Base has method and derived redefines without `override` => diagnostic (new code optional; can use `SD313`-family until dedicated code exists).
- If derived omits method and base provides one => inherit base method as final emitted method.
- `abstract` methods:
  - parser support is out of current subset; if encountered once parsed in future, defer with diagnostic until final composition validation stage exists.

## 7) Base call lowering proposal

### Option comparison
- **A) helper mutates `inout StriVStageStreams streams`**
  - Pros: natural for statement-style base mutations (`base.VSMain();`), avoids copying struct repeatedly, maps cleanly to VS stream mutation model.
  - Cons: needs helper signature transformation for methods that originally had no stream parameter.
- **B) helper returns `StriVStageStreams`**
  - Pros: purely functional shape.
  - Cons: awkward for non-VS methods and mixed side-effect expectations; introduces more temporary plumbing.
- **C) preserve original signature and use body-local stream variable**
  - Pros: fewer signature changes.
  - Cons: difficult to guarantee deterministic shared stream state semantics across chained calls.

### Recommendation
Adopt **A (`inout StriVStageStreams streams`)** for first implementation.

Proposed generated shape (for VS-like methods):
```hlsl
void __base_BaseSprite_VSMain(inout StriVStageStreams streams)
{
    streams.Color = float4(1, 1, 1, 1);
}

StriVStageStreams VSMain()
{
    StriVStageStreams streams;
    __base_BaseSprite_VSMain(streams);
    streams.Color *= 0.5;
    return streams;
}
```

### HLSL validity note
- HLSL supports `inout` function parameters for user structs; DXC should accept this pattern in helper functions compiled from the same translation unit.
- M4j should add DXC smoke coverage for a synthetic inheritance fixture to validate this emitted shape concretely.

## 8) Method body rewriting requirements

- Rewrite only modeled `BaseCall` spans (from scanner), never blind string replace.
- Preserve comments/strings and untouched raw text outside replacement spans.
- Keep method bodies raw otherwise.
- First pass support:
  - zero-argument base calls,
  - simple argument forwarding.
- Argument rule:
  - lower `base.Method(arg1, arg2)` to `__base_<Base>_Method(streams, arg1, arg2)`.
  - If helper signature cannot be safely formed for specific method kind, diagnose as unsupported args variant (`SD302` or new `SD312` subcase) rather than guess.

## 9) First synthetic fixture plan

Add synthetic fixture before SpriteBatch semantic lowering:
- `striv/tests/fixtures/shaders/sdsl/inheritance/simple_base_shader.sdsl`

Suggested content:
```hlsl
shader BaseSprite
{
    stage stream float4 Color : COLOR0;

    stage override void VSMain()
    {
        streams.Color = float4(1.0, 1.0, 1.0, 1.0);
    }
}

shader ChildSprite : BaseSprite
{
    stage override void VSMain()
    {
        base.VSMain();
        streams.Color *= 0.5;
    }

    stage override float4 PSMain()
    {
        return streams.Color;
    }
}
```

Expected checks:
- Parse captures two shaders and one inheritance edge.
- Child merged layout includes `Color` stream.
- Lowered child HLSL includes base helper + rewritten base call.

## 10) Test-first implementation plan (M4j)

1. Parse multiple shaders in one document/source set.
2. Build shader registry keyed by name.
3. Detect inheritance edge and topological merge for single-base.
4. Merge base stream into child layout.
5. Emit base helper for inherited `VSMain` implementation.
6. Rewrite `base.VSMain()` to helper call.
7. DXC compile smoke for lowered synthetic inheritance fixture (when `/usr/bin/dxc` exists).
8. Diagnostic test: unresolved base shader (`SD310`).
9. Diagnostic test: missing base method for base call (`SD312`).
10. Diagnostic test: stream conflicts (`SD315` and/or `SD201`).

## 11) Diagnostics plan

Current codes:
- `SD300` inheritance parsed but merge not implemented.
- `SD301` generic specialization not implemented.
- `SD302` base call unresolved pending merge.

Planned refinement:
- `SD310`: unresolved base shader.
- `SD311`: inheritance cycle.
- `SD312`: missing base method for base call / invalid override target.
- `SD313`: ambiguous base method (multi-base conflict).
- `SD314`: unsupported generic base specialization.
- `SD315`: incompatible stream redeclaration across inheritance merge.

Transition recommendation:
- Keep `SD300` only for unsupported inheritance cases after M4j (e.g., multi-base unresolved).
- Replace broad `SD302` where specific causes are known (SD310/312/313/314).

## 12) Scope limits (explicit non-goals for M4i/M4j slice)

- No `clone` semantics.
- No `compose` semantics.
- No `partial effect` lowering.
- No full generic specialization.
- No multi-inheritance conflict resolution beyond diagnostics.
- No full HLSL expression parser.
- No full real SpriteBatch DXC-success target in this slice.

## 13) Risk register

1. Semantic mismatch with legacy SDSL merge behavior.
2. Wrong helper signature convention (`inout` placement/extra args ordering).
3. Incorrect stream mutation semantics during chained base calls.
4. Multi-base ambiguity handling may block real-world fixtures.
5. Generic specialization complexity may leak into base lookup early.
6. Raw-text span rewriting bugs could corrupt bodies/comments.
7. DXC may reject some emitted helper/body combinations.
8. Overfitting to SpriteBatch patterns instead of generalized deterministic merge.

## 14) Recommended M4j implementation prompt

> Implement the first **single-base inheritance merge** slice for `StriV.ShaderPipeline` with tests first.
>
> Constraints:
> - handwritten parser/lowering only (no parser generators),
> - no legacy Stride parser/compiler imports,
> - no `clone`, `compose`, `partial effect`, or full generic specialization,
> - keep method bodies raw except span-based base-call rewrites.
>
> Required work:
> 1. Introduce a shader registry (`name -> SdslShader`) for parsed shader sets.
> 2. Add inheritance graph resolution with cycle detection.
> 3. Support semantic merge for **0 or 1 base shader only**; multi-base emits diagnostic/deferred.
> 4. Merge streams deterministically (base first, child append; detect name/semantic/type conflicts).
> 5. Merge stage methods by name with override validation.
> 6. Generate base helper methods named like `__base_<BaseShader>_<Method>`.
> 7. Lower base calls by replacing scanned `BaseCall` spans with helper invocations using `inout StriVStageStreams streams` as first arg.
> 8. Add diagnostics codes SD310..SD315 as proposed, and narrow SD302 usage.
> 9. Add synthetic fixture `sdsl/inheritance/simple_base_shader.sdsl` and tests:
>    - parse multi-shader document,
>    - inheritance edge,
>    - merged stream layout,
>    - helper emission,
>    - base-call rewrite,
>    - unresolved base/missing method/conflict diagnostics,
>    - DXC smoke compile when available.
>
> Success criteria:
> - synthetic inheritance fixture lowers to deterministic HLSL with helper-based base call resolution,
> - DXC compile smoke passes when available,
> - SpriteBatch remains diagnostic-first for unsupported semantics (especially generics/multi-feature interactions).
