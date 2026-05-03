using Stride.BepuPhysics;
using Stride.Engine;
using Stride.Core;
using Stride.Core.Mathematics;
using Xunit;

namespace StriV.CleanGraph.Tests;

public class CleanGraphSmokeTests
{
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
}
