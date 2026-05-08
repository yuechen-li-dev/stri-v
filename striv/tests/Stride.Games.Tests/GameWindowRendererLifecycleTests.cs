using Stride.Core;
using Stride.Games.Tests.Fakes;
using Xunit;

namespace Stride.Games.Tests;

public class GameWindowRendererLifecycleTests
{
    [Fact]
    public void GameWindowRenderer_Initialize_BindsWindowWithoutGraphicsDevice()
    {
        var services = new ServiceRegistry();
        var context = new GameContextHeadless();
        var window = new FakeGameWindow();
        var platform = new FakeGamePlatform(window);
        services.AddService<IGamePlatform>(platform);

        var renderer = new GameWindowRenderer(services, context);

        renderer.Initialize();

        Assert.Same(window, renderer.Window);
        Assert.True(window.Visible);
        Assert.Equal(1, platform.CreateWindowCallCount);
        Assert.Same(context, platform.LastContext);
        Assert.Null(renderer.Presenter);
    }

    [Fact]
    public void GameWindowRenderer_Destroy_ClearsPresenterAndWindow_AndIsIdempotent()
    {
        var services = new ServiceRegistry();
        var context = new GameContextHeadless();
        var window = new FakeGameWindow();
        var platform = new FakeGamePlatform(window);
        services.AddService<IGamePlatform>(platform);

        var renderer = new GameWindowRenderer(services, context);
        renderer.Initialize();

        renderer.Dispose();
        renderer.Dispose();

        Assert.Null(renderer.Window);
        Assert.Null(renderer.Presenter);
        Assert.Equal(1, window.DisposeCount);
    }

    [Fact]
    public void GameWindowRenderer_BeginDraw_BeforeInitialize_ReturnsFalse()
    {
        var services = new ServiceRegistry();
        var context = new GameContextHeadless();
        var renderer = new GameWindowRenderer(services, context);

        var canDraw = renderer.BeginDraw();

        Assert.False(canDraw);
    }
}
