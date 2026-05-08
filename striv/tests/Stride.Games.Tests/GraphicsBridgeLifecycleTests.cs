using Stride.Core;
using Stride.Graphics;
using Xunit;

namespace Stride.Games.Tests;

public class GraphicsBridgeLifecycleTests
{
    [Fact]
    public void GameGraphicsParameters_Constructs_WithStableDefaults()
    {
        var parameters = new GameGraphicsParameters();

        Assert.Equal(0, parameters.PreferredBackBufferWidth);
        Assert.Equal(0, parameters.PreferredBackBufferHeight);
        Assert.Equal(PixelFormat.None, parameters.PreferredBackBufferFormat);
        Assert.Equal(PixelFormat.None, parameters.PreferredDepthStencilFormat);
        Assert.False(parameters.IsFullScreen);
        Assert.NotNull(parameters.PreferredGraphicsProfile);
        Assert.Empty(parameters.PreferredGraphicsProfile);
        Assert.Null(parameters.RequiredAdapterUid);
    }

    [Fact]
    public void GraphicsDeviceInformation_Constructs_WithStableDefaults()
    {
        var info = new GraphicsDeviceInformation();

        Assert.NotNull(info.Adapter);
        Assert.NotNull(info.PresentationParameters);
        Assert.Equal(default, info.GraphicsProfile);
        Assert.Equal(default, info.DeviceCreationFlags);
    }

    [Fact]
    public void GameWindowRenderer_Constructs_WithoutPresenter()
    {
        var services = new ServiceRegistry();
        var gameContext = new GameContextHeadless();

        var renderer = new GameWindowRenderer(services, gameContext);

        Assert.Same(gameContext, renderer.GameContext);
        Assert.Null(renderer.Window);
        Assert.Null(renderer.Presenter);
    }
}
