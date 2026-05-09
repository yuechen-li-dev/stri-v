# M21d — Stride.Engine Straighten mechanical nullability cleanup pass

## 1) Files changed
- `striv/projects/Stride.Engine/Engine/SceneInstance.cs`
- `docs/stri-v/audits/1000+/2040-m21d-stride-engine-straighten-format-pass.md`

## 2) Task scope
This pass was a **Straighten** step only:
- re-baseline warnings,
- run `dotnet format` on safe diagnostics only,
- probe risky diagnostics in verify-only mode,
- avoid broad architecture/lifecycle rewrites,
- no warning suppression,
- no Dominatus migration.

## 3) Before warnings
Focused baseline command:
`dotnet build striv/projects/Stride.Engine/Stride.Engine.csproj -c Debug -p:StriVWarningFocusProject=Stride.Engine --no-incremental`

Focused warning lines before: **948**.

Top warning types before:
- CS8618: 332
- CS8625: 134
- CS8604: 86
- CS8602: 82
- CS8600: 70
- CS8603: 68

Top file buckets before (sample):
- `Engine/ScriptComponent.cs CS8618` (28)
- `Rendering/Compositing/ForwardRenderer.cs CS8618` (24)
- `Engine/SceneInstance.cs CS8622` (22)
- `Engine/Design/CloneSerializer.cs CS8602` (20)

## 4) `dotnet format` safe pass
Command:
`dotnet format striv/StriV.Core.slnx --diagnostics CS8618 CS8625 CS8600 CS8601 CS8603 --severity warn --include striv/projects/Stride.Engine --verbosity diagnostic`

Applied diagnostics: `CS8618 CS8625 CS8600 CS8601 CS8603`.

Result: formatter reported **Formatted 0 of 2770 files** for this scoped pass.

Diff review findings:
- No `Stride.Engine` nullable-fix hunks were produced by the safe diagnostics pass.
- A line-ending-only side effect touched Dominatus sample files; these were reverted before final patch.

## 5) Risky diagnostics probe (verify-only)
Command:
`dotnet format striv/StriV.Core.slnx --diagnostics CS8602 CS8604 --severity warn --include striv/projects/Stride.Engine --verify-no-changes --verbosity diagnostic`

Result: verify run completed with no applied changes (as intended).

Useful future targets remain concentrated around lifecycle-heavy buckets:
- `SceneInstance`
- `SceneSystem`
- `Game`
- `CloneSerializer`

## 6) Manual low-risk cleanup
No behavior-changing manual nullable fixes were applied in M21d.

Reason: safe formatter produced no applicable changes, and remaining top warnings in key buckets are lifecycle-coupled and not safely fixable without broader reasoning/tests.

## 7) Deferred warning comments
Added one classification marker in a lifecycle hotspot:
- `striv/projects/Stride.Engine/Engine/SceneInstance.cs`
  - Marker: `STRIV-TODO: Nullability/lifecycle cleanup`
  - Reason: `RootScene = null` is currently a legacy teardown-state marker.
  - Future direction: explicit lifecycle state transition model.

## 8) After warnings
After format (pre-manual): **948** focused warning lines.
After manual pass: **948** focused warning lines.

Distribution after: unchanged from baseline.

Total delta vs M21c baseline (~948): **~0** in focused warning-line count.

Movement summary:
- No net warning reduction in M21d.
- One lifecycle hotspot now explicitly classified for a targeted follow-up pass.

## 9) Validation results
Commands and outcomes:
- `dotnet test striv/tests/Stride.Engine.Tests/Stride.Engine.Tests.csproj -v minimal` → exit 0, pass.
- `dotnet build striv/StriV.Core.slnx -c Debug -v minimal` → exit 0, pass (warnings present in other projects).
- `./striv/build/striv-check-focused-projects.sh Stride.BepuPhysics Stride.Core.Mathematics Stride.Core.IO Stride.Input Stride.Games Stride.Core.Reflection` → exit 0, pass.
- `dotnet test striv/tests/StriV.Engine.Dominatus.Tests/StriV.Engine.Dominatus.Tests.csproj -v minimal` → exit 0, pass.
- `dotnet test striv/tests/Stride.Core.Reflection.Tests/Stride.Core.Reflection.Tests.csproj -v minimal` → exit 0, pass.
- `dotnet test striv/tests/Stride.Games.Tests/Stride.Games.Tests.csproj -v minimal` → exit 0, pass.
- `dotnet test striv/tests/Stride.Input.Tests/Stride.Input.Tests.csproj -v minimal` → exit 0, pass.
- `dotnet test striv/tests/StriV.CleanGraph.Tests/StriV.CleanGraph.Tests.csproj -v minimal` → exit 0, pass.
- `dotnet test striv/tests/StriV.AssetTool.Tests/StriV.AssetTool.Tests.csproj -v minimal` → exit 0, pass.
- `dotnet test striv/tests/StriV.AssetPipeline.Tests/StriV.AssetPipeline.Tests.csproj --no-build -v minimal` → exit 0, pass.
- `dotnet test striv/tests/StriV.ShaderPipeline.Tests/StriV.ShaderPipeline.Tests.csproj --no-build -v minimal` → exit 0, pass.
- `./striv/build/striv-build-core.sh` → exit 0, pass.

Output truncation: terminal capture was truncated in-session due volume, but command exits were captured and successful.

## 10) Recommended next task (M21e)
**M21e SceneInstance/SceneSystem test-first lifecycle cleanup**.

Rationale:
- M21d proved safe mechanical diagnostics do not materially reduce this warning cluster.
- Largest remaining buckets include lifecycle/stateful paths where `null` encodes teardown or ownership transitions.
- Next improvement should be targeted, test-first, and limited to one lifecycle cluster at a time.
