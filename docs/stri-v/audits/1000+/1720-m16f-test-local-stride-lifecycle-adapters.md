# 1720 — M16f test-local Stride lifecycle adapters

## 1) Files changed
- Added `striv/tests/StriV.Engine.Dominatus.Tests/Adapters/StrideTransformLifecycleTestAdapter.cs`.
- Added `striv/tests/StriV.Engine.Dominatus.Tests/Adapters/StrideSceneLifecycleTestAdapter.cs`.
- Updated `striv/tests/StriV.Engine.Dominatus.Tests/TransformLifecycleBridgeTests.cs` to use reusable test adapter.
- Updated `striv/tests/StriV.Engine.Dominatus.Tests/SceneLifecycleBridgeTests.cs` to use reusable test adapter.
- Added `striv/tests/StriV.Engine.Dominatus.Tests/StrideLifecycleTestAdapterTests.cs`.
- Added this audit document.

`Stride.Engine` production files changed: **No**.

## 2) Task scope
M16f only extracts reusable **test-local** adapters for transform and scene lifecycle bridge proofs. No production runtime rewiring, no production adapter project, and no migration of live engine behavior.

## 3) Adapter design
- `StrideTransformLifecycleTestAdapter` implements `ITransformLifecycleActuator` with:
  - `AttachParentAsync` using `child.Transform.Parent = parent.Transform`.
  - `DetachParentAsync` using an internal helper boundary.
  - `AttachCalls`/`DetachCalls` counters for transition correlation tests.
- `StrideSceneLifecycleTestAdapter` implements `ISceneLifecycleActuator` with:
  - `AttachEntityToSceneAsync` using `entity.Scene = scene`.
  - `DetachEntityFromSceneAsync` using an internal helper boundary.
  - `AttachCalls`/`DetachCalls` counters for transition correlation tests.
  - no-op implementations for scene attach/detach interface members not in current proof flow.

## 4) Legacy API containment
Legacy null-as-detach API calls are intentionally isolated inside adapter-private helper methods:
- `child.Transform.Parent = null!`
- `entity.Scene = null!`

These are explicitly documented as compatibility behavior within the test adapter boundary and are not a production design endorsement.

## 5) Tests
- Bridge tests now consume reusable adapters while preserving prior behavior assertions and transition call-count checks.
- Existing throwing actuator tests remain local and unchanged in purpose.
- New adapter-focused tests directly verify current Stride API wrapping for:
  - transform attach,
  - transform detach,
  - scene attach,
  - scene detach.
- No runtime rewiring is introduced by any test.

## 6) Behavior compatibility
- `Stride.Engine` behavior is unchanged.
- No direct Dominatus dependency added to `Stride.Engine`.
- No runtime migration applied.

## 7) Validation results
1. `dotnet build striv/projects/StriV.Engine.Dominatus/StriV.Engine.Dominatus.csproj -c Debug -v minimal`
   - Exit: 0
   - First meaningful warning/error: warning baseline from upstream Stride projects (nullable/obsolete warnings).
   - Pass/Fail: Pass
   - Output truncated: Yes

2. `dotnet test striv/tests/StriV.Engine.Dominatus.Tests/StriV.Engine.Dominatus.Tests.csproj -v minimal`
   - Exit: 0
   - First meaningful warning/error: none for test run; all tests passed.
   - Pass/Fail: Pass
   - Output truncated: No

3. `dotnet build striv/projects/Stride.Engine/Stride.Engine.csproj -c Debug -p:StriVWarningFocusProject=Stride.Engine --no-incremental 2>&1 | tee /tmp/striv-m16f-engine-focused.log`
   - Exit: 0
   - First meaningful warning/error: existing nullable warning in `Stride.Engine` (`CompressedTimeSpan.cs` etc.).
   - Pass/Fail: Pass (warnings expected)
   - Output truncated: Yes

4. `dotnet build striv/StriV.Core.slnx -c Debug -v minimal`
   - Exit: 0
   - First meaningful warning/error: existing baseline warnings from core/assembly-processor/tests.
   - Pass/Fail: Pass
   - Output truncated: Yes

5. Initial parallel `dotnet test` attempt alongside focused engine build
   - Exit: 1
   - First meaningful warning/error: `MSB4018 GenerateDepsFile` IO lock on `Stride.Core.deps.json` due to concurrent builds.
   - Pass/Fail: Fail (environment concurrency artifact)
   - Output truncated: Yes

## 8) Recommended next task (M16g)
Recommended M16g path:
1. Introduce a production-side adapter project still outside `Stride.Engine`.
2. Add processor lifecycle bridge proof.
3. Add root-scene lifecycle bridge proof.
4. Optionally choose a bounded `EntityCloner` transition proof if processor/root-scene scope is deferred.
