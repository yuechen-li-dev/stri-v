using System;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Games.Tests.Fakes;
using Stride.Graphics;
using Xunit;

namespace Stride.Games.Tests;

public class PlatformWindowLifecycleTests
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
