# Stri-V Asset Tool CLI (M5c) Validation

## 1) Files changed
- `sources/Directory.Packages.props`
- `striv/StriV.Core.slnx`
- `striv/projects/StriV.AssetTool/StriV.AssetTool.csproj`
- `striv/projects/StriV.AssetTool/Program.cs`
- `striv/projects/StriV.AssetTool/CliDiagnosticFormatter.cs`
- `striv/tests/StriV.AssetTool.Tests/StriV.AssetTool.Tests.csproj`
- `striv/tests/StriV.AssetTool.Tests/AssetToolCliTests.cs`
- `striv/tests/fixtures/assets/invalid_manifest/assets.toml`

## 2) CLI design
- Project path: `striv/projects/StriV.AssetTool/StriV.AssetTool.csproj`.
- Command structure:
  - root command: `StriV.AssetTool`
  - subcommand: `build-assets`
- Options:
  - required: `--manifest <path>`, `--output <path>`
  - optional: `--diagnostics <text|json|jsonl>`, `--strict-dxc`, `--no-dxc`, `--verbose`
- `System.CommandLine` usage:
  - `RootCommand`, `Command`, typed `Option<T>`, and `SetAction` are used.
  - no manual argument token parsing.
- Help generation policy:
  - uses built-in `System.CommandLine` help (`--help`) only.
  - no custom usage banner implemented.

## 3) Asset pipeline integration
- Manifest and output paths are consumed via typed options and normalized using `FileInfo/DirectoryInfo` full paths.
- CLI loads TOML via `AssetManifestParser.Parse(...)` and executes `AssetPipelineRunner.BuildShaders(...)`.
- Build results are emitted as `OK <shaderId> -> <manifest.json path>` lines.
- Diagnostics are passed through `AssetDiagnostic` and formatted in text/json/jsonl output.
- Exit code policy:
  - `0` when no fatal diagnostics.
  - `1` when fatal parse/validation/build diagnostics exist.
  - `2` for missing manifest path.

## 4) Diagnostics formatting
- `text`: `ERROR AM201 <source>:<line>:<column> <message>` style.
- `jsonl`: one JSON object per line using pipeline diagnostic fields.
- `json`: single JSON array of diagnostic objects (implemented as optional extra).
- Fatal/nonfatal handling:
  - fatal diagnostics drive nonzero exit status.
  - nonfatal diagnostics are emitted but do not fail process by themselves.

## 5) Tests/smoke coverage
- Added CLI test project and tests covering:
  1) help includes `build-assets`, `--manifest`, `--output`
  2) valid fixture manifest success
  3) invalid manifest nonzero exit
  4) jsonl output emits parseable JSON with expected code
- Ran required smoke commands for root help, command help, valid build, and jsonl mode.

## 6) Validation results
| Command | Exit | First meaningful warning/error | Pass/Fail | Output truncated |
|---|---:|---|---|---|
| `dotnet test striv/tests/StriV.AssetPipeline.Tests/StriV.AssetPipeline.Tests.csproj` | 0 | `CS8604` warning in existing AssetPipeline source | Pass | No |
| `dotnet test striv/tests/StriV.ShaderPipeline.Tests/StriV.ShaderPipeline.Tests.csproj --no-build -v minimal` | 0 | none surfaced | Pass | No |
| `dotnet test striv/tests/StriV.CleanGraph.Tests/StriV.CleanGraph.Tests.csproj --no-build -v minimal` | 0 | none surfaced | Pass | No |
| `dotnet run --project striv/projects/StriV.AssetTool/StriV.AssetTool.csproj -- --help` | 0 | none | Pass | No |
| `dotnet run --project striv/projects/StriV.AssetTool/StriV.AssetTool.csproj -- build-assets --help` | 0 | none | Pass | No |
| `dotnet run --project striv/projects/StriV.AssetTool/StriV.AssetTool.csproj -- build-assets --manifest striv/tests/fixtures/assets/shader_manifest/assets.toml --output /tmp/striv-asset-tool-smoke` | 0 | none | Pass | No |
| `dotnet run --project striv/projects/StriV.AssetTool/StriV.AssetTool.csproj -- build-assets --manifest striv/tests/fixtures/assets/shader_manifest/assets.toml --output /tmp/striv-asset-tool-smoke-jsonl --diagnostics jsonl` | 0 | none | Pass | No |
| `dotnet run --project striv/projects/StriV.AssetTool/StriV.AssetTool.csproj -- build-assets --manifest striv/tests/fixtures/assets/invalid_manifest/assets.toml --output /tmp/striv-asset-tool-invalid --diagnostics jsonl` | 1 | `AM201` missing required field | Pass | No |
| `./striv/build/striv-build-core.sh` | 0 | existing repo warnings (nullability/#warning) | Pass | Yes |

## 7) Limitations
- Shader assets only.
- No watch mode.
- No incremental cache.
- No runtime/editor integration.
- No material/texture/scene assets.
- DXC remains optional unless `--strict-dxc` is used.

## 8) Recommended next task
- **CLI polish**: add richer summary modes, optional quiet mode, and stable machine-readable success records for CI integration.
