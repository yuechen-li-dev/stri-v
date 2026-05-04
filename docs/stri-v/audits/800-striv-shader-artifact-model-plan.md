# 800 — Stri-V shader artifact model plan (M4s design)

## 1) Evidence collection

### Files and areas inspected
- `striv/projects/StriV.ShaderPipeline/**` (parser/AST/lowerer boundaries, diagnostics, stage IO model).
- `striv/tests/StriV.ShaderPipeline.Tests/**` (fixture coverage, DXC smoke/inventory, specialization, diagnostics expectations).
- `striv/tests/fixtures/shaders/**` (exact copied Sprite fixtures and SDSL effect syntax).
- Prior M4 audit reports 620/630/640/710/730/740/750/760/770/780/790 for milestone chronology and explicit out-of-scope boundaries.
- Legacy runtime shader model references (`sources/engine/Stride.Shaders/**`, `sources/engine/Stride.Graphics/**EffectBytecode**`) for terminology and future mapping shape only, not for compiler import or compatibility commitments.

### Searches run
- `rg -n "EffectBytecode|ShaderBytecode|ShaderSource|ShaderMixin|Effect|ParameterKey|Reflection|InputElement|ResourceBinding|ConstantBuffer|Descriptor|SPIR-V|DXIL|bytecode|Stage|EntryPoint|ShaderStage|Semantic" sources/engine/Stride.Shaders sources/engine/Stride.Graphics striv/projects/StriV.ShaderPipeline docs/stri-v`
- `rg -n "partial effect|using params|mixin|SpriteAlphaCutoffEffect|SpriteBatchShader|SpriteBaseKeys|ColorIsSRgb" sources/engine/Stride.Graphics/Shaders striv/tests/fixtures/shaders docs/stri-v`
- `rg -n "SD2|SD3|SD4|StageIoLayout|SdslEffectBlock|EffectBlocks|VSMain|PSMain|specializ|DXC|spirv|dxc|StriVStageStreams" striv/projects/StriV.ShaderPipeline striv/tests/StriV.ShaderPipeline.Tests`

### DXC/reflection capability check
- `dxc --help | grep -i -E "reflect|fspv|spirv|fvk|Qstrip|Fc|Fre|Fo|Zi|Qembed" | head -n 80 || true`
- Result highlights: `-spirv`, `-fspv-reflect`, `-Fre`, `-Fo`, `-Qstrip_reflect`, `-Zi`, and Vulkan binding knobs are available, which supports a staged plan where initial reflection can come from Stri-V lowering while preserving a future on-ramp to compiler-assisted reflection.

### Uncertainty
- The exact DXC build/version variability across developer/CI machines can affect emitted binary determinism and optional reflection payload shape.
- First-class resource/CBV/UAV/sampler reflection completeness is currently uncertain without committing to a specific reflection extraction path (DXC text reflection, SPIR-V tools, or custom parser).

---

## 2) Current M4 pipeline recap

### What parser/lowerer can do now
- Handwritten lexer/parser/AST for HLSL + SDSL subset, no parser generators.
- Shader declarations, streams, stage methods, single-base merge path, base-call inventory and rewrite for supported paths.
- Bool generic specialization (`TSRgb`) with diagnostics for missing/unsupported/unknown specialization keys.
- Stage IO splitting (`StriVVSInput`, `StriVVSOutput`, `StriVPSInput`) and stream usage analysis with conservative PSInput pruning.
- Document-level effect block capture via `SdslEffectBlock` and `SdslDocument.EffectBlocks`.

### Fixtures covered
- Exact copied `SpriteBase.sdsl` + `SpriteBatchShader.sdsl` fixture pair path.
- Exact copied `SpriteAlphaCutoff.sdsl` fixture including `partial effect`, `using params`, and `mixin` parse/capture.
- Plain HLSL fixture compile-smoke path.

### DXC path proven
- Optional DXC probe and compile-smoke tests are in place.
- Stage-targeted compile invocations for `VSMain`/`PSMain` with `vs_6_0`/`ps_6_0` and `-spirv` when supported are validated in tests.

### Unsupported / deferred
- Full effect composition semantics (`partial effect` materialization, mixin graph resolution).
- Full parameter binding (`using params` to concrete runtime binding model).
- Full runtime integration, full asset pipeline integration, full cross-platform packaging.

---

## 3) Artifact goals (v1)

The first artifact should support:
1. Deterministic, reproducible output for the same inputs.
2. Cacheability keyed by source hash + specialization canonical key + backend profile.
3. Readable machine manifest (JSON) with explicit phase boundaries.
4. Vulkan-target stage binaries (`.spv`) for VS/PS.
5. Future backend expansion (DXIL profile side-by-side) without format reset.
6. Future runtime loading without embedding runtime assumptions today.
7. Future asset manifest linkage (human-authored TOML later, machine JSON now).
8. Diagnostic preservation across parse/lower/compile phases.

---

## 4) Non-goals (first artifact)

- Runtime renderer integration.
- Material/parameter runtime system completion.
- Hot reload system.
- Full reflection parity with legacy ecosystems.
- Cross-platform single-file packaging.
- Compatibility cloning of old `EffectBytecode` formats.
- Semantic completeness for `clone`/`compose`/full `partial effect` behavior.

---

## 5) Artifact shape proposal

### Recommendation
- **Use JSON for artifact manifest v1** (machine-authored, deterministic key order, easy schema evolution).
- Reserve **TOML for future human-authored asset manifests** that *reference* shader artifact requests.

### Proposed manifest skeleton
```json
{
  "format": "striv.shader.artifact.v1",
  "artifactVersion": 1,
  "source": {
    "path": "relative/source/path.sdsl",
    "hash": "sha256:<normalized-source-hash>",
    "language": "sdsl"
  },
  "selection": {
    "entryShader": "SpriteBatchShader",
    "effect": {
      "name": "SpriteAlphaCutoffEffect",
      "namespace": "Stride.Rendering",
      "usingParams": ["SpriteBaseKeys"],
      "mixins": ["SpriteAlphaCutoff<SpriteBaseKeys.ColorIsSRgb>"]
    }
  },
  "specialization": {
    "kind": "shader-generic",
    "shader": "SpriteBatchShader",
    "parameters": {
      "TSRgb": false
    },
    "canonicalKey": "SpriteBatchShader|TSRgb=false"
  },
  "backend": {
    "compiler": "dxc",
    "compilerVersion": "<captured>",
    "targetFamily": "spirv",
    "profile": "vulkan1.1",
    "options": ["-spirv"]
  },
  "stages": {
    "vertex": {
      "entryPoint": "VSMain",
      "targetProfile": "vs_6_0",
      "hlsl": "generated/vertex.hlsl",
      "binary": "bin/vertex.spv",
      "binaryHash": "sha256:..."
    },
    "pixel": {
      "entryPoint": "PSMain",
      "targetProfile": "ps_6_0",
      "hlsl": "generated/pixel.hlsl",
      "binary": "bin/pixel.spv",
      "binaryHash": "sha256:..."
    }
  },
  "reflection": {
    "layoutSource": "striv.lowerer.stage-io.v1",
    "vertexInputs": [],
    "vertexOutputs": [],
    "pixelInputs": [],
    "pixelOutputs": [],
    "resources": [],
    "constantBuffers": [],
    "parameterKeys": []
  },
  "diagnostics": []
}
```

### Determinism rules
- Stable key ordering in JSON writer (fixed schema order, lexicographic order for maps).
- All paths stored as `/`-normalized relative paths.
- Hash input normalization: UTF-8, `\n` line endings, trimmed trailing whitespace policy (explicitly documented and fixed).
- Semantic versioned `format` string + numeric `artifactVersion`.

---

## 6) Specialization key design

- Represent specialization as typed name/value pairs (initially bool only).
- Canonical parameter ordering: ordinal by parameter name.
- Canonical text: `<Shader>|<Param>=<value>[;<Param2>=<value2>]`.
- Cache key tuple:
  - `sourceHash`
  - `entryShader`
  - `canonicalSpecializationKey`
  - `backend.compiler`
  - `backend.targetFamily`
  - `stage targets (vs_6_0/ps_6_0)`
- If specialization missing/unsupported:
  - Keep artifact emission optional policy-driven (default: emit manifest with diagnostics, no binaries).
  - Preserve diagnostics (`SD320`, `SD321`, `SD322`) in manifest.

---

## 7) Stage output design

### Current supported stages
- `vertex` (`VSMain`, `vs_6_0`)
- `pixel` (`PSMain`, `ps_6_0`)

### Binary policy
- v1 target: `.spv` for Vulkan profile.
- Future extension: optional `.dxil` sibling outputs with same stage slots and a backend variant marker.

### Generated HLSL retention
- Keep generated stage HLSL files in artifact output by default for debuggability and reproducibility audits.
- Future knob may allow stripping generated HLSL in release cache packs, but default should be retained in M4t/M5.

---

## 8) Reflection metadata design (minimal first cut)

### Source of truth now
- Derive reflection-like metadata from Stri-V lowerer stage IO model (`StageIoLayout`) and parsed effect blocks (`SdslEffectBlock`).

### Include now
- VS input fields: name/type/semantic/index.
- VS output fields: name/type/semantic/index.
- PS input fields: name/type/semantic/index (post-pruning).
- PS output return semantic (currently `SV_Target` mapping for `float4`).
- Captured effect raw metadata:
  - `using params` entries as textual parameter key containers.
  - `mixin` invocations as raw textual composition requests.

### Defer now
- Accurate bound resource lists, descriptor sets, cbuffer layouts, register spaces from compiled binaries.
- Full SPIR-V reflection extraction.

### Diagnostic guardrails
- Manifest should include explicit reflection coverage marker and missing categories, not silently claim completeness.

---

## 9) Diagnostics model

Each diagnostic record in artifact manifest should include:
- `code` (e.g., `SD400`)
- `severity` (`info|warning|error`)
- `message`
- `sourcePath` + span (`line`,`column`,`length` when known)
- `phase` (`parse|lower|specialize|compile|artifact`)
- `stage` (`vertex|pixel|none`)
- `fatal` (bool; whether it blocked binary emission)

### Current code families to preserve
- Base/merge/specialization and parse/lower codes in use, including:
  - `SD200/SD201`
  - `SD300+` (including `SD310`, `SD311`, `SD312`, `SD313`, `SD314`, `SD315`, `SD316`, `SD320`, `SD321`, `SD322`, `SD323`)
  - stream/IO analysis (`SD330`, `SD331`, `SD340`, `SD341`)
  - effect-block parse markers (`SD400`, `SD401`, `SD402`)

---

## 10) File layout proposal

Recommended deterministic output layout:

```text
artifacts/shaders/<artifact-key>/
  manifest.json
  generated/
    vertex.hlsl
    pixel.hlsl
  bin/
    vertex.spv
    pixel.spv
  logs/
    dxc.vertex.txt
    dxc.pixel.txt
```

Where `<artifact-key>` is stable and filesystem-safe, e.g.:
`<entry-shader>__<specialization-key-hash>__<source-hash-short>__<backend>`.

---

## 11) First implementation slice proposal (M4t)

1. Add artifact model classes in `StriV.ShaderPipeline` only (no runtime deps).
2. Add deterministic manifest writer (fixed key order, invariant culture, UTF-8 no BOM).
3. Wire existing lowered SpriteBatch specialized-false path into artifact emission.
4. Emit generated stage HLSL files under `generated/`.
5. If DXC available, compile and write `.spv` stage binaries + logs.
6. Emit manifest regardless; mark compile stage diagnostics if DXC unavailable/fails.
7. Add tests asserting:
   - manifest exists,
   - deterministic JSON ordering/content for same input,
   - stage entries exist for VS/PS,
   - binary files exist when DXC available,
   - diagnostics are serialized and preserve codes.

No runtime integration, no asset pipeline integration.

---

## 12) Runtime integration preview (future, not implemented)

Future runtime flow can be:
1. Load `manifest.json`.
2. Select backend-compatible stage binaries.
3. Load SPIR-V blobs and create shader modules.
4. Build vertex input layout from artifact reflection section.
5. Bind resources via future parameter/resource reflection once implemented.

---

## 13) Asset pipeline preview (future, not implemented)

Future asset flow can be:
1. Human-authored TOML shader asset points to SDSL/HLSL source + effect/shader selection + specialization values.
2. Asset compiler invokes Stri-V shader pipeline.
3. Compiler emits machine artifact folder + manifest JSON.
4. Later codegen emits typed C# handles/IDs for safe runtime lookup.

---

## 14) Risk register

1. **Artifact format churn**: early fields may move; mitigate via explicit `format`/`artifactVersion`.
2. **Premature legacy compatibility pressure**: avoid coupling to old `EffectBytecode` binary format.
3. **Insufficient reflection**: v1 may under-serve runtime; mitigate by explicit coverage markers.
4. **DXC nondeterminism**: version/options/platform differences; capture compiler version/options and hash outputs.
5. **Generated HLSL instability**: innocuous formatting drift breaks cache; enforce canonical emission.
6. **Specialization key instability**: ordering/type formatting drift; enforce canonical serializer.
7. **Source path portability**: absolute-path leakage; force relative normalized paths.
8. **Runtime assumption creep**: keep manifest generic and avoid descriptor binding commitments until reflection matures.

---

## 15) Recommended M4t implementation prompt

> Implement the first Stri-V shader artifact emitter prototype in `striv/projects/StriV.ShaderPipeline` with no runtime or asset-pipeline integration. Add model classes for `striv.shader.artifact.v1`, deterministic JSON serialization with stable key ordering, and artifact directory emission for the current SpriteBatch specialization path (`TSRgb=false`). Emit `manifest.json`, generated `vertex.hlsl` + `pixel.hlsl`, optional DXC `vertex.spv` + `pixel.spv` when `dxc` is available, and compile logs. Populate reflection with current lowerer-derived stage IO (`VSInput/VSOutput/PSInput/PSOutput`) and effect parse raw metadata (`using params`, `mixin`) where available. Persist all parse/lower/specialization/compile diagnostics with phase/stage/fatal classification. Add tests in `striv/tests/StriV.ShaderPipeline.Tests` that verify manifest existence, deterministic serialization, stage entries, conditional binary output, and diagnostics preservation. Do not implement runtime loading, asset manifests, full effect composition, or parameter binding.

