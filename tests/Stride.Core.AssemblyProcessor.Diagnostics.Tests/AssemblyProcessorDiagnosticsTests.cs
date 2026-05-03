using System.Diagnostics;
using System.Reflection;
using System.Runtime.Loader;
using System.Security.Cryptography;
using Xunit;

namespace Stride.Core.AssemblyProcessor.Diagnostics.Tests;

public class AssemblyProcessorDiagnosticsTests
{
    [Fact]
    public void AssemblyProcessorPayloadInventory_IsComprehensiveAndActionable()
    {
        var report = AssemblyProcessorProbe.ProbeAllPayloads();
        var hasAnyPayload = report.Any(r => r.Exists);
        Assert.True(hasAnyPayload, $"No payloads found under deps/AssemblyProcessor.\n{AssemblyProcessorProbe.FormatReport(report)}");
    }

    [Fact]
    public void AssemblyProcessorModernPayload_net10_CanLoadTaskType()
    {
        var probe = AssemblyProcessorProbe.SelectPreferredProbe("net10.0");
        Assert.NotNull(probe);
        Assert.True(probe!.Exists, "Expected net10.0 payload candidate to exist.");
        Assert.True(probe.LoadSuccess, probe.ToMultilineString());
        Assert.True(probe.AssemblyProcessorTaskFound, probe.ToMultilineString());
    }

    [Fact]
    public void AssemblyProcessorProgram_CanBeInvokedForHelpOrNoArgs()
    {
        var candidate = AssemblyProcessorProbe.SelectPreferredCandidate("net10.0");
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

    public static IEnumerable<(string Tfm, string Path)> PayloadPaths()
    {
        foreach (var candidate in SourceBuiltCandidatePaths())
            yield return candidate;

        var depsRoot = Path.Combine(RepoRoot, "deps/AssemblyProcessor");
        if (Directory.Exists(depsRoot))
        {
            foreach (var dir in Directory.GetDirectories(depsRoot).OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
            {
                var tfm = Path.GetFileName(dir);
                yield return (tfm, Path.Combine(dir, "Stride.Core.AssemblyProcessor.dll"));
            }
        }

        // Explicit required checks
        yield return ("netstandard2.0", Path.Combine(RepoRoot, "deps/AssemblyProcessor/netstandard2.0/Stride.Core.AssemblyProcessor.dll"));
        yield return ("net10.0", Path.Combine(RepoRoot, "deps/AssemblyProcessor/net10.0/Stride.Core.AssemblyProcessor.dll"));
    }

    private static IEnumerable<(string Tfm, string Path)> SourceBuiltCandidatePaths()
    {
        var explicitAssemblyPath = Environment.GetEnvironmentVariable("STRIV_ASSEMBLY_PROCESSOR_PATH");
        if (!string.IsNullOrWhiteSpace(explicitAssemblyPath))
            yield return ("source-env-path", NormalizeAssemblyPath(explicitAssemblyPath!));

        var explicitBasePath = Environment.GetEnvironmentVariable("STRIV_ASSEMBLY_PROCESSOR_BASE_PATH");
        if (!string.IsNullOrWhiteSpace(explicitBasePath))
            yield return ("source-env-base", Path.Combine(explicitBasePath!, "Stride.Core.AssemblyProcessor.dll"));

        yield return ("source-default-net10", Path.Combine(RepoRoot, "sources/core/Stride.Core.AssemblyProcessor/bin/Debug/net10.0/Stride.Core.AssemblyProcessor.dll"));
    }

    private static string NormalizeAssemblyPath(string path)
    {
        if (path.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
            return path;

        return Path.Combine(path, "Stride.Core.AssemblyProcessor.dll");
    }

    public static List<ProbeResult> ProbeAllPayloads() =>
        PayloadPaths()
            .GroupBy(p => p.Path, StringComparer.OrdinalIgnoreCase)
            .Select(g => ProbePath(g.First().Path, g.First().Tfm))
            .OrderBy(r => r.Tfm, StringComparer.OrdinalIgnoreCase)
            .ToList();

    public static ProbeResult? ProbeByTfm(string tfm) =>
        ProbeAllPayloads().FirstOrDefault(p => string.Equals(p.Tfm, tfm, StringComparison.OrdinalIgnoreCase));

    public static ProbeResult? SelectPreferredProbe(string preferredTfm)
    {
        var probes = ProbeAllPayloads();
        var preferred = probes.FirstOrDefault(p => p.Exists && (p.Tfm.Equals("source-env-path", StringComparison.OrdinalIgnoreCase) || p.Tfm.Equals("source-env-base", StringComparison.OrdinalIgnoreCase) || p.Tfm.Equals("source-default-net10", StringComparison.OrdinalIgnoreCase)));
        if (preferred != null)
            return preferred;

        return probes.FirstOrDefault(p => p.Exists && p.Tfm.Equals(preferredTfm, StringComparison.OrdinalIgnoreCase))
               ?? probes.FirstOrDefault(p => p.Exists);
    }

    public static ProbeResult ProbePath(string path, string tfm)
    {
        var result = new ProbeResult { Tfm = tfm, Path = path, Exists = File.Exists(path) };
        if (!result.Exists) return result;

        var bytes = File.ReadAllBytes(path);
        result.Size = bytes.LongLength;
        result.Sha256 = Convert.ToHexString(SHA256.HashData(bytes));
        result.FirstBytesHex = Convert.ToHexString(bytes.Take(32).ToArray());
        result.HasMZHeader = bytes.Length >= 2 && bytes[0] == 'M' && bytes[1] == 'Z';

        var textPrefix = System.Text.Encoding.UTF8.GetString(bytes.Take(Math.Min(bytes.Length, 300)).ToArray());
        result.IsLikelyGitLfsPointer = textPrefix.Contains("version https://git-lfs.github.com/spec/v1", StringComparison.Ordinal);

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

        var loadResult = TryLoadAndFindTaskType(path);
        result.LoadSuccess = loadResult.Success;
        result.LoadMessage = loadResult.Message;
        result.AssemblyProcessorTaskFound = loadResult.TaskTypeFound;

        return result;
    }

    public static string? SelectPreferredCandidate(string preferredTfm)
    {
        var probes = ProbeAllPayloads();
        var preferred = probes.FirstOrDefault(p => p.Exists && p.Tfm.Equals(preferredTfm, StringComparison.OrdinalIgnoreCase));
        return preferred?.Path ?? probes.FirstOrDefault(p => p.Exists)?.Path;
    }

    public static (bool Success, bool TaskTypeFound, string Message) TryLoadAndFindTaskType(string path)
    {
        var loadContext = new AssemblyLoadContext("APDiag", isCollectible: true);
        var resolver = new AssemblyDependencyResolver(path);
        loadContext.Resolving += (_, assemblyName) =>
        {
            var resolvedPath = resolver.ResolveAssemblyToPath(assemblyName);
            if (!string.IsNullOrWhiteSpace(resolvedPath) && File.Exists(resolvedPath))
                return loadContext.LoadFromAssemblyPath(resolvedPath);

            var localCandidate = Path.Combine(Path.GetDirectoryName(path)!, $"{assemblyName.Name}.dll");
            if (File.Exists(localCandidate))
                return loadContext.LoadFromAssemblyPath(localCandidate);

            return null;
        };
        try
        {
            var asm = loadContext.LoadFromAssemblyPath(path);
            var type = asm.GetTypes().FirstOrDefault(t => t.Name == "AssemblyProcessorTask" || t.FullName?.Contains("AssemblyProcessorTask") == true);
            if (type != null)
                return (true, true, $"Found task type: {type.FullName}");

            var nearby = string.Join(", ", asm.GetTypes().Where(t => t.Name.Contains("AssemblyProcessor") || t.Name.Contains("Task")).Take(20).Select(t => t.FullName));
            return (true, false, $"Assembly loaded but task type not found. Nearby types: {nearby}");
        }
        catch (ReflectionTypeLoadException rtle)
        {
            var loaderErrors = string.Join(" | ", rtle.LoaderExceptions.Where(e => e is not null).Select(e => $"{e!.GetType().Name}: {e.Message}"));
            return (false, false, $"ReflectionTypeLoadException while loading task type: {loaderErrors}");
        }
        catch (Exception ex)
        {
            return (false, false, $"Load failure: {ex.GetType().Name}: {ex.Message}");
        }
        finally
        {
            loadContext.Unload();
        }
    }

    public static string FormatReport(IEnumerable<ProbeResult> results) => string.Join("\n\n", results.Select(r => r.ToMultilineString()));
}

internal sealed class ProbeResult
{
    public string Tfm { get; set; } = string.Empty;
    public string Path { get; set; } = string.Empty;
    public bool Exists { get; set; }
    public long? Size { get; set; }
    public string? Sha256 { get; set; }
    public string? FirstBytesHex { get; set; }
    public bool HasMZHeader { get; set; }
    public bool IsLikelyGitLfsPointer { get; set; }
    public bool GetAssemblyNameSuccess { get; set; }
    public string? AssemblyName { get; set; }
    public string? GetAssemblyNameException { get; set; }
    public bool LoadSuccess { get; set; }
    public bool AssemblyProcessorTaskFound { get; set; }
    public string? LoadMessage { get; set; }

    public string ToMultilineString() =>
        $"TFM: {Tfm}\nPath: {Path}\nExists: {Exists}\nSize: {Size}\nSHA256: {Sha256}\nFirstBytesHex: {FirstBytesHex}\nHasMZHeader: {HasMZHeader}\nIsLikelyGitLfsPointer: {IsLikelyGitLfsPointer}\nGetAssemblyNameSuccess: {GetAssemblyNameSuccess}\nAssemblyName: {AssemblyName}\nGetAssemblyNameException: {GetAssemblyNameException}\nLoadSuccess: {LoadSuccess}\nLoadMessage: {LoadMessage}\nAssemblyProcessorTaskFound: {AssemblyProcessorTaskFound}";
}
