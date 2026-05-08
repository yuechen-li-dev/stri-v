# 1510 — M13a `Stride.Core.MicroThreading` replacement viability audit

## 1) Files changed
- `docs/stri-v/audits/1000+/1510-m13a-microthreading-replacement-viability-audit.md` (report-only change)

## 2) Problem statement
`Stride.Core.MicroThreading` is a custom cooperative microthread scheduler/runtime inherited from Stride. The M13a decision is whether Stri-V should keep and clean it, or replace it with modern .NET primitives (`Task`/`ValueTask`, `System.Threading.Channels`, `CancellationToken`, single-reader worker loops, and bounded queues).

Given Stri-V doctrine (“one project, one clear function”), polishing an obsolete custom runtime before confirming necessity would be non-convergent. This audit therefore focuses on: what the project provides, who still consumes it in active runtime paths, semantic requirements, and replacement/migration risk.

## 3) Current project inventory

### 3.1 Project/package shape
- Project: `striv/projects/Stride.Core.MicroThreading/Stride.Core.MicroThreading.csproj`
- References only `Stride.Core`.
- Is referenced by `Stride.Core.Serialization`.

### 3.2 Source inventory
25 files in `striv/projects/Stride.Core.MicroThreading`, including:
- scheduler/runtime core: `Scheduler`, `MicroThread`, `SchedulerEntry`, `MicroThreadState`, `ScheduleMode`
- cooperative awaiters/channels: `Channel<T>`, `ChannelMicroThreadAwaiter<T>`, `MicroThreadYieldAwaiter`, `SwitchToAwaiter`
- context/local state: `MicroThreadSynchronizationContext`, `MicrothreadProxySynchronizationContext`, `MicroThreadLocal`
- cancellation/events/signaling: `AsyncSignal`, `AsyncAutoResetEvent`, `MicroThreadEvent`

### 3.3 Warning baseline (focused build)
Command run:
```bash
dotnet build striv/projects/Stride.Core.MicroThreading/Stride.Core.MicroThreading.csproj -c Debug \
  -p:StriVWarningFocusProject=Stride.Core.MicroThreading \
  --no-incremental
```
Result: exit code `0`, build succeeded, `8` warning lines in direct output (`16` lines in harvested grep due to repeated summary lines).

Harvested warning code frequency from `/tmp/striv-m13a-microthreading-warning-lines.log`:
- `CS8625`: 4
- `CS8618`: 4
- `CS8604`: 4
- `CS8603`: 2
- `CS8601`: 2

(Counts are from warning-line extraction command requested in task; duplicates reflect warning + summary duplication.)

### 3.4 Major behavioral types observed
- `Scheduler`: priority-bucket execution queues, `Run()` pump, re-entrant scheduling, `NextFrame()` via internal channel, exception propagation policy.
- `MicroThread`: lifecycle/state machine (`Starting/Running/Completed/Canceled/Failed`), cancellation token source, continuation scheduling.
- `MicroThreadSynchronizationContext`/`MicrothreadProxySynchronizationContext`: context-based identity and microthread-local access.

## 4) Consumer map

| Consumer | Active? | Uses what | Needed semantics | Replaceable by Channel/Task? | Notes |
| --- | --- | --- | --- | --- | --- |
| `Stride.Engine/Engine/Processors/ScriptSystem.cs` | Yes (runtime core) | `Scheduler`, `MicroThread`, `NextFrame`, script priority scheduling, `Run()` frame pump | Frame-affine cooperative update orchestration, priority, deterministic per-frame pumping, exception routing | Partially yes; needs custom frame dispatcher abstraction on top of Task/Channel | This is the largest semantic anchor.
| `Stride.Engine/Engine/AsyncScript.cs` | Yes | `MicroThread` + cancellation token exposure to scripts | Script cancellation + script lifecycle coupling | Yes with compatibility layer | Depends on `ScriptSystem` migration path.
| `Stride.BepuPhysics/BepuSimulation.cs` | Yes | `Scheduler.CurrentMicroThread`, `SynchronizationContext`, `TickAwaiter` to pre/post physics tick | Tick-affine continuation resumption with context capture | Yes with explicit tick dispatcher queue + context token | Requires deterministic tick boundaries.
| `Stride.Core.Serialization/Serialization/Contents/ContentManager.cs` | Yes | `Scheduler.CurrentMicroThread`, `MicrothreadProxySynchronizationContext` | Preserve microthread-local context during `Task.Factory.StartNew` | Yes with async-local/context shim | Can be decoupled from full scheduler.
| `Stride.Core.Serialization.csproj` | Structural | Project reference only | Compile-time dependency | Yes | Could be removed after context API decoupling.
| Older audit docs mentioning project | Fossil metadata | textual references | none | n/a | Non-runtime.

## 5) Semantics map (what MicroThreading actually provides)

1. **Cooperative scheduling model**
   - Scheduler-managed callback lists, not OS-thread preemption.
   - Priority buckets and schedule mode first/last ordering.

2. **Yield model**
   - `Scheduler.Yield()` and `Scheduler.NextFrame()` with microthread-context enforcement.

3. **Frame integration**
   - `ScriptSystem.Update()` pumps scheduler each frame; async scripts and sync script batches share scheduler priority space.

4. **Cancellation**
   - Each `MicroThread` has `CancellationTokenSource`; cancellation expected to propagate through cooperative awaiters.

5. **Synchronization context integration**
   - `Scheduler.CurrentMicroThread` is derived from current sync context.
   - Proxy sync context used by serialization to access microthread-local state.

6. **Exception behavior**
   - Microthread exceptions stored per microthread; optionally escalated via `PropagateExceptions` unless ignored by flags.

7. **Task/future integration**
   - `WhenAll` over microthreads via `TaskCompletionSource`.

8. **Microthread-local state**
   - `MicroThreadLocal` exists and is accessed via context identity.

9. **Lifecycle/shutdown**
   - Stateful transitions and all-thread tracking list; scheduler disposal is lightweight (does not own worker threads).

## 6) MSDF channel/worker pattern audit

### 6.1 File inspected
- `striv/projects/Stride.Graphics/Font/RuntimeSignedDistanceFieldSpriteFont.cs`

### 6.2 Observed pattern
- Uses `System.Threading.Channels` with bounded channel capacity (`WorkQueueCapacity=1024`).
- Fixed worker pool (`WorkerCount=2`) launched via `Task.Run`.
- Cancellation and shutdown via `CancellationTokenSource`, `TryComplete`, cancellation-aware reader loop.
- Producer side avoids blocking render thread (`TryWrite` only).
- Results published through `ConcurrentQueue` and drained on render thread.
- Deterministic ownership split: CPU generation on workers, GPU upload on render thread.

### 6.3 Semantics covered already
- Async job queueing
- Bounded backpressure behavior
- Cancellation + worker lifetime
- Graceful shutdown path
- Deterministic publication handoff

### 6.4 Gaps vs MicroThreading
- No microthread-local abstraction.
- No built-in frame/tick await primitive like `NextFrame`.
- No unified priority scheduling across heterogeneous script tasks.
- No script-system exception policy equivalent.

**Conclusion:** MSDF pattern is a strong practical template for *work queues*, but not a full drop-in for engine script scheduling semantics.

## 7) Replacement feasibility

| Requirement | MicroThreading currently provides? | Channels/Tasks replacement | Risk |
| --- | --- | --- | --- |
| Cooperative yield inside script update model | Yes | Frame dispatcher queue + `await NextFrameAsync()` shim | Medium |
| Priority scheduling | Yes (priority buckets) | Multi-queue channels or priority queue + worker/frame pump | Medium |
| Cancellation tokens on logical script units | Yes | Native `CancellationToken` | Low |
| Context identity for “current logical task” | Yes (sync context based) | `AsyncLocal` + explicit logical-context scope | Medium |
| Frame-affine continuation | Yes (`NextFrame`) | Main-thread/frame pump queue | Medium |
| Physics pre/post tick await | Yes (in Bepu integration) | Tick-phase dispatcher queues | Medium |
| Exception propagation policy | Yes | Explicit supervisor policy in dispatcher | Low-Medium |
| Work queueing for background jobs | Partial/custom | Channels (already proven in MSDF path) | Low |

Overall feasibility: **high for replacement in principle**, but **not an immediate delete** because active consumers rely on frame/tick/context semantics that must be reintroduced intentionally.

## 8) Proposed minimal replacement architecture (design-only)

Recommendation is to avoid a new “scheduler kingdom” and split concerns:

1. **Core work queue abstraction** (channel-backed)
   - `StriVWorkQueue : IAsyncDisposable`
   - `Enqueue(Func<CancellationToken, ValueTask>)`
   - `RunAsync(CancellationToken)`

2. **Frame dispatcher abstraction** (main-thread pump)
   - `FrameWorkQueue` with `NextFrameAsync()` and deterministic drain in engine update.

3. **Tick dispatcher (physics phases)**
   - `PreTickQueue` and `PostTickQueue` to replace BEPU awaiter semantics.

4. **Logical context shim**
   - Minimal context carrier using `AsyncLocal` and optional custom `SynchronizationContext` bridge only where needed (serialization path).

5. **Compatibility facade (temporary)**
   - Thin adapters exposing just enough old API shape while consumers migrate.

## 9) Migration plan

### Proposed strategy: **C now, then B**
- **C (temporary compatibility shim)**: keep `Stride.Core.MicroThreading` API surface stable while introducing small queue/dispatcher primitives.
- **B (replacement)**: migrate active consumers incrementally.

Phases:
1. **Consumer hardening audit (M13b)**: pin exact call-sites and runtime tests for ScriptSystem, Bepu tick awaiters, ContentManager context usage.
2. **Introduce minimal abstractions** in a small runtime/threading module (no broad framework).
3. **Migrate lowest-risk consumer first**: serialization context bridge and/or isolated async worker use.
4. **Migrate ScriptSystem + Bepu tick semantics** with regression tests for frame order/cancellation/exception behavior.
5. **Quarantine MicroThreading** (obsolete namespace, compatibility-only mode).
6. **Delete** once no runtime consumers remain.

## 10) Risks / blockers
- Hidden dependencies on `SynchronizationContext`-based microthread identity.
- Script update determinism and priority ordering regressions.
- Physics tick phase affinity regressions.
- Exception propagation behavior mismatch.
- Cancellation behavior mismatches in long-running scripts.
- Potential latent dependencies in engine subsystems not captured by textual search.

## 11) Validation results

### 11.1 Focused MicroThreading build
- Command: `dotnet build striv/projects/Stride.Core.MicroThreading/Stride.Core.MicroThreading.csproj -c Debug -p:StriVWarningFocusProject=Stride.Core.MicroThreading --no-incremental`
- Exit code: `0`
- Pass/fail: **Pass**
- First meaningful warning/error: `CS8604` in `Channel.cs` (nullability)
- Output truncated: **No** (captured to `/tmp/striv-m13a-microthreading-baseline.log`)

### 11.2 Core solution build
- Command: `dotnet build striv/StriV.Core.slnx -c Debug -p:StriVWarningFocusProject=Stride.Core.MicroThreading --no-incremental`
- Exit code: `0`
- Pass/fail: **Pass**
- First meaningful warning/error: `CS8604` in `Stride.Core.MicroThreading/Channel.cs`
- Output truncated: **No** (captured to `/tmp/m13a-build-slnx.log`)

### 11.3 Games tests
- Command: `dotnet test striv/tests/Stride.Games.Tests/Stride.Games.Tests.csproj -v minimal`
- Exit code: `0`
- Pass/fail: **Pass**
- First meaningful warning/error: none; test summary `Passed 25`
- Output truncated: **No** (captured to `/tmp/m13a-test-games.log`)

### 11.4 Input tests
- Command: `dotnet test striv/tests/Stride.Input.Tests/Stride.Input.Tests.csproj -v minimal`
- Exit code: `0`
- Pass/fail: **Pass**
- First meaningful warning/error: none; test summary `Passed 10`
- Output truncated: **No** (captured to `/tmp/m13a-test-input.log`)

### 11.5 Focused-project check script
- Command: `./striv/build/striv-check-focused-projects.sh Stride.BepuPhysics Stride.Core.Mathematics Stride.Core.IO Stride.Input Stride.Games`
- Exit code: `0`
- Pass/fail: **Pass**
- First meaningful warning/error: none; all listed projects `pass`, warnings `0`
- Output truncated: **No** (captured to `/tmp/m13a-check-focused.log`)

### 11.6 Core build script
- Command: `./striv/build/striv-build-core.sh`
- Exit code: `0`
- Pass/fail: **Pass**
- First meaningful warning/error: nullable warnings in `Stride.Engine/Game.cs` (`CS8625`)
- Output truncated: **No** (captured to `/tmp/m13a-build-core.log`)

## 12) Recommendation and next task

## Recommendation: **C (keep temporarily behind compatibility shim), then execute B migration**

Rationale:
- `Stride.Core.MicroThreading` is **not dead code**; it anchors active runtime script and tick-flow semantics.
- But a full custom microthread runtime is likely **over-scoped** for Stri-V direction, and channels/tasks already prove effective for worker pipelines (MSDF case).
- Therefore immediate deletion is unsafe; full 5S polishing first is likely wasteful.

### Recommended next task
**M13b consumer migration audit/spec** (first), immediately followed by **M13b minimal channel work queue + frame/tick dispatcher design spec**.

This keeps convergence: isolate exact semantic contracts first, then replace with smallest viable abstractions.
