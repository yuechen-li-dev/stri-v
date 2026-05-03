# 410 Visual Studio Source Visibility Props Validation

## 1. Files changed

- `Directory.Build.targets`
- `sources/sdk/Stride.Build.Sdk/Sdk/Sdk.targets`
- `docs/stri-v/audits/410-visual-studio-source-visibility-props-validation.md`

Validation also updated repo-local build outputs under:

- `deps/AssemblyProcessor/net10.0/Stride.Core.AssemblyProcessor.dll`
- `deps/AssemblyProcessor/net10.0/Stride.Core.AssemblyProcessor.dll.hash`
- `deps/AssemblyProcessor/net10.0/Stride.Core.AssemblyProcessor.pdb`

## 2. Why previous fix failed

The previous fix was real from MSBuild's perspective, but too late for the Visual Studio/CPS project tree.

- `Directory.Build.targets` did add normal `Compile` items during design-time outer builds.
- `dotnet msbuild ... -getItem:Compile` showed those source files, but they were defined by `Directory.Build.targets`.
- Visual Studio still showed mostly `Properties`, `References`, `SharedAssemblyInfo.cs`, and explicit `None` folders/items, which is consistent with CPS not using that late repo-level target injection to form the source tree.

What the investigation showed:

- Hypothesis 1 was effectively true: the late `Directory.Build.targets` injection was visible to `msbuild -getItem`, but not sufficient for the VS tree.
- Hypothesis 2 was not enough by itself: moving the exact item logic into `Directory.Build.props` did **not** produce the desired `Compile` items in evaluation.
- Hypothesis 3 was false for the observed SDK behavior: `Microsoft.NET.Sdk.DefaultItems.props` uses `$(DefaultItemExcludes);$(DefaultExcludesInProjectFolder)`. `DefaultItemExcludesInProjectFolder` stayed empty in diagnostics and was not the operative SDK exclusion input here.
- The practical root cause was import timing: the working condition needed `TargetFramework == ''` and `TargetFrameworks != ''`, but the effective place also had to be early enough for CPS and late enough that Stride's target-framework expansion had already happened.

## 3. Fix implemented

The effective fix was **not** `Directory.Build.props`. The working solution moved the design-time source injection into:

- `sources/sdk/Stride.Build.Sdk/Sdk/Sdk.targets`

Placement:

- After `Stride.Frameworks.targets` / `Stride.Platform.targets` / `Stride.Graphics.targets`
- Before `Import Project="Sdk.targets" Sdk="Microsoft.NET.Sdk"`

This gives the injection:

- access to Stride-expanded `TargetFrameworks`
- a stable outer-build discriminator
- earlier evaluation than the old repo-level `Directory.Build.targets` hook

Exact item condition:

```xml
'$(DesignTimeBuild)' == 'true'
And '$(BuildingInsideVisualStudio)' == 'true'
And '$(TargetFramework)' == ''
And '$(TargetFrameworks)' != ''
And '$(MSBuildProjectExtension)' == '.csproj'
```

Injected items:

```xml
<Compile Include="**\*.cs"
         Exclude="$(DefaultItemExcludes);$(DefaultExcludesInProjectFolder)" />
<None Remove="**\*.cs" />
```

Other changes:

- The old repo-level injection was removed from `Directory.Build.targets`, which is now an empty `<Project />`.
- `Visible=false` pack payload items were preserved as-is.

Duplicate-risk handling:

- The injection only runs in the outer design-time build (`TargetFramework == ''` and `TargetFrameworks != ''`).
- Inner-build spot-check with `TargetFramework=net10.0` showed `Compile` items coming from `Microsoft.NET.Sdk.DefaultItems.props`, not from the new Stride SDK injection.
- That confirms normal inner/default compile globs remain authoritative and the new item group does not compete there.

## 4. Item diagnostics

Commands were run with:

- `DesignTimeBuild=true`
- `BuildingInsideVisualStudio=true`
- `-getItem:Compile`
- `-getItem:None`

### Stride.Core

- `Compile` count: `212`
- `None` count: `32`
- First compile items: `..\..\shared\SharedAssemblyInfo.cs`, `AccessorMetadata.cs`, `Annotations\CanBeNullAttribute.cs`, `Annotations\CategoryOrderAttribute.cs`, `Annotations\DataMemberRangeAttribute.cs`
- Outer-build injected source files are now defined by `sources/sdk/Stride.Build.Sdk/Sdk/Sdk.targets`

### Stride.Core.IO

- `Compile` count: `33`
- `None` count: `1`
- First compile items: `..\..\shared\SharedAssemblyInfo.cs`, `AndroidAssetProvider.cs`, `DirectoryWatcher.cs`, `DirectoryWatcher.Desktop.cs`, `DriveFileProvider.cs`

### Stride.Core.Mathematics

- `Compile` count: `52`
- `None` count: `1`
- First compile items: `..\..\shared\SharedAssemblyInfo.cs`, `AngleSingle.cs`, `AngleType.cs`, `BoundingBox.cs`, `BoundingBoxExt.cs`

### Stride.Engine

- `Compile` count: `226`
- `None` count: `1`
- First compile items: `..\..\shared\SharedAssemblyInfo.cs`, `Animations\AnimationBlender.cs`, `Animations\AnimationBlendOperation.cs`, `Animations\AnimationChannel.cs`, `Animations\AnimationClip.cs`

### Inner-build duplicate spot-check

Command:

```powershell
dotnet msbuild sources\core\Stride.Core\Stride.Core.csproj `
  -p:DesignTimeBuild=true `
  -p:BuildingInsideVisualStudio=true `
  -p:TargetFramework=net10.0 `
  -getItem:Compile
```

Result:

- `Compile` items came from `Microsoft.NET.Sdk.DefaultItems.props`
- no duplicate-compile error occurred

## 5. Pack payload visibility

Observed in `Stride.Core` design-time `@(None)`:

- AssemblyProcessor payload files still appear in `@(None)`
- those payload items still carry `Visible=false`
- the repo-local `build/*.props` / `build/*.targets` pack items still carry `Visible=false`

Sample counts for `Stride.Core`:

- hidden `None` items: `29`
- visible `None` items: `3`

The visible `None` items in `Stride.Core` were normal template/source-adjacent files such as the serialization `.ttinclude` / `.tt` items, not AssemblyProcessor payload clutter.

No visible AssemblyProcessor payload clutter was observed in the sampled projects.

## 6. CLI validation

### Restore

Command:

```powershell
dotnet restore build\StriV.Core.slnx
```

Result:

- succeeded
- warnings only:
  - `NU1901` on `NuGet.Packaging` / `NuGet.Protocol` 7.0.0 in existing asset/tasks projects
  - `NU1510` package-pruning warnings in existing engine projects

### Prep script

Command:

```powershell
powershell -ExecutionPolicy Bypass -File build\striv-vs-prepare-core.ps1
```

Result:

- succeeded
- rebuilt the AssemblyProcessor and re-ran restore
- produced existing warning noise in AssemblyProcessor and restore, but no blocking error

### Selected build

Command:

```powershell
dotnet build samples\StriV\CoreSmoke\StriV.CoreSmoke.csproj -c Debug
```

Result:

- failed
- blocking errors observed:
  - `NU5019`: missing `sources/sdk/Stride.Build.Sdk/nuget-icon.png` during pack-related work for `Stride.Core.CompilerServices`
  - `MSB3491`: `Stride.Input.csproj.FileListAbsolute.txt` already exists under `obj\Debug\net10.0\Direct3D11`

This build failure was observed during validation and was not resolved as part of this VS source-visibility task.

## 7. Visual Studio validation instructions

1. Close Visual Studio.
2. If the tree still looks stale, delete the solution `.vs` folder.
3. Run:

```powershell
powershell -ExecutionPolicy Bypass -File build\striv-vs-prepare-core.ps1
```

4. Reopen `build\StriV.Core.slnx`.
5. If individual projects still look stale, unload/reload them.
6. Verify normal source folders/files appear under:
   - `Stride.Core`
   - `Stride.Engine`
   - `Stride.BepuPhysics`
   - `StriV.CoreSmoke`

## 8. Remaining risks

- Visual Studio cache can still mask a good evaluation result until VS is fully closed/reopened.
- CPS project tree behavior can still differ from raw `msbuild -getItem` output, even after earlier injection.
- Design-time-only globs should restore source visibility, but virtual folder shape may still not be perfect in every project.
- Hidden pack payload items still exist in `@(None)` and may remain part of evaluation noise even if they are not visible.
- CLI remains the authoritative correctness path; the VS tree fix is explicitly design-time/UI scoped.
- The selected `CoreSmoke` build still has unrelated validation failures that were not addressed here.

## 9. Recommended next task

Recommended next task: **local Visual Studio validation**

Reason:

- the MSBuild evidence now matches the intended CPS-facing fix location
- the remaining question is whether the local VS cache/project-tree layer picks it up as expected
- that is the shortest path to confirming whether we can move on or need one more CPS-specific adjustment
