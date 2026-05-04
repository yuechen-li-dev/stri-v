# 830 - StriV Asset Pipeline M5b Shader Manifest Validation

## 1) Files changed
Implemented new `StriV.AssetPipeline` project, test project, fixture assets, solution wiring, and central package update.

## 2) Project design
- New project: `striv/projects/StriV.AssetPipeline`
- Depends on `Tomlyn` and `StriV.ShaderPipeline`.
- No runtime/editor integration added.

## 3) TOML schema implemented
- `[[shader]]`
- `[[shader.specialization]]`
- `[[shader.effect]]`
- Flat record model only (no table-per-id).

## 4) Validation rules
- Implemented: AM100, AM200, AM201, AM202, AM203, AM204, AM205, AM206, AM207.
- Deferred: AM101 strict unknown top-level/field rejection (Tomlyn model parser currently permissive).

## 5) Pipeline behavior
- Resolves source paths relative to manifest directory.
- Reads shader source text.
- Builds bool specialization map per shader ID.
- Invokes `ShaderArtifactEmitter` and writes `manifest.json`.
- Output layout: `<output>/shaders/<shader-id>/{generated,bin,logs,manifest.json}`.

## 6) Tests
Added parse/validate/build and diagnostic coverage tests for duplicate IDs, missing fields, paths, backend/profile, references, duplicate specs, type mismatch, and effect parsing.

## 7) DXC behavior
Tests run with non-strict DXC mode; build remains green if DXC is unavailable. HLSL artifact assertions are mandatory.

## 8) Validation results
- `dotnet test striv/tests/StriV.AssetPipeline.Tests/StriV.AssetPipeline.Tests.csproj`
  - Exit code: 0
  - First meaningful warning/error: none
  - Pass/fail: pass
  - Output truncated: no
- `dotnet test striv/tests/StriV.ShaderPipeline.Tests/StriV.ShaderPipeline.Tests.csproj --no-build -v minimal`
  - Exit code: 0
  - First meaningful warning/error: none emitted
  - Pass/fail: pass
  - Output truncated: no
- `dotnet test striv/tests/StriV.CleanGraph.Tests/StriV.CleanGraph.Tests.csproj --no-build -v minimal`
  - Exit code: 0
  - First meaningful warning/error: none emitted
  - Pass/fail: pass
  - Output truncated: no
- `./striv/build/striv-build-core.sh`
  - Exit code: 0
  - First meaningful warning/error: existing upstream warnings (e.g. CS1030 in `ObjectIdBuilder.cs`)
  - Pass/fail: pass
  - Output truncated: yes (tool output limit)

## 9) Limitations
- Shader assets only.
- Bool specialization only.
- No materials/textures/scenes/models/audio.
- No runtime loading.
- No editor integration.
- No incremental cache.
- No TOML writing.
- No cross-asset graph beyond shader references.

## 10) Recommended next task
Add a CLI wrapper for asset pipeline execution and structured diagnostic output formatting.
