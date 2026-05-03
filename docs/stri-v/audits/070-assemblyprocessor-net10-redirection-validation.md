# 070 - AssemblyProcessor net10 redirection validation

## 1. Files changed
- `sources/sdk/Stride.Build.Sdk/Sdk/Stride.AssemblyProcessor.targets`
- `tests/Stride.Core.AssemblyProcessor.Diagnostics.Tests/AssemblyProcessorDiagnosticsTests.cs`
- `docs/stri-v/audits/070-assemblyprocessor-net10-redirection-validation.md`

## 2. Payload inventory
Inventory command: `python` scan of `deps/AssemblyProcessor/*/Stride.Core.AssemblyProcessor.dll`.

| TFM | Exists | Size (bytes) | SHA256 | First bytes (hex) | PE/MZ header | Git LFS pointer | Managed (`AssemblyName.GetAssemblyName`) | Assembly identity | `AssemblyLoadContext` load | `AssemblyProcessorTask` |
|---|---:|---:|---|---|---|---|---|---|---|---|
| net10.0 | yes | 131 | `2A28433292375964EA74030C25D28C045773D9AA65F424D21E6A4DABA21B32F3` | `76657273696F6E2068747470733A2F2F6769742D6C66732E6769746875622E63` | no | yes | fail: `BadImageFormatException: Unknown file format.` | n/a | fail: `BadImageFormatException: Bad IL format` | not found (load failed) |
| net8.0 | yes | 131 | `53ACC7F906D40A7EF9B420B566FF2DB6AF1630675E1F27A5DDF81A6D66A9E8E8` | `76657273696F6E2068747470733A2F2F6769742D6C66732E6769746875622E63` | no | yes | fail: `BadImageFormatException: Unknown file format.` | n/a | fail: bad image format (pointer payload) | not found (load failed) |
| netstandard2.0 | yes | 131 | `8EBF8D5DABF39D90D11E2854FD8E56BF52B1709AA1E742F81A8F0BA06595450B` | `76657273696F6E2068747470733A2F2F6769742D6C66732E6769746875622E63` | no | yes | fail: `BadImageFormatException: Unknown file format.` | n/a | fail: bad image format (pointer payload) | not found (load failed) |

## 3. Command-line property validation
Command:

```bash
dotnet build build/StriV.Core.M1a.slnf -c Debug -v minimal -p:StrideAssemblyProcessorFramework=net10.0
```

- Exit code: `1`.
- Got past old `netstandard2.0` path lookup, but **did not** get past Bad IL: now fails at `/tmp/Stride/AssemblyProcessor/net10.0/.../Stride.Core.AssemblyProcessor.dll` with `MSB4062` Bad IL format.
- First new meaningful error: none; same class of blocker on `net10.0` payload.
- M1a build succeeded: no.

## 4. Durable fix chosen
- Chosen option: **C** (target default update).
- Change: default `StrideAssemblyProcessorFramework` changed from `netstandard2.0` to `net10.0` in `Stride.AssemblyProcessor.targets`.
- Scope: centralized AssemblyProcessor default in Stri-V fork SDK build logic.
- Why acceptable for Stri-V: this hardfork is modern .NET-first and does not require legacy netstandard default behavior.

## 5. Post-fix validation
Commands run:

1. `dotnet restore build/StriV.Core.M1a.slnf`
   - Exit code: `0`
   - First meaningful warning/error: none
   - Classification: pass

2. `dotnet build build/StriV.Core.M1a.slnf -c Debug -v minimal`
   - Exit code: `1`
   - First meaningful error: `MSB4062` loading `/tmp/Stride/AssemblyProcessor/net10.0/.../Stride.Core.AssemblyProcessor.dll` (`Bad IL format`)
   - Classification: fail

3. `dotnet test build/StriV.AssemblyProcessor.Diagnostics.M1b.slnf -c Debug -v normal`
   - Exit code: `1`
   - First meaningful failure: `AssemblyProcessorModernPayload_net10_CanLoadTaskType` reports net10.0 payload is Git LFS pointer (131 bytes), not a managed PE.
   - Classification: fail

## 6. Build result interpretation
- Forcing/using `net10.0` did **not** fix the prior Bad IL format issue.
- M1a core slice still does not build.
- New first blocker: the `net10.0` payload in `deps/AssemblyProcessor/net10.0` is also a Git LFS pointer text file, so AssemblyProcessor task DLL is invalid.
- This is narrower and actionable: payload materialization/LFS checkout is missing for all AssemblyProcessor TFM payloads.

## 7. Next action recommendation
**Recommend payload/source-build repair:** fetch/populate real AssemblyProcessor binaries (resolve Git LFS pointers or build AssemblyProcessor from source and wire output), then rerun M1a build and diagnostics.
