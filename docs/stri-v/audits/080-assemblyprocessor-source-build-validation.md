# 080 - AssemblyProcessor source-build validation

## 1) Files changed
- `tests/Stride.Core.AssemblyProcessor.Diagnostics.Tests/AssemblyProcessorDiagnosticsTests.cs`
- `docs/stri-v/audits/080-assemblyprocessor-source-build-validation.md`

## 2) Source build result
Commands:
- `dotnet restore sources/core/Stride.Core.AssemblyProcessor/Stride.Core.AssemblyProcessor.csproj` → exit code `0`.
- `dotnet build sources/core/Stride.Core.AssemblyProcessor/Stride.Core.AssemblyProcessor.csproj -c Debug -v minimal` → exit code `0`.

First meaningful warning observed during build:
- `CS1030` from `ObjectIdBuilder.cs` (“PERF: Do not copy byte-for-byte.”).

Output directory used for validation:
- `/workspace/stri-v/sources/core/Stride.Core.AssemblyProcessor/bin/Debug/net10.0/`

Produced payload:
- `Stride.Core.AssemblyProcessor.dll` exists in that folder.

## 3) Source-built payload validation
Validated source-built DLL:
- Path: `/workspace/stri-v/sources/core/Stride.Core.AssemblyProcessor/bin/Debug/net10.0/Stride.Core.AssemblyProcessor.dll`
- Size: `157696` bytes
- SHA256: `a10dbcf6e60f62563edfab4847261a41e0135da8cb7a73374195ebd891531e74`
- First bytes: `4D 5A` (`MZ`), so PE header is present.
- `AssemblyName.GetAssemblyName`: success, `Stride.Core.AssemblyProcessor, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null`.
- `AssemblyLoadContext` test via diagnostics: partial failure without dependency context (`Microsoft.Build.Utilities.Core` / `Mono.Cecil` missing in test load context).
- `AssemblyProcessorTask` type lookup in diagnostics currently fails for the same dependency-resolution reason in test context.
- `dotnet <dll> --help` result: not `Bad IL format`; expected host/runtimeconfig-style error for class library launch attempt.

## 4) M1a command-line override validation
Attempted command (base-path only):
- `dotnet build build/StriV.Core.M1a.slnf -c Debug -v minimal -p:StrideAssemblyProcessorFramework=net10.0 -p:StrideAssemblyProcessorBasePath="/workspace/stri-v/sources/core/Stride.Core.AssemblyProcessor/bin/Debug/net10.0/"`
- Exit code: `1`
- Result: got past prior `Bad IL format`, but failed with `MSB4044` (`StrideEnsureFilesCopied` missing `DestinationFolder`) because `StrideAssemblyProcessorTempBasePath` depends on hash-file derived properties.

Successful narrowed override command:
- `dotnet build build/StriV.Core.M1a.slnf -c Debug -v minimal -p:StrideAssemblyProcessorFramework=net10.0 -p:StrideAssemblyProcessorBasePath="/workspace/stri-v/sources/core/Stride.Core.AssemblyProcessor/bin/Debug/net10.0/" -p:StrideAssemblyProcessorHash=sourcebuild`
- Exit code: `0`
- Result: M1a build succeeded and no longer blocked by LFS pointer `Bad IL format`.

## 5) Diagnostics test update/result
Diagnostics update:
- Added source-built candidate discovery via:
  - `STRIV_ASSEMBLY_PROCESSOR_PATH` (file or directory input)
  - `STRIV_ASSEMBLY_PROCESSOR_BASE_PATH`
  - default source-built fallback path under `sources/core/Stride.Core.AssemblyProcessor/bin/Debug/net10.0/`
- Added candidate preference logic so modern payload checks can target source-built candidates first.

Diagnostics run:
- `dotnet test build/StriV.AssemblyProcessor.Diagnostics.M1b.slnf -c Debug -v normal`
- Current status: 2 passed, 1 failed.
- Failing test: `AssemblyProcessorModernPayload_net10_CanLoadTaskType`.
- Failure is actionable and attributable to dependency resolution in the isolated test `AssemblyLoadContext` rather than invalid IL/pointer payload.

## 6) Durable fix recommendation
Preferred short-term operational path:
1. Source-build `Stride.Core.AssemblyProcessor` as a prerequisite.
2. Build M1a with command-line overrides:
   - `StrideAssemblyProcessorFramework=net10.0`
   - `StrideAssemblyProcessorBasePath=<source-build-output-dir-with-trailing-slash>`
   - `StrideAssemblyProcessorHash=<stable-value>`

Rationale:
- This is narrow, avoids changing broad SDK targets, and bypasses invalid checked-in LFS payloads without deleting or overwriting them.

## 7) Next action recommendation
**Recommend M1b-prep for adding the next core/engine layer**, because M1a now builds using the source-built AssemblyProcessor path and is no longer blocked by `Bad IL format` pointer payload usage.

Follow-up item:
- Keep a focused diagnostics fix task to improve `AssemblyLoadContext` dependency probing for `Microsoft.Build.Utilities.Core`/`Mono.Cecil` so `AssemblyProcessorTask` lookup can pass consistently from the diagnostics harness.
