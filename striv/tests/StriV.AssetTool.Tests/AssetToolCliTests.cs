using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Xunit;

namespace StriV.AssetTool.Tests;

public class AssetToolCliTests
{
    private static readonly TimeSpan ProcessTimeout = TimeSpan.FromSeconds(20);

    [Fact]
    public async Task Help_RendersBuiltInSystemCommandLineHelp()
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
    public async Task BuildAssets_ValidManifest_ReturnsZero()
    {
        var output = Path.Combine(Path.GetTempPath(), "striv-assettool-tests", Guid.NewGuid().ToString("N"));
        var result = await RunToolAsync($"build-assets --manifest {Quote("striv/tests/fixtures/assets/shader_manifest/assets.toml")} --output {Quote(output)}");

        Assert.Equal(0, result.ExitCode);
        Assert.Contains("OK shader.sprite_batch", result.StdOut);
        Assert.True(File.Exists(Path.Combine(output, "shaders", "shader.sprite_batch", "manifest.json")));
    }

    [Fact]
    public async Task BuildAssets_QuietSuppressesSuccessText()
    {
        var output = Path.Combine(Path.GetTempPath(), "striv-assettool-tests", Guid.NewGuid().ToString("N"));
        var result = await RunToolAsync($"build-assets --manifest {Quote("striv/tests/fixtures/assets/shader_manifest/assets.toml")} --output {Quote(output)} --quiet");

        Assert.Equal(0, result.ExitCode);
        Assert.DoesNotContain("OK shader.sprite_batch", result.StdOut);
        Assert.DoesNotContain("SUCCESS:", result.StdOut);
    }

    [Fact]
    public async Task BuildAssets_InvalidManifest_ReturnsNonZeroAndJsonlDiagnostic()
    {
        var output = Path.Combine(Path.GetTempPath(), "striv-assettool-tests", Guid.NewGuid().ToString("N"));
        var result = await RunToolAsync($"build-assets --manifest {Quote("striv/tests/fixtures/assets/invalid_manifest/assets.toml")} --output {Quote(output)} --diagnostics jsonl");
        Assert.Equal(1, result.ExitCode);

        var jsonRecords = result.StdOut
            .Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries)
            .Where(line => line.StartsWith('{'))
            .Select(line => JsonDocument.Parse(line))
            .ToList();

        Assert.Contains(jsonRecords, record => record.RootElement.TryGetProperty("code", out var code) && code.GetString() == "AM201");

        foreach (var record in jsonRecords)
            record.Dispose();
    }

    [Fact]
    public async Task BuildAssets_JsonlCanEmitArtifactRecord()
    {
        var output = Path.Combine(Path.GetTempPath(), "striv-assettool-tests", Guid.NewGuid().ToString("N"));
        var result = await RunToolAsync($"build-assets --manifest {Quote("striv/tests/fixtures/assets/shader_manifest/assets.toml")} --output {Quote(output)} --diagnostics jsonl");
        Assert.Equal(0, result.ExitCode);

        var jsonRecords = result.StdOut
            .Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries)
            .Where(line => line.StartsWith('{'))
            .Select(line => JsonDocument.Parse(line))
            .ToList();

        Assert.Contains(jsonRecords, record =>
            record.RootElement.TryGetProperty("kind", out var kind)
            && kind.GetString() == "artifact"
            && record.RootElement.GetProperty("id").GetString() == "shader.sprite_batch");

        foreach (var record in jsonRecords)
            record.Dispose();
    }

    private static async Task<(int ExitCode, string StdOut, string StdErr)> RunToolAsync(string arguments)
    {
        var repoRoot = LocateRepoRoot();
        var toolDllPath = LocateBuiltAssetToolDll(repoRoot);

        var psi = new ProcessStartInfo("dotnet", $"{Quote(toolDllPath)} {arguments}")
        {
            WorkingDirectory = repoRoot,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = psi };
        process.Start();

        var stdoutTask = process.StandardOutput.ReadToEndAsync();
        var stderrTask = process.StandardError.ReadToEndAsync();

        var exited = await WaitForExitAsync(process, ProcessTimeout);
        if (!exited)
        {
            TryKill(process);
            var partialStdOut = await stdoutTask;
            var partialStdErr = await stderrTask;
            throw new TimeoutException($"AssetTool process timed out after {ProcessTimeout.TotalSeconds:0}s.\nSTDOUT:\n{partialStdOut}\nSTDERR:\n{partialStdErr}");
        }

        var stdout = await stdoutTask;
        var stderr = await stderrTask;
        return (process.ExitCode, stdout, stderr);
    }

    private static string LocateBuiltAssetToolDll(string repoRoot)
    {
        var dllPath = Path.Combine(repoRoot, "striv", "projects", "StriV.AssetTool", "bin", "Debug", "net10.0", "StriV.AssetTool.dll");
        if (!File.Exists(dllPath))
            throw new FileNotFoundException("Built StriV.AssetTool.dll not found. Ensure project references build it before tests.", dllPath);

        return dllPath;
    }

    private static string LocateRepoRoot()
    {
        var current = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(current))
        {
            if (Directory.Exists(Path.Combine(current, "striv")))
                return current;

            var parent = Directory.GetParent(current);
            if (parent is null)
                break;

            current = parent.FullName;
        }

        throw new DirectoryNotFoundException("Repository root not found.");
    }

    private static async Task<bool> WaitForExitAsync(Process process, TimeSpan timeout)
    {
        var waitTask = process.WaitForExitAsync();
        var completed = await Task.WhenAny(waitTask, Task.Delay(timeout));
        if (completed != waitTask)
            return false;

        await waitTask;
        return true;
    }

    private static void TryKill(Process process)
    {
        try
        {
            if (!process.HasExited)
                process.Kill(entireProcessTree: true);
        }
        catch
        {
            // best effort
        }
    }

    private static string Quote(string path) => $"\"{path}\"";
}
