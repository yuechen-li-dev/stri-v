using Stride.Rendering.Background;
using Stride.Rendering.Compositing;
using Stride.Rendering.Sprites;
using Xunit;

namespace Stride.Engine.Tests;

public class RenderingLifecycleConstructionTests
{
    [Fact]
    public void GraphicsCompositor_DefaultConstruction_HasValidCollections()
    {
        var compositor = new GraphicsCompositor();

        Assert.NotNull(compositor.Cameras);
        Assert.Empty(compositor.Cameras);
        Assert.NotNull(compositor.RenderStages);
        Assert.NotNull(compositor.RenderFeatures);
        Assert.Null(compositor.Game);
        Assert.Null(compositor.SingleView);
        Assert.Null(compositor.Editor);
    }

    [Fact]
    public void SceneCameraRenderer_DefaultConstruction_LeavesOptionalSceneCameraUnset()
    {
        var renderer = new SceneCameraRenderer();

        Assert.NotNull(renderer.RenderView);
        Assert.Null(renderer.Camera);
        Assert.Null(renderer.Child);
    }

    [Fact]
    public void SpriteRenderProcessor_DefaultConstruction_DoesNotRequireGraphicsDevice()
    {
        var processor = new SpriteRenderProcessor();

        Assert.Null(processor.VisibilityGroup);
    }

    [Fact]
    public void BackgroundRenderProcessor_DefaultConstruction_DoesNotRequireGraphicsDevice()
    {
        var processor = new BackgroundRenderProcessor();

        Assert.Null(processor.VisibilityGroup);
        Assert.Null(processor.ActiveBackground);
    }
}
