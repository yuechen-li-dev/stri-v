# 1520 - M13b MicroThreading legacy marker + dispatch seam validation

## 1) Files changed

- `striv/projects/Stride.Core.MicroThreading/README-legacy-compatibility.md`
- `striv/projects/Stride.Core.MicroThreading/Compatibility/IFrameDispatcher.cs`
- `striv/projects/Stride.Core.MicroThreading/MicroThread.cs`
- `striv/projects/Stride.Core.MicroThreading/Scheduler.cs`
- `striv/projects/Stride.Core.MicroThreading/MicroThreadSynchronizationContext.cs`
- `striv/projects/Stride.Core.MicroThreading/MicroThreadLocal.cs`

## 2) Task scope

This was a marker/seam task only:

- Added explicit legacy compatibility documentation for `Stride.Core.MicroThreading`.
- Added non-breaking remarks on key public types.
- Added a minimal future-facing interface seam (`IFrameDispatcher`) without implementation or rewiring.

No migration was performed, and no runtime behavior changes were introduced.

## 3) Legacy compatibility status

`Stride.Core.MicroThreading` is retained temporarily because active runtime consumers still depend on it. Verified consumers include:

- `Stride.Engine/Engine/Processors/ScriptSystem.cs`
- `Stride.Engine/Engine/AsyncScript.cs`
- `Stride.BepuPhysics/BepuSimulation.cs`
- `Stride.Core.Serialization/Serialization/Contents/ContentManager.cs`

It remains compatibility infrastructure, not future Stri-V architecture.

## 4) Replacement direction

Documented replacement direction is:

- Dominatus for scheduling/policy/lifecycle.
- Channel/Task/ValueTask/CancellationToken-based work queues.
- Explicit frame/tick dispatchers for frame-affine and tick-affine operations.
- AsyncLocal/logical context shim only where context preservation is needed.

## 5) Documentation/comments added

- Added `README-legacy-compatibility.md` in `Stride.Core.MicroThreading` with status, retained-consumer rationale, strategic direction, and usage rules.
- Added Stri-V legacy compatibility remarks on:
  - `MicroThread`
  - `Scheduler`
  - `MicroThreadSynchronizationContext`
  - `MicroThreadLocal<T>`

## 6) Interface seam

Added:

- `striv/projects/Stride.Core.MicroThreading/Compatibility/IFrameDispatcher.cs`
- Interface: `Stride.Core.MicroThreading.Compatibility.IFrameDispatcher`

Intended future role:

- Minimal abstraction for frame-bound continuation and queueing semantics (`NextFrameAsync`, `EnqueueAsync`).

Why no implementation/migration was done:

- M13b scope is marker/seam only.
- Any concrete implementation or consumer rewiring would be migration work and was explicitly deferred.

## 7) Behavior compatibility

- No scheduler behavior changed.
- No `MicroThread` behavior changed.
- No active consumers were migrated.
- No project references were changed.
- Build/test evidence (below) confirms compatibility remained intact.

## 8) Warning snapshot

Focused `Stride.Core.MicroThreading` build warning count remains **8** warnings (pre-existing nullability warnings surfaced by build). No broad warning cleanup was performed.

## 9) Validation results

### Command results

1. Command:

```bash
dotnet build striv/projects/Stride.Core.MicroThreading/Stride.Core.MicroThreading.csproj -c Debug \
  -p:StriVWarningFocusProject=Stride.Core.MicroThreading \
  --no-incremental \
  2>&1 | tee /tmp/striv-m13b-microthreading-build.log
```

- Exit code: `0`
- First meaningful warning/error: `Scheduler.cs(122,44): warning CS8604 ...`
- Pass/fail: **PASS**
- Output truncated: **No**

2. Command:

```bash
dotnet build striv/StriV.Core.slnx -c Debug \
  -p:StriVWarningFocusProject=Stride.Core.MicroThreading \
  --no-incremental \
  2>&1 | tee /tmp/striv-m13b-microthreading-slnx-build.log
```

- Exit code: `0`
- First meaningful warning/error: `Scheduler.cs(122,44): warning CS8604 ...` (from focused project)
- Pass/fail: **PASS**
- Output truncated: **No**

3. Command:

```bash
dotnet test striv/tests/Stride.Games.Tests/Stride.Games.Tests.csproj -v minimal
```

- Exit code: `0`
- First meaningful warning/error: none (tests passed)
- Pass/fail: **PASS**
- Output truncated: **No**

4. Command:

```bash
dotnet test striv/tests/Stride.Input.Tests/Stride.Input.Tests.csproj -v minimal
```

- Exit code: `0`
- First meaningful warning/error: none (tests passed)
- Pass/fail: **PASS**
- Output truncated: **No**

5. Command:

```bash
./striv/build/striv-check-focused-projects.sh Stride.BepuPhysics Stride.Core.Mathematics Stride.Core.IO Stride.Input Stride.Games
```

- Exit code: `0`
- First meaningful warning/error: none (`pass` for all listed projects)
- Pass/fail: **PASS**
- Output truncated: **No**

6. Command:

```bash
./striv/build/striv-build-core.sh
```

- Exit code: `0`
- First meaningful warning/error: solution-level existing warnings outside M13b scope (e.g., `Stride.Rendering` CS0436/CS8625 family)
- Pass/fail: **PASS**
- Output truncated: **Yes** (terminal capture truncated; command log still confirms success)

## 10) Recommended next task

Recommended next step: **defer MicroThreading migration until Dominatus integration planning is concretely defined**, then produce a consumer-by-consumer migration spec (starting with a narrow, lowest-risk consumer path).
