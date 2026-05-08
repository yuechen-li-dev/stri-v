using System.Linq;
using Stride.Core.Mathematics;
using Stride.Games;
using Xunit;

namespace Stride.Input.Tests;

public class InputRuntimeSmokeTests
{
    private static InputManager CreateInitializedInputManager()
    {
        var inputManager = new InputManager();
        inputManager.Initialize(new GameContextHeadless());
        return inputManager;
    }

    [Fact]
    public void SimulatedInputSource_CanRegisterAndUpdateThroughInputManager()
    {
        var inputManager = CreateInitializedInputManager();
        var simulatedSource = new InputSourceSimulated();

        inputManager.Sources.Add(simulatedSource);

        var keyboard = simulatedSource.AddKeyboard();
        var mouse = simulatedSource.AddMouse();
        var gamePad = simulatedSource.AddGamePad();

        var firstUpdateException = Record.Exception(() => inputManager.Update(new GameTime(TimeSpan.Zero, TimeSpan.FromMilliseconds(16))));
        var secondUpdateException = Record.Exception(() => inputManager.Update(new GameTime(TimeSpan.FromMilliseconds(16), TimeSpan.FromMilliseconds(16))));

        Assert.Null(firstUpdateException);
        Assert.Null(secondUpdateException);
        Assert.Contains(keyboard, inputManager.Keyboards);
        Assert.Contains(mouse, inputManager.Pointers);
        Assert.Contains(gamePad, inputManager.GamePads);
        Assert.True(inputManager.HasKeyboard);
        Assert.True(inputManager.HasMouse);
        Assert.True(inputManager.HasGamePad);
    }

    [Fact]
    public void SimulatedKeyboardOrMouse_StateChangeFlowsThroughManager()
    {
        var inputManager = CreateInitializedInputManager();
        var simulatedSource = new InputSourceSimulated();

        inputManager.Sources.Add(simulatedSource);

        var keyboard = simulatedSource.AddKeyboard();
        var mouse = simulatedSource.AddMouse();

        keyboard.SimulateDown(Keys.Space);
        mouse.SimulateMouseDown(MouseButton.Left);
        mouse.SetPosition(new Vector2(0.25f, 0.75f));

        inputManager.Update(new GameTime(TimeSpan.Zero, TimeSpan.FromMilliseconds(16)));

        Assert.Contains(Keys.Space, inputManager.PressedKeys);
        Assert.Contains(Keys.Space, inputManager.DownKeys);
        Assert.Contains(MouseButton.Left, inputManager.PressedButtons);
        Assert.Contains(MouseButton.Left, inputManager.DownButtons);
        Assert.Equal(new Vector2(0.25f, 0.75f), inputManager.MousePosition);

        keyboard.SimulateUp(Keys.Space);
        mouse.SimulateMouseUp(MouseButton.Left);

        inputManager.Update(new GameTime(TimeSpan.FromMilliseconds(16), TimeSpan.FromMilliseconds(16)));

        Assert.Contains(Keys.Space, inputManager.ReleasedKeys);
        Assert.DoesNotContain(Keys.Space, inputManager.DownKeys);
        Assert.Contains(MouseButton.Left, inputManager.ReleasedButtons);
        Assert.DoesNotContain(MouseButton.Left, inputManager.DownButtons);
    }

    [Fact]
    public void SimulatedGamePad_StateChangeFlowsThroughManager()
    {
        var inputManager = CreateInitializedInputManager();
        var simulatedSource = new InputSourceSimulated();

        inputManager.Sources.Add(simulatedSource);

        var gamePad = simulatedSource.AddGamePad();

        gamePad.SetButton(GamePadButton.A, true);
        gamePad.SetAxis(GamePadAxis.LeftTrigger, 0.5f);
        gamePad.SetAxis(GamePadAxis.LeftThumbX, -0.75f);

        inputManager.Update(new GameTime(TimeSpan.Zero, TimeSpan.FromMilliseconds(16)));

        Assert.True(inputManager.HasGamePad);
        Assert.NotNull(inputManager.DefaultGamePad);
        Assert.Contains(inputManager.Events, evt => evt is GamePadButtonEvent);
        Assert.Equal(2, inputManager.Events.Count(evt => evt is GamePadAxisEvent));

        gamePad.SetButton(GamePadButton.A, false);
        var releaseUpdateException = Record.Exception(() => inputManager.Update(new GameTime(TimeSpan.FromMilliseconds(16), TimeSpan.FromMilliseconds(16))));

        Assert.Null(releaseUpdateException);
    }
}
