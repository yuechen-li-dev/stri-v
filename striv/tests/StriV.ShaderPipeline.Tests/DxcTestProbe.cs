using System.Diagnostics;

namespace StriV.ShaderPipeline.Tests;

internal sealed class DxcTestProbe
{
    private DxcTestProbe(bool isAvailable, string? executablePath, bool supportsSpirv, string? unavailableReason)
    {
        IsAvailable = isAvailable;
        ExecutablePath = executablePath;
        SupportsSpirv = supportsSpirv;
        UnavailableReason = unavailableReason;
    }

    public bool IsAvailable { get; }
    public string? ExecutablePath { get; }
    public bool SupportsSpirv { get; }
    public string? UnavailableReason { get; }

    public static DxcTestProbe Create()
    {
        var pathEnv = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrWhiteSpace(pathEnv))
            return new(false, null, false, "PATH is empty; could not locate dxc.");

        var candidateNames = OperatingSystem.IsWindows() ? new[] { "dxc.exe", "dxc" } : new[] { "dxc" };
        foreach (var dir in pathEnv.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            foreach (var candidateName in candidateNames)
            {
                var candidate = Path.Combine(dir, candidateName);
                if (!File.Exists(candidate))
                    continue;

                var help = Run(candidate, "--help");
                if (help.ExitCode != 0)
                    return new(false, candidate, false, $"dxc exists at '{candidate}' but '--help' failed with exit code {help.ExitCode}.");

                var combined = string.Concat(help.StdOut, "\n", help.StdErr);
                var supportsSpirv = combined.Contains("-spirv", StringComparison.OrdinalIgnoreCase);
                return new(true, candidate, supportsSpirv, null);
            }
        }

        return new(false, null, false, "dxc was not found on PATH.");
    }

    public ProcessResult Compile(string hlslPath, string profile, string entry, string outputPath, string extraArgs)
    {
        if (!IsAvailable || string.IsNullOrWhiteSpace(ExecutablePath))
            return new(-1, string.Empty, "dxc unavailable");

        var args = $"-T {profile} -E {entry} {extraArgs} \"{hlslPath}\" -Fo \"{outputPath}\"".Trim();
        return Run(ExecutablePath, args);
    }

    private static ProcessResult Run(string fileName, string arguments)
    {
        using var process = new Process();
        process.StartInfo = new ProcessStartInfo
        {
            FileName = fileName,
            Arguments = arguments,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        process.Start();
        var stdOut = process.StandardOutput.ReadToEnd();
        var stdErr = process.StandardError.ReadToEnd();
        process.WaitForExit();

        return new(process.ExitCode, stdOut, stdErr);
    }

    internal readonly record struct ProcessResult(int ExitCode, string StdOut, string StdErr);
}
