# 820 — Stri-V TOML Asset Manifest M5a Plan

## 1. Evidence collection

### Files inspected
- `striv/projects/StriV.ShaderPipeline/Artifacts/ShaderArtifactOptions.cs`
- `striv/projects/StriV.ShaderPipeline/Artifacts/ShaderArtifactEmitter.cs`
- `striv/projects/StriV.ShaderPipeline/Artifacts/ShaderArtifactManifest.cs`
- `striv/projects/StriV.ShaderPipeline/Artifacts/ShaderArtifactJsonWriter.cs`
- `striv/tests/StriV.ShaderPipeline.Tests/ShaderArtifactEmitterTests.cs`
- `striv/tests/fixtures/shaders/sdsl/sprite/SpriteBatchShader.sdsl`
- `striv/tests/fixtures/shaders/sdsl/sprite/SpriteAlphaCutoff.sdsl`
- `docs/stri-v/audits/800-striv-shader-artifact-model-plan.md`
- `docs/stri-v/audits/810-striv-shader-artifact-emitter-m4t-validation.md`

### Searches run
- `rg -n "ShaderArtifact|manifest.json|specialization|SpriteBatchShader|TSRgb|EffectBlock|effectUsingParams|effectMixins|diagnostics|generatedHlslPath|binaryPath" striv docs/stri-v`
- `find . -name '*.sd*' -o -name '*.yml' -o -name '*.yaml' | head -n 100`

### Old asset formats glanced at for contrast (not inheritance)
- `.sdpkg`, `.sdscene`, `.sdmat`, `.sdfx`, `.sdtex`, `.sdgamesettings` files under `sources/engine/**` (inventory-only contrast check).
- Confirmed they are present and numerous; this plan explicitly avoids reusing those schemas.

### Evidence summary
- M4 artifact output is machine-authored JSON (`manifest.json`) with deterministic property order and flat record arrays (`stages`, `specializations`, `io`, `effectUsingParams`, `effectMixins`, `diagnostics`).
- Current emitter inputs are effectively: source path/text, entry shader, bool specialization map, and output root.
- DXC path is optional and diagnostic-backed (`DXC000` unavailable, `DXC001` failed compile).

### Uncertainty
- Final naming and shape for future non-shader assets should remain non-binding in M5a.
- Exact effect-selection surface (`[[shader.effect]]` vs inline fields) is not yet constrained by runtime requirements, so v1 should pick one simple route and allow additive extension.

---

## 2. Design doctrine

Stri-V M5a doctrine for human-authored assets:
- TOML is the human-authored source-of-intent format.
- JSON shader manifests and binary outputs are machine-authored artifacts.
- File extensions describe container format; use `.toml` (not custom per-asset extensions).
- Prefer low nesting and flat records for readability and tooling.
- Use stable textual IDs as source truth.
- Do not use generated GUIDs as authoritative source IDs.
- Do not recreate legacy Stride/Quantum/Game Studio asset authoring formats.

---

## 3. Format style decision

### Option A: table-per-id
```toml
[shader.sprite_batch]
source = "Shaders/SpriteBatchShader.sdsl"
```

### Option B: array-of-records
```toml
[[shader]]
id = "shader.sprite_batch"
source = "Shaders/SpriteBatchShader.sdsl"
```

### Recommendation (v1)
Adopt **Option B: array-of-flat-records** as the primary M5a schema style.

Rationale:
1. Matches M4 machine-manifest shape (flat arrays) and reduces conceptual mismatch.
2. Better for codegen/table tooling and schema evolution (additive fields per record).
3. Supports deterministic validation loops (iterate records, detect duplicate IDs/references).
4. Easier future expansion to `[[texture]]`, `[[material]]`, etc., while keeping uniform structure.

Non-binding note:
- Table-per-id may be optionally supported later as an ergonomic sugar layer if needed.

---

## 4. Shader asset schema v1

### Primary shader request record
```toml
[[shader]]
id = "shader.sprite_batch"
source = "Shaders/SpriteBatchShader.sdsl"
entry = "SpriteBatchShader"
backend = "vulkan"
profile = "default"
```

Field intent:
- `id`: stable manifest-local/global textual asset ID.
- `source`: source shader file path (`.sdsl` initially; HLSL may be supported later by policy).
- `entry`: shader declaration/entry selection used by the M4 lowerer/emitter.
- `backend`: target family selector (`vulkan` in v1).
- `profile`: backend profile alias (`default` for v1 canonical Vulkan path).

### Specialization record
```toml
[[shader.specialization]]
shader = "shader.sprite_batch"
name = "TSRgb"
type = "bool"
value = false
```

Policy:
- `shader` references `[[shader]].id`.
- v1 supports `type = "bool"` only.
- duplicate `(shader, name)` is a validation error.

### Effect selection record (recommended shape)
```toml
[[shader.effect]]
shader = "shader.alpha_cutoff"
name = "SpriteAlphaCutoffEffect"
namespace = "Stride.Rendering"
```

Recommendation:
- Keep effect selection in a separate flat array (`[[shader.effect]]`) rather than nesting more fields directly in `[[shader]]`.
- Rationale: preserves flat-table style and avoids inflating `[[shader]]` with optional effect-only fields.

### Output artifact path policy
- Default: compiler-generated output layout under configured artifact root.
- M5a v1 manifests should not require explicit per-shader output path.
- Future optional `output` override can be additive, but not required now.

### Diagnostics behavior
- Manifest parse/validation errors should be explicit and deterministic.
- Invalid records should not silently fallback.
- Compiler may continue per-record collection mode (report all detectable issues) before failing.

---

## 5. ID and reference policy

### ID rules
- IDs are stable text and human-selected.
- Recommended namespace-like prefixes by kind:
  - `shader.sprite_batch`
  - `texture.player_albedo`
  - `material.player`
- Allowed characters (v1): `[a-z0-9._-]`.
- Case sensitivity: **case-sensitive by policy**, with recommendation to author lowercase IDs.

### Duplicate policy
- Duplicate IDs within same kind table are errors.
- Duplicate IDs across kinds should also be errors in v1 to avoid ambiguous generic references.

### Reference syntax
Recommendation for v1: **plain typed fields** (e.g., `shader = "shader.sprite_batch"`) rather than `ref:<id>`.

Rationale:
- Simpler to read/write.
- typed field already conveys expected target kind.
- avoids string prefix protocol overhead in first schema.

---

## 6. Path policy

- Paths are resolved relative to the manifest file directory unless explicitly documented otherwise.
- Canonical stored/normalized form uses `/` separators.
- Absolute paths are disallowed for committed manifests in v1.
- Local-only absolute override (if ever added) should be CLI-only/config-only, not committed source.
- Output artifact paths are generated by compiler policy by default.

---

## 7. Backend/profile policy

- v1 supported backend: `vulkan`.
- v1 compiler default: `dxc`.
- v1 target family: SPIR-V.
- v1 profile values:
  - `default` (maps to current M4 VS/PS SPIR-V compile path).
- Future planned backends/profiles (non-binding): `d3d12` (DXIL), additional Vulkan profiles.

How this feeds M4 emitter:
- backend/profile resolve to artifact emitter compile settings and naming metadata (`BackendProfile`, `Compiler`, `TargetFamily`, stage target profiles).

---

## 8. Diagnostics and validation policy

### Recommended diagnostic code families
Use compact, manifest-scoped codes:
- `AM1xx` = manifest parse/structure
- `AM2xx` = shader asset semantic validation

Justification:
- concise and distinct from existing `SDxxx` (parser/lowering) and `DXCxxx` (compile) families.
- easy filtering in CLI and CI.

### Proposed initial diagnostics
- `AM100`: manifest parse failure.
- `AM101`: unknown top-level table/field (strict mode).
- `AM200`: duplicate asset ID.
- `AM201`: missing required field.
- `AM202`: invalid path format.
- `AM203`: unsupported backend/profile.
- `AM204`: unknown shader reference.
- `AM205`: duplicate specialization parameter for shader.
- `AM206`: unknown specialization parameter for selected shader entry.
- `AM207`: specialization type mismatch.

Validation list required by M5a:
- duplicate id,
- missing required field,
- unknown reference,
- invalid path,
- unsupported backend,
- unknown specialization parameter,
- duplicate specialization parameter,
- type mismatch for specialization value.

---

## 9. Example manifest

### Minimal current SpriteBatch request
```toml
[[shader]]
id = "shader.sprite_batch"
source = "Shaders/SpriteBatchShader.sdsl"
entry = "SpriteBatchShader"
backend = "vulkan"
profile = "default"

[[shader.specialization]]
shader = "shader.sprite_batch"
name = "TSRgb"
type = "bool"
value = false
```

### Future sketch (non-binding) for SpriteAlphaCutoff
```toml
[[shader]]
id = "shader.alpha_cutoff"
source = "Shaders/SpriteAlphaCutoff.sdsl"
entry = "SpriteAlphaCutoff"
backend = "vulkan"
profile = "default"

[[shader.effect]]
shader = "shader.alpha_cutoff"
name = "SpriteAlphaCutoffEffect"
namespace = "Stride.Rendering"

[[shader.specialization]]
shader = "shader.alpha_cutoff"
name = "TSRgb"
type = "bool"
value = true
```

---

## 10. Mapping to shader artifact emitter

M5a TOML -> existing M4 emitter mapping:
- `[[shader]].source` -> `ShaderArtifactOptions.SourcePath` + file-read into `SourceText`.
- `[[shader]].entry` -> `ShaderArtifactOptions.EntryShader`.
- `[[shader.specialization]]` -> `ShaderArtifactOptions.BoolSpecialization` (`Dictionary<string,bool>`).
- backend/profile -> `ShaderArtifactManifest.BackendProfile` policy mapping + DXC SPIR-V compile mode.
- output artifact -> machine-authored `manifest.json`, generated HLSL, optional SPIR-V binaries/logs.

Important: TOML is intent input; JSON remains derived output.

---

## 11. File layout proposal

### First asset project layout (recommended)
```text
Game/
  Assets/
    assets.toml
    Shaders/
      SpriteBatchShader.sdsl
  Artifacts/
    shaders/
      ...
```

### Test fixture layout candidate
```text
striv/tests/fixtures/assets/shader_manifest/assets.toml
```

---

## 12. Future asset kinds (non-binding sketch)

```toml
[[texture]]
id = "texture.player_albedo"
source = "Textures/player_albedo.png"
format = "srgb"

[[material]]
id = "material.player"
shader = "shader.sprite_batch"
```

Non-binding note:
- This section is directional only; schema is intentionally deferred beyond shader assets for M5a.

---

## 13. Implementation proposal for M5b

### Project placement
Recommend new standalone project:
- `striv/projects/StriV.AssetPipeline`

Rationale:
- keeps asset-manifest concerns cleanly separated from `StriV.ShaderPipeline` compiler internals.
- allows future multi-asset expansion without polluting shader-lowering project boundaries.

### TOML library recommendation
Recommend **Tomlyn** for M5b.

Rationale:
- mature TOML parsing/writing support in .NET ecosystem.
- avoids unnecessary handwritten parser complexity.
- aligns with M5a direction to avoid overbuilding.

### M5b minimal scope
1. Parse `assets.toml` shader records.
2. Validate v1 schema/IDs/references/paths/backend/profile/specializations.
3. Materialize `ShaderArtifactOptions` and call `StriV.ShaderPipeline` emitter.
4. Emit artifacts to configured root.
5. Add focused fixture-based tests for parse + validation + emitter invocation.

Explicitly out of scope for M5b:
- runtime integration,
- material/texture/scene processing,
- full effect composition semantics.

---

## 14. Risk register

1. **Schema churn risk**
   - Mitigation: versioned schema contract + additive fields + strict diagnostics.
2. **Nesting creep**
   - Mitigation: enforce flat-array authoring conventions and typed reference fields.
3. **Legacy format gravity**
   - Mitigation: keep old Stride asset files as contrast only; do not mirror shape.
4. **ID rename/migration cost**
   - Mitigation: stable textual IDs + future alias/migration table strategy.
5. **Path portability issues**
   - Mitigation: relative paths + `/` normalization + absolute-path prohibition.
6. **TOML dependency risk**
   - Mitigation: pin Tomlyn version, wrap parser behind small adapter boundary.
7. **Premature multi-asset design**
   - Mitigation: lock implementation to shader-only in M5b.
8. **Artifact/source confusion**
   - Mitigation: docs and diagnostics explicitly separate TOML source intent from machine JSON/binaries.

---

## 15. Recommended M5b implementation prompt

> Implement Stri-V M5b shader asset manifest ingestion as a new project `striv/projects/StriV.AssetPipeline` with no runtime integration. Use Tomlyn (or equivalent stable TOML library) to parse `assets.toml` using the M5a flat-array schema (`[[shader]]`, `[[shader.specialization]]`, optional `[[shader.effect]]`). Validate required fields, duplicate IDs, reference integrity, path policy, supported backend/profile, specialization duplicates/unknowns/type mismatches, and emit deterministic diagnostics under `AM1xx/AM2xx` codes. For each valid shader request, invoke `StriV.ShaderPipeline` artifact emission (source path/text, entry, bool specialization) and write machine artifacts (`manifest.json`, generated HLSL, optional SPIR-V/logs) to configured output roots. Add fixture-based tests under `striv/tests` for parse/validation success and failure cases plus emitter bridge coverage. Do not implement material/texture/scene pipelines, runtime loading, or editor integration in this milestone.
