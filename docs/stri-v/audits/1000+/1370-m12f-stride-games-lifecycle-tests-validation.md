# 1370 - M12f Stride.Games lifecycle tests validation

## 1) Files changed
- striv/tests/Stride.Games.Tests/Stride.Games.Tests.csproj
- striv/tests/Stride.Games.Tests/LifecycleTests.cs
- striv/StriV.Core.slnx
- striv/projects/Stride.Games/Properties/AssemblyInfo.cs

## 2) Task scope
M12f is a **test-first lifecycle stabilization pass** for `Stride.Games`, not a warning cleanup pass. The goal here was to establish deterministic behavioral coverage around headless host/window/system lifecycle seams before M12g nullability cleanup, so we can avoid changing lifecycle semantics while fixing warning-heavy areas.

## 3) Test project / design
- New test project: `striv/tests/Stride.Games.Tests/Stride.Games.Tests.csproj`.
- Dependencies: `Stride.Games`, `Stride.Core`, `xUnit`, `Microsoft.NET.Test.Sdk`.
- Internal seam added: `InternalsVisibleTo("Stride.Games.Tests" + Stride.PublicKeys.Default)` in `Stride.Games` to allow direct testing of internal headless window type.
- Tests are deterministic/headless and avoid SDL/native window/presenter/device requirements.

## 4) Lifecycle map established
- `GameTime` defaults are stable on construction; factor clamping and reset behavior are stable.
- `GameContextHeadless` constructs with `Headless` context type and null control callbacks, without native window prerequisites.
- `GameWindowHeadless` can be constructed with no native handle and supports deterministic resize-bound changes.
- `GameSystemCollection` preserves insertion order at collection level; update/draw execution follows update/draw order and honors Enabled/Visible filters.
- Headless tests confirm lifecycle coverage without `GraphicsDeviceManager` or presenter creation.
- Deferred unknowns remain for real backend run-loop/window/presenter device interactions.

## 5) Tests added
- `GameTime_Constructs_WithStableDefaults`
  - Locks stable timing defaults and factor guard behavior.
  - Supports safe M12g cleanup around timing-nullability assumptions.
- `GameContextHeadless_ConstructsWithoutNativeWindow`
  - Locks no-window required construction semantics.
  - Protects headless lifecycle path while cleaning host warnings.
- `GameWindowHeadless_ConstructsWithoutNativeHandle`
  - Locks internal headless window behavior without native handle.
  - Protects no-OS-window assumptions.
- `GameSystemCollection_AddRemove_PreservesOrder`
  - Locks system collection add/remove order behavior.
  - Guards lifecycle ordering assumptions before nullability cleanups.
- `GameSystemCollection_UpdateDraw_UsesEnabledVisibleOrder`
  - Locks update order, draw order, and enabled/visible filtering behavior.
  - Reduces M12g risk when touching lifecycle-heavy system code.

## 6) Production changes
- `striv/projects/Stride.Games/Properties/AssemblyInfo.cs`
  - Added `InternalsVisibleTo` for `Stride.Games.Tests` only.
  - No runtime behavior change.

## 7) Focused warning snapshot
From `/tmp/striv-m12f-games-warning-lines.log`:
- Focused warning count after test additions: **204**.
- Top codes:
  - CS8618: 120
  - CS8625: 44
  - CS8603: 8
  - CS8601: 8
  - CS8604: 6
  - CS8602: 6
  - CS0162: 6
  - CS8600: 4
  - CS8073: 2
- Comparison with M12e after-count: unchanged at 204.
- New warnings introduced in focused target: none.

## 8) Validation results
### Command: `dotnet test striv/tests/Stride.Games.Tests/Stride.Games.Tests.csproj -v minimal`
- Exit code: 0
- First meaningful warning/error: none in final run (all tests passed).
- Result: pass
- Output truncated: no

### Command: `dotnet build striv/projects/Stride.Games/Stride.Games.csproj -c Debug -p:StriVWarningFocusProject=Stride.Games --no-incremental`
- Exit code: 0
- First meaningful warning/error: `CS8625` in `Host/GameBase.cs`.
- Result: pass (with expected warnings)
- Output truncated: yes (terminal capture truncation in tool output)

### Command block (standard validation):
- `dotnet build striv/StriV.Core.slnx -c Debug -p:StriVWarningFocusProject=Stride.Games --no-incremental`
- `dotnet test striv/tests/Stride.Input.Tests/Stride.Input.Tests.csproj -v minimal`
- `dotnet test striv/tests/Stride.Games.Tests/Stride.Games.Tests.csproj -v minimal`
- `dotnet test striv/tests/StriV.AssetTool.Tests/StriV.AssetTool.Tests.csproj -v minimal`
- `dotnet test striv/tests/StriV.AssetPipeline.Tests/StriV.AssetPipeline.Tests.csproj --no-build -v minimal`
- `dotnet test striv/tests/StriV.ShaderPipeline.Tests/StriV.ShaderPipeline.Tests.csproj --no-build -v minimal`
- `dotnet test striv/tests/StriV.CleanGraph.Tests/StriV.CleanGraph.Tests.csproj -v minimal`
- `./striv/build/striv-build-core.sh`
- Exit code: 0 (command block)
- First meaningful warning/error: focused `Stride.Games` CS8625 warning during solution build.
- Result: pass
- Output truncated: yes (tool token truncation)

### Command block (focused snapshot + checker)
- `dotnet build ... | tee /tmp/striv-m12f-games-after-tests.log`
- warning extraction + `wc -l` + top-code histogram
- `./striv/build/striv-check-focused-project.sh Stride.Games`
- Exit code: 0 (wrapper command)
- First meaningful warning/error: focused warnings detected; checker returns 4
- Result: pass (expected checker status while warnings remain)
- Output truncated: yes (tool token truncation)

## 9) Deferred tests
- Graphics device manager lifecycle.
- Real window/message loop lifecycle.
- SDL desktop backend behavior.
- Presentation bridge behavior.
- Full `GameBase` run-loop end-to-end lifecycle.

## 10) Recommended next task
Proceed with **M12g Shine pass 3** to clean lifecycle nullability warnings in these now-tested areas (`GameContextHeadless`, `GameWindowHeadless`, `GameSystemCollection`-adjacent lifecycle assumptions), preserving tested ordering and headless semantics.
