using Xunit;
using System.Diagnostics;
using System.Text.Json;

namespace StriV.AssetTool.Tests;

public class AssetToolCliTests
{
    [Fact]
    public async Task Help_Includes_BuildAssets_And_Required_Options()
    {
        var result = await RunToolAsync("--help");
        Assert.Equal(0, result.ExitCode);
        Assert.Contains("build-assets", result.StdOut);

        var buildHelp = await RunToolAsync("build-assets --help");
        Assert.Equal(0, buildHelp.ExitCode);
        Assert.Contains("--manifest", buildHelp.StdOut);
        Assert.Contains("--output", buildHelp.StdOut);
    }

    [Fact]
    public async Task Valid_Manifest_Builds_Successfully()
    {
        var output = Path.Combine(Path.GetTempPath(), "striv-assettool-tests", Guid.NewGuid().ToString("N"));
        var result = await RunToolAsync($"build-assets --manifest {Quote("striv/tests/fixtures/assets/shader_manifest/assets.toml")} --output {Quote(output)}");

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("SUCCESS", result.StdOut);
    }

    [Fact]
    public async Task Invalid_Manifest_Returns_Nonzero()
    {
        var output = Path.Combine(Path.GetTempPath(), "striv-assettool-tests", Guid.NewGuid().ToString("N"));
        var result = await RunToolAsync($"build-assets --manifest {Quote("striv/tests/fixtures/assets/invalid_manifest/assets.toml")} --output {Quote(output)}");

        Assert.NotEqual(0, result.ExitCode);
        Assert.Contains("AM201", result.StdOut);
    }

    [Fact]
    public async Task Jsonl_Diagnostics_Emit_Parseable_Json()
    {
        var output = Path.Combine(Path.GetTempPath(), "striv-assettool-tests", Guid.NewGuid().ToString("N"));
        var result = await RunToolAsync($"build-assets --manifest {Quote("striv/tests/fixtures/assets/invalid_manifest/assets.toml")} --output {Quote(output)} --diagnostics jsonl");
        Assert.NotEqual(0, result.ExitCode);

        var firstJsonLine = result.StdOut.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries)
            .First(line => line.StartsWith('{'));
        using var doc = JsonDocument.Parse(firstJsonLine);
        Assert.Equal("AM201", doc.RootElement.GetProperty("code").GetString());
    }

    private static async Task<(int ExitCode, string StdOut, string StdErr)> RunToolAsync(string arguments)
    {
        var psi = new ProcessStartInfo("dotnet", $"run --project striv/projects/StriV.AssetTool/StriV.AssetTool.csproj -- {arguments}")
        {
            WorkingDirectory = LocateRepoRoot(),
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        using var process = Process.Start(psi)!;
        var stdout = await process.StandardOutput.ReadToEndAsync();
        var stderr = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        return (process.ExitCode, stdout, stderr);
    }

    private static string LocateRepoRoot()
    {
        var current = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(current))
        {
            var candidate = Path.GetFullPath(Path.Combine(current, "..", "..", "..", "..", "..", ".."));
            if (Directory.Exists(Path.Combine(candidate, "striv")))
            {
                return candidate;
            }

            var parent = Directory.GetParent(current);
            if (parent is null)
            {
                break;
            }

            current = parent.FullName;
        }

        throw new DirectoryNotFoundException("Repository root not found.");
    }

    private static string Quote(string path) => $"\"{path}\"";
}
