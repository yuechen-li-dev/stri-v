# 450 - Clean AssemblyProcessor Target Validation

## 1) Files changed
- `striv/build/StriV.AssemblyProcessor.targets`
- `striv/projects/Stride.Core.AssemblyProcessor/Stride.Core.AssemblyProcessor.csproj`

## 2) AP failure recap
- Project: `striv/projects/Stride.Core/Stride.Core.csproj`
- Target: `StriVRunAssemblyProcessor` (`striv/build/StriV.AssemblyProcessor.targets`)
- Original exit code: `131`
- Original symptom: `dotnet <AP.dll> <Stride.Core.dll> --auto-module-initializer --serialization` failed before AP logic with missing `libhostpolicy.so` and missing `Stride.Core.AssemblyProcessor.runtimeconfig.json`.

## 3) Exact command captured
Captured command after observability updates:

```text
dotnet "/workspace/stri-v/striv/projects/Stride.Core.AssemblyProcessor/bin/Debug/net10.0/Stride.Core.AssemblyProcessor.dll" "/workspace/stri-v/striv/projects/Stride.Core/bin/Debug/net10.0/Stride.Core.dll" --auto-module-initializer --serialization --references-file="obj/Debug/net10.0/StriV.AP.references.cache"
```

Captured metadata in build log:
- AP DLL path
- target assembly path
- project name
- target framework
- configuration
- AP options
- working directory
- reference list path and `@(ReferencePath)` contents

## 4) Manual AP run result
### 4.1 Original failing invocation (before fix)
Command:
```bash
dotnet striv/projects/Stride.Core.AssemblyProcessor/bin/Debug/net10.0/Stride.Core.AssemblyProcessor.dll striv/projects/Stride.Core/bin/Debug/net10.0/Stride.Core.dll --auto-module-initializer --serialization
```
Result:
- Exit code: `131`
- stderr: fatal host error (`libhostpolicy.so`) because runtime config was missing.
- First failure text: AP was treated as self-contained due to missing `Stride.Core.AssemblyProcessor.runtimeconfig.json`.

### 4.2 Invocation after runtime fix but before reference fix
Same command, after AP project OutputType change:
- Exit code: `1`
- stdout/stderr summary: AP started (`Patch for assembly [...]`) then threw:
  - `Mono.Cecil.AssemblyResolutionException: Failed to resolve assembly: 'ServiceWire, Version=5.6.0.0, ...'`
- First stack frame: `CustomAssemblyResolver.Resolve(...)` in `sources/core/Stride.Core.AssemblyProcessor/CustomAssemblyResolver.cs:137`.

## 5) Old target comparison
- Old target (`sources/sdk/Stride.Build.Sdk/Sdk/Stride.AssemblyProcessor.targets`) passes extra reference context via `--references-file` and `--add-reference=...`, and builds those from `@(ReferencePath)`.
- Clean target initially only passed positional input assembly + basic options.
- Key missing behavior in clean target: no references file (`--references-file`) for Cecil resolver context.
- Additional runtime packaging difference in clean AP project: AP built as `Library` and lacked runtime config needed for `dotnet <dll>` execution.

Likely/observed causes:
1. Missing runtime config => exit `131` host failure.
2. Missing references context => assembly resolution failure for `ServiceWire`.

## 6) Fix implemented
### `striv/projects/Stride.Core.AssemblyProcessor/Stride.Core.AssemblyProcessor.csproj`
- Changed `<OutputType>Library</OutputType>` to `<OutputType>Exe</OutputType>` so build emits runtime config and AP can run via `dotnet <dll>`.

### `striv/build/StriV.AssemblyProcessor.targets`
- Added high-importance diagnostic messages for AP command and context.
- Added `WriteLinesToFile` for `@(ReferencePath)` to `obj/.../StriV.AP.references.cache`.
- Appended `--references-file="...StriV.AP.references.cache"` to AP options.
- Quoted `$(TargetPath)` in command.
- Set explicit AP working directory (`$(MSBuildProjectDirectory)`).

This is minimal: no re-import of old SDK targets, no use of `deps/AssemblyProcessor`, no global AP disable.

## 7) Validation results
1. `./striv/build/striv-build-core.sh`
   - Exit: `1` (initial run)
   - First meaningful error: `MSB3073` AP command exited `131`.
   - Pass/Fail: Fail
   - Output truncated: yes

2. `dotnet striv/projects/Stride.Core.AssemblyProcessor/bin/Debug/net10.0/Stride.Core.AssemblyProcessor.dll --help || true`
   - Exit: shell command forced success with `|| true`; AP process itself failed.
   - First meaningful error: missing `Stride.Core.AssemblyProcessor.runtimeconfig.json` / `libhostpolicy.so`.
   - Pass/Fail: Fail (tool runtime not runnable in original state)
   - Output truncated: no

3. `dotnet striv/projects/Stride.Core.AssemblyProcessor/bin/Debug/net10.0/Stride.Core.AssemblyProcessor.dll striv/projects/Stride.Core/bin/Debug/net10.0/Stride.Core.dll --auto-module-initializer --serialization; echo EXIT:$?`
   - Exit: `131` (original), then `1` (after runtime fix)
   - First meaningful error after runtime fix: `AssemblyResolutionException` for `ServiceWire`.
   - Pass/Fail: Fail (before reference fix)
   - Output truncated: no

4. `./striv/build/striv-build-core.sh` (after fixes)
   - Exit: `1`
   - First meaningful error: new non-AP compile blocker in `Stride.Core.IO` (`Android`/`Context` types missing).
   - Pass/Fail: Partial pass (AP blocker repaired; graph moved forward to next blocker)
   - Output truncated: yes

## 8) First new blocker
- Project: `striv/projects/Stride.Core.IO/Stride.Core.IO.csproj`
- File/errors:
  - `sources/core/Stride.Core.IO/System.IO.Compression.Zip/ApkExpansionSupport.cs(19,11): CS0246 Android`
  - `sources/core/Stride.Core.IO/System.IO.Compression.Zip/ApkExpansionSupport.cs(20,11): CS0246 Android`
  - `sources/core/Stride.Core.IO/System.IO.Compression.Zip/ApkExpansionSupport.cs(53,64): CS0246 Context`
  - `sources/core/Stride.Core.IO/System.IO.Compression.Zip/ApkExpansionSupport.cs(101,63): CS0246 Context`
- Likely cause: Android-specific source compiled in Linux desktop clean graph without Android reference/TFM guards.
- Smallest next repair: add conditional compile exclusion or platform/TFM guard for Android-only source in clean `Stride.Core.IO` project.

## 9) Test results
- `dotnet test striv/tests/StriV.CleanGraph.Tests/StriV.CleanGraph.Tests.csproj` not run.
- Reason: stopped at first new blocker per instruction.

## 10) Worktree status
```bash
git status --short
```
Output:
```text
 M striv/build/StriV.AssemblyProcessor.targets
 M striv/projects/Stride.Core.AssemblyProcessor/Stride.Core.AssemblyProcessor.csproj
```

## 11) Recommended next task
Next clean graph repair: fix `Stride.Core.IO` Android-only compile inclusion for `net10.0` desktop clean profile (small csproj include/exclude/condition adjustment).
