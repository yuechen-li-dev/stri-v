# Stri-V Core M1a Filter Validation

## 1) Files changed

Created/modified files in this task:

- `build/StriV.Core.M1a.slnf` (created)
- `docs/stri-v/audits/050-striv-core-m1a-filter-validation.md` (created)

No other repository files were modified.

## 2) Solution filter contents

- Filter file: `build/StriV.Core.M1a.slnf`
- Base solution path: `Stride.sln`
- Included projects (exact):
  1. `sources/core/Stride.Core/Stride.Core.csproj`
  2. `sources/core/Stride.Core.Mathematics/Stride.Core.Mathematics.csproj`
  3. `sources/core/Stride.Core.IO/Stride.Core.IO.csproj`
  4. `sources/core/Stride.Core.MicroThreading/Stride.Core.MicroThreading.csproj`
  5. `sources/core/Stride.Core.Serialization/Stride.Core.Serialization.csproj`
  6. `sources/core/Stride.Core.Reflection/Stride.Core.Reflection.csproj`

Confirmation: no editor/assets/presentation/tools/samples/mobile/graphics/engine projects are included.

## 3) Environment / baseline

- OS: Linux x86_64 (`Ubuntu 24.04.3 LTS`)
- Current UTC date at execution: 2026-05-03
- `dotnet --info`: .NET SDK `10.0.107`, MSBuild `18.0.11+b16286c22`, RID `ubuntu.24.04-x64`
- `git branch --show-current`: `work`
- `git log -1 --pretty=format:'%H%n%s'`:
  - `6580f8ba7c15e0dd7a59676eab5b8abcc519bbbf`
  - `Merge pull request #5 from yuechen-li-dev/codex/perform-static-feasibility-audit-for-stri-v-core`
- `git status --short` before creating the filter: clean (no output)

## 4) Restore result

Command:

```bash
dotnet restore build/StriV.Core.M1a.slnf
```

- Exit code: `0`
- Duration: not captured (attempt to use `/usr/bin/time` failed because `/usr/bin/time` was unavailable)
- First meaningful warning/error: none from restore itself
- Restore succeeded: **Yes**
- Warnings relevant to M1a: none blocking; restore completed for the six filtered projects and transitive `Stride.Core.CompilerServices`.

## 5) Build result

Command:

```bash
dotnet build build/StriV.Core.M1a.slnf -c Debug -v minimal
```

- Exit code: `1`
- Duration: `Time Elapsed 00:00:22.19`
- First meaningful error:
  - `MSB4062` from `sources/sdk/Stride.Build.Sdk/Sdk/Stride.AssemblyProcessor.targets(187,5)`
  - `AssemblyProcessorTask` could not be loaded from:
    `/tmp/Stride/AssemblyProcessor/netstandard2.0/6C5BC9DA7E5A867C953C362C3419AD3C9554DF115BAA61BAEC10ED2C3856B752/Stride.Core.AssemblyProcessor.dll`
  - Failure detail: `Bad IL format`
- Error category: **expected AssemblyProcessor blocker**
- Likely Linux-specific: **uncertain** (same IL-load symptom seen in prior Linux-oriented canaries, but this report does not prove OS exclusivity)
- Narrower than prior `build/Stride.Runtime.slnf` failures: **Yes** (this is a minimal six-project core slice and still hits AssemblyProcessor early)

## 6) AssemblyProcessor observations

- First project that triggers the blocker: `sources/core/Stride.Core/Stride.Core.csproj` (`TargetFramework=net10.0` in log context)
- Target file and line: `sources/sdk/Stride.Build.Sdk/Sdk/Stride.AssemblyProcessor.targets(187,5)`
- Processor path loaded:
  - `/tmp/Stride/AssemblyProcessor/netstandard2.0/6C5BC9DA7E5A867C953C362C3419AD3C9554DF115BAA61BAEC10ED2C3856B752/Stride.Core.AssemblyProcessor.dll`
- Failure signature: still `Bad IL format` (same class of failure), not a new error type.

No fixes were applied, per task constraints.

## 7) Worktree status after validation

Command:

```bash
git status --short
```

Output:

- `?? build/StriV.Core.M1a.slnf`
- `?? docs/stri-v/audits/050-striv-core-m1a-filter-validation.md`

Tracked files were not modified by restore/build in this run.

## 8) M1a verdict

| Candidate                   | Verdict            | Current blocker                     | Next action |
| --------------------------- | ------------------ | ----------------------------------- | ----------- |
| `build/StriV.Core.M1a.slnf` | Adopt after repair | AssemblyProcessor task load (`MSB4062`, `Bad IL format`) | Perform narrowly scoped AssemblyProcessor repair audit/implementation for this M1a slice |

## 9) Recommended next task

Build failed on AssemblyProcessor with `Bad IL format`, so the recommended next task is:

- **Narrowly scoped AssemblyProcessor repair audit/implementation for the M1a slice**.
