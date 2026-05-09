using System;
using Stride.Games;
using Xunit;

namespace Stride.Engine.Tests;

public class GameLifecycleTests
{
    [Fact]
    public void Game_DefaultConstruction_HasServiceRegistryAndCoreSystems()
    {
        var game = new Game();

        Assert.NotNull(game.Services);
        Assert.NotNull(game.GameSystems);
        Assert.NotNull(game.GraphicsDeviceManager);
        Assert.NotNull(game.Script);
        Assert.NotNull(game.SceneSystem);
        Assert.NotNull(game.Streaming);
        Assert.NotNull(game.SpriteAnimation);
        Assert.NotNull(game.DebugTextSystem);
        Assert.NotNull(game.ProfilingSystem);
    }

    [Fact]
    public void Game_InitializeAssetDatabase_ReturnsProvider()
    {
        var provider = Game.InitializeAssetDatabase();
        Assert.NotNull(provider);
        provider.Dispose();
    }
}
