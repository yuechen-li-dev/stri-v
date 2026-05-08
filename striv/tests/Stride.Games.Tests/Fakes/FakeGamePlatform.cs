namespace Stride.Games.Tests.Fakes;

internal sealed class FakeGamePlatform : IGamePlatform
{
    private readonly FakeGameWindow window;

    public FakeGamePlatform(FakeGameWindow window)
    {
        this.window = window;
    }

    public string DefaultAppDirectory => "/tmp";
    public GameWindow MainWindow => window;
    public int CreateWindowCallCount { get; private set; }
    public GameContext? LastContext { get; private set; }

    public GameWindow CreateWindow(GameContext? gameContext = null)
    {
        CreateWindowCallCount++;
        LastContext = gameContext;
        return window;
    }
}
