# Stri-V M1 Golden-Path Summary

## 1) Executive summary

M1 is closed out: Stri-V now has an extracted, editorless runtime spine that compiles on Linux (Debug/Release) and runs a tiny smoke executable under Xvfb + Mesa llvmpipe Vulkan, then exits cleanly.

In plain terms, Stri-V proved the minimum viable runtime path can:
- build without Game Studio/editor terrain,
- launch through SDL/X11/display in sandbox,
- clear Vulkan loader/ICD/device startup,
- avoid legacy shader-source compiler/audio/VR blockers,
- and terminate cleanly.

## 2) Reproduction commands

Debug:

```bash
./build/striv-build-coresmoke-m1g.sh
xvfb-run -a ./build/striv-run-coresmoke-m1h.sh
```

Release:

```bash
./build/striv-build-coresmoke-m1g.sh Release
xvfb-run -a ./build/striv-run-coresmoke-m1h.sh Release
```

## 3) Validated project spine

Golden-path project groups:

- **Core**: foundational runtime and utilities (`Stride.Core`, mathematics, IO, microthreading, serialization, reflection).
- **Engine/runtime**: base runtime path (`Stride`, `Stride.Games`, `Stride.Graphics`, `Stride.Input`, `Stride.Engine`).
- **Physics**: `Stride.BepuPhysics` (modern physics direction).
- **Smoke executable**: `StriV.CoreSmoke` (minimal runtime proof executable).
- **Bootstrap tooling**: `Stride.Core.AssemblyProcessor` source-built and routed as authoritative processor input.

Forward-looking organization for this spine now lives in `build/StriV.Core.slnx`.

## 4) Intentional exclusions

- **Shader compiler / CppNet / SDSL source compiler**
  - Excluded via `StrideIncludeShaderCompiler=false`.
  - Why: remove legacy source-compiler burden from M1 runtime proof.
  - TODO: design Stri-V shader pipeline (HLSL + SDSL-inspired extension -> SPIR-V/DXIL) and quarantine/remove CppNet dependency.

- **Audio / Celt / OpenAL / NativePath stack**
  - Excluded via `StrideIncludeAudio=false`.
  - Why: keep native audio payload risks out of core runtime proof.
  - TODO: evaluate modern backend options (OwnAudioSharp/miniaudio/system Opus/OpenAL), keep legacy Celt path quarantined.

- **VirtualReality / OpenVR / OpenXR / NativePath stack**
  - Excluded via `StrideIncludeVirtualReality=false`.
  - Why: avoid native VR startup and dependency risks during core extraction.
  - TODO: reintroduce as optional module after core tracks stabilize.

- **Old `Stride.Physics`**
  - Excluded from golden path.
  - Why: Stri-V physics direction is Bepu-based.
  - TODO: keep old physics legacy-only unless a compatibility wrapper is explicitly needed.

- **Editor/Game Studio/presentation/Quantum**
  - Excluded from M1 scope.
  - Why: M1 target is editorless runtime spine, not authoring stack.
  - TODO: define independent Stri-V authoring/runtime boundaries later.

- **Legacy source asset compiler/YAML pipeline**
  - Excluded from M1 scope.
  - Why: avoid legacy asset/source pipeline coupling during runtime extraction.
  - TODO: replace with Stri-V manifest/content track (TOML + deterministic IDs).

- **Bepu companion modules/samples/tests**
  - Excluded from core golden path (`Debug`, `Navigation`, `Soft`, `2D`, tests, extra samples).
  - Why: constrain M1 to minimal runtime + core Bepu module.
  - TODO: admit incrementally as optional expansions.

- **Old full Stride solution/build workflows**
  - Remain present but treated as legacy terrain for Stri-V Core.
  - Why: M1 focuses on narrow, reproducible extraction workflows.
  - TODO: add small Stri-V-first CI/workflow equivalents around the new spine.

## 5) Current scripts/artifacts

Primary M1 closeout artifacts:
- `build/striv-build-coresmoke-m1g.sh`
- `build/striv-run-coresmoke-m1h.sh`
- `build/StriV.Core.slnx`

Historical extraction scripts (still useful for staged provenance):
- `build/striv-build-core-m1a.sh`
- `build/striv-build-engine-foundation-m1b.sh`
- `build/striv-build-platform-graphics-basics-m1c.sh`
- `build/striv-build-input-m1d.sh`
- `build/striv-build-engine-m1e.sh`
- `build/striv-build-engine-bepu-m1f.sh`
- `build/striv-build-coresmoke-m1g.sh`
- `build/striv-run-coresmoke-m1h.sh`

## 6) Known limitations

- CoreSmoke is intentionally tiny.
- Runtime rendering content is not validated.
- Asset loading/content pipeline behavior is not validated.
- Shader source compilation is not validated.
- Audio is not validated.
- VR is not validated.
- No real scene/editor/content authoring path is validated.
- Xvfb runtime success uses Mesa llvmpipe CPU Vulkan in sandbox.
- Native/local GPU validation is still valuable and recommended.

## 7) Next expansion priorities

1. **Shader pipeline track**: Stri-V shader pipeline for HLSL + SDSL-inspired extensions to SPIR-V/DXIL; CppNet removal/quarantine plan.
2. **Asset pipeline track**: low-nesting TOML manifest, deterministic IDs, and no Game Studio/Quantum dependency.
3. **Audio module track**: modern backend evaluation; keep legacy Celt path quarantined.
4. **Build/CI organization track**: `.slnx`-centered tiny Stri-V workflows replacing heavy legacy paths for core validation.
5. **Optional module expansion**: add runtime modules only after golden-path stability and reproducible checks.

## 8) Doctrine notes

- Stri-V Core is allowed to be temporarily incomplete.
- Exclusions must be explicit, documented, default-preserving, and reversible.
- Legacy Stride behavior remains default where practical unless Stri-V scripts opt out.
- Stri-V Core prioritizes a clean runtime spine over full legacy feature compatibility.
