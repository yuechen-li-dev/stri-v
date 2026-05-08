using System;
using Stride.Core.Mathematics;
using Stride.Graphics;

namespace Stride.Games.Tests.Fakes;

internal sealed class FakeGameWindow : GameWindow
{
    public int DisposeCount { get; private set; }

    public override bool AllowUserResizing { get; set; }
    public override Rectangle ClientBounds { get; } = new(0, 0, 640, 480);
    public override DisplayOrientation CurrentOrientation => DisplayOrientation.Default;
    public override bool IsMinimized => false;
    public override bool Focused => true;
    public override bool IsMouseVisible { get; set; }
    public override WindowHandle NativeWindow { get; } = new(AppContextType.Headless, new object(), IntPtr.Zero);
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

    protected override void Destroy()
    {
        DisposeCount++;
        base.Destroy();
    }
}
