using Stride.Core;
using Stride.Games;
using Xunit;

namespace Stride.Input.Tests;

public class InputManagerLifecycleTests
{
    [Fact]
    public void InputManager_Constructs_WithVirtualButtonCompatibilitySurface()
    {
        var inputManager = new InputManager();

        Assert.Null(inputManager.VirtualButtonConfigSet);
        Assert.Empty(inputManager.Sources);
        Assert.Empty(inputManager.Events);
    }

    [Fact]
    public void InputManager_AddsSimulatedSource_AndUpdatesWithoutPlatformBackend()
    {
        var inputManager = new InputManager();
        var simulatedSource = new InputSourceSimulated();

        inputManager.Sources.Add(simulatedSource);
        simulatedSource.AddKeyboard();

        var exception = Record.Exception(() => inputManager.Update(new GameTime(TimeSpan.Zero, TimeSpan.Zero)));

        Assert.Null(exception);
        Assert.True(inputManager.HasKeyboard);
        Assert.Single(inputManager.Keyboards);
        Assert.Equal(simulatedSource, inputManager.Keyboards[0].Source);
    }

    [Fact]
    public void InputManager_DeviceCallbacks_RegisterAndRemoveDevices()
    {
        var inputManager = new InputManager();
        var simulatedSource = new InputSourceSimulated();

        inputManager.Sources.Add(simulatedSource);
        var keyboard = simulatedSource.AddKeyboard();
        var mouse = simulatedSource.AddMouse();

        Assert.True(inputManager.HasKeyboard);
        Assert.True(inputManager.HasMouse);
        Assert.Contains(keyboard, inputManager.Keyboards);
        Assert.Contains(mouse, inputManager.Pointers);

        simulatedSource.RemoveKeyboard(keyboard);
        simulatedSource.RemoveMouse(mouse);

        Assert.False(inputManager.HasKeyboard);
        Assert.False(inputManager.HasMouse);
        Assert.Empty(inputManager.Keyboards);
        Assert.Empty(inputManager.Pointers);
    }

    [Fact]
    public void InputManager_Update_DoesNotRequireGameContextBeforeInitialization()
    {
        var inputManager = new InputManager();

        var exception = Record.Exception(() => inputManager.Update(new GameTime(TimeSpan.Zero, TimeSpan.FromMilliseconds(16))));

        Assert.Null(exception);
    }

    [Fact]
    public void InputManager_SourceRegistration_DuplicateAddFailsPredictably()
    {
        var inputManager = new InputManager();
        var simulatedSource = new InputSourceSimulated();

        inputManager.Sources.Add(simulatedSource);

        var exception = Assert.Throws<InvalidOperationException>(() => inputManager.Sources.Add(simulatedSource));
        Assert.Contains("already added", exception.Message);
    }
}
