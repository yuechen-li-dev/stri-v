using System.Linq;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Stride.BepuPhysics;
using Stride.Engine;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Core.Serialization;
using Stride.Core.Reflection;
using Stride.Core.Serialization.Contents;
using Stride.Shaders;
using Xunit;

namespace StriV.CleanGraph.Tests;

public class CleanGraphSmokeTests
{
    private static readonly TimeSpan ProcessTimeout = TimeSpan.FromSeconds(30);

    [Fact]
    public void AssemblyIdentityTypesAreReachable()
    {
        Assert.Equal("Stride.Core.Utilities", typeof(Utilities).FullName);
        Assert.Equal("Stride.Core.Mathematics.Vector3", typeof(Vector3).FullName);
    }

    [Fact]
    public void CleanProfileConstantsAreDefined()
    {
#if STRIDE_PLATFORM_LINUX && STRIDE_UI_SDL && STRIDE_GRAPHICS_API_VULKAN && STRIDE_ENGINE_WITHOUT_SHADER_COMPILER && STRIDE_ENGINE_WITHOUT_AUDIO && STRIDE_ENGINE_WITHOUT_VIRTUAL_REALITY
        Assert.True(true);
#else
        Assert.True(false, "Expected clean-graph profile constants are missing.");
#endif
    }

    [Fact]
    public void CleanGraphProjectReferencesResolve()
    {
        Assert.Equal("Stride.Engine.Game", typeof(Game).FullName);
        Assert.Equal("Stride.BepuPhysics.BepuSimulation", typeof(BepuSimulation).FullName);
    }

    [Fact]
    public void EffectBytecodeSerializer_IsAvailable_InCleanProfile()
    {
        _ = typeof(EffectBytecode).Assembly;
        _ = typeof(EffectBytecode).FullName;

        var shaderAssembly = typeof(EffectBytecode).Assembly;
        RuntimeHelpers.RunModuleConstructor(shaderAssembly.ManifestModule.ModuleHandle);
        DataSerializerFactory.RegisterSerializationAssembly(shaderAssembly);
        AssemblyRegistry.Register(shaderAssembly, "engine");
        var isShaderAssemblyLoaded = AppDomain.CurrentDomain
            .GetAssemblies()
            .Any(assembly => assembly == shaderAssembly);

        Console.WriteLine($"EffectBytecode assembly: {shaderAssembly.FullName}");
        Console.WriteLine($"EffectBytecode assembly location: {shaderAssembly.Location}");
        Console.WriteLine($"EffectBytecode assembly loaded in AppDomain: {isShaderAssemblyLoaded}");

        var contentSerializer = new DataContentSerializer<EffectBytecode>();
        Assert.NotNull(contentSerializer);
    }

    [Fact]
    public async Task FocusedWarningLane_BepuPhysics_HasZeroWarnings()
    {
        var repoRoot = LocateRepoRoot();
        var scriptPath = Path.Combine(repoRoot, "striv", "build", "striv-check-focused-project.sh");

        var psi = new ProcessStartInfo("bash", $"{Quote(scriptPath)} Stride.BepuPhysics")
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
            throw new TimeoutException($"Focused warning lane timed out after {ProcessTimeout.TotalSeconds:0}s.\nSTDOUT:\n{partialStdOut}\nSTDERR:\n{partialStdErr}");
        }

        var stdout = await stdoutTask;
        var stderr = await stderrTask;

        Assert.True(process.ExitCode == 0, $"Focused warning check failed with exit code {process.ExitCode}.\nSTDOUT:\n{stdout}\nSTDERR:\n{stderr}");
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
