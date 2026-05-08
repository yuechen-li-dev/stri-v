using System;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Games.Tests.Fakes;
using Stride.Graphics;
using Xunit;

namespace Stride.Games.Tests;

public partial class PlatformWindowLifecycleTests
{
    [Fact]
    public void GameWindow_DefaultLifecycle_AllowsNoNativeHandle()
    {
        var window = new ProbeGameWindow();

        Assert.NotNull(window.Title);
        Assert.Equal(string.Empty, window.Title);
        Assert.Null(window.NativeWindow);
        Assert.Null(window.InitCallback);
        Assert.Null(window.RunCallback);
        Assert.Null(window.ExitCallback);
    }

    [Fact]
    public void GameWindow_EventRaising_WithNoSubscribers_DoesNotThrow()
    {
        var window = new ProbeGameWindow();

        var ex = Record.Exception(() => window.RaiseLifecycleEvents());

        Assert.Null(ex);
    }

    [Fact]
    public void FakeGamePlatform_CreateWindow_AllowsNullableContextWhenContractPermits()
    {
        var window = new FakeGameWindow();
        var platform = new FakeGamePlatform(window);

        var created = platform.CreateWindow(null);

        Assert.Same(window, created);
        Assert.Null(platform.LastContext);
        Assert.Equal(1, platform.CreateWindowCallCount);
    }

    private sealed class ProbeGameWindow : GameWindow
    {
        public override bool AllowUserResizing { get; set; }
        public override Rectangle ClientBounds { get; } = new(0, 0, 100, 100);
        public override DisplayOrientation CurrentOrientation => DisplayOrientation.Default;
        public override bool IsMinimized => false;
        public override bool Focused => true;
        public override bool IsMouseVisible { get; set; }
        public override WindowHandle NativeWindow => null;
        public override bool Visible { get; set; }
        public override double Opacity { get; set; } = 1.0;
        public override bool IsBorderLess { get; set; }

        public override void BeginScreenDeviceChange(bool willBeFullScreen) { }
        public override void EndScreenDeviceChange(int clientWidth, int clientHeight) { }
        protected internal override void Initialize(GameContext gameContext) { }
        internal override void Run() { }
        internal override void Resize(int width, int height) { }
        protected internal override void SetSupportedOrientations(DisplayOrientation orientations) { }
        protected override void SetTitle(string title) { }

        public void RaiseLifecycleEvents()
        {
            OnActivated(this, EventArgs.Empty);
            OnClientSizeChanged(this, EventArgs.Empty);
            OnDeactivated(this, EventArgs.Empty);
            OnOrientationChanged(this, EventArgs.Empty);
            OnFullscreenToggle(this, EventArgs.Empty);
            OnDisableFullScreen(this, EventArgs.Empty);
            OnClosing(this, EventArgs.Empty);
        }
    }
}

public partial class PlatformWindowLifecycleTests
{
    [Fact]
    public void GameBase_Constructs_WithStablePreRunDefaults()
    {
        var game = new ProbeGameBase();

        Assert.NotNull(game.Services);
        Assert.NotNull(game.GameSystems);
        Assert.NotNull(game.UpdateTime);
        Assert.NotNull(game.DrawTime);
        Assert.False(game.IsRunning);
        Assert.False(game.IsExiting);
        Assert.Null(game.Content);
        Assert.Null(game.Context);
        Assert.Null(game.GraphicsDevice);
        Assert.Null(game.GraphicsContext);
    }

    [Fact]
    public void GameBase_PreRunLifecycleMethods_WithNoSubscribers_DoNotThrow()
    {
        var game = new ProbeGameBase();

        var ex = Record.Exception(() =>
        {
            game.TriggerActivated();
            game.TriggerDeactivated();
            game.TriggerExiting();
            game.TriggerWindowCreated();
        });

        Assert.Null(ex);
    }

    [Fact]
    public void GameBase_Exit_BeforeRun_SetsIsExiting_AndDoesNotThrow()
    {
        var game = new ProbeGameBase();

        var ex = Record.Exception(game.Exit);

        Assert.Null(ex);
        Assert.True(game.IsExiting);
        Assert.False(game.IsRunning);
    }

    [Fact]
    public void GamePlatform_MainWindow_BeforeCreateWindow_ThrowsInvalidOperationException()
    {
        var platform = new ProbeGamePlatform(new ProbeGameBase());

        Assert.Throws<InvalidOperationException>(() => _ = platform.MainWindow);
    }

    [Fact]
    public void GamePlatform_Exit_BeforeCreateWindow_DoesNotThrow()
    {
        var platform = new ProbeGamePlatform(new ProbeGameBase());

        var ex = Record.Exception(platform.Exit);

        Assert.Null(ex);
    }

    [Fact]
    public void GamePlatform_PostCreateWindow_DeviceChanged_UsesCreatedWindow()
    {
        var platform = new ProbeGamePlatform(new ProbeGameBase());
        var window = new FakeGameWindow();
        platform.AttachWindow(window);
        var info = new GraphicsDeviceInformation();
        info.PresentationParameters.BackBufferWidth = 800;
        info.PresentationParameters.BackBufferHeight = 600;

        var ex = Record.Exception(() => platform.DeviceChanged(null!, info));

        Assert.Null(ex);
    }

    private sealed class ProbeGamePlatform : GamePlatform
    {
        public ProbeGamePlatform(GameBase game)
            : base(game)
        {
        }

        public override string DefaultAppDirectory => "/tmp";

        internal override GameWindow GetSupportedGameWindow(AppContextType type) => new FakeGameWindow();

        public void AttachWindow(GameWindow window) => gameWindow = window;
    }

    private sealed class ProbeGameBase : GameBase
    {
        public override void ConfirmRenderingSettings(bool gameCreation) { }

        public void TriggerActivated() => OnActivated(this, EventArgs.Empty);
        public void TriggerDeactivated() => OnDeactivated(this, EventArgs.Empty);
        public void TriggerExiting() => OnExiting(this, EventArgs.Empty);
        public void TriggerWindowCreated() => OnWindowCreated();
    }
}
