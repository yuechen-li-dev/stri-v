# 520 - clean runtime EffectBytecode serializer validation

## 1. Files changed
- `striv/projects/Stride.Shaders/Stride.Shaders.csproj`
- `striv/tests/StriV.CleanGraph.Tests/StriV.CleanGraph.Tests.csproj`
- `striv/tests/StriV.CleanGraph.Tests/CleanGraphSmokeTests.cs`
- `docs/stri-v/building-core.md`

## 2. Runtime blocker recap
- Prior failing command (from 510 audit context): `xvfb-run -a ./striv/build/striv-run-coresmoke.sh`.
- Exception: `System.InvalidOperationException`.
- Missing serializer type: `Stride.Shaders.EffectBytecode`.
- Classification: managed serializer registration failure (after SDL/X11 + Vulkan init gates), not native platform bootstrap.

## 3. Serializer registration audit
- `EffectBytecode` is defined in `sources/engine/Stride.Shaders/EffectBytecode.cs` and marked `[DataContract]` plus content serializer metadata.
- Expected mechanism is AssemblyProcessor serialization registration (no dedicated manual `EffectBytecode` serializer class exists in `Stride.Shaders`; this is contract-based serializer generation/registration path).
- Old `sources/engine/Stride.Shaders/Stride.Shaders.csproj` has:
  - `StrideAssemblyProcessor=true`
  - `StrideAssemblyProcessorOptions=--serialization --parameter-key`
- Clean `striv/projects/Stride.Shaders/Stride.Shaders.csproj` before fix imported StriV AP target but had no `StriVAssemblyProcessorOptions`, so clean AP target condition prevented AP execution.
- Clean settings after fix: `StriVAssemblyProcessorOptions=--serialization --parameter-key --auto-module-initializer`.
- Module initializer/registration behavior was effectively missing in clean graph because AP wasn’t running at all for this project.

## 4. Diagnostic test
- Added: `EffectBytecodeSerializer_IsAvailable_InCleanProfile` in `striv/tests/StriV.CleanGraph.Tests/CleanGraphSmokeTests.cs`.
- Test assertion: `SerializerSelector.Default.GetSerializer<EffectBytecode>()` is non-null.
- Why narrow: checks only serializer availability for runtime-missing type; no assets/shader compile/parser/audio/VR paths touched.
- What it does not prove: end-to-end runtime content loading success.

## 5. Fix implemented
- Enabled clean AP options for `Stride.Shaders` in clean csproj.
- Added direct clean test project reference to `Stride.Shaders` and diagnostic test.
- Minimality rationale: change is restricted to reactivating the serializer registration path and adding one oracle test.
- Shader compiler/parser remains excluded by profile constants (no opt-in changes made).
- This is not a serializer redesign; it is registration-path restoration only.

## 6. Validation results
1. `./striv/build/striv-build-core.sh`
   - Exit code: `0`
   - First meaningful warning: existing nullable/legacy warnings and NU1510 package-pruning warnings.
   - Pass/fail: **PASS**
   - Output truncated: **Yes** (tool output capped)

2. `dotnet test striv/tests/StriV.CleanGraph.Tests/StriV.CleanGraph.Tests.csproj`
   - Exit code: `1`
   - First meaningful error: test failure `EffectBytecodeSerializer_IsAvailable_InCleanProfile` with `Assert.NotNull() Failure: Value is null`.
   - Pass/fail: **FAIL**
   - Output truncated: **No**

3. Re-run after AP option update:
   - `dotnet test striv/tests/StriV.CleanGraph.Tests/StriV.CleanGraph.Tests.csproj`
   - Exit code: `1`
   - First meaningful error: same failing test (`SerializerSelector.Default.GetSerializer<EffectBytecode>() == null`).
   - Pass/fail: **FAIL**
   - Output truncated: **No**

## 7. First new blocker
- Command: `dotnet test striv/tests/StriV.CleanGraph.Tests/StriV.CleanGraph.Tests.csproj`
- Failure: `EffectBytecodeSerializer_IsAvailable_InCleanProfile` still fails.
- Likely cause: AP-dependent serializer registration for `Stride.Shaders` is still not visible in this plain `dotnet test` flow (likely AP execution/routing mismatch under test invocation, or registration path not executed/loaded in this context).
- Smallest next repair: trace AP invocation and post-processed IL/registration artifacts for `Stride.Shaders` in the test build path, then align test/runtime build property routing (or minimal explicit bootstrap registration call) based strictly on evidence.

## 8. Runtime smoke result
- Not attempted in this run due stop-at-first-blocker rule after test failure.
- Therefore cannot yet claim whether serializer error disappeared at CoreSmoke runtime.

## 9. Worktree status
- `git status --short` run after edits (see terminal section in this task run).

## 10. Recommended next task
- **next serializer registration repair** (focused on AP invocation/registration visibility for `Stride.Shaders` in test/runtime invocation paths).
