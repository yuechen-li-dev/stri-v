# 090 - Stri-V Core bootstrap build validation

## 1) Files changed

- `build/striv-build-core-m1a.sh`
- `build/striv-build-core-m1a.ps1`
- `docs/stri-v/building-core.md`
- `docs/stri-v/audits/090-striv-core-bootstrap-build-validation.md`

## 2) Script design

### Repo root detection

- Linux script derives `SCRIPT_DIR` from `${BASH_SOURCE[0]}` and sets repo root to `SCRIPT_DIR/..`.
- PowerShell script derives `$ScriptDir` from `$MyInvocation.MyCommand.Path` and resolves `..` as repo root.
- This supports invocation from repo root or any other current working directory.

### Configuration handling

- Linux script: first positional argument defaults to `Debug`; accepts only `Debug` or `Release`; remaining args are forwarded to M1a `dotnet build`.
- PowerShell script: `-Configuration` parameter with `ValidateSet(Debug, Release)`, default `Debug`; remaining args are forwarded to M1a `dotnet build`.

### AssemblyProcessor bootstrap build

Both scripts build:

- `sources/core/Stride.Core.AssemblyProcessor/Stride.Core.AssemblyProcessor.csproj`

using:

- `dotnet build ... -c <Configuration> -v minimal`

### AssemblyProcessor output validation

Both scripts validate `sources/core/Stride.Core.AssemblyProcessor/bin/<Configuration>/net10.0/Stride.Core.AssemblyProcessor.dll` by checking:

1. file exists,
2. file size > 1024 bytes,
3. first bytes do not match Git LFS pointer prefix (`version https://git-lfs.github`),
4. first two bytes are `MZ`.

If any check fails, scripts print explicit error text and exit nonzero.

### M1a build invocation

Both scripts build:

- `build/StriV.Core.M1a.slnf`

with:

- `-p:StrideAssemblyProcessorFramework=net10.0`
- `-p:StrideAssemblyProcessorBasePath=<absolute AP output dir with trailing slash>`
- `-p:StrideAssemblyProcessorHash=sourcebuild`

and `-c <Configuration> -v minimal`.

## 3) Validation results

### Command 1

- Exact command: `./build/striv-build-core-m1a.sh`
- Exit code: `0`
- First meaningful warning/error: `warning CS1030: #warning: 'PERF: Do not copy byte-for-byte.'`
- Pass/fail: **PASS**
- Notes: AssemblyProcessor bootstrap + M1a filter build both completed; warnings present but no errors.

### Command 2

- Exact command: `./build/striv-build-core-m1a.sh Release`
- Exit code: `0`
- First meaningful warning/error: `warning CS1030: #warning: 'PERF: Do not copy byte-for-byte.'`
- Pass/fail: **PASS**
- Notes: Release bootstrap + M1a filter build completed; warnings present but no errors.

### Command 3

- Exact command: `pwsh ./build/striv-build-core-m1a.ps1`
- Exit code: `127`
- First meaningful warning/error: `/bin/bash: line 1: pwsh: command not found`
- Pass/fail: **FAIL (environment limitation)**
- Notes: PowerShell is not installed in this Linux sandbox. Validate on Windows (or Linux with PowerShell Core installed) by running both Debug and Release invocations.

## 4) AssemblyProcessor payload validation evidence

- Output path: `/workspace/stri-v/sources/core/Stride.Core.AssemblyProcessor/bin/Debug/net10.0/Stride.Core.AssemblyProcessor.dll`
- File size: `157696` bytes
- First bytes: `4d 5a 90 00 ...` (`MZ` header present)
- Validity: **VALID** (not LFS pointer, size above threshold, PE header present)

## 5) Worktree status

Command run:

```bash
git status --short
```

Output at report time:

```text
?? build/striv-build-core-m1a.ps1
?? build/striv-build-core-m1a.sh
?? docs/stri-v/audits/090-striv-core-bootstrap-build-validation.md
?? docs/stri-v/building-core.md
```

## 6) Remaining limitations

- This establishes only the foundational core bootstrap path (M1a), not full engine runtime capability.
- No validation of windowing, rendering, input, audio, physics, asset compiler, or editor.
- Windows script behavior is prepared but not executed in this sandbox due to missing `pwsh`.

## 7) Next recommended task

Scripts are functioning for Linux bootstrap builds, so the next recommended task is:

- **M1c-prep: smallest engine foundation slice feasibility**.
