using Stride.Rendering.Compositing;
using Stride.Graphics;
using Xunit;

namespace Stride.Engine.Tests;

public class ForwardRendererLifecycleTests
{
    [Fact]
    public void ForwardRenderer_DefaultConstruction_HasSafeDefaultConfiguration()
    {
        var renderer = new ForwardRenderer();

        Assert.NotNull(renderer.Clear);
        Assert.True(renderer.LightProbes);
        Assert.NotNull(renderer.ShadowMapRenderStages);
        Assert.Empty(renderer.ShadowMapRenderStages);
        Assert.NotNull(renderer.MSAAResolver);
    }

    [Fact]
    public void ForwardRenderer_DefaultConstruction_LeavesOptionalRenderLinksUnset()
    {
        var renderer = new ForwardRenderer();

        Assert.Null(renderer.OpaqueRenderStage);
        Assert.Null(renderer.TransparentRenderStage);
        Assert.Null(renderer.GBufferRenderStage);
        Assert.Null(renderer.PostEffects);
        Assert.Null(renderer.LightShafts);
        Assert.Null(renderer.SubsurfaceScatteringBlurEffect);
    }

    [Fact]
    public void ForwardRenderer_DefaultConstruction_DoesNotRequireGraphicsDeviceForConfigurationAccess()
    {
        var renderer = new ForwardRenderer();

        Assert.Equal(MultisampleCount.None, renderer.MSAALevel);
        Assert.True(renderer.BindDepthAsResourceDuringTransparentRendering);
        Assert.False(renderer.BindOpaqueAsResourceDuringTransparentRendering);
    }
}
