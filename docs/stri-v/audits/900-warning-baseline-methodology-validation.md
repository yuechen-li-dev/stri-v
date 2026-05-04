# 900 Warning Baseline Methodology Validation

## A. Files changed
- `docs/stri-v/audits/900-warning-baseline-methodology-validation.md`
- `striv/build/striv-warning-baseline.sh`

## B. Problem recap
M6a reported a major warning reduction, but the M6a Windows-input follow-up returned to the original warning totals. The most likely cause is methodology drift: the warning counts were taken from different effective build snapshots/profiles/commands, so they are not directly comparable.

Core rule established in this validation: **warning deltas are valid only when the exact same command and extraction pipeline are used against the same repo state policy and equivalent build state assumptions.**

## C. Build script audit
Audited `striv/build/striv-build-core.sh` and `striv/build/striv-build-core.ps1`.

Observed behavior:
1. Build AssemblyProcessor project (`dotnet build` AP csproj).
2. Validate resulting AP DLL exists and is a real PE (bash adds extra checks including LFS pointer check).
3. Restore full solution (`dotnet restore striv/StriV.Core.slnx`).
4. Build full solution (`dotnet build ... -p:StriVAssemblyProcessorPath=...`).

Implications for warning counting:
- **Multiple phases exist**, including an AP pre-build and then full solution build.
- **Warning duplication is expected** when the same project emits warnings in more than one phase (AP project appears in phase 1 and again inside solution build).
- Script does **not clean** outputs.
- Therefore, extracted warning lines from full stdout/stderr can include repeated warnings and can be heavily state-sensitive if incremental build reuse occurs.

Count definitions for future reports:
- **Build summary warnings**: final `N Warning(s)` value from the canonical run log.
- **Extracted warning lines**: regex-matched lines (`warning CS/CA/NU/STRIDE...`) across full log; includes repeats by design.
- **Unique source locations**: optional secondary metric if needed later, not required for baseline gate.

## D. Current exclusion state
Validation of target csproj files:
- Android exclusion present in `Stride.Games` (`Starter/StrideActivity.cs` removed).
- Input exclusions present in `Stride.Input`:
  - `Android/**/*.cs` removed.
  - `UWP/**/*.cs` removed.
  - `InputSourceWindowsDirectInput.cs` removed.
  - `InputSourceWindowsXInput.cs` removed.
- Windows RawInput remains retained (no remove rule for RawInput source).
- WindowsMixedReality remains excluded in `Stride.Graphics` (`WindowsMixedReality/**/*.cs` removed).

Conclusion: M6a platform exclusion intent is still present with RawInput retained and DirectInput/XInput excluded.

## E. Candidate baseline comparison

| Candidate | Command | Exit | Summary warnings | Extracted lines | Notes |
| --------- | ------- | ---: | ---------------: | --------------: | ----- |
| A | `./striv/build/striv-build-core.sh` | 0 | 2621 | 5472 | Full graph coverage via current project workflow; includes duplicate emissions across phases. |
| B | `dotnet build striv/StriV.Core.slnx -c Debug --no-restore` | 0 | 0 | 0 | Not suitable as warning baseline: highly incremental/no-op on warmed outputs. |
| C | `dotnet build striv/projects/StriV.CoreSmoke/StriV.CoreSmoke.csproj -c Debug --no-restore` | 0 | 0 | 0 | Too narrow and incremental-sensitive; not representative of full clean graph warning surface. |

## F. Chosen canonical method
Chosen canonical method: **Candidate A pipeline**.

Canonical command:
```bash
./striv/build/striv-build-core.sh 2>&1 | tee /tmp/striv-warning-baseline.log
```

Canonical extraction commands:
```bash
grep -E "warning (CS|CA|NU|STRIDE)[0-9]+" /tmp/striv-warning-baseline.log > /tmp/striv-warning-lines.log || true
wc -l /tmp/striv-warning-lines.log
sed -E 's/.*warning ((CS|CA|NU|STRIDE)[0-9]+).*/\1/' /tmp/striv-warning-lines.log | sort | uniq -c | sort -nr | head -n 40
grep -Eo "\[[^]]+\.csproj\]" /tmp/striv-warning-lines.log | sort | uniq -c | sort -nr | head -n 40
```

Future report comparison rules:
1. Use **exactly** this build command.
2. Use **exactly** this extraction pipeline (or helper script that executes same logic).
3. Record both:
   - final build-summary warning count,
   - extracted warning-line count.
4. Interpret extracted lines as repeat-inclusive; do not treat them as unique warnings.

## G. Current canonical baseline
Captured on **2026-05-04** at commit **`1439e65`**.

Results:
- Build summary warnings: **2621**
- Extracted warning lines: **5472**

Top warning codes (count):
- CS8618 (2066)
- CS8625 (940)
- CS8604 (500)
- CS8600 (492)
- CS8603 (366)
- CS8601 (292)
- CS8602 (230)
- CS8622 (132)
- CS8765 (122)
- CS0618 (68)

Top warning projects (count):
- Stride.Rendering.csproj (1650)
- Stride.Engine.csproj (982)
- Stride.Graphics.csproj (886)
- Stride.FreeImage.csproj (390)
- Stride.Games.csproj (292)
- Stride.Core.AssemblyProcessor.csproj (232)
- Stride.Input.csproj (204)

## H. Helper script
Added helper:
- `striv/build/striv-warning-baseline.sh`

Behavior:
- Runs canonical baseline command (`striv-build-core.sh`) from repo root.
- Captures full build log.
- Extracts warning lines using canonical regex.
- Prints:
  - build exit code,
  - build-summary warning count,
  - extracted warning-line count,
  - top warning codes,
  - top warning projects.
- Exits with underlying build exit code (does not mask build failures).

Options:
- `--log <path>`: write full log to specific path.
- default log path: temporary `/tmp/striv-warning-baseline.XXXXXX.log`.

## I. Validation
Because a helper script was added, baseline execution was run through both direct command and helper.

Executed:
- `./striv/build/striv-build-core.sh` (canonical direct run) — success.
- `./striv/build/striv-warning-baseline.sh --log /tmp/striv-warning-baseline-canonical.log` — helper behavior validated.

Note: helper run executed on a warmed build state and reported zero warnings, which reinforces the requirement to keep method + build state assumptions explicit when comparing deltas.

## J. Recommended next task
Proceed to **M6b Engine/Rendering axing** only using the canonical baseline command/extraction above for before/after measurements. If a stricter state control is desired, add a future enhancement to baseline policy (e.g., explicit clean strategy) before starting M6b deltas.
