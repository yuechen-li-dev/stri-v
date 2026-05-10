using Stride.Core;
using Stride.Profiling;
using Xunit;

namespace Stride.Engine.Tests;

public class GameProfilingSystemLifecycleTests
{
    [Fact]
    public void GameProfilingSystem_DefaultConstruction_HasValidInertState()
    {
        var system = new GameProfilingSystem(new ServiceRegistry());

        Assert.False(system.Enabled);
        Assert.False(system.Visible);
        Assert.Equal(GameProfilingResults.Fps, system.FilteringMode);
        Assert.Equal(GameProfilingSorting.ByTime, system.SortingMode);
        Assert.Equal(500, system.RefreshTime);
        Assert.Equal((uint)1, system.CurrentResultPage);
        Assert.Null(system.RenderTarget);
    }

    [Fact]
    public void GameProfilingSystem_DisableProfiling_IsIdempotent()
    {
        var system = new GameProfilingSystem(new ServiceRegistry());

        var first = Record.Exception(system.DisableProfiling);
        var second = Record.Exception(system.DisableProfiling);

        Assert.Null(first);
        Assert.Null(second);
        Assert.False(system.Enabled);
        Assert.False(system.Visible);
        Assert.Equal(GameProfilingResults.Fps, system.FilteringMode);
    }
}
