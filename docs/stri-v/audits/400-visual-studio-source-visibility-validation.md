# 400 Visual Studio Source Visibility Validation

## 1. Files changed

- `C:\Users\yuech\source\repos\stri-v\Directory.Build.targets`
- `C:\Users\yuech\source\repos\stri-v\sources\core\Stride.Core\Stride.Core.csproj`
- `C:\Users\yuech\source\repos\stri-v\sources\sdk\Stride.Build.Sdk\Stride.Build.Sdk.csproj`

Generated-but-not-intended changes were also produced by the prep workflow under `deps/AssemblyProcessor/` and should not be committed as part of this fix.

## 2. Problem recap

The Stri-V Core profile now restores and opens `build/StriV.Core.slnx` in Visual Studio more reliably than before, so the restore/build profile story is materially improved.

However, Visual Studio still did not present the normal source tree for many Stri-V Core projects in Solution Explorer. Projects such as `Stride.Core`, `Stride.Core.IO`, and `Stride.Core.Mathematics` were showing mostly structural or packaging-related nodes such as `Properties`, `References`, `build`, `Serialization`, and AssemblyProcessor payload files like `Mono.Cecil.dll`, `Stride.Core.AssemblyProcessor.dll`, `.deps.json`, `.hash`, and `.pdb`.

That symptom points much more strongly at project item discovery during design-time evaluation than at the normal CLI restore/build path. The question was not whether the projects could compile in a real target framework build, but whether the project system could discover the normal `Compile` items that should populate Solution Explorer.

## 3. Item discovery audit

### Audited files

- `C:\Users\yuech\source\repos\stri-v\Directory.Build.props`
- `C:\Users\yuech\source\repos\stri-v\build\StriV.Core.props`
- `C:\Users\yuech\source\repos\stri-v\sources\Directory.Build.props`
- `C:\Users\yuech\source\repos\stri-v\sources\core\Stride.Core\Stride.Core.csproj`
- `C:\Users\yuech\source\repos\stri-v\sources\core\Stride.Core.IO\Stride.Core.IO.csproj`
- `C:\Users\yuech\source\repos\stri-v\sources\core\Stride.Core.Mathematics\Stride.Core.Mathematics.csproj`
- `C:\Users\yuech\source\repos\stri-v\sources\core\Stride.Core.Serialization\Stride.Core.Serialization.csproj`
- `C:\Users\yuech\source\repos\stri-v\sources\sdk\Stride.Build.Sdk\Sdk\Sdk.props`
- `C:\Users\yuech\source\repos\stri-v\sources\sdk\Stride.Build.Sdk\Sdk\Sdk.targets`
- `C:\Users\yuech\source\repos\stri-v\sources\sdk\Stride.Build.Sdk\Sdk\*.props`
- `C:\Users\yuech\source\repos\stri-v\sources\sdk\Stride.Build.Sdk\Sdk\*.targets`
- `C:\Users\yuech\source\repos\stri-v\sources\core\Stride.Core\build\Stride.Core.targets`
- `C:\Users\yuech\source\repos\stri-v\sources\sdk\Stride.Build.Sdk\Stride.Build.Sdk.csproj`

### Default item settings

For the cross-targeting outer build of `Stride.Core`, these properties came back empty:

```powershell
dotnet msbuild sources\core\Stride.Core\Stride.Core.csproj `
  -getProperty:EnableDefaultItems `
  -getProperty:EnableDefaultCompileItems `
  -getProperty:EnableDefaultNoneItems
```

Observed result:

- `EnableDefaultItems = ""`
- `EnableDefaultCompileItems = ""`
- `EnableDefaultNoneItems = ""`

For an inner build with an explicit target framework, the same project reported the expected SDK defaults:

```powershell
dotnet msbuild sources\core\Stride.Core\Stride.Core.csproj `
  -p:TargetFramework=net10.0 `
  -getProperty:EnableDefaultItems `
  -getProperty:EnableDefaultCompileItems `
  -getProperty:EnableDefaultNoneItems
```

Observed result:

- `EnableDefaultItems = true`
- `EnableDefaultCompileItems = true`
- `EnableDefaultNoneItems = true`

This shows that default SDK items are not globally disabled for the project. They are present in the inner build, but not materially represented in the outer cross-targeting evaluation that Visual Studio appears to be using for tree population.

### `@(Compile)` visibility

Before the fix, the outer build of `Stride.Core` exposed only the explicitly declared shared file:

```powershell
dotnet msbuild sources\core\Stride.Core\Stride.Core.csproj -getItem:Compile
```

Observed result before fix:

- `..\..\shared\SharedAssemblyInfo.cs`

The same was true for an explicitly design-time flavored evaluation:

```powershell
dotnet msbuild sources\core\Stride.Core\Stride.Core.csproj `
  -p:DesignTimeBuild=true `
  -p:BuildingInsideVisualStudio=true `
  -getItem:Compile
```

Observed result before fix:

- `..\..\shared\SharedAssemblyInfo.cs`

That explains why Solution Explorer had almost no normal source files to show.

By contrast, the inner build did contain the expected source tree:

```powershell
dotnet msbuild sources\core\Stride.Core\Stride.Core.csproj `
  -p:TargetFramework=net10.0 `
  -getItem:Compile
```

Observed result:

- Normal `Stride.Core` source files appeared in `@(Compile)` from SDK default item evaluation.

After the fix, the design-time outer build also exposes the normal source tree:

```powershell
dotnet msbuild sources\core\Stride.Core\Stride.Core.csproj `
  -p:DesignTimeBuild=true `
  -p:BuildingInsideVisualStudio=true `
  -getItem:Compile
```

Observed result after fix:

- `..\..\shared\SharedAssemblyInfo.cs`
- Normal local files such as `AccessorMetadata.cs`, `AnonymousDisposable.cs`, `Utilities.cs`, and the expected recursive `Annotations`, `Collections`, `IO`, `Reflection`, `Serialization`, `Settings`, `Storage`, `Threading`, and `Unsafe` sources

Spot checks on `Stride.Core.IO` and `Stride.Core.Mathematics` showed the same corrected behavior under design-time evaluation.

### `@(None)` and packaging payload visibility

`Stride.Core.csproj` was explicitly adding packaging items as `None`:

- `build\**\*.targets`
- `build\**\*.props`
- `..\..\..\deps\AssemblyProcessor\**\*.*`

Those items are exactly consistent with the Visual Studio symptom. The `None` list included visible package payloads such as:

- `Mono.Cecil.dll`
- `Mono.Cecil.Pdb.dll`
- `Stride.Core.AssemblyProcessor.dll`
- `Stride.Core.AssemblyProcessor.deps.json`
- `Stride.Core.AssemblyProcessor.dll.hash`
- `Stride.Core.AssemblyProcessor.pdb`

`Stride.Build.Sdk.csproj` had a similar pattern for:

- `Sdk/**/*.props`
- `Sdk/**/*.targets`
- `Sdk/Stride.ruleset`
- `../../../deps/AssemblyProcessor/**`

Those items were valid for packing, but they were also eligible to clutter the project tree.

### Relevant item owners

The main item behavior came from three places:

- `Microsoft.NET.Sdk.DefaultItems.props`
  This provides the normal SDK default `Compile` and `None` globs during inner builds.
- `Stride.Core.csproj`
  This explicitly adds shared compile items and pack-only `None` items for build props/targets and AssemblyProcessor payload.
- `Stride.Build.Sdk.csproj`
  This explicitly adds pack-only `None` items for SDK files and AssemblyProcessor payload.

I did not find evidence that Stri-V Core had globally disabled SDK default compile items in a simple, direct way. The evidence instead points to outer-build item visibility not surfacing those defaults in the project tree used by Visual Studio.

## 4. Root cause

The likely root cause is a design-time cross-targeting mismatch.

`Stride.Core` and the related Core projects still get their normal source files through SDK default `Compile` items in real target-framework builds, but the cross-targeting outer build used for project-system visibility was exposing almost none of those files. In that outer build, `@(Compile)` effectively collapsed to the explicitly declared `SharedAssemblyInfo.cs`.

At the same time, several pack-oriented `None` item groups were explicitly present and visible, especially AssemblyProcessor payload files and SDK/build props/targets. That caused Solution Explorer to show packaging artifacts prominently while the normal source tree was mostly absent.

So the issue was not "source files removed from the project" in the CLI sense. It was "source files not materialized into the design-time visible item set that Solution Explorer relies on."

## 5. Fix implemented

### Smallest functional fix

I added a repo-local `Directory.Build.targets` with a narrowly scoped design-time outer-build item group:

```xml
<Project>
  <ItemGroup Condition="'$(DesignTimeBuild)' == 'true' and '$(TargetFramework)' == '' and '$(TargetFrameworks)' != '' and '$(MSBuildProjectExtension)' == '.csproj'">
    <Compile Include="**\*.cs"
             Exclude="$(DefaultItemExcludes);$(DefaultExcludesInProjectFolder)" />
    <None Remove="**\*.cs" />
  </ItemGroup>
</Project>
```

Why this scope was chosen:

- `DesignTimeBuild == true`
  Limits the behavior to design-time evaluation.
- `TargetFramework == ''` and `TargetFrameworks != ''`
  Restricts it to the cross-targeting outer build, not the real inner build.
- `MSBuildProjectExtension == '.csproj'`
  Avoids affecting non-C# project types.

This makes normal `*.cs` files visible to the Visual Studio project tree in the specific evaluation mode where they were previously missing, while leaving the existing CLI golden path intact.

### AssemblyProcessor and pack file demotion

I also marked explicit pack-only `None` items as invisible in the project tree:

- In `sources/core/Stride.Core/Stride.Core.csproj`
- In `sources/sdk/Stride.Build.Sdk/Stride.Build.Sdk.csproj`

The change was:

```xml
<Visible>false</Visible>
```

applied via attribute form on the explicit `None Include=...` entries.

This keeps the files packable and available to the build/package pipeline, but reduces their prominence in Solution Explorer.

### Why this is the smallest fix

- It does not re-enable shader compiler, audio, or VR.
- It does not remove AssemblyProcessor.
- It does not delete or rewrite package payloads.
- It does not alter the inner build compile graph that already works for CLI restore/build.
- It is scoped to design-time visibility and explicit pack-item presentation.

## 6. Validation commands

### Ran successfully

```powershell
dotnet msbuild sources\core\Stride.Core\Stride.Core.csproj -p:DesignTimeBuild=true -p:BuildingInsideVisualStudio=true -getItem:Compile
dotnet msbuild sources\core\Stride.Core\Stride.Core.csproj -p:TargetFramework=net10.0 -getItem:None
dotnet msbuild sources\core\Stride.Core\Stride.Core.csproj -p:TargetFramework=net10.0 -getProperty:EnableDefaultItems -getProperty:EnableDefaultCompileItems -getProperty:EnableDefaultNoneItems
dotnet restore build\StriV.Core.slnx
powershell -ExecutionPolicy Bypass -File build\striv-vs-prepare-core.ps1
```

Observed outcomes:

- Design-time `@(Compile)` now contains the normal source tree.
- Inner-build `@(None)` still contains pack payload items, but the explicit pack-only items now carry `Visible=false`.
- `dotnet restore build\StriV.Core.slnx` succeeded.
- `build\striv-vs-prepare-core.ps1` succeeded and rebuilt the AssemblyProcessor payload used by the current workflow.

### Attempted but environment-blocked

The requested shell-script validations were attempted, but the bash environment on this machine did not have `dotnet` on `PATH`:

```powershell
bash build/striv-build-coresmoke-m1g.sh
bash build/striv-prepare-core.sh
```

Observed outcome:

- failed with `dotnet: command not found`

Because the failure was the shell environment rather than MSBuild project logic, this does not currently contradict the Visual Studio item-discovery fix. CLI `dotnet restore` from PowerShell remained successful and is still the authoritative local signal here.

## 7. Visual Studio validation instructions

Use this exact sequence for local validation:

1. Close Visual Studio completely.
2. Run:

   ```powershell
   powershell -ExecutionPolicy Bypass -File C:\Users\yuech\source\repos\stri-v\build\striv-vs-prepare-core.ps1
   ```

3. If Visual Studio still shows stale project trees, delete:

   - `C:\Users\yuech\source\repos\stri-v\.vs\`

4. Reopen:

   - `C:\Users\yuech\source\repos\stri-v\build\StriV.Core.slnx`

5. If a project still looks stale, unload and reload that project once.
6. Check these projects in Solution Explorer:

   - `Stride.Core`
   - `Stride.Engine`
   - `Stride.BepuPhysics`
   - `StriV.CoreSmoke`

7. In particular, confirm that normal `.cs` files now appear under `Stride.Core`, `Stride.Core.IO`, `Stride.Core.Mathematics`, and related folders instead of seeing mostly payload binaries and package files.

## 8. Remaining risks

- Visual Studio caches project tree state aggressively, so a stale `.vs` cache can mask a correct MSBuild fix.
- Some source visibility issues may not resolve until the solution or individual projects are reloaded.
- Some package/tool files may still appear in projects that add them through other item groups not covered by this narrow fix.
- Hiding package files too broadly could interfere with pack expectations, so this change only marked explicit pack-oriented items invisible instead of removing them.
- CLI evaluation remains the authoritative signal for actual build correctness; this fix is specifically about Visual Studio design-time item visibility.
- The prep script dirties `deps/AssemblyProcessor` outputs locally; those generated binaries should not be folded into the source-visibility change set.

## 9. Recommended next task

Local Visual Studio validation by the user.

That is the highest-value next step because the design-time `@(Compile)` evidence is now aligned with the intended source tree, and the remaining uncertainty is Visual Studio cache/project-system behavior rather than missing MSBuild items.
