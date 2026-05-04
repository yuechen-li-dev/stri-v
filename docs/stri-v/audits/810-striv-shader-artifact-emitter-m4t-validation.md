# Stri-V M4t Shader Artifact Emitter Validation

## 1. Files changed
- `striv/projects/StriV.ShaderPipeline/Artifacts/ShaderArtifactManifest.cs`
- `striv/projects/StriV.ShaderPipeline/Artifacts/ShaderArtifactOptions.cs`
- `striv/projects/StriV.ShaderPipeline/Artifacts/ShaderArtifactJsonWriter.cs`
- `striv/projects/StriV.ShaderPipeline/Artifacts/ShaderArtifactEmitter.cs`
- `striv/projects/StriV.ShaderPipeline/Lowering/ShaderLowerer.cs`
- `striv/tests/StriV.ShaderPipeline.Tests/ShaderArtifactEmitterTests.cs`

## 2. Manifest schema
JSON manifest uses scalar top-level fields and flat arrays (`stages`, `specializations`, `io`, `effectUsingParams`, `effectMixins`, `diagnostics`) to preserve TOML-friendly mapping.

## 3. Emitter behavior
Uses SpriteBase+SpriteBatch combined fixture source, lowers `SpriteBatchShader` with `TSRgb=false`, emits whole lowered TU into both stage HLSL files, optionally invokes `dxc` for SPIR-V, and writes canonical output layout.

## 4. Determinism
Stable JSON property order via dedicated writer, ordered array emission, normalized `/` separators, relative manifest paths, normalized source hashing (`\n` line endings), binary byte hashing, deterministic manifest equality asserted in repeated-run test.

## 5. Reflection/IO metadata
Emits `io` records from lowerer `StageIoLayout` (VS in/out, PS in/out). SPIR-V reflection is deferred.

## 6. Diagnostics
Diagnostic records include code/severity/phase/stage/message/sourcePath/line/column/length/fatal. DXC missing/failure is emitted as compile warning unless strict mode is requested.

## 7. Tests
Added artifact emitter coverage for write-path, determinism, flat-shape schema, DXC conditional binary emission, and diagnostics inclusion.

## 8. Validation results
- `dotnet test striv/tests/StriV.ShaderPipeline.Tests/StriV.ShaderPipeline.Tests.csproj`
  - exit code: 0
  - first meaningful warning/error: none (1 existing skipped test: `StreamLiveness_DoesNotPruneWhenAccessUnknown`)
  - pass/fail: pass
  - output truncated: no
- `dotnet test striv/tests/StriV.CleanGraph.Tests/StriV.CleanGraph.Tests.csproj --no-build -v minimal`
  - exit code: 0
  - first meaningful warning/error: none
  - pass/fail: pass
  - output truncated: no
- `./striv/build/striv-build-core.sh`
  - exit code: 0
  - first meaningful warning/error: existing repository warnings (nullable/compat warnings in legacy projects)
  - pass/fail: pass
  - output truncated: yes (tool output token limit)

## 9. Limitations
No runtime integration, no asset pipeline integration, no SPIR-V reflection, HLSL currently whole TU per stage file, no full effect composition, no EffectBytecode compatibility.

## 10. Recommended next task
Start asset TOML manifest prep.
