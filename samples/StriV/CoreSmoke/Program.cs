using Stride.Engine;
using Stride.Games;

using var game = new CoreSmokeGame();
game.Run();

internal sealed class CoreSmokeGame : Game
{
    private int frameCount;

    protected override void Update(GameTime gameTime)
    {
        base.Update(gameTime);

        if (++frameCount >= 1)
            Exit();
    }
}
