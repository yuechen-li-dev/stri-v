# 670 — DXC Vulkan SDK Provisioning Validation

## 1) Files changed
- `docs/stri-v/audits/670-dxc-vulkan-sdk-provisioning-validation.md` (this report only).

## 2) Environment
- OS probe command:
  - `. /etc/os-release`
  - `echo "$ID $VERSION_ID $VERSION_CODENAME"`
- Result: `ubuntu 24.04 noble`.
- LunarG repo add attempt:
  - `wget -qO- https://packages.lunarg.com/lunarg-signing-key-pub.asc | tee /etc/apt/trusted.gpg.d/lunarg.asc >/dev/null`
  - `wget -qO /etc/apt/sources.list.d/lunarg-vulkan-noble.list http://packages.lunarg.com/vulkan/lunarg-vulkan-noble.list`
- Outcome: repository fetches through proxy were blocked (HTTP 403 Forbidden / proxy tunneling forbidden), so repo provisioning could not be freshly validated from network in this sandbox.
- Repo list file path used: `/etc/apt/sources.list.d/lunarg-vulkan-noble.list` (present but currently 0 bytes).

## 3) Package install results
- Ran:
  - `apt-get update`
  - `apt-get install -y wget ca-certificates gnupg`
  - `apt-get update`
  - `apt-get install -y vulkan-sdk`
- Results:
  - prerequisite packages were already installed.
  - `apt-get update` emitted warnings for blocked external repos via proxy (including `https://packages.lunarg.com/vulkan` and `https://mise.jdx.dev/deb`) with `HTTP/1.1 403 Forbidden`.
  - `vulkan-sdk` install command succeeded and reported:
    - `vulkan-sdk is already the newest version (1.4.313.0~rc3-1lunarg24.04-1).`
- No SDK binaries were vendored into the repository.

## 4) DXC verification
Commands run:
- `command -v dxc || true`
- `dxc --help | head -n 40 || true`
- `dxc --help | grep -i -- "-spirv" | head -n 10 || true`
- `./striv/build/striv-probe-dxc.sh`
- `./striv/build/striv-probe-dxc.sh --require`

Results:
- `dxc` path: `/usr/bin/dxc`
- `dxc --help`: succeeded.
- `-spirv` support: present (`-spirv Generate SPIR-V code`).
- `striv-probe-dxc.sh`: passed, including tiny HLSL→SPIR-V compile smoke.
- `striv-probe-dxc.sh --require`: passed.

## 5) Shader pipeline test results
- Command:
  - `dotnet test striv/tests/StriV.ShaderPipeline.Tests/StriV.ShaderPipeline.Tests.csproj`
- Exit code: `0`
- Result summary: `Passed! - Failed: 0, Passed: 7, Skipped: 0, Total: 7`.
- DXC optional tests behavior: with DXC present, no skips were reported in this suite output.
- First meaningful warning/error: none from this test invocation (the command succeeded without test failures).
- Final status: pass.

## 6) Clean graph validation
- Clean graph tests:
  - Command: `dotnet test striv/tests/StriV.CleanGraph.Tests/StriV.CleanGraph.Tests.csproj --no-build -v minimal`
  - Exit code: `0`
  - Summary: `Passed! - Failed: 0, Passed: 4, Skipped: 0, Total: 4`.
- Clean graph/core build:
  - Command: `./striv/build/striv-build-core.sh`
  - Exit code: `0`
  - Result: successful build completion.
- Optional runtime smoke attempted:
  - Command: `xvfb-run -a ./striv/build/striv-run-coresmoke.sh`
  - Exit code: `0`
  - Result: runtime status `success`.

## 7) Worktree status
- Command:
  - `git status --short`
- Output after report creation should show only this report file as modified/added.

## 8) Recommended next task
DXC provisioning is functionally working in this sandbox (`dxc` present with `-spirv`, probe passes, shader pipeline tests pass), so next M4 task recommendation:
- **Add SpriteBatchShader fixture parse/lower as next M4 task.**

## Notes / constraints adherence
- No DXC binaries were vendored.
- No downloaded SDK artifacts were committed.
- DXC remains optional in test design; this task only validated availability in environment.
- No shader pipeline code changes were made.
