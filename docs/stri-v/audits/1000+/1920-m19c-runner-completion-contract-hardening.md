# 1920 — M19c runner completion contract hardening

## 1) Files changed
- `striv/projects/StriV.Engine.Dominatus.Runtime/StriVEngineLifecycleRunner.cs`
- `striv/tests/StriV.Engine.Dominatus.Tests/Runtime/StriVEngineLifecycleRunnerTests.cs`

`Stride.Engine` production files changed: **none**.

## 2) Task scope
This change hardens the opt-in runner contract around tick budgeting, cancellation checks, and argument/options validation only. It does not rewire runtime host paths, does not add new lifecycle features, and does not modify default engine execution paths.

## 3) Runtime API findings
- `HfsmInstance` does not expose an explicit terminal state/outcome API; it does expose `GetActivePath()`.
- `AiAgent` does not expose running/completed/faulted status.
- `AiWorld.Tick(float)` returns `void` and does not report tick outcomes.
- `ActuatorHost` does not expose a public pending-count API.
- Completion in this runner remains primarily inferred from bounded ticks; `GetActivePath().Count == 0` is observable but not treated as a strict completion contract for timeout enforcement.
- Honest timeout behavior cannot be asserted without a robust runtime completion API for this pattern.

## 4) Runner contract
- `MaxTicks` and `FixedDeltaSeconds` are now explicit runner options with validation.
- Cancellation before execution is checked and throws `OperationCanceledException` before actuation starts.
- Cancellation is checked between ticks and throws `OperationCanceledException` if requested during loop iterations.
- Actuation handler failures are not swallowed by the runner; exceptions propagate from runtime/handlers.
- The runner executes a bounded tick loop and may exit early if HFSM path is empty; no timeout exception is asserted in M19c because completion observability is limited.

## 5) Tests
- Added options validation tests for invalid max ticks and fixed delta.
- Added cancellation-before-start test for attach path and verified no mutations occur.
- Added cancellation-before-start test for cleanup path and verified no cleanup mutations occur.
- Preserved existing success tests and null-argument tests.

## 6) Behavior compatibility
- Existing runner success behavior is preserved.
- No engine runtime rewiring was introduced.
- No direct Dominatus dependency was added to `Stride.Engine`.

## 7) Deferred
- Explicit timeout/failure-on-noncompletion policy tied to a reliable completion API.
- Dedicated failure-injection seam for runner-level handler exception tests (kept out to avoid API overbuild).
- Async/deferred actuation completion contract tests for this runner path (current handlers are synchronous).

## 8) Validation results
1. `dotnet build striv/projects/StriV.Engine.Dominatus.Runtime/StriV.Engine.Dominatus.Runtime.csproj -c Debug -v minimal`
   - Exit: 0
   - First meaningful warning/error: existing repository warnings (non-M19c)
   - Pass/Fail: PASS
   - Output truncated: yes

2. `dotnet test striv/tests/StriV.Engine.Dominatus.Tests/StriV.Engine.Dominatus.Tests.csproj -v minimal`
   - Exit: 0 (final run)
   - First meaningful warning/error: none in final run
   - Pass/Fail: PASS
   - Output truncated: no

3. `dotnet build striv/projects/Stride.Engine/Stride.Engine.csproj -c Debug -p:StriVWarningFocusProject=Stride.Engine --no-incremental 2>&1 | tee /tmp/striv-m19c-engine-focused.log`
   - Exit: 0
   - First meaningful warning/error: existing nullable warnings in `Stride.Engine`
   - Pass/Fail: PASS
   - Output truncated: yes

## 9) Recommended next task
M19d candidate: add an explicit opt-in cleanup method that detaches scene/transform ownership boundaries (small, production-safe migration step), while keeping runtime host integration opt-in.
