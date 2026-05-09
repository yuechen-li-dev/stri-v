# 1900 — M19a opt-in engine lifecycle runner

## Part 1 — pre-coding inspection answers

- Repeated runtime setup in harness: create `ActuatorHost`, register runtime handlers + adapters, build `HfsmGraph` root state, create `AiAgent`/`HfsmInstance`, create `AiWorld`, add+initialize agent, tick world.
- Composed node registration: graph root set to `StateId("Root")`, then `graph.Add(new HfsmStateDef { Id = rootStateId, Node = node })`.
- Current test completion: one harness tick (`Tick(world)` defaults `count = 1`).
- Observable completion signal: no strong explicit completion assertion is used by current tests.
- Bounded tick count rationale: one tick is currently sufficient for M18/M19a path because all registered handlers complete synchronously and the composed node is a short linear `Ai.Act(...)` sequence.

## 1) Files changed

- Added runtime project: `striv/projects/StriV.Engine.Dominatus.Runtime/StriV.Engine.Dominatus.Runtime.csproj`
- Added runtime runner: `striv/projects/StriV.Engine.Dominatus.Runtime/StriVEngineLifecycleRunner.cs`
- Added runtime README: `striv/projects/StriV.Engine.Dominatus.Runtime/README.md`
- Added runtime tests: `striv/tests/StriV.Engine.Dominatus.Tests/Runtime/StriVEngineLifecycleRunnerTests.cs`
- Updated test references: `striv/tests/StriV.Engine.Dominatus.Tests/StriV.Engine.Dominatus.Tests.csproj`
- Updated solution: `striv/StriV.Core.slnx`
- Added this audit report.

`Stride.Engine` production files changed: **none**.

## 2) Task scope

Implemented first opt-in Dominatus lifecycle runner with one composed add-flow method only. No default runtime rewiring.

## 3) Runtime project design

- New project isolates opt-in Dominatus runtime hosting outside `Stride.Engine`.
- Dependency direction preserved: runtime references Dominatus project + adapters + `Stride.Engine`; `Stride.Engine` does not reference Dominatus.
- No direct Dominatus dependency added to `Stride.Engine`.

## 4) Runner design

- Public method: `AttachSceneTransformAndProcessorAsync(...)`.
- Registers handlers/adapters:
  - `EntitySceneAttachActuationHandler(new StrideSceneLifecycleActuator())`
  - `TransformParentAttachActuationHandler(new StrideTransformLifecycleActuator())`
  - `ProcessorSystemAddActuationHandler(new StrideProcessorLifecycleActuator())`
  - `ProcessorEntityAddActuationHandler(new StrideProcessorLifecycleActuator())`
- Executes node: `EngineLifecycleDominatusNodes.AttachSceneTransformAndProcessor(...)`.
- Tick strategy: bounded fixed tick count (`MaxTicks = 1`) based on current synchronous immediate-completion behavior and existing test harness evidence.
- Limitation: no explicit runtime completion contract used yet; M19a intentionally keeps this minimal and bounded.

## 5) Tests

- `...RunsThroughDominatusRuntime`
  - Proves runner executes composed runtime path.
  - Asserts scene attach, transform parent attach, processor system add, processor entity add callbacks/recording.
- `...RejectsNullArguments`
  - Proves guard clauses for scene/parent/child/entityManager/processor.

## 6) Behavior compatibility

- No engine default behavior changed.
- Adapters remain opt-in.
- No direct Dominatus dependency added to `Stride.Engine`.

## 7) Validation results

- `dotnet build striv/projects/StriV.Engine.Dominatus.Runtime/StriV.Engine.Dominatus.Runtime.csproj -c Debug -v minimal`
  - exit: 0
  - first meaningful warning/error: existing legacy nullability warnings in baseline projects
  - pass/fail: pass
  - truncated: yes
- `dotnet test striv/tests/StriV.Engine.Dominatus.Tests/StriV.Engine.Dominatus.Tests.csproj -v minimal`
  - exit: 0
  - first meaningful warning/error: none in final run
  - pass/fail: pass
  - truncated: no

(Other requested long validation suite commands not executed in this iteration.)

## 8) Recommended next task

M19b cleanup/remove runner method using the M18f cleanup composed node, keeping the same opt-in and bounded-host pattern.
