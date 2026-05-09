# 1960 — M20b Build/Gate Repair

## 1) Files changed
- `docs/stri-v/audits/1000+/1960-m20b-build-gate-repair.md`

## 2) Task scope
Build repair only. No feature additions, no runtime behavior changes, no warning suppression, no orchestrator redesign.

## 3) Failure reproduction
### Command: solution build (requested failing repro)
- Command:
  - `dotnet build striv/StriV.Core.slnx -c Debug -v minimal 2>&1 | tee /tmp/striv-m20b-slnx-failure.log`
- Exit code: `0`
- Result: Passed in this environment (no `CS0006`, no metadata-file-not-found errors).
- Evidence query:
  - `grep -n "CS0006\|metadata file\|error " /tmp/striv-m20b-slnx-failure.log | head -n 80`
  - Output: empty.

### Command: focused gate batch
- Command:
  - `./striv/build/striv-check-focused-projects.sh Stride.BepuPhysics Stride.Core.Mathematics Stride.Core.IO Stride.Input Stride.Games Stride.Core.Reflection 2>&1 | tee /tmp/striv-m20b-focused-failure.log`
- Exit code: `0`
- Result: All listed focused projects passed.
- Evidence query:
  - `grep -n "FAILED\|error \|CS0006\|metadata file" /tmp/striv-m20b-focused-failure.log | head -n 120`
  - Output: empty.

### Captured `CS0006` metadata-file paths
- None observed in either reproduced log (`/tmp/striv-m20b-slnx-failure.log`, `/tmp/striv-m20b-focused-failure.log`).

## 4) Root cause
No currently reproducible build-graph failure was observed. Based on clean and non-clean passes, the prior M20b gate failure appears non-persistent and most consistent with transient/stale local state from the earlier failing environment rather than a present graph/config issue.

## 5) Fix applied
No code or project graph fix was required because the failing condition could not be reproduced and all target gates currently pass.

## 6) Validation results
| Command | Exit | First meaningful warning/error | Pass/Fail | Output truncated |
|---|---:|---|---|---|
| `dotnet build striv/StriV.Core.slnx -c Debug -v minimal 2>&1 | tee /tmp/striv-m20b-slnx-failure.log` | 0 | Warnings only (no errors) | Pass | Yes (console), full log in `/tmp` |
| `grep -n "CS0006\|metadata file\|error " /tmp/striv-m20b-slnx-failure.log | head -n 80` | 0 | No matches | Pass | No |
| `./striv/build/striv-check-focused-projects.sh Stride.BepuPhysics Stride.Core.Mathematics Stride.Core.IO Stride.Input Stride.Games Stride.Core.Reflection 2>&1 | tee /tmp/striv-m20b-focused-failure.log` | 0 | No project failures | Pass | No |
| `grep -n "FAILED\|error \|CS0006\|metadata file" /tmp/striv-m20b-focused-failure.log | head -n 120` | 0 | No matches | Pass | No |
| `grep -n "StriV.Engine.Dominatus.Runtime\|StriV.Engine.Dominatus.Adapters\|StriV.Engine.Dominatus.Tests" striv/StriV.Core.slnx || true` | 0 | N/A | Pass | No |
| `dotnet clean striv/StriV.Core.slnx -c Debug -v minimal` | 0 | No errors | Pass | No |
| `find striv/projects striv/tests striv/external -type d \( -name bin -o -name obj \) -prune -print | wc -l` | 0 | N/A (`76`) | Pass | No |
| `dotnet build striv/StriV.Core.slnx -c Debug -v minimal 2>&1 | tee /tmp/striv-m20b-slnx-after-clean.log` | 0 | Warnings only (no errors) | Pass | Yes (console), full log in `/tmp` |
| `grep -n "CS0006\|metadata file\|error " /tmp/striv-m20b-slnx-after-clean.log | head -n 80` | 0 | No matches | Pass | No |
| `dotnet build striv/projects/StriV.Engine.Dominatus.Runtime/StriV.Engine.Dominatus.Runtime.csproj -c Debug -v minimal` | 0 | No errors | Pass | No |
| `dotnet test striv/tests/StriV.Engine.Dominatus.Tests/StriV.Engine.Dominatus.Tests.csproj -v minimal` | 0 | No errors | Pass | No |
| `dotnet build striv/StriV.Core.slnx -c Debug -v minimal 2>&1 | tee /tmp/striv-m20b-slnx-fixed.log` | 0 | Warnings only (no errors) | Pass | Yes (console), full log in `/tmp` |
| `./striv/build/striv-check-focused-projects.sh Stride.BepuPhysics Stride.Core.Mathematics Stride.Core.IO Stride.Input Stride.Games Stride.Core.Reflection` | 0 | No project failures | Pass | No |

## 7) Recommendation
Fixed state (currently green in this environment): resume M20c work.

If the intermittent failure reappears in CI or another machine, next triage command should be:
- `dotnet build striv/StriV.Core.slnx -c Debug -v diag 2>&1 | tee /tmp/striv-m20b-slnx-diag.log`
followed by:
- `grep -n "CS0006\|metadata file\|error " /tmp/striv-m20b-slnx-diag.log | head -n 200`
