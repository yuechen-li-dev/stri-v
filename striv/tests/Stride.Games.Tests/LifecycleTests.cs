// Required for interface event contracts in lightweight test doubles.
#pragma warning disable CS0067
using System;
using System.Collections.Generic;
using Stride.Core;
using Xunit;

namespace Stride.Games.Tests;

public class LifecycleTests
{
    [Fact]
    public void GameTime_Constructs_WithStableDefaults()
    {
        var gameTime = new GameTime();

        Assert.Equal(TimeSpan.Zero, gameTime.Elapsed);
        Assert.Equal(TimeSpan.Zero, gameTime.Total);
        Assert.Equal(TimeSpan.Zero, gameTime.WarpElapsed);
        Assert.Equal(0, gameTime.FrameCount);
        Assert.Equal(0f, gameTime.FramePerSecond);
        Assert.Equal(TimeSpan.Zero, gameTime.TimePerFrame);
        Assert.False(gameTime.FramePerSecondUpdated);
        Assert.Equal(1d, gameTime.Factor);

        gameTime.Factor = -1.0;
        Assert.Equal(0d, gameTime.Factor);

        gameTime.ResetTimeFactor();
        Assert.Equal(1d, gameTime.Factor);
    }

    [Fact]
    public void GameContextHeadless_ConstructsWithoutNativeWindow()
    {
        var context = new GameContextHeadless(1234, 567);

        Assert.Equal(AppContextType.Headless, context.ContextType);
        Assert.False(context.IsUserManagingRun);
        Assert.Equal(1234, context.RequestedWidth);
        Assert.Equal(567, context.RequestedHeight);
        Assert.Null(context.Control);
        Assert.Null(context.RunCallback);
        Assert.Null(context.ExitCallback);
    }

    [Fact]
    public void GameWindowHeadless_ConstructsWithoutNativeHandle()
    {
        var window = new GameWindowHeadless();

        Assert.Null(window.NativeWindow);
        Assert.True(window.Focused);
        Assert.False(window.IsMinimized);
        Assert.Equal(1.0, window.Opacity);

        Assert.Equal(0, window.ClientBounds.Width);
        Assert.Equal(0, window.ClientBounds.Height);

        window.Resize(640, 360);
        Assert.Equal(640, window.ClientBounds.Width);
        Assert.Equal(360, window.ClientBounds.Height);
    }

    [Fact]
    public void GameSystemCollection_AddRemove_PreservesOrder()
    {
        var systems = new GameSystemCollection(new ServiceRegistry());
        var first = new TestSystem(updateOrder: 20, drawOrder: 20);
        var second = new TestSystem(updateOrder: 10, drawOrder: 10);
        var third = new TestSystem(updateOrder: 30, drawOrder: 30);

        systems.Add(first);
        systems.Add(second);
        systems.Add(third);

        Assert.Collection(
            systems,
            item => Assert.Same(first, item),
            item => Assert.Same(second, item),
            item => Assert.Same(third, item));

        systems.Remove(second);

        Assert.Collection(
            systems,
            item => Assert.Same(first, item),
            item => Assert.Same(third, item));
    }

    [Fact]
    public void GameSystemCollection_UpdateDraw_UsesEnabledVisibleOrder()
    {
        var systems = new GameSystemCollection(new ServiceRegistry());
        var calls = new List<string>();

        var drawFirst = new TestSystem(updateOrder: 20, drawOrder: 10, calls: calls, id: "draw-first");
        var updateFirst = new TestSystem(updateOrder: 10, drawOrder: 20, calls: calls, id: "update-first");
        var disabled = new TestSystem(updateOrder: 30, drawOrder: 30, calls: calls, id: "disabled") { Enabled = false };
        var invisible = new TestSystem(updateOrder: 40, drawOrder: 40, calls: calls, id: "invisible") { Visible = false };

        systems.Add(drawFirst);
        systems.Add(updateFirst);
        systems.Add(disabled);
        systems.Add(invisible);

        systems.Initialize();

        var gameTime = new GameTime();
        systems.Update(gameTime);
        systems.Draw(gameTime);

        Assert.Equal(
            new[]
            {
                "update:update-first",
                "update:draw-first",
                "update:invisible",
                "begin:draw-first",
                "draw:draw-first",
                "end:draw-first",
                "begin:update-first",
                "draw:update-first",
                "end:update-first",
                "begin:disabled",
                "draw:disabled",
                "end:disabled"
            },
            calls);
    }

    private sealed class TestSystem : ComponentBase, IGameSystemBase, IUpdateable, IDrawable
    {
        private readonly List<string>? calls;
        private readonly string id;

        public TestSystem(int updateOrder, int drawOrder, List<string>? calls = null, string id = "system")
        {
            UpdateOrder = updateOrder;
            DrawOrder = drawOrder;
            Enabled = true;
            Visible = true;
            this.calls = calls;
            this.id = id;
        }

        public event EventHandler<EventArgs>? EnabledChanged;
        public event EventHandler<EventArgs>? UpdateOrderChanged;
        public event EventHandler<EventArgs>? DrawOrderChanged;
        public event EventHandler<EventArgs>? VisibleChanged;

        public bool Enabled { get; set; }
        public int UpdateOrder { get; set; }
        public bool Visible { get; set; }
        public int DrawOrder { get; set; }

        public void Initialize() { }

        public void Update(GameTime gameTime) => calls?.Add($"update:{id}");
        public bool BeginDraw()
        {
            calls?.Add($"begin:{id}");
            return true;
        }

        public void Draw(GameTime gameTime) => calls?.Add($"draw:{id}");
        public void EndDraw() => calls?.Add($"end:{id}");
    }
}
