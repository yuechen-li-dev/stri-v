# 650 - StriV Shader Pipeline M4d DXC Smoke Validation

## 1) Files changed
- `striv/tests/StriV.ShaderPipeline.Tests/ShaderPipelineTests.cs`
- `striv/tests/StriV.ShaderPipeline.Tests/DxcTestProbe.cs`
- `docs/stri-v/audits/650-striv-shader-pipeline-m4d-dxc-smoke-validation.md`

## 2) DXC detection design
- Added `DxcTestProbe` test helper to detect `dxc` on `PATH` by manually scanning path entries and candidate executable names (`dxc` / `dxc.exe` on Windows).
- If no executable is found, probe returns `IsAvailable=false` with reason `dxc was not found on PATH.`
- If an executable is found, probe runs `dxc --help` to verify it can execute successfully.
- `-spirv` support is inferred by searching `--help` output for `-spirv`.
- Absence/unavailability does not fail tests: DXC smoke tests print a clear skip reason and return early.

## 3) Compile smoke design
- Lowered fixture used: `fixtures/shaders/sdsl/simple_stream_shader.sdsl`.
- Tests write lowered/generated HLSL into a unique temp directory under `Path.GetTempPath()/striv-shader-pipeline/<guid>/`.
- VS/PS commands are executed through `dxc` with:
  - SPIR-V preferred: `-T vs_6_0 -E VSMain -spirv ... -Fo ...` and `-T ps_6_0 -E PSMain -spirv ... -Fo ...`
  - DXIL fallback behavior: when probe reports no `-spirv` support, commands are run without `-spirv` and outputs are `.dxil`.
- Tests assert:
  - compile exit code is zero,
  - output artifacts exist and are non-empty.
- Tool absence never fails the suite; tests are environment-aware and early-return with log output.

## 4) Lowering changes
- No lowerer changes were required for this M4d slice.
- M4c lowered model remained unchanged and this task stayed within the existing simple subset.

## 5) Test results
1. Command: `dotnet test striv/tests/StriV.ShaderPipeline.Tests/StriV.ShaderPipeline.Tests.csproj`
   - Exit code: `0`
   - First meaningful warning/error: none
   - Pass/fail: **PASS**
   - DXC-specific tests: executed in environment-aware mode; no failure due to missing DXC
   - Output truncated: **No**

2. Command: `dotnet test striv/tests/StriV.CleanGraph.Tests/StriV.CleanGraph.Tests.csproj --no-build -v minimal`
   - Exit code: `0`
   - First meaningful warning/error: none in captured output
   - Pass/fail: **PASS**
   - Output truncated: **No**

3. Command: `./striv/build/striv-build-core.sh`
   - Exit code: `0`
   - First meaningful warning/error: existing repository warnings (e.g., nullable/reference warnings in core/engine projects)
   - Pass/fail: **PASS**
   - Output truncated: **Yes** (very large warning output)

4. Command: `command -v dxc || true`
   - Exit code: `0`
   - First meaningful warning/error: no path output for `dxc`
   - Pass/fail: **PASS** (probe command completed)
   - Output truncated: **No**

5. Command: `dxc --help | head -n 20 || true`
   - Exit code: `0`
   - First meaningful warning/error: `/bin/bash: line 1: dxc: command not found`
   - Pass/fail: **PASS** (expected in missing-tool environment; command guarded)
   - Output truncated: **No**

## 6) DXC results
- DXC was **absent** in this environment (`dxc: command not found`).
- Result: DXC compile-smoke tests are skipped at runtime by design (early return with reason), not treated as failures.

## 7) Limitations
- No runtime integration.
- No shader artifact format yet.
- No reflection metadata yet.
- No full SDSL support.
- No mixin/base/clone/partial effect support.
- No guaranteed semantic equivalence against legacy pipeline.

## 8) Recommended next task
- **Add DXC provisioning strategy**:
  - define optional CI/local provisioning path for `dxc`,
  - keep smoke tests optional,
  - enable deterministic SPIR-V compile coverage in at least one validation environment.
