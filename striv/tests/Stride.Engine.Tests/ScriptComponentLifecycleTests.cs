using Stride.Engine.Processors;
using Xunit;

namespace Stride.Engine.Tests;

public class ScriptComponentLifecycleTests
{
    [Fact]
    public void ScriptComponent_DefaultConstruction_HasValidEmptyState()
    {
        var script = new TestScriptComponent();

        Assert.Null(script.Services);
        Assert.Null(script.Game);
        Assert.Null(script.GraphicsDevice);
        Assert.NotNull(script.ProfilingKey);
    }

    [Fact]
    public void ScriptComponent_DefaultConstruction_DoesNotRequireRuntimeInjectionForBasicAccess()
    {
        var script = new TestScriptComponent();

        var exception = Record.Exception(() =>
        {
            _ = script.Content;
            _ = script.GameProfiler;
            _ = script.Input;
            _ = script.Script;
            _ = script.SceneSystem;
            _ = script.EffectSystem;
            _ = script.DebugText;
            _ = script.SpriteAnimation;
            _ = script.Streaming;
            _ = script.ProfilingKey;
        });

        Assert.Null(exception);
    }

    private sealed class TestScriptComponent : ScriptComponent
    {
    }
}
