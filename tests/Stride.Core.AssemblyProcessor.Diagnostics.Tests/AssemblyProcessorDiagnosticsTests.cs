using System.Diagnostics;
using System.Reflection;
using System.Runtime.Loader;
using System.Security.Cryptography;
using Xunit;

namespace Stride.Core.AssemblyProcessor.Diagnostics.Tests;

public class AssemblyProcessorDiagnosticsTests
{
    [Fact]
    public void AssemblyProcessorDependencyPayload_IsPresentOrReportsUsefulMissingPath()
    {
        var report = AssemblyProcessorProbe.ProbeAllCandidates();
        var hasSourceProject = File.Exists(AssemblyProcessorProbe.SourceProjectPath);
        var hasAnyCandidate = report.Any(r => r.Exists);

        var summary = AssemblyProcessorProbe.FormatReport(report);
        Assert.True(hasAnyCandidate || hasSourceProject,
            $"No candidate AssemblyProcessor binaries found and source project missing.\n{summary}");
    }

    [Fact]
    public void AssemblyProcessorBinary_HasValidManagedAssemblyMetadata()
    {
        var candidate = AssemblyProcessorProbe.SelectLikelyBuildCandidate();
        Assert.NotNull(candidate);
        Assert.True(File.Exists(candidate!), $"Candidate does not exist: {candidate}");

        try
        {
            _ = AssemblyName.GetAssemblyName(candidate!);
        }
        catch (Exception ex)
        {
            var details = AssemblyProcessorProbe.ProbePath(candidate!);
            Assert.Fail($"Invalid managed assembly metadata: {ex.GetType().Name}: {ex.Message}\n{details.ToMultilineString()}");
        }
    }

    [Fact]
    public void AssemblyProcessorTask_TypeCanBeLocated()
    {
        var candidate = AssemblyProcessorProbe.SelectLikelyBuildCandidate();
        Assert.NotNull(candidate);

        var result = AssemblyProcessorProbe.TryLoadAndFindTaskType(candidate!);
        Assert.True(result.Success, result.Message);
    }

    [Fact]
    public void AssemblyProcessorProgram_CanBeInvokedForHelpOrNoArgs()
    {
        var candidate = AssemblyProcessorProbe.SelectLikelyBuildCandidate();
        Assert.NotNull(candidate);

        var psi = new ProcessStartInfo("dotnet", $"\"{candidate}\" --help")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false
        };

        using var proc = Process.Start(psi);
        Assert.NotNull(proc);
        proc!.WaitForExit(15000);

        var stdout = proc.StandardOutput.ReadToEnd();
        var stderr = proc.StandardError.ReadToEnd();
        var combined = $"ExitCode={proc.ExitCode}\nSTDOUT:\n{stdout}\nSTDERR:\n{stderr}";

        Assert.True(proc.ExitCode == 0 || !string.IsNullOrWhiteSpace(stdout) || !string.IsNullOrWhiteSpace(stderr),
            $"No actionable process output. {combined}");
    }
}

internal static class AssemblyProcessorProbe
{
    public static string RepoRoot => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));
    public static string SourceProjectPath => Path.Combine(RepoRoot, "sources/core/Stride.Core.AssemblyProcessor/Stride.Core.AssemblyProcessor.csproj");

    public static List<string> CandidatePaths()
    {
        var list = new List<string>
        {
            Path.Combine(RepoRoot, "deps/AssemblyProcessor/netstandard2.0/Stride.Core.AssemblyProcessor.dll"),
            Path.Combine(RepoRoot, "sources/core/Stride.Core.AssemblyProcessor/bin/Debug/netstandard2.0/Stride.Core.AssemblyProcessor.dll")
        };

        var tmp = "/tmp/Stride/AssemblyProcessor";
        if (Directory.Exists(tmp))
        {
            list.AddRange(Directory.GetFiles(tmp, "Stride.Core.AssemblyProcessor.dll", SearchOption.AllDirectories));
        }

        return list.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    public static List<ProbeResult> ProbeAllCandidates() => CandidatePaths().Select(ProbePath).ToList();

    public static ProbeResult ProbePath(string path)
    {
        var result = new ProbeResult { Path = path, Exists = File.Exists(path) };
        if (!result.Exists) return result;

        var bytes = File.ReadAllBytes(path);
        result.Size = bytes.LongLength;
        result.Sha256 = Convert.ToHexString(SHA256.HashData(bytes));
        result.FirstBytesHex = Convert.ToHexString(bytes.Take(32).ToArray());
        result.HasMZHeader = bytes.Length >= 2 && bytes[0] == 'M' && bytes[1] == 'Z';

        try
        {
            var an = AssemblyName.GetAssemblyName(path);
            result.AssemblyName = an.FullName;
            result.GetAssemblyNameSuccess = true;
        }
        catch (Exception ex)
        {
            result.GetAssemblyNameException = $"{ex.GetType().Name}: {ex.Message}";
        }

        return result;
    }

    public static string? SelectLikelyBuildCandidate()
    {
        var probes = ProbeAllCandidates();
        var tmpCandidate = probes.FirstOrDefault(p => p.Exists && p.Path.StartsWith("/tmp/Stride/AssemblyProcessor", StringComparison.OrdinalIgnoreCase));
        return tmpCandidate?.Path ?? probes.FirstOrDefault(p => p.Exists)?.Path;
    }

    public static (bool Success, string Message) TryLoadAndFindTaskType(string path)
    {
        var loadContext = new AssemblyLoadContext("APDiag", isCollectible: true);
        try
        {
            var asm = loadContext.LoadFromAssemblyPath(path);
            var type = asm.GetTypes().FirstOrDefault(t => t.Name == "AssemblyProcessorTask" || t.FullName?.Contains("AssemblyProcessorTask") == true);
            if (type != null)
                return (true, $"Found task type: {type.FullName}");

            var nearby = string.Join(", ", asm.GetTypes().Where(t => t.Name.Contains("AssemblyProcessor") || t.Name.Contains("Task")).Take(20).Select(t => t.FullName));
            return (false, $"Assembly loaded but task type not found. Nearby types: {nearby}");
        }
        catch (ReflectionTypeLoadException rtle)
        {
            var loaderErrors = string.Join(" | ", rtle.LoaderExceptions.Where(e => e is not null).Select(e => $"{e!.GetType().Name}: {e.Message}"));
            return (false, $"ReflectionTypeLoadException while loading task type: {loaderErrors}");
        }
        catch (Exception ex)
        {
            return (false, $"Load failure: {ex.GetType().Name}: {ex.Message}");
        }
        finally
        {
            loadContext.Unload();
        }
    }

    public static string FormatReport(IEnumerable<ProbeResult> results) => string.Join("\n", results.Select(r => r.ToMultilineString()));
}

internal sealed class ProbeResult
{
    public string Path { get; set; } = string.Empty;
    public bool Exists { get; set; }
    public long? Size { get; set; }
    public string? Sha256 { get; set; }
    public string? FirstBytesHex { get; set; }
    public bool HasMZHeader { get; set; }
    public bool GetAssemblyNameSuccess { get; set; }
    public string? AssemblyName { get; set; }
    public string? GetAssemblyNameException { get; set; }

    public string ToMultilineString() =>
        $"Path: {Path}\nExists: {Exists}\nSize: {Size}\nSHA256: {Sha256}\nFirstBytesHex: {FirstBytesHex}\nHasMZHeader: {HasMZHeader}\nGetAssemblyNameSuccess: {GetAssemblyNameSuccess}\nAssemblyName: {AssemblyName}\nGetAssemblyNameException: {GetAssemblyNameException}";
}
