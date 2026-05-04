# 620 — Stri-V shader pipeline M4a plan

## 1) Evidence collection

### Commands run
- `find . -name '*.sdsl' | sort` to inventory available SDSL corpus.
- `rg -n "shader |stage |stream |streams|partial effect|mixin |using params|compose |clone |abstract |override |base\.|\[Color\]|\[Link|\[Map" sources samples striv -g '*.sdsl' -g '*.cs'` to validate extension usage in real files.
- `rg -n "SpriteAlphaCutoff|SpriteBatchShader|IShaderMixinBuilder|EffectBytecode|ShaderMixin|ShaderClass|ParameterKey" sources/engine/Stride.Shaders sources/engine/Stride.Graphics docs/stri-v` to connect shader authoring/runtime model and prior M3 docs.
- Tooling checks:
  - `command -v dxc || true`
  - `dxc --help | head -n 40 || true`
  - `command -v slangc || true`
  - `slangc -help | head -n 40 || true`

### Files read directly
- `docs/stri-v/Claude on SDSL Extension to SPIR-V.txt`
- `sources/engine/Stride.Graphics/Shaders/SpriteAlphaCutoff.sdsl`
- `sources/engine/Stride.Graphics/Shaders/SpriteBatchShader.sdsl`
- `striv/projects/Stride.Shaders/Stride.Shaders.csproj`
- `striv/projects/Stride.Graphics/Stride.Graphics.csproj`
- `striv/StriV.Core.slnx`
- prior M3/M3.5 audit docs under `docs/stri-v/audits` for current state consistency

### Tooling availability result
- `dxc`: not found in current environment.
- `slangc`: not found in current environment.
- Therefore M4a should record compile-tool probing as optional checks, not required gates.

### Uncertainty
- This pass did not perform full semantic diffing of all engine SDSLs (large corpus); classification is evidence-based on representative files + targeted search, not exhaustive theorem-proof.
- DXC `-spirv` support could not be directly validated because `dxc` binary is absent.

---

## 2) M3 shader state recap

Based on current clean graph and prior audits:

- Clean SDK-style graph under `striv/` is the active primary graph.
- `Stride.Shaders` runtime/model assembly is present in clean graph (`striv/projects/Stride.Shaders/Stride.Shaders.csproj`).
- `Stride.Graphics` references `Stride.Shaders` in clean graph (`striv/projects/Stride.Graphics/Stride.Graphics.csproj`).
- Clean solution includes `Stride.Shaders` and `Stride.Graphics` projects (`striv/StriV.Core.slnx`).
- Legacy compiler/parser/CpNet path remains excluded from clean graph admission goals and should stay excluded.
- CoreSmoke in M3 operates on runtime path (prebuilt/effect bytecode/runtime loading boundary), not on reintroducing source SDSL compilation pipeline.

Implication for M4: authoring/compiler work must be a new Stri-V-owned prototype path, isolated from legacy Stride compiler stack.

---

## 3) SDSL extension inventory and classification

### Ground truth seed from Claude note
`Claude on SDSL Extension to SPIR-V.txt` lists the intended extension surface: `shader`, `partial effect`, `stage`, `stream`/`streams`, mandatory `override`, `abstract`, `clone`, `base`, generics/templates, `[Color]/[Link]/[Map]`, `compose`, static cross-shader calls.

### Verified against real files (representative)
- `SpriteAlphaCutoff.sdsl` and `SpriteBatchShader.sdsl` both confirm:
  - `shader` declaration
  - generic parameter (`<bool TSRgb>`)
  - `stage stream` members
  - `stage override` methods
  - `streams.*` references
  - `base.*` calls
- `SpriteAlphaCutoff.sdsl` additionally confirms inline `partial effect` with `using params` + `mixin` composition block.

### Classification for first prototype

| Feature | Classification | Notes |
|---|---|---|
| `shader` declarations | **needed in first sample** | required to parse selected sprite sample root |
| `stage` modifier | **needed in first sample** | required for `VSMain`/`Shading` signatures |
| `stream` declarations | **needed in first sample** | required (`stage stream ...`) |
| implicit `streams` object | **needed in first sample** | required (`streams.Color`, `streams.Swizzle`) |
| mandatory `override` | **needed in first sample** | appears in both stage methods |
| `base` calls | **needed in first sample** | `base.VSMain()`, `base.Shading()` |
| shader generics/templates | **needed in first sample** | `<bool TSRgb>` used directly |
| `partial effect` block | **needed soon** | can be parsed/ignored in M4b; not needed for pure shader-body lowering |
| `mixin`/`using params` in effect block | **needed soon** | required once effect composition parsing is included |
| `compose` | **needed soon** | common in material/particle composition shaders |
| `abstract` | **defer** | not needed for initial sprite sample |
| `clone` | **defer (hard gap)** | explicitly complex; postpone |
| static cross-shader calls | **defer/unknown** | not needed for first sample |
| `[Color]`, `[Link]`, `[Map]` annotations | **needed soon** | likely needed for key/reflection metadata, but not required for first shader text lowering |

---

## 4) First sample recommendation

## Recommended first sample: `SpriteBatchShader.sdsl`

### Why this one first
- It is small and focused while still representative of core SDSL authoring extensions.
- It exercises the key “minimum viable semantics” for M4b:
  - shader declaration + generic bool parameter
  - stage stream declarations
  - stage override methods
  - streams usage and base calls
- It avoids inline `partial effect` block complexity found in `SpriteAlphaCutoff.sdsl`, reducing parser scope for first prototype.

### Why not `SpriteAlphaCutoff.sdsl` as very first parser fixture
- It is also valid and close, but includes extra effect composition DSL (`namespace` + `partial effect` + `using params` + `mixin`) which increases first-parser scope.
- Better as immediate second fixture once core shader subset lowering is stable.

### Follow-up fixture order
1. `SpriteBatchShader.sdsl` (first lowering target)
2. `SpriteAlphaCutoff.sdsl` (adds effect block parsing tolerance and/or separate effect AST)

---

## 5) Lowering target decision

## Option A — Lower to clean HLSL (recommended first)
**Pros**
- Lowest dependency risk; conceptually simplest backend contract.
- Aligns with planned DXC backend (DXIL/SPIR-V) later without forcing new frontend semantics now.
- Easy to golden-test as generated text.

**Cons**
- Some SDSL semantics (`stream` weaving, clone-like behavior) remain frontend burden.
- If mixin/interface features grow, HLSL target may require more explicit desugaring than Slang.

## Option B — Lower to Slang first
**Pros**
- Potentially closer conceptual match for modular shader composition/generics.
- Might reduce some frontend complexity in later phases.

**Cons**
- Tool availability risk now (`slangc` absent).
- Adds an extra ecosystem dependency before core semantics are proven.

## Option C — Direct SPIR-V
**Pros**
- Direct Vulkan artifact path.

**Cons**
- Wrong abstraction level for M4a.
- High complexity and high lock-in too early; conflicts with stated doctrine.

## Decision
- **M4b first target: clean HLSL emission.**
- **Slang remains optional/experimental later** if extension semantics (especially mixin/clone) prove materially easier there.
- **Direct SPIR-V rejected for M4a/M4b.**

---

## 6) Parser strategy recommendation

Compared strategies:
- Handwritten recursive descent: best control for small subset and custom diagnostics.
- ANTLR grammar-first: too much upfront grammar scope for M4b.
- tree-sitter: good tooling, but extra integration and grammar authoring cost.
- regex/token prototype only: fast but brittle; poor diagnostics and scale.
- Roslyn-style architecture: ideal long-term shape, heavier initial investment.

## Recommendation for M4b
- Build a **small handwritten lexer + recursive-descent parser** for a constrained subset.
- Emit explicit AST nodes for:
  - shader declaration (+ optional generic params)
  - stage stream declarations
  - stage methods (signature + raw body text span)
- Preserve method bodies as mostly raw text initially (token spans/string slices) to avoid full HLSL grammar.
- Allow parser to skip/record unsupported constructs with structured diagnostics.

---

## 7) First lowering design (minimum passes)

1. Parse shader header:
   - `shader Name<...> : BaseA, BaseB`
2. Parse generic parameters enough to preserve/specialize literal values in a basic way.
3. Parse and collect `stage stream` declarations.
4. Parse stage methods (`stage override` currently required in subset).
5. Lowering pass A (stream model):
   - Generate explicit `struct` (or equivalent explicit variable block) for stream fields.
   - Rewrite `streams.X` access to generated target form.
6. Lowering pass B (method metadata):
   - Retain method signatures and bodies with minimal transformation.
7. `base`/`override` handling:
   - For first sample, treat `base.*` calls as pass-through placeholders (or emitted helpers) with explicit “not fully resolved inheritance” diagnostic if full flattening is not implemented yet.
8. Defer:
   - `clone`
   - full mixin merge graph
   - full effect block lowering
   - runtime artifact integration

---

## 8) Test-first plan

## Proposed new test project
- `striv/tests/StriV.ShaderPipeline.Tests`

## Initial test types
- Golden-text parser tests:
  - parse succeeds for fixture `SpriteBatchShader.sdsl`
  - AST contains expected node counts (shader, streams, stage methods, generic param)
- Golden-text lowering tests:
  - lowered HLSL output matches checked-in expected text
  - verifies `streams.*` rewrite and generated stream struct presence
- Diagnostic tests:
  - unsupported construct emits deterministic diagnostic code/message/location
- Optional compile check (if tooling exists):
  - if `dxc` present, compile lowered HLSL and assert success
  - if absent, test should skip with explicit reason

## Non-goals for tests
- No GPU runtime requirement
- No asset pipeline coupling
- No dependency on legacy parser/compiler/CpNet

---

## 9) First implementation slice proposal (M4b)

## New project
- `striv/projects/StriV.ShaderPipeline/StriV.ShaderPipeline.csproj`

## New tests
- `striv/tests/StriV.ShaderPipeline.Tests/StriV.ShaderPipeline.Tests.csproj`

## Initial fixtures
- `striv/tests/fixtures/shaders/SpriteBatchShader.sdsl` (copied, minimized fixture version)
- Optional second fixture: `SpriteAlphaCutoff.sdsl` as parse-only for effect block tolerance

## Minimum deliverables
- Lexer + parser subset
- AST model
- Lowering pass to clean HLSL text for first sample
- Golden output + parser/lowering tests
- Optional DXC compile smoke (conditional)

## Explicit exclusions
- No runtime integration in `Stride.Graphics`/`Stride.Rendering`
- No old compiler/parser project import
- No CppNet
- No SPIR-V binary emission in M4b

---

## 10) Risk register

1. **Accidental legacy re-coupling** (`Stride.Shaders.Compiler`/`Stride.Shaders.Parser`/CppNet)
   - Mitigation: enforce project-reference guardrails in `striv/` only.
2. **Parser scope creep**
   - Mitigation: fixture-driven subset contract; unsupported features emit diagnostics.
3. **HLSL grammar complexity**
   - Mitigation: preserve method bodies as raw text initially.
4. **`stream`/`streams` weaving complexity**
   - Mitigation: first sample-limited rewrite strategy and deterministic rules.
5. **`clone` semantics complexity**
   - Mitigation: explicitly deferred.
6. **Slang toolchain availability**
   - Mitigation: keep optional; do not gate M4b on slangc.
7. **DXC availability**
   - Mitigation: optional compile tests with skip semantics.
8. **Semantic drift (compiles but behavior differs)**
   - Mitigation: add later differential tests on selected shader behavior.
9. **Premature runtime/asset coupling**
   - Mitigation: keep prototype library + tests standalone.

---

## 11) Recommended M4b implementation prompt

> Implement the first Stri-V shader pipeline prototype in the clean `striv/` graph.
>
> Scope:
> - Create `striv/projects/StriV.ShaderPipeline/StriV.ShaderPipeline.csproj`.
> - Create `striv/tests/StriV.ShaderPipeline.Tests` and add it to `striv/StriV.Core.slnx`.
> - Add first fixture `striv/tests/fixtures/shaders/SpriteBatchShader.sdsl`.
> - Implement a small handwritten lexer+parser that supports:
>   - `shader` declarations with optional generic parameters
>   - `stage stream` field declarations
>   - `stage override` methods with preserved raw HLSL bodies
>   - `streams.<name>` references in method text
> - Implement a first lowering pass that emits clean HLSL text:
>   - explicit stream carrier struct
>   - rewritten `streams.<name>` references
>   - preserved method bodies/signatures where possible
> - Add golden tests for parse and lowered output.
> - Add optional DXC compile test that runs only if `dxc` is present.
>
> Constraints:
> - Do **not** import or reference `Stride.Shaders.Compiler`, `Stride.Shaders.Parser`, or CppNet.
> - Do **not** integrate with runtime rendering, asset pipeline, or SPIR-V emission yet.
> - Keep implementation small, test-driven, and evidence-backed.

