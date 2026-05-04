# 530 — Clean runtime EffectBytecode assembly-load validation

## 1. Files changed
- `striv/tests/StriV.CleanGraph.Tests/CleanGraphSmokeTests.cs`
- `docs/stri-v/audits/530-clean-runtime-effectbytecode-assembly-load-validation.md`

## 2. Hypothesis recap
- **Candidate A (AP registration missing):** AssemblyProcessor did not inject/register the `EffectBytecode` serializer into clean `Stride.Shaders`.
- **Candidate B (assembly not loaded/initialized):** `Stride.Shaders.dll` was not loaded (or module initializer not run) before `SerializerSelector.Default.GetSerializer<EffectBytecode>()`.

This follow-up tested **Candidate B first** with the smallest possible test-local change.

## 3. Test change
In `EffectBytecodeSerializer_IsAvailable_InCleanProfile`, added explicit type/assembly touches before serializer lookup:

```csharp
_ = typeof(EffectBytecode).Assembly;
_ = typeof(EffectBytecode).FullName;
```

Also added diagnostics:
- `EffectBytecode` assembly full name,
- assembly location,
- whether the same assembly instance is present in `AppDomain.CurrentDomain.GetAssemblies()`.

Then serializer lookup remains unchanged:

```csharp
var serializer = SerializerSelector.Default.GetSerializer<EffectBytecode>();
Assert.NotNull(serializer);
```

## 4. Validation results

### Command 1
- **Command:** `./striv/build/striv-build-core.sh`
- **Exit code:** `0`
- **First meaningful warning/error:** `warning NU1510` package-pruning warnings in `Stride.Graphics`/`Stride.Engine`.
- **Pass/fail:** **Pass**
- **Output truncated:** **No** (captured to `/tmp/build_core.log`, inspected via `tail`).

### Command 2
- **Command:** `dotnet test striv/tests/StriV.CleanGraph.Tests/StriV.CleanGraph.Tests.csproj --no-build -v minimal`
- **Exit code:** `0`
- **First meaningful warning/error:** none in this run.
- **Pass/fail:** **Pass** (`Passed: 4, Failed: 0`).
- **Output truncated:** **No** (captured to `/tmp/clean_test.log`).

### Command 3
- **Command:** `xvfb-run -a ./striv/build/striv-run-coresmoke.sh`
- **Exit code:** `0`
- **First meaningful warning/error:** recurring `warning NU1510` during build phase; runtime succeeded.
- **Pass/fail:** **Pass**
- **Output truncated:** **No** (captured to `/tmp/coresmoke.log`).

## 5. Outcome
- **Did forced assembly load make serializer visible?** Yes (test now passes).
- **Implication:** Evidence supports **Candidate B**. `Stride.Shaders` AP serialization registration appears present, and lookup reliability depends on assembly load/initialization ordering before serializer resolution.
- Additional AP evidence from build logs: `Stride.Shaders` was processed with `--serialization --parameter-key --auto-module-initializer` and reports `Patch for assembly [Stride.Shaders, ...]`.

## 6. Runtime smoke
- **Command:** `xvfb-run -a ./striv/build/striv-run-coresmoke.sh`
- **Exit code:** `0`
- **First meaningful error:** none (runtime success).
- **Did EffectBytecode serializer error disappear?** Yes (no serializer failure observed).
- **First new blocker:** none encountered.

## 7. Next recommendation
- **Recommended next path:** **runtime assembly-load repair** (ensure `Stride.Shaders` load/initializer execution is deterministic before serializer lookup sites that may run earlier in some host/test paths).
- Keep manual/explicit serializer registration as fallback only if future evidence disproves load-order root cause.
